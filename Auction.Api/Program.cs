using Auction.Api.BackgroundServices;
using Auction.Api.Configuration;
using Auction.Api.Middlewares;
using Auction.Core.Interfaces;
using Auction.Core.Mapping;
using Auction.Core.Settings;
using Auction.Data;
using Auction.Data.Repositories;
using Auction.Services;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// NLog replaces the built-in loggers; its settings are in nlog.config.
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Add services to the container.

// Locally the connection string comes from User Secrets; on Render - from an environment variable (as a URL).
var connectionString = DatabaseUrl.ToConnectionString(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty);
builder.Services.AddDbContext<AuctionDbContext>(options => options.UseNpgsql(connectionString));

// On Render, HTTPS ends at Render's proxy and the request reaches us as HTTP.
// The proxy sends the original scheme in X-Forwarded-Proto - this makes ASP.NET use it.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear(); // the proxy address is not known in advance
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();
builder.Services.AddScoped<IBidRepository, BidRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuctionService, AuctionService>();
builder.Services.AddScoped<IBidService, BidService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

// Fails at startup (not at the first login) if a JWT setting is missing or the key is too short.
builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer), "Jwt:Issuer is missing.")
    .Validate(s => !string.IsNullOrWhiteSpace(s.Audience), "Jwt:Audience is missing.")
    .Validate(s => s.LifetimeMinutes > 0, "Jwt:LifetimeMinutes must be positive.")
    .Validate(s => s.Key.Length >= JwtSettings.MinKeyLength,
        $"Jwt:Key must be at least {JwtSettings.MinKeyLength} characters. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<key>\"")
    .ValidateOnStart();

// How the server checks a token that arrives in the "Authorization: Bearer <token>" header.
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // the default allows 5 extra minutes after expiry
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });

builder.Services.AddHostedService<AuctionClosingService>();

builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Adds the "Authorize" button: paste the token from POST api/auth/login,
    // and Swagger sends it as "Authorization: Bearer <token>" on every request.
    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste only the token (without the word Bearer).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { bearerScheme, Array.Empty<string>() } });
});

var app = builder.Build();

// On Render there is no Package Manager Console - the Migrations are applied when the app starts.
// Locally this stays off (Update-Database is run by hand, as before).
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AuctionDbContext>().Database.MigrateAsync();
}

// Development only (Seed:Enabled is in appsettings.Development.json): fills an empty DB with demo data.
// The demo users' password is not in the code - it is in User Secrets:
// dotnet user-secrets set "Seed:Password" "<password>" --project Auction.Api
var seedPassword = app.Configuration["Seed:Password"];
if (app.Configuration.GetValue<bool>("Seed:Enabled"))
{
    if (string.IsNullOrEmpty(seedPassword))
    {
        app.Logger.LogWarning("Seed:Password is missing in User Secrets - demo data was not created.");
    }
    else
    {
        using var scope = app.Services.CreateScope();
        await DataSeeder.SeedAsync(
            scope.ServiceProvider.GetRequiredService<AuctionDbContext>(),
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            seedPassword);
    }
}

// Our middlewares come first, so they wrap everything after them:
// 1. RequestLogging - outermost: sees every request and the final status code (also a 500).
// 2. ErrorHandling  - inside it: turns any unhandled exception into a 500 response,
//    so RequestLogging logs "finished: 500" instead of a crash.
app.UseForwardedHeaders();
app.UseRequestLogging();
app.UseErrorHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// The React app (built into wwwroot by the Dockerfile) is served by the same server - one address, no CORS.
// Before authentication: the site files are public. Locally there is no wwwroot
// (React runs on its own with npm run dev), so these lines do nothing.
app.UseDefaultFiles();
app.UseStaticFiles();

// Order matters: first find out WHO the user is (reads the JWT and fills HttpContext.User),
// then check WHAT they are allowed to do ([Authorize]).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// An unknown /api address stays a real 404 (JSON), and every other address
// (e.g. /auctions/5 after a page refresh) gets index.html, so React Router can show the page.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();

// Makes the Program class visible to the test project (WebApplicationFactory<Program>).
public partial class Program { }

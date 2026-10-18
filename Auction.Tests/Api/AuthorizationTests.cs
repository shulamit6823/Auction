using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Auction.Tests.Api;

using Auction.Core.Entities;
using Auction.Core.Interfaces;

public class AuthorizationTests : IClassFixture<AuctionApiFactory>
{
    private readonly AuctionApiFactory _factory;

    public AuthorizationTests(AuctionApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Guest_CreateAuction_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/auctions", new { Title = "Bike", StartingPrice = 10, DurationAmount = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "api/users")]
    [InlineData("GET", "api/auctions/all")]
    [InlineData("PUT", "api/auctions/1/approve")]
    [InlineData("DELETE", "api/auctions/1")]
    public async Task RegularUser_AdminAction_Returns403(string method, string url)
    {
        var client = CreateClientFor(UserRole.User);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("api/users")]
    [InlineData("api/auctions/all")]
    public async Task Admin_CanSeeUsersAndAllAuctions(string url)
    {
        var client = CreateClientFor(UserRole.Admin);

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateClientFor(UserRole role)
    {
        // A real token, created by the same service the Login uses.
        var tokenService = _factory.Services.GetRequiredService<ITokenService>();
        var (token, _) = tokenService.CreateToken(new User { Id = 10, Username = role.ToString(), Role = role });

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

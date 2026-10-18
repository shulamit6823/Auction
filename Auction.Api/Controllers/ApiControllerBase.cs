using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction.Api.Controllers;

using Auction.Core.Entities;

// Secure by default: every endpoint requires a logged-in user (a valid JWT),
// unless the action is explicitly marked [AllowAnonymous].
[Authorize]
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // The logged-in user's id comes only from the token claims, never from the client.
    // Returns null only when there is no valid token.
    protected int? CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    // Read from the Role claim in the token.
    protected bool IsAdmin => User.IsInRole(nameof(UserRole.Admin));

    // Errors are returned as ProblemDetails - the same JSON format ASP.NET uses
    // for its automatic 400 responses, so the client handles one error format.
    protected ObjectResult NotFoundProblem(string message)
    {
        return Problem(detail: message, statusCode: StatusCodes.Status404NotFound);
    }

    protected ObjectResult BadRequestProblem(string message)
    {
        return Problem(detail: message, statusCode: StatusCodes.Status400BadRequest);
    }

    // 409: the data changed since the client read it. The client should reload and decide again.
    protected ObjectResult ConflictProblem(string message)
    {
        return Problem(detail: message, statusCode: StatusCodes.Status409Conflict);
    }

    // 403: logged in, but not allowed to change this item.
    protected ObjectResult ForbiddenProblem(string message)
    {
        return Problem(detail: message, statusCode: StatusCodes.Status403Forbidden);
    }

    protected ObjectResult UnauthorizedProblem(string message)
    {
        return Problem(detail: message, statusCode: StatusCodes.Status401Unauthorized);
    }
}

using AleTrack.Common.Utils;
using AleTrack.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AleTrack.Features.Users.Commands.Logout;

/// <summary>
/// Request to revoke a refresh token (logout)
/// </summary>
public sealed record LogoutRequest
{
    /// <summary>
    /// Body of the request
    /// </summary>
    [FromBody]
    public LogoutDto Data { get; set; } = null!;
}

/// <summary>
/// DTO containing the refresh token to revoke
/// </summary>
public sealed record LogoutDto
{
    /// <summary>
    /// The refresh token to revoke
    /// </summary>
    public string RefreshToken { get; set; } = null!;
}

/// <summary>
/// Endpoint to revoke a refresh token, invalidating it from further use
/// </summary>
public sealed class LogoutEndpoint(AleTrackDbContext dbContext, IJwtService jwtService) :
    Endpoint<LogoutRequest>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("logout");
        Description(b => b
            .Produces(StatusCodes.Status204NoContent)
            .WithName(nameof(LogoutEndpoint)));

        DontCatchExceptions();
        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "Revoke a refresh token";
            s.Responses[StatusCodes.Status204NoContent] = "Token revoked";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(LogoutRequest req, CancellationToken ct)
    {
        var hashedToken = jwtService.HashToken(req.Data.RefreshToken);

        var refreshToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken, ct);

        if (refreshToken is not null && !refreshToken.IsRevoked)
        {
            refreshToken.IsRevoked = true;
            await dbContext.SaveChangesAsync(ct);
        }

        await Send.NoContentAsync(ct);
    }
}

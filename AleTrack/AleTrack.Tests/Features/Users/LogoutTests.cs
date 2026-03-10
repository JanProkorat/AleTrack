using AleTrack.Common.Utils;
using AleTrack.Entities;
using AleTrack.Features.Users.Commands.Logout;
using AleTrack.Tests.Builders;
using AleTrack.Tests.Mocks;
using FluentAssertions;
using Moq;

namespace AleTrack.Tests.Features.Users;

public sealed class LogoutTests
{
    [Fact]
    public async Task HandleAsync_ValidToken_RevokesTokenAndReturnsNoContent()
    {
        var rawToken = "valid-refresh-token";
        var hashedToken = "hashed-token";

        var user = UserBuilder.BuildEntity();
        var refreshToken = new RefreshToken
        {
            Id = 1,
            UserId = user.Id,
            User = user,
            Token = hashedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false
        };

        var dbContext = AleTrackDbContextMockFactory.CreateMock(
            users: [user],
            refreshTokens: [refreshToken]);
        var jwtService = new Mock<IJwtService>();

        jwtService.Setup(j => j.HashToken(rawToken)).Returns(hashedToken);
        dbContext.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var request = new LogoutRequest
        {
            Data = new LogoutDto { RefreshToken = rawToken }
        };

        var endpoint = EndpointBuilder<LogoutRequest, LogoutEndpoint>
            .Create(dbContext.Object, jwtService.Object);
        await endpoint.HandleAsync(request, CancellationToken.None);

        refreshToken.IsRevoked.Should().BeTrue();
        jwtService.Verify(j => j.HashToken(rawToken), Times.Once);
        dbContext.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TokenNotFound_ReturnsNoContentWithoutSaving()
    {
        var rawToken = "unknown-token";
        var hashedToken = "unknown-hashed";

        var dbContext = AleTrackDbContextMockFactory.CreateMock();
        var jwtService = new Mock<IJwtService>();

        jwtService.Setup(j => j.HashToken(rawToken)).Returns(hashedToken);

        var request = new LogoutRequest
        {
            Data = new LogoutDto { RefreshToken = rawToken }
        };

        var endpoint = EndpointBuilder<LogoutRequest, LogoutEndpoint>
            .Create(dbContext.Object, jwtService.Object);
        await endpoint.HandleAsync(request, CancellationToken.None);

        jwtService.Verify(j => j.HashToken(rawToken), Times.Once);
        dbContext.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AlreadyRevokedToken_ReturnsNoContentWithoutSaving()
    {
        var rawToken = "already-revoked-token";
        var hashedToken = "hashed-revoked";

        var user = UserBuilder.BuildEntity();
        var refreshToken = new RefreshToken
        {
            Id = 1,
            UserId = user.Id,
            User = user,
            Token = hashedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = true
        };

        var dbContext = AleTrackDbContextMockFactory.CreateMock(
            users: [user],
            refreshTokens: [refreshToken]);
        var jwtService = new Mock<IJwtService>();

        jwtService.Setup(j => j.HashToken(rawToken)).Returns(hashedToken);

        var request = new LogoutRequest
        {
            Data = new LogoutDto { RefreshToken = rawToken }
        };

        var endpoint = EndpointBuilder<LogoutRequest, LogoutEndpoint>
            .Create(dbContext.Object, jwtService.Object);
        await endpoint.HandleAsync(request, CancellationToken.None);

        jwtService.Verify(j => j.HashToken(rawToken), Times.Once);
        dbContext.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

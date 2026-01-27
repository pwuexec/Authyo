using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Users;
using Authy.Presentation.Entitites;
using Authy.Presentation.Persistence.Repositories;
using Authy.Presentation.Shared.Abstractions;
using NSubstitute;

namespace Authy.UnitTests.Domain.Users;

[TestClass]
public class RefreshTokenCommandHandlerTests : TestBase
{
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private UserRepository _userRepository = null!;
    private RefreshTokenRepository _refreshTokenRepository = null!;
    private RefreshTokenCommandHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _userRepository = new UserRepository(DbContext);
        _refreshTokenRepository = new RefreshTokenRepository(DbContext);
        _handler = new RefreshTokenCommandHandler(
            _refreshTokenRepository,
            _userRepository,
            _jwtService,
            TimeProvider);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_RefreshTokenNotFound()
    {
        // Arrange
        const string accessToken = "old-access-token";
        const string refreshToken = "non-existent-refresh-token";
        var command = new RefreshTokenCommand(accessToken, refreshToken);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.RefreshToken.Invalid, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_RefreshTokenRevoked()
    {
        // Arrange
        const string tokenValue = "revoked-token";
        const string accessToken = "old-access-token";

        var refreshToken = new RefreshToken 
        { 
            Token = tokenValue, 
            RevokedOn = TimeProvider.GetUtcNow().UtcDateTime 
        };

        await DbContext.RefreshTokens.AddAsync(refreshToken, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var command = new RefreshTokenCommand(accessToken, refreshToken.Token);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.RefreshToken.Revoked, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_RefreshTokenExpired()
    {
        // Arrange
        const string tokenValue = "expired-token";
        const string accessToken = "old-access-token";

        var refreshToken = new RefreshToken 
        { 
            Token = tokenValue, 
            ExpiresOn = TimeProvider.GetUtcNow().UtcDateTime.AddHours(-1) 
        };
        await DbContext.RefreshTokens.AddAsync(refreshToken, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var command = new RefreshTokenCommand(accessToken, refreshToken.Token);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.RefreshToken.Expired, result.Error);
    }

}

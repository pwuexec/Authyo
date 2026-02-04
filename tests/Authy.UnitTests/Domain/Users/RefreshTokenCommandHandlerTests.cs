using Authy.Application.Domain;
using Authy.Application.Domain.Users;
using Authy.Application.Entitites;
using Authy.Application.Data.Repositories;
using Authy.Application.Shared.Abstractions;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_AccessTokenInvalid()
    {
        // Arrange
        const string tokenValue = "valid-refresh-token";
        const string invalidAccessToken = "not-a-jwt";

        var refreshToken = new RefreshToken
        {
            Token = tokenValue,
            ExpiresOn = TimeProvider.GetUtcNow().UtcDateTime.AddDays(1)
        };
        await DbContext.RefreshTokens.AddAsync(refreshToken, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var command = new RefreshTokenCommand(invalidAccessToken, refreshToken.Token);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.AccessToken.Invalid, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_JtiMismatch()
    {
        // Arrange
        const string tokenValue = "valid-refresh-token";
        const string jtiInRefreshToken = "jti-1";
        const string jtiInAccessToken = "jti-2";

        var refreshToken = new RefreshToken
        {
            Token = tokenValue,
            ExpiresOn = TimeProvider.GetUtcNow().UtcDateTime.AddDays(1),
            JwtId = jtiInRefreshToken
        };
        await DbContext.RefreshTokens.AddAsync(refreshToken, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var accessToken = GenerateJwtToken(jtiInAccessToken);
        var command = new RefreshTokenCommand(accessToken, refreshToken.Token);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.RefreshToken.Mismatch, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_UserNotFound()
    {
        // Arrange
        const string tokenValue = "valid-refresh-token";
        const string jti = "jti-1";

        var refreshToken = new RefreshToken
        {
            Token = tokenValue,
            ExpiresOn = TimeProvider.GetUtcNow().UtcDateTime.AddDays(1),
            JwtId = jti,
            UserId = Guid.NewGuid() // Random User ID that doesn't exist
        };
        await DbContext.RefreshTokens.AddAsync(refreshToken, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var accessToken = GenerateJwtToken(jti);
        var command = new RefreshTokenCommand(accessToken, refreshToken.Token);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.User.NotFound, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_RotateTokens_When_Valid()
    {
        // Arrange
        const string tokenValue = "valid-refresh-token";
        const string jti = "jti-1";
        const string newAccessToken = "new-access-token";
        const string newJti = "new-jti";

        var expectedTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        TimeProvider.SetUtcNow(expectedTime);

        var user = new User
        {
            Name = "Test User",
            Organization = new Organization { Name = "Test Org" }
        };
        await DbContext.Users.AddAsync(user, TestContext.CancellationToken);

        var refreshToken = new RefreshToken
        {
            Token = tokenValue,
            ExpiresOn = TimeProvider.GetUtcNow().UtcDateTime.AddDays(1),
            JwtId = jti,
            UserId = user.Id
        };
        await DbContext.RefreshTokens.AddAsync(refreshToken, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        _jwtService.GenerateToken(user.Id, user.Name, Arg.Any<List<string>>())
            .Returns((newAccessToken, newJti));

        var accessToken = GenerateJwtToken(jti);
        var command = new RefreshTokenCommand(accessToken, refreshToken.Token, "127.0.0.1", "test-agent");

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(newAccessToken, result.Value.AccessToken);
        Assert.IsNotNull(result.Value.RefreshToken);

        // Verify Old Token Revoked
        var oldToken = await _refreshTokenRepository.GetByIdAsync(refreshToken.Id, CancellationToken);
        Assert.IsNotNull(oldToken!.RevokedOn);
        Assert.AreEqual(expectedTime.UtcDateTime, oldToken.RevokedOn);
        Assert.IsNotNull(oldToken.ReplacedByTokenId);

        // Verify New Token Created
        var newToken = await _refreshTokenRepository.GetByTokenAsync(result.Value.RefreshToken, CancellationToken);
        Assert.IsNotNull(newToken);
        Assert.AreEqual(user.Id, newToken.UserId);
        Assert.AreEqual(newJti, newToken.JwtId);
        Assert.AreEqual(oldToken.ReplacedByTokenId, newToken.Id);
        Assert.AreEqual("127.0.0.1", newToken.IpAddress);
        Assert.AreEqual("test-agent", newToken.UserAgent);
    }

    private string GenerateJwtToken(string jti)
    {
        var token = new JwtSecurityToken(claims: new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, jti)
        });
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

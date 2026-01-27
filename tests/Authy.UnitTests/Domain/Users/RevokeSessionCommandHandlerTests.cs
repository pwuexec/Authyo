using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Users;
using Authy.Presentation.Entitites;
using Authy.Presentation.Persistence.Repositories;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;
using NSubstitute;

namespace Authy.UnitTests.Domain.Users;

[TestClass]
public class RevokeSessionCommandHandlerTests : TestBase
{
    private readonly IAuthorizationService _authorizationService = Substitute.For<IAuthorizationService>();
    private RefreshTokenRepository _refreshTokenRepository = null!;
    private RevokeSessionCommandHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _refreshTokenRepository = new RefreshTokenRepository(DbContext);
        _handler = new RevokeSessionCommandHandler(
            _authorizationService,
            _refreshTokenRepository,
            TimeProvider);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_SessionNotFound()
    {
        // Arrange
        var command = new RevokeSessionCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.RefreshToken.NotFound, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_UserNotAuthorized()
    {
        // Arrange
        const string tokenValue = "token";
        var refreshToken = new RefreshToken 
        { 
            UserId = Guid.NewGuid(), 
            Token = tokenValue 
        };
        await DbContext.RefreshTokens.AddAsync(refreshToken, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var requestingUserId = Guid.NewGuid();
        _authorizationService.EnsureCanManageUserAsync(refreshToken.UserId, requestingUserId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(DomainErrors.User.Unauthorized));

        var command = new RevokeSessionCommand(refreshToken.Id, requestingUserId);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.User.Unauthorized, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_RevokeSession_When_Authorized()
    {
        // Arrange
        const string tokenValue = "token";
        var refreshToken = new RefreshToken 
        { 
            UserId = Guid.NewGuid(), 
            Token = tokenValue 
        };
        await DbContext.RefreshTokens.AddAsync(refreshToken, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var requestingUserId = refreshToken.UserId; // Same user
        _authorizationService.EnsureCanManageUserAsync(refreshToken.UserId, requestingUserId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new RevokeSessionCommand(refreshToken.Id, requestingUserId);
        var expectedTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        TimeProvider.SetUtcNow(expectedTime);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        
        var updatedToken = await _refreshTokenRepository.GetByIdAsync(refreshToken.Id, CancellationToken);
        Assert.IsNotNull(updatedToken!.RevokedOn);
        Assert.AreEqual(expectedTime.UtcDateTime, updatedToken.RevokedOn);
    }
}

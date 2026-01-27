using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Users;
using Authy.Presentation.Entitites;
using Authy.Presentation.Persistence.Repositories;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;
using NSubstitute;

namespace Authy.UnitTests.Domain.Users;

[TestClass]
public class GetSessionsQueryHandlerTests : TestBase
{
    private readonly IAuthorizationService _authorizationService = Substitute.For<IAuthorizationService>();
    private RefreshTokenRepository _refreshTokenRepository = null!;
    private GetSessionsQueryHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _refreshTokenRepository = new RefreshTokenRepository(DbContext);
        _handler = new GetSessionsQueryHandler(
            _authorizationService,
            _refreshTokenRepository);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_NotAuthorized()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        
        _authorizationService.EnsureCanManageUserAsync(targetUserId, requestingUserId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(DomainErrors.User.Unauthorized));

        var query = new GetSessionsQuery(targetUserId, requestingUserId);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.User.Unauthorized, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnSessions_When_Authorized()
    {
        // Arrange
        const string tokenValue = "token-1";
        var targetUserId = Guid.NewGuid();
        var refreshToken = new RefreshToken 
        { 
            UserId = targetUserId, 
            Token = tokenValue 
        };
        await DbContext.RefreshTokens.AddAsync(refreshToken, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        _authorizationService.EnsureCanManageUserAsync(targetUserId, targetUserId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var query = new GetSessionsQuery(targetUserId, targetUserId);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.Value);
        Assert.AreEqual(tokenValue, result.Value[0].Token);
    }
}

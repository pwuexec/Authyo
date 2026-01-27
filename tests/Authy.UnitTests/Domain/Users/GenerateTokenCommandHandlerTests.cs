using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Users;
using Authy.Presentation.Entitites;
using Authy.Presentation.Persistence.Repositories;
using Authy.Presentation.Shared.Abstractions;
using NSubstitute;

namespace Authy.UnitTests.Domain.Users;

[TestClass]
public class GenerateTokenCommandHandlerTests : TestBase
{
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private UserRepository _userRepository = null!;
    private RefreshTokenRepository _refreshTokenRepository = null!;
    private GenerateTokenCommandHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _userRepository = new UserRepository(DbContext);
        _refreshTokenRepository = new RefreshTokenRepository(DbContext);
        _handler = new GenerateTokenCommandHandler(
            _userRepository, 
            _jwtService, 
            _refreshTokenRepository,
            TimeProvider);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_UserNotFound()
    {
        // Arrange
        var command = new GenerateTokenCommand(Guid.NewGuid());

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.User.NotFound, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnTokens_When_UserExists()
    {
        // Arrange
        const string userName = "Test User";
        const string organizationName = "Test Org";
        const string accessTokenPrefix = "access-token";
        const string jtiValue = "jti-123";
        const string ipAddr = "127.0.0.1";
        const string agent = "test-agent";

        var org = new Organization { Name = organizationName };
        await DbContext.Organizations.AddAsync(org, TestContext.CancellationToken);

        var user = new User 
        { 
            Name = userName, 
            Organization = org 
        };
        await DbContext.Users.AddAsync(user, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        _jwtService.GenerateToken(user.Id, user.Name, Arg.Any<List<string>>())
            .Returns((accessTokenPrefix, jtiValue));

        var command = new GenerateTokenCommand(user.Id, ipAddr, agent);
        var expectedTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        TimeProvider.SetUtcNow(expectedTime);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(accessTokenPrefix, result.Value.AccessToken);
        Assert.IsNotNull(result.Value.RefreshToken);

        var storedToken = await _refreshTokenRepository.GetByTokenAsync(result.Value.RefreshToken, CancellationToken);
        Assert.IsNotNull(storedToken);
        Assert.AreEqual(user.Id, storedToken.UserId);
        Assert.AreEqual(jtiValue, storedToken.JwtId);
        Assert.AreEqual(ipAddr, storedToken.IpAddress);
        Assert.AreEqual(agent, storedToken.UserAgent);
        Assert.AreEqual(expectedTime.UtcDateTime, storedToken.CreatedOn);
        Assert.AreEqual(expectedTime.UtcDateTime.AddDays(7), storedToken.ExpiresOn);
    }
}

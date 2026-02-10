using Authy.Application.Data.Repositories;
using Authy.Application.Domain;
using Authy.Application.Domain.Organizations.Data;
using Authy.Application.Domain.Users.Data;
using Authy.Application.Entitites;
using Authy.Application.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Authy.UnitTests.Shared;

[TestClass]
public class AuthorizationServiceTests : TestBase
{
    private AuthorizationService _sut = null!;
    private IUserRepository _userRepository = null!;
    private IOrganizationRepository _organizationRepository = null!;
    private IHttpContextAccessor _httpContextAccessor = null!;
    private IOptions<RootIpOptions> _options = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();

        _userRepository = new UserRepository(DbContext);
        _organizationRepository = new OrganizationRepository(DbContext);
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _options = Substitute.For<IOptions<RootIpOptions>>();
        _options.Value.Returns(new RootIpOptions());

        var context = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(context);

        _sut = new AuthorizationService(
            _httpContextAccessor,
            _options,
            _organizationRepository,
            _userRepository);
    }

    [TestMethod]
    public async Task EnsureCanManageUserAsync_TargetIsSelf_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _sut.EnsureCanManageUserAsync(userId, userId, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task EnsureCanManageUserAsync_TargetUserNotFound_ReturnsFailure()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();

        // Act
        var result = await _sut.EnsureCanManageUserAsync(targetUserId, requestingUserId, CancellationToken);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(DomainErrors.User.NotFound, result.Error);
    }

    [TestMethod]
    public async Task EnsureCanManageUserAsync_RequestingUserIsOwner_ReturnsSuccess()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var org = new Organization
        {
            Id = orgId,
            Name = "Test Org",
            Owners = new List<User>()
        };

        var owner = new User
        {
            Id = ownerId,
            Name = "Owner",
            OrganizationId = orgId
        };
        org.Owners.Add(owner);

        var targetUser = new User
        {
            Id = targetUserId,
            Name = "Target",
            OrganizationId = orgId
        };

        DbContext.Organizations.Add(org);
        DbContext.Users.Add(targetUser);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _sut.EnsureCanManageUserAsync(targetUserId, ownerId, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task EnsureCanManageUserAsync_RequestingUserIsNotOwner_ReturnsFailure()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var nonOwnerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var org = new Organization
        {
            Id = orgId,
            Name = "Test Org",
            Owners = new List<User>()
        };

        var targetUser = new User
        {
            Id = targetUserId,
            Name = "Target",
            OrganizationId = orgId
        };

        DbContext.Organizations.Add(org);
        DbContext.Users.Add(targetUser);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _sut.EnsureCanManageUserAsync(targetUserId, nonOwnerId, CancellationToken);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(DomainErrors.User.UnauthorizedOwner, result.Error);
    }
}

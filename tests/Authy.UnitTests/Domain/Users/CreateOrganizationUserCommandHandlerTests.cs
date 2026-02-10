using Authy.Application.Data.Repositories;
using Authy.Application.Domain;
using Authy.Application.Domain.Organizations.Data;
using Authy.Application.Domain.Users;
using Authy.Application.Domain.Users.Data;
using Authy.Application.Entitites;
using Authy.Application.Shared;
using Authy.Application.Shared.Abstractions;
using NSubstitute;

namespace Authy.UnitTests.Domain.Users;

[TestClass]
public class CreateOrganizationUserCommandHandlerTests : TestBase
{
    private IAuthorizationService _authorizationService = null!;
    private IOrganizationRepository _organizationRepository = null!;
    private IUserRepository _userRepository = null!;
    private CreateOrganizationUserCommandHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();

        _authorizationService = Substitute.For<IAuthorizationService>();
        _organizationRepository = new OrganizationRepository(DbContext);
        _userRepository = new UserRepository(DbContext);
        _handler = new CreateOrganizationUserCommandHandler(
            _authorizationService,
            _organizationRepository,
            _userRepository);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_OrganizationMissing()
    {
        // Arrange
        const string userName = "New User";
        var orgId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        _authorizationService.EnsureRootIpOrOwnerAsync(orgId, requestingUserId, CancellationToken)
            .Returns(Result.Success());

        var command = new CreateOrganizationUserCommand(orgId, userName, requestingUserId);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.Organization.NotFound, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_CreateUser_When_Valid()
    {
        // Arrange
        const string userName = "New User";
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        DbContext.Organizations.Add(org);
        await DbContext.SaveChangesAsync(CancellationToken);

        var requestingUserId = Guid.NewGuid();
        _authorizationService.EnsureRootIpOrOwnerAsync(org.Id, requestingUserId, CancellationToken)
            .Returns(Result.Success());

        var command = new CreateOrganizationUserCommand(org.Id, userName, requestingUserId);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);
        await DbContext.SaveChangesAsync(CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(userName, result.Value.Name);
        Assert.AreEqual(org.Id, result.Value.OrganizationId);

        var storedUser = await _userRepository.GetByIdAsync(result.Value.Id, CancellationToken);
        Assert.IsNotNull(storedUser);
        Assert.AreEqual(userName, storedUser.Name);
    }
}

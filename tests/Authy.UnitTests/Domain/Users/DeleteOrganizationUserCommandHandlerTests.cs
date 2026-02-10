using Authy.Application.Data.Repositories;
using Authy.Application.Domain;
using Authy.Application.Domain.Users;
using Authy.Application.Domain.Users.Data;
using Authy.Application.Entitites;
using Authy.Application.Shared;
using Authy.Application.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Authy.UnitTests.Domain.Users;

[TestClass]
public class DeleteOrganizationUserCommandHandlerTests : TestBase
{
    private IAuthorizationService _authorizationService = null!;
    private IUserRepository _userRepository = null!;
    private DeleteOrganizationUserCommandHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();

        _authorizationService = Substitute.For<IAuthorizationService>();
        _userRepository = new UserRepository(DbContext);
        _handler = new DeleteOrganizationUserCommandHandler(
            _authorizationService,
            _userRepository);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_UserNotFound()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var missingUserId = Guid.NewGuid();

        _authorizationService.EnsureRootIpOrOwnerAsync(orgId, requestingUserId, CancellationToken)
            .Returns(Result.Success());

        var command = new DeleteOrganizationUserCommand(orgId, missingUserId, requestingUserId);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.User.NotFound, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_DeleteUser_When_Valid()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var user = new User { Id = Guid.NewGuid(), Name = "Owner", OrganizationId = orgId };
        var org = new Organization { Id = orgId, Name = "Org", Owners = new List<User> { user } };

        DbContext.Organizations.Add(org);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync(CancellationToken);

        var requestingUserId = Guid.NewGuid();
        _authorizationService.EnsureRootIpOrOwnerAsync(orgId, requestingUserId, CancellationToken)
            .Returns(Result.Success());

        var command = new DeleteOrganizationUserCommand(orgId, user.Id, requestingUserId);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);
        await DbContext.SaveChangesAsync(CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        var storedUser = await _userRepository.GetByIdAsync(user.Id, CancellationToken);
        Assert.IsNull(storedUser);

        var storedOrg = await DbContext.Organizations
            .Include(o => o.Owners)
            .SingleAsync(o => o.Id == orgId, CancellationToken);
        Assert.IsTrue(storedOrg.Owners.All(o => o.Id != user.Id));
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_RequestingUserIsTargetUser()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var user = new User { Id = Guid.NewGuid(), Name = "Self", OrganizationId = orgId };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync(CancellationToken);

        _authorizationService.EnsureRootIpOrOwnerAsync(orgId, user.Id, CancellationToken)
            .Returns(Result.Success());

        var command = new DeleteOrganizationUserCommand(orgId, user.Id, user.Id);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.User.CannotRemoveSelf, result.Error);
    }
}

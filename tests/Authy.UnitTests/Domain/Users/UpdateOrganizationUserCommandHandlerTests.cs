using Authy.Application.Data.Repositories;
using Authy.Application.Domain;
using Authy.Application.Domain.Users;
using Authy.Application.Domain.Users.Data;
using Authy.Application.Entitites;
using Authy.Application.Shared;
using Authy.Application.Shared.Abstractions;
using NSubstitute;

namespace Authy.UnitTests.Domain.Users;

[TestClass]
public class UpdateOrganizationUserCommandHandlerTests : TestBase
{
    private IAuthorizationService _authorizationService = null!;
    private IUserRepository _userRepository = null!;
    private UpdateOrganizationUserCommandHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();

        _authorizationService = Substitute.For<IAuthorizationService>();
        _userRepository = new UserRepository(DbContext);
        _handler = new UpdateOrganizationUserCommandHandler(_authorizationService, _userRepository);
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

        var command = new UpdateOrganizationUserCommand(orgId, missingUserId, "New Name", requestingUserId);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.User.NotFound, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_UpdateUser_When_Valid()
    {
        // Arrange
        const string updatedName = "Updated User";
        var orgId = Guid.NewGuid();
        var user = new User { Id = Guid.NewGuid(), Name = "Original", OrganizationId = orgId };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync(CancellationToken);

        var requestingUserId = Guid.NewGuid();
        _authorizationService.EnsureRootIpOrOwnerAsync(orgId, requestingUserId, CancellationToken)
            .Returns(Result.Success());

        var command = new UpdateOrganizationUserCommand(orgId, user.Id, updatedName, requestingUserId);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);
        await DbContext.SaveChangesAsync(CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(updatedName, result.Value.Name);

        var storedUser = await _userRepository.GetByIdAsync(user.Id, CancellationToken);
        Assert.IsNotNull(storedUser);
        Assert.AreEqual(updatedName, storedUser.Name);
    }
}

using Authy.Application.Data.Repositories;
using Authy.Application.Domain.Users;
using Authy.Application.Domain.Users.Data;
using Authy.Application.Entitites;
using Authy.Application.Shared;
using Authy.Application.Shared.Abstractions;
using NSubstitute;

namespace Authy.UnitTests.Domain.Users;

[TestClass]
public class GetOrganizationUsersQueryHandlerTests : TestBase
{
    private IAuthorizationService _authorizationService = null!;
    private IUserRepository _userRepository = null!;
    private GetOrganizationUsersQueryHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();

        _authorizationService = Substitute.For<IAuthorizationService>();
        _userRepository = new UserRepository(DbContext);
        _handler = new GetOrganizationUsersQueryHandler(_authorizationService, _userRepository);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnUsers_When_Authorized()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();

        DbContext.Users.AddRange(
            new User { Id = Guid.NewGuid(), Name = "User A", OrganizationId = orgId },
            new User { Id = Guid.NewGuid(), Name = "User B", OrganizationId = orgId },
            new User { Id = Guid.NewGuid(), Name = "Other User", OrganizationId = otherOrgId });
        await DbContext.SaveChangesAsync(CancellationToken);

        _authorizationService.EnsureRootIpOrOwnerAsync(orgId, requestingUserId, CancellationToken)
            .Returns(Result.Success());

        var query = new GetOrganizationUsersQuery(orgId, requestingUserId);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(2, result.Value);
        Assert.IsTrue(result.Value.All(u => u.OrganizationId == orgId));
    }
}

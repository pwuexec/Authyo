using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Roles;
using Authy.Presentation.Domain.Scopes;
using Authy.Presentation.Persistence.Repositories;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;
using NSubstitute;

namespace Authy.UnitTests.Domain.Roles;

[TestClass]
public class GetRolesQueryHandlerTests : TestBase
{
    private readonly IAuthorizationService _authorizationService = Substitute.For<IAuthorizationService>();
    private RoleRepository _roleRepository = null!;
    private GetRolesQueryHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _roleRepository = new RoleRepository(DbContext);
        _handler = new GetRolesQueryHandler(_authorizationService, _roleRepository);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_UserIsNotAuthorized()
    {
        // Arrange
        var query = new GetRolesQuery(Guid.NewGuid(), Guid.NewGuid());
        
        _authorizationService.EnsureRootIpOrOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(DomainErrors.User.UnauthorizedOwner));

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.User.UnauthorizedOwner, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnEmptyList_When_NoRolesExist()
    {
        // Arrange
        var query = new GetRolesQuery(Guid.NewGuid(), Guid.NewGuid());
        
        _authorizationService.EnsureRootIpOrOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Value);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnRolesWithScopes_When_RolesExist()
    {
        // Arrange
        const string roleName = "test-role";
        const string scopeName = "test-scope";
        
        var orgId = Guid.NewGuid();
        var scope = new Scope { Id = Guid.NewGuid(), Name = scopeName, OrganizationId = orgId };
        var role = new Role 
        { 
            Id = Guid.NewGuid(), 
            Name = roleName, 
            OrganizationId = orgId,
            Scopes = new List<Scope> { scope }
        };

        await DbContext.Scopes.AddAsync(scope);
        await DbContext.Roles.AddAsync(role);
        await DbContext.SaveChangesAsync();

        var query = new GetRolesQuery(orgId, Guid.NewGuid());
        
        _authorizationService.EnsureRootIpOrOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.Value);
        
        var returnedRole = result.Value[0];
        Assert.AreEqual(roleName, returnedRole.Name);
        Assert.HasCount(1, returnedRole.Scopes);
        Assert.AreEqual(scopeName, returnedRole.Scopes[0].Name);
    }
}

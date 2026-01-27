using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Scopes;
using Authy.Presentation.Entitites;
using Authy.Presentation.Persistence.Repositories;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;
using NSubstitute;

namespace Authy.UnitTests.Domain.Scopes;

[TestClass]
public class GetScopesQueryHandlerTests : TestBase
{
    private readonly IAuthorizationService _authorizationService = Substitute.For<IAuthorizationService>();
    private ScopeRepository _scopeRepository = null!;
    private GetScopesQueryHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _scopeRepository = new ScopeRepository(DbContext);
        _handler = new GetScopesQueryHandler(_authorizationService, _scopeRepository);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_UserIsNotAuthorized()
    {
        // Arrange
        var query = new GetScopesQuery(Guid.NewGuid(), Guid.NewGuid());
        
        _authorizationService.EnsureRootIpOrOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(DomainErrors.User.UnauthorizedOwner));

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.User.UnauthorizedOwner, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnEmptyList_When_NoScopesExist()
    {
        // Arrange
        var query = new GetScopesQuery(Guid.NewGuid(), Guid.NewGuid());
        
        _authorizationService.EnsureRootIpOrOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Value);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnScopesWithRoles_When_ScopesExist()
    {
        // Arrange
        const string scopeName = "test-scope";
        const string roleName = "test-role";
        
        var orgId = Guid.NewGuid();
        var scope = new Scope 
        { 
            Name = scopeName, 
            OrganizationId = orgId 
        };
        var role = new Role 
        { 
            Name = roleName, 
            OrganizationId = orgId,
            Scopes = new List<Scope> { scope }
        };

        // Note: EF Core handles many-to-many. 
        // Adding role with scopes should persist relationship.
        await DbContext.Roles.AddAsync(role, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var query = new GetScopesQuery(orgId, Guid.NewGuid());
        
        _authorizationService.EnsureRootIpOrOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.Value);
        
        var returnedScope = result.Value[0];
        Assert.AreEqual(scopeName, returnedScope.Name);
        Assert.HasCount(1, returnedScope.Roles);
        Assert.AreEqual(roleName, returnedScope.Roles[0].Name);
    }
}

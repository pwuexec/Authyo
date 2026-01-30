using Authy.Application.Domain;
using Authy.Application.Domain.Roles;
using Authy.Application.Entitites;
using Authy.Application.Data.Repositories;
using Authy.Application.Shared;
using Authy.Application.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Authy.UnitTests.Domain.Roles;

[TestClass]
public class UpsertRoleCommandHandlerTests : TestBase
{
    private readonly IAuthorizationService _authorizationService = Substitute.For<IAuthorizationService>();
    private RoleRepository _roleRepository = null!;
    private ScopeRepository _scopeRepository = null!;
    private UpsertRoleCommandHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _roleRepository = new RoleRepository(DbContext);
        _scopeRepository = new ScopeRepository(DbContext);
        _handler = new UpsertRoleCommandHandler(_authorizationService, _roleRepository, _scopeRepository);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_ScopesListIsEmpty()
    {
        // Arrange
        const string roleName = "role";
        var command = new UpsertRoleCommand(Guid.NewGuid(), roleName, new List<string>(), Guid.NewGuid());

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.Role.ScopesRequired, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_ScopeNotFound()
    {
        // Arrange
        const string roleName = "role";
        const string missingScopeName = "missing-scope";
        
        var command = new UpsertRoleCommand(Guid.NewGuid(), roleName, new List<string> { missingScopeName }, Guid.NewGuid());
        
        _authorizationService.EnsureRootIpOrOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // No scopes in DB, so "missing-scope" is definitely missing.

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.IsTrue(result.Errors.Any(e => e.Code == DomainErrors.Role.ScopeNotFound(missingScopeName).Code));
    }

    [TestMethod]
    public async Task HandleAsync_Should_CreateRole_When_Valid()
    {
        // Arrange
        const string newRoleName = "new-role";
        const string existingScopeName = "existing-scope";
        
        var orgId = Guid.NewGuid();
        var scope = new Scope { Id = Guid.NewGuid(), Name = existingScopeName, OrganizationId = orgId };
        
        // Seed Scope
        await DbContext.Scopes.AddAsync(scope, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var command = new UpsertRoleCommand(orgId, newRoleName, new List<string> { existingScopeName }, Guid.NewGuid());
        
        _authorizationService.EnsureRootIpOrOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        
        // Verify DB State
        var roleInDb = await DbContext.Roles.Include(r => r.Scopes).FirstOrDefaultAsync(r => r.Name == newRoleName && r.OrganizationId == orgId, TestContext.CancellationToken);
        Assert.IsNotNull(roleInDb);
        Assert.HasCount(1, roleInDb.Scopes);
        Assert.AreEqual(existingScopeName, roleInDb.Scopes.First().Name);
    }
}



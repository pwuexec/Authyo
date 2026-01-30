using Authy.Application.Domain;
using Authy.Application.Domain.Scopes;
using Authy.Application.Entitites;
using Authy.Application.Data.Repositories;
using Authy.Application.Shared;
using Authy.Application.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Authy.UnitTests.Domain.Scopes;

[TestClass]
public class UpsertScopeCommandHandlerTests : TestBase
{
    private readonly IAuthorizationService _authorizationService = Substitute.For<IAuthorizationService>();
    private ScopeRepository _scopeRepository = null!;
    private UpsertScopeCommandHandler _handler = null!;

    [TestInitialize]
    public override void Setup()
    {
        base.Setup();
        _scopeRepository = new ScopeRepository(DbContext);
        _handler = new UpsertScopeCommandHandler(_authorizationService, _scopeRepository);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_NameIsEmpty()
    {
        // Arrange
        var command = new UpsertScopeCommand(Guid.NewGuid(), "", Guid.NewGuid(), new UpsertScopeFields(""));

        // Mock auth success to ensure we reach validation (if applicable)
        _authorizationService.EnsureRootIpOrOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.Scope.NameEmpty, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_ReturnFailure_When_UserIsNotAuthorized()
    {
        // Arrange
        const string scopeArg = "scope";
        var command = new UpsertScopeCommand(Guid.NewGuid(), scopeArg, Guid.NewGuid(), new UpsertScopeFields(scopeArg));

        _authorizationService.EnsureRootIpOrOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(DomainErrors.User.UnauthorizedOwner));

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(DomainErrors.User.UnauthorizedOwner, result.Error);
    }

    [TestMethod]
    public async Task HandleAsync_Should_CreateScope_When_ScopeDoesNotExist()
    {
        // Arrange
        const string newScopeName = "new-scope";
        var command = new UpsertScopeCommand(Guid.NewGuid(), newScopeName, Guid.NewGuid(),
            new UpsertScopeFields(newScopeName));

        _authorizationService.EnsureRootIpOrOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(newScopeName, result.Value.Name);

        // Verify DB State
        var scopeInDb = await DbContext.Scopes.FirstOrDefaultAsync(
            s => s.Name == newScopeName && s.OrganizationId == command.OrganizationId, TestContext.CancellationToken);
        Assert.IsNotNull(scopeInDb);
    }

    [TestMethod]
    public async Task HandleAsync_Should_UpdateScope_When_ScopeExists()
    {
        // Arrange
        const string oldScopeName = "old-name";
        const string newNameForUpdate = "new-name";

        var orgId = Guid.NewGuid();
        var existingScope = new Scope { Name = oldScopeName, OrganizationId = orgId };

        // Seed DB
        await DbContext.Scopes.AddAsync(existingScope, TestContext.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var command =
            new UpsertScopeCommand(orgId, oldScopeName, Guid.NewGuid(), new UpsertScopeFields(newNameForUpdate));

        _authorizationService.EnsureRootIpOrOwnerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(newNameForUpdate, result.Value.Name);

        // Verify DB State
        var scopeInDb =
            await DbContext.Scopes.FirstOrDefaultAsync(s => s.Id == existingScope.Id, TestContext.CancellationToken);
        Assert.IsNotNull(scopeInDb);
        Assert.AreEqual(newNameForUpdate, scopeInDb.Name);
    }
}


using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Organizations;
using Authy.Presentation.Domain.Roles;
using Authy.Presentation.Domain.Scopes;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Shared;

public class MockRepository : IOrganizationRepository, IRoleRepository, IScopeRepository, IUnitOfWork
{
    private static readonly List<Organization> Organizations = new()
    {
        new Organization 
        { 
            Id = Guid.Parse("31052a63-0ade-4b14-bada-1ea2fc1ae40a"), 
            Name = "Test Org",
            Owners = new List<User> { new() { Id = Guid.Parse("f7fa1c38-9736-41b6-91b8-0745d0cec70e") } }
        }
    };

    private static readonly List<Role> Roles = new();
    private static readonly List<Scope> Scopes = new();

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Organizations.FirstOrDefault(o => o.Id == id));
    }

    public Task AddAsync(Organization organization, CancellationToken cancellationToken)
    {
        Organizations.Add(organization);
        return Task.CompletedTask;
    }

    public Task AddAsync(Role role, CancellationToken cancellationToken)
    {
        Roles.Add(role);
        return Task.CompletedTask;
    }

    public Task AddAsync(Scope scope, CancellationToken cancellationToken)
    {
        Scopes.Add(scope);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

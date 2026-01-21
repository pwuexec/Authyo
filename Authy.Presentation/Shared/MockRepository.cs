using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Shared;

public class MockRepository : IOrganizationRepository, IRoleRepository, IScopeRepository, IUnitOfWork
{
    private static readonly List<Organization> Organizations = new()
    {
        new Organization 
        { 
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), 
            Name = "Test Org",
            Owners = new List<User> { new User { Id = Guid.Parse("00000000-0000-0000-0000-000000000002") } }
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
        // For mock, we'll just log or assume it's "saved"
        Console.WriteLine("Changes saved to database.");
        return Task.CompletedTask;
    }
}

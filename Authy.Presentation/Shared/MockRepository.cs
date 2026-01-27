using Authy.Presentation.Domain.Organizations.Data;
using Authy.Presentation.Domain.Roles.Data;
using Authy.Presentation.Domain.Scopes.Data;
using Authy.Presentation.Shared.Abstractions;
using Authy.Presentation.Domain.Users.Data;
using Authy.Presentation.Entitites;

namespace Authy.Presentation.Shared;

public class MockRepository : IOrganizationRepository, IRoleRepository, IScopeRepository, IUserRepository, IUnitOfWork, IRefreshTokenRepository
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

    private static readonly List<User> Users = new()
    {
        new User 
        { 
            Id = Guid.Parse("f7fa1c38-9736-41b6-91b8-0745d0cec70e"), 
            Name = "Test User", 
            OrganizationId = Guid.Parse("31052a63-0ade-4b14-bada-1ea2fc1ae40a") 
        }
    };

    private static readonly List<Role> Roles = new();
    private static readonly List<Scope> Scopes = new();

    Task<User?> IUserRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
    }

    Task<Organization?> IOrganizationRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Organizations.FirstOrDefault(o => o.Id == id));
    }

    Task<List<Organization>> IOrganizationRepository.GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Organizations.ToList());
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

    public Task UpdateAsync(Role role, CancellationToken cancellationToken)
    {
        var existing = Roles.FirstOrDefault(r => r.Id == role.Id);
        if (existing != null)
        {
            Roles.Remove(existing);
            Roles.Add(role);
        }
        return Task.CompletedTask;
    }

    Task<List<Role>> IRoleRepository.GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Roles.Where(r => r.OrganizationId == organizationId).ToList());
    }

    Task<Role?> IRoleRepository.GetByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken)
    {
        return Task.FromResult(Roles.FirstOrDefault(r => r.OrganizationId == organizationId && r.Name == name));
    }

    public Task AddAsync(Scope scope, CancellationToken cancellationToken)
    {
        Scopes.Add(scope);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Scope scope, CancellationToken cancellationToken)
    {
        var existing = Scopes.FirstOrDefault(s => s.Id == scope.Id);
        if (existing != null)
        {
            Scopes.Remove(existing);
            Scopes.Add(scope);
        }
        return Task.CompletedTask;
    }

    Task<List<Scope>> IScopeRepository.GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Scopes.Where(s => s.OrganizationId == organizationId).ToList());
    }

    Task<List<Scope>> IScopeRepository.GetByNamesAsync(Guid organizationId, List<string> names, CancellationToken cancellationToken)
    {
        return Task.FromResult(Scopes.Where(s => s.OrganizationId == organizationId && names.Contains(s.Name)).ToList());
    }

    Task<Scope?> IScopeRepository.GetByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken)
    {
        return Task.FromResult(Scopes.FirstOrDefault(s => s.OrganizationId == organizationId && s.Name == name));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private static readonly List<RefreshToken> RefreshTokens = new();

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        RefreshTokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(RefreshTokens.FirstOrDefault(rt => rt.Id == id));
    }

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)
    {
        return Task.FromResult(RefreshTokens.FirstOrDefault(rt => rt.Token == token));
    }

    public Task<List<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(RefreshTokens.Where(rt => rt.UserId == userId).ToList());
    }

    public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        var existing = RefreshTokens.FirstOrDefault(rt => rt.Id == refreshToken.Id);
        if (existing != null)
        {
            RefreshTokens.Remove(existing);
            RefreshTokens.Add(refreshToken);
        }
        return Task.CompletedTask;
    }
}

using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Organizations;
using Authy.Presentation.Domain.Roles;
using Authy.Presentation.Domain.Scopes;
using Microsoft.EntityFrameworkCore;

namespace Authy.Presentation.Persistence.Repositories;

public class ScopeRepository(AuthyDbContext dbContext) : IScopeRepository
{
    public async Task AddAsync(Scope scope, CancellationToken cancellationToken)
    {
        await dbContext.Scopes.AddAsync(scope, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Scope scope, CancellationToken cancellationToken)
    {
        dbContext.Scopes.Update(scope);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<Scope>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return dbContext.Scopes
            .Include(s => s.Roles)
            .Where(s => s.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Scope>> GetByNamesAsync(Guid organizationId, List<string> names, CancellationToken cancellationToken)
    {
        return dbContext.Scopes
            .Where(s => s.OrganizationId == organizationId && names.Contains(s.Name))
            .ToListAsync(cancellationToken);
    }

    public Task<Scope?> GetByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken)
    {
        return dbContext.Scopes
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.Name == name, cancellationToken);
    }
}

public class RoleRepository(AuthyDbContext dbContext) : IRoleRepository
{
    public async Task AddAsync(Role role, CancellationToken cancellationToken)
    {
        await dbContext.Roles.AddAsync(role, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken)
    {
        dbContext.Roles.Update(role);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<Role>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .Include(r => r.Scopes)
            .Where(r => r.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }

    public Task<Role?> GetByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .Include(r => r.Scopes)
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Name == name, cancellationToken);
    }
}

public class OrganizationRepository(AuthyDbContext dbContext) : IOrganizationRepository
{
    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Organizations
            .Include(o => o.Owners)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public Task<List<Organization>> GetAllAsync(CancellationToken cancellationToken)
    {
        return dbContext.Organizations.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken)
    {
        await dbContext.Organizations.AddAsync(organization, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

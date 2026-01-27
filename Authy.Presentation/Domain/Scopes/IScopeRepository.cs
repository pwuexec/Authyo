namespace Authy.Presentation.Domain.Scopes;

public interface IScopeRepository
{
    Task AddAsync(Scope scope, CancellationToken cancellationToken);
    Task UpdateAsync(Scope scope, CancellationToken cancellationToken);
    Task<List<Scope>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<List<Scope>> GetByNamesAsync(Guid organizationId, List<string> names, CancellationToken cancellationToken);
    Task<Scope?> GetByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken);
}

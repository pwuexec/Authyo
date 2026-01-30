namespace Authy.Application.Domain.Organizations.Data;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Organization organization, CancellationToken cancellationToken);
    Task<List<Organization>> GetAllAsync(CancellationToken cancellationToken);
}


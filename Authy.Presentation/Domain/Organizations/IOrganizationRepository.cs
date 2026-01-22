namespace Authy.Presentation.Domain.Organizations;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Organization organization, CancellationToken cancellationToken);
}

using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Organizations;
using Authy.Presentation.Extensions;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Domain.Organizations;

public record CreateOrganizationCommand(string Name) : ICommand<Result<Organization>>;

public class CreateOrganizationCommandHandler : ICommandHandler<CreateOrganizationCommand, Result<Organization>>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IOrganizationRepository _organizationRepository;

    public CreateOrganizationCommandHandler(
        IHttpContextAccessor httpContextAccessor, 
        IConfiguration configuration,
        IOrganizationRepository organizationRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _organizationRepository = organizationRepository;
    }

    public async Task<Result<Organization>> HandleAsync(CreateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var authResult = _httpContextAccessor.HttpContext.EnsureRootIp(_configuration);
        if (authResult.IsFailure)
        {
            return Result.Failure<Organization>(authResult.Error);
        }

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = command.Name
        };

        await _organizationRepository.AddAsync(org, cancellationToken);

        return Result.Success(org);
    }
}

using Authy.Presentation.Extensions;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Domain.Organizations;

public record CreateOrganizationCommand(string Name) : ICommand<Result<Organization>>;

public class CreateOrganizationCommandHandler(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    IOrganizationRepository organizationRepository)
    : ICommandHandler<CreateOrganizationCommand, Result<Organization>>
{
    public async Task<Result<Organization>> HandleAsync(CreateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var authResult = httpContextAccessor.HttpContext.EnsureRootIp(configuration);
        if (authResult.IsFailure)
        {
            return Result.Failure<Organization>(authResult.Error);
        }

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = command.Name
        };

        await organizationRepository.AddAsync(org, cancellationToken);

        return Result.Success(org);
    }
}

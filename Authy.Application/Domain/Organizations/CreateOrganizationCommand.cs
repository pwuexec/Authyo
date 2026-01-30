using Authy.Application.Domain.Organizations.Data;
using Authy.Application.Extensions;
using Authy.Application.Shared.Abstractions;

namespace Authy.Application.Domain.Organizations;

public record CreateOrganizationCommand(string Name) : ICommand<Result<Organization>>;

public class CreateOrganizationCommandHandler(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    IOrganizationRepository organizationRepository)
    : ICommandHandler<CreateOrganizationCommand, Result<Organization>>
{
    public async Task<Result<Organization>> HandleAsync(CreateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var validationFailure = Validate(command).FailureOrNull<Organization>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

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

    private static List<Error> Validate(CreateOrganizationCommand command)
    {
        var errors = new List<Error>();

        errors.NotEmptyIfNotNull(command.Name, DomainErrors.Organization.NameEmpty);

        return errors;
    }
}


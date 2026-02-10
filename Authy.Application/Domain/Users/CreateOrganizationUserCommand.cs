using Authy.Application.Domain.Organizations.Data;
using Authy.Application.Domain.Users.Data;
using Authy.Application.Shared.Abstractions;

namespace Authy.Application.Domain.Users;

public record CreateOrganizationUserCommand(Guid OrganizationId, string Name, Guid RequestingUserId)
    : ICommand<Result<OrganizationUserOutput>>;

public class CreateOrganizationUserCommandHandler(
    IAuthorizationService authorizationService,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository)
    : ICommandHandler<CreateOrganizationUserCommand, Result<OrganizationUserOutput>>
{
    public async Task<Result<OrganizationUserOutput>> HandleAsync(
        CreateOrganizationUserCommand command,
        CancellationToken cancellationToken)
    {
        var validationFailure = Validate(command).FailureOrNull<OrganizationUserOutput>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var authResult = await authorizationService.EnsureRootIpOrOwnerAsync(
            command.OrganizationId,
            command.RequestingUserId,
            cancellationToken);

        if (authResult.IsFailure)
        {
            return Result.Failure<OrganizationUserOutput>(authResult.Error);
        }

        var organization = await organizationRepository.GetByIdAsync(command.OrganizationId, cancellationToken);
        if (organization == null)
        {
            return Result.Failure<OrganizationUserOutput>(DomainErrors.Organization.NotFound);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            OrganizationId = organization.Id
        };

        await userRepository.AddAsync(user, cancellationToken);

        return Result.Success(new OrganizationUserOutput(user.Id, user.Name, user.OrganizationId));
    }

    private static List<Error> Validate(CreateOrganizationUserCommand command)
    {
        var errors = new List<Error>();

        errors.NotEmptyIfNotNull(command.Name, DomainErrors.User.NameEmpty);

        return errors;
    }
}

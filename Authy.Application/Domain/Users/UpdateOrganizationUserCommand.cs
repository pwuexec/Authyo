using Authy.Application.Domain.Users.Data;
using Authy.Application.Shared.Abstractions;

namespace Authy.Application.Domain.Users;

public record UpdateOrganizationUserCommand(Guid OrganizationId, Guid UserId, string Name, Guid RequestingUserId)
    : ICommand<Result<OrganizationUserOutput>>;

public class UpdateOrganizationUserCommandHandler(
    IAuthorizationService authorizationService,
    IUserRepository userRepository)
    : ICommandHandler<UpdateOrganizationUserCommand, Result<OrganizationUserOutput>>
{
    public async Task<Result<OrganizationUserOutput>> HandleAsync(
        UpdateOrganizationUserCommand command,
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

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null || user.OrganizationId != command.OrganizationId)
        {
            return Result.Failure<OrganizationUserOutput>(DomainErrors.User.NotFound);
        }

        user.Name = command.Name;

        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success(new OrganizationUserOutput(user.Id, user.Name, user.OrganizationId));
    }

    private static List<Error> Validate(UpdateOrganizationUserCommand command)
    {
        var errors = new List<Error>();

        errors.NotEmptyIfNotNull(command.Name, DomainErrors.User.NameEmpty);

        if (command.UserId == Guid.Empty)
        {
            errors.Add(DomainErrors.User.NotFound);
        }

        return errors;
    }
}

using Authy.Application.Domain.Users.Data;
using Authy.Application.Shared;
using Authy.Application.Shared.Abstractions;

namespace Authy.Application.Domain.Users;

public record DeleteOrganizationUserCommand(Guid OrganizationId, Guid UserId, Guid RequestingUserId)
    : ICommand<Result>;

public class DeleteOrganizationUserCommandHandler(
    IAuthorizationService authorizationService,
    IUserRepository userRepository)
    : ICommandHandler<DeleteOrganizationUserCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteOrganizationUserCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
        {
            return Result.Failure(DomainErrors.User.NotFound);
        }

        var authResult = await authorizationService.EnsureRootIpOrOwnerAsync(
            command.OrganizationId,
            command.RequestingUserId,
            cancellationToken);

        if (authResult.IsFailure)
        {
            return Result.Failure(authResult.Error);
        }

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null || user.OrganizationId != command.OrganizationId)
        {
            return Result.Failure(DomainErrors.User.NotFound);
        }

        if (command.UserId == command.RequestingUserId)
        {
            return Result.Failure(DomainErrors.User.CannotRemoveSelf);
        }

        await userRepository.DeleteAsync(user, cancellationToken);

        return Result.Success();
    }
}

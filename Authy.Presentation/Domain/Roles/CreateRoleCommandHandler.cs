using Authy.Presentation.Domain;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Domain.Roles;

public record CreateRoleCommand(Guid OrganizationId, string Name, Guid UserId) : ICommand<Result<Role>>;

public class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, Result<Role>>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IRoleRepository _roleRepository;

    public CreateRoleCommandHandler(
        IAuthorizationService authorizationService,
        IRoleRepository roleRepository)
    {
        _authorizationService = authorizationService;
        _roleRepository = roleRepository;
    }

    public async Task<Result<Role>> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var authResult = await _authorizationService.EnsureRootIpOrOwnerAsync(
            command.OrganizationId, 
            command.UserId, 
            cancellationToken);

        if (authResult.IsFailure)
        {
            return Result.Failure<Role>(authResult.Error);
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            OrganizationId = command.OrganizationId
        };

        await _roleRepository.AddAsync(role, cancellationToken);

        return Result.Success(role);
    }
}

using Authy.Presentation.Domain.Roles.Data;
using Authy.Presentation.Domain.Scopes.Data;
using Authy.Presentation.Entitites;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Domain.Roles;

public record UpsertRoleCommand(Guid OrganizationId, string Name, List<string> ScopeNames, Guid UserId) : ICommand<Result<Role>>;

public class UpsertRoleCommandHandler(
    IAuthorizationService authorizationService,
    IRoleRepository roleRepository,
    IScopeRepository scopeRepository)
    : ICommandHandler<UpsertRoleCommand, Result<Role>>
{
    public async Task<Result<Role>> HandleAsync(UpsertRoleCommand command, CancellationToken cancellationToken)
    {
        var validationFailure = Validate(command).FailureOrNull<Role>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var authResult = await authorizationService.EnsureRootIpOrOwnerAsync(
            command.OrganizationId, 
            command.UserId, 
            cancellationToken);

        if (authResult.IsFailure)
        {
            return Result.Failure<Role>(authResult.Error);
        }

        // Verify scopes exist in organization
        var foundScopes = await scopeRepository.GetByNamesAsync(command.OrganizationId, command.ScopeNames, cancellationToken);
        
        var missingScopeNames = command.ScopeNames
            .Distinct()
            .Except(foundScopes.Select(s => s.Name))
            .ToList();

        var scopeFailure = missingScopeNames
            .Select(DomainErrors.Role.ScopeNotFound)
            .ToList()
            .FailureOrNull<Role>();

        if (scopeFailure is not null)
        {
            return scopeFailure;
        }

        var role = await roleRepository.GetByNameAsync(command.OrganizationId, command.Name, cancellationToken);

        if (role == null)
        {
            role = new Role
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                OrganizationId = command.OrganizationId,
                Scopes = foundScopes
            };
            await roleRepository.AddAsync(role, cancellationToken);
        }
        else
        {
            role.Scopes = foundScopes;
            await roleRepository.UpdateAsync(role, cancellationToken);
        }

        return Result.Success(role);
    }

    private static List<Error> Validate(UpsertRoleCommand command)
    {
        var errors = new List<Error>();

        if (command.ScopeNames.Count == 0)
        {
            errors.Add(DomainErrors.Role.ScopesRequired);
        }

        return errors;
    }
}

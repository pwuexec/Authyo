using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;
using Authy.Presentation.Domain.Scopes.Data;
using Authy.Presentation.Entitites;

namespace Authy.Presentation.Domain.Scopes;

public record UpsertScopeFields(string? Name);

public record UpsertScopeCommand(Guid OrganizationId, string Name, Guid UserId, UpsertScopeFields UpsertFields) : ICommand<Result<Scope>>;

public class UpsertScopeCommandHandler(
    IAuthorizationService authorizationService,
    IScopeRepository scopeRepository)
    : ICommandHandler<UpsertScopeCommand, Result<Scope>>
{
    public async Task<Result<Scope>> HandleAsync(UpsertScopeCommand command, CancellationToken cancellationToken)
    {
        var validationFailure = Validate(command).FailureOrNull<Scope>();
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
            return Result.Failure<Scope>(authResult.Error);
        }

        var scope = await scopeRepository.GetByNameAsync(command.OrganizationId, command.Name, cancellationToken);

        if (scope == null)  
        {
            scope = new Scope
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                OrganizationId = command.OrganizationId
            };

            await scopeRepository.AddAsync(scope, cancellationToken);
        }
        else
        {
            // Scope already exists with this name in this organization.
            // Since there are no other editable fields on scope currently, we just return the existing one.
            // But we could call UpdateAsync if we had more fields.
            scope.Name = Update.IfProvided(scope.Name, command.UpsertFields.Name);

            await scopeRepository.UpdateAsync(scope, cancellationToken);
        }

        return Result.Success(scope);
    }

    private static List<Error> Validate(UpsertScopeCommand command)
    {
        var errors = new List<Error>();

        errors.NotEmptyIfNotNull(command.UpsertFields.Name, DomainErrors.Scope.NameEmpty);

        return errors;
    }
}

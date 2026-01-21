using Authy.Presentation.Domain;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Domain.Scopes;

public record CreateScopeCommand(Guid OrganizationId, string Name, Guid UserId) : ICommand<Result<Scope>>;

public class CreateScopeCommandHandler : ICommandHandler<CreateScopeCommand, Result<Scope>>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IScopeRepository _scopeRepository;

    public CreateScopeCommandHandler(
        IAuthorizationService authorizationService,
        IScopeRepository scopeRepository)
    {
        _authorizationService = authorizationService;
        _scopeRepository = scopeRepository;
    }

    public async Task<Result<Scope>> HandleAsync(CreateScopeCommand command, CancellationToken cancellationToken)
    {
        var authResult = await _authorizationService.EnsureRootIpOrOwnerAsync(
            command.OrganizationId, 
            command.UserId, 
            cancellationToken);

        if (authResult.IsFailure)
        {
            return Result.Failure<Scope>(authResult.Error);
        }

        var scope = new Scope
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            OrganizationId = command.OrganizationId
        };

        await _scopeRepository.AddAsync(scope, cancellationToken);

        return Result.Success(scope);
    }
}

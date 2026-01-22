namespace Authy.Presentation.Domain.Scopes;

public interface IScopeRepository
{
    Task AddAsync(Scope scope, CancellationToken cancellationToken);
}

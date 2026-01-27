using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Domain.Users;

public record GenerateTokenCommand(Guid UserId) : ICommand<Result<string>>;

public class GenerateTokenCommandHandler(
    IUserRepository userRepository,
    IJwtService jwtService)
    : ICommandHandler<GenerateTokenCommand, Result<string>>
{
    public async Task<Result<string>> HandleAsync(GenerateTokenCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<string>(DomainErrors.User.NotFound); // Need to ensure this exists
        }

        // Aggregate all scopes from all roles
        var scopes = user.Roles
            .SelectMany(r => r.Scopes)
            .Select(s => s.Name)
            .Distinct()
            .ToList();

        var token = jwtService.GenerateToken(user.Id, user.Name, scopes);

        return Result.Success(token);
    }
}

using Authy.Presentation.Shared;

namespace Authy.Presentation.Shared.Abstractions;

public interface ICommand<TResult>
{
}

public interface ICommand : ICommand<Result>
{
}

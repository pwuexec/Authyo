namespace Authy.Application.Shared;

public static class ResultExtensions
{
    extension(List<Error> errors)
    {
        public Result<T>? FailureOrNull<T>()
        {
            return errors.Count > 0 ? Result.Failure<T>(errors.ToArray()) : null;
        }

        public Result? FailureOrNull()
        {
            return errors.Count > 0 ? Result.Failure(errors.ToArray()) : null;
        }
    }
}


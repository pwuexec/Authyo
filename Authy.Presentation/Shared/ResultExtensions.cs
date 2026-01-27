namespace Authy.Presentation.Shared;

public static class ResultExtensions
{
    public static Result<T>? FailureOrNull<T>(this List<Error> errors)
    {
        return errors.Count > 0 ? Result.Failure<T>(errors.ToArray()) : null;
    }
    
    public static Result? FailureOrNull(this List<Error> errors)
    {
        return errors.Count > 0 ? Result.Failure(errors.ToArray()) : null;
    }
}

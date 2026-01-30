namespace Authy.Application.Shared;

public static class Ensure
{
    public static void NotEmptyIfNotNull(this List<Error> errors, string? value, Error error)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
        {
            errors.Add(error);
        }
    }
}


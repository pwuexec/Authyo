namespace Authy.Application.Shared;

public static class Update
{
    public static T IfProvided<T>(T current, T? next) where T : class
    {
        if (next is not null && !next.Equals(current))
        {
            return next;
        }

        return current;
    }
}


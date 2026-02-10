namespace Authy.Application.Extensions;

public static class OptionsExtensions
{
    public static TOptions AddAuthyOptions<TOptions>(this IServiceCollection services, IConfiguration configuration, string? sectionName = null)
        where TOptions : class
    {
        var configSectionName = sectionName ?? GetSectionName<TOptions>();
        var section = configuration.GetSection(configSectionName);

        if (!section.Exists())
        {
            throw new InvalidOperationException($"Configuration section '{configSectionName}' is missing.");
        }

        services
            .AddOptions<TOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return section.Get<TOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{configSectionName}' could not be bound to {typeof(TOptions).Name}.");
    }

    private static string GetSectionName<TOptions>()
    {
        const string optionsSuffix = "Options";
        var typeName = typeof(TOptions).Name;

        if (typeName.EndsWith(optionsSuffix, StringComparison.Ordinal))
        {
            return typeName[..^optionsSuffix.Length];
        }

        return typeName;
    }
}

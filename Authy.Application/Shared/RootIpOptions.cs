namespace Authy.Application.Shared;

public class RootIpOptions
{
    public const string SectionName = "RootIps";

    public string[] RootIps { get; set; } = Array.Empty<string>();
}

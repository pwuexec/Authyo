using System.Diagnostics.Metrics;

namespace Authy.Application.Shared.Diagnostics;

public class Telemetry
{
    public const string ServiceName = "Authy";
    public static readonly Meter Meter = new(ServiceName);

    public static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>(
        "authy.command.duration",
        unit: "ms",
        description: "Duration of command execution");

    public static readonly Histogram<double> QueryDuration = Meter.CreateHistogram<double>(
        "authy.query.duration",
        unit: "ms",
        description: "Duration of query execution");

    public static readonly Counter<long> AuthenticationAttempts = Meter.CreateCounter<long>(
        "authy.authentication.attempts",
        unit: "{attempt}",
        description: "Number of authentication attempts");
}

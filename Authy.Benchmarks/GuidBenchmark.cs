using BenchmarkDotNet.Attributes;

namespace Authy.Benchmarks;

[MemoryDiagnoser]
public class GuidBenchmark
{
    [Benchmark(Baseline = true)]
    public Guid NewGuid() => Guid.NewGuid();

    [Benchmark]
    public Guid CreateVersion7() => Guid.CreateVersion7();
}

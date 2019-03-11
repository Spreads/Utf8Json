using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using PerfBenchmark;

class Program
{
    static void Main(string[] args)
    {
        var switcher = new BenchmarkSwitcher(new[]
        {
            typeof(SerializeBenchmark),
            typeof(DeserializeBenchmark),
            typeof(JsonSerializeBench)
        });

        // args = new string[] { "0" };

#if DEBUG
        var b = new DeserializeBenchmark();
        b.Utf8JsonSerializer();
#else


        switcher.Run(args);
#endif
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        Add(MarkdownExporter.GitHub);
        Add(MemoryDiagnoser.Default);

        var baseConfig = Job.ShortRun; // .WithLaunchCount(1).WithTargetCount(2).WithWarmupCount(1);
        //Add(baseConfig.With(Runtime.Core).With(Jit.RyuJit).With(Platform.X64));
        Add(baseConfig.With(Runtime.Core).With(Jit.RyuJit).With(Platform.X64).With(CsProjCoreToolchain.NetCoreApp30));
    }
}
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace DemoFile.Benchmark;

[WarmupCount(1)]
[MemoryDiagnoser]
[Config(typeof(Config))]
public class DemoParserBenchmark
{
    private class Config : ManualConfig
    {
        public Config()
        {
            var baseJob = Job.Default;

            AddJob(baseJob.WithArguments(new[] {new MsBuildArgument("/p:Baseline=true")}).AsBaseline().WithId("Baseline"));
            AddJob(baseJob.WithId("Current"));
        }
    }

    private DemoParser _demoParser;
    private MemoryStream _fileStream;
    private byte[] _demoBytes;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _demoBytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos", "space-vs-forward-m1-ancient.dem"));
    }

    [IterationSetup]
    public void Setup()
    {
        _fileStream = new MemoryStream(_demoBytes);
    }

    [Benchmark]
    public async Task ParseDemo()
    {
        _demoParser = new DemoParser();
        await _demoParser.Start(_fileStream, default);
    }
}

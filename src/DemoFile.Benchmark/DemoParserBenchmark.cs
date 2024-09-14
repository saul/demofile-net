using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace DemoFile.Benchmark;

[WarmupCount(1)]
[MemoryDiagnoser]
[Config(typeof(Config))]
[MaxIterationCount(16)]
public class DemoParserBenchmark
{
    private class Config : ManualConfig
    {
        public Config()
        {
            var baseJob = Job.Default;

            AddJob(baseJob.WithCustomBuildConfiguration("Baseline").AsBaseline().WithId("Baseline"));
            AddJob(baseJob);
            //AddJob(baseJob.WithArguments(new[] { new MsBuildArgument("/p:ClearBuf=true") }).WithId("ClearBuf"));
        }
    }

#if BASELINE
    private DemoParser _demoParser;
#else
    private CsDemoParser _demoParser;
#endif
    private MemoryStream _fileStream;
    private byte[] _demoBytes;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _demoBytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "demos", "navi-javelins-vs-9-pandas-fearless-m1-mirage.dem"));
    }

    [IterationSetup]
    public void Setup()
    {
        _fileStream = new MemoryStream(_demoBytes);
    }

    [Benchmark]
    public async Task ParseDemo()
    {
#if BASELINE
        _demoParser = new DemoParser();
        await _demoParser.ReadAllAsync(_fileStream, default);
#else
        _demoParser = new CsDemoParser();
        await _demoParser.ReadAllAsync(_fileStream, default);
#endif
    }

    [Benchmark]
    public async Task ParseDemoParallel()
    {
#if BASELINE
        await DemoParser.ReadAllParallelAsync(_demoBytes, _ => { }, default);
#else
        await CsDemoParser.ReadAllParallelAsync(_demoBytes, _ => { }, default);
#endif
    }
}

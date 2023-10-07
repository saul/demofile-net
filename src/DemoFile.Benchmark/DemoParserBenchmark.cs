using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace DemoFile.Benchmark;

[SimpleJob]
[WarmupCount(1)]
[MemoryDiagnoser]
public class DemoParserBenchmark
{
    private DemoParser _demoParser;
    private MemoryStream _fileStream;
    private byte[] _demoBytes;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _demoBytes = File.ReadAllBytes(@"/Users/saul/Code/demofile-net/demos/space-vs-forward-m1-ancient.dem");
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

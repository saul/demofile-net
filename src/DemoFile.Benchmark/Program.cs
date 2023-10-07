using BenchmarkDotNet.Running;

namespace DemoFile.Benchmark;

internal class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<DemoParserBenchmark>();
    }
}

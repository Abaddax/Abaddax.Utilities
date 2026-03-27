using Abaddax.Utilities.Benchmarks.Buffers;
using Abaddax.Utilities.Benchmarks.Threading;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Abaddax.Utilities.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run(
                [
                    //typeof(BufferPoolBenchmark),
                    typeof(SemaphoreBenchmarks),
                    typeof(ManualResetEventBenchmark)
                ]
                //ManualConfig.Create(DefaultConfig.Instance)
                //    .WithOption(ConfigOptions.JoinSummary, true)
            );
        }
    }
}

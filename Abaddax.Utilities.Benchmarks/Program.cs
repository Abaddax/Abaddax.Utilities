using Abaddax.Utilities.Benchmarks.Buffers;
using BenchmarkDotNet.Running;

namespace Abaddax.Utilities.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<BufferPoolBenchmark>();
        }
    }
}

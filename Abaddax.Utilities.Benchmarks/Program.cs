using Abaddax.Utilities.Benchmarks.Buffers;
using BenchmarkDotNet.Running;

namespace Abaddax.Utilities.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //BufferPoolBenchmark x = new()
            //{
            //    Size = 1000
            //};

            //for(int i = 0; i<100000; i++)
            //{
            //    x.AllocArrayPoolExtensions2();
            //}
            //x.AllocArrayPoolExtensions2();

            BenchmarkRunner.Run<BufferPoolBenchmark>();
        }
    }
}

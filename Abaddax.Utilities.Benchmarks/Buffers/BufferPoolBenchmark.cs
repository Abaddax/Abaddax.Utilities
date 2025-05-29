using Abaddax.Utilities.Buffers;
using BenchmarkDotNet.Attributes;

namespace Abaddax.Utilities.Benchmarks.Buffers
{
    [MemoryDiagnoser(true)]
    public class BufferPoolBenchmark
    {
        [Params(100, 500, 1000, 1500, 10000)]
        public int Size { get; set; }

        [Benchmark]
        public int AllocDefault()
        {
            var buffer = new byte[Size];
            return buffer.Length;
        }
        [Benchmark(Baseline = true)]
        public int AllocPool()
        {
            using var buffer = BufferPool<byte>.Rent(Size);
            return buffer.Span.Length;
        }
        [Benchmark]
        public int AllocUninitialized()
        {
            var buffer = GC.AllocateUninitializedArray<byte>(Size);
            return buffer.Length;
        }
    }
}

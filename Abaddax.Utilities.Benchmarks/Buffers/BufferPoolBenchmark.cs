using Abaddax.Utilities.Buffers;
using BenchmarkDotNet.Attributes;
using System.Buffers;

namespace Abaddax.Utilities.Benchmarks.Buffers
{
    [MemoryDiagnoser(true)]
    public class BufferPoolBenchmark
    {
        [Params(100, 1000, 5000, 50000)]
        public int Size { get; set; }

        [Benchmark(Baseline = true)]
        public byte AllocDefault()
        {
            var buffer = new byte[Size];
            return buffer[buffer.Length - 1];
        }
        [Benchmark]
        public byte AllocUninitialized()
        {
            var buffer = GC.AllocateUninitializedArray<byte>(Size);
            return buffer[buffer.Length - 1];
        }
        //[Benchmark(Baseline = true)]
        //public byte AllocBufferPool()
        //{
        //    using var buffer = BufferPool<byte>.Rent(Size);
        //    return buffer[buffer.Length - 1];
        //}
        [Benchmark]
        public byte AllocArrayPool()
        {
            var buffer = ArrayPool<byte>.Shared.Rent(Size);
            try
            {
                return buffer[buffer.Length - 1];
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        [Benchmark]
        public byte AllocArrayPoolExtensions()
        {
            using var buffer = ArrayPool<byte>.Shared.RentArray(Size);
            return buffer[buffer.Length - 1];
        }
        [Benchmark]
        public byte AllocMemoryPool()
        {
            using var buffer = MemoryPool<byte>.Shared.Rent(Size);
            return buffer.Memory.Span[buffer.Memory.Length - 1];
        }
    }
}

using System.Buffers;

namespace Abaddax.Utilities.Buffers
{
    public static class BufferPool<T>
    {
        public const int DefaultSmallBufferOptimizationThreshhold = 1024;

        public static DisposableBuffer Rent(int length, int smallBufferThreshold = DefaultSmallBufferOptimizationThreshhold)
        {
            if (length <= smallBufferThreshold)
            {
                var buffer = GC.AllocateUninitializedArray<T>(length);
                return new DisposableBuffer(buffer, length, false);
            }
            else
            {
                var buffer = ArrayPool<T>.Shared.Rent(length);
                return new DisposableBuffer(buffer, length, true);
            }
        }

        public static DisposableBuffer Copy(ReadOnlySpan<T> span, int smallBufferThreshold = DefaultSmallBufferOptimizationThreshhold)
        {
            var buffer = Rent(span.Length, smallBufferThreshold);
            span.CopyTo(buffer);
            return buffer;
        }
        public static DisposableBuffer Copy(ReadOnlyMemory<T> memory, int smallBufferThreshold = DefaultSmallBufferOptimizationThreshhold)
        {
            var buffer = Rent(memory.Length, smallBufferThreshold);
            memory.CopyTo(buffer);
            return buffer;
        }


        public class DisposableBuffer : IDisposable
        {
            private readonly T[] _buffer;
            private readonly int _length;
            private readonly bool _pooledBuffer;
            private bool _disposedValue = false;

            public Span<T> Span
            {
                get
                {
                    ObjectDisposedException.ThrowIf(_disposedValue, this);
                    return new Span<T>(_buffer, 0, _length);
                }
            }
            public Memory<T> Memory
            {
                get
                {
                    ObjectDisposedException.ThrowIf(_disposedValue, this);
                    return new Memory<T>(_buffer, 0, _length);
                }
            }

            public T this[int i]
            {
                get => Span[i];
                set => Span[i] = value;
            }
            public Span<T> this[Range range]
            {
                get => Span[range];
            }

            public static implicit operator Span<T>(DisposableBuffer buffer) => buffer.Span;
            public static implicit operator Memory<T>(DisposableBuffer buffer) => buffer.Memory;
            public static implicit operator ReadOnlySpan<T>(DisposableBuffer buffer) => buffer.Span;
            public static implicit operator ReadOnlyMemory<T>(DisposableBuffer buffer) => buffer.Memory;

            internal DisposableBuffer(T[] buffer, int length, bool pooledBuffer)
            {
                _buffer = buffer;
                _length = length;
                _pooledBuffer = pooledBuffer;
            }

            #region IDisposable
            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (_pooledBuffer)
                        ArrayPool<T>.Shared.Return(_buffer);
                    _disposedValue = true;
                }
            }
            ~DisposableBuffer()
            {
                Dispose(disposing: false);
            }
            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
            #endregion
        }
    }
}

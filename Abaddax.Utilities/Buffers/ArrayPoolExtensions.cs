using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Abaddax.Utilities.Buffers
{
    public static class ArrayPoolExtensions
    {
        public static PooledArray<T> RentArray<T>(this ArrayPool<T> arrayPool, int size)
        {
            var buffer = arrayPool.Rent(size);
            return new PooledArray<T>(arrayPool, buffer, size);
        }
        public static PooledArray<T> CopyArray<T>(this ArrayPool<T> arrayPool, ReadOnlySpan<T> span)
        {
            var buffer = arrayPool.Rent(span.Length);
            span.CopyTo(buffer);
            return new PooledArray<T>(arrayPool, buffer, span.Length);
        }
        public static PooledArray<T> CopyArray<T>(this ArrayPool<T> arrayPool, ReadOnlyMemory<T> memory)
        {
            var buffer = arrayPool.Rent(memory.Length);
            memory.CopyTo(buffer);
            return new PooledArray<T>(arrayPool, buffer, memory.Length);
        }

        public sealed class PooledArray<T> : IDisposable
        {
            private readonly ArrayPool<T> _pool;
            private readonly int _length;
            private T[]? _buffer;
            private MemoryManager? _memoryManager;

            public int Length
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Span.Length;
            }

            public Span<T> Span
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    ObjectDisposedException.ThrowIf(_buffer == null, this);
                    return new Span<T>(_buffer, 0, _length);
                }
            }
            public Memory<T> Memory
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    _memoryManager ??= new MemoryManager(this);
                    return _memoryManager.Memory;
                }
            }

            public T this[int i]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Span[i];
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => Span[i] = value;
            }
            public Span<T> this[Range range]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Span[range];
            }

            internal PooledArray(ArrayPool<T> pool, T[] buffer, int length)
            {
                _pool = pool ?? throw new ArgumentNullException(nameof(pool));
                _buffer = buffer;
                _length = length;
            }

            #region IDisposable
            private void Dispose(bool disposing)
            {
                if (_buffer != null)
                {
                    if (disposing)
                    {
                        (_memoryManager as IDisposable)?.Dispose();
                    }
                    //Memory mamanger will call Dispose as well
                    if (_buffer != null)
                        _pool.Return(_buffer);
                    _buffer = null;
                }
            }
            ~PooledArray()
            {
                Dispose(disposing: false);
            }
            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
            #endregion

            /// <summary>
            /// Get access to raw rented array.
            /// Array might exceed <see cref="Length"/>!
            /// Only use if nesseary!
            /// </summary>
            public static T[] GetRawArray(PooledArray<T> array)
            {
                ObjectDisposedException.ThrowIf(array._buffer == null, typeof(PooledArray<T>));
                return array._buffer;
            }

            public static implicit operator Span<T>(PooledArray<T> buffer) => buffer.Span;
            public static implicit operator ReadOnlySpan<T>(PooledArray<T> buffer) => buffer.Span;
            public static implicit operator Memory<T>(PooledArray<T> buffer) => buffer.Memory;
            public static implicit operator ReadOnlyMemory<T>(PooledArray<T> buffer) => buffer.Memory;

            #region Helper
            private sealed class MemoryManager : MemoryManager<T>
            {
                private readonly PooledArray<T> _buffer;
                MemoryHandle? _pinnedHandle;
                public MemoryManager(PooledArray<T> buffer)
                {
                    _buffer = buffer;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public override Span<T> GetSpan() => _buffer.Span;
                public unsafe override MemoryHandle Pin(int elementIndex = 0)
                {
                    ObjectDisposedException.ThrowIf(_buffer._buffer == null, this);
                    lock (_buffer)
                    {
                        if (_pinnedHandle != null)
                            throw new InvalidOperationException("Already pinned");
                        var gcHandle = GCHandle.Alloc(_buffer._buffer, GCHandleType.Pinned);
                        _pinnedHandle = new MemoryHandle(gcHandle.AddrOfPinnedObject().ToPointer(), gcHandle);
                        return _pinnedHandle.Value;
                    }
                }
                public override void Unpin()
                {
                    lock (_buffer)
                    {
                        _pinnedHandle?.Dispose();
                        _pinnedHandle = null;
                    }
                }
                protected override void Dispose(bool disposing)
                {
                    Unpin();
                    if (disposing)
                        _buffer.Dispose(false);
                }
            }
            #endregion
        }
    }
}

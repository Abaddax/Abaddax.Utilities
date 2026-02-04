using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if !NET9_0_OR_GREATER
using Abaddax.Utilities.Collections;
#endif

namespace Abaddax.Utilities.Buffers
{
    public static class ArrayPoolExtensions
    {
        public static PooledArray<T> RentArray<T>(this ArrayPool<T> arrayPool, int size)
        {
            var buffer = arrayPool.Rent(size);
            return new PooledArray<T>(arrayPool, buffer, size);
        }
#if NET9_0_OR_GREATER
        [OverloadResolutionPriority(2)]
#endif
        public static PooledArray<T> CopyArray<T>(this ArrayPool<T> arrayPool, ReadOnlySpan<T> span)
        {
            var buffer = arrayPool.Rent(span.Length);
            span.CopyTo(buffer);
            return new PooledArray<T>(arrayPool, buffer, span.Length);
        }
#if NET9_0_OR_GREATER
        [OverloadResolutionPriority(1)]
#endif
        public static PooledArray<T> CopyArray<T>(this ArrayPool<T> arrayPool, ReadOnlyMemory<T> memory)
        {
            var buffer = arrayPool.Rent(memory.Length);
            memory.CopyTo(buffer);
            return new PooledArray<T>(arrayPool, buffer, memory.Length);
        }
        public static PooledArray<T> CopyArray<T>(this ArrayPool<T> arrayPool, IEnumerable<T> enumerable)
        {
            using var resizableBuffer = new ResizeableBuffer<T, PooledArray<T>>(16,
                (length) => arrayPool.RentArray((int)length));
            if (enumerable is T[] arr)
                return CopyArray(arrayPool, arr.AsSpan());
            if (enumerable is List<T> list)
                return CopyArray(arrayPool, CollectionsMarshal.AsSpan(list));
            foreach (var item in enumerable.Index())
            {
                if (item.Index >= resizableBuffer.Length)
                    resizableBuffer.Resize((uint)Math.Min(resizableBuffer.Length * 2, 4096));
                resizableBuffer[item.Index] = item.Item;
            }
            return arrayPool.CopyArray(resizableBuffer.Span);
        }
    }
    public sealed class PooledArray<T> : IBuffer<T>, IDisposable
    {
        private readonly ArrayPool<T> _pool;
        private readonly int _length;
        private readonly T[] _buffer;
        private MemoryManager? _memoryManager;
        private bool _disposedValue = false;

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
                ObjectDisposedException.ThrowIf(_disposedValue, this);
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
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _length = length;
        }

        #region IDisposable
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    (_memoryManager as IDisposable)?.Dispose();
                }
                _pool.Return(_buffer);
                _disposedValue = true;
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
            private GCHandle? _gcHandle = null;
            public MemoryManager(PooledArray<T> buffer)
            {
                _buffer = buffer;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Span<T> GetSpan() => _buffer.Span;
            public unsafe override MemoryHandle Pin(int elementIndex = 0)
            {
                ObjectDisposedException.ThrowIf(_buffer._disposedValue, this);
                ArgumentOutOfRangeException.ThrowIfLessThan(elementIndex, 0);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(elementIndex, _buffer.Length);
                lock (_buffer)
                {
                    if (_gcHandle != null)
                        throw new InvalidOperationException("Already pinned");
                    _gcHandle = GCHandle.Alloc(_buffer._buffer, GCHandleType.Pinned);
                    void* pointer = Unsafe.Add<T>((void*)_gcHandle.Value.AddrOfPinnedObject(), elementIndex);
                    return new MemoryHandle(pointer, default, this);
                }
            }
            public override void Unpin()
            {
                lock (_buffer)
                {
                    _gcHandle?.Free();
                    _gcHandle = null;
                }
            }
            protected override void Dispose(bool disposing)
            {
                Unpin();
            }
        }
        #endregion
    }
}

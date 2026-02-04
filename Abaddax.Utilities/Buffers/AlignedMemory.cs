using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Abaddax.Utilities.Buffers
{
    public static unsafe class AlignedMemory
    {
        public static NativeArray<T> Allocate<T>(uint size, uint alignment)
            where T : unmanaged
        {
            var byteCount = size * sizeof(T);
            var buffer = NativeMemory.AlignedAlloc((nuint)byteCount, alignment);
            return new NativeArray<T>((T*)buffer, (int)size);
        }
        public static NativeArray<T> CopyArray<T>(ReadOnlySpan<T> span, uint alignment)
            where T : unmanaged
        {
            var buffer = Allocate<T>((uint)span.Length, alignment);
            span.CopyTo(buffer);
            return buffer;
        }

        public unsafe sealed class NativeArray<T> : IBuffer<T>, IDisposable
            where T : unmanaged
        {
            private readonly int _length;
            private readonly T* _buffer;
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
                    return new Span<T>(_buffer, _length);
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

            internal NativeArray(T* buffer, int length)
            {
                _buffer = buffer != null ? buffer : throw new ArgumentNullException(nameof(buffer));
                _length = length;
            }

            #region IDisposable
            private void Dispose(bool disposing)
            {
                //TODO: Avoid race conditions
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        (_memoryManager as IDisposable)?.Dispose();
                    }
                    NativeMemory.AlignedFree(_buffer);
                    _disposedValue = true;
                }
            }
            ~NativeArray()
            {
                Dispose(disposing: false);
            }
            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
            #endregion

            public static implicit operator Span<T>(NativeArray<T> buffer) => buffer.Span;
            public static implicit operator ReadOnlySpan<T>(NativeArray<T> buffer) => buffer.Span;

            private unsafe sealed class MemoryManager : MemoryManager<T>
            {
                private readonly NativeArray<T> _buffer;
                public MemoryManager(NativeArray<T> buffer)
                {
                    _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public override Span<T> GetSpan() => _buffer.Span;

                public override MemoryHandle Pin(int elementIndex = 0)
                {
                    ObjectDisposedException.ThrowIf(_buffer._disposedValue, this);
                    ArgumentOutOfRangeException.ThrowIfLessThan(elementIndex, 0);
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(elementIndex, _buffer.Length);
                    // memory is already pinned
                    return new MemoryHandle(_buffer._buffer + elementIndex, default, this);
                }
                public override void Unpin()
                {
                    // noop
                }
                protected override void Dispose(bool disposing)
                {
                    Unpin();
                }
            }
        }
    }
}

using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Buffers
{
    public sealed class ResizeableBuffer<T, TBuffer> : IBuffer<T>, IDisposable
        where TBuffer : IBuffer<T>
    {
        public delegate TBuffer RentBufferCallback(uint length);

        private readonly RentBufferCallback _rentBufferCallback;
        private readonly Lock _lock = new();
        private TBuffer _buffer;
        private uint _length;
        private bool _disposedValue;

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Span.Length;
        }
        public Span<T> Span
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposedValue, this);
                return _buffer.Span.Slice(0, (int)_length);
            }
        }
        public Memory<T> Memory
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposedValue, this);
                return _buffer.Memory.Slice(0, (int)_length);
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

        public ResizeableBuffer(uint length, RentBufferCallback rentBufferCallback)
        {
            _rentBufferCallback = rentBufferCallback ?? throw new ArgumentNullException(nameof(rentBufferCallback));
            _buffer = _rentBufferCallback.Invoke(length) ?? throw new Exception($"Received 'null' from {nameof(RentBufferCallback)}");
            _length = length;
        }

        public void Resize(uint length, bool copyContent = false)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            if (_length == length)
                return;
            lock (_lock)
            {
                if (_buffer.Length < length)
                {
                    var oldBuffer = _buffer;
                    var newBuffer = _rentBufferCallback.Invoke(length) ?? throw new Exception($"Received 'null' from {nameof(RentBufferCallback)}");
                    _buffer = newBuffer;
                    if (copyContent)
                        oldBuffer.Span.CopyTo(newBuffer.Span);
                    _length = length;
                    oldBuffer.Dispose();
                }
                else
                {
                    _length = length;
                }
            }
        }

        #region Disposable
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }
                _buffer.Dispose();
                _disposedValue = true;
            }
        }
        ~ResizeableBuffer()
        {
            Dispose(disposing: false);
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        public static implicit operator Span<T>(ResizeableBuffer<T, TBuffer> buffer) => buffer.Span;
        public static implicit operator ReadOnlySpan<T>(ResizeableBuffer<T, TBuffer> buffer) => buffer.Span;
    }


}

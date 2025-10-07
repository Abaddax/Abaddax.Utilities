using Abaddax.Utilities.Threading.Tasks;
using System.Buffers;

namespace Abaddax.Utilities.IO
{
    public class CallbackStream : SpanStream, IDisposable
    {
        public delegate int ReadCallback(Span<byte> buffer);
        public delegate void WriteCallback(ReadOnlySpan<byte> buffer);
        public delegate ValueTask<int> ReadCallbackAsync(Memory<byte> buffer, CancellationToken cancellationToken);
        public delegate ValueTask WriteCallbackAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

        private readonly ReadCallback _readCallback;
        private readonly WriteCallback _writeCallback;
        private readonly ReadCallbackAsync _readCallbackAsync;
        private readonly WriteCallbackAsync _writeCallbackAsync;
        private bool _disposedValue = false;

        public CallbackStream(ReadCallback readCallback, WriteCallback writeCallback)
        {
            ArgumentNullException.ThrowIfNull(readCallback);
            ArgumentNullException.ThrowIfNull(writeCallback);

            _readCallback = readCallback;
            _writeCallback = writeCallback;
            _readCallbackAsync = (buffer, token) =>
            {
                var result = _readCallback.Invoke(buffer.Span);
                return ValueTask.FromResult(result);
            };
            _writeCallbackAsync = (buffer, token) =>
            {
                _writeCallback.Invoke(buffer.Span);
                return ValueTask.CompletedTask;
            };
        }
        public CallbackStream(ReadCallbackAsync readCallback, WriteCallbackAsync writeCallback)
        {
            ArgumentNullException.ThrowIfNull(readCallback);
            ArgumentNullException.ThrowIfNull(writeCallback);

            _readCallbackAsync = readCallback;
            _writeCallbackAsync = writeCallback;
            _readCallback = (buffer) =>
            {
                using (var sharedMemory = MemoryPool<byte>.Shared.Rent(buffer.Length))
                {
                    var memory = sharedMemory.Memory;
                    buffer.CopyTo(memory.Span);
                    var task = _readCallbackAsync.Invoke(memory, default).AsTask();
                    var result = task.AwaitSync();
                    return result;
                }
            };
            _writeCallback = (buffer) =>
            {
                using (var sharedMemory = MemoryPool<byte>.Shared.Rent(buffer.Length))
                {
                    var memory = sharedMemory.Memory;
                    buffer.CopyTo(memory.Span);
                    var task = _writeCallbackAsync.Invoke(memory, default).AsTask();
                    task.AwaitSync();
                    return;
                }
            };
        }

        public override int Read(Span<byte> buffer)
            => _readCallback.Invoke(buffer);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            => _readCallbackAsync.Invoke(buffer, cancellationToken);

        public override void Write(ReadOnlySpan<byte> buffer)
            => _writeCallback.Invoke(buffer);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            => _writeCallbackAsync.Invoke(buffer, cancellationToken);

        #region Stream
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        #endregion

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                base.Dispose(disposing);
                _disposedValue = true;
            }
        }
        #endregion
    }
}

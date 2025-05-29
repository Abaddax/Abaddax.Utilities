using Abaddax.Utilities.Buffers;
using Abaddax.Utilities.Threading.Tasks;

namespace Abaddax.Utilities.IO
{
    public class CallbackStream : SpanStream, IDisposable
    {
        public delegate int ReadCallback(Span<byte> buffer);
        public delegate void WriteCallback(ReadOnlySpan<byte> buffer);
        public delegate ValueTask<int> ReadCallbackAsync(Memory<byte> buffer, CancellationToken cancellationToken);
        public delegate ValueTask WriteCallbackAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

        private readonly Stream _innerStream;
        private readonly bool _leaveOpen;
        private readonly ReadCallback _readCallback;
        private readonly WriteCallback _writeCallback;
        private readonly ReadCallbackAsync _readCallbackAsync;
        private readonly WriteCallbackAsync _writeCallbackAsync;
        private bool _disposedValue = false;

        public CallbackStream(Stream stream, ReadCallback readCallback, WriteCallback writeCallback, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(readCallback);
            ArgumentNullException.ThrowIfNull(writeCallback);

            _innerStream = stream;
            _leaveOpen = leaveOpen;
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
        public CallbackStream(Stream stream, ReadCallbackAsync readCallback, WriteCallbackAsync writeCallback, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(readCallback);
            ArgumentNullException.ThrowIfNull(writeCallback);

            _innerStream = stream;
            _leaveOpen = leaveOpen;
            _readCallbackAsync = readCallback;
            _writeCallbackAsync = writeCallback;
            _readCallback = (buffer) =>
            {
                using (var memory = BufferPool<byte>.Copy(buffer))
                {
                    var task = _readCallbackAsync.Invoke(memory, default).AsTask();
                    var result = task.AwaitSync();
                    return result;
                }
            };
            _writeCallback = (buffer) =>
            {
                using (var memory = BufferPool<byte>.Copy(buffer))
                {
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
        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public override void Flush() => _innerStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);
        #endregion

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (!_leaveOpen)
                    _innerStream?.Dispose();
                base.Dispose(disposing);
                _disposedValue = true;
            }
        }
        #endregion
    }
}

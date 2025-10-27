using Abaddax.Utilities.Threading.Tasks;
using System.Buffers;

namespace Abaddax.Utilities.IO
{
    public class CallbackStream : SpanStream, IDisposable
    {
        public delegate int ReadCallback(Span<byte> buffer, object? state);
        public delegate void WriteCallback(ReadOnlySpan<byte> buffer, object? state);
        public delegate ValueTask<int> ReadCallbackAsync(Memory<byte> buffer, object? state, CancellationToken cancellationToken);
        public delegate ValueTask WriteCallbackAsync(ReadOnlyMemory<byte> buffer, object? state, CancellationToken cancellationToken);

        private readonly ReadCallback _readCallback;
        private readonly WriteCallback _writeCallback;
        private readonly ReadCallbackAsync _readCallbackAsync;
        private readonly WriteCallbackAsync _writeCallbackAsync;
        private bool _disposedValue = false;

        protected virtual object? State { get; set; }

        public CallbackStream(ReadCallback readCallback, WriteCallback writeCallback, object? state = null)
        {
            ArgumentNullException.ThrowIfNull(readCallback);
            ArgumentNullException.ThrowIfNull(writeCallback);

            State = state;

            _readCallback = readCallback;
            _writeCallback = writeCallback;
            _readCallbackAsync = (buffer, state, token) =>
            {
                var result = _readCallback.Invoke(buffer.Span, state);
                return ValueTask.FromResult(result);
            };
            _writeCallbackAsync = (buffer, state, token) =>
            {
                _writeCallback.Invoke(buffer.Span, state);
                return ValueTask.CompletedTask;
            };
        }
        public CallbackStream(ReadCallbackAsync readCallback, WriteCallbackAsync writeCallback, object? state = null)
        {
            ArgumentNullException.ThrowIfNull(readCallback);
            ArgumentNullException.ThrowIfNull(writeCallback);

            State = state;

            _readCallbackAsync = readCallback;
            _writeCallbackAsync = writeCallback;
            _readCallback = (buffer, state) =>
            {
                using (var sharedMemory = MemoryPool<byte>.Shared.Rent(buffer.Length))
                {
                    var memory = sharedMemory.Memory;
                    buffer.CopyTo(memory.Span);
                    var task = _readCallbackAsync.Invoke(memory, state, default).AsTask();
                    var result = task.AwaitSync();
                    return result;
                }
            };
            _writeCallback = (buffer, state) =>
            {
                using (var sharedMemory = MemoryPool<byte>.Shared.Rent(buffer.Length))
                {
                    var memory = sharedMemory.Memory;
                    buffer.CopyTo(memory.Span);
                    var task = _writeCallbackAsync.Invoke(memory, state, default).AsTask();
                    task.AwaitSync();
                    return;
                }
            };
        }

        public void UpdateState(object? state)
        {
            State = state;
        }

        public override int Read(Span<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _readCallback.Invoke(buffer, State);
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _readCallbackAsync.Invoke(buffer, State, cancellationToken);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            _writeCallback.Invoke(buffer, State);
        }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _writeCallbackAsync.Invoke(buffer, State, cancellationToken);

        }

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

        public override void Flush()
        {
            return;
        }
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
    public sealed class CallbackStream<TInnerStream> : CallbackStream
        where TInnerStream : Stream
    {
        public new delegate int ReadCallback(Span<byte> buffer, TInnerStream innerStream);
        public new delegate void WriteCallback(ReadOnlySpan<byte> buffer, TInnerStream innerStream);
        public new delegate ValueTask<int> ReadCallbackAsync(Memory<byte> buffer, TInnerStream innerStream, CancellationToken cancellationToken);
        public new delegate ValueTask WriteCallbackAsync(ReadOnlyMemory<byte> buffer, TInnerStream innerStream, CancellationToken cancellationToken);

        private readonly bool _leaveOpen;
        private bool _disposedValue = false;

        protected override object? State
        {
            set => throw new NotSupportedException();
        }
        private TInnerStream InnerStream => (TInnerStream)State!;

        public CallbackStream(TInnerStream innerStream, ReadCallback readCallback, WriteCallback writeCallback, bool leaveOpen = false)
            : base(
                readCallback: (buffer, state) => readCallback.Invoke(buffer, (TInnerStream)state!),
                writeCallback: (buffer, state) => writeCallback.Invoke(buffer, (TInnerStream)state!),
                state: innerStream
            )
        {
            ArgumentNullException.ThrowIfNull(innerStream);
            ArgumentNullException.ThrowIfNull(readCallback);
            ArgumentNullException.ThrowIfNull(writeCallback);

            _leaveOpen = leaveOpen;
        }
        public CallbackStream(TInnerStream innerStream, ReadCallbackAsync readCallback, WriteCallbackAsync writeCallback, object? state = null, bool leaveOpen = false)
            : base(
                readCallback: (buffer, state, cancellationToken) => readCallback.Invoke(buffer, (TInnerStream)state!, cancellationToken),
                writeCallback: (buffer, state, cancellationToken) => writeCallback.Invoke(buffer, (TInnerStream)state!, cancellationToken),
                state: innerStream
            )
        {
            ArgumentNullException.ThrowIfNull(innerStream);
            ArgumentNullException.ThrowIfNull(readCallback);
            ArgumentNullException.ThrowIfNull(writeCallback);

            _leaveOpen = leaveOpen;
        }

        #region Stream
        public override bool CanRead => InnerStream.CanRead;
        public override bool CanSeek => InnerStream.CanSeek;
        public override bool CanWrite => InnerStream.CanWrite;
        public override long Length => InnerStream.Length;
        public override long Position
        {
            get => InnerStream.Position;
            set => InnerStream.Position = value;
        }

        public override void Flush() => InnerStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => InnerStream.Seek(offset, origin);
        public override void SetLength(long value) => InnerStream.SetLength(value);
        #endregion

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (!_leaveOpen)
                        InnerStream.Dispose();
                }
                base.Dispose(disposing);
                _disposedValue = true;
            }
        }
        #endregion
    }
}

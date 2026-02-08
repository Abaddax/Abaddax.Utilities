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

#pragma warning disable CA1051
        protected ReadCallback _readCallback;
        protected WriteCallback _writeCallback;
        protected ReadCallbackAsync _readCallbackAsync;
        protected WriteCallbackAsync _writeCallbackAsync;
#pragma warning restore CA1051
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
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _readCallback.Invoke(buffer);
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _readCallbackAsync.Invoke(buffer, cancellationToken);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            _writeCallback.Invoke(buffer);
        }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _writeCallbackAsync.Invoke(buffer, cancellationToken);
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
    public class CallbackStream<TState> : CallbackStream
    {
        public new delegate int ReadCallback(Span<byte> buffer, TState state);
        public new delegate void WriteCallback(ReadOnlySpan<byte> buffer, TState state);
        public new delegate ValueTask<int> ReadCallbackAsync(Memory<byte> buffer, TState state, CancellationToken cancellationToken);
        public new delegate ValueTask WriteCallbackAsync(ReadOnlyMemory<byte> buffer, TState state, CancellationToken cancellationToken);

        public TState State { get; protected set; }

        public CallbackStream(TState state, ReadCallback readCallback, WriteCallback writeCallback)
            : base(
                  readCallback: (_) => throw new NotImplementedException(),
                  writeCallback: (_) => throw new NotImplementedException()
                  )
        {
            ArgumentNullException.ThrowIfNull(readCallback);
            ArgumentNullException.ThrowIfNull(writeCallback);

            State = state;

            _readCallback = (buffer) => readCallback.Invoke(buffer, State);
            _writeCallback = (buffer) => writeCallback.Invoke(buffer, State);
        }
        public CallbackStream(TState state, ReadCallbackAsync readCallback, WriteCallbackAsync writeCallback)
            : base(
                readCallback: (_, _) => throw new NotImplementedException(),
                writeCallback: (_, _) => throw new NotImplementedException()
            )
        {
            ArgumentNullException.ThrowIfNull(readCallback);
            ArgumentNullException.ThrowIfNull(writeCallback);

            State = state;

            _readCallbackAsync = (buffer, cancellationToken) => readCallback.Invoke(buffer, State, cancellationToken);
            _writeCallbackAsync = (buffer, cancellationToken) => writeCallback.Invoke(buffer, State, cancellationToken);
        }

        public virtual void UpdateState(TState newState)
        {
            State = newState;
        }
    }
    public sealed class CallbackStreamWrapper<TInnerStream> : CallbackStream<TInnerStream>
        where TInnerStream : Stream
    {
        private readonly bool _leaveOpen;
        private bool _disposedValue = false;

        public override void UpdateState(TInnerStream newState)
        {
            ArgumentNullException.ThrowIfNull(newState);
            if (newState == State)
                return;
            if (!_leaveOpen)
                State.Dispose();
            State = newState;
        }

        public CallbackStreamWrapper(TInnerStream innerStream, ReadCallback readCallback, WriteCallback writeCallback, bool leaveOpen = false)
           : base(innerStream, readCallback, writeCallback)
        {
            ArgumentNullException.ThrowIfNull(innerStream);
            _leaveOpen = leaveOpen;
        }
        public CallbackStreamWrapper(TInnerStream innerStream, ReadCallbackAsync readCallback, WriteCallbackAsync writeCallback, bool leaveOpen = false)
            : base(innerStream, readCallback, writeCallback)
        {
            ArgumentNullException.ThrowIfNull(innerStream);
            _leaveOpen = leaveOpen;
        }

        #region Stream
        public override bool CanRead => State.CanRead;
        public override bool CanSeek => State.CanSeek;
        public override bool CanWrite => State.CanWrite;
        public override long Length => State.Length;
        public override long Position
        {
            get => State.Position;
            set => State.Position = value;
        }

        public override void Flush() => State.Flush();
        public override long Seek(long offset, SeekOrigin origin) => State.Seek(offset, origin);
        public override void SetLength(long value) => State.SetLength(value);
        #endregion

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (!_leaveOpen)
                        State.Dispose();
                }
                base.Dispose(disposing);
                _disposedValue = true;
            }
        }
        #endregion
    }
}

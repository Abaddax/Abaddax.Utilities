using Abaddax.Utilities.Threading.Tasks;
using static Abaddax.Utilities.IO.ListenStream;

namespace Abaddax.Utilities.IO
{
    /// <summary>
    /// Asynchronous event-based reading from stream
    /// </summary>
    public sealed class ListenStream : SpanStream, IDisposable
    {
        /// <summary>
        /// Depending on success or failure
        /// either <paramref name="readException"/> or <paramref name="message"/> are not null
        /// </summary>
        public delegate Task OnMessageEventHandler(Exception? readException, ReadOnlyMemory<byte> message, CancellationToken token);

        private readonly Stream _innerStream;
        private readonly bool _leaveOpen;
        private readonly byte[] _buffer;
        private CancellationTokenSource? _cancelSource = null;
        private OnMessageEventHandler? _handler = null;
        private bool _disposedValue = false;

        public bool Listening => (!_cancelSource?.IsCancellationRequested) ?? false;

        private async Task AsyncListen()
        {
            try
            {
                try
                {
                    while (!_cancelSource!.IsCancellationRequested)
                    {
                        var read = await _innerStream.ReadAsync(_buffer, _cancelSource.Token);
                        if (read <= 0)
                            throw new EndOfStreamException();
                        await _handler!.Invoke(null, _buffer.AsMemory().Slice(0, read), _cancelSource.Token);
                    }
                }
                finally
                {
                    await _cancelSource!.CancelAsync().IgnoreException();
                }
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is OperationCanceledException)
                    return;//Canceled
                await _handler!.Invoke(ex.InnerException, null, _cancelSource!.Token).IgnoreException();
            }
            catch (OperationCanceledException ex)
            {
                return;//Canceled
            }
            catch (Exception ex)
            {
                await _handler!.Invoke(ex, null, _cancelSource!.Token).IgnoreException();
            }
        }

        public ListenStream(Stream stream, uint maxBufferSize = 65536, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(stream);

            _buffer = new byte[maxBufferSize];
            _innerStream = stream;
            _leaveOpen = leaveOpen;
        }

        public void StartListening(OnMessageEventHandler messageHandler)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            ArgumentNullException.ThrowIfNull(messageHandler);
            if (Listening)
                throw new InvalidOperationException($"{nameof(StartListening)} is not supported while {nameof(Listening)}. Call {nameof(StopListening)} first");

            _handler = messageHandler;

            _cancelSource?.Dispose();
            _cancelSource = new CancellationTokenSource();

            //Run in background
            _ = Task.Run(AsyncListen, _cancelSource.Token);
        }
        public void StopListening()
        {
            _cancelSource?.Cancel();
        }

        #region Stream
        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

        public override int Read(Span<byte> buffer)
        {
            if (Listening)
                throw new InvalidOperationException($"{nameof(Read)} is not supported while {nameof(Listening)}");
            return _innerStream.Read(buffer);
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (Listening)
                throw new InvalidOperationException($"{nameof(Read)} is not supported while {nameof(Listening)}");
            return _innerStream.ReadAsync(buffer, cancellationToken);
        }
        public override void Flush() => _innerStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);
        public override void Write(ReadOnlySpan<byte> buffer) => _innerStream.Write(buffer);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) => _innerStream.WriteAsync(buffer, cancellationToken);
        #endregion

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                StopListening();
                if (!_leaveOpen)
                    _innerStream?.Dispose();
                _cancelSource?.Dispose();
                base.Dispose(disposing);
                _disposedValue = true;
            }
        }
        #endregion
    }

    /// <summary>
    /// Asynchronous event-based reading from stream
    /// </summary>
    public sealed class ListenStream<TProtocol> : SpanStream, IDisposable where TProtocol : IStreamProtocol
    {
        private readonly Stream _innerStream;
        private readonly bool _leaveOpen;
        private readonly TProtocol _protocol;
        private readonly byte[] _buffer;
        private CancellationTokenSource? _cancelSource = null;
        private OnMessageEventHandler? _handler = null;
        private bool _disposedValue = false;

        public bool Listening => (!_cancelSource?.IsCancellationRequested) ?? false;

        private async Task AsyncListen()
        {
            try
            {
                try
                {
                    while (!_cancelSource!.IsCancellationRequested)
                    {
                        await _innerStream.ReadExactlyAsync(_buffer, _cancelSource.Token);
                        var packet = await _protocol.GetPacketBytesAsync(_buffer, _innerStream, _cancelSource.Token);
                        await _handler!.Invoke(null, packet, _cancelSource!.Token);
                    }
                }
                finally
                {
                    await _cancelSource!.CancelAsync().IgnoreException();
                }
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is OperationCanceledException)
                    return;//Canceled
                await _handler!.Invoke(ex.InnerException, null, _cancelSource!.Token).IgnoreException();
            }
            catch (OperationCanceledException ex)
            {
                return;//Canceled
            }
            catch (Exception ex)
            {
                await _handler!.Invoke(ex, null, _cancelSource!.Token).IgnoreException();
            }
        }

        public ListenStream(Stream stream, TProtocol protocol, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(protocol);

            _innerStream = stream;
            _leaveOpen = leaveOpen;

            _protocol = protocol;
            _buffer = new byte[_protocol.FixedHeaderSize];
        }

        public void StartListening(OnMessageEventHandler messageHandler)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            ArgumentNullException.ThrowIfNull(messageHandler);
            if (Listening)
                throw new InvalidOperationException($"{nameof(StartListening)} is not supported while {nameof(Listening)}. Call {nameof(StopListening)} first");

            _handler = messageHandler;

            _cancelSource?.Dispose();
            _cancelSource = new CancellationTokenSource();

            _ = Task.Run(AsyncListen, _cancelSource.Token);
        }
        public void StopListening()
        {
            if (!_disposedValue)
                _cancelSource?.Cancel();
        }

        #region Stream
        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

        public override int Read(Span<byte> buffer)
        {
            if (Listening)
                throw new InvalidOperationException($"{nameof(Read)} is not supported while {nameof(Listening)}");
            return _innerStream.Read(buffer);
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (Listening)
                throw new InvalidOperationException($"{nameof(Read)} is not supported while {nameof(Listening)}");
            return _innerStream.ReadAsync(buffer, cancellationToken);
        }
        public override void Flush() => _innerStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);
        public override void Write(ReadOnlySpan<byte> buffer) => _innerStream.Write(buffer);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) => _innerStream.WriteAsync(buffer, cancellationToken);
        #endregion

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                StopListening();
                if (!_leaveOpen)
                    _innerStream?.Dispose();
                _cancelSource?.Dispose();
                base.Dispose(disposing);
                _disposedValue = true;
            }
        }
        #endregion
    }

}

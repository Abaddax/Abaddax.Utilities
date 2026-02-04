using Abaddax.Utilities.Threading.Tasks;

namespace Abaddax.Utilities.IO
{
    /// <summary>
    /// Depending on success or failure
    /// either <paramref name="readException"/> or <paramref name="message"/> are not null
    /// </summary>
    public delegate Task OnMessageEventHandler(Exception? readException, ReadOnlyMemory<byte> message, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronous event-based reading from stream
    /// </summary>
    public class ListenStream<TProtocol> : SpanStream, IDisposable
        where TProtocol : IStreamProtocol
    {
        private readonly Stream _innerStream;
        private readonly bool _leaveOpen;
        private readonly TProtocol _protocol;
        private readonly byte[] _headerBuffer = new byte[TProtocol.FixedHeaderSize];
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
                        if (_headerBuffer.Length > 0)
                            await _innerStream.ReadExactlyAsync(_headerBuffer, _cancelSource.Token);
                        var packet = await _protocol.GetPacketBytesAsync(_headerBuffer, _innerStream, _cancelSource.Token);
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
                    return; //Canceled
                await _handler!.Invoke(ex.InnerException, null, _cancelSource!.Token).IgnoreException();
            }
            catch (OperationCanceledException)
            {
                return; //Canceled
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
        public override bool CanRead => _innerStream.CanRead && !Listening;
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
                if (disposing)
                {
                    StopListening();
                    if (!_leaveOpen)
                        _innerStream?.Dispose();
                    _cancelSource?.Dispose();
                }
                base.Dispose(disposing);
                _disposedValue = true;
            }
        }
        #endregion
    }

    /// <summary>
    /// Asynchronous event-based reading from stream
    /// </summary>
    public sealed class ListenStream : ListenStream<RawStreamProtocol>
    {
        public ListenStream(Stream stream, uint maxBufferSize = RawStreamProtocol.DefaultBufferSize, bool leaveOpen = false)
            : base(stream, new RawStreamProtocol(maxBufferSize), leaveOpen) { }
    }

}

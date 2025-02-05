using Abaddax.Utilities.Threading.Tasks;
using static Abaddax.Utilities.IO.ListenStream;

namespace Abaddax.Utilities.IO
{
    /// <summary>
    /// Asynchronous event-based reading from stream
    /// </summary>
    public sealed class ListenStream : Stream, IDisposable
    {
        /// <summary>
        /// Depending on success or failure
        /// either <paramref name="readException"/> or <paramref name="message"/> are not null
        /// </summary>
        public delegate Task OnMessageEventHandler(Exception? readException, ReadOnlyMemory<byte> message, CancellationToken token);

        private readonly Stream _workStream;
        private readonly byte[] _buffer;
        private readonly ThreadSafeDispose _disposedValue = new();
        private CancellationTokenSource? _cancelSource = null;
        private OnMessageEventHandler? _handler = null;

        public bool Listening => (!_cancelSource?.IsCancellationRequested) ?? false;

        private async Task AsyncListen()
        {
            try
            {
                try
                {
                    while (!_cancelSource!.IsCancellationRequested)
                    {
                        var read = await _workStream.ReadAsync(_buffer, 0, _buffer.Length, _cancelSource.Token);
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

        public ListenStream(Stream stream, uint maxBufferSize = 65536)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            _buffer = new byte[maxBufferSize];
            _workStream = Stream.Synchronized(stream);
        }

        public void StartListening(OnMessageEventHandler messageHandler)
        {
            if (_disposedValue.IsDisposed)
                throw new ObjectDisposedException(this.GetType().FullName);
            if (messageHandler == null)
                throw new ArgumentNullException(nameof(messageHandler));
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
        public override bool CanRead => _workStream.CanRead;
        public override bool CanSeek => _workStream.CanSeek;
        public override bool CanWrite => _workStream.CanWrite;
        public override long Length => _workStream.Length;
        public override long Position { get => _workStream.Position; set => _workStream.Position = value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Listening)
                throw new InvalidOperationException($"{nameof(Read)} is not supported while {nameof(Listening)}");
            return _workStream.Read(buffer, offset, count);
        }
        public override void Flush() => _workStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _workStream.Seek(offset, origin);
        public override void SetLength(long value) => _workStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _workStream.Write(buffer, offset, count);
        #endregion

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (_disposedValue.TryDispose())
            {
                StopListening();
                _workStream?.Dispose();
            }
        }
        #endregion
    }

    /// <summary>
    /// Asynchronous event-based reading from stream
    /// </summary>
    public sealed class ListenStream<TProtocol> : Stream, IDisposable where TProtocol : IStreamProtocol
    {
        private readonly Stream _workStream;
        private readonly TProtocol _protocol;
        private readonly byte[] _buffer;
        private readonly ThreadSafeDispose _disposedValue = new();
        private int _bufferOffset = 0;
        private CancellationTokenSource? _cancelSource = null;
        private OnMessageEventHandler? _handler = null;

        public bool Listening => (!_cancelSource?.IsCancellationRequested) ?? false;

        private async Task AsyncListen()
        {
            try
            {
                try
                {
                    while (!_cancelSource!.IsCancellationRequested)
                    {
                        await _workStream.ReadExactlyAsync(_buffer, _cancelSource.Token);
                        var packet = await _protocol.GetPacketBytesAsync(_buffer, _workStream, _cancelSource.Token);
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

        public ListenStream(Stream stream, TProtocol protocol)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (protocol == null)
                throw new ArgumentNullException(nameof(protocol));

            _workStream = Stream.Synchronized(stream);

            _protocol = protocol;
            _buffer = new byte[_protocol.FixedHeaderSize];
        }

        public void StartListening(OnMessageEventHandler messageHandler)
        {
            if (_disposedValue.IsDisposed)
                throw new ObjectDisposedException(this.GetType().FullName);
            if (messageHandler == null)
                throw new ArgumentNullException(nameof(messageHandler));
            if (Listening)
                throw new InvalidOperationException($"{nameof(StartListening)} is not supported while {nameof(Listening)}. Call {nameof(StopListening)} first");

            _handler = messageHandler;
            _bufferOffset = 0;

            _cancelSource?.Dispose();
            _cancelSource = new CancellationTokenSource();

            _ = Task.Run(AsyncListen, _cancelSource.Token);
        }
        public void StopListening()
        {
            _cancelSource?.Cancel();
        }

        #region Stream
        public override bool CanRead => _workStream.CanRead;
        public override bool CanSeek => _workStream.CanSeek;
        public override bool CanWrite => _workStream.CanWrite;
        public override long Length => _workStream.Length;
        public override long Position { get => _workStream.Position; set => _workStream.Position = value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Listening)
                throw new InvalidOperationException($"{nameof(Read)} is not supported while {nameof(Listening)}");
            return _workStream.Read(buffer, offset, count);
        }
        public override void Flush() => _workStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _workStream.Seek(offset, origin);
        public override void SetLength(long value) => _workStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _workStream.Write(buffer, offset, count);
        #endregion

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (_disposedValue.TryDispose())
            {
                StopListening();
                _workStream?.Dispose();
            }
        }
        #endregion
    }

}

using Abaddax.Utilities.IO;
using Abaddax.Utilities.Threading.Tasks;

namespace Abaddax.Utilities.Network
{
    /// <summary>
    /// Forwards messages from one stream to the other and vice versa
    /// </summary>
    public sealed class StreamProxy : IProxy, IDisposable
    {
        private readonly ListenStream _stream1;
        private readonly ListenStream _stream2;
        private readonly ThreadSafeDispose _disposedValue = new();

        public bool Active => _stream1.Listening || _stream2.Listening;

        private async Task Stream1Handler(Exception? readException, ReadOnlyMemory<byte> message, CancellationToken token)
        {
            if (readException != null)
            {
                _stream1.StopListening();
                _stream2.StopListening();
                return;
            }
            await _stream2.WriteAsync(message, token);
        }
        private async Task Stream2HHandler(Exception? readException, ReadOnlyMemory<byte> message, CancellationToken token)
        {
            if (readException != null)
            {
                _stream1.StopListening();
                _stream2.StopListening();
                return;
            }
            await _stream1.WriteAsync(message, token);
        }

        public StreamProxy(Stream stream1, Stream stream2)
        {
            _stream1 = new ListenStream(stream1);
            _stream2 = new ListenStream(stream2);
        }

        #region IProxy
        public void Tunnel(CancellationToken token = default)
        {
            TunnelAsync(token).ExecuteSynchronously();
        }
        public async Task TunnelAsync(CancellationToken token = default)
        {
            if (_disposedValue.IsDisposed)
                throw new ObjectDisposedException(this.GetType().FullName);

            _stream1.StartListening(Stream1Handler);
            _stream2.StartListening(Stream2HHandler);

            while (!token.IsCancellationRequested && Active)
            {
                await Task.Delay(100);
            }

            _stream1.StopListening();
            _stream2.StopListening();
        }
        #endregion

        #region IDisposable
        private void Dispose(bool disposing)
        {
            if (_disposedValue.TryDispose())
            {
                _stream1?.Dispose();
                _stream2?.Dispose();
            }
        }
        ~StreamProxy()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    /// <summary>
    /// Forwards messages from one stream to the other and vice versa
    /// </summary>
    public sealed class StreamProxy<TProtocol> : IProxy, IDisposable where TProtocol : IStreamProtocol, new()
    {
        private readonly ListenStream<TProtocol> _stream1;
        private readonly ListenStream<TProtocol> _stream2;
        private ThreadSafeDispose _disposedValue = new();

        public bool Active => _stream1.Listening || _stream2.Listening;

        private async Task Stream1Handler(Exception? readException, ReadOnlyMemory<byte> message, CancellationToken token)
        {
            if (readException != null)
            {
                _stream1.StopListening();
                _stream2.StopListening();
                return;
            }
            await _stream2.WriteAsync(message, token);
        }
        private async Task Stream2HHandler(Exception? readException, ReadOnlyMemory<byte> message, CancellationToken token)
        {
            if (readException != null)
            {
                _stream1.StopListening();
                _stream2.StopListening();
                return;
            }
            await _stream1.WriteAsync(message, token);
        }

        public StreamProxy(Stream stream1, Stream stream2)
        {
            _stream1 = new ListenStream<TProtocol>(stream1, new TProtocol());
            _stream2 = new ListenStream<TProtocol>(stream2, new TProtocol());
        }

        #region IProxy
        public void Tunnel(CancellationToken token = default)
        {
            TunnelAsync(token).ExecuteSynchronously();
        }
        public async Task TunnelAsync(CancellationToken token = default)
        {
            if (_disposedValue.IsDisposed)
                throw new ObjectDisposedException(this.GetType().FullName);

            _stream1.StartListening(Stream1Handler);
            _stream2.StartListening(Stream2HHandler);

            while (!token.IsCancellationRequested && Active)
            {
                await Task.Delay(100);
            }

            _stream1.StopListening();
            _stream2.StopListening();
        }
        #endregion       

        #region IDisposable
        private void Dispose(bool disposing)
        {
            if (_disposedValue.TryDispose())
            {
                _stream1?.Dispose();
                _stream2?.Dispose();
            }
        }
        ~StreamProxy()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

}

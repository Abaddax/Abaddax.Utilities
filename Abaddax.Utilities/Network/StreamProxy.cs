using Abaddax.Utilities.IO;
using Abaddax.Utilities.Threading.Tasks;

namespace Abaddax.Utilities.Network
{
    /// <summary>
    /// Forwards messages from one stream to the other and vice versa
    /// </summary>
    public class StreamProxy<TProtocol> : IProxy, IDisposable
        where TProtocol : IStreamProtocol, new()
    {
        private readonly ListenStream<TProtocol> _stream1;
        private readonly ListenStream<TProtocol> _stream2;
        private bool _disposedValue = false;

        public bool Active => _stream1.Listening || _stream2.Listening;

        private async Task Stream1Handler(Exception? readException, ReadOnlyMemory<byte> message, CancellationToken cancellationToken)
        {
            if (readException != null)
            {
                _stream1.StopListening();
                _stream2.StopListening();
                return;
            }
            await _stream2.WriteAsync(message, cancellationToken);
        }
        private async Task Stream2HHandler(Exception? readException, ReadOnlyMemory<byte> message, CancellationToken cancellationToken)
        {
            if (readException != null)
            {
                _stream1.StopListening();
                _stream2.StopListening();
                return;
            }
            await _stream1.WriteAsync(message, cancellationToken);
        }

        public StreamProxy(Stream stream1, Stream stream2, bool leaveStream1Open = false, bool leaveStream2Open = false)
        {
            _stream1 = new ListenStream<TProtocol>(stream1, new TProtocol(), leaveOpen: leaveStream1Open);
            _stream2 = new ListenStream<TProtocol>(stream2, new TProtocol(), leaveOpen: leaveStream2Open);
        }

        #region IProxy
        public void Tunnel(CancellationToken cancellationToken = default)
        {
            TunnelAsync(cancellationToken).AwaitSync();
        }
        public async Task TunnelAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            _stream1.StartListening(Stream1Handler);
            _stream2.StartListening(Stream2HHandler);

            while (!cancellationToken.IsCancellationRequested && Active)
            {
                await Task.Delay(100, cancellationToken).IgnoreException();
            }

            _stream1.StopListening();
            _stream2.StopListening();
        }
        #endregion       

        #region IDisposable
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _stream1?.Dispose();
                    _stream2?.Dispose();
                }
                _disposedValue = true;
            }
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
    public sealed class StreamProxy : StreamProxy<RawStreamProtocol>, IProxy, IDisposable
    {
        public StreamProxy(Stream stream1, Stream stream2, bool leaveStream1Open = false, bool leaveStream2Open = false)
            : base(stream1, stream2, leaveStream1Open, leaveStream2Open) { }
    }
}

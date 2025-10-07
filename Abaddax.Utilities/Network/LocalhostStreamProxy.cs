using Abaddax.Utilities.IO;
using Abaddax.Utilities.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Abaddax.Utilities.Network
{
    /// <summary>
    /// Forwards messages from one stream to the other and vice versa
    /// </summary>
    /// <remarks>All messages sent are transparently via loopback</remarks>
    public sealed class LocalhostStreamProxy : IProxy, IDisposable
    {
        private readonly int _localHostPort;
        private readonly TcpClient _client1;
        private readonly TcpClient _client2;
        private readonly StreamProxy _proxy1;
        private readonly StreamProxy _proxy2;

        private bool _disposedValue = false;

        public bool Active => _proxy1.Active && _proxy2.Active;
        public bool Connected => _client1.Connected && _client2.Connected;
        public int LoopbackPort => _localHostPort;

        public LocalhostStreamProxy(Stream stream1, Stream stream2, int port)
        {
            using (TcpListener listener = new TcpListener(IPAddress.Loopback, port))
            {
                try
                {
                    listener.Start();
                    _client1 = new TcpClient();

                    var clientCon = _client1.ConnectAsync((IPEndPoint)listener.LocalEndpoint);
                    var serverAcc = listener.AcceptTcpClientAsync();

                    serverAcc.AwaitSync();
                    clientCon.AwaitSync();

                    _client2 = serverAcc.Result;

                    _proxy1 = new StreamProxy(stream1, _client1.GetStream());
                    _proxy2 = new StreamProxy(stream2, _client2.GetStream());

                    Console.WriteLine($"{((IPEndPoint)_client1.Client.LocalEndPoint!).Port} <-> {((IPEndPoint)_client2.Client.LocalEndPoint!).Port}");
                }
                finally
                {
                    _localHostPort = (listener.LocalEndpoint as IPEndPoint)?.Port ?? 0;
                }
            }
        }

        #region IProxy
        public void Tunnel(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            TunnelAsync(cancellationToken).AwaitSync();
        }
        public async Task TunnelAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            using (var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var task1 = _proxy1.TunnelAsync(tokenSource.Token);
                var task2 = _proxy2.TunnelAsync(tokenSource.Token);

                while (!cancellationToken.IsCancellationRequested && Active)
                {
                    await Task.Delay(100, cancellationToken).IgnoreException();
                }
                await tokenSource.CancelAsync();

                await task1;
                await task2;
            }
        }
        #endregion

        #region IDisposable
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _proxy1?.Dispose();
                    _proxy2?.Dispose();
                    _client1?.Dispose();
                    _client2?.Dispose();
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
    /// <remarks>All messages sent are transparently via loopback</remarks>
    public sealed class LocalhostStreamProxy<TProtocol> : IProxy, IDisposable where TProtocol : IStreamProtocol, new()
    {
        private readonly int _localHostPort;
        private readonly TcpClient _client1;
        private readonly TcpClient _client2;
        private readonly StreamProxy<TProtocol> _proxy1;
        private readonly StreamProxy<TProtocol> _proxy2;
        private bool _disposedValue = false;

        public bool Active => _proxy1.Active && _proxy2.Active;
        public bool Connected => _client1.Connected && _client2.Connected;
        public int LoopbackPort => _localHostPort;

        public LocalhostStreamProxy(Stream stream1, Stream stream2, int port)
        {
            using (TcpListener listener = new TcpListener(IPAddress.Loopback, port))
            {
                try
                {
                    listener.Start();
                    _client1 = new TcpClient();

                    var clientCon = _client1.ConnectAsync((IPEndPoint)listener.LocalEndpoint);
                    var serverAcc = listener.AcceptTcpClientAsync();

                    serverAcc.AwaitSync();
                    clientCon.AwaitSync();

                    _client2 = serverAcc.Result;

                    _proxy1 = new StreamProxy<TProtocol>(stream1, _client1.GetStream());
                    _proxy2 = new StreamProxy<TProtocol>(stream2, _client2.GetStream());

                    Console.WriteLine($"{((IPEndPoint)_client1.Client.LocalEndPoint!).Port} <-> {((IPEndPoint)_client2.Client.LocalEndPoint!).Port}");
                }
                finally
                {
                    _localHostPort = (listener.LocalEndpoint as IPEndPoint)?.Port ?? 0;
                }
            }
        }

        #region IProxy
        public void Tunnel(CancellationToken cancellationToken = default)
        {
            TunnelAsync(cancellationToken).AwaitSync();
        }
        public async Task TunnelAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            using (var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var task1 = _proxy1.TunnelAsync(tokenSource.Token);
                var task2 = _proxy2.TunnelAsync(tokenSource.Token);

                while (!cancellationToken.IsCancellationRequested && Active)
                {
                    await Task.Delay(100, cancellationToken).IgnoreException();
                }
                await tokenSource.CancelAsync();

                await task1;
                await task2;
            }
        }
        #endregion

        #region IDisposable 
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _proxy1?.Dispose();
                    _proxy2?.Dispose();
                    _client1?.Dispose();
                    _client2?.Dispose();
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

}

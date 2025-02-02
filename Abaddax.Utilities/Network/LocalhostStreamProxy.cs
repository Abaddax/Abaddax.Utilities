using Abaddax.Utilities.IO;
using System.Net;
using System.Net.Sockets;

namespace Abaddax.Utilities.Network
{
    /// <summary>
    /// Forwards messages from one stream to the other and vice versa
    /// </summary>
    /// <remarks>All messages sent are transparently via loopback</remarks>
    public class LocalhostStreamProxy : IProxy, IDisposable
    {
        readonly int _localHostPort;
        readonly TcpClient _client1;
        readonly TcpClient _client2;

        readonly StreamProxy _proxy1;
        readonly StreamProxy _proxy2;
        private bool disposedValue;

        public bool Active => _proxy1.Active && _proxy2.Active;
        public int LoopbackPort => _localHostPort;

        public LocalhostStreamProxy(Stream stream1, Stream stream2, int port)
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, port);
            try
            {
                listener.Start();
                _client1 = new TcpClient();
                var clientCon = _client1.ConnectAsync((IPEndPoint)listener.LocalEndpoint);
                var serverAcc = listener.AcceptTcpClientAsync();

                clientCon.Wait();
                _client2 = serverAcc.Result;

                _proxy1 = new StreamProxy(stream1, _client1.GetStream());
                _proxy2 = new StreamProxy(stream2, _client2.GetStream());

                Console.WriteLine($"{((IPEndPoint)_client1.Client.LocalEndPoint!).Port} <-> {((IPEndPoint)_client2.Client.LocalEndPoint!).Port}");
            }
            finally
            {
                _localHostPort = (listener.LocalEndpoint as IPEndPoint)?.Port ?? 0;
                listener.Stop();
            }
        }

        #region IProxy
        public void Tunnel(CancellationToken token = default)
        {
            TunnelAsync(token).Wait();
        }
        public async Task TunnelAsync(CancellationToken token = default)
        {
            using (var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                var task1 = _proxy1.TunnelAsync(tokenSource.Token);
                var task2 = _proxy2.TunnelAsync(tokenSource.Token);

                while (!token.IsCancellationRequested && Active)
                {
                    await Task.Delay(100);
                }
                tokenSource.Cancel();

                await task1;
                await task2;
            }
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _proxy1?.Dispose();
                _proxy2?.Dispose();
                _client1?.Dispose();
                _client2?.Dispose();
                disposedValue = true;
            }
        }
        ~LocalhostStreamProxy()
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
    /// <remarks>All messages sent are transparently via loopback</remarks>
    public class LocalhostStreamProxy<TProtocol> : IProxy, IDisposable where TProtocol : IStreamProtocol, new()
    {
        readonly int _localHostPort;
        readonly TcpClient _client1;
        readonly TcpClient _client2;

        readonly StreamProxy<TProtocol> _proxy1;
        readonly StreamProxy<TProtocol> _proxy2;
        private bool disposedValue;

        public bool Active => _proxy1.Active && _proxy2.Active;
        public int LoopbackPort => _localHostPort;

        public LocalhostStreamProxy(Stream stream1, Stream stream2, int port)
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, port);
            try
            {
                listener.Start();
                _client1 = new TcpClient();
                var clientCon = _client1.ConnectAsync((IPEndPoint)listener.LocalEndpoint);
                var serverAcc = listener.AcceptTcpClientAsync();

                clientCon.Wait();
                _client2 = serverAcc.Result;

                _proxy1 = new StreamProxy<TProtocol>(stream1, _client1.GetStream());
                _proxy2 = new StreamProxy<TProtocol>(stream2, _client2.GetStream());

                Console.WriteLine($"{((IPEndPoint)_client1.Client.LocalEndPoint!).Port} <-> {((IPEndPoint)_client2.Client.LocalEndPoint!).Port}");
            }
            finally
            {
                _localHostPort = (listener.LocalEndpoint as IPEndPoint)?.Port ?? 0;
                listener.Stop();
            }
        }

        #region IProxy
        public void Tunnel(CancellationToken token = default)
        {
            TunnelAsync(token).Wait();
        }
        public async Task TunnelAsync(CancellationToken token = default)
        {
            using (var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                var task1 = _proxy1.TunnelAsync(tokenSource.Token);
                var task2 = _proxy2.TunnelAsync(tokenSource.Token);

                while (!token.IsCancellationRequested && Active)
                {
                    await Task.Delay(100);
                }
                tokenSource.Cancel();

                await task1;
                await task2;
            }
        }
        #endregion

        #region IDisposable 
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _proxy1?.Dispose();
                _proxy2?.Dispose();
                _client1?.Dispose();
                _client2?.Dispose();
                disposedValue = true;
            }
        }
        ~LocalhostStreamProxy()
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

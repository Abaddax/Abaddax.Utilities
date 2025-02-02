using System.Net.Sockets;

namespace Abaddax.Utilities.Network
{
    /// <summary>
    /// Forwards messages from one socket to the other and vice versa
    /// </summary>
    public class SocketProxy : IProxy, IDisposable
    {
        ListenSocket socket1;
        ListenSocket socket2;
        private bool disposedValue;

        public bool Active => socket1.Listening || socket2.Listening;

        private void Socket1Handler(Exception? readException, byte[]? message)
        {
            if (readException != null)
            {
                Console.WriteLine($"Stopping. {readException}");
                socket1.StopReceiving();
                socket2.StopReceiving();
                return;
            }
            Console.WriteLine($"Sending {message!.Length} Bytes to {socket2.Socket.RemoteEndPoint}");
            socket2.Socket.Send(message);
        }
        private void Socket2Handler(Exception? readException, byte[]? message)
        {
            if (readException != null)
            {
                Console.WriteLine($"Stopping. {readException}");
                socket1.StopReceiving();
                socket2.StopReceiving();
                return;
            }
            Console.WriteLine($"Sending {message!.Length} Bytes to {socket1.Socket.RemoteEndPoint}");
            socket1.Socket.Send(message);
        }

        public SocketProxy(Socket socket1, Socket socket2)
        {
            this.socket1 = new ListenSocket(socket1);
            this.socket2 = new ListenSocket(socket2);
        }

        #region IProxy
        public void Tunnel(CancellationToken token = default)
        {
            TunnelAsync(token).Wait();
        }
        public async Task TunnelAsync(CancellationToken token = default)
        {
            socket1.StartReceiving(Socket1Handler);
            socket2.StartReceiving(Socket2Handler);

            while (!token.IsCancellationRequested && Active)
            {
                await Task.Delay(100);
            }

            socket1.StopReceiving();
            socket2.StopReceiving();
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                socket1?.Dispose();
                socket2?.Dispose();
                disposedValue = true;
            }
        }
        ~SocketProxy()
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

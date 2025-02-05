using System.Net.Sockets;
using System.Net;

namespace Abaddax.Utilities.Network
{
    public class PollingTcpClient : TcpClient, IDisposable
    {
        private readonly Timer _timer;
        private readonly ThreadSafeDispose _disposedValue = new();

        public TimeSpan PollingRate { get; }

        void PollConnectionCallback(object? state)
        {
            if (_disposedValue.IsDisposed)
                return;

            if (Client?.Connected != true)
                return;

            //Poll client
            try
            {
                //Check if client is still connected
                if (Client.Poll(0, SelectMode.SelectRead))
                {
                    Span<byte> buff = stackalloc byte[1];
                    if (Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        //Client is diconnected
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                //Client is diconnected
                Close();
            }
        }

        public PollingTcpClient(TimeSpan? pollingRate = null)
            : base()
        {
            PollingRate = pollingRate ?? TimeSpan.FromSeconds(5);
            _timer = new Timer(PollConnectionCallback, null, TimeSpan.Zero, PollingRate);           
        }
        public PollingTcpClient(AddressFamily family, TimeSpan? pollingRate = null)
            : base(family)
        {
            PollingRate = pollingRate ?? TimeSpan.FromSeconds(5);
            _timer = new Timer(PollConnectionCallback, null, TimeSpan.Zero, PollingRate);
        }
        public PollingTcpClient(IPEndPoint localEP, TimeSpan? pollingRate = null)
            : base(localEP)
        {
            PollingRate = pollingRate ?? TimeSpan.FromSeconds(5);
            _timer = new Timer(PollConnectionCallback, null, TimeSpan.Zero, PollingRate);
        }
        public PollingTcpClient(string hostname, int port, TimeSpan? pollingRate = null)
            : base(hostname, port)
        {
            PollingRate = pollingRate ?? TimeSpan.FromSeconds(5);
            _timer = new Timer(PollConnectionCallback, null, TimeSpan.Zero, PollingRate);
        }
        public PollingTcpClient(TcpClient client, TimeSpan? pollingRate = null)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            Client = client.Client;
            Active = client.Connected;

            PollingRate = pollingRate ?? TimeSpan.FromSeconds(5);
            _timer = new Timer(PollConnectionCallback, null, TimeSpan.Zero, PollingRate);
        }

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (_disposedValue.TryDispose())
            {
                _timer.Dispose();
                base.Dispose(disposing);
            }
        }
        ~PollingTcpClient()
        {
            Dispose(false);
        }
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

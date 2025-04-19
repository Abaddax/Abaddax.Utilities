using System.Net;
using System.Net.Sockets;

namespace Abaddax.Utilities.Network
{
    public class PollingTcpClient : TcpClient, IDisposable
    {
        public static readonly TimeSpan DefaultPollingRate = TimeSpan.FromSeconds(5);

        private readonly Timer _timer;
        private bool _disposedValue = false;

        public TimeSpan PollingRate { get; }

        void PollConnectionCallback(object? state)
        {
            if (_disposedValue)
                return;

            if (Client?.Connected != true)
                return;

            Client.IsConnected(closeDisconnectedSocket: true);
        }

        public PollingTcpClient(TimeSpan? pollingRate = null)
            : base()
        {
            PollingRate = pollingRate ?? DefaultPollingRate;
            _timer = new Timer(PollConnectionCallback, null, TimeSpan.Zero, PollingRate);
        }
        public PollingTcpClient(AddressFamily family, TimeSpan? pollingRate = null)
            : base(family)
        {
            PollingRate = pollingRate ?? DefaultPollingRate;
            _timer = new Timer(PollConnectionCallback, null, TimeSpan.Zero, PollingRate);
        }
        public PollingTcpClient(IPEndPoint localEP, TimeSpan? pollingRate = null)
            : base(localEP)
        {
            PollingRate = pollingRate ?? DefaultPollingRate;
            _timer = new Timer(PollConnectionCallback, null, TimeSpan.Zero, PollingRate);
        }
        public PollingTcpClient(string hostname, int port, TimeSpan? pollingRate = null)
            : base(hostname, port)
        {
            PollingRate = pollingRate ?? DefaultPollingRate;
            _timer = new Timer(PollConnectionCallback, null, TimeSpan.Zero, PollingRate);
        }
        public PollingTcpClient(TcpClient client, TimeSpan? pollingRate = null)
        {
            ArgumentNullException.ThrowIfNull(client);

            Client = client.Client;
            Active = client.Connected;

            PollingRate = pollingRate ?? DefaultPollingRate;
            _timer = new Timer(PollConnectionCallback, null, TimeSpan.Zero, PollingRate);
        }

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _timer.Dispose();
                base.Dispose(disposing);
                _disposedValue = true;
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

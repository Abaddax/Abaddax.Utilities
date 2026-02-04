using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Network
{
    public static class TcpListenerExtensions
    {

        public static readonly TimeSpan DefaultPollingRate = TimeSpan.FromMicroseconds(10);

        public delegate Task ClientConnectedCallback(TcpClient newClient, CancellationToken cancellationToken);


        public static async Task AcceptTcpClientsAsync(this TcpListener listener, ClientConnectedCallback clientConnectedCallback, TimeSpan? pollingInterval = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(listener);
            ArgumentNullException.ThrowIfNull(clientConnectedCallback);

            await foreach (var client in listener.AcceptTcpClientsAsync(pollingInterval, cancellationToken))
            {
                await clientConnectedCallback.Invoke(client, cancellationToken);
            }
        }
        public static async IAsyncEnumerable<TcpClient> AcceptTcpClientsAsync(this TcpListener listener, TimeSpan? pollingInterval = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            pollingInterval ??= TimeSpan.FromMilliseconds(50);
            while (!cancellationToken.IsCancellationRequested && listener.Server.IsBound)
            {
                if (!listener.Pending())
                {
                    await Task.Delay(pollingInterval.Value, cancellationToken);
                    continue;
                }
                yield return await listener.AcceptTcpClientAsync(cancellationToken);
            }
        }
    }
}

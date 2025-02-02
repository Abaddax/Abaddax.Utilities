using System.Net.Sockets;

namespace Abaddax.Utilities.Network
{
    public class ListenSocket : IDisposable
    {
        /// <summary>
        /// Depending on success or failure
        /// either <paramref name="readException"/> or <paramref name="message"/> are not null
        /// </summary>
        public delegate void OnMessageEventHandler(Exception? receiveException, byte[]? message);

        private readonly Socket workSocket;
        private readonly byte[] buffer;
        private CancellationTokenSource? cancelSource = null;
        private OnMessageEventHandler? handler = null;
        private int taskTrace = 0;
        private bool disposedValue;

        public bool Listening => (!cancelSource?.IsCancellationRequested) ?? false;
        public Socket Socket => workSocket;

        private static Exception? RunSafe(Action function)
        {
            try
            {
                function.Invoke();
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        private async void AsyncReceive()
        {

            Interlocked.Increment(ref taskTrace);
            try
            {
                while (!cancelSource!.IsCancellationRequested)
                {
                    int read = await workSocket.ReceiveAsync(buffer, SocketFlags.None, cancelSource.Token);
                    if (read < 0)
                        continue;
                    //throw new IOException("Socket closed");
                    var message = buffer[0..read];
                    handler!.Invoke(null, message);
                }
            }
            catch (AggregateException ex)
            {
                RunSafe(() => cancelSource!.Cancel());
                RunSafe(() =>
                {
                    if (ex.InnerExceptions.Count == 0)
                        handler!.Invoke(new Exception("Unknown", null), null);
                    else if (ex.InnerExceptions.FirstOrDefault(match => match is OperationCanceledException) as OperationCanceledException != null)
                        return;
                    else if (ex.InnerExceptions[0] is IOException || ex.InnerExceptions[0] is NotSupportedException || ex.InnerExceptions[0] is ObjectDisposedException)
                        handler!.Invoke(ex.InnerExceptions[0], null);
                    else
                        handler!.Invoke(new Exception("Unknown", ex.InnerExceptions[0]), null);
                    return;
                });
            }
            catch (TaskCanceledException)
            {
                //StopRead
                RunSafe(() => cancelSource!.Cancel());
            }
            catch (Exception ex)
            {
                RunSafe(() => cancelSource!.Cancel());
                RunSafe(() => handler!.Invoke(ex, null));
            }
            finally
            {
                Interlocked.Decrement(ref taskTrace);
            }
        }

        public ListenSocket(Socket socket)
        {
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));

            workSocket = socket;

            buffer = new byte[workSocket.ReceiveBufferSize];
        }

        public void StartReceiving(OnMessageEventHandler messageHandler)
        {
            if (messageHandler == null)
                throw new ArgumentNullException(nameof(messageHandler));
            if (Listening)
                throw new InvalidOperationException($"{nameof(StartReceiving)} is not supported while {nameof(Listening)}. Call {nameof(StopReceiving)} first");

            handler = messageHandler;

            cancelSource?.Dispose();
            cancelSource = new CancellationTokenSource();

            AsyncReceive();
        }
        public void StopReceiving()
        {
            cancelSource?.Cancel();
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                StopReceiving();
                workSocket?.Dispose();
                disposedValue = true;
            }
        }
        ~ListenSocket()
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

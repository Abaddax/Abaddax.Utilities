using Abaddax.Utilities.IO;

namespace Abaddax.Utilities.Network
{
    /// <summary>
    /// Forwards messages from one stream to the other and vice versa
    /// </summary>
    public class StreamProxy : IProxy, IDisposable
    {
        readonly ListenStream stream1;
        readonly ListenStream stream2;
        private bool disposedValue;

        public bool Active => stream1.Listening || stream2.Listening;

        private async Task Stream1Handler(Exception? readException, Memory<byte> message, CancellationToken token)
        {
            if (readException != null)
            {
                stream1.StopListening();
                stream2.StopListening();
                return;
            }
            Console.WriteLine($"Sending {message!.Length} Bytes to stream2");
            await stream2.WriteAsync(message, token);
        }
        private async Task Stream2HHandler(Exception? readException, Memory<byte> message, CancellationToken token)
        {
            if (readException != null)
            {
                stream1.StopListening();
                stream2.StopListening();
                return;
            }
            Console.WriteLine($"Sending {message!.Length} Bytes to stream1");
            await stream1.WriteAsync(message, token);
        }

        public StreamProxy(Stream stream1, Stream stream2)
        {
            this.stream1 = new ListenStream(stream1);
            this.stream2 = new ListenStream(stream2);
        }

        #region IProxy
        public void Tunnel(CancellationToken token = default)
        {
            TunnelAsync(token).Wait();
        }
        public async Task TunnelAsync(CancellationToken token = default)
        {
            stream1.StartListening(Stream1Handler);
            stream2.StartListening(Stream2HHandler);

            while (!token.IsCancellationRequested && Active)
            {
                await Task.Delay(100);
            }

            stream1.StopListening();
            stream2.StopListening();
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                stream1?.Dispose();
                stream2?.Dispose();
                disposedValue = true;
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
    public class StreamProxy<TProtocol> : IProxy, IDisposable where TProtocol : IStreamProtocol, new()
    {
        readonly ListenStream<TProtocol> stream1;
        readonly ListenStream<TProtocol> stream2;
        private bool disposedValue;

        public bool Active => stream1.Listening || stream2.Listening;

        private async Task Stream1Handler(Exception? readException, Memory<byte> message, CancellationToken token)
        {
            if (readException != null)
            {
                stream1.StopListening();
                stream2.StopListening();
                return;
            }
            Console.WriteLine($"Sending {message!.Length} Bytes to stream2");
            await stream2.WriteAsync(message, token);
        }
        private async Task Stream2HHandler(Exception? readException, Memory<byte> message, CancellationToken token)
        {
            if (readException != null)
            {
                stream1.StopListening();
                stream2.StopListening();
                return;
            }
            Console.WriteLine($"Sending {message!.Length} Bytes to stream1");
            await stream1.WriteAsync(message, token);
        }

        public StreamProxy(Stream stream1, Stream stream2)
        {
            this.stream1 = new ListenStream<TProtocol>(stream1, new TProtocol());
            this.stream2 = new ListenStream<TProtocol>(stream2, new TProtocol());
        }

        #region IProxy
        public void Tunnel(CancellationToken token = default)
        {
            TunnelAsync(token).Wait();
        }
        public async Task TunnelAsync(CancellationToken token = default)
        {
            stream1.StartListening(Stream1Handler);
            stream2.StartListening(Stream2HHandler);

            while (!token.IsCancellationRequested && Active)
            {
                await Task.Delay(100);
            }

            stream1.StopListening();
            stream2.StopListening();
        }
        #endregion       

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                stream1?.Dispose();
                stream2?.Dispose();
                disposedValue = true;
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

using Abaddax.Utilities.Threading.Tasks;
using static Abaddax.Utilities.IO.ListenStream;

namespace Abaddax.Utilities.IO
{
    /// <summary>
    /// Asynchronous event-based reading from stream
    /// </summary>
    public class ListenStream : Stream, IDisposable
    {
        /// <summary>
        /// Depending on success or failure
        /// either <paramref name="readException"/> or <paramref name="message"/> are not null
        /// </summary>
        public delegate Task OnMessageEventHandler(Exception? readException, Memory<byte> message, CancellationToken token);

        private readonly Stream workStream;
        private readonly byte[] buffer;
        private CancellationTokenSource? cancelSource = null;
        private OnMessageEventHandler? handler = null;
        private int taskTrace = 0;
        private bool disposedValue = false;

        public bool Listening => (!cancelSource?.IsCancellationRequested) ?? false;

        private static Exception? RunSafe(Action function)
        {
            function.InvokeSafe(out var ex);
            return ex;
        }
        private async Task AsyncListen()
        {
            Interlocked.Increment(ref taskTrace);
            try
            {
                while (!cancelSource!.IsCancellationRequested)
                {
                    var read = await workStream.ReadAsync(buffer, 0, buffer.Length, cancelSource.Token);
                    if (read <= 0)
                        throw new IOException("End of stream reached");
                    await handler!.Invoke(null, buffer[0..read], cancelSource!.Token);
                }
            }
            catch (AggregateException ex)
            {
                RunSafe(() => cancelSource!.Cancel());
                RunSafe(() =>
                {
                    if (ex.InnerExceptions.FirstOrDefault(match => match is OperationCanceledException) != null ||
                        ex.InnerExceptions.FirstOrDefault(match => match is TaskCanceledException) != null)
                        return; //Canceled
                    else if (ex.InnerException == null)
                        handler!.Invoke(ex.InnerException, null, cancelSource!.Token).AwaitSync();
                    else
                        handler!.Invoke(ex, null, cancelSource!.Token).AwaitSync();
                    return;
                });
            }
            catch (TaskCanceledException)
            {
                //Stop read
                RunSafe(() => cancelSource!.Cancel());
            }
            catch (Exception ex)
            {
                RunSafe(() => cancelSource!.Cancel());
                RunSafe(() => handler!.Invoke(ex, null, cancelSource!.Token).AwaitSync());
            }
            finally
            {
                Interlocked.Decrement(ref taskTrace);
            }
        }

        public ListenStream(Stream stream, uint maxBufferSize = 65536)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            buffer = new byte[maxBufferSize];
            workStream = Stream.Synchronized(stream);
        }

        public void StartListening(OnMessageEventHandler messageHandler)
        {
            if (disposedValue)
                throw new ObjectDisposedException(this.GetType().FullName);
            if (messageHandler == null)
                throw new ArgumentNullException(nameof(messageHandler));
            if (Listening)
                throw new InvalidOperationException($"{nameof(StartListening)} is not supported while {nameof(Listening)}. Call {nameof(StopListening)} first");

            handler = messageHandler;

            cancelSource?.Dispose();
            cancelSource = new CancellationTokenSource();

            _ = Task.Run(AsyncListen, cancelSource.Token);
        }
        public void StopListening()
        {
            cancelSource?.Cancel();
        }

        #region Stream
        public override bool CanRead => workStream.CanRead;
        public override bool CanSeek => workStream.CanSeek;
        public override bool CanWrite => workStream.CanWrite;
        public override long Length => workStream.Length;
        public override long Position { get => workStream.Position; set => workStream.Position = value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Listening)
                throw new InvalidOperationException($"{nameof(Read)} is not supported while {nameof(Listening)}");
            return workStream.Read(buffer, offset, count);
        }
        public override void Flush() => workStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => workStream.Seek(offset, origin);
        public override void SetLength(long value) => workStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => workStream.Write(buffer, offset, count);
        #endregion

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
                StopListening();
                workStream?.Dispose();
            }
        }
        #endregion
    }

    /// <summary>
    /// Asynchronous event-based reading from stream
    /// </summary>
    public class ListenStream<TProtocol> : Stream, IDisposable where TProtocol : IStreamProtocol
    {
        private readonly Stream workStream;
        private readonly TProtocol protocol;
        private readonly byte[] buffer;
        private int bufferOffset = 0;
        private CancellationTokenSource? cancelSource = null;
        private OnMessageEventHandler? handler = null;
        private int taskTrace = 0;
        private bool disposedValue = false;

        public bool Listening => (!cancelSource?.IsCancellationRequested) ?? false;

        private static Exception? RunSafe(Action function)
        {
            function.InvokeSafe(out var ex);
            return ex;
        }
        private async Task AsyncListen()
        {
            Interlocked.Increment(ref taskTrace);
            try
            {
                while (!cancelSource!.IsCancellationRequested)
                {
                    while (bufferOffset < buffer.Length)
                    {
                        var read = await workStream.ReadAsync(buffer, bufferOffset, buffer.Length - bufferOffset, cancelSource.Token);
                        if (read <= 0)
                            throw new IOException("End of stream reached");
                        bufferOffset += read;
                    }
                    bufferOffset = 0;
                    var packet = await protocol.GetPacketBytesAsync(buffer, workStream, cancelSource.Token);
                    await handler!.Invoke(null, packet, cancelSource!.Token);
                }
            }
            catch (AggregateException ex)
            {
                RunSafe(() => cancelSource!.Cancel());
                RunSafe(() =>
                {
                    if (ex.InnerExceptions.FirstOrDefault(match => match is OperationCanceledException) != null ||
                        ex.InnerExceptions.FirstOrDefault(match => match is TaskCanceledException) != null)
                        return; //Canceled
                    else if (ex.InnerException == null)
                        handler!.Invoke(ex.InnerException, null, cancelSource!.Token).AwaitSync();
                    else
                        handler!.Invoke(ex, null, cancelSource!.Token).AwaitSync();
                    return;
                });
            }
            catch (TaskCanceledException)
            {
                //Stop read
                RunSafe(() => cancelSource!.Cancel());
            }
            catch (Exception ex)
            {
                RunSafe(() => cancelSource!.Cancel());
                RunSafe(() => handler!.Invoke(ex, null, cancelSource!.Token).AwaitSync());
            }
            finally
            {
                Interlocked.Decrement(ref taskTrace);
            }
        }

        public ListenStream(Stream stream, TProtocol protocol)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (protocol == null)
                throw new ArgumentNullException(nameof(protocol));

            workStream = Stream.Synchronized(stream);

            this.protocol = protocol;
            buffer = new byte[this.protocol.FixedHeaderSize];
        }

        public void StartListening(OnMessageEventHandler messageHandler)
        {
            if (disposedValue)
                throw new ObjectDisposedException(this.GetType().FullName);
            if (messageHandler == null)
                throw new ArgumentNullException(nameof(messageHandler));
            if (Listening)
                throw new InvalidOperationException($"{nameof(StartListening)} is not supported while {nameof(Listening)}. Call {nameof(StopListening)} first");

            handler = messageHandler;
            bufferOffset = 0;

            cancelSource?.Dispose();
            cancelSource = new CancellationTokenSource();

            _ = Task.Run(AsyncListen, cancelSource.Token);
        }
        public void StopListening()
        {
            cancelSource?.Cancel();
        }

        #region Stream
        public override bool CanRead => workStream.CanRead;
        public override bool CanSeek => workStream.CanSeek;
        public override bool CanWrite => workStream.CanWrite;
        public override long Length => workStream.Length;
        public override long Position { get => workStream.Position; set => workStream.Position = value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Listening)
                throw new InvalidOperationException($"{nameof(Read)} is not supported while {nameof(Listening)}");
            return workStream.Read(buffer, offset, count);
        }
        public override void Flush() => workStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => workStream.Seek(offset, origin);
        public override void SetLength(long value) => workStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => workStream.Write(buffer, offset, count);
        #endregion

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
                StopListening();
                workStream?.Dispose();
            }
        }
        #endregion
    }

}

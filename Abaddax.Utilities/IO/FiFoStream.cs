using Abaddax.Utilities.Buffers;
using System.Buffers;
using static Abaddax.Utilities.Buffers.ArrayPoolExtensions;

namespace Abaddax.Utilities.IO
{
    public class FiFoStream : SpanStream, IDisposable
    {
        private readonly LinkedList<ArraySegment> _pendingSegments = new LinkedList<ArraySegment>();
        private readonly ArrayPool<byte> _pool;
        private bool _disposedValue = false;

        public FiFoStream(ArrayPool<byte>? arrayPool = null)
        {
            _pool = arrayPool ?? ArrayPool<byte>.Shared;
        }

        #region Stream
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        public override long Length
        {
            get
            {
                lock (_pendingSegments)
                {
                    var length = 0;
                    foreach (var segment in _pendingSegments)
                    {
                        length += segment.Span.Length;
                    }
                    return length;
                }
            }
        }
        public override long Position { get => 0; set => throw new NotSupportedException(); }

        public override void Flush()
        {
            lock (_pendingSegments)
            {
                foreach (var segment in _pendingSegments)
                {
                    segment.Dispose();
                }
                _pendingSegments.Clear();
            }
        }

        public override int Read(Span<byte> buffer)
        {
            lock (_pendingSegments)
            {
                if (_pendingSegments.Count == 0)
                    return -1;

                //Offset in buffer
                int currentOffset = 0;
                while (currentOffset != buffer.Length)
                {
                    //Offset in segment
                    int bytesWritten = 0;
                    var segment = _pendingSegments.First!.Value;
                    _pendingSegments.RemoveFirst();

                    var src = segment.Span;
                    var dst = buffer.Slice(currentOffset, buffer.Length - currentOffset);

                    //Copy src to dst
                    if (src.Length <= dst.Length)
                    {
                        src.CopyTo(dst);
                        bytesWritten = src.Length;
                        currentOffset += bytesWritten;
                    }
                    else
                    {
                        src.Slice(0, dst.Length).CopyTo(dst);
                        bytesWritten = dst.Length;
                        currentOffset += bytesWritten;
                    }

                    //Reappend to segment
                    if (src.Length > bytesWritten)
                    {
                        segment.Advance(bytesWritten);
                        _pendingSegments.AddFirst(segment);
                    }
                    //Return to pool
                    else
                    {
                        segment.Dispose();
                    }

                    if (_pendingSegments.Count == 0)
                        break;
                }
                return currentOffset;
            }
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = Read(buffer.Span);
            return ValueTask.FromResult(result);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            lock (_pendingSegments)
            {
                var segment = new ArraySegment(buffer, _pool);
                _pendingSegments.AddLast(segment);
            }
        }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        #endregion

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Flush();
                }
                base.Dispose(disposing);
                _disposedValue = true;
            }
        }
        #endregion

        #region Helper
        private sealed class ArraySegment : IDisposable
        {
            private readonly PooledArray<byte> _buffer;
            private bool _disposedValue = false;

            public Span<byte> Span => _buffer.Span.Slice(Offset);
            public int Offset { get; private set; }

            public ArraySegment(ReadOnlySpan<byte> buffer, ArrayPool<byte> pool)
            {
                _buffer = pool.CopyArray(buffer);
                Offset = 0;
            }

            public void Advance(int count)
            {
                if (Offset + count > _buffer.Span.Length)
                    throw new InvalidOperationException("Buffersize exceeded.");
                Offset += count;
            }

            #region IDisposable
            private void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _buffer.Dispose();
                    }
                    _disposedValue = true;
                }
            }
            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
            #endregion
        }
        #endregion
    }
}

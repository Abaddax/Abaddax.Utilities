namespace Abaddax.Utilities.IO
{
    public abstract class SpanStream : Stream
    {
        public abstract override int Read(Span<byte> buffer);
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
        public abstract override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public abstract override void Write(ReadOnlySpan<byte> buffer);
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public abstract override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }
}

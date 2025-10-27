namespace Abaddax.Utilities.IO
{
    public sealed class RawStreamProtocol : IStreamProtocol
    {
        public const uint DefaultBufferSize = ushort.MaxValue;
        public static uint FixedHeaderSize => 0;

        private readonly byte[] _buffer;

        public RawStreamProtocol()
            : this(DefaultBufferSize) { }
        public RawStreamProtocol(uint maxBufferSize = DefaultBufferSize)
        {
            ArgumentOutOfRangeException.ThrowIfZero(maxBufferSize);
            _buffer = new byte[maxBufferSize];
        }
        public async Task<ReadOnlyMemory<byte>> GetPacketBytesAsync(ReadOnlyMemory<byte> header, Stream stream, CancellationToken cancellationToken)
        {
            if (header.Length != FixedHeaderSize)
                throw new InvalidOperationException($"Header-size does not match {nameof(FixedHeaderSize)}");

            var read = await stream.ReadAsync(_buffer, cancellationToken);
            if (read <= 0)
                throw new EndOfStreamException();
            return _buffer.AsMemory(0, read);
        }
    }
}

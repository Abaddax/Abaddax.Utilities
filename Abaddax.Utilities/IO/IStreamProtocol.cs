namespace Abaddax.Utilities.IO
{
    public interface IStreamProtocol
    {
        static abstract uint FixedHeaderSize { get; }
        Task<ReadOnlyMemory<byte>> GetPacketBytesAsync(ReadOnlyMemory<byte> header, Stream stream, CancellationToken cancellationToken = default);
    }
}

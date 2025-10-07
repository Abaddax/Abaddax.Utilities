namespace Abaddax.Utilities.IO
{
    public interface IStreamProtocol
    {
        int FixedHeaderSize { get; }
        Task<ReadOnlyMemory<byte>> GetPacketBytesAsync(ReadOnlyMemory<byte> header, Stream stream, CancellationToken cancellationToken = default);
    }
}

namespace Abaddax.Utilities.IO
{
    public interface IStreamProtocol
    {
        int FixedHeaderSize { get; }
        Task<Memory<byte>> GetPacketBytesAsync(Memory<byte> header, Stream stream, CancellationToken token = default);
    }
}

namespace Abaddax.Utilities.Network
{
    public interface IStreamParser<T>
    {
        /// <summary>
        /// Reads a message from <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">Stream with the data to parse</param>
        /// <param name="cancellationToken"></param>
        Task<T> ReadAsync(Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Write <paramref name="message"/> to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">Stream to write the parsed data</param>
        /// <param name="message">Message to parse</param>
        /// <param name="cancellationToken"></param>
        Task WriteAsync(Stream stream, T message, CancellationToken cancellationToken = default);
    }
}

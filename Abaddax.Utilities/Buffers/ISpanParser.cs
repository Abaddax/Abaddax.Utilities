namespace Abaddax.Utilities.Buffers
{
    public interface ISpanParser<T>
    {
        /// <summary>
        /// Parse binary <paramref name="packet"/> to <typeparamref name="T"/>
        /// </summary>
        /// <param name="packet">binary data</param>
        T Read(ReadOnlySpan<byte> packet);

        /// <summary>
        /// Gets the necessary buffer size to write the <paramref name="message"/>
        /// </summary>
        /// <returns>Number of bytes required in the buffer</returns>
        int GetMessageSize(T message);

        /// <summary>
        /// Parse the <paramref name="message"/> to binary
        /// </summary>
        /// <param name="destination">Destination of the binary data</param>
        /// <param name="message"></param>
        /// <returns>The readonly slice of <paramref name="destination"/> with the message</returns>
        int Write(T message, Span<byte> destination);
    }
}

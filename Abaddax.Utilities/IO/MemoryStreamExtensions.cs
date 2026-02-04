using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.IO
{
    public static class MemoryStreamExtensions
    {
        #region Helper
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_buffer")]
        private static extern ref byte[] _buffer(this MemoryStream ms);
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_origin")]
        private static extern ref int _origin(this MemoryStream ms);
        #endregion

        private static ArraySegment<byte> GetInternalBuffer(MemoryStream stream)
        {
            if (stream.TryGetBuffer(out var buffer))
                return buffer;
            return new ArraySegment<byte>(stream._buffer(), stream._origin(), (int)stream.Length);
        }

        public static ReadOnlySpan<byte> AsReadOnlySpan(this MemoryStream stream)
        {
            return GetInternalBuffer(stream);
        }
        public static ReadOnlyMemory<byte> AsReadOnlyMemory(this MemoryStream stream)
        {
            return GetInternalBuffer(stream);
        }
    }
}

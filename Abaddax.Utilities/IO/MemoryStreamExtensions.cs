﻿using System.Reflection;

namespace Abaddax.Utilities.IO
{
    public static class MemoryStreamExtensions
    {
        #region Helper
        private static readonly FieldInfo? _bufferField = typeof(MemoryStream)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(x => x.FieldType == typeof(byte[]))
            .FirstOrDefault(x => x.Name.Contains("buffer", StringComparison.InvariantCultureIgnoreCase));
        private static readonly FieldInfo? _originField = typeof(MemoryStream)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(x => x.FieldType == typeof(int))
            .FirstOrDefault(x => x.Name.Contains("origin", StringComparison.InvariantCultureIgnoreCase));
        #endregion

        private static ArraySegment<byte> GetInternalBuffer(MemoryStream stream)
        {
            if (stream.TryGetBuffer(out var buffer))
                return buffer;
            if (_bufferField == null || _originField == null)
                return stream.ToArray();

            var internalBuffer = (byte[])_bufferField.GetValue(stream)!;

            return new ArraySegment<byte>(internalBuffer, (int)_originField.GetValue(stream)!, (int)stream.Length);
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

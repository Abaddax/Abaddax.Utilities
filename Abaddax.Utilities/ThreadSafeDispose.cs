namespace Abaddax.Utilities
{
    public static class ThreadSafeDispose
    {
        /// <summary>
        /// Tries to set <paramref name="disposedValue"/> to <see langword="1"/>
        /// </summary>
        /// <returns>false -> already disposed <br/> true -> dispose now</returns>
        public static bool TryDispose(ref int disposedValue)
        {
            return Interlocked.CompareExchange(ref disposedValue, 1, 0) == 0;
        }

        /// <summary>
        /// Checks if <paramref name="disposedValue"/> to <see langword="0"/>
        /// </summary>
        /// <returns>false -> disposeValue is not set jet <br/> true -> already disposed</returns>
        public static bool IsDisposed(ref int disposedValue)
        {
            return Interlocked.CompareExchange(ref disposedValue, 0, 0) != 0;
        }
    }
}

namespace Abaddax.Utilities
{
    public struct ThreadSafeDispose
    {
        private int _disposedValue = 0;

        /// <summary>
        /// Check if already maked as disposed
        /// </summary>
        public bool IsDisposed
        {
            get => Interlocked.CompareExchange(ref _disposedValue, 0, 0) != 0;
        }
        /// <summary>
        /// Tries to mark as disposed
        /// </summary>
        /// <returns><see langword="false"/>: already disposed<br/> <see langword="true"/>: dispose now</returns>
        /// <example><see langword="if"/>(<see cref="TryDispose"/>)<br/>{<br/>//Dispose<br/>}</example>
        public bool TryDispose()
        {
            return Interlocked.CompareExchange(ref _disposedValue, 1, 0) == 0;
        }

        public ThreadSafeDispose()
        {

        }
    }
}

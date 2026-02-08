namespace Abaddax.Utilities.Network.Http
{
    public static class HttpClientExtensions
    {
        public static HttpClient CreateHttpClientFromStream(Stream stream)
        {
#pragma warning disable CA2000 //Ownership transfer
            var socketHttpHandler = new SocketsHttpHandler()
            {
                ConnectCallback = (context, cancellationToken) =>
                {
                    return ValueTask.FromResult(stream);
                },
                ConnectTimeout = TimeSpan.FromSeconds(10),
                MaxConnectionsPerServer = 1,
                PooledConnectionIdleTimeout = TimeSpan.Zero,
                PooledConnectionLifetime = TimeSpan.Zero,
                EnableMultipleHttp2Connections = false,
#if NET9_0_OR_GREATER
                EnableMultipleHttp3Connections = false,
#endif
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions()
                {
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.None,
                },
                AllowAutoRedirect = false
            };
            return new HttpClient(socketHttpHandler, disposeHandler: true);
#pragma warning restore CA2000
        }
    }
}

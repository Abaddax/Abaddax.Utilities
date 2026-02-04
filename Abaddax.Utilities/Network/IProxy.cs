namespace Abaddax.Utilities.Network
{
    public interface IProxy
    {
        void Tunnel(CancellationToken cancellationToken = default);
        Task TunnelAsync(CancellationToken cancellationToken = default);
    }
}

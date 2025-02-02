namespace Abaddax.Utilities.Network
{
    public interface IProxy
    {
        void Tunnel(CancellationToken token = default);
        Task TunnelAsync(CancellationToken token = default);
    }
}

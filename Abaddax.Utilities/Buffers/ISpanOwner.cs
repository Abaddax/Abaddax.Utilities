namespace Abaddax.Utilities.Buffers
{
    public interface ISpanOwner<T> : IDisposable
    {
        Span<T> Span { get; }
    }
}

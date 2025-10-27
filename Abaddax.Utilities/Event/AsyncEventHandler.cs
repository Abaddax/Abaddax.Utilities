namespace Abaddax.Utilities.Event
{
    public delegate Task AsyncEventHandler(object? sender, EventArgs e, CancellationToken cancellationToken);
    public delegate Task AsyncEventHandler<TEventArgs>(object? sender, TEventArgs e, CancellationToken cancellationToken);
}

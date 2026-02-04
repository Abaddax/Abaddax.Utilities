namespace Abaddax.Utilities.Event
{
#if NET9_0_OR_GREATER
    public delegate void RefEventHandler<TEventArgs>(object? sender, TEventArgs e) where TEventArgs : struct, allows ref struct;
#endif
}

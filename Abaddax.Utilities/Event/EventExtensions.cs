using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Event
{
    public static class EventExtensions
    {
        public static IEnumerable<Exception> InvokeSafe(this EventHandler handler, object? sender, EventArgs args)
            => InvokeSafe<EventArgs>(handler.Invoke, sender, args);
        public static IEnumerable<Exception> InvokeSafe<TEventArgs>(this EventHandler<TEventArgs> handler, object? sender, TEventArgs args)
        {
            foreach (var invocation in handler.GetTypedInvocationList())
            {
                Exception exception;
                try
                {
                    invocation.Invoke(sender, args);
                    continue;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                yield return exception;
            }
        }

#if NET9_0_OR_GREATER
        public static IEnumerable<Exception> InvokeSafe<TEventArgs>(this RefEventHandler<TEventArgs> handler, object? sender, TEventArgs args)
            where TEventArgs : struct, allows ref struct
        {
            List<Exception>? exceptions = null;
            foreach (var invocation in handler.GetTypedInvocationList())
            {
                try
                {
                    invocation.Invoke(sender, args);
                }
                catch (Exception ex)
                {
                    exceptions ??= new();
                    exceptions.Add(ex);
                }
            }
            return exceptions ?? Enumerable.Empty<Exception>();
        }
#endif

        public static Task InvokeAsync(this AsyncEventHandler handler, object? sender, EventArgs args, CancellationToken cancellationToken)
            => InvokeAsync<EventArgs>(handler.Invoke, sender, args, cancellationToken);
        public static async Task InvokeAsync<TEventArgs>(this AsyncEventHandler<TEventArgs> handler, object? sender, TEventArgs args, CancellationToken cancellationToken)
        {
            List<Exception>? exceptions = null;
            await foreach (var exception in InvokeSafeAsync(handler, sender, args, cancellationToken))
            {
                exceptions ??= new();
                exceptions.Add(exception);
            }
            if (exceptions != null)
                throw new AggregateException(exceptions);
        }

        public static async IAsyncEnumerable<Exception> InvokeSafeAsync<TEventArgs>(this AsyncEventHandler<TEventArgs> handler, object? sender, TEventArgs args, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var invocationResults = handler.InvokeWithResults<Task, AsyncEventHandler<TEventArgs>>(sender, args, cancellationToken)
                .ToArray();
            foreach (var invokationResult in invocationResults)
            {
                Exception exception;
                if (!invokationResult.Success)
                {
                    exception = invokationResult.Exception;
                    goto YIELD_EXCEPTION;
                }
                try
                {
                    await invokationResult.Result;
                    continue;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            YIELD_EXCEPTION:
                yield return exception;
            }
        }
    }
}

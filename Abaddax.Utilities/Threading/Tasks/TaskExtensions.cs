using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Threading.Tasks
{
    public static class TaskExtensions
    {
        /// <exception cref="Exception"></exception>
        public static void AwaitSync(this Task task)
        {
            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException != null)
                    throw ex.InnerException;
                throw;
            }
        }
        /// <exception cref="Exception"></exception>
        public static TResult AwaitSync<TResult>(this Task<TResult> task)
        {
            try
            {
                return task.GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException != null)
                    throw ex.InnerException;
                throw;
            }
        }

        /// <exception cref="none"></exception>
        public static async Task IgnoreException(this Task task, Action<Exception>? exceptionHandler = null)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                exceptionHandler?.InvokeSafe(ex, out _);
                return;
            }
        }
        /// <exception cref="none"></exception>
        public static async Task<TResult> IgnoreException<TResult>(this Task<TResult> task, TResult errorResult = default!, Action<Exception>? exceptionHandler = null)
        {
            try
            {
                return await task;
            }
            catch (Exception ex)
            {
                exceptionHandler?.InvokeSafe(ex, out _);
                return errorResult;
            }
        }

#if NET9_0_OR_GREATER
        public static async Task AwaitAll(this IEnumerable<Task> source, CancellationToken token = default)
        {
            await Task.WhenAll(source).WaitAsync(token);
        }
        public static async IAsyncEnumerable<TResult> AwaitAll<TResult>(this IEnumerable<Task<TResult>> source, [EnumeratorCancellation] CancellationToken token = default)
        {
            await foreach (var task in Task.WhenEach(source).WithCancellation(token))
            {
                yield return await task;
            }
        }

        public static void AwaitAllSync(this IEnumerable<Task> source)
        {
            source.AwaitAll().AwaitSync();
        }
        public static IEnumerable<TResult> AwaitAllSync<TResult>(this IEnumerable<Task<TResult>> source)
        {
            return source.AwaitAll().ToEnumerableAsync().AwaitSync();
        }

        public static async Task<IEnumerable<TResult>> ToEnumerableAsync<TResult>(this IAsyncEnumerable<TResult> source, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            IEnumerable<TResult> _results = Enumerable.Empty<TResult>();
            await foreach (var result in source)
            {
                token.ThrowIfCancellationRequested();
                _results = _results.Append(result);
            }
            return _results;
        }
#endif

    }
}

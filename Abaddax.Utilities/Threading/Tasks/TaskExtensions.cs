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

        public static Task CompletedIfNull(this Task? task)
        {
            return task ?? Task.CompletedTask;
        }
        public static Task<TResult> CompletedIfNull<TResult>(this Task<TResult>? task, TResult nullResult = default!)
        {
            return task ?? Task.FromResult(nullResult);
        }
        public static ValueTask CompletedIfNull(this in ValueTask? task)
        {
            return task ?? ValueTask.CompletedTask;
        }
        public static ValueTask<TResult> CompletedIfNull<TResult>(this in ValueTask<TResult>? task, TResult nullResult = default!)
        {
            return task ?? ValueTask.FromResult(nullResult);
        }

#if NET9_0_OR_GREATER
        public static async Task AwaitAll(this IEnumerable<Task> source, CancellationToken cancellationToken = default)
        {
            await Task.WhenAll(source).WaitAsync(cancellationToken);
        }
        public static async IAsyncEnumerable<TResult> AwaitAll<TResult>(this IEnumerable<Task<TResult>> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var task in Task.WhenEach(source).WithCancellation(cancellationToken))
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

        public static async Task<IEnumerable<TResult>> ToEnumerableAsync<TResult>(this IAsyncEnumerable<TResult> source, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            var results = new List<TResult>();
            await foreach (var result in source)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(result);
            }
            return results.ToArray();
        }
#endif

    }
}

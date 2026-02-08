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
        public static Task IgnoreException(this Task task, Action<Exception>? exceptionHandler = null)
            => IgnoreException<Exception>(task, exceptionHandler);
        /// <exception cref="none"></exception>
        public static async Task IgnoreException<TException>(this Task task, Action<TException>? exceptionHandler = null)
            where TException : Exception
        {
            try
            {
                await task;
            }
            catch (TException ex)
            {
                exceptionHandler?.InvokeSafe(ex, out _);
                return;
            }
        }

        /// <exception cref="none"></exception>
        public static Task<TResult> IgnoreException<TResult>(this Task<TResult> task, TResult errorResult = default!, Action<Exception>? exceptionHandler = null)
            => IgnoreException<TResult, Exception>(task, errorResult, exceptionHandler);
        public static async Task<TResult> IgnoreException<TResult, TException>(this Task<TResult> task, TResult errorResult = default!, Action<TException>? exceptionHandler = null)
            where TException : Exception
        {
            try
            {
                return await task;
            }
            catch (TException ex)
            {
                exceptionHandler?.InvokeSafe(ex, out _);
                return errorResult;
            }
        }

        public static Task? AsTask(this ref ValueTask? task)
        {
            if (task == null)
                return null;
            return task.Value.AsTask();
        }
        public static Task<TResult>? AsTask<TResult>(this ref ValueTask<TResult>? task)
        {
            if (task == null)
                return null;
            return task.Value.AsTask();
        }

        public static async Task AwaitAll(this IEnumerable<Task> source, CancellationToken cancellationToken = default)
        {
            await Task.WhenAll(source).WaitAsync(cancellationToken);
        }
        public static async Task<IEnumerable<TResult>> AwaitAll<TResult>(this IEnumerable<Task<TResult>> source, CancellationToken cancellationToken = default)
        {
            return await Task.WhenAll(source).WaitAsync(cancellationToken);
        }

        public static void AwaitAllSync(this IEnumerable<Task> source)
        {
            source.AwaitAll().AwaitSync();
        }
        public static IEnumerable<TResult> AwaitAllSync<TResult>(this IEnumerable<Task<TResult>> source)
        {
            return source.AwaitAll().AwaitSync();
        }

        public static async Task<IEnumerable<TResult>> ToBlockingEnumerableAsync<TResult>(this IAsyncEnumerable<TResult> source, CancellationToken cancellationToken = default)
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
    }
}

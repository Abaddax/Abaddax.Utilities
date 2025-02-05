namespace Abaddax.Utilities.Threading.Tasks
{
    public static class TaskExtensions
    {
        /// <exception cref="Exception"></exception>
        public static void AwaitSync(this Task task)
        {
            try
            {
                task.Wait();
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
                task.Wait();
                return task.Result;
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
        public static async Task<TResult> IgnoreException<TResult>(this Task<TResult> task, TResult errorResult = default, Action<Exception>? exceptionHandler = null)
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

    }
}

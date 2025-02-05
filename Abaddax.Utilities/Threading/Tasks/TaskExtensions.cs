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

        /// <inheritdoc cref="Task.RunSynchronously()"/>
        public static void ExecuteSynchronously(this Task task)
        {
            task.RunSynchronously();
        }
        /// <inheritdoc cref="Task.RunSynchronously(TaskScheduler)"/>
        public static void ExecuteSynchronously(this Task task, TaskScheduler scheduler)
        {
            task.RunSynchronously();
        }
        /// <inheritdoc cref="Task.RunSynchronously()"/>
        public static TResult ExecuteSynchronously<TResult>(this Task<TResult> task)
        {
            task.RunSynchronously();
            return task.Result;
        }
        /// <inheritdoc cref="Task.RunSynchronously(TaskScheduler)"/>
        public static TResult ExecuteSynchronously<TResult>(this Task<TResult> task, TaskScheduler scheduler)
        {
            task.RunSynchronously(scheduler);
            return task.Result;
        }
    }
}

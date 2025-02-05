namespace Abaddax.Utilities
{
    public static class SafeExecute
    {
        /// <exception cref="none"></exception>
        public static void InvokeSafe(this Action function, out Exception? exception)
        {
            exception = null;
            try
            {
                function.Invoke();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }
        /// <exception cref="none"></exception>
        public static void InvokeSafe<T1>(this Action<T1> function, T1 arg1, out Exception? exception)
        {
            exception = null;
            try
            {
                function.Invoke(arg1);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }
        /// <exception cref="none"></exception>
        public static void InvokeSafe<T1, T2>(this Action<T1, T2> function, T1 arg1, T2 arg2, out Exception? exception)
        {
            exception = null;
            try
            {
                function.Invoke(arg1, arg2);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }
        /// <exception cref="none"></exception>
        public static void InvokeSafe<T1, T2, T3>(this Action<T1, T2, T3> function, T1 arg1, T2 arg2, T3 arg3, out Exception? exception)
        {
            exception = null;
            try
            {
                function.Invoke(arg1, arg2, arg3);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }
        /// <exception cref="none"></exception>
        public static void InvokeSafe<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> function, T1 arg1, T2 arg2, T3 arg3, T4 arg4, out Exception? exception)
        {
            exception = null;
            try
            {
                function.Invoke(arg1, arg2, arg3, arg4);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }

        /// <exception cref="none"></exception>
        public static Exception? InvokeSafe(this Action function)
        {
            function.InvokeSafe(out var exception);
            return exception;
        }
        /// <exception cref="none"></exception>
        public static Exception? InvokeSafe<T1>(this Action<T1> function, T1 arg1)
        {
            function.InvokeSafe(arg1, out var exception);
            return exception;
        }
        /// <exception cref="none"></exception>
        public static Exception? InvokeSafe<T1, T2>(this Action<T1, T2> function, T1 arg1, T2 arg2)
        {
            function.InvokeSafe(arg1, arg2, out var exception);
            return exception;
        }
        /// <exception cref="none"></exception>
        public static Exception? InvokeSafe<T1, T2, T3>(this Action<T1, T2, T3> function, T1 arg1, T2 arg2, T3 arg3)
        {
            function.InvokeSafe(arg1, arg2, arg3, out var exception);
            return exception;
        }
        /// <exception cref="none"></exception>
        public static Exception? InvokeSafe<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> function, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            function.InvokeSafe(arg1, arg2, arg3, arg4, out var exception);
            return exception;
        }



        /// <exception cref="none"></exception>
        public static TResult InvokeSafe<TResult>(this Func<TResult> function, out Exception? exception, TResult errorResult = default)
        {
            exception = null;
            try
            {
                return function.Invoke();
            }
            catch (Exception ex)
            {
                exception = ex;
                return errorResult;
            }
        }
        /// <exception cref="none"></exception>
        public static TResult InvokeSafe<TResult, T1>(this Func<T1, TResult> function, T1 arg1, out Exception? exception, TResult errorResult = default)
        {
            exception = null;
            try
            {
                return function.Invoke(arg1);
            }
            catch (Exception ex)
            {
                exception = ex;
                return errorResult;
            }
        }
        /// <exception cref="none"></exception>
        public static TResult InvokeSafe<TResult, T1, T2>(this Func<T1, T2, TResult> function, T1 arg1, T2 arg2, out Exception? exception, TResult errorResult = default)
        {
            exception = null;
            try
            {
                return function.Invoke(arg1, arg2);
            }
            catch (Exception ex)
            {
                exception = ex;
                return errorResult;
            }
        }
        /// <exception cref="none"></exception>
        public static TResult InvokeSafe<TResult, T1, T2, T3>(this Func<T1, T2, T3, TResult> function, T1 arg1, T2 arg2, T3 arg3, out Exception? exception, TResult errorResult = default)
        {
            exception = null;
            try
            {
                return function.Invoke(arg1, arg2, arg3);
            }
            catch (Exception ex)
            {
                exception = ex;
                return errorResult;
            }
        }
        /// <exception cref="none"></exception>
        public static TResult InvokeSafe<TResult, T1, T2, T3, T4>(this Func<T1, T2, T3, T4, TResult> function, T1 arg1, T2 arg2, T3 arg3, T4 arg4, out Exception? exception, TResult errorResult = default)
        {
            exception = null;
            try
            {
                return function.Invoke(arg1, arg2, arg3, arg4);
            }
            catch (Exception ex)
            {
                exception = ex;
                return errorResult;
            }
        }



    }
}

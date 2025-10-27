using System.Diagnostics.CodeAnalysis;

namespace Abaddax.Utilities
{
    public static class DelegateExtensions
    {
        public class InvocationResult<TResult>
        {
            [MemberNotNullWhen(true, nameof(Result))]
            [MemberNotNullWhen(false, nameof(Exception))]
            public bool Success => Exception == null;
            public TResult? Result { get; init; } = default;
            public Exception? Exception { get; init; }
        }

        public static IEnumerable<TDelegate> GetTypedInvocationList<TDelegate>(this TDelegate @delegate)
             where TDelegate : Delegate
        {
            var invocationList = @delegate.GetInvocationList();
            return invocationList.Cast<TDelegate>();
        }
        public static IEnumerable<InvocationResult<TResult>> InvokeWithResults<TResult, TFunc>(this TFunc function, params object?[]? parameters)
            where TFunc : Delegate
        {
            foreach (var invocation in function.GetTypedInvocationList())
            {
                InvocationResult<TResult> result;
                try
                {
                    result = new InvocationResult<TResult>()
                    {
                        Result = (TResult)invocation.DynamicInvoke(parameters)!
                    };
                }
                catch (Exception ex)
                {
                    result = new InvocationResult<TResult>()
                    {
                        Exception = ex
                    };
                }
                yield return result;
            }
        }

        public static IEnumerable<InvocationResult<TResult>> InvokeWithResults<TResult>(this Func<TResult> function)
            => function.InvokeWithResults<TResult, Func<TResult>>();
        public static IEnumerable<InvocationResult<TResult>> InvokeWithResults<TResult, T1>(this Func<T1, TResult> function, T1 arg1)
            => function.InvokeWithResults<TResult, Func<T1, TResult>>(arg1);
        public static IEnumerable<InvocationResult<TResult>> InvokeWithResults<TResult, T1, T2>(this Func<T1, T2, TResult> function, T1 arg1, T2 arg2)
            => function.InvokeWithResults<TResult, Func<T1, T2, TResult>>(arg1, arg2);
        public static IEnumerable<InvocationResult<TResult>> InvokeWithResults<TResult, T1, T2, T3>(this Func<T1, T2, T3, TResult> function, T1 arg1, T2 arg2, T3 arg3)
            => function.InvokeWithResults<TResult, Func<T1, T2, T3, TResult>>(arg1, arg2, arg3);
        public static IEnumerable<InvocationResult<TResult>> InvokeWithResults<TResult, T1, T2, T3, T4>(this Func<T1, T2, T3, T4, TResult> function, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => function.InvokeWithResults<TResult, Func<T1, T2, T3, T4, TResult>>(arg1, arg2, arg3, arg4);
    }
}

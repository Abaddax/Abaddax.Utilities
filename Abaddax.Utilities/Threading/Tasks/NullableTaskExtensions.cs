using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Threading.Tasks
{
    public static class NullableTaskExtensions
    {
        public static NullableTaskWrapper CompletedIfNull(this Task? task)
            => new NullableTaskWrapper()
            {
                Task = task
            };
        public static NullableValueTaskWrapper CompletedIfNull(this ValueTask? task)
            => new NullableValueTaskWrapper()
            {
                Task = task
            };

        public readonly struct NullableTaskWrapper
        {
            public required Task? Task { get; init; }
            public TaskAwaiter GetAwaiter() => NullableTaskExtensions.GetAwaiter(this);
        }
        public readonly struct NullableValueTaskWrapper
        {
            public required ValueTask? Task { get; init; }
            public ValueTaskAwaiter GetAwaiter() => NullableTaskExtensions.GetAwaiter(this);
        }

        public static TaskAwaiter GetAwaiter(this NullableTaskWrapper? taskWrapper)
            => taskWrapper?.Task?.GetAwaiter() ?? Task.CompletedTask.GetAwaiter();
        public static ValueTaskAwaiter GetAwaiter(this NullableValueTaskWrapper? taskWrapper)
            => taskWrapper?.Task?.GetAwaiter() ?? ValueTask.CompletedTask.GetAwaiter();
    }
}

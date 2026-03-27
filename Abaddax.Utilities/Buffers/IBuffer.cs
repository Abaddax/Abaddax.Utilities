using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Buffers
{
    public interface IBuffer<T> : IMemoryOwner<T>, ISpanOwner<T>, IEnumerable<T>, IDisposable
    {
        int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Span.Length;
        }
        T this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Span[i];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Span[i] = value;
        }
        Span<T> this[Range range]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Span[range];
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => AsEnumerable().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<T> AsEnumerable()
        {
            for (int i = 0; i < Length; i++)
            {
                yield return this[i];
            }
        }
    }
}

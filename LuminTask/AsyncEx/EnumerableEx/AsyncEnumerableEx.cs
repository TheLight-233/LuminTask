using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LuminThread.Interface;

namespace LuminThread.AsyncEx.EnumerableEx;

public static class AsyncEnumerableExtensions
{
    public static ILuminTaskAsyncEnumerable<T> AsLuminTaskAsyncEnumerable<T>(this IAsyncEnumerable<T> source)
    {
        return new AsyncEnumerableToLuminTaskAsyncEnumerable<T>(source);
    }

    public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this ILuminTaskAsyncEnumerable<T> source)
    {
        return new LuminTaskAsyncEnumerableToAsyncEnumerable<T>(source);
    }

    sealed class AsyncEnumerableToLuminTaskAsyncEnumerable<T> : ILuminTaskAsyncEnumerable<T>
    {
        readonly IAsyncEnumerable<T> source;

        public AsyncEnumerableToLuminTaskAsyncEnumerable(IAsyncEnumerable<T> source)
        {
            this.source = source;
        }

        public ILuminTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new Enumerator(source.GetAsyncEnumerator(cancellationToken));
        }

        sealed class Enumerator : ILuminTaskAsyncEnumerator<T>
        {
            readonly IAsyncEnumerator<T> enumerator;

            public Enumerator(IAsyncEnumerator<T> enumerator)
            {
                this.enumerator = enumerator;
            }

            public T Current => enumerator.Current;

            public async LuminTask DisposeAsync()
            {
                await enumerator.DisposeAsync();
            }

            public async LuminTask<bool> MoveNextAsync()
            {
                return await enumerator.MoveNextAsync();
            }
        }
    }

    sealed class LuminTaskAsyncEnumerableToAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        readonly ILuminTaskAsyncEnumerable<T> source;

        public LuminTaskAsyncEnumerableToAsyncEnumerable(ILuminTaskAsyncEnumerable<T> source)
        {
            this.source = source;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new Enumerator(source.GetAsyncEnumerator(cancellationToken));
        }

        sealed class Enumerator : IAsyncEnumerator<T>
        {
            readonly ILuminTaskAsyncEnumerator<T> enumerator;

            public Enumerator(ILuminTaskAsyncEnumerator<T> enumerator)
            {
                this.enumerator = enumerator;
            }

            public T Current => enumerator.Current;

            public ValueTask DisposeAsync()
            {
                return enumerator.DisposeAsync();
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return enumerator.MoveNextAsync();
            }
        }
    }
}
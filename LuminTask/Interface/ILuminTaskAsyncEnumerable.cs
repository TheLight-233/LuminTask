using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace LuminThread.Interface;

public interface ILuminTaskAsyncEnumerable<out T>
{
    ILuminTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
}

public interface ILuminTaskAsyncEnumerator<out T> : ILuminTaskAsyncDisposable
{
    T Current { get; }
    LuminTask<bool> MoveNextAsync();
}

public interface ILuminTaskAsyncDisposable
{
    LuminTask DisposeAsync();
}

public interface IConnectableUniTaskAsyncEnumerable<out T> : ILuminTaskAsyncEnumerable<T>
{
    IDisposable Connect();
}

public static class LuminTaskAsyncEnumerableExtensions
{
    public static LuminTaskCancelableAsyncEnumerable<T> WithCancellation<T>(this ILuminTaskAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        return new LuminTaskCancelableAsyncEnumerable<T>(source, cancellationToken);
    }
}

[StructLayout(LayoutKind.Auto)]
public readonly struct LuminTaskCancelableAsyncEnumerable<T>
{
    private readonly ILuminTaskAsyncEnumerable<T> enumerable;
    private readonly CancellationToken cancellationToken;

    internal LuminTaskCancelableAsyncEnumerable(ILuminTaskAsyncEnumerable<T> enumerable, CancellationToken cancellationToken)
    {
        this.enumerable = enumerable;
        this.cancellationToken = cancellationToken;
    }

    public Enumerator GetAsyncEnumerator()
    {
        return new Enumerator(enumerable.GetAsyncEnumerator(cancellationToken));
    }

    [StructLayout(LayoutKind.Auto)]
    public readonly struct Enumerator
    {
        private readonly ILuminTaskAsyncEnumerator<T> enumerator;

        internal Enumerator(ILuminTaskAsyncEnumerator<T> enumerator)
        {
            this.enumerator = enumerator;
        }

        public T Current => enumerator.Current;

        public LuminTask<bool> MoveNextAsync()
        {
            return enumerator.MoveNextAsync();
        }


        public LuminTask DisposeAsync()
        {
            return enumerator.DisposeAsync();
        }
    }
}
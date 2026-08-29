using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

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

public interface IConnectableLuminTaskAsyncEnumerable<out T> : ILuminTaskAsyncEnumerable<T>
{
    IDisposable Connect();
}

public static class LuminTaskAsyncEnumerableExtensions
{
    public static LuminTaskCancelableAsyncEnumerable<T> WithCancellation<T>(this ILuminTaskAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        return new LuminTaskCancelableAsyncEnumerable<T>(source, cancellationToken);
    }

    /// <summary>Adapts a LuminTask async enumerable to the BCL await-foreach contract.</summary>
    public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this ILuminTaskAsyncEnumerable<T> source)
        => new BclAsyncEnumerableAdapter<T>(source);

    /// <summary>Controls context capture for await-foreach consumption.</summary>
    public static IAsyncEnumerable<T> ConfigureAwait<T>(this ILuminTaskAsyncEnumerable<T> source, bool continueOnCapturedContext)
        => new BclAsyncEnumerableAdapter<T>(source, continueOnCapturedContext);
}

internal sealed class BclAsyncEnumerableAdapter<T> : IAsyncEnumerable<T>
{
    private readonly ILuminTaskAsyncEnumerable<T> _source;
    private readonly bool _continueOnCapturedContext;

    internal BclAsyncEnumerableAdapter(ILuminTaskAsyncEnumerable<T> source, bool continueOnCapturedContext = true)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _continueOnCapturedContext = continueOnCapturedContext;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new BclAsyncEnumeratorAdapter<T>(_source.GetAsyncEnumerator(cancellationToken), _continueOnCapturedContext);
}

internal sealed class BclAsyncEnumeratorAdapter<T> : IAsyncEnumerator<T>
{
    private readonly ILuminTaskAsyncEnumerator<T> _inner;
    private readonly bool _continueOnCapturedContext;

    internal BclAsyncEnumeratorAdapter(ILuminTaskAsyncEnumerator<T> inner, bool continueOnCapturedContext)
    {
        _inner = inner;
        _continueOnCapturedContext = continueOnCapturedContext;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
        => MoveNextCore();

    private async ValueTask<bool> MoveNextCore()
        => await _inner.MoveNextAsync().ConfigureAwait(_continueOnCapturedContext);

    public ValueTask DisposeAsync()
        => DisposeCore();

    private async ValueTask DisposeCore()
        => await _inner.DisposeAsync().ConfigureAwait(_continueOnCapturedContext);
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

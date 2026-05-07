using System.Threading;
using System.Threading.Tasks;

#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace Darkmatter.Fonepay
{
    internal static class FonepayAsyncBridge
    {
#if UNITASK_SUPPORT
        internal static FonepayAsync<T> Wrap<T>(Task<T> task, CancellationToken ct = default)
            => new FonepayAsync<T>(task.AsUniTask().AttachExternalCancellation(ct));

        internal static FonepayAsync Wrap(Task task, CancellationToken ct = default)
            => new FonepayAsync(task.AsUniTask().AttachExternalCancellation(ct));
#else
        // Task is already awaitable — wrap directly, no Awaitable needed
        internal static FonepayAsync<T> Wrap<T>(Task<T> task, CancellationToken ct = default)
            => new FonepayAsync<T>(task);

        internal static FonepayAsync Wrap(Task task, CancellationToken ct = default)
            => new FonepayAsync(task);
#endif
    }
}
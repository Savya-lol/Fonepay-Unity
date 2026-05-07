using System.Threading.Tasks;

namespace Darkmatter.Fonepay
{
#if UNITASK_SUPPORT
    using Cysharp.Threading.Tasks;

    public readonly struct FonepayAsync<T>
    {
        private readonly UniTask<T> _task;
        internal FonepayAsync(UniTask<T> task) => _task = task;

        // UniTask<T>.Awaiter is a public named type — no problem
        public UniTask<T>.Awaiter GetAwaiter() => _task.GetAwaiter();
    }

    public readonly struct FonepayAsync
    {
        private readonly UniTask _task;
        internal FonepayAsync(UniTask task) => _task = task;

        public UniTask.Awaiter GetAwaiter() => _task.GetAwaiter();
    }

#else
    // Unity 2023.1+ path — Awaitable<T>.GetAwaiter() return type is internal,
    // so we store Task<T> and convert lazily via an async wrapper.
    // The wrapper's return type is inferred — we never name the awaiter.

    public readonly struct FonepayAsync<T>
    {
        private readonly System.Threading.Tasks.Task<T> _task;
        internal FonepayAsync(System.Threading.Tasks.Task<T> task) => _task = task;

        // Return type inferred from the async method — compiler handles it
        public System.Runtime.CompilerServices.TaskAwaiter<T> GetAwaiter()
            => _task.GetAwaiter();
    }

    public readonly struct FonepayAsync
    {
        private readonly System.Threading.Tasks.Task _task;
        internal FonepayAsync(System.Threading.Tasks.Task task) => _task = task;

        public System.Runtime.CompilerServices.TaskAwaiter GetAwaiter()
            => _task.GetAwaiter();
    }
#endif
}
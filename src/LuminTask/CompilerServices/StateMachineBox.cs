using System;
using System.Runtime.CompilerServices;

namespace LuminThread.CompilerServices
{
    // Heap home for a suspended async state machine. One pool per TStateMachine (i.e. per async
    // method signature); the cached MoveNext delegate is allocated once per box and reused across
    // every suspension and every rent/return cycle, so after warmup an async method that suspends
    // allocates nothing. The pool is per-thread (ThreadStatic): a box may be rented on one thread
    // and returned on another, which only rebalances the per-thread free lists — a given box is
    // only ever touched by one logical invocation at a time.
    internal interface IStateMachineBox
    {
        Action MoveNextAction { get; }
        void Return();
    }

    internal sealed class StateMachineBox<TStateMachine> : IStateMachineBox
        where TStateMachine : IAsyncStateMachine
    {
        [ThreadStatic] private static StateMachineBox<TStateMachine>? _freeList;

        private StateMachineBox<TStateMachine>? _next;
        public TStateMachine StateMachine = default!;
        private readonly Action _moveNext;

        private StateMachineBox()
        {
            _moveNext = MoveNext;
        }

        public Action MoveNextAction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _moveNext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StateMachineBox<TStateMachine> Rent()
        {
            var box = _freeList;
            if (box != null)
            {
                _freeList = box._next;
                box._next = null;
                return box;
            }
            return new StateMachineBox<TStateMachine>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return()
        {
            StateMachine = default!;
            _next = _freeList;
            _freeList = this;
        }

        private void MoveNext() => StateMachine.MoveNext();
    }
}

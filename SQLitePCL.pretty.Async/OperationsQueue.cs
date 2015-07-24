/*
Copyright (c) 2014 David Bordoley
Copyright (c) 2012 GitHub

Permission is hereby granted,  free of charge,  to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to  use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

// Based off of https://github.com/akavache/Akavache/blob/master/Akavache/Portable/KeyedOperationQueue.cs
namespace SQLitePCL.pretty
{
    internal sealed class OperationsQueue
    {
        private abstract class Operation
        {
            public abstract Task EvaluateFunc();
        }

        private sealed class Operation<T> : Operation
        {
            public static Operation<T> Create(Func<CancellationToken, Task<T>> f, CancellationToken cancellationToken) =>
                new Operation<T>(f, cancellationToken);

            private readonly Func<CancellationToken, Task<T>> f;
            private readonly CancellationToken cancellationToken;
            private readonly TaskCompletionSource<T> result = new TaskCompletionSource<T>();

            private Operation(Func<CancellationToken, Task<T>> f, CancellationToken cancellationToken)
            {
                this.f = f;
                this.cancellationToken = cancellationToken;
            }

            public Task<T> Result
            {
                get
                {
                    return result.Task;
                }
            }

            public override async Task EvaluateFunc()
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var result = await f(cancellationToken).ConfigureAwait(false);
                    this.result.SetResult(result);
                }
                catch (Exception ex)
                {
                    this.result.SetException(ex);
                }
            }
        }

        private readonly Subject<Operation> queuedOps = new Subject<Operation>();
        private readonly IConnectableObservable<Unit> resultObs;
        private readonly Lazy<Task> dispose;

        internal OperationsQueue()
        {
            resultObs = queuedOps
                .Select(operation =>
                    // Defer Processing of the operation until subscribed to
                    Observable.Defer(() => operation.EvaluateFunc().ToObservable()))
                .Concat()
                .Multicast(new Subject<Unit>());

            resultObs.Connect();

            dispose = new Lazy<Task>(() =>
            {
                queuedOps.OnCompleted();
                return resultObs.LastOrDefaultAsync().ToTask();
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public Task<T> EnqueueOperation<T>(Func<CancellationToken, Task<T>> asyncCalculationFunc, CancellationToken cancellationToken)
        {
            var item = Operation<T>.Create(asyncCalculationFunc, cancellationToken);
            queuedOps.OnNext(item);
            return item.Result;
        }

        public Task DisposeAsync()
        {
            return dispose.Value;
        }
    }

    internal static class OperationsQueueExtensions
    {
        public static Task EnqueueOperation(this OperationsQueue This, Action<CancellationToken> calculationFunc, IScheduler scheduler, CancellationToken cancellationToken) =>
            This.EnqueueOperation(ct =>
                Observable.Start(() => calculationFunc(ct), scheduler).ToTask(), 
                cancellationToken);
    }
}
 

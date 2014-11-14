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
    internal abstract class Operation
    {
        public abstract IObservable<Unit> EvaluateFunc();
    }

    internal class Operation<T> : Operation
    {
        public static Operation<T> Create(Func<IObservable<T>> f)
        {
            return new Operation<T>(f);
        }

        private readonly Func<IObservable<T>> f;
        private readonly ReplaySubject<T> result = new ReplaySubject<T>();

        private Operation(Func<IObservable<T>> f)
        {
            this.f = f;
        }

        public IObservable<T> Result
        {
            get
            {
                return result;
            }
        }

        public override IObservable<Unit> EvaluateFunc()
        {
            var ret = f().Multicast(result);
            ret.Connect();

            return ret.Select(_ => Unit.Default);
        }
    }
    
    internal class OperationsQueue
    {
        static IObservable<Operation> ProcessOperation(Operation operation)
        {
            return Observable.Defer(operation.EvaluateFunc)
                .Select(_ => operation)
                .Catch(Observable.Return(operation));
        }

        private readonly Subject<Operation> queuedOps = new Subject<Operation>();
        readonly IConnectableObservable<Operation> resultObs;
        AsyncSubject<Unit> shutdownObs;

        internal OperationsQueue()
        {
            resultObs = queuedOps
                .Select(ProcessOperation)
                .Concat()
                .Multicast(new Subject<Operation>());

            resultObs.Connect();
        }

        public IObservable<T> EnqueueOperation<T>(Func<IObservable<T>> asyncCalculationFunc)
        {
            var item = Operation<T>.Create(asyncCalculationFunc);
            queuedOps.OnNext(item);
            return item.Result;
        }

        public Task<T> EnqueueOperation<T>(Func<T> calculationFunc, IScheduler scheduler)
        {
            return EnqueueOperation(() => SafeStart(calculationFunc, scheduler)).ToTask();
        }

        public Task<T> EnqueueOperation<T>(Func<T> calculationFunc, IScheduler scheduler, CancellationToken cancellationToken)
        {
            return EnqueueOperation(() => SafeStart(calculationFunc, scheduler)).ToTask(cancellationToken);
        }

        public Task Shutdown()
        {
            lock (queuedOps)
            {
                if (shutdownObs != null) return shutdownObs.AsObservable().ToTask();

                queuedOps.OnCompleted();

                shutdownObs = new AsyncSubject<Unit>();
                var sub = resultObs.Materialize()
                    .Where(x => x.Kind != NotificationKind.OnNext)
                    .SelectMany(x =>
                        (x.Kind == NotificationKind.OnError) ?
                            Observable.Throw<Unit>(x.Exception) :
                            Observable.Return(Unit.Default))
                    .Multicast(shutdownObs);

                sub.Connect();

                return shutdownObs.AsObservable().ToTask();
            }
        }

        private IObservable<T> SafeStart<T>(Func<T> calculationFunc, IScheduler scheduler)
        {
            var ret = new AsyncSubject<T>();
            Observable.Start(() =>
            {
                try
                {
                    var val = calculationFunc();
                    ret.OnNext(val);
                    ret.OnCompleted();
                }
                catch (Exception ex)
                {
                    ret.OnError(ex);
                }
            }, scheduler);

            return ret;
        }
    }
}

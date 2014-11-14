using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    public static class AsyncStatement
    {
        public static IObservable<T> Use<T>(this IAsyncStatement This, Func<IStatement, IEnumerable<T>> f)
        {
            Contract.Requires(This != null);
            Contract.Requires(f != null);

            return This.Use(f, AsyncDatabaseConnection.defaultScheduler);
        }

        public static Task Reset(this IAsyncStatement This, IScheduler scheduler, CancellationToken cancellationToken)
        {
            return This.Use(stmt =>
                {
                    ((IEnumerator)stmt).Reset();
                    return Enumerable.Empty<Unit>();
                }, scheduler).ToTask(cancellationToken);
        }

        public static Task Reset(this IAsyncStatement This)
        {
            return Reset(This, AsyncDatabaseConnection.defaultScheduler, CancellationToken.None);
        }

        public static Task Reset(this IAsyncStatement This, IScheduler scheduler)
        {
            return Reset(This, scheduler, CancellationToken.None);
        }

        public static Task Reset(this IAsyncStatement This, CancellationToken cancellationToken)
        {
            return Reset(This, AsyncDatabaseConnection.defaultScheduler, cancellationToken);
        }
    }

    internal class AsyncStatementImpl : IAsyncStatement
    {
        private readonly IStatement stmt;
        private readonly IAsyncDatabaseConnection conn;

        private volatile bool disposed = false;

        internal AsyncStatementImpl(IStatement stmt, IAsyncDatabaseConnection conn)
        {
            this.stmt = stmt;
            this.conn = conn;
        }

        public IObservable<T> Use<T>(Func<IStatement, IEnumerable<T>> f, IScheduler scheduler)
        {
            Contract.Requires(f != null);
            Contract.Requires(scheduler != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return conn.Use(_ => f(this.stmt), scheduler);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            // FIXME: Can this deadlock?
            disposed = true;
            conn.Use( _  =>
                {
                    stmt.Dispose();
                }, Scheduler.CurrentThread).Wait();
        }
    }
}

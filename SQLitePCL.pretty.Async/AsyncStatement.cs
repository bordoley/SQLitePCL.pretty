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
        public static Task Reset(this IAsyncStatement This, CancellationToken cancellationToken)
        {
            return This.Use(stmt =>
                {
                    ((IEnumerator)stmt).Reset();
                    return Enumerable.Empty<Unit>();
                }).ToTask(cancellationToken);
        }

        public static Task Reset(this IAsyncStatement This)
        {
            return Reset(This, CancellationToken.None);
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

        public IObservable<T> Use<T>(Func<IStatement, IEnumerable<T>> f)
        {
            Contract.Requires(f != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return conn.Use(_ => f(this.stmt));
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            conn.Use( _  =>
                {
                    stmt.Dispose();
                }).Wait();
        }
    }
}

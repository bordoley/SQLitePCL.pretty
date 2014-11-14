/*
   Copyright 2014 David Bordoley

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    public static class AsyncDatabaseConnection
    {
        internal static readonly IScheduler defaultScheduler = TaskPoolScheduler.Default;

        public static IAsyncDatabaseConnection AsAsyncDatabaseConnection(this IDatabaseConnection This)
        {
            Contract.Requires(This != null);
            return new AsyncDatabaseConnectionImpl(This);
        }

        public static Task Use(
            this IAsyncDatabaseConnection This,
            Action<IDatabaseConnection> f, 
            IScheduler scheduler, 
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(f != null);
            Contract.Requires(scheduler != null);

            return This.Use(conn =>
                {
                    f(conn);
                    return Enumerable.Empty<Unit>(); ;
                }, scheduler).ToTask(cancellationToken);
        }

        public static Task Use(this IAsyncDatabaseConnection This, Action<IDatabaseConnection> f)
        {
            return Use(This, f, defaultScheduler, CancellationToken.None);
        }

        public static Task Use(this IAsyncDatabaseConnection This, Action<IDatabaseConnection> f, CancellationToken cancellationToken)
        {
            return Use(This, f, defaultScheduler, cancellationToken);
        }

        public static Task Use(this IAsyncDatabaseConnection This, Action<IDatabaseConnection> f, IScheduler scheduler)
        {
            return Use(This, f, scheduler, CancellationToken.None);
        }

        public static Task<T> Use<T>(
            this IAsyncDatabaseConnection This, 
            Func<IDatabaseConnection, T> f,
            IScheduler scheduler, 
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(f != null);
            Contract.Requires(scheduler != null);

            return This.Use(conn => Enumerable.Repeat(f(conn), 1), scheduler).ToTask(cancellationToken);
        }

        public static Task<T> Use<T>(this IAsyncDatabaseConnection This, Func<IDatabaseConnection, T> f)
        {
            return Use(This, f, defaultScheduler, CancellationToken.None);
        }

        public static Task<T> Use<T>(this IAsyncDatabaseConnection This, Func<IDatabaseConnection, T> f, CancellationToken cancellationToken)
        {
            return Use(This, f, defaultScheduler, cancellationToken);
        }

        public static Task<T> Use<T>(this IAsyncDatabaseConnection This, Func<IDatabaseConnection, T> f, IScheduler scheduler)
        {
            return Use(This, f, scheduler, CancellationToken.None);
        }

        public static IObservable<T> Use<T>(this IAsyncDatabaseConnection This, Func<IDatabaseConnection, IEnumerable<T>> f)
        {
            return This.Use(f, defaultScheduler);
        }

        public static Task ExecuteAll(
            this IAsyncDatabaseConnection This,
            string sql,
            IScheduler scheduler,
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(scheduler != null);
            
            // FIXME: I think this could actually honor the cancellation token if it was implemented in a way to return an enumerable
            return This.Use(conn =>
            {
                conn.ExecuteAll(sql);
            }, scheduler, cancellationToken);
        }

        public static Task ExecuteAll(this IAsyncDatabaseConnection This, string sql)
        {
            return ExecuteAll(This, sql, defaultScheduler, CancellationToken.None);
        }

        public static Task ExecuteAll(this IAsyncDatabaseConnection This, string sql, CancellationToken cancellationToken)
        {
            return ExecuteAll(This, sql, defaultScheduler, cancellationToken);
        }

        public static Task ExecuteAll(this IAsyncDatabaseConnection This, string sql, IScheduler scheduler)
        {
            return ExecuteAll(This, sql, scheduler, CancellationToken.None);
        }

        public static Task Execute(
            this IAsyncDatabaseConnection This, 
            string sql, 
            IScheduler scheduler,
            CancellationToken cancellationToken,
            params object[] a)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(scheduler != null);
            Contract.Requires(a != null);

            return This.Use(conn =>
                {
                    conn.Execute(sql, a);
                }, scheduler, cancellationToken);
        }

        public static Task Execute(this IAsyncDatabaseConnection This, string sql, params object[] a)
        {
            return Execute(This, sql, defaultScheduler, CancellationToken.None, a);
        }

        public static Task Execute(this IAsyncDatabaseConnection This, string sql, IScheduler scheduler, params object[] a)
        {
            return Execute(This, sql, scheduler, CancellationToken.None, a);
        }

        public static Task Execute(this IAsyncDatabaseConnection This, string sql, CancellationToken cancellationToken, params object[] a)
        {
            return Execute(This, sql, defaultScheduler, cancellationToken, a);
        }

        public static Task Execute(
            this IAsyncDatabaseConnection This,
            string sql,
            IScheduler scheduler,
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(scheduler != null);

            return This.Use(conn =>
            {
                conn.Execute(sql);
            }, scheduler, cancellationToken);
        }

        public static Task Execute(this IAsyncDatabaseConnection This, string sql)
        {
            return Execute(This, sql, defaultScheduler, CancellationToken.None);
        }

        public static Task Execute(this IAsyncDatabaseConnection This, string sql, IScheduler scheduler)
        {
            return Execute(This, sql, scheduler, CancellationToken.None);
        }

        public static Task Execute(this IAsyncDatabaseConnection This, string sql, CancellationToken cancellationToken)
        {
            return Execute(This, sql, defaultScheduler, cancellationToken);
        }

        public static Task<IAsyncStatement> PrepareStatement(
           this IAsyncDatabaseConnection This,
           string sql,
           IScheduler scheduler,
           CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(scheduler != null);

            return Use<IAsyncStatement>(This, conn =>
            {
                var stmt = conn.PrepareStatement(sql);
                return new AsyncStatementImpl(stmt, This);
            });
        }

        public static Task<IAsyncStatement> PrepareStatement(this IAsyncDatabaseConnection This, string sql)
        {
            return PrepareStatement(This, sql, defaultScheduler, CancellationToken.None);
        }

        public static Task<IAsyncStatement> PrepareStatement(this IAsyncDatabaseConnection This, string sql, IScheduler scheduler)
        {
            return PrepareStatement(This, sql, scheduler, CancellationToken.None);
        }

        public static Task<IAsyncStatement> PrepareStatement(this IAsyncDatabaseConnection This, string sql, CancellationToken cancellationToken)
        {
            return PrepareStatement(This, sql, defaultScheduler, cancellationToken);
        }

        public static Task<Tuple<IAsyncStatement, string>> PrepareStatementWithTail(
            this IAsyncDatabaseConnection This,
            string sql,
            IScheduler scheduler,
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(scheduler != null);

            return Use(This, conn =>
                {
                    string tail = null;
                    var stmt = conn.PrepareStatement(sql, out tail);
                    return Tuple.Create<IAsyncStatement, string>(new AsyncStatementImpl(stmt, This), tail);
                });
        }

        public static Task<Tuple<IAsyncStatement, string>> PrepareStatementWithTail(
            this IAsyncDatabaseConnection This,
            string sql)
        {
            return PrepareStatementWithTail(This, sql, defaultScheduler, CancellationToken.None);
        }

        public static Task<Tuple<IAsyncStatement, string>> PrepareStatementWithTail(
            this IAsyncDatabaseConnection This,
            string sql,
            IScheduler scheduler)
        {
            return PrepareStatementWithTail(This, sql, scheduler, CancellationToken.None);
        }

        public static Task<Tuple<IAsyncStatement, string>> PrepareStatementWithTail(
            this IAsyncDatabaseConnection This,
            string sql,
            CancellationToken cancellationToken)
        {
            return PrepareStatementWithTail(This, sql, defaultScheduler, cancellationToken);
        }

        public static IObservable<T> Query<T>(
            this IAsyncDatabaseConnection This, 
            string sql, 
            Func<IReadOnlyList<IResultSetValue>, T> selector, 
            IScheduler scheduler,
            params object[] a)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(selector != null);
            Contract.Requires(scheduler != null);
            Contract.Requires(a != null);

            return This.Use(conn =>
            {
                return conn.Query(sql, a).Select(selector);
            }, scheduler);
        }

        public static IObservable<T> Query<T>(this IAsyncDatabaseConnection This, string sql, Func<IReadOnlyList<IResultSetValue>, T> selector, params object[] a)
        {
            return Query(This, sql, selector, defaultScheduler, a);
        }

        public static IObservable<T> Query<T>(
            this IAsyncDatabaseConnection This,
            string sql,
            Func<IReadOnlyList<IResultSetValue>, T> selector,
            IScheduler scheduler)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(selector != null);
            Contract.Requires(scheduler != null);

            return This.Use(conn =>
            {
                return conn.Query(sql).Select(selector);
            }, scheduler);
        }

        public static IObservable<T> Query<T>(this IAsyncDatabaseConnection This, string sql, Func<IReadOnlyList<IResultSetValue>, T> selector)
        {
            return Query(This, sql, selector, defaultScheduler);
        }
    }

    internal sealed class AsyncDatabaseConnectionImpl : IAsyncDatabaseConnection
    {
        private readonly OperationsQueue queue = new OperationsQueue();

        private readonly IDatabaseConnection conn;
        private readonly IObservable<DatabaseTraceEventArgs> trace;
        private readonly IObservable<DatabaseProfileEventArgs> profile;
        private readonly IObservable<DatabaseUpdateEventArgs> update;

        private volatile bool disposed = false;

        internal AsyncDatabaseConnectionImpl(IDatabaseConnection conn)
        {
            this.conn = conn;
            this.trace = Observable.FromEventPattern<DatabaseTraceEventArgs>(conn, "Trace").Select(e => e.EventArgs);
            this.profile = Observable.FromEventPattern<DatabaseProfileEventArgs>(conn, "Profile").Select(e => e.EventArgs);
            this.update = Observable.FromEventPattern<DatabaseUpdateEventArgs>(conn, "Update").Select(e => e.EventArgs);
        }

        public IObservable<DatabaseTraceEventArgs> Trace
        {
            get 
            {
                return trace;
            }
        }

        public IObservable<DatabaseProfileEventArgs> Profile
        {
            get
            {
                return profile;
            }
        }

        public IObservable<DatabaseUpdateEventArgs> Update
        {
            get 
            {
                return update;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            // FIXME: Can this deadlock?
            this.disposed = true;
            queue.EnqueueOperation(() =>
            {
                this.conn.Dispose();
                return Unit.Default;
            }, Scheduler.CurrentThread).Wait();
        }

        public IObservable<T> Use<T>(Func<IDatabaseConnection, IEnumerable<T>> f, IScheduler scheduler)
        {
            Contract.Requires(f != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() => f(this.conn).ToObservable(scheduler));
        }
    }
}

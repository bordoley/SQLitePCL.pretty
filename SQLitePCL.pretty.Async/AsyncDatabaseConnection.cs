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
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    public static class AsyncDatabaseConnection
    {
        public static IAsyncDatabaseConnection AsAsyncDatabaseConnection(this IDatabaseConnection This)
        {
            Contract.Requires(This != null);
            return new AsyncDatabaseConnectionImpl(This);
        }

        public static Task Use(this IAsyncDatabaseConnection This, Action<IDatabaseConnection> f)
        {
            Contract.Requires(This != null);
            Contract.Requires(f != null);

            return This.Use(conn =>
                {
                    f(conn);
                    return Unit.Default;
                });
        }

        public static Task ExecuteAll(this IAsyncDatabaseConnection This, string sql)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            return This.Use(conn =>
            {
                conn.ExecuteAll(sql);
            });
        }

        public static Task Execute(this IAsyncDatabaseConnection This, string sql, params object[] a)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(a != null);

            return This.Use(conn =>
                {
                    conn.Execute(sql, a);
                });
        }

        public static Task<string> GetFilenameAsync(this IAsyncDatabaseConnection This, string database)
        {
            Contract.Requires(This != null);
            Contract.Requires(database != null);

            return This.Use(conn =>
                {
                    return conn.GetFileName(database);
                });
        }

        public static IObservable<T> QueryAndSelect<T>(this IAsyncDatabaseConnection This, string sql, Func<IReadOnlyList<IResultSetValue>, T> selector, params object[] a)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(selector != null);
            Contract.Requires(a != null);

            return This.Use(conn =>
            {
                return conn.Query(sql, a).Select(selector).ToObservable();
            });
        }
    }

    internal sealed class AsyncDatabaseConnectionImpl : IAsyncDatabaseConnection
    {
        private readonly OperationsQueue queue = new OperationsQueue(TaskPoolScheduler.Default);

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

            this.disposed = true;
            queue.EnqueueOperation(() =>
            {
                this.conn.Dispose();
            }).Wait();
        }

        public IObservable<T> Use<T>(Func<IDatabaseConnection, IObservable<T>> f)
        {
            Contract.Requires(f != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueObservableOperation(() => f(this.conn));
        }

        public Task<T> Use<T>(Func<IDatabaseConnection, T> f)
        {
            Contract.Requires(f != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() => f(this.conn));
        }

        public Task<Tuple<IAsyncStatement, string>> PrepareStatement(string sql)
        {
            Contract.Requires(sql != null);

            return this.Use(conn =>
            {
                string tail = null;
                var stmt = conn.PrepareStatement(sql, out tail);
                return Tuple.Create<IAsyncStatement, string>(new AsyncStatementImpl(stmt, queue), tail);
            });
        }
    }
}

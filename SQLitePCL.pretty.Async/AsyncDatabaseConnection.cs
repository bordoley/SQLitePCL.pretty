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
using System.IO;
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
        private static IScheduler defaultScheduler = TaskPoolScheduler.Default;
        public static IScheduler DefaultScheduler 
        { 
            set
            {
                defaultScheduler = value;
            }
        }

        public static IAsyncDatabaseConnection AsAsyncDatabaseConnection(this IDatabaseConnection This, IScheduler scheduler)
        {
            Contract.Requires(This != null);
            Contract.Requires(scheduler != null);
            return new AsyncDatabaseConnectionImpl(This, scheduler);
        }

        public static IAsyncDatabaseConnection AsAsyncDatabaseConnection(this IDatabaseConnection This)
        {
            return AsAsyncDatabaseConnection(This, defaultScheduler);
        }

        public static Task ExecuteAllAsync(
            this IAsyncDatabaseConnection This,
            string sql,
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            
            // FIXME: I think this could actually honor the cancellation token if it was implemented in a way to return an enumerable
            return This.Use(conn =>
            {
                conn.ExecuteAll(sql);
            }, cancellationToken);
        }

        public static Task ExecuteAllAsync(this IAsyncDatabaseConnection This, string sql)
        {
            return ExecuteAllAsync(This, sql, CancellationToken.None);
        }

        public static Task ExecuteAsync(
            this IAsyncDatabaseConnection This, 
            string sql, 
            CancellationToken cancellationToken,
            params object[] a)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(a != null);

            return This.Use(conn =>
                {
                    conn.Execute(sql, a);
                }, cancellationToken);
        }

        public static Task ExecuteAsync(this IAsyncDatabaseConnection This, string sql, params object[] a)
        {
            return ExecuteAsync(This, sql, CancellationToken.None, a);
        }

        public static Task ExecuteAsync(
            this IAsyncDatabaseConnection This,
            string sql,
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            return This.Use(conn =>
            {
                conn.Execute(sql);
            }, cancellationToken);
        }

        public static Task ExecuteAsync(this IAsyncDatabaseConnection This, string sql)
        {
            return ExecuteAsync(This, sql, CancellationToken.None);
        }

        public static Task<Stream> OpenBlobAsync(
            this IAsyncDatabaseConnection This,
            string database,
            string tableName,
            string columnName,
            long rowId,
            bool canWrite = false)
        {
            return OpenBlobAsync(This, database, tableName, columnName, rowId, CancellationToken.None, canWrite);
        }

        public static Task<Stream> OpenBlobAsync(
            this IAsyncDatabaseConnection This, 
            string database,
            string tableName, 
            string columnName, 
            long rowId,
            CancellationToken cancellationToken,
            bool canWrite = false)
        {
            return This.Use(db =>
                {
                    return db.OpenBlob(database, tableName, columnName, rowId, canWrite);
                }, cancellationToken);
        }

        public static Task<IAsyncStatement> PrepareStatementAsync(
           this IAsyncDatabaseConnection This,
           string sql,
           CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            return Use<IAsyncStatement>(This, conn =>
            {
                var stmt = conn.PrepareStatement(sql);
                return new AsyncStatementImpl(stmt, This);
            }, cancellationToken);
        }

        public static Task<IAsyncStatement> PrepareStatementAsync(this IAsyncDatabaseConnection This, string sql)
        {
            return PrepareStatementAsync(This, sql, CancellationToken.None);
        }

        public static Task<Tuple<IAsyncStatement, string>> PrepareStatementWithTailAsync(
            this IAsyncDatabaseConnection This,
            string sql,
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            return Use(This, conn =>
                {
                    string tail = null;
                    var stmt = conn.PrepareStatement(sql, out tail);
                    return Tuple.Create<IAsyncStatement, string>(new AsyncStatementImpl(stmt, This), tail);
                }, cancellationToken);
        }

        public static Task<Tuple<IAsyncStatement, string>> PrepareStatementWithTailAsync(
            this IAsyncDatabaseConnection This,
            string sql)
        {
            return PrepareStatementWithTailAsync(This, sql, CancellationToken.None);
        }

        public static IObservable<T> Query<T>(
            this IAsyncDatabaseConnection This, 
            string sql, 
            Func<IReadOnlyList<IResultSetValue>, T> selector, 
            params object[] a)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(selector != null);
            Contract.Requires(a != null);

            return This.Use(conn =>
            {
                return conn.Query(sql, a).Select(selector);
            });
        }

        public static IObservable<T> Query<T>(
            this IAsyncDatabaseConnection This,
            string sql,
            Func<IReadOnlyList<IResultSetValue>, T> selector)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(selector != null);

            return This.Use(conn =>
            {
                return conn.Query(sql).Select(selector);
            });
        }

        public static Task Use(
            this IAsyncDatabaseConnection This,
            Action<IDatabaseConnection> f,
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(f != null);

            return This.Use(conn =>
            {
                f(conn);
                return Enumerable.Empty<Unit>();
            }, cancellationToken);
        }

        public static Task Use(this IAsyncDatabaseConnection This, Action<IDatabaseConnection> f)
        {
            return Use(This, f, CancellationToken.None);
        }

        public static Task<T> Use<T>(
            this IAsyncDatabaseConnection This,
            Func<IDatabaseConnection, T> f,
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(f != null);

            return This.Use(conn => Enumerable.Repeat(f(conn), 1)).ToTask(cancellationToken);
        }

        public static Task<T> Use<T>(this IAsyncDatabaseConnection This, Func<IDatabaseConnection, T> f)
        {
            return Use(This, f, CancellationToken.None);
        }
    }

    internal sealed class AsyncDatabaseConnectionImpl : IAsyncDatabaseConnection
    {
        private readonly OperationsQueue queue = new OperationsQueue();
        private readonly IScheduler scheduler;

        private readonly IDatabaseConnection conn;
        private readonly IObservable<DatabaseTraceEventArgs> trace;
        private readonly IObservable<DatabaseProfileEventArgs> profile;
        private readonly IObservable<DatabaseUpdateEventArgs> update;

        private volatile bool disposed = false;

        internal AsyncDatabaseConnectionImpl(IDatabaseConnection conn, IScheduler scheduler)
        {
            this.conn = conn;
            this.scheduler = scheduler;

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
            this.queue.Shutdown().Wait();
            this.conn.Dispose();
        }

        public IObservable<T> Use<T>(Func<IDatabaseConnection, IEnumerable<T>> f)
        {
            Contract.Requires(f != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return Observable.Create((IObserver<T> observer, CancellationToken cancellationToken) =>
                {
                    return queue.EnqueueOperation(() =>
                        {
                            var cancellationTokenRegistration = cancellationToken.Register(() => { /* this.conn.Interrupt() */});
                               
                            try
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                foreach (var e in f(new DatabaseConnectionWrapper(this.conn)))
                                {  
                                    observer.OnNext(e);
                                    cancellationToken.ThrowIfCancellationRequested();
                                }
                                observer.OnCompleted();
                             }
                             catch (Exception ex)
                             {
                                observer.OnError(ex);
                             }
                             finally
                             {
                                cancellationTokenRegistration.Dispose();
                             }
                             return Unit.Default;
                       }, scheduler, cancellationToken);
               });
        }
    }

    internal sealed class DatabaseConnectionWrapper : IDatabaseConnection
    {
        private readonly IDatabaseConnection db;

        internal DatabaseConnectionWrapper(IDatabaseConnection db)
        {
            this.db = db;
        }

        public event EventHandler Rollback
        {
            add { throw new NotSupportedException(); }
            remove { throw new NotSupportedException(); }
        }

        public event EventHandler<DatabaseTraceEventArgs> Trace
        {
            add { throw new NotSupportedException(); }
            remove { throw new NotSupportedException(); }
        }

        public event EventHandler<DatabaseProfileEventArgs> Profile
        {
            add { throw new NotSupportedException(); }
            remove { throw new NotSupportedException(); }
        }

        public event EventHandler<DatabaseUpdateEventArgs> Update
        {
            add { throw new NotSupportedException(); }
            remove { throw new NotSupportedException(); }
        }

        public bool IsAutoCommit
        {
            get 
            {
                return db.IsAutoCommit;
            }
        }

        public TimeSpan BusyTimeout
        {
            set { throw new NotSupportedException(); }
        }

        public int Changes
        {
            get
            {
                return db.Changes;
            }
        }

        public long LastInsertedRowId
        {
            get 
            {
                return db.LastInsertedRowId;
            }
        }

        public IEnumerable<IStatement> Statements
        {
            get 
            {
                return db.Statements;
            }
        }

        public bool TryGetFileName(string database, out string filename)
        {
            return db.TryGetFileName(database, out filename);
        }

        public Stream OpenBlob(string database, string tableName, string columnName, long rowId, bool canWrite)
        {
            return db.OpenBlob(database, tableName, columnName, rowId, canWrite);
        }

        public IStatement PrepareStatement(string sql, out string tail)
        {
            return db.PrepareStatement(sql, out tail);
        }

        public void RegisterCollation(string name, Comparison<string> comparison)
        {
            throw new NotSupportedException();
        }

        public void RegisterCommitHook(Func<bool> onCommit)
        {
            throw new NotSupportedException();
        }

        public void RegisterAggregateFunc<T>(string name, int nArg, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            throw new NotSupportedException();
        }

        public void RegisterScalarFunc(string name, int nArg, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            // No-op
        }
    }
}

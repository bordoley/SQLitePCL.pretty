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
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    internal class WriteLockedRef<T>
    {
        private readonly object gate = new object();
        private T value;

        public WriteLockedRef(T defaultValue)
        {
            this.value = defaultValue;
        }

        public T Value
        {
            get { return value; }

            set 
            {
                lock (gate)
                {
                    this.value = value;
                }
            }
        }

    }

    /// <summary>
    /// SQLiteDatabaseConnectionBuilder extension functions.
    /// </summary>
    public static class SQLiteDatabaseConnectionBuilderExtensions
    {
        /// <summary>
        /// Builds an IAsyncDatabaseConnection using the specified scheduler.
        /// </summary>
        /// <returns>An IAsyncDatabaseConnection using the specified scheduler.</returns>
        /// <param name="This">A SQLiteDatabaseConnectionBuilder instance.</param>
        /// <param name="scheduler">An RX scheduler</param>
        public static IAsyncDatabaseConnection BuildAsyncDatabaseConnection(
            this SQLiteDatabaseConnectionBuilder This,
            IScheduler scheduler)
        {
            Contract.Requires(This != null);
            Contract.Requires(scheduler != null);

            var builder = This.Clone();

            var progressHandlerResult = new WriteLockedRef<bool>(false);
            builder.ProgressHandler = () => progressHandlerResult.Value;
            var db = This.Build();

            return new AsyncDatabaseConnectionImpl(db, scheduler, progressHandlerResult);
        }

        /// <summary>
        /// Builds an IAsyncDatabaseConnection using the default TaskPool scheduler.
        /// </summary>
        /// <returns>An IAsyncDatabaseConnection using the default TaskPool scheduler.</returns>
        /// <param name="This">A SQLiteDatabaseConnectionBuilder instance.</param>
        public static IAsyncDatabaseConnection BuildAsyncDatabaseConnection(this SQLiteDatabaseConnectionBuilder This) =>
            This.BuildAsyncDatabaseConnection(TaskPoolScheduler.Default);
    }
        
    /// <summary>
    /// Extensions methods for <see cref="IAsyncDatabaseConnection"/>.
    /// </summary>
    public static class AsyncDatabaseConnection
    {
        /// <summary>
        /// Compiles and executes multiple SQL statements.
        /// </summary>
        /// <param name="This">An asynchronous database connection.</param>
        /// <param name="sql">One or more semicolon delimited SQL statements.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the task.</param>
        /// <returns>A task that completes when all statements have been executed.</returns>
        public static Task ExecuteAllAsync(
            this IAsyncDatabaseConnection This,
            string sql,
            CancellationToken cancellationToken)
        {
            Contract.Requires(sql != null);

            return This.Use((conn, ct) => conn.ExecuteAll(sql), cancellationToken);
        }

        /// <summary>
        /// Compiles and executes multiple SQL statements.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">One or more semicolon delimited SQL statements.</param>
        /// <returns>A task that completes when all statements have been executed.</returns>
        public static Task ExecuteAllAsync(this IAsyncDatabaseConnection This, string sql) =>
            This.ExecuteAllAsync(sql, CancellationToken.None);

        /// <summary>
        /// Compiles and executes a SQL statement with the provided bind parameter values.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">The SQL statement to compile and execute.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the task.</param>
        /// <param name="values">The bind parameter values.</param>
        /// <returns>A task that completes when the statement has been executed.</returns>
        public static Task ExecuteAsync(
            this IAsyncDatabaseConnection This,
            string sql,
            CancellationToken cancellationToken,
            params object[] values)
        {
            Contract.Requires(sql != null);
            Contract.Requires(values != null);

            return This.Use((conn, ct) => conn.Execute(sql, values), cancellationToken);
        }

        /// <summary>
        /// Compiles and executes a SQL statement with the provided bind parameter values.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">The SQL statement to compile and execute.</param>
        /// <param name="values">The bind parameter values.</param>
        /// <returns>A task that completes when the statement has been executed.</returns>
        public static Task ExecuteAsync(this IAsyncDatabaseConnection This, string sql, params object[] values) =>
            This.ExecuteAsync(sql, CancellationToken.None, values);

        /// <summary>
        /// Compiles and executes a SQL statement.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">The SQL statement to compile and execute.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the task.</param>
        /// <returns>A task that completes when the statement has been executed.</returns>
        public static Task ExecuteAsync(
            this IAsyncDatabaseConnection This,
            string sql,
            CancellationToken cancellationToken)
        {
            Contract.Requires(sql != null);

            return This.Use((conn, ct) => conn.Execute(sql), cancellationToken);
        }

        /// <summary>
        /// Compiles and executes a SQL statement.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">The SQL statement to compile and execute.</param>
        /// <returns>A task that completes when the statement has been executed.</returns>
        public static Task ExecuteAsync(this IAsyncDatabaseConnection This, string sql) =>
            This.ExecuteAsync(sql, CancellationToken.None);

        /// <summary>
        /// Opens the blob located by the a database, table, column, and rowid for incremental asynchronous I/O as a <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="database">The database containing the blob.</param>
        /// <param name="tableName">The table containing the blob.</param>
        /// <param name="columnName">The column containing the blob.</param>
        /// <param name="rowId">The row containing the blob.</param>
        /// <param name="canWrite">
        ///     <see langwords="true"/> if the Stream should be open for both read and write operations.
        ///     <see langwords="false"/> if the Stream should be open oly for read operations.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes with a <see cref="System.IO.Stream"/> that can be used to asynchronously write and read to and from blob.</returns>
        public static Task<Stream> OpenBlobAsync(
                this IAsyncDatabaseConnection This,
                string database,
                string tableName,
                string columnName,
                long rowId,
                bool canWrite = false) =>
            This.OpenBlobAsync(database, tableName, columnName, rowId, canWrite, CancellationToken.None);

        /// <summary>
        /// Opens the blob located by the a database, table, column, and rowid for incremental asynchronous I/O as a <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="database">The database containing the blob.</param>
        /// <param name="tableName">The table containing the blob.</param>
        /// <param name="columnName">The column containing the blob.</param>
        /// <param name="rowId">The row containing the blob.</param>
        /// <param name="canWrite">
        ///     <see langwords="true"/> if the Stream should be open for both read and write operations.
        ///     <see langwords="false"/> if the Stream should be open oly for read operations.
        /// </param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the task.</param>
        /// <returns>A <see cref="Task"/> that completes with a <see cref="System.IO.Stream"/> that can be used to asynchronously write and read to and from blob.</returns>
        public static Task<Stream> OpenBlobAsync(
            this IAsyncDatabaseConnection This,
            string database,
            string tableName,
            string columnName,
            long rowId,
            bool canWrite,
            CancellationToken cancellationToken)
        {
            Contract.Requires(database != null);
            Contract.Requires(tableName != null);
            Contract.Requires(columnName != null);

            return This.Use<Stream>((db, ct) =>
                {
                    var blob = db.OpenBlob(database, tableName, columnName, rowId, canWrite);
                    return new AsyncBlobStream(blob, This);
                }, cancellationToken);
        }

        /// <summary>
        ///  Opens the blob located by the a database, table, column, and rowid for incremental asynchronous I/O as a <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="columnInfo">The ColumnInfo of the blob value.</param>
        /// <param name="rowId">The row containing the blob.</param>
        /// <param name="canWrite">
        ///     <see langwords="true"/> if the Stream should be open for both read and write operations.
        ///     <see langwords="false"/> if the Stream should be open oly for read operations.
        /// </param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the task.</param>
        /// <returns>A <see cref="Task"/> that completes with a <see cref="System.IO.Stream"/> that can be used to asynchronously write and read to and from blob.</returns>
        public static Task<Stream> OpenBlobAsync(
            this IAsyncDatabaseConnection This,
            ColumnInfo columnInfo,
            long rowId,
            bool canWrite,
            CancellationToken cancellationToken)
        {
            Contract.Requires(columnInfo != null);
            return This.OpenBlobAsync(columnInfo.DatabaseName, columnInfo.TableName, columnInfo.OriginName, rowId, canWrite, cancellationToken);
        }

        /// <summary>
        ///  Opens the blob located by the a database, table, column, and rowid for incremental asynchronous I/O as a <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="columnInfo">The ColumnInfo of the blob value.</param>
        /// <param name="rowId">The row containing the blob.</param>
        /// <param name="canWrite">
        ///     <see langwords="true"/> if the Stream should be open for both read and write operations.
        ///     <see langwords="false"/> if the Stream should be open oly for read operations.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes with a <see cref="System.IO.Stream"/> that can be used to asynchronously write and read to and from blob.</returns>
        public static Task<Stream> OpenBlobAsync(
                this IAsyncDatabaseConnection This,
                ColumnInfo columnInfo,
                long rowId,
                bool canWrite = false) => 
            This.OpenBlobAsync(columnInfo, rowId, canWrite, CancellationToken.None);

        /// <summary>
        /// Compiles one or more SQL statements.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">One or more semicolon delimited SQL statements.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the task.</param>
        /// <returns>A <see cref="Task"/> that completes with a <see cref="IReadOnlyList&lt;T&gt;"/>
        /// of the compiled <see cref="IAsyncStatement"/>instances.</returns>
        public static Task<IReadOnlyList<IAsyncStatement>> PrepareAllAsync(
           this IAsyncDatabaseConnection This,
           string sql,
           CancellationToken cancellationToken)
        {
            Contract.Requires(sql != null);

            return This.Use<IReadOnlyList<IAsyncStatement>>((conn, ct) =>
                {
                    // Eagerly prepare all the statements. The synchronous version of PrepareAll()
                    // is lazy, preparing each statement when MoveNext() is called on the Enumerator.
                    // Hence an implementation like:
                    //
                    //   return conn.PrepareAll(sql).Select(stmt => new AsyncStatementImpl(stmt, This));
                    //
                    // would result in unintentional database access not on the operations queue.
                    // Added bonus of being eager: Callers can retrieve individual statements via
                    // the index in the list.
                    return conn.PrepareAll(sql).Select(stmt => new AsyncStatementImpl(stmt, This)).ToList();
                }, cancellationToken);
        }

        /// <summary>
        /// Compiles one or more SQL statements.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">One or more semicolon delimited SQL statements.</param>
        /// <returns>A <see cref="Task"/> that completes with a <see cref="IReadOnlyList&lt;T&gt;"/>
        /// of the compiled <see cref="IAsyncStatement"/>instances.</returns>
        public static Task<IReadOnlyList<IAsyncStatement>> PrepareAllAsync(this IAsyncDatabaseConnection This, string sql) =>
            This.PrepareAllAsync(sql, CancellationToken.None);

        /// <summary>
        /// Compiles a SQL statement.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">The SQL statement to compile.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the task.</param>
        /// <returns>Task that completes with a <see cref="IAsyncStatement"/> that
        /// can be used to query the result set asynchronously.</returns>
        public static Task<IAsyncStatement> PrepareStatementAsync(
           this IAsyncDatabaseConnection This,
           string sql,
           CancellationToken cancellationToken)
        {
            Contract.Requires(sql != null);

            return This.Use<IAsyncStatement>((conn, ct) =>
            {
                var stmt = conn.PrepareStatement(sql);
                return new AsyncStatementImpl(stmt, This);
            }, cancellationToken);
        }

        /// <summary>
        /// Compiles a SQL statement.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">The SQL statement to compile.</param>
        /// <returns>Task that completes with a <see cref="IAsyncStatement"/> that
        /// can be used to query the result set asynchronously.</returns>
        public static Task<IAsyncStatement> PrepareStatementAsync(this IAsyncDatabaseConnection This, string sql) =>
            This.PrepareStatementAsync(sql, CancellationToken.None);

        /// <summary>
        /// Returns a cold observable that compiles a SQL statement with
        /// provided bind parameter values, that publishes the rows in the result
        /// set for each subscription.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">The SQL statement to compile and Query.</param>
        /// <param name="values">The bind parameter values.</param>
        /// <returns>A cold observable of rows in the result set.</returns>
        public static IObservable<IReadOnlyList<IResultSetValue>> Query(
            this IAsyncDatabaseConnection This,
            string sql,
            params object[] values)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(values != null);

            return This.Use((conn, ct) => conn.Query(sql, values));
        }

        /// <summary>
        /// Returns a cold observable that compiles a SQL statement
        /// that publishes the rows in the result set for each subscription.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">The SQL statement to compile and Query.</param>
        /// <returns>A cold observable of rows in the result set.</returns>
        public static IObservable<IReadOnlyList<IResultSetValue>> Query(
            this IAsyncDatabaseConnection This,
            string sql)
        {
            Contract.Requires(sql != null);

            return This.Use(conn => conn.Query(sql));
        }

        /// <summary>
        /// Schedules the <see cref="Action"/> <paramref name="f"/> on the database operations queue.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="f">The action.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the task.</param>
        /// <returns>A task that completes when <paramref name="f"/> returns.</returns>
        public static Task Use(
            this IAsyncDatabaseConnection This,
            Action<IDatabaseConnection, CancellationToken> f,
            CancellationToken cancellationToken)
        {
            Contract.Requires(f != null);

            return This.Use((conn, ct) =>
            {
                f(conn, ct);
                return Enumerable.Empty<Unit>();
            }, cancellationToken);
        }

        /// <summary>
        /// Schedules the <see cref="Action"/> <paramref name="f"/> on the database operations queue.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="f">The action.</param>
        /// <returns>A task that completes when <paramref name="f"/> returns.</returns>
        public static Task Use(this IAsyncDatabaseConnection This, Action<IDatabaseConnection> f)
        {
            Contract.Requires(f != null);

            return This.Use((db, ct) => f(db), CancellationToken.None);
        }

        /// <summary>
        /// Schedules the <see cref="Func&lt;T,TResult&gt;"/> <paramref name="f"/> on the database operations queue.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="f">A function from <see cref="IDatabaseConnection"/> to <typeparamref name="T"/>.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the task.</param>
        /// <returns>A task that completes with the result of <paramref name="f"/>.</returns>
        public static Task<T> Use<T>(
            this IAsyncDatabaseConnection This,
            Func<IDatabaseConnection, CancellationToken, T> f,
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(f != null);

            return This.Use((conn, ct) => new[] { f(conn, ct) }).ToTask(cancellationToken);
        }

        /// <summary>
        /// Schedules the <see cref="Func&lt;T,TResult&gt;"/> <paramref name="f"/> on the database operations queue.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="f">A function from <see cref="IDatabaseConnection"/> to <typeparamref name="T"/>.</param>
        /// <returns>A task that completes with the result of <paramref name="f"/>.</returns>
        public static Task<T> Use<T>(this IAsyncDatabaseConnection This, Func<IDatabaseConnection,T> f)
        {
            Contract.Requires(f != null);
            return This.Use((db, ct) => f(db), CancellationToken.None);
        }

        /// <summary>
        /// Returns a cold IObservable which schedules the function f on the database operation queue each
        /// time it is is subscribed to. The published values are generated by enumerating the IEnumerable returned by f.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="f">
        /// A function that may synchronously use the provided IDatabaseConnection and returns
        /// an IEnumerable of produced values that are published to the subscribed IObserver.
        /// The returned IEnumerable may block. This allows the IEnumerable to provide the results of
        /// enumerating a SQLite prepared statement for instance.
        /// </param>
        /// <returns>A cold observable of the values produced by the function f.</returns>
        public static IObservable<T> Use<T>(this IAsyncDatabaseConnection This, Func<IDatabaseConnection, IEnumerable<T>> f)
        {
            Contract.Requires(f != null);
            return This.Use((conn, ct) => f(conn));
        }
    }

    internal sealed class AsyncDatabaseConnectionImpl : IAsyncDatabaseConnection
    {
        private readonly OperationsQueue queue = new OperationsQueue();
        private readonly IScheduler scheduler;
        private readonly SQLiteDatabaseConnection conn;
        private readonly WriteLockedRef<bool> progressHandlerResult;

        private bool disposed = false;

        internal AsyncDatabaseConnectionImpl(SQLiteDatabaseConnection conn, IScheduler scheduler, WriteLockedRef<bool> progressHandlerResult)
        {
            this.conn = conn;
            this.scheduler = scheduler;
            this.progressHandlerResult = progressHandlerResult;

            this.Trace = Observable.FromEventPattern<DatabaseTraceEventArgs>(conn, "Trace").Select(e => e.EventArgs);
            this.Profile = Observable.FromEventPattern<DatabaseProfileEventArgs>(conn, "Profile").Select(e => e.EventArgs);
            this.Update = Observable.FromEventPattern<DatabaseUpdateEventArgs>(conn, "Update").Select(e => e.EventArgs);
        }

        public IObservable<DatabaseTraceEventArgs> Trace { get; }

        public IObservable<DatabaseProfileEventArgs> Profile { get; }

        public IObservable<DatabaseUpdateEventArgs> Update { get; }

        public async Task DisposeAsync()
        {
            if (disposed)
            {
                return;
            }

            this.disposed = true;
            await this.queue.DisposeAsync();

            // FIXME: This is a little broken. We really should track open Async
            // AsyncStatementImpl's and AsyncBlobStream's and dispose them here first.
            // Othewise those objects won't have their state correctly set to disposed,
            // leading to some user errors.
            // Two possible solutions: 
            // * Track the open AsyncStatementImpl's and AsyncBlobStream's
            // * Add a disposing event, and listen to it from AsyncStatementImpl's and AsyncBlobStream's.
            this.conn.Dispose();
        }

        // Yes async void is evil and ugly. This is essentially a compromise decision to avoid
        // common deadlock pitfalls. For instance, if Dispose was implemented as
        //
        //  public void Dispose()
        //  {
        //    this.DisposeAsync().Wait();
        //  }
        //
        // then this trivial example would deadlock when run on the task pool:
        //
        //   using (var db = SQLite3.OpenInMemoryDb().AsAsyncDatabaseConnection())
        //   {
        //     await db.Use(_ => { });
        //   }
        //
        // In this case, the task pool immediately schedules the call to Dispose() after the
        // Action completes on the same thread. This results in deadlock as the call to Dispose()
        // is waiting for the queue to indicate its empty, but the current running item on the
        // queue never completes since its blocked by the Dispose() call.
        //
        // A fix for this is available in .NET 4.5.3, which provides TaskCreationOptions.RunContinuationsAsynchronously
        // Unfortunately this is not, and will not for the forseable future be available in the PCL profile.
        // Also the RX implementation of ToTask() does not accept TaskCreationOptions but that could
        // be worked around with a custom implementation.
        //
        // Another fix considered but abandoned was to provide variant of Use() accepting an
        // IScheduler that can be used to ObserveOn(). However this would essentially double the
        // number of static methods in this class, and would only in practice be
        // useful in unit tests, as most client code will typically call these async
        // methods from an event loop, which doesn't suffer from this bug.
        public async void Dispose()
        {
            await this.DisposeAsync();
        }

        public IObservable<T> Use<T>(Func<IDatabaseConnection, CancellationToken, IEnumerable<T>> f)
        {
            Contract.Requires(f != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return Observable.Create((IObserver<T> observer, CancellationToken cancellationToken) =>
                {
                    // Prevent calls to subscribe after the connection is disposed
                    if (this.disposed)
                    {
                        observer.OnError(new ObjectDisposedException(this.GetType().FullName));
                        return Task.FromResult(Unit.Default);
                    }

                    return queue.EnqueueOperation(ct =>
                        {
                            this.progressHandlerResult.Value = false;
                            var ctSubscription = ct.Register(() => this.progressHandlerResult.Value = true);
                                
                            try
                            {
                                ct.ThrowIfCancellationRequested();

                                // Note: Diposing the connection wrapper doesn't dispose the underlying connection
                                // The intent here is to prevent access to the underlying connection outside of the
                                // function call.
                                using (var db = new DelegatingDatabaseConnection(this.conn))
                                {
                                    foreach (var e in f(db, ct))
                                    {
                                        observer.OnNext(e);
                                        ct.ThrowIfCancellationRequested();
                                    }

                                    observer.OnCompleted();
                                }
                            }
                            finally
                            {
                                ctSubscription.Dispose();
                                this.progressHandlerResult.Value = false;
                            }
                        }, scheduler, cancellationToken);
                });
        }
    }
}

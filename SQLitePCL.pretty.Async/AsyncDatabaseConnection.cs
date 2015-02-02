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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Extensions methods for <see cref="IDatabaseConnection"/>
    /// </summary>
    public static partial class DatabaseConnection
    {
        private static IScheduler defaultScheduler = TaskPoolScheduler.Default;

        /// <summary>
        /// Allows an application to set a default scheduler for <see cref="IAsyncDatabaseConnection"/>
        /// instances created with <see cref="DatabaseConnection.AsAsyncDatabaseConnection(SQLiteDatabaseConnection)"/>.
        /// </summary>
        /// <remarks>This is a convenience feature that allows an application to set a global
        /// <see cref="IScheduler"/> instance, instead of supplying it with each call to
        /// <see cref="DatabaseConnection.AsAsyncDatabaseConnection(SQLiteDatabaseConnection)"/>.
        /// </remarks>
        /// <threadsafety static="false">This setter sets global state and should not be
        /// used after application initialization.</threadsafety>
        public static IScheduler DefaultScheduler
        {
            set
            {
                Contract.Requires(value != null);
                defaultScheduler = value;
            }
        }

        /// <summary>
        /// Returns an <see cref="IAsyncDatabaseConnection"/> instance that delegates database requests
        /// to the provided <see cref="IDatabaseConnection"/>.
        /// </summary>
        /// <remarks>Note, once this method is called the provided <see cref="IDatabaseConnection"/>
        /// is owned by the returned <see cref="IAsyncDatabaseConnection"/>, and may no longer be
        /// safely used directly.</remarks>
        /// <param name="This">The database connection.</param>
        /// <param name="scheduler">A scheduler used to schedule asynchronous database use on.</param>
        /// <returns>An <see cref="IAsyncDatabaseConnection"/> instance.</returns>
        public static IAsyncDatabaseConnection AsAsyncDatabaseConnection(this SQLiteDatabaseConnection This, IScheduler scheduler)
        {
            Contract.Requires(This != null);
            Contract.Requires(scheduler != null);
            return new AsyncDatabaseConnectionImpl(This, scheduler);
        }

        /// <summary>
        /// Returns an <see cref="IAsyncDatabaseConnection"/> instance that delegates database requests
        /// to the provided <see cref="IDatabaseConnection"/>.
        /// </summary>
        /// <remarks>Note, once this method is called the provided <see cref="IDatabaseConnection"/>
        /// is owned by the returned <see cref="IAsyncDatabaseConnection"/>, and may no longer be
        /// safely used directly.</remarks>
        /// <param name="This">The database connection.</param>
        /// <returns>An <see cref="IAsyncDatabaseConnection"/> instance.</returns>
        public static IAsyncDatabaseConnection AsAsyncDatabaseConnection(this SQLiteDatabaseConnection This)
        {
            Contract.Requires(This != null);
            return AsAsyncDatabaseConnection(This, defaultScheduler);
        }
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
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            return This.Use((conn, ct) => conn.ExecuteAll(sql), cancellationToken);
        }

        /// <summary>
        /// Compiles and executes multiple SQL statements.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">One or more semicolon delimited SQL statements.</param>
        /// <returns>A task that completes when all statements have been executed.</returns>
        public static Task ExecuteAllAsync(this IAsyncDatabaseConnection This, string sql)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            return This.ExecuteAllAsync(sql, CancellationToken.None);
        }

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
            Contract.Requires(This != null);
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
        public static Task ExecuteAsync(this IAsyncDatabaseConnection This, string sql, params object[] values)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(values != null);

            return ExecuteAsync(This, sql, CancellationToken.None, values);
        }

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
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            return This.Use((conn, ct) => conn.Execute(sql), cancellationToken);
        }

        /// <summary>
        /// Compiles and executes a SQL statement.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="sql">The SQL statement to compile and execute.</param>
        /// <returns>A task that completes when the statement has been executed.</returns>
        public static Task ExecuteAsync(this IAsyncDatabaseConnection This, string sql)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            return ExecuteAsync(This, sql, CancellationToken.None);
        }

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
            bool canWrite = false)
        {
            Contract.Requires(This != null);
            Contract.Requires(database != null);
            Contract.Requires(tableName != null);
            Contract.Requires(columnName != null);

            return OpenBlobAsync(This, database, tableName, columnName, rowId, canWrite, CancellationToken.None);
        }

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
            Contract.Requires(This != null);
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
            Contract.Requires(This != null);
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
            bool canWrite = false)
        {
            return This.OpenBlobAsync(columnInfo, rowId, canWrite, CancellationToken.None);
        }

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
            Contract.Requires(This != null);
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
        public static Task<IReadOnlyList<IAsyncStatement>> PrepareAllAsync(
           this IAsyncDatabaseConnection This,
           string sql)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            return PrepareAllAsync(This, sql, CancellationToken.None);
        }

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
            Contract.Requires(This != null);
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
        public static Task<IAsyncStatement> PrepareStatementAsync(this IAsyncDatabaseConnection This, string sql)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            return PrepareStatementAsync(This, sql, CancellationToken.None);
        }

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
            Contract.Requires(This != null);
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
            Contract.Requires(This != null);
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
            Contract.Requires(This != null);
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
            Contract.Requires(This != null);
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
        private readonly IObservable<DatabaseTraceEventArgs> trace;
        private readonly IObservable<DatabaseProfileEventArgs> profile;
        private readonly IObservable<DatabaseUpdateEventArgs> update;

        private bool disposed = false;

        internal AsyncDatabaseConnectionImpl(SQLiteDatabaseConnection conn, IScheduler scheduler)
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
        //   using (var db = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
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
                            //this.conn.RegisterProgressHandler(100, () => ct.IsCancellationRequested);
                            try
                            {
                                ct.ThrowIfCancellationRequested();

                                // Note: Diposing the connection wrapper doesn't dispose the underlying connection
                                // The intent here is to prevent access to the underlying connection outside of the
                                // function call.
                                using (var db = new DatabaseConnectionWrapper(this.conn))
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
                                //this.conn.RemoveProgressHandler();
                            }
                        }, scheduler, cancellationToken);
                });
        }

        private sealed class DatabaseConnectionWrapper : IDatabaseConnection
        {
            private readonly SQLiteDatabaseConnection db;
            private readonly OrderedSet<StatementWrapper> statements = new OrderedSet<StatementWrapper>();
            private readonly int initialTotalChanges;

            private readonly EventHandler rollback;
            private readonly EventHandler<DatabaseTraceEventArgs> trace;
            private readonly EventHandler<DatabaseProfileEventArgs> profile;
            private readonly EventHandler<DatabaseUpdateEventArgs> update;

            private bool disposed = false;

            internal DatabaseConnectionWrapper(SQLiteDatabaseConnection db)
            {
                this.db = db;

                this.rollback = (o, e) => this.Rollback(this, e);
                this.trace = (o, e) => this.Trace(this, e);
                this.profile = (o, e) => this.Profile(this, e);
                this.update = (o, e) => this.Update(this, e);

                db.Rollback += rollback;
                db.Trace += trace;
                db.Profile += profile;
                db.Update += update;

                this.initialTotalChanges = db.TotalChanges;
            }

            public event EventHandler Rollback = (o, e) => { };

            public event EventHandler<DatabaseTraceEventArgs> Trace = (o, e) => { };

            public event EventHandler<DatabaseProfileEventArgs> Profile = (o, e) => { };

            public event EventHandler<DatabaseUpdateEventArgs> Update = (o, e) => { };

            public bool IsAutoCommit
            {
                get
                {
                    if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                    return db.IsAutoCommit;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                    return db.IsReadOnly;
                }
            }

            public int Changes
            {
                get
                {
                    if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                    return db.Changes;
                }
            }

            public int TotalChanges
            {
                get
                {
                    if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                    return db.TotalChanges - initialTotalChanges;
                }
            }

            public long LastInsertedRowId
            {
                get
                {
                    if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                    return db.LastInsertedRowId;
                }
            }

            public IEnumerable<IStatement> Statements
            {
                get
                {
                    if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                    // Reverse the order of the statements to match the order returned by SQLite.
                    // Side benefit of preventing callers from being able to cast the statement 
                    // list and do evil things see: http://stackoverflow.com/a/491591
                    return this.statements.Reverse();
                }
            }

            public void WalCheckPoint(string dbName, WalCheckPointMode mode, out int nLog, out int nCkpt)
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                db.WalCheckPoint(dbName, mode, out nLog, out nCkpt);
            }

            public bool IsDatabaseReadOnly(string dbName)
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return db.IsDatabaseReadOnly(dbName);
            }

            internal void RemoveStatement(StatementWrapper stmt)
            {
                statements.Remove(stmt);
            }

            public TableColumnMetadata GetTableColumnMetadata(string dbName, string tableName, string columnName)
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return db.GetTableColumnMetadata(dbName, tableName, columnName);
            }

            public Stream OpenBlob(string database, string tableName, string columnName, long rowId, bool canWrite)
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return db.OpenBlob(database, tableName, columnName, rowId, canWrite);
            }

            public IStatement PrepareStatement(string sql, out string tail)
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                var stmt = db.PrepareStatement(sql, out tail);
                var retval = new StatementWrapper(stmt, this);
                this.statements.Add(retval);
                return retval;
            }

            public SQLiteStatusResult Status(DatabaseConnectionStatusCode statusCode, bool reset)
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return this.db.Status(statusCode, reset);
            }

            public void Dispose()
            {
                db.Rollback -= rollback;
                db.Trace -= trace;
                db.Profile -= profile;
                db.Update -= update;

                // Guard against someone taking a reference to this and trying to use it outside of
                // the Use function delegate
                disposed = true;
                // We don't actually own the database connection so its not disposed
            }
        }

        private class StatementWrapper : IStatement
        {
            internal readonly IStatement stmt;
            private readonly WeakReference<DatabaseConnectionWrapper> db;

            internal StatementWrapper(IStatement stmt, DatabaseConnectionWrapper db)
            {
                this.stmt = stmt;

                // The statement may outlive the connection wrapper.
                // This is ok as long as the statement is disposed prior to disposing
                // The actual underlying database connection.
                this.db = new WeakReference<DatabaseConnectionWrapper>(db);
            }

            public IReadOnlyOrderedDictionary<string, IBindParameter> BindParameters
            {
                get { return stmt.BindParameters; }
            }

            public IReadOnlyList<ColumnInfo> Columns
            {
                get { return stmt.Columns; }
            }

            public string SQL
            {
                get { return stmt.SQL; }
            }

            public bool IsReadOnly
            {
                get { return stmt.IsReadOnly; }
            }

            public bool IsBusy
            {
                get { return stmt.IsBusy; }
            }

            public void ClearBindings()
            {
                stmt.ClearBindings();
            }

            public IReadOnlyList<IResultSetValue> Current
            {
                get { return stmt.Current; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return stmt.Current; }
            }

            public bool MoveNext()
            {
                return stmt.MoveNext();
            }

            public void Reset()
            {
                stmt.Reset();
            }

            public void Dispose()
            {
                DatabaseConnectionWrapper conn;
                if (db.TryGetTarget(out conn))
                {
                    conn.RemoveStatement(this);
                }

                stmt.Dispose();
            }

            public int Status(StatementStatusCode statusCode, bool reset)
            {
                return stmt.Status(statusCode, reset);
            }
        }
    }
}

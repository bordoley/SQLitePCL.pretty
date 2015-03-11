using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Extensions methods for <see cref="IAsyncDatabaseConnection"/>.
    /// </summary>
    public static class AsyncDatabaseConnection
    {
        /// <summary>
        /// Schedules the <see cref="Action"/> <paramref name="action"/> on the database operations queue in a transaction.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="action">The action.</param>
        /// <returns>A task that completes when <paramref name="action"/> returns.</returns>
        public static Task RunInTransactionAsync(
            this IAsyncDatabaseConnection This, 
            Action<IDatabaseConnection> action)
        {
            return This.Use(db => 
                db.RunInTransaction(_ => 
                    action(db)));
        }

        /// <summary>
        /// Schedules the <see cref="Action"/> <paramref name="action"/> on the database operations queue in a transaction.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="action">The action.</param>
        /// <param name="ct">Cancellation token that can be used to cancel the task.</param>
        /// <returns>A task that completes when <paramref name="action"/> returns.</returns>
        public static Task RunInTransactionAsync(
            this IAsyncDatabaseConnection This, 
            Action<IDatabaseConnection,CancellationToken> action, 
            CancellationToken ct)
        {
            return This.Use(
                (db, _) => db.RunInTransaction(__ => action(db, ct)), 
                ct);
        }

        /// <summary>
        /// Schedules the <see cref="Func&lt;T,TResult&gt;"/> <paramref name="f"/> on the database operations queue in a transaction.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="f">A function from <see cref="IDatabaseConnection"/> to <typeparamref name="T"/>.</param>
        /// <returns>A task that completes with the result of <paramref name="f"/>.</returns>
        public static Task<T> RunInTransactionAsync<T>(
            this IAsyncDatabaseConnection This, 
            Func<IDatabaseConnection, T> f)
        {
            return This.Use(db => db.RunInTransaction(_ => f(db)));
        }
            
        /// <summary>
        /// Schedules the <see cref="Func&lt;T,TResult&gt;"/> <paramref name="f"/> on the database operations queue in a transaction.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="f">A function from <see cref="IDatabaseConnection"/> to <typeparamref name="T"/>.</param>
        /// <param name="ct">Cancellation token that can be used to cancel the task.</param>
        /// <returns>A task that completes with the result of <paramref name="f"/>.</returns>
        public static Task<T> RunInTransactionAsync<T>(
            this IAsyncDatabaseConnection This, 
            Func<IDatabaseConnection, CancellationToken, T> f,
            CancellationToken ct)
        {
            return This.Use(
                (db, _) => db.RunInTransaction(__ => f(db, ct)), 
                ct);
        }
    }
}


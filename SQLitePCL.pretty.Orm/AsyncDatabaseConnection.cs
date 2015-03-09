using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    public static class AsyncDatabaseConnection
    {
        public static Task RunInTransactionAsync(
            this IAsyncDatabaseConnection This, 
            Action<IDatabaseConnection> action)
        {
            return This.Use(db => 
                db.RunInTransaction(_ => 
                    action(db)));
        }

        public static Task RunInTransactionAsync(
            this IAsyncDatabaseConnection This, 
            Action<IDatabaseConnection,CancellationToken> action, 
            CancellationToken ct)
        {
            return This.Use(
                (db, _) => db.RunInTransaction(__ => action(db, ct)), 
                ct);
        }

        public static Task<T> RunInTransactionAsync<T>(
            this IAsyncDatabaseConnection This, 
            Func<IDatabaseConnection, T> func)
        {
            return This.Use(db => db.RunInTransaction(_ => func(db)));
        }

        public static Task<T> RunInTransactionAsync<T>(
            this IAsyncDatabaseConnection This, 
            Func<IDatabaseConnection, CancellationToken, T> func,
            CancellationToken ct)
        {
            return This.Use(
                (db, _) => db.RunInTransaction(__ => func(db, ct)), 
                ct);
        }

        public static IObservable<T> RunInTransaction<T>(
            this IAsyncDatabaseConnection This, 
            Func<IDatabaseConnection, IEnumerable<T>> func)
        {
            return This.Use(db => db.RunInTransaction(__ => func(db)));
        }

        public static IObservable<T> RunInTransaction<T>(
            this IAsyncDatabaseConnection This, 
            Func<IDatabaseConnection, CancellationToken, IEnumerable<T>> func)
        {
            return This.Use<T>((db, ct) => db.RunInTransaction(__ => func(db, ct)));
        }
    }
}


using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace SQLitePCL.pretty.Orm
{
    public static partial class DatabaseConnection
    { 
        /// <summary>
        /// Prepares a SQLite statement that can be bound to an object primary key to delete row from the database.
        /// </summary>
        /// <returns>A prepared statement.</returns>
        /// <param name="This">The database connection</param>
        /// <param name="tableMapping">The table mapping.</param>
        public static IStatement PrepareDeleteStatement(this IDatabaseConnection This, TableMapping tableMapping)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
           
            return This.PrepareDelete(tableMapping.TableName, tableMapping.PrimaryKeyColumn());   
        }

        private static IEnumerable<KeyValuePair<long,T>> YieldDeleteAll<T>(
            this IDatabaseConnection This, 
            TableMapping tableMapping, 
            IEnumerable<long> primaryKeys, 
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector)
        {
            using (var deleteStmt = This.PrepareDeleteStatement(tableMapping))
            using (var findStmt = This.PrepareFindStatement(tableMapping))
            {
                foreach (var primaryKey in primaryKeys)
                {
                    var result = findStmt.Query(primaryKey).Select(x =>
                        {
                            deleteStmt.Execute(primaryKey);
                            return x;
                        }).Select(resultSelector).FirstOrDefault();
                    yield return new KeyValuePair<long,T>(primaryKey, result);
                }
            }
        }

        /// <summary>
        /// Tries to delete the object in the database with the provided primary key.
        /// </summary>
        /// <returns><c>true</c>, if an object was found and deleted, <c>false</c> otherwise.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="primaryKey">A primary key.</param>
        /// <param name="deleted">If found in the database, the deleted object.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static bool TryDelete<T>(
            this IDatabaseConnection This, 
            TableMapping tableMapping, 
            long primaryKey, 
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector, 
            out T deleted)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
            Contract.Requires(resultSelector != null);
           
            var result = This.YieldDeleteAll(tableMapping, new long[] { primaryKey }, resultSelector).FirstOrDefault();
            if (result.Value != null)
            {
                deleted = result.Value;
                return true;
            }
            else
            {
                deleted = default(T);
                return false;
            }
        }

        /// <summary>
        /// Deleted all object instances specified by their primary keys.
        /// </summary>
        /// <returns>A dictionary mapping the primary key to its value if found in the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="primaryKeys">An IEnumerable of primary keys to delete.</param>
        /// <typeparam name="T">The mapped type.</typeparam>       
        public static IReadOnlyDictionary<long,T> DeleteAll<T>(
            this IDatabaseConnection This, 
            TableMapping tableMapping, 
            IEnumerable<long> primaryKeys,
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
            Contract.Requires(primaryKeys != null);
            Contract.Requires(resultSelector != null);

            return This.RunInTransaction(_ => 
                        This.YieldDeleteAll(tableMapping, primaryKeys, resultSelector)
                            .Where(kvp => kvp.Value != null)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }
    }

    public static partial class AsyncDatabaseConnection
    { 
        /// <summary>
        /// Deletes all object instances specified by their primary keys.
        /// </summary>
        /// <returns>A task that completes with a dictionary mapping the primary key to its value if found in the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="primaryKeys">An IEnumerable of primary keys to delete.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task<IReadOnlyDictionary<long,T>> DeleteAllAsync<T>(
            this IAsyncDatabaseConnection This, 
            TableMapping tableMapping, 
            IEnumerable<long> primaryKeys, 
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector,
            CancellationToken ct)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
            Contract.Requires(primaryKeys != null);
            Contract.Requires(resultSelector != null);

            return This.Use((db,_) => db.DeleteAll<T>(tableMapping, primaryKeys, resultSelector), ct);
        }

        /// <summary>
        /// Deletes all object instances specified by their primary keys.
        /// </summary>
        /// <returns>A task that completes with a dictionary mapping the primary key to its value if found in the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="primaryKeys">An IEnumerable of primary keys to delete.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task<IReadOnlyDictionary<long,T>> DeleteAllAsync<T>(
            this IAsyncDatabaseConnection This, 
            TableMapping tableMapping, 
            IEnumerable<long> primaryKeys,
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
            Contract.Requires(primaryKeys != null);
            Contract.Requires(resultSelector != null);

            return This.DeleteAllAsync(tableMapping, primaryKeys, resultSelector, CancellationToken.None);
        }
    }
}


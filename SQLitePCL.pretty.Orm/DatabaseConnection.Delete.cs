using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;


namespace SQLitePCL.pretty.Orm
{
    public static partial class DatabaseConnection
    { 
        private static readonly ConditionalWeakTable<TableMapping, string> deleteQueries = 
            new ConditionalWeakTable<TableMapping, string>();

        /// <summary>
        /// Prepares a SQLite statement that can be bound to an object primary key to delete row from the database.
        /// </summary>
        /// <returns>A prepared statement.</returns>
        /// <param name="This">The database connection</param>
        /// <param name="tableMapping">The table mapping.</param>
        public static IStatement PrepareDeleteStatement<T>(this IDatabaseConnection This)
        {
            Contract.Requires(This != null);

            var tableMapping = TableMapping.Create<T>();
            var sql = deleteQueries.GetValue(tableMapping, mapping => 
                {
                    var primaryKeyColumn = mapping.PrimaryKeyColumn();
                    return SQLBuilder.DeleteUsingPrimaryKey(mapping.TableName, primaryKeyColumn);
                });

           
            return This.PrepareStatement(sql);
        }

        private static IEnumerable<KeyValuePair<long,T>> YieldDeleteAll<T>(
            this IDatabaseConnection This, 
            IEnumerable<long> primaryKeys, 
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector)
        {
            using (var deleteStmt = This.PrepareDeleteStatement<T>())
            using (var findStmt = This.PrepareFindStatement<T>())
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
            long primaryKey, 
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector, 
            out T deleted)
        {
            Contract.Requires(This != null);
            Contract.Requires(resultSelector != null);
           
            var result = This.YieldDeleteAll(new long[] { primaryKey }, resultSelector).FirstOrDefault();
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
            IEnumerable<long> primaryKeys,
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(primaryKeys != null);
            Contract.Requires(resultSelector != null);

            return This.RunInTransaction(_ => 
                        This.YieldDeleteAll(primaryKeys, resultSelector)
                            .Where(kvp => kvp.Value != null)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        /// <summary>
        /// Drops the table if it exists. Otherwise this is a no-op.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="table">The table name.</param>
        /// <seealso href="https://www.sqlite.org/lang_droptable.html"/>
        public static void DropTableIfExists<T>(this IDatabaseConnection This)
        {
            Contract.Requires(This != null);

            var tableMapping = TableMapping.Create<T>();
            This.Execute(SQLBuilder.DropTableIfExists(tableMapping.TableName));
        }


        /// <summary>
        /// Deletes all rows in a given table.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="table">The table name.</param>
        public static void DeleteAllRows<T>(this IDatabaseConnection This)
        {
            Contract.Requires(This != null);

            var tableMapping = TableMapping.Create<T>();
            This.Execute(SQLBuilder.DeleteAll(tableMapping.TableName));
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
            IEnumerable<long> primaryKeys, 
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector,
            CancellationToken ct)
        {
            Contract.Requires(This != null);
            Contract.Requires(primaryKeys != null);
            Contract.Requires(resultSelector != null);

            return This.Use((db,_) => db.DeleteAll<T>(primaryKeys, resultSelector), ct);
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
            IEnumerable<long> primaryKeys,
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(primaryKeys != null);
            Contract.Requires(resultSelector != null);

            return This.DeleteAllAsync(primaryKeys, resultSelector, CancellationToken.None);
        }


        /// <summary>
        /// Drops the table if exists async.
        /// </summary>
        /// <returns>The table if exists async.</returns>
        /// <param name="This">This.</param>
        /// <param name="table">The table name.</param>
        /// <param name="ct">Ct.</param>
        public static Task DropTableIfExistsAsync<T>(this IAsyncDatabaseConnection This,  CancellationToken ct)
        {
            Contract.Requires(This != null);

            return This.Use((db, _) => db.DropTableIfExists<T>(), ct);
        }

        /// <summary>
        /// Drops the table if exists async.
        /// </summary>
        /// <returns>The table if exists async.</returns>
        /// <param name="This">This.</param>
        /// <param name="table">The table name.</param>
        public static Task DropTableIfExistsAsync<T>(this IAsyncDatabaseConnection This)
        {
            Contract.Requires(This != null);
            return This.DropTableIfExistsAsync<T>(CancellationToken.None);
        }

        /// <summary>
        /// Deletes all rows in a given table, asynchronously.
        /// </summary>
        /// <returns>A task that completes when all rows are deleted succesfully.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="table">The table name.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        public static Task DeleteAllRowsAsync<T>(this IAsyncDatabaseConnection This, CancellationToken ct)
        {
            Contract.Requires(This != null);
            return This.Use((db, _) => db.DeleteAllRows<T>(), ct);
        }

        /// <summary>
        /// Deletes all rows in a given table, asynchronously.
        /// </summary>
        /// <returns>A task that completes when all rows are deleted succesfully.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="table">The table name.</param>
        public static Task DeleteAllRowsAsync<T>(this IAsyncDatabaseConnection This)
        {
            Contract.Requires(This != null);
            return This.DeleteAllRowsAsync<T>(CancellationToken.None);
        }
    }
}


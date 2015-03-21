using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace SQLitePCL.pretty.Orm
{
    public static partial class TableMapping
    { 

        /// <summary>
        /// Prepares a SQLite statement that can be bound to an object primary key to delete row from the database.
        /// </summary>
        /// <returns>A prepared statement.</returns>
        /// <param name="This">The database connection</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <typeparam name="T">The mapped type</typeparam>
        public static IStatement PrepareDeleteStatement<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return This.PrepareDelete(tableMapping.TableName, tableMapping.PrimaryKeyColumn());   
        }

        private static IEnumerable<KeyValuePair<long,T>> YieldDeleteAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<long> primaryKeys)
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
                        }).FirstOrDefault();
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
        public static bool TryDelete<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, long primaryKey, out T deleted)
        {
            var result = This.YieldDeleteAll(tableMapping, new long[] { primaryKey }).FirstOrDefault();
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
        public static IReadOnlyDictionary<long,T> DeleteAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<long> primaryKeys)
        {
            return This.RunInTransaction(_ => 
                        This.YieldDeleteAll(tableMapping, primaryKeys)
                            .Where(kvp => kvp.Value != null)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        /// <summary>
        /// Deletes all object instances specified by their primary keys.
        /// </summary>
        /// <returns>A task that completes with a dictionary mapping the primary key to its value if found in the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="primaryKeys">An IEnumerable of primary keys to delete.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task<IReadOnlyDictionary<long,T>> DeleteAllAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<long> primaryKeys, CancellationToken ct)
        {
            return This.Use((db,_) => db.DeleteAll<T>(tableMapping, primaryKeys), ct);
        }

        /// <summary>
        /// Deletes all object instances specified by their primary keys.
        /// </summary>
        /// <returns>A task that completes with a dictionary mapping the primary key to its value if found in the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="primaryKeys">An IEnumerable of primary keys to delete.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task<IReadOnlyDictionary<long,T>> DeleteAllAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<long> primaryKeys)
        {
            return This.DeleteAllAsync(tableMapping, primaryKeys, CancellationToken.None);
        }

        /// <summary>
        /// Deletes all rows in a given table.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static void DeleteAllRows<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            This.DeleteAll(tableMapping.TableName);
        }

        /// <summary>
        /// Deletes all rows in a given table, asynchronously.
        /// </summary>
        /// <returns>A task that completes when all rows are deleted succesfully.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">he table mapping.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task DeleteAllRowsAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, CancellationToken ct)
        {
            return This.Use((db, _) => db.DeleteAllRows(tableMapping), ct);
        }

        /// <summary>
        /// Deletes all rows in a given table, asynchronously.
        /// </summary>
        /// <returns>A task that completes when all rows are deleted succesfully.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">he table mapping.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task DeleteAllRowsAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return This.DeleteAllRowsAsync(tableMapping, CancellationToken.None);
        }
    }
}


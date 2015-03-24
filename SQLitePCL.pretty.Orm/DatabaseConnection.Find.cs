using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty.Orm
{
    public static partial class DatabaseConnection
    { 
        private static readonly ConditionalWeakTable<TableMapping, string> find = 
            new ConditionalWeakTable<TableMapping, string>();

        private static string Find(this TableMapping This)
        {
            return find.GetValue(This, mapping => 
                {
                    var column = This.PrimaryKeyColumn();
                    return SQLBuilder.SelectWhereColumnEquals(This.TableName, column);
                });
        }

        /// <summary>
        /// Prepares a SQLite statement that can be bound to an object primary key to retrieve an instance from the database.
        /// </summary>
        /// <returns>A prepared statement.</returns>
        /// <param name="This">The database connection</param>
        /// <param name="tableMapping">The table mapping.</param>
        public static IStatement PrepareFindStatement(this IDatabaseConnection This, TableMapping tableMapping)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);

            return This.PrepareStatement(tableMapping.Find());   
        }

        private static IEnumerable<KeyValuePair<long,T>> YieldFindAll<T>(
            this IDatabaseConnection This, 
            TableMapping tableMapping, 
            IEnumerable<long> primaryKeys,
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector)
        {
            using (var findStmt = This.PrepareFindStatement(tableMapping))
            {
                foreach (var primaryKey in primaryKeys)
                {
                    var result = findStmt.Query(primaryKey).Select(resultSelector).FirstOrDefault();
                    yield return new KeyValuePair<long,T>(primaryKey, result);
                }
            }
        }

        /// <summary>
        /// Tries to find an object in the database with the provided primary key.
        /// </summary>
        /// <returns><c>true</c>, if an object was found, <c>false</c> otherwise.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="primaryKey">A primary key.</param>
        /// <param name="value">If found in the database, the found object.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static bool TryFind<T>(
            this IDatabaseConnection This, 
            TableMapping tableMapping, 
            long primaryKey, 
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector,
            out T value)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
            Contract.Requires(resultSelector != null);

            var result = This.YieldFindAll(tableMapping, new long[] { primaryKey }, resultSelector).FirstOrDefault();

            if (result.Value != null)
            {
                value = result.Value;
                return true;
            }

            value = default(T);
            return false;
        }

        /// <summary>
        /// Finds all object instances specified by their primary keys.
        /// </summary>
        /// <returns>A dictionary mapping the primary key to its value if found in the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="primaryKeys">An IEnumerable of primary keys to find.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static IReadOnlyDictionary<long,T> FindAll<T>(
            this IDatabaseConnection This, 
            TableMapping tableMapping, 
            IEnumerable<long> primaryKeys,
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
            Contract.Requires(primaryKeys != null);
            Contract.Requires(resultSelector != null);

            return This.YieldFindAll(tableMapping, primaryKeys, resultSelector)
                       .Where(kvp => kvp.Value != null)
                       .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
     }

     public static partial class AsyncDatabaseConnection
     {
        /// <summary>
        /// Finds all object instances specified by their primary keys.
        /// </summary>
        /// <returns>A task that completes with a dictionary mapping the primary key to its value if found in the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="primaryKeys">An IEnumerable of primary keys to find.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task<IReadOnlyDictionary<long,T>> FindAllAsync<T>(
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

            return This.Use((db,_) => db.FindAll(tableMapping, primaryKeys, resultSelector), ct);
        }

        /// <summary>
        /// Finds all object instances specified by their primary keys.
        /// </summary>
        /// <returns>A task that completes with a dictionary mapping the primary key to its value if found in the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="primaryKeys">An IEnumerable of primary keys to find.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task<IReadOnlyDictionary<long,T>> FindAllAsync<T>(
            this IAsyncDatabaseConnection This, 
            TableMapping tableMapping, 
            IEnumerable<long> primaryKeys,
            Func<IReadOnlyList<IResultSetValue>,T> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);
            Contract.Requires(primaryKeys != null);
            Contract.Requires(resultSelector != null);

            return This.FindAllAsync(tableMapping, primaryKeys, resultSelector, CancellationToken.None);
        }
    }
}


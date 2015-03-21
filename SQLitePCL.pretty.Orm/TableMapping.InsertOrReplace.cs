using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty.Orm
{
    public static partial class TableMapping
    { 
        private static string InsertOrReplace<T>(this ITableMapping<T> tableMapping)
        {
            return SQLBuilder.InsertOrReplace(tableMapping.TableName, tableMapping.Columns.Select(x => x.Key));     
        }

        /// <summary>
        /// Prepares a SQLite statement that can be bound to an object of type T to insert into the database.
        /// </summary>
        /// <returns>A prepared statement.</returns>
        /// <param name="This">The database connection</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <typeparam name="T">The mapped type</typeparam>
        public static ITableMappedStatement<T> PrepareInsertOrReplaceStatement<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return new TableMappedStatement<T>(This.PrepareStatement(tableMapping.InsertOrReplace()), tableMapping);   
        }

        private static IEnumerable<KeyValuePair<T,T>> YieldInsertOrReplaceAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            using (var insertOrReplaceStmt = This.PrepareInsertOrReplaceStatement(tableMapping))
            using (var findStmt = This.PrepareFindByRowId(tableMapping))
            {
                foreach (var obj in objects)
                {
                    insertOrReplaceStmt.Execute(obj);
                    var rowId = This.LastInsertedRowId;
                    yield return new KeyValuePair<T,T>(obj, findStmt.Query(rowId).First());
                } 
            }
        }

        /// <summary>
        /// Inserts an object into the database, replacing the existing entry if the primary key already exists.
        /// </summary>
        /// <returns>The object inserted into the database including it's primary key.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="obj">The object to insert.</param>
        /// <typeparam name="T">The mapped type.</typeparam>     
        public static T InsertOrReplace<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.YieldInsertOrReplaceAll(tableMapping, new T[] {obj}).First().Value;
        }

        /// <summary>
        /// Inserts the objects into the database, replacing existing entries if the given primary keys already exist.
        /// </summary>
        /// <returns>A dictionary mapping the provided objects to the objects that were inserted into the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="objects">The objects to be inserted into the database.</param>
        /// <typeparam name="T">The mapped type.</typeparam> 
        public static IReadOnlyDictionary<T,T> InsertOrReplaceAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            return This.RunInTransaction(_ => 
                This.YieldInsertOrReplaceAll(tableMapping, objects).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        /// <summary>
        /// Inserts the objects into the database, replacing existing entries if the given primary keys already exist.
        /// </summary>
        /// <returns>A Task that completes with a dictionary mapping the provided objects to the objects that were inserted into the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="objects">The objects to be inserted into the database.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation</param>
        /// <typeparam name="T">The mapped type.</typeparam> 
        public static Task<IReadOnlyDictionary<T,T>> InsertOrReplaceAllAsync<T>(
            this IAsyncDatabaseConnection This, 
            ITableMapping<T> tableMapping,
            IEnumerable<T> objects,
            CancellationToken ct)
        {
            return This.Use((db, _) => db.InsertOrReplaceAll(tableMapping, objects), ct);
        }

        /// <summary>
        /// Inserts the objects into the database, replacing existing entries if the given primary keys already exist.
        /// </summary>
        /// <returns>A Task that completes with a dictionary mapping the provided objects to the objects that were inserted into the database.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="objects">The objects to be inserted into the database.</param>
        /// <typeparam name="T">The mapped type.</typeparam> 
        public static Task<IReadOnlyDictionary<T,T>> InsertOrReplaceAllAsync<T>(
            this IAsyncDatabaseConnection This, 
            ITableMapping<T> tableMapping,
            IEnumerable<T> objects)
        {
            return This.InsertOrReplaceAllAsync(tableMapping, objects, CancellationToken.None);
        }
    }
}


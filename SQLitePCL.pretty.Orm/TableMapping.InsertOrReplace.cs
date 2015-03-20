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
            
        public static T InsertOrReplace<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.YieldInsertOrReplaceAll(tableMapping, new T[] {obj}).First().Value;
        }

        // These are internal for now. Probably will be removed. More likely than not an insert will want to wrapped in a larger transaction.
        // Using the async inserts is terrible for perf as you jump threads.
        internal static Task<T> InsertOrReplaceAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj, CancellationToken cancellationToken)
        {
            return This.Use((x, ct) => x.InsertOrReplace(tableMapping, obj), cancellationToken);
        }

        internal static Task<T> InsertOrReplaceAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.InsertOrReplaceAsync(tableMapping, obj, CancellationToken.None);
        }

        public static IReadOnlyDictionary<T,T> InsertOrReplaceAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            return This.RunInTransaction(_ => 
                This.YieldInsertOrReplaceAll(tableMapping, objects).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        public static Task<IReadOnlyDictionary<T,T>> InsertOrReplaceAllAsync<T>(
            this IAsyncDatabaseConnection This, 
            ITableMapping<T> tableMapping,
            IEnumerable<T> objects,
            CancellationToken ct)
        {
            return This.Use((db, _) => db.InsertOrReplaceAll(tableMapping, objects), ct);
        }

        public static Task<IReadOnlyDictionary<T,T>> InsertOrReplaceAllAsync<T>(
            this IAsyncDatabaseConnection This, 
            ITableMapping<T> tableMapping,
            IEnumerable<T> objects)
        {
            return This.InsertOrReplaceAllAsync(tableMapping, objects, CancellationToken.None);
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace SQLitePCL.pretty.Orm
{
    // Make these internal for now, and probably remove them. For most use cases I'm thinking what is really desired
    // is the semantics of InsertOrReplace as oppose to Insert which throws exceptions if a PK already exists.
    public static partial class TableMapping
    { 
        private static string Insert<T>(this ITableMapping<T> tableMapping)
        {
            return SQLBuilder.Insert(tableMapping.TableName, tableMapping.Columns.Select(x => x.Key));
        }
 
        internal static ITableMappedStatement<T> PrepareInsertStatement<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return new TableMappedStatement<T>(This.PrepareStatement(tableMapping.Insert()), tableMapping);   
        }

        private static IEnumerable<KeyValuePair<T,T>> YieldInsertAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            using (var insertStmt = This.PrepareInsertStatement(tableMapping))
            using (var findStmt = This.PrepareFindByRowId(tableMapping))
            {
                foreach (var obj in objects)
                {
                    insertStmt.Execute(obj);
                    var rowId = This.LastInsertedRowId;
                    yield return new KeyValuePair<T,T>(obj, findStmt.Query(rowId).First());
                }
            }
        }

        internal static T Insert<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.YieldInsertAll(tableMapping, new T[] { obj }).First().Value;
        }

        internal static Task<T> InsertAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj, CancellationToken cancellationToken)
        {
            return This.Use((x, ct) => x.Insert(tableMapping, obj), cancellationToken);
        }

        internal static Task<T> InsertAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.InsertAsync(tableMapping, obj, CancellationToken.None);
        }

        internal static IReadOnlyDictionary<T,T> InsertAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            return This.RunInTransaction(_ => This.YieldInsertAll(tableMapping, objects).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        internal static Task<IReadOnlyDictionary<T,T>> InsertAll<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            return This.Use(db => db.InsertAll(tableMapping, objects));
        }
    }
}


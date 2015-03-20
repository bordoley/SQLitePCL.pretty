using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace SQLitePCL.pretty.Orm
{
    public static partial class TableMapping
    { 
        public static ITableMappedStatement<T> PrepareDeleteStatement<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return new TableMappedStatement<T>(
                This.PrepareDelete(tableMapping.TableName, tableMapping.PrimaryKeyColumn()), 
                tableMapping);   
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
            
        public static IReadOnlyDictionary<long,T> DeleteAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<long> primaryKeys)
        {
            return This.RunInTransaction(_ => 
                        This.YieldDeleteAll(tableMapping, primaryKeys)
                            .Where(kvp => kvp.Value != null)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        public static Task<IReadOnlyDictionary<long,T>> DeleteAllAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<long> primaryKeys, CancellationToken ct)
        {
            return This.Use((db,_) => db.DeleteAll<T>(tableMapping, primaryKeys), ct);
        }

        public static Task<IReadOnlyDictionary<long,T>> DeleteAllAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<long> primaryKeys)
        {
            return This.DeleteAllAsync(tableMapping, primaryKeys, CancellationToken.None);
        }

        public static void DeleteAllRows<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            This.DeleteAll(tableMapping.TableName);
        }

        public static Task DeleteAllRowsAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, CancellationToken ct)
        {
            return This.Use((db, _) => db.DeleteAllRows(tableMapping), ct);
        }

        public static Task DeleteAllRowsAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return This.DeleteAllRowsAsync(tableMapping, CancellationToken.None);
        }
    }
}


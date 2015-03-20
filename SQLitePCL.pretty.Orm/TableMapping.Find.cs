using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty.Orm
{
    public static partial class TableMapping
    { 
        private static readonly ConditionalWeakTable<ITableMapping, string> find = 
            new ConditionalWeakTable<ITableMapping, string>();

        private static string Find(this ITableMapping This)
        {
            return find.GetValue(This, mapping => 
                {
                    var column = This.PrimaryKeyColumn();
                    return SQLBuilder.SelectWhereColumnEquals(This.TableName, column);
                });
        }

        public static ITableMappedStatement<T> PrepareFindStatement<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return new TableMappedStatement<T>(This.PrepareStatement(tableMapping.Find()), tableMapping);   
        }

        private static IEnumerable<KeyValuePair<long,T>> YieldFindAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<long> primaryKeys)
        {
            using (var findStmt = This.PrepareFindStatement(tableMapping))
            {
                foreach (var primaryKey in primaryKeys)
                {
                    var result = findStmt.Query(primaryKey).FirstOrDefault();
                    yield return new KeyValuePair<long,T>(primaryKey, result);
                }
            }
        }

        public static bool TryFind<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, long primaryKey, out T value)
        {
            var result = This.YieldFindAll(tableMapping, new long[] { primaryKey }).FirstOrDefault();

            if (result.Value != null)
            {
                value = result.Value;
                return true;
            }

            value = default(T);
            return false;
        }

        public static IReadOnlyDictionary<long,T> FindAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<long> primaryKeys)
        {
            return This.YieldFindAll(tableMapping, primaryKeys)
                       .Where(kvp => kvp.Value != null)
                       .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static Task<IReadOnlyDictionary<long,T>> FindAllAsync<T>(
            this IAsyncDatabaseConnection This, 
            ITableMapping<T> tableMapping, 
            IEnumerable<long> primaryKeys, 
            CancellationToken ct)
        {
            return This.Use((db,_) => db.FindAll(tableMapping, primaryKeys), ct);
        }

        public static Task<IReadOnlyDictionary<long,T>> FindAllAsync<T>(
            this IAsyncDatabaseConnection This, 
            ITableMapping<T> tableMapping, 
            IEnumerable<long> primaryKeys)
        {
            return This.FindAllAsync(tableMapping, primaryKeys, CancellationToken.None);
        }

    }
}


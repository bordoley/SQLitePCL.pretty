//
// Copyright (c) 2009-2015 Krueger Systems, Inc.
// Copyright (c) 2015 David Bordoley
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.Orm
{
    /// <summary>
    /// Extensions methods for instances of <see cref="ITableMapping"/>
    /// </summary>
    public static class TableMapping
    {   
        internal static void Bind<T>(this IStatement This, ITableMapping<T> tableMapping, T obj)
        {
            foreach (var column in tableMapping.Columns)
            {
                var key = ":" + column.Key;
                var value = column.Value.Property.GetValue(obj);
                This.BindParameters[key].Bind(value);
            }
        }

        public static TableQuery<T> CreateQuery<T>(this ITableMapping<T> This)
        {
            return new TableQuery<T>(This, "*", null, new List<Ordering>(), null, null);
        }
            
        public static void InitTable<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            This.CreateTableIfNotExists(tableMapping.TableName, CreateFlags.None, tableMapping.Columns.Select(x => Tuple.Create(x.Key, x.Value.Metadata)));

            if (This.Changes == 0)
            {
                This.MigrateTable(tableMapping);
            }

            foreach (var index in tableMapping.Indexes) 
            {
                This.CreateIndex(index.Key, tableMapping.TableName, index.Value.Columns, index.Value.Unique);
            }
        }

        public static Task InitTableAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, CancellationToken cancellationToken)
        {
            return This.Use((db, ct) => db.InitTable(tableMapping), cancellationToken);
        }

        public static Task InitTableAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return This.InitTableAsync(tableMapping, CancellationToken.None);
        }
            
        private static void MigrateTable<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            var existingCols = This.GetTableInfo(tableMapping.TableName);
            
            var toBeAdded = new List<Tuple<string, TableColumnMetadata>> ();

            // FIXME: Nasty n^2 search due to case insensitive strings. Number of columns
            // is normally small so not that big of a deal.
            foreach (var p in tableMapping.Columns) 
            {
                var found = false;

                foreach (var c in existingCols) 
                {
                    found = (string.Compare (p.Key, c.Key, StringComparison.OrdinalIgnoreCase) == 0);
                    if (found) { break; }
                }

                if (!found) { toBeAdded.Add (Tuple.Create(p.Key, p.Value.Metadata)); }
            }
            
            foreach (var p in toBeAdded) 
            {
                This.Execute (SQLBuilder.AlterTableAddColumn(tableMapping.TableName, p.Item1, p.Item2));
            }
        }

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

        public static ITableMappedStatement<T> PrepareFind<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return new TableMappedStatement<T>(This.PrepareStatement(tableMapping.Find()), tableMapping);   
        }

        private static IEnumerable<T> YieldFindAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable primaryKeys)
        {
            using (var findStmt = This.PrepareFind(tableMapping))
            {
                foreach (var primaryKey in primaryKeys)
                {
                    var result = findStmt.Query(primaryKey).FirstOrDefault();
                    yield return result;
                }
            }
        }

        /*
        public static T Find<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            var primaryKey = tableMapping.GetPrimaryKey(obj);
            return This.Find(tableMapping, primaryKey);
        }*/

        public static T Find<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, object primaryKey)
        {
            return This.FindAll(tableMapping, new object[] { primaryKey }).FirstOrDefault();
        }

        /*
        public static Task<T> FindAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.Find(tableMapping, obj, CancellationToken.None);
        }

        public static Task<T> FindAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj, CancellationToken ct)
        {
            return This.Use((db, _) => db.Find(tableMapping, obj), ct);
        }*/

        public static Task<T> FindAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, object primaryKey, CancellationToken ct)
        {
            return This.Use((db, _) => db.Find(tableMapping, primaryKey), ct);
        }

        public static Task<T> FindAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, object primaryKey)
        {
            return This.FindAsync(tableMapping, primaryKey, CancellationToken.None);
        }

        public static IReadOnlyList<T> FindAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable primaryKeys)
        {
            return This.YieldFindAll(tableMapping, primaryKeys).ToList();
        }

        public static IObservable<T> FindAll<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable primaryKeys)
        {
            return This.Use(db => db.YieldFindAll(tableMapping, primaryKeys));
        }

        /*
        public static IEnumerable<T> FindAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            var primaryKeys = objects.Select(tableMapping.GetPrimaryKey);
            return This.FindAll(tableMapping, primaryKeys);
        }

        public static IObservable<T> FindAll<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            var primaryKeys = objects.Select(tableMapping.GetPrimaryKey);
            return This.FindAll(tableMapping, primaryKeys);
        }*/

        private static ITableMappedStatement<T> PrepareFindByRowId<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return new TableMappedStatement<T>(This.PrepareFindByRowId(tableMapping.TableName), tableMapping);   
        }

        private static readonly ConditionalWeakTable<ITableMapping, string> primaryKeyColumn = 
            new ConditionalWeakTable<ITableMapping, string>();

        private static string PrimaryKeyColumn(this ITableMapping This)
        {
            return primaryKeyColumn.GetValue(This, mapping => 
                // Intentionally throw if the column doesn't have a primary key
                mapping.Columns.Where(x => x.Value.Metadata.IsPrimaryKeyPart).Select(x => x.Key).First());
        }

        private static object GetPrimaryKey<T>(this ITableMapping<T> This, T obj)
        {
            var primaryKeyPropery = This.Columns[This.PrimaryKeyColumn()].Property;
            return primaryKeyPropery.GetValue(obj);
        } 

        private static string Insert<T>(this ITableMapping<T> tableMapping)
        {
            return SQLBuilder.Insert(tableMapping.TableName, tableMapping.Columns.Select(x => x.Key));
        }
 
        public static ITableMappedStatement<T> PrepareInsert<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return new TableMappedStatement<T>(This.PrepareStatement(tableMapping.Insert()), tableMapping);   
        }

        private static IEnumerable<T> YieldInsertAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            using (var insertStmt = This.PrepareInsert(tableMapping))
            using (var findStmt = This.PrepareFindByRowId(tableMapping))
            {
                foreach (var obj in objects)
                {
                    insertStmt.Execute(obj);
                    var rowId = This.LastInsertedRowId;
                    yield return findStmt.Query(rowId).First();
                }
            }
        }

        public static T Insert<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.YieldInsertAll(tableMapping, new T[] { obj }).First();
        }

        public static Task<T> InsertAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj, CancellationToken cancellationToken)
        {
            return This.Use((x, ct) => x.Insert(tableMapping, obj), cancellationToken);
        }

        public static Task<T> InsertAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.InsertAsync(tableMapping, obj, CancellationToken.None);
        }

        public static IReadOnlyList<T> InsertAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            return This.YieldInsertAll(tableMapping, objects).ToList();
        }

        public static IObservable<T> InsertAll<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            return This.Use(db => db.YieldInsertAll(tableMapping, objects));
        }

        private static string InsertOrReplace<T>(this ITableMapping<T> tableMapping)
        {
            return SQLBuilder.InsertOrReplace(tableMapping.TableName, tableMapping.Columns.Select(x => x.Key));     
        }

        public static ITableMappedStatement<T> PrepareInsertOrReplace<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return new TableMappedStatement<T>(This.PrepareStatement(tableMapping.InsertOrReplace()), tableMapping);   
        }

        private static IEnumerable<T> YieldInsertOrReplaceAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            using (var insertOrReplaceStmt = This.PrepareInsertOrReplace(tableMapping))
            using (var findStmt = This.PrepareFindByRowId(tableMapping))
            {
                foreach (var obj in objects)
                {
                    insertOrReplaceStmt.Execute(obj);
                    var rowId = This.LastInsertedRowId;
                    yield return findStmt.Query(rowId).First();
                } 
            }
        }
            
        public static T InsertOrReplace<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.YieldInsertOrReplaceAll(tableMapping, new T[] {obj}).First();
        }

        public static Task<T> InsertOrReplaceAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj, CancellationToken cancellationToken)
        {
            return This.Use((x, ct) => x.InsertOrReplace(tableMapping, obj), cancellationToken);
        }

        public static Task<T> InsertOrReplaceAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.InsertOrReplaceAsync(tableMapping, obj, CancellationToken.None);
        }

        public static IReadOnlyList<T> InsertOrReplaceAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            return This.YieldInsertOrReplaceAll(tableMapping, objects).ToList();
        }

        public static IObservable<T> InsertOrReplaceAll<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            return This.Use(db => db.YieldInsertOrReplaceAll(tableMapping, objects));
        }

        private static string Update<T>(this ITableMapping<T> tableMapping)
        {
            return SQLBuilder.Update(tableMapping.TableName, tableMapping.Columns.Select(x => x.Key), tableMapping.PrimaryKeyColumn());     
        }
            
        public static ITableMappedStatement<T> PrepareUpdate<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return new TableMappedStatement<T>(This.PrepareStatement(tableMapping.Update()), tableMapping);   
        }

        private static IEnumerable<T> YieldUpdateAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            using (var updateAllStmt = This.PrepareUpdate(tableMapping))
            using (var findStmt = This.PrepareFindByRowId(tableMapping))
            {
                foreach (var obj in objects)
                {
                    updateAllStmt.Execute(obj);
                    var rowId = This.LastInsertedRowId;
                    yield return findStmt.Query(rowId).First();
                }
            }
        }
      
        public static T Update<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.YieldUpdateAll(tableMapping, new T[] {obj}).First();
        }

        public static Task<T> UpdateAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj, CancellationToken cancellationToken)
        {
            return This.Use((x, ct) => x.Update<T>(tableMapping, obj), cancellationToken);
        }

        public static Task<T> UpdateAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.Use(x => x.Update<T>(tableMapping, obj));
        }

        public static IReadOnlyList<T> UpdateAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            return This.YieldUpdateAll(tableMapping, objects).ToList();
        }

        public static IObservable<T> UpdateAll<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            return This.Use(db => db.YieldUpdateAll(tableMapping, objects));
        }

        public static ITableMappedStatement<T> PrepareDelete<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return new TableMappedStatement<T>(
                This.PrepareDelete(tableMapping.TableName, tableMapping.PrimaryKeyColumn()), 
                tableMapping);   
        }

        private static IEnumerable<T> YieldDeleteAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable primaryKeys)
        {
            using (var deleteStmt = This.PrepareDelete(tableMapping))
            using (var findStmt = This.PrepareFind(tableMapping))
            {
                foreach (var primaryKey in primaryKeys)
                {
                    var result = findStmt.Query(primaryKey).Select(x =>
                        {
                            deleteStmt.Execute(primaryKey);
                            return x;
                        }).FirstOrDefault();
                    yield return result;
                }
            }
        }

        private static T Delete<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, object primaryKey)
        {
            return This.YieldDeleteAll(tableMapping, new object[] { primaryKey }).FirstOrDefault();
        }

        /*
        internal static Task<T> DeleteAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, object primaryKey, CancellationToken cancellationToken)
        {
            return This.Use((x, ct) => x.Delete(tableMapping, primaryKey), cancellationToken);
        }

        internal static Task<T> DeleteAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, object primaryKey)
        {
            return This.DeleteAsync(tableMapping, primaryKey, CancellationToken.None);
        }*/

        public static T Delete<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            var primaryKey = tableMapping.GetPrimaryKey(obj);
            return This.Delete(tableMapping, primaryKey);
        }

        public static Task<T> DeleteAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj, CancellationToken cancellationToken)
        {
            return This.Use((x, ct) => x.Delete(tableMapping, obj), cancellationToken);
        }

        public static Task<T> DeleteAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, T obj)
        {
            return This.DeleteAsync(tableMapping, obj, CancellationToken.None);
        }
            
        private static IReadOnlyList<T> DeleteAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable primaryKeys)
        {
            return This.YieldDeleteAll(tableMapping, primaryKeys).ToList();
        }
            
        private static IObservable<T> DeleteAll<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable primaryKeys)
        {
            return This.Use(db => db.YieldDeleteAll(tableMapping, primaryKeys));
        }

        public static IReadOnlyList<T> DeleteAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            var primaryKeys = objects.Select(tableMapping.GetPrimaryKey);
            return This.DeleteAll<T>(tableMapping, primaryKeys);
        }

        public static IObservable<T> DeleteAll<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, IEnumerable<T> objects)
        {
            var primaryKeys = objects.Select(tableMapping.GetPrimaryKey);
            return This.DeleteAll<T>(tableMapping, primaryKeys);
        }

        public static void DeleteAll<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            This.DeleteAll(tableMapping.TableName);
        }

        public static Task DeleteAllAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return This.Use(db => db.DeleteAll(tableMapping));
        }

        public static void DropTable<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            This.DropTable(tableMapping.TableName);
        }

        public static Task DropTableAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            return This.Use(db => db.DropTable(tableMapping));
        }

        public static ITableMapping<T> Create<T>()
        {
            Func<object> builder = () => Activator.CreateInstance<T>();
            Func<object, T> build = obj => (T) obj;

            return TableMapping.Create(builder, build);
        }
            
        public static ITableMapping<T> Create<T>(Func<object> builder, Func<object, T> build)
        {
            var mappedType = typeof(T);
            var tableName = mappedType.GetTableName();
            var props = mappedType.GetPublicInstanceProperties();

            // FIXME: I wish this was immutable
            var columnToMapping = new Dictionary<string, ColumnMapping>();

            var indexes = new Dictionary<string,IndexInfo>();

            // Add single column indexes
            foreach (var prop in props.Where(prop => !prop.Ignore()))
            {
                var name = prop.GetColumnName();
                var columnType = prop.PropertyType.GetUnderlyingType();
                var metadata = CreateColumnMetadata(prop);
                columnToMapping.Add(name, new ColumnMapping(columnType, prop, metadata));

                var columnIndex = prop.GetColumnIndex();
                if (columnIndex != null)
                {
                    var indexName = SQLBuilder.NameIndex(tableName, name);
                    indexes.Add(
                        indexName,
                        new IndexInfo(
                            columnIndex.Unique,
                            new string[] { name }));
                }
            }

            // Add composite indexes.
            foreach (var compositeIndex in mappedType.GetCompositeIndexes())
            {
                var indexName = compositeIndex.Name ?? SQLBuilder.NameIndex(tableName, compositeIndex.Columns);
                var columns = compositeIndex.Columns.ToList();

                // Validate the column names
                foreach (var column in columns)
                {
                    if (!columnToMapping.ContainsKey(column)) { throw new ArgumentException(column + " is not a valid column name."); }
                }

                indexes.Add(
                    indexName,
                    new IndexInfo(
                        compositeIndex.Unique,
                        columns));
            }

            // Validate primary keys
            var primaryKeyParts = columnToMapping.Where(x => x.Value.Metadata.IsPrimaryKeyPart).ToList();
            if (primaryKeyParts.Count < 1)
            {
                throw new ArgumentException("Table mapping requires at least one primary key");
            }

            return new TableMapping<T>(builder, build, tableName, columnToMapping, indexes);
        }
            
        private static TableColumnMetadata CreateColumnMetadata(PropertyInfo prop)
        {
            var definedType = prop.PropertyType;
            var columnType = definedType.GetUnderlyingType();
            var collation = prop.GetCollationSequence();
            var isPK = prop.IsPrimaryKeyPart();

            var isNullableInt = (definedType == typeof(Nullable<int>)) || (definedType == typeof(Nullable<long>));
            var isAutoInc = isPK && isNullableInt;

            var hasNotNullConstraint = isPK || prop.HasNotNullConstraint() || definedType.GetTypeInfo().IsValueType;

            return new TableColumnMetadata(columnType.GetSQLiteType().ToSQLDeclaredType(), collation, hasNotNullConstraint, isPK, isAutoInc);
        }

        private static string ToSQLDeclaredType(this SQLiteType This)
        {
            switch (This)
            {
                case SQLiteType.Integer:
                    return "INTEGER";
                case SQLiteType.Float:
                    return "REAL";
                case SQLiteType.Text:
                    return "TEXT";
                case SQLiteType.Blob:
                    return "BLOB";
                case SQLiteType.Null:
                    return "NULL";
                default:
                    throw new ArgumentException("Invalid SQLite type.");
            }
        }

        private static SQLiteType GetSQLiteType(this Type clrType)
        {
            if (clrType == typeof(Boolean)        || 
                clrType == typeof(Byte)           || 
                clrType == typeof(UInt16)         || 
                clrType == typeof(SByte)          || 
                clrType == typeof(Int16)          || 
                clrType == typeof(Int32)          ||
                clrType == typeof(UInt32)         || 
                clrType == typeof(Int64)          ||
                clrType == typeof(TimeSpan)       ||
                clrType == typeof(DateTime)       ||
                clrType == typeof(DateTimeOffset) ||  
                clrType.GetTypeInfo().IsEnum)
            { 
                return SQLiteType.Integer; 
            } 
                
            else if (clrType == typeof(Single) || clrType == typeof(Double) || clrType == typeof(Decimal)) { return SQLiteType.Float; } 
            else if (clrType == typeof(String) || clrType == typeof(Guid) || clrType == typeof(Uri))       { return SQLiteType.Text; } 
            else if (clrType == typeof(byte[]) || clrType.IsSameOrSubclass(typeof(Stream)))                { return SQLiteType.Blob; } 
            else 
            {
                throw new NotSupportedException ("Don't know about " + clrType);
            }
        }

        internal static object ToObject(this ISQLiteValue value, Type clrType)
        {
            if (value.SQLiteType == SQLiteType.Null)    { return null; } 
            else if (clrType == typeof(String))         { return value.ToString(); } 
            else if (clrType == typeof(Int32))          { return value.ToInt(); } 
            else if (clrType == typeof(Boolean))        { return value.ToBool(); } 
            else if (clrType == typeof(double))         { return value.ToDouble(); } 
            else if (clrType == typeof(float))          { return value.ToFloat(); } 
            else if (clrType == typeof(TimeSpan))       { return value.ToTimeSpan(); } 
            else if (clrType == typeof(DateTime))       { return value.ToDateTime(); } 
            else if (clrType == typeof(DateTimeOffset)) { return value.ToDateTimeOffset(); }
            else if (clrType.GetTypeInfo().IsEnum)      { return value.ToInt(); } 
            else if (clrType == typeof(Int64))          { return value.ToInt64(); } 
            else if (clrType == typeof(UInt32))         { return value.ToUInt32(); } 
            else if (clrType == typeof(decimal))        { return value.ToDecimal(); } 
            else if (clrType == typeof(Byte))           { return value.ToByte(); } 
            else if (clrType == typeof(UInt16))         { return value.ToUInt16(); } 
            else if (clrType == typeof(Int16))          { return value.ToShort(); } 
            else if (clrType == typeof(sbyte))          { return value.ToSByte(); } 
            else if (clrType == typeof(byte[]))         { return value.ToBlob(); } 
            else if (clrType == typeof(Guid))           { return value.ToGuid(); } 
            else if (clrType == typeof(Uri))            { return value.ToUri(); } 
            else 
            {
                throw new NotSupportedException ("Don't know how to read " + clrType);
            }
        }
    }

    internal sealed class TableMapping<T> : ITableMapping<T>
    {
        private readonly Func<object> builder;
        private readonly Func<object,T> build;

        private readonly string tableName;
        private readonly IReadOnlyDictionary<string, ColumnMapping> columns;
        private readonly IReadOnlyDictionary<string, IndexInfo> indexes;

        internal TableMapping(
            Func<object> builder, 
            Func<object,T> build, 
            string tableName,
            IReadOnlyDictionary<string, ColumnMapping> columns,
            IReadOnlyDictionary<string, IndexInfo> indexes)
        {
            this.builder = builder;
            this.build = build;
            this.tableName = tableName;
            this.columns = columns;
            this.indexes = indexes;
        }

        public String TableName { get { return tableName; } }

        public IReadOnlyDictionary<string, IndexInfo> Indexes { get { return indexes; } }

        public IReadOnlyDictionary<string,ColumnMapping> Columns { get { return columns; } }

        public T ToObject(IReadOnlyList<IResultSetValue> row)
        {
            var builder = this.builder();

            foreach (var resultSetValue in row)
            {
                var columnName = resultSetValue.ColumnInfo.OriginName;
                ColumnMapping columnMapping; 

                if (columns.TryGetValue(columnName, out columnMapping))
                {
                    var value = resultSetValue.ToObject(columnMapping.ClrType);
                    var prop = columnMapping.Property;
                    prop.SetValue (builder, value, null);
                }
            }

            return build(builder);
        }
    }
}
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
    public sealed class ColumnMapping: IEquatable<ColumnMapping>
    {
        /// <summary>
        /// Indicates whether the two ColumnMapping instances are equal to each other.
        /// </summary>
        /// <param name="x">A ColumnMapping instance.</param>
        /// <param name="y">A ColumnMapping instance.</param>
        /// <returns><see langword="true"/> if the two instances are equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator ==(ColumnMapping x, ColumnMapping y)
        {
            return x.Equals(y);
        }

        /// <summary>
        /// Indicates whether the two ColumnMapping instances are not equal each other.
        /// </summary>
        /// <param name="x">A ColumnMapping instance.</param>
        /// <param name="y">A ColumnMapping instance.</param>
        /// <returns><see langword="true"/> if the two instances are not equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator !=(ColumnMapping x, ColumnMapping y)
        {
            return !(x == y);
        }

        private readonly Type clrType;
        private readonly PropertyInfo property;
        private readonly TableColumnMetadata metadata;

        internal ColumnMapping(Type clrType, PropertyInfo property, TableColumnMetadata metadata)
        {
            this.clrType = clrType;
            this.property = property;
            this.metadata = metadata;
        }

        public Type ClrType { get { return clrType; } }

        public PropertyInfo Property { get { return property; } }

        public TableColumnMetadata Metadata { get { return metadata; } }

        /// <inheritdoc/>
        public bool Equals(ColumnMapping other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.ClrType == other.ClrType &&
                this.Property == other.Property &&
                this.Metadata == other.Metadata;
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return other is ColumnMapping && this == (ColumnMapping)other;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + this.ClrType.GetHashCode();
            hash = hash * 31 + this.Property.GetHashCode();
            hash = hash * 31 + this.Metadata.GetHashCode();
            return hash;
        }
    }

    public interface ITableMapping : IReadOnlyDictionary<string, ColumnMapping>
    {
        String TableName { get; }

        ColumnMapping this[string column] { get; }

        IEnumerable<IndexInfo> Indexes { get; }
    }

    public interface ITableMapping<T> : ITableMapping
    {
        T ToObject(IReadOnlyList<IResultSetValue> row);
    }

    public static class TableMapping
    {   
        internal static string PropertyToColumnName(PropertyInfo prop)
        {
            var colAttr = (ColumnAttribute)prop.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();
            return colAttr == null ? prop.Name : colAttr.Name;
        }

        internal static void Bind<T>(this IStatement This, ITableMapping<T> tableMapping, T obj)
        {
            foreach (var column in tableMapping)
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
            This.CreateTableIfNotExists(tableMapping.TableName, CreateFlags.None, tableMapping.Select(x => Tuple.Create(x.Key, x.Value.Metadata)));

            if (This.Changes == 0)
            {
                This.MigrateTable(tableMapping);
            }

            foreach (var index in tableMapping.Indexes) 
            {
                This.CreateIndex(index.Name, tableMapping.TableName, index.Columns, index.Unique);
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
            foreach (var p in tableMapping) 
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
                mapping.Where(x => x.Value.Metadata.IsPrimaryKeyPart).Select(x => x.Key).First());
        }

        private static object GetPrimaryKey<T>(this ITableMapping<T> This, T obj)
        {
            var primaryKeyPropery = This[This.PrimaryKeyColumn()].Property;
            return primaryKeyPropery.GetValue(obj);
        } 

        private static string Insert<T>(this ITableMapping<T> tableMapping)
        {
            return SQLBuilder.Insert(tableMapping.TableName, tableMapping.Select(x => x.Key));
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
            return SQLBuilder.InsertOrReplace(tableMapping.TableName, tableMapping.Select(x => x.Key));     
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
            return SQLBuilder.Update(tableMapping.TableName, tableMapping.Select(x => x.Key), tableMapping.PrimaryKeyColumn());     
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

            var tableAttr = 
                (TableAttribute)CustomAttributeExtensions.GetCustomAttribute(mappedType.GetTypeInfo(), typeof(TableAttribute), true);

            // TableAttribute name can be null
            var tableName = tableAttr != null ? (tableAttr.Name ?? mappedType.Name) : mappedType.Name;

            var props = mappedType.GetRuntimeProperties().Where(p => p.GetMethod != null && p.GetMethod.IsPublic && !p.GetMethod.IsStatic);

            // FIXME: I wish this was immutable
            var columnToMapping = new Dictionary<string, ColumnMapping>();

            // map each column to it's index attributes
            var columnToIndexMapping = new Dictionary<string, IEnumerable<IndexedAttribute>>();
            foreach (var prop in props)
            {
                if (prop.GetCustomAttributes(typeof(IgnoreAttribute), true).Count() == 0)
                {
                    var columnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                    var colAttr = (ColumnAttribute)prop.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();
                    var name = colAttr == null ? prop.Name : colAttr.Name;

                    var metadata = CreateColumnMetadata(prop);

                    columnToMapping.Add(name, new ColumnMapping(columnType, prop, metadata));

                    var columnIndexes = GetIndexes(prop);

                    columnToIndexMapping.Add(name, columnIndexes);
                }
            }

            // A Map of the index name to its columns and whether its unique or not
            var indexToColumns = new Dictionary<string, Tuple<bool, Dictionary<int, string>>>();

            foreach (var columnIndex in columnToIndexMapping)
            {
                foreach (var indexAttribute in columnIndex.Value)
                {
                    var indexName = indexAttribute.Name ?? SQLBuilder.NameIndex(tableName, columnIndex.Key);

                    Tuple<bool, Dictionary<int, string>> iinfo;
                    if (!indexToColumns.TryGetValue(indexName, out iinfo))
                    {
                        iinfo = Tuple.Create(indexAttribute.Unique, new Dictionary<int,string>());
                        indexToColumns.Add(indexName, iinfo);
                    }

                    if (indexAttribute.Unique != iinfo.Item1)
                    {
                        throw new Exception("All the columns in an index must have the same value for their Unique property");
                    }

                    if (iinfo.Item2.ContainsKey(indexAttribute.Order))
                    {
                        throw new Exception("Ordered columns must have unique values for their Order property.");
                    }

                    iinfo.Item2.Add(indexAttribute.Order, columnIndex.Key);
                }
            }
 
            var indexes = 
                indexToColumns.Select(x => 
                    new IndexInfo(
                        x.Key, 
                        x.Value.Item1, 
                        x.Value.Item2.OrderBy(col => col.Key).Select(col => col.Value).ToList()
                    )
                ).ToList();

            return new TableMapping<T>(builder, build, tableName, columnToMapping, indexes);
        }
            
        private static TableColumnMetadata CreateColumnMetadata(PropertyInfo prop)
        {
            var definedType = prop.PropertyType;

            // If this type is Nullable<T> then Nullable.GetUnderlyingType returns the T, otherwise it returns null, so get the actual type instead
            var columnType = Nullable.GetUnderlyingType(definedType) ?? definedType;
            var collation = prop.Collation();
            var isPK = prop.IsPrimaryKey();

            var pkIsAutoInc = false;
            if (isPK && definedType.GetTypeInfo().IsGenericType &&
                ((definedType.GetGenericTypeDefinition() == typeof(Nullable<int>)) ||
                 (definedType.GetGenericTypeDefinition() == typeof(Nullable<long>))
                ))
            {
                pkIsAutoInc = true;
            }

            var isAutoInc = prop.IsAutoIncrement() || pkIsAutoInc;

            var hasNotNullConstraint = isPK || IsMarkedNotNull(prop);

            return new TableColumnMetadata(columnType.GetSqlType(), collation, hasNotNullConstraint, isPK, isAutoInc);
        }

        private const int DefaultMaxStringLength = 140;

        private static string GetSqlType(this Type clrType)
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
                return "INTEGER"; 
            } 
                
            else if (clrType == typeof(Single) || clrType == typeof(Double) || clrType == typeof(Decimal))               { return "REAL"; } 
            else if (clrType == typeof(String) || clrType == typeof(Guid) || clrType == typeof(Uri))                     { return "TEXT"; } 
            else if (clrType == typeof(byte[]) || clrType.GetTypeInfo().IsAssignableFrom(typeof(Stream).GetTypeInfo()))  { return "BLOB"; } 
            else 
            {
                throw new NotSupportedException ("Don't know about " + clrType);
            }
        }

        private static bool IsPrimaryKey (this MemberInfo p)
        {
            var attrs = p.GetCustomAttributes (typeof(PrimaryKeyAttribute), true);
            return attrs.Count() > 0;
        }

        private static string Collation (this MemberInfo p)
        {
            var attrs = p.GetCustomAttributes (typeof(CollationAttribute), true);
            if (attrs.Count() > 0) {
                return ((CollationAttribute)attrs.First()).Value;
            } else {
                return string.Empty;
            }
        }

        private static bool IsAutoIncrement (this MemberInfo p)
        {
            var attrs = p.GetCustomAttributes (typeof(AutoIncrementAttribute), true);
            return attrs.Count() > 0;
        }

        private static IEnumerable<IndexedAttribute> GetIndexes(this MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(IndexedAttribute), true);
            return attrs.Cast<IndexedAttribute>();
        }

        private static bool IsMarkedNotNull(this MemberInfo p)
        {
            var attrs = p.GetCustomAttributes (typeof (NotNullAttribute), true);
            return attrs.Count() > 0;
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

        private readonly IReadOnlyDictionary<string, ColumnMapping> columnToMapping;

        // FIXME: To implement equality correctly this should be a set
        private readonly IReadOnlyList<IndexInfo> indexes;

        internal TableMapping(
            Func<object> builder, 
            Func<object,T> build, 
            string tableName,
            IReadOnlyDictionary<string, ColumnMapping> columnToMapping,

            // FIXME: To implement equality correctly this should be a set
            IReadOnlyList<IndexInfo> indexes)
        {
            this.builder = builder;
            this.build = build;
            this.tableName = tableName;
            this.columnToMapping = columnToMapping;
            this.indexes = indexes;
        }

        public String TableName { get { return tableName; } }

        public IEnumerable<IndexInfo> Indexes { get { return indexes.AsEnumerable(); } }

        public ColumnMapping this [string column] { get  { return columnToMapping[column]; } }

        public T ToObject(IReadOnlyList<IResultSetValue> row)
        {
            var builder = this.builder();

            foreach (var resultSetValue in row)
            {
                var columnName = resultSetValue.ColumnInfo.OriginName;
                ColumnMapping columnMapping; 

                if (columnToMapping.TryGetValue(columnName, out columnMapping))
                {
                    var value = resultSetValue.ToObject(columnMapping.ClrType);
                    var prop = columnMapping.Property;
                    prop.SetValue (builder, value, null);
                }
            }

            return build(builder);
        }

        public IEnumerator<KeyValuePair<string, ColumnMapping>> GetEnumerator()
        {
            return this.columnToMapping.GetEnumerator();
        }
            
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public bool ContainsKey(string key)
        {
            return this.columnToMapping.ContainsKey(key);
        }

        public bool TryGetValue(string column, out ColumnMapping mapping)
        {
            return this.columnToMapping.TryGetValue(column, out mapping);
        }

        public IEnumerable<string> Keys
        {
            get
            {
                return this.columnToMapping.Keys;
            }
        }

        public IEnumerable<ColumnMapping> Values
        {
            get
            {
                return this.columnToMapping.Values;
            }
        }

        public int Count
        {
            get
            {
                return this.columnToMapping.Count;
            }
        }
    }
}
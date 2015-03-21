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
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.Orm
{
    /// <summary>
    /// Extensions methods for instances of <see cref="ITableMapping"/>
    /// </summary>
    public static partial class TableMapping
    {   
        internal static void Bind<T>(this IStatement This, ITableMapping<T> tableMapping, T obj) {
            foreach (var column in tableMapping.Columns)
            {
                var key = ":" + column.Key;
                var value = column.Value.Property.GetValue(obj);
                This.BindParameters[key].Bind(value);
            }
        }

        /*
        internal static TableQuery<T> Query<T>(this ITableMapping<T> This)
        {
            return new TableQuery<T>(This, "*", null, new List<Ordering>(), null, null);
        }*/

                                                                
        /// <summary>
        /// Creates or migrate a table in the database for the given table mapping, creating indexes if needed.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static void InitTable<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);

            This.RunInTransaction(_ =>
                {
                    This.CreateTableIfNotExists(tableMapping.TableName, CreateFlags.None, tableMapping.Columns);

                    if (This.Changes != 0)
                    {
                        This.MigrateTable(tableMapping);
                    }

                    foreach (var index in tableMapping.Indexes) 
                    {
                        This.CreateIndex(index.Key, tableMapping.TableName, index.Value.Columns, index.Value.Unique);
                    }
                });
        }

        /// <summary>
        /// Creates or migrate a table in the database for the given table mapping, creating indexes if needed.
        /// </summary>
        /// <returns>A task that completes once the table is succesfully created and is ready for use.</returns>
        /// <param name="This">The database connection</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task InitTableAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);

            return This.Use((db, ct) => db.InitTable(tableMapping), cancellationToken);
        }

        /// <summary>
        /// Creates or migrate a table in the database for the given table mapping, creating indexes if needed.
        /// </summary>
        /// <returns>A task that completes once the table is succesfully created and is ready for use.</returns>
        /// <param name="This">The database connection</param>
        /// <param name="tableMapping">The table mapping.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task InitTableAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);

            return This.InitTableAsync(tableMapping, CancellationToken.None);
        }
            
        private static void MigrateTable<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            var existingCols = This.GetTableInfo(tableMapping.TableName);
            
            var toBeAdded = new List<KeyValuePair<string, ColumnMapping>> ();

            foreach (var p in tableMapping.Columns) 
            {
                if (!existingCols.ContainsKey(p.Key)) { toBeAdded.Add (p); }
            }
            
            foreach (var p in toBeAdded) 
            {
                This.Execute (SQLBuilder.AlterTableAddColumn(tableMapping.TableName, p.Key, p.Value));
            }
        }

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

        /// <summary>
        /// Drops the table if exists.
        /// </summary>
        /// <param name="This">This.</param>
        /// <param name="tableMapping">Table mapping.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static void DropTableIfExists<T>(this IDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);

            This.DropTableIfExists(tableMapping.TableName);
        }

        /// <summary>
        /// Drops the table if exists async.
        /// </summary>
        /// <returns>The table if exists async.</returns>
        /// <param name="This">This.</param>
        /// <param name="tableMapping">Table mapping.</param>
        /// <param name="ct">Ct.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task DropTableIfExistsAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping, CancellationToken ct)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);

            return This.Use((db, _) => db.DropTableIfExists(tableMapping), ct);
        }

        /// <summary>
        /// Drops the table if exists async.
        /// </summary>
        /// <returns>The table if exists async.</returns>
        /// <param name="This">This.</param>
        /// <param name="tableMapping">Table mapping.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task DropTableIfExistsAsync<T>(this IAsyncDatabaseConnection This, ITableMapping<T> tableMapping)
        {
            Contract.Requires(This != null);
            Contract.Requires(tableMapping != null);

            return This.DropTableIfExistsAsync(tableMapping, CancellationToken.None);
        }

        /// <summary>
        /// Create a table mapping for a mutable type.
        /// </summary>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static ITableMapping<T> Create<T>()
        {
            Func<object> builder = () => Activator.CreateInstance<T>();
            Func<object, T> build = obj => (T) obj;

            return TableMapping.Create(builder, build);
        }
            
        /// <summary>
        /// Creates a table mapping instance that will use the given builder to build new instances.
        /// </summary>
        /// <param name="builder">A builder provider that provides builder instances used to build a new result. This function may
        /// either always return a new builder instance or may return a builder with thread affinity to a single thread.</param>
        /// <param name="build">A function to call with the builder object that returns a result object.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static ITableMapping<T> Create<T>(Func<object> builder, Func<object, T> build)
        {
            Contract.Requires(builder != null);
            Contract.Requires(build != null);

            var mappedType = typeof(T);
            var tableName = mappedType.GetTableName();
            var props = mappedType.GetPublicInstanceProperties();

            // FIXME: I wish this was immutable
            var columns = new Dictionary<string, ColumnMapping>();

            var indexes = new Dictionary<string,IndexInfo>();

            // Add single column indexes
            foreach (var prop in props.Where(prop => !prop.Ignore()))
            {
                var name = prop.GetColumnName();
                var columnType = prop.PropertyType;
                var metadata = prop.CreateColumnMetadata();
                var defaultValue = prop.GetDefaultValue();
                columns.Add(name, new ColumnMapping(columnType, defaultValue, prop, metadata));

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
                var indexColumns = compositeIndex.Columns.ToList();

                // Validate the column names
                foreach (var column in indexColumns)
                {
                    if (!columns.ContainsKey(column))
                    {
                        throw new ArgumentException(column + " is not a valid column name.");
                    }
                }

                indexes.Add(
                    indexName,
                    new IndexInfo(
                        compositeIndex.Unique,
                        indexColumns));
            }

            // Validate primary keys
            var primaryKeyParts = columns.Where(x => x.Value.Metadata.IsPrimaryKeyPart).ToList();
            if (primaryKeyParts.Count < 1)
            {
                throw new ArgumentException("Table mapping requires at least one primary key");
            }
            else if (primaryKeyParts.Count > 1)
            {
                throw new ArgumentException("Table mapping only allows one primary key");
            }

            return new TableMapping<T>(builder, build, tableName, columns, indexes);
        }
            
        private static TableColumnMetadata CreateColumnMetadata(this PropertyInfo This)
        {
            var columnType = This.PropertyType;

            // Default SQLite collation sequence is always binary (i think)
            var collation = This.GetCollationSequence() ?? "BINARY";

            var isPK = This.IsPrimaryKey();

            var isAutoInc = false;
            if (isPK)
            {
                // Only use autoincrement if the primary key is nullable otherwise.
                if (columnType == typeof(Nullable<long>))
                {
                    isAutoInc = true;

                } 
                else if (columnType == typeof(long))
                {
                    isAutoInc = false;
                }
                else
                {
                    throw new ArgumentException("Primary key value must be of type Nullable<long>.");
                }
            }

            // Though technically not required to be not null by SQLite, we enforce that the primary key is always not null
            var hasNotNullConstraint = 
                isPK || 
                This.HasNotNullConstraint() || 
                (columnType.GetTypeInfo().IsValueType && !columnType.IsNullable());

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
            // Just in case the clrType is nullable
            clrType = clrType.GetUnderlyingType();

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
            // Just in case the clrType is nullable
            clrType = clrType.GetUnderlyingType();

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

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>The name of the table.</value>
        public String TableName { get { return tableName; } }

        /// <summary>
        /// Gets the table indexes.
        /// </summary>
        /// <value>The indexes.</value>
        public IReadOnlyDictionary<string, IndexInfo> Indexes { get { return indexes; } }

        /// <summary>
        /// Gets the table columns.
        /// </summary>
        /// <value>The columns.</value>
        public IReadOnlyDictionary<string,ColumnMapping> Columns { get { return columns; } }

        /// <summary>
        /// Converts a result set row to an instance of type T.
        /// </summary>
        /// <returns>An instance of type T.</returns>
        /// <param name="row">The result set row.</param>
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

                    // The builder may be of a different type than type T but must use the same property names
                    var builderProp = builder.GetType().GetTypeInfo().GetDeclaredProperty(columnMapping.Property.Name);
                    builderProp.SetValue (builder, value, null);
                }
            }

            return build(builder);
        }
    }
}
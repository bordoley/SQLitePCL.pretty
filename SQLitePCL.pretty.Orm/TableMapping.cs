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
    // FIXME: Implement Equality
    public sealed class TableMapping
    {
        /// <summary>
        /// Creates a table mapping instance that will use the given builder to build new instances.
        /// </summary>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static TableMapping Create<T>()
        {
            var mappedType = typeof(T);
            var tableName = mappedType.GetTableName();
            var props = mappedType.GetNotIgnoredGettableProperties();

            // FIXME: I wish this was immutable
            var columns = new Dictionary<string, ColumnMapping>();

            var indexes = new Dictionary<string,IndexInfo>();

            // Add single column indexes
            foreach (var prop in props)
            {
                var columnMapping = prop.GetColumnMapping();
                columns.Add(columnMapping.Key, columnMapping.Value);

                var columnIndex = prop.GetColumnIndex();
                if (columnIndex != null)
                {
                    var indexName = SQLBuilder.NameIndex(tableName, columnMapping.Key);
                    indexes.Add(
                        indexName,
                        new IndexInfo(
                            columnIndex.Unique,
                            new string[] { columnMapping.Key }));
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

            return new TableMapping(tableName, columns, indexes);
        }

        private readonly string tableName;
        private readonly IReadOnlyDictionary<string, ColumnMapping> columns;
        private readonly IReadOnlyDictionary<string, IndexInfo> indexes;

        internal TableMapping(
            string tableName,
            IReadOnlyDictionary<string, ColumnMapping> columns,
            IReadOnlyDictionary<string, IndexInfo> indexes)
        {
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
    }

    internal static partial class TableMappingExt
    {   
        private static readonly ConditionalWeakTable<TableMapping, string> primaryKeyColumn = 
            new ConditionalWeakTable<TableMapping, string>();

        internal static string PrimaryKeyColumn(this TableMapping This)
        {
            return primaryKeyColumn.GetValue(This, mapping => 
                // Intentionally throw if the column doesn't have a primary key
                mapping.Columns.Where(x => 
                    x.Value.Metadata.IsPrimaryKeyPart).Select(x => x.Key).First());
        } 

        internal static KeyValuePair<string,ColumnMapping> GetColumnMapping(this PropertyInfo This)
        {
            var name = This.GetColumnName();
            var columnType = This.PropertyType;
            var metadata = This.CreateColumnMetadata();
            var defaultValue = This.GetDefaultValue();
            var fkConstraint = This.GetForeignKeyConstraint();

            if (fkConstraint != null)
            {
                // Only use autoincrement if the primary key is nullable otherwise.
                if ((columnType != typeof(Nullable<long>)) && (columnType != typeof(long)))
                {
                    throw new ArgumentException("Foreign key value must be of type long or Nullable<long>.");
                }
            }

            return new KeyValuePair<string,ColumnMapping>(name, new ColumnMapping(columnType, defaultValue, This, metadata, fkConstraint));
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
                    throw new ArgumentException("Primary key value must be of type long or Nullable<long>.");
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
    }
}
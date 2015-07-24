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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SQLitePCL.pretty.Orm.Attributes
{   
    /// <summary>
    /// Attribute used to specify the SQL table a class should be serialized to.
    /// </summary>
    [AttributeUsage (AttributeTargets.Class)]
    public sealed class TableAttribute : Attribute
    {
        /// <summary>
        /// The table name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLitePCL.pretty.Orm.Attributes.TableAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        public TableAttribute (string name)
        {
            Contract.Requires(name != null);
            Contract.Requires(name.Length != 0);
            this.Name = name;
        }
    }

    /// <summary>
    /// Attribute used to specify the column in the SQL table a property value should be serialized to.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class ColumnAttribute : Attribute
    {
        /// <summary>
        /// The column name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLitePCL.pretty.Orm.Attributes.ColumnAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        public ColumnAttribute (string name)
        {
            Contract.Requires(name != null);
            Contract.Requires(name.Length != 0);
            this.Name = name;
        }
    }

    /// <summary>
    /// Indicates that the annotated value's column is part of the table's primary key.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class PrimaryKeyAttribute : Attribute
    {
    }

    /// <summary>
    /// Class level attribute used to define a composite index.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class CompositeIndexAttribute : Attribute
    {
        private readonly List<string> columns;

        /// <summary>
        /// Whether the index is unique or not.
        /// </summary>
        public bool Unique { get; }

        /// <summary>
        /// The index name or null.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a non-unique index across one or more columns
        /// </summary>
        /// <param name="columns">The mapped column names.</param>
        public CompositeIndexAttribute(params string[] columns) : this(false, columns) 
        {
        }

        /// <summary>
        /// Creates an index across one or more columns
        /// </summary>
        /// <param name="unique">Whether the index is unique.</param>
        /// <param name="columns">The mapped column names.</param>
        public CompositeIndexAttribute(bool unique, params string[] columns)
        {
            Contract.Requires(columns != null);
            Contract.Requires(columns.Length != 0);

            this.Name = null;
            this.Unique = unique;
            this.columns = new List<string>(columns);
        }

        /// <summary>
        /// Creates anindex across one or more columns
        /// </summary>
        /// <param name="name">The index name.</param>
        /// <param name="unique">Whether the index is unique.</param>
        /// <param name="columns">The mapped column names.</param>
        public CompositeIndexAttribute(string name, bool unique, params string[] columns)
        {
            Contract.Requires(name != null);
            Contract.Requires(columns != null);
            Contract.Requires(columns.Length != 0);

            this.Name = name;
            this.Unique = unique;
            this.columns = new List<string>(columns);
        }

        /// <summary>
        /// The table columns in order which compose the index.
        /// </summary>
        public IEnumerable<string> Columns { get { return columns.AsEnumerable(); } }
    }

    /// <summary>
    /// Attribute used to specify table column indexes.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class IndexedAttribute : Attribute
    {
        /// <summary>
        /// Whether the index should be unique or not.
        /// </summary>
        public bool Unique { get; }

        /// <summary>
        /// Creates a non-unique index on the given column.
        /// </summary>
        public IndexedAttribute() : this(false)
        { 
        }

        /// <summary>
        /// Creates an index on the given column.
        /// </summary>
        /// <param name="unique">Whether the index should be unique or not.</param>
        public IndexedAttribute(bool unique)
        {
            this.Unique = unique;
        }
    }

    /// <summary>
    /// Attribute to indicate that a given property should be ignored by the table mapping.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class IgnoreAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute used to specify the collation function that should be used to sort the column.
    /// Note it is an error to annotate a non-string column with this attribute.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class CollationAttribute: Attribute
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLitePCL.pretty.Orm.Attributes.CollationAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the collation function</param>
        public CollationAttribute (string name)
        {
            Contract.Requires(name != null);

            this.Name = name;
        }
    }

    /// <summary>
    /// Indicates that a table column is not nullable.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class NotNullAttribute : Attribute
    {
        /// <summary>
        /// The default value to use if the table is migrated.
        /// </summary>
        public object DefaultValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLitePCL.pretty.Orm.Attributes.NotNullAttribute"/> class.
        /// </summary>
        /// <param name="defaultValue">The default value to use if the table is migrated.</param>
        public NotNullAttribute(object defaultValue)
        {
            Contract.Requires(defaultValue != null);
            this.DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// Indicates that a column is a foreign key constraint.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class ForeignKeyAttribute : Attribute
    {
        /// <summary>
        /// The table that constrains the annotated column.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// The column in the table that constrains the annotated column.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLitePCL.pretty.Orm.Attributes.ForeignKeyAttribute"/> class.
        /// </summary>
        /// <param name="typ">The class whose primary key constrains the property.</param>
        public ForeignKeyAttribute(Type typ)
        {
            Contract.Requires(typ != null);

            var table = TableMapping.Get(typ);

            this.TableName = table.TableName;
            this.ColumnName = table.PrimaryKeyColumn();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLitePCL.pretty.Orm.Attributes.ForeignKeyAttribute"/> class.
        /// </summary>
        /// <param name="tableName">The foreign key table.</param>
        /// <param name="columnName">The foreign key column.</param>
        public ForeignKeyAttribute(string tableName, string columnName)
        {
            Contract.Requires(tableName != null);
            Contract.Requires(columnName != null);

            this.TableName = tableName;
            this.ColumnName = columnName;
        }
    }

    internal static class OrmAttributes
    {
        internal static bool IsPrimaryKey(this PropertyInfo This)
        {
            var attrs = This.GetCustomAttributes (typeof(PrimaryKeyAttribute), true);
            return attrs.Count() > 0;
        }

        internal static bool HasNotNullConstraint(this PropertyInfo This)
        {
            var attrs = This.GetCustomAttributes<NotNullAttribute>(true);
            return attrs.Count() > 0;
        }

        internal static string GetCollationSequence(this PropertyInfo This) =>
            This.GetCustomAttributes<CollationAttribute>(true).Select(x => x.Name).FirstOrDefault();

        private static bool Ignore(this PropertyInfo This) =>
            This.GetCustomAttributes<IgnoreAttribute>(true).Count() > 0;

        internal static IndexedAttribute GetColumnIndex(this PropertyInfo This) =>
            This.GetCustomAttributes<IndexedAttribute>(true).FirstOrDefault();

        internal static string GetColumnName(this PropertyInfo This)
        {
            var colAttr = This.GetCustomAttributes<ColumnAttribute>(true).FirstOrDefault();
            return colAttr == null ? This.Name : colAttr.Name;
        }

        internal static string GetTableName(this Type This)
        {
            var tableAttr = This.GetTypeInfo().GetCustomAttribute<TableAttribute>(true);
            return tableAttr != null ? tableAttr.Name : This.Name;
        }

        internal static IEnumerable<CompositeIndexAttribute> GetCompositeIndexes(this Type This) =>
            This.GetTypeInfo().GetCustomAttributes<CompositeIndexAttribute>(true);

        internal static object GetDefaultValue(this PropertyInfo This)
        {
            var notNullAttribute = This.GetCustomAttributes<NotNullAttribute>(true).FirstOrDefault();
                 
            if      (notNullAttribute!= null)                     { return notNullAttribute.DefaultValue; }
            else if (This.PropertyType.GetTypeInfo().IsValueType) { return Activator.CreateInstance(This.PropertyType); }
            else                                                  { return null; }
        }

        internal static ForeignKeyConstraint GetForeignKeyConstraint(this PropertyInfo This) =>
            This.GetCustomAttributes<ForeignKeyAttribute>(true)
                .Select(x => new ForeignKeyConstraint(x.TableName, x.ColumnName))
                .FirstOrDefault();

        internal static IEnumerable<PropertyInfo> GetNotIgnoredGettableProperties(this Type This) =>
            This.GetPublicInstanceProperties().Where(x => !x.Ignore());

        internal static IEnumerable<PropertyInfo> GetNotIgnoredSettableProperties(this Type This) =>
            This.GetPublicInstanceSettableProperties().Where(x => !x.Ignore());
    }
}
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
using System.Diagnostics.Contracts;
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
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLitePCL.pretty.Orm.Attributes.TableAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        public TableAttribute (string name)
        {
            Contract.Requires(name != null);
            Contract.Requires(name.Length != 0);
            _name = name;
        }

        /// <summary>
        /// The table name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get { return _name; } }
    }

    /// <summary>
    /// Attribute used to specify the column in the SQL table a property value should be serialized to.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class ColumnAttribute : Attribute
    {
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLitePCL.pretty.Orm.Attributes.ColumnAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        public ColumnAttribute (string name)
        {
            Contract.Requires(name != null);
            Contract.Requires(name.Length != 0);
            _name = name;
        }

        /// <summary>
        /// The column name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get { return _name; } }
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
        private readonly bool unique;
        private readonly string name;

        // FIXME: Add constructors that take Expressions and use reflection to get the property info to column name mappings

        /// <summary>
        /// Creates a non-unique index across one or more columns
        /// </summary>
        /// <param name="name">The index name.</param>
        /// <param name="columns">The mapped column names.</param>
        public CompositeIndexAttribute(params string[] columns) : this(false, columns) 
        {
        }

        /// <summary>
        /// Creates anindex across one or more columns
        /// </summary>
        /// <param name="name">The index name.</param>
        /// <param name="unique">Whether the index is unique.</param>
        /// <param name="columns">The mapped column names.</param>
        public CompositeIndexAttribute(bool unique, params string[] columns)
        {
            Contract.Requires(columns != null);
            Contract.Requires(columns.Length != 0);

            this.name = null;
            this.unique = unique;
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

            this.name = name;
            this.unique = unique;
            this.columns = new List<string>(columns);
        }

        /// <summary>
        /// Whether the index is unique or not.
        /// </summary>
        public bool Unique { get { return unique; } }

        /// <summary>
        /// The table columns in order which compose the index.
        /// </summary>
        public IEnumerable<string> Columns { get { return columns.AsEnumerable(); } }

        /// <summary>
        /// The index name or null.
        /// </summary>
        public string Name { get { return name; } }
    }

    /// <summary>
    /// Attribute used to specify table column indexes.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class IndexedAttribute : Attribute
    {
        private readonly bool _unique;

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
            _unique = unique;
        }

        /// <summary>
        /// Whether the index should be unique or not.
        /// </summary>
        public bool Unique { get { return _unique; } }
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
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLitePCL.pretty.Orm.Attributes.CollationAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the collation function</param>
        public CollationAttribute (string name)
        {
            Contract.Requires(name != null);

            _name = name;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public string Name { get { return _name; } }
    }

    /// <summary>
    /// Indicates that a table column is not nullable.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class NotNullAttribute : Attribute
    {
    }

    internal static class OrmAttributes
    {
        internal static bool IsPrimaryKeyPart(this PropertyInfo This)
        {
            var attrs = This.GetCustomAttributes (typeof(PrimaryKeyAttribute), true);
            return attrs.Count() > 0;
        }

        internal static bool HasNotNullConstraint(this PropertyInfo This)
        {
            var attrs = This.GetCustomAttributes<NotNullAttribute>(true);
            return attrs.Count() > 0;
        }

        internal static string GetCollationSequence(this PropertyInfo This)
        {
            var attrs = This.GetCustomAttributes<CollationAttribute>(true);
            return attrs.Count() > 0 ? attrs.First().Name : string.Empty;
        }

        internal static bool Ignore(this PropertyInfo This)
        {
            return This.GetCustomAttributes<IgnoreAttribute>(true).Count() > 0;
        }

        internal static IndexedAttribute GetColumnIndex(this PropertyInfo This)
        {
            return This.GetCustomAttributes<IndexedAttribute>(true).FirstOrDefault();
        }

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

        internal static IEnumerable<CompositeIndexAttribute> GetCompositeIndexes(this Type This)
        {
            return This.GetTypeInfo().GetCustomAttributes<CompositeIndexAttribute>(true);
        }
    }
}
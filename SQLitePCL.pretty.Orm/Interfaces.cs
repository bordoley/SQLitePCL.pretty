using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace SQLitePCL.pretty.Orm
{
    /// <summary>
    /// The mapping of a type to a SQL Table.
    /// </summary>
    public interface ITableMapping
    {
        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        String TableName { get; }

        /// <summary>
        /// Gets the table columns.
        /// </summary>
        IReadOnlyDictionary<string, ColumnMapping> Columns { get; }

        /// <summary>
        /// Gets the table indexes.
        /// </summary>
        IReadOnlyDictionary<string, IndexInfo> Indexes { get; }
    }

    /// <summary>
    /// The mapping of a type <see typeparamref="T"/> to a SQL Table.
    /// </summary>
    [ContractClass(typeof(ITableMappingContract<>))]
    public interface ITableMapping<T> : ITableMapping
    {
        /// <summary>
        /// Converts a result set row to an instance of type T.
        /// </summary>
        /// <returns>An instance of type T.</returns>
        /// <param name="row">The result set row.</param>
        T ToObject(IReadOnlyList<IResultSetValue> row);
    }

    /// <summary>
    /// An <see cref="IStatement"/> that allows the current value to be retrieved 
    /// as an instance of type T, based upon an underlying table mapping.
    /// </summary>
    public interface ITableMappedStatement<T> : IStatement
    {
        /// <summary>
        /// The underlying <see cref="ITableMapping&lt;T&gt;"/> used to map the result set to an instance of T.
        /// </summary>
        ITableMapping<T> Mapping { get; }
    }

    [ContractClassFor(typeof(ITableMapping<>))]
    internal abstract class ITableMappingContract<T> : ITableMapping<T>
    {
        public T ToObject(IReadOnlyList<IResultSetValue> row)
        {
            Contract.Requires(row != null);
            return default(T);
        }

        public string TableName
        {
            get
            {
                return default(string);
            }
        }

        public IReadOnlyDictionary<string, ColumnMapping> Columns
        {
            get
            {
                return default(IReadOnlyDictionary<string, ColumnMapping>);
            }
        }

        public IReadOnlyDictionary<string, IndexInfo> Indexes
        {
            get
            {
                return default(IReadOnlyDictionary<string, IndexInfo>);
            }
        }
    }
}


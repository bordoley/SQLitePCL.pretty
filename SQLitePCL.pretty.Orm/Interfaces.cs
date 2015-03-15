using System;
using System.Collections.Generic;

namespace SQLitePCL.pretty.Orm
{
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

    public interface ITableMappedStatement<T> : IStatement
    {
        ITableMapping<T> Mapping { get; }

        T Current { get; } 
    }
}


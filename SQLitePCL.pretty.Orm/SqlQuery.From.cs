using System;
using System.Linq;
using System.Linq.Expressions;

using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.Orm
{
    public static partial class SqlQuery
    {
        public sealed class FromClause<T>
        {
            private readonly string table;

            internal FromClause(string table)
            {
                this.table = table;
            }

            public WhereClause<T> Select()
            {
                var table = TableMapping.Create<T>();
                var columns = table.Columns.Keys.Select(col => table.TableName + "." + col).ToList();
                return new WhereClause<T>(this, columns, null);
            }

            public override string ToString()
            {
                return "FROM \"" + table + "\"";
            }
        }
    }
}


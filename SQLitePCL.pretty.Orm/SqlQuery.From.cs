using System;
using System.Linq;
using System.Linq.Expressions;

using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.Orm
{
    public static partial class SqlQuery
    {
        /// <summary>
        /// The FROM clause of a SQL query.
        /// </summary>
        public sealed class FromClause<T>
        {
            private readonly string table;

            internal FromClause(string table)
            {
                this.table = table;
            }

            /// <summary>
            /// Select all columns from the table.
            /// </summary>
            public SelectClause<T> Select()
            {
                var table = TableMapping.Get<T>();
                var columns = table.Columns.Keys.Select(col => table.TableName + "." + col).ToList();
                return new SelectClause<T>(this, columns);
            }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents the current <see cref="FromClause&lt;T&gt;"/>.
            /// </summary>
            /// <returns>A <see cref="System.String"/> that represents the current <see cref="FromClause&lt;T&gt;"/>.</returns>
            public override string ToString()
            {
                return "FROM \"" + table + "\"";
            }
        }
    }
}


using System;
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
                return new WhereClause<T>(this, Expression.Constant("*"), null);
            }

            public override string ToString()
            {
                return "FROM \"" + table + "\"";
            }
        }
    }
}


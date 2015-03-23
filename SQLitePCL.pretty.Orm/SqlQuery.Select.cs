using System;
using System.Linq;
namespace SQLitePCL.pretty.Orm
{
    public static partial class SqlQuery
    {
        public sealed class SelectClause<T>
        {
            private readonly string from;

            internal SelectClause(string from)
            {
                this.from = from;
            }

            public WhereClause<T> Select()
            {
                return new WhereClause<T>(this.from, "*", null);
            }

            public WhereClause<T> Count()
            {
                return new WhereClause<T>(this.from, "COUNT(*)", null);
            }
        }
    }
}



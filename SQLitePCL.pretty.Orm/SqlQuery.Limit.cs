using System;
using System.Linq.Expressions;
using System.Collections.Generic;


namespace SQLitePCL.pretty.Orm
{
    public static partial class SqlQuery
    {
        public sealed class LimitClause : ISqlQuery
        {
            private readonly string table;
            private readonly string selection;
            private readonly Expression where;
            private readonly IReadOnlyList<Tuple<string, bool>> ordering;
            private readonly Nullable<int> limit;
            private readonly Nullable<int> offset;

            internal LimitClause(string table, string selection, Expression where, IReadOnlyList<Tuple<string, bool>> ordering, Nullable<int> limit, Nullable<int> offset)
            {
                this.table = table;
                this.selection = selection;
                this.where = where;
                this.ordering = ordering;
                this.limit = limit;
                this.offset = offset;
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
            /// </summary>
            /// <param name="n">The number of elements to return.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitClause Take(int n)
            {
                return new LimitClause(table, selection, where, ordering, n, offset);
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
            /// </summary>
            /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitClause Skip(int n)
            {
                return new LimitClause(table, selection, where, ordering, limit, n);
            }

            public override string ToString()
            {
                return SqlQuery.ToString(selection, table, where, ordering, limit, offset); 
            }

            public string ToSql()
            {
                return this.ToString();
            }
        }
    }
}


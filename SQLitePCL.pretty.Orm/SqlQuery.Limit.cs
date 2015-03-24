using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Collections.Generic;


namespace SQLitePCL.pretty.Orm
{
    public static partial class SqlQuery
    {
        public sealed class LimitClause<T> : ISqlQuery
        {
            private readonly OrderByClause<T> orderBy;
            private readonly Nullable<int> limit;
            private readonly Nullable<int> offset;

            internal LimitClause(OrderByClause<T> orderBy, Nullable<int> limit, Nullable<int> offset)
            {
                this.orderBy = orderBy;
                this.limit = limit;
                this.offset = offset;
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
            /// </summary>
            /// <param name="n">The number of elements to return.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitClause<T> Take(int n)
            {
                Contract.Requires(n >= 0);
                return new LimitClause<T>(orderBy, n, offset);
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
            /// </summary>
            /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitClause<T> Skip(int n)
            {
                Contract.Requires(n >= 0);
                return new LimitClause<T>(orderBy, limit, n);
            }

            public override string ToString()
            {
                return 
                    orderBy.ToString() +
                    (limit.HasValue ? "\r\nLIMIT " + limit.Value : "" ) +
                    (offset.HasValue ? "\r\nOFFSET " + offset.Value : "");
            }
        }
    }
}


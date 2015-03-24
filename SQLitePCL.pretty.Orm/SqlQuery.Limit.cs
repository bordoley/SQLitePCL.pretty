using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Collections.Generic;


namespace SQLitePCL.pretty.Orm
{
    public static partial class SqlQuery
    {
        /// <summary>
        /// The LIMIT clause of a SQL query.
        /// </summary>
        public sealed class LimitClause<T> : ISqlQuery
        {
            private readonly OrderByClause<T> orderBy;
            private readonly int limit;
            private readonly Nullable<int> offset;

            internal LimitClause(OrderByClause<T> orderBy, int limit, Nullable<int> offset)
            {
                this.orderBy = orderBy;
                this.limit = limit;
                this.offset = offset;
            }

            /// <summary>
            /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
            /// </summary>
            /// <param name="n">The number of elements to return.</param>
            /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
            public LimitClause<T> Take(int n)
            {
                Contract.Requires(n >= 0);
                return new LimitClause<T>(orderBy, n, offset);
            }

            /// <summary>
            /// Returns a <see cref="LimitClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
            /// </summary>
            /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
            /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
            public LimitClause<T> Skip(int n)
            {
                Contract.Requires(n >= 0);
                return new LimitClause<T>(orderBy, limit, n);
            }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents the current <see cref="LimitClause&lt;T&gt;"/>.
            /// </summary>
            /// <returns>A <see cref="System.String"/> that represents the current <see cref="LimitClause&lt;T&gt;"/>.</returns>
            public override string ToString()
            {
                return 
                    orderBy.ToString() +
                    "\r\nLIMIT " + limit +
                    (offset.HasValue ? "\r\nOFFSET " + offset.Value : "");
            }
        }
    }
}


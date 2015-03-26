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

            internal LimitClause(OrderByClause<T> orderBy, int limit)
            {
                this.orderBy = orderBy;
                this.limit = limit;
            }

            /// <summary>
            /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
            /// </summary>
            /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
            /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
            public OffsetClause<T> Skip(int n)
            {
                Contract.Requires(n >= 0);
                return new OffsetClause<T>(this, n);
            }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents the current <see cref="LimitClause&lt;T&gt;"/>.
            /// </summary>
            /// <returns>A <see cref="System.String"/> that represents the current <see cref="LimitClause&lt;T&gt;"/>.</returns>
            public override string ToString()
            {
                return 
                    orderBy.ToString() +
                    "\r\nLIMIT " + limit;
            }
        }
    }
}


using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace SQLitePCL.pretty.Orm.Sql
{
    internal static partial class SqlCompiler
    {
        internal static String CompileLimitClause(this Expression This) =>
            "LIMIT " + This.CompileExpr();
    }

    /// <summary>
    /// The LIMIT clause of a SQL query.
    /// </summary>
    public abstract class LimitClause
    {
        private readonly OrderByClause orderBy;
        private readonly string limit;

        internal LimitClause(OrderByClause orderBy, string limit)
        {
            this.orderBy = orderBy;
            this.limit = limit;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.LimitClause"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.LimitClause"/>.</returns>
        public override string ToString() =>
            orderBy + "\r\n" + limit;
    }

    /// <summary>
    /// The LIMIT clause of a SQL query.
    /// </summary>
    public sealed class LimitClause<T> : LimitClause
    {
        internal LimitClause(OrderByClause orderBy, string limit) : base(orderBy, limit)
        {
        }

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T> Skip(int n)
        {
            Contract.Requires(n >= 0);
            return new OffsetClause<T>(this, Expression.Constant(n).CompileOffsetClause());
        }
    }

    /// <summary>
    /// The LIMIT clause of a SQL query.
    /// </summary>
    public sealed class LimitClause<T1,T2> : LimitClause
    {
        internal LimitClause(OrderByClause orderBy, string limit) : base(orderBy, limit)
        {
        }

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2> Skip(int n)
        {
            Contract.Requires(n >= 0);
            return new OffsetClause<T1,T2>(this, Expression.Constant(n).CompileOffsetClause());
        }
    }

    /// <summary>
    /// The LIMIT clause of a SQL query.
    /// </summary>
    public sealed class LimitClause<T1,T2,T3> : LimitClause
    {
        internal LimitClause(OrderByClause orderBy, string limit) : base(orderBy, limit)
        {
        }

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3> Skip(int n)
        {
            Contract.Requires(n >= 0);
            return new OffsetClause<T1,T2,T3>(this, Expression.Constant(n).CompileOffsetClause());
        }
    }

    /// <summary>
    /// The LIMIT clause of a SQL query.
    /// </summary>
    public sealed class LimitClause<T1,T2,T3,T4> : LimitClause
    {
        internal LimitClause(OrderByClause orderBy, string limit) : base(orderBy, limit)
        {
        }

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3,T4> Skip(int n)
        {
            Contract.Requires(n >= 0);
            return new OffsetClause<T1,T2,T3,T4>(this, Expression.Constant(n).CompileOffsetClause());
        }
    }
}
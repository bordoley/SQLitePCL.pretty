using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;

using SQLitePCL.pretty.Orm.Attributes;
using System.Linq;

namespace SQLitePCL.pretty.Orm.Sql
{
    internal static partial class SqlCompiler
    {
        internal static string CompileOrderByOrdering(Expression expr, bool asc) =>
            expr.CompileExpr() + (asc ? "" : " DESC");
    }

    /// <summary>
    /// The ORDER BY clause of a SQL query.
    /// </summary>
    public abstract class OrderByClause : ISqlQuery
    {
        private readonly WhereClause whereClause;
        private readonly IReadOnlyList<string> orderings;

        internal OrderByClause(WhereClause whereClause, IReadOnlyList<string> orderings)
        {
            this.whereClause = whereClause;
            this.orderings = orderings;
        }

        internal WhereClause WhereClause { get { return whereClause; } }
        internal IReadOnlyList<string> Orderings { get { return orderings; } }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.OrderByClause"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.OrderByClause"/>.</returns>
        public override string ToString() =>
            this.whereClause +
                (this.orderings.Count > 0 ? $"\r\nORDER BY {string.Join(", ", this.orderings)}" : "");
    }

    /// <summary>
    /// The ORDER BY clause of a SQL query.
    /// </summary>
    public sealed class OrderByClause<T> : OrderByClause
    {
        internal OrderByClause(WhereClause whereClause, IReadOnlyList<string> orderings) : base(whereClause, orderings)
        {
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T> ThenBy<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return AddOrderBy(orderExpr, true);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T> ThenByDescending<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return AddOrderBy(orderExpr, false);
        }

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public LimitClause<T> Take(int n)
        {
            Contract.Requires(n >= 0);
            return new LimitClause<T>(this, SqlCompiler.CompileLimitClause(Expression.Constant(n)));
        }

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T> Skip(int n)
        {
            //If the LIMIT expression evaluates to a negative value, then there is no upper bound on the number of rows returned
            return new LimitClause<T>(this, SqlCompiler.CompileLimitClause(Expression.Constant(-1))).Skip(n);
        }

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T> ElementAt(int index)
        {
            Contract.Requires(index >= 0);
            return this.Take(1).Skip(index);
        }

        internal OrderByClause<T> AddOrderBy<TValue>(Expression<Func<T, TValue>> orderExpr, bool asc)
        {  
            var orderings = new List<string>(this.Orderings);
            orderings.Add(SqlCompiler.CompileOrderByOrdering(orderExpr, asc));
            return new OrderByClause<T>(this.WhereClause, orderings);
        }
    }

    /// <summary>
    /// The ORDER BY clause of a SQL query.
    /// </summary>
    public sealed class OrderByClause<T1,T2> : OrderByClause
    {
        internal OrderByClause(WhereClause whereClause, IReadOnlyList<string> orderings) : base(whereClause, orderings)
        {
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2> ThenBy<TValue>(Expression<Func<T1,T2,TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return AddOrderBy(orderExpr, true);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2> ThenByDescending<TValue>(Expression<Func<T1,T2,TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return AddOrderBy(orderExpr, false);
        }

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public LimitClause<T1,T2> Take(int n)
        {
            Contract.Requires(n >= 0);
            return new LimitClause<T1,T2>(this, SqlCompiler.CompileLimitClause(Expression.Constant(n)));
        }

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2> Skip(int n)
        {
            //If the LIMIT expression evaluates to a negative value, then there is no upper bound on the number of rows returned
            return new LimitClause<T1,T2>(this, SqlCompiler.CompileLimitClause(Expression.Constant(-1))).Skip(n);
        }

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2> ElementAt(int index)
        {
            Contract.Requires(index >= 0);
            return this.Take(1).Skip(index);
        }

        internal OrderByClause<T1,T2> AddOrderBy<TValue>(Expression<Func<T1,T2,TValue>> orderExpr, bool asc)
        {  
            var orderings = new List<string>(this.Orderings);
            orderings.Add(SqlCompiler.CompileOrderByOrdering(orderExpr, asc));
            return new OrderByClause<T1,T2>(this.WhereClause, orderings);
        }
    }

    /// <summary>
    /// The ORDER BY clause of a SQL query.
    /// </summary>
    public sealed class OrderByClause<T1,T2,T3> : OrderByClause
    {
        internal OrderByClause(WhereClause whereClause, IReadOnlyList<string> orderings) : base(whereClause, orderings)
        {
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2,T3> ThenBy<TValue>(Expression<Func<T1,T2,T3,TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return AddOrderBy(orderExpr, true);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2,T3> ThenByDescending<TValue>(Expression<Func<T1,T2,T3,TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return AddOrderBy(orderExpr, false);
        }

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public LimitClause<T1,T2,T3> Take(int n)
        {
            Contract.Requires(n >= 0);
            return new LimitClause<T1,T2,T3>(this, SqlCompiler.CompileLimitClause(Expression.Constant(n)));
        }

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3> Skip(int n)
        {
            //If the LIMIT expression evaluates to a negative value, then there is no upper bound on the number of rows returned
            return new LimitClause<T1,T2,T3>(this, SqlCompiler.CompileLimitClause(Expression.Constant(-1))).Skip(n);
        }

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3> ElementAt(int index)
        {
            Contract.Requires(index >= 0);
            return this.Take(1).Skip(index);
        }

        internal OrderByClause<T1,T2,T3> AddOrderBy<TValue>(Expression<Func<T1,T2,T3,TValue>> orderExpr, bool asc)
        {  
            var orderings = new List<string>(this.Orderings);
            orderings.Add(SqlCompiler.CompileOrderByOrdering(orderExpr, asc));
            return new OrderByClause<T1,T2,T3>(this.WhereClause, orderings);
        }
    }

    /// <summary>
    /// The ORDER BY clause of a SQL query.
    /// </summary>
    public sealed class OrderByClause<T1,T2,T3,T4> : OrderByClause
    {
        internal OrderByClause(WhereClause whereClause, IReadOnlyList<string> orderings) : base(whereClause, orderings)
        {
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2,T3,T4> ThenBy<TValue>(Expression<Func<T1,T2,T3,T4,TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return AddOrderBy(orderExpr, true);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2,T3,T4> ThenByDescending<TValue>(Expression<Func<T1,T2,T3,T4,TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return AddOrderBy(orderExpr, false);
        }

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public LimitClause<T1,T2,T3,T4> Take(int n)
        {
            Contract.Requires(n >= 0);
            return new LimitClause<T1,T2,T3,T4>(this, SqlCompiler.CompileLimitClause(Expression.Constant(n)));
        }

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3,T4> Skip(int n)
        {
            //If the LIMIT expression evaluates to a negative value, then there is no upper bound on the number of rows returned
            return new LimitClause<T1,T2,T3,T4>(this, SqlCompiler.CompileLimitClause(Expression.Constant(-1))).Skip(n);
        }

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3,T4> ElementAt(int index)
        {
            Contract.Requires(index >= 0);
            return this.Take(1).Skip(index);
        }

        internal OrderByClause<T1,T2,T3,T4> AddOrderBy<TValue>(Expression<Func<T1,T2,T3,T4,TValue>> orderExpr, bool asc)
        {  
            var orderings = new List<string>(this.Orderings);
            orderings.Add(SqlCompiler.CompileOrderByOrdering(orderExpr, asc));
            return new OrderByClause<T1,T2,T3,T4>(this.WhereClause, orderings);
        }
    }
}


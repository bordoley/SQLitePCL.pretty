using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.Orm.Sql
{
    internal static partial class SqlCompiler
    {
        internal static String CompileWhereClause(this Expression This)
        {
            var compiled = This.CompileExpr();
            return compiled.Length > 0 ? "WHERE " + This.CompileExpr() : "";
        }
    }

    /// <summary>
    /// The WHERE clause of a SQL query.
    /// </summary>
    public abstract class WhereClause : ISqlQuery
    {
        private readonly SelectClause select;
        private readonly string where;

        internal WhereClause(SelectClause select, string where)
        {
            this.select = select;
            this.where = where;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.WhereClause"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.WhereClause"/>.</returns>
        public override string ToString() =>
            this.select + "\r\n" + where;
    }

    /// <summary>
    /// The WHERE clause of a SQL query.
    /// </summary>
    public sealed class WhereClause<T> : WhereClause
    {
        internal WhereClause(SelectClause select, string where) : base(select, where)
        {
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T> OrderBy<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return CreateOrderBy(orderExpr, true);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return CreateOrderBy(orderExpr, false);
        }

        private OrderByClause<T> CreateOrderBy<TValue>(Expression<Func<T, TValue>> orderExpr, bool asc)
        {  
            Contract.Requires(orderExpr != null);
            return this.OrderByNone().AddOrderBy(orderExpr, asc);
        }

        private OrderByClause<T> OrderByNone()
        {
            var orderings = new List<string>();
            return new OrderByClause<T>(this, orderings);
        }

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public LimitClause<T> Take(int n) =>
            this.OrderByNone().Take(n);

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T> Skip(int n) =>
            this.OrderByNone().Skip(n);

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <returns>The <see cref="LimitClause&lt;T&gt;"/>.</returns>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T> ElementAt(int index) =>
            this.OrderByNone().ElementAt(index);
    }

    /// <summary>
    /// The WHERE clause of a SQL query.
    /// </summary>
    public sealed class WhereClause<T1,T2> : WhereClause
    {
        internal WhereClause(SelectClause select, string where) : base(select, where)
        {
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2> OrderBy<TValue>(Expression<Func<T1,T2,TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return CreateOrderBy(orderExpr, true);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2> OrderByDescending<TValue>(Expression<Func<T1,T2,TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return CreateOrderBy(orderExpr, false);
        }

        private OrderByClause<T1,T2> CreateOrderBy<TValue>(Expression<Func<T1,T2,TValue>> orderExpr, bool asc)
        {  
            Contract.Requires(orderExpr != null);
            return this.OrderByNone().AddOrderBy(orderExpr, asc);
        }

        private OrderByClause<T1,T2> OrderByNone()
        {
            var orderings = new List<string>();
            return new OrderByClause<T1,T2>(this, orderings);
        }

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public LimitClause<T1,T2> Take(int n) =>
            this.OrderByNone().Take(n);

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2> Skip(int n) =>
            this.OrderByNone().Skip(n);

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <returns>The <see cref="LimitClause&lt;T&gt;"/>.</returns>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2> ElementAt(int index) =>
            this.OrderByNone().ElementAt(index);
    }

    /// <summary>
    /// The WHERE clause of a SQL query.
    /// </summary>
    public sealed class WhereClause<T1,T2,T3> : WhereClause
    {
        internal WhereClause(SelectClause select, string where) : base(select, where)
        {
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2,T3> OrderBy<TValue>(Expression<Func<T1,T2,T3,TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return CreateOrderBy(orderExpr, true);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2,T3> OrderByDescending<TValue>(Expression<Func<T1,T2,T3,TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return CreateOrderBy(orderExpr, false);
        }

        private OrderByClause<T1,T2,T3> CreateOrderBy<TValue>(Expression<Func<T1,T2,T3,TValue>> orderExpr, bool asc)
        {  
            Contract.Requires(orderExpr != null);
            return this.OrderByNone().AddOrderBy(orderExpr, asc);
        }

        private OrderByClause<T1,T2,T3> OrderByNone()
        {
            var orderings = new List<string>();
            return new OrderByClause<T1,T2,T3>(this, orderings);
        }

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public LimitClause<T1,T2,T3> Take(int n) =>
            this.OrderByNone().Take(n);

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3> Skip(int n) =>
            this.OrderByNone().Skip(n);

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <returns>The <see cref="LimitClause&lt;T&gt;"/>.</returns>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3> ElementAt(int index) =>
            this.OrderByNone().ElementAt(index);
    }

    /// <summary>
    /// The WHERE clause of a SQL query.
    /// </summary>
    public sealed class WhereClause<T1,T2,T3,T4> : WhereClause
    {
        internal WhereClause(SelectClause select, string where) : base(select, where)
        {
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2,T3,T4> OrderBy<TValue>(Expression<Func<T1,T2,T3,T4,TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return CreateOrderBy(orderExpr, true);
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2,T3,T4> OrderByDescending<TValue>(Expression<Func<T1,T2,T3,T4,TValue>> orderExpr)
        {
            Contract.Requires(orderExpr != null);
            return CreateOrderBy(orderExpr, false);
        }

        private OrderByClause<T1,T2,T3,T4> CreateOrderBy<TValue>(Expression<Func<T1,T2,T3,T4,TValue>> orderExpr, bool asc)
        {  
            Contract.Requires(orderExpr != null);
            return this.OrderByNone().AddOrderBy(orderExpr, asc);
        }

        private OrderByClause<T1,T2,T3,T4> OrderByNone()
        {
            var orderings = new List<string>();
            return new OrderByClause<T1,T2,T3,T4>(this, orderings);
        }

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public LimitClause<T1,T2,T3,T4> Take(int n) =>
            this.OrderByNone().Take(n);

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3,T4> Skip(int n) =>
            this.OrderByNone().Skip(n);

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <returns>The <see cref="LimitClause&lt;T&gt;"/>.</returns>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3,T4> ElementAt(int index) =>
            this.OrderByNone().ElementAt(index);
    }
}
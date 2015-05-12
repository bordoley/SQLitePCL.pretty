using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Diagnostics.Contracts;
using System.Reflection;

using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.Orm.Sql
{
    internal static partial class SqlCompiler
    {
        private static string CompileSelectClause(this IEnumerable<Expression> This, bool distinct) =>
            "SELECT " + 
                (distinct ? "DISTINCT " : "") +
                string.Join(", ", This.Select(x => x.CompileExpr()));

        internal static string SelectAllColumnsClause(bool distinct, params Type[] types) =>
            types.SelectMany(typ =>
                {   
                    var table = TableMapping.Get(typ);
                    var parameter = Expression.Parameter(typ);
                    return table.Columns.Values.Select(x => 
                        Expression.Property(parameter, x.Property));
                }).CompileSelectClause(distinct);
    }

    /// <summary>
    /// The SELECT clause of a SQL query.
    /// </summary>
    public abstract class SelectClause : ISqlQuery
    {
        private readonly JoinClause join;
        private readonly string select;

        internal SelectClause(JoinClause join, string select)
        {
            this.join = join;
            this.select = select;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.SelectClause"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.SelectClause"/>.</returns>
        public override string ToString() => 
            select + "\r\n" + join;
    }

    /// <summary>
    /// The SELECT clause of a SQL query.
    /// </summary>
    public sealed class SelectClause<T> : SelectClause
    {
        internal SelectClause(JoinClause join, string select) : base(join, select)
        {
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        /// <typeparam name="X">The 4th bind parameter.</typeparam>
        /// <typeparam name="Y">The 5th bind parameter.</typeparam>
        /// <typeparam name="Z">The 6th bind parameter.</typeparam>
        public WhereClause<T> Where<U,V,W,X,Y,Z>(Expression<Func<T,U,V,W,X,Y,Z,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        /// <typeparam name="X">The 4th bind parameter.</typeparam>
        /// <typeparam name="Y">The 5th bind parameter.</typeparam>
        public WhereClause<T> Where<U,V,W,X,Y>(Expression<Func<T,U,V,W,X,Y,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        /// <typeparam name="X">The 4th bind parameter.</typeparam>
        public WhereClause<T> Where<U,V,W,X>(Expression<Func<T,U,V,W,X,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        public WhereClause<T> Where<U,V,W>(Expression<Func<T,U,V,W,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Filters the selected rows based on a predicate with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        public WhereClause<T> Where<U,V>(Expression<Func<T,U,V,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Filters the selected rows based on a predicate with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        public WhereClause<T> Where<U>(Expression<Func<T,U,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Filters the selected rows based on a predicate.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        public WhereClause<T> Where(Expression<Func<T, bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        private WhereClause<T> Where(Expression expr)
        {
            var where = expr.CompileWhereClause();
            return new WhereClause<T>(this, where);
        }

        private WhereClause<T> Where() =>
            this.Where(Expression.Empty());

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T> OrderBy<TValue>(Expression<Func<T, TValue>> orderExpr) =>
            this.Where().OrderBy(orderExpr);

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> orderExpr) =>
            this.Where().OrderByDescending(orderExpr);

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public LimitClause<T> Take(int n) =>
            this.Where().Take(n);

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T> Skip(int n) =>
            this.Where().Skip(n);

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <returns>The <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T> ElementAt(int index) =>
            this.Where().ElementAt(index);
    }

    /// <summary>
    /// The SELECT clause of a SQL query.
    /// </summary>
    public sealed class SelectClause<T1,T2> : SelectClause
    {
        internal SelectClause(JoinClause join, string select) : base(join, select)
        {
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        /// <typeparam name="X">The 4th bind parameter.</typeparam>
        /// <typeparam name="Y">The 5th bind parameter.</typeparam>
        /// <typeparam name="Z">The 6th bind parameter.</typeparam>
        public WhereClause<T1,T2> Where<U,V,W,X,Y,Z>(Expression<Func<T1,T2,U,V,W,X,Y,Z,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        /// <typeparam name="X">The 4th bind parameter.</typeparam>
        /// <typeparam name="Y">The 5th bind parameter.</typeparam>
        public WhereClause<T1,T2> Where<U,V,W,X,Y>(Expression<Func<T1,T2,U,V,W,X,Y,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        /// <typeparam name="X">The 4th bind parameter.</typeparam>
        public WhereClause<T1,T2> Where<U,V,W,X>(Expression<Func<T1,T2,U,V,W,X,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        public WhereClause<T1,T2> Where<U,V,W>(Expression<Func<T1,T2,U,V,W,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where(predExpr);
        }

        /// <summary>
        /// Filters the selected rows based on a predicate with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        public WhereClause<T1,T2> Where<U,V>(Expression<Func<T1,T2,U,V,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Filters the selected rows based on a predicate with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        public WhereClause<T1,T2> Where<U>(Expression<Func<T1,T2,U,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Filters the selected rows based on a predicate.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        public WhereClause<T1,T2> Where(Expression<Func<T1,T2,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        private WhereClause<T1,T2> Where(Expression expr)
        {
            var where = expr.CompileWhereClause();
            return new WhereClause<T1,T2>(this, where);
        }

        private WhereClause<T1,T2> Where() =>
            this.Where(Expression.Empty());

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2> OrderBy<TValue>(Expression<Func<T1,T2,TValue>> orderExpr) =>
            this.Where().OrderBy(orderExpr);

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2> OrderByDescending<TValue>(Expression<Func<T1,T2,TValue>> orderExpr) =>
            this.Where().OrderByDescending(orderExpr);

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public LimitClause<T1,T2> Take(int n) =>
            this.Where().Take(n);

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2> Skip(int n) =>
            this.Where().Skip(n);

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <returns>The <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2> ElementAt(int index) =>
             this.Where().ElementAt(index);
    }

    /// <summary>
    /// The SELECT clause of a SQL query.
    /// </summary>
    public sealed class SelectClause<T1,T2,T3> : SelectClause
    {
        internal SelectClause(JoinClause join, string select) : base(join, select)
        {
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        /// <typeparam name="X">The 4th bind parameter.</typeparam>
        /// <typeparam name="Y">The 5th bind parameter.</typeparam>
        /// <typeparam name="Z">The 6th bind parameter.</typeparam>
        public WhereClause<T1,T2,T3> Where<U,V,W,X,Y,Z>(Expression<Func<T1,T2,T3,U,V,W,X,Y,Z,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        /// <typeparam name="X">The 4th bind parameter.</typeparam>
        /// <typeparam name="Y">The 5th bind parameter.</typeparam>
        public WhereClause<T1,T2,T3> Where<U,V,W,X,Y>(Expression<Func<T1,T2,T3,U,V,W,X,Y,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        /// <typeparam name="X">The 4th bind parameter.</typeparam>
        public WhereClause<T1,T2,T3> Where<U,V,W,X>(Expression<Func<T1,T2,T3,U,V,W,X,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        public WhereClause<T1,T2,T3> Where<U,V,W>(Expression<Func<T1,T2,T3,U,V,W,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where(predExpr);
        }

        /// <summary>
        /// Filters the selected rows based on a predicate with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        public WhereClause<T1,T2,T3> Where<U,V>(Expression<Func<T1,T2,T3,U,V,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Filters the selected rows based on a predicate with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        public WhereClause<T1,T2,T3> Where<U>(Expression<Func<T1,T2,T3,U,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Filters the selected rows based on a predicate.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        public WhereClause<T1,T2,T3> Where(Expression<Func<T1,T2,T3,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        private WhereClause<T1,T2,T3> Where(Expression expr)
        {
            var where = expr.CompileWhereClause();
            return new WhereClause<T1,T2,T3>(this, where);
        }

        private WhereClause<T1,T2,T3> Where() =>
            this.Where(Expression.Empty());

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2,T3> OrderBy<TValue>(Expression<Func<T1,T2,T3,TValue>> orderExpr) =>
            this.Where().OrderBy(orderExpr);

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2,T3> OrderByDescending<TValue>(Expression<Func<T1,T2,T3,TValue>> orderExpr) =>
            this.Where().OrderByDescending(orderExpr);

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public LimitClause<T1,T2,T3> Take(int n) =>
             this.Where().Take(n);

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3> Skip(int n) =>
            this.Where().Skip(n);

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <returns>The <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3> ElementAt(int index) =>
            this.Where().ElementAt(index);
    }

    /// <summary>
    /// The SELECT clause of a SQL query.
    /// </summary>
    public sealed class SelectClause<T1,T2,T3,T4> : SelectClause
    {
        internal SelectClause(JoinClause join, string select) : base(join, select)
        {
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        /// <typeparam name="X">The 4th bind parameter.</typeparam>
        /// <typeparam name="Y">The 5th bind parameter.</typeparam>
        /// <typeparam name="Z">The 6th bind parameter.</typeparam>
        public WhereClause<T1,T2,T3,T4> Where<U,V,W,X,Y,Z>(Expression<Func<T1,T2,T3,T4,U,V,W,X,Y,Z,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        /// <typeparam name="X">The 4th bind parameter.</typeparam>
        /// <typeparam name="Y">The 5th bind parameter.</typeparam>
        public WhereClause<T1,T2,T3,T4> Where<U,V,W,X,Y>(Expression<Func<T1,T2,T3,T4,U,V,W,X,Y,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        /// <typeparam name="X">The 4th bind parameter.</typeparam>
        public WhereClause<T1,T2,T3,T4> Where<U,V,W,X>(Expression<Func<T1,T2,T3,T4,U,V,W,X,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Where the specified predExpr with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        /// <typeparam name="W">The 3rd bind parameter.</typeparam>
        public WhereClause<T1,T2,T3,T4> Where<U,V,W>(Expression<Func<T1,T2,T3,T4,U,V,W,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where(predExpr);
        }

        /// <summary>
        /// Filters the selected rows based on a predicate with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        /// <typeparam name="V">The 2nd bind parameter.</typeparam>
        public WhereClause<T1,T2,T3,T4> Where<U,V>(Expression<Func<T1,T2,T3,T4,U,V,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Filters the selected rows based on a predicate with named bind parameters.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        /// <typeparam name="U">The 1st bind parameter.</typeparam>
        public WhereClause<T1,T2,T3,T4> Where<U>(Expression<Func<T1,T2,T3,T4,U,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        /// <summary>
        /// Filters the selected rows based on a predicate.
        /// </summary>
        /// <param name="predExpr">Pred expr.</param>
        public WhereClause<T1,T2,T3,T4> Where(Expression<Func<T1,T2,T3,T4,bool>> predExpr)
        {
            Contract.Requires(predExpr != null);
            return this.Where((Expression) predExpr);
        }

        private WhereClause<T1,T2,T3,T4> Where(Expression expr)
        {
            var where = expr.CompileWhereClause();
            return new WhereClause<T1,T2,T3,T4>(this, where);
        }

        private WhereClause<T1,T2,T3,T4> Where() =>
            this.Where(Expression.Empty());

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2,T3,T4> OrderBy<TValue>(Expression<Func<T1,T2,T3,T4,TValue>> orderExpr) =>
            this.Where().OrderBy(orderExpr);

        /// <summary>
        /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
        /// </summary>
        /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
        /// <param name="orderExpr">A function to extract a key from each element.</param>
        /// <typeparam name="TValue">The type of the key</typeparam>
        public OrderByClause<T1,T2,T3,T4> OrderByDescending<TValue>(Expression<Func<T1,T2,T3,T4,TValue>> orderExpr) =>
            this.Where().OrderByDescending(orderExpr);

        /// <summary>
        /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public LimitClause<T1,T2,T3,T4> Take(int n) =>
            this.Where().Take(n);

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3,T4> Skip(int n) =>
            this.Where().Skip(n);

        /// <summary>
        /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <returns>The <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        /// <param name="index">The index of the element to retrieve.</param>
        /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
        public OffsetClause<T1,T2,T3,T4> ElementAt(int index) =>
            this.Where().ElementAt(index);
    }
}


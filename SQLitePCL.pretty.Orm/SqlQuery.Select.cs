using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Diagnostics.Contracts;

namespace SQLitePCL.pretty.Orm
{
    public static partial class SqlQuery
    {
        public sealed class SelectClause<T> : ISqlQuery
        {
            // FIXME: Long term, prefer some sort of expression syntax
            private readonly IReadOnlyList<String> select;
            private readonly FromClause<T> from;

            internal SelectClause(FromClause<T> from, IReadOnlyList<String> select)
            {
                this.from = from;
                this.select = select;
            }

            public WhereClause<T> Where<U,V,W,X,Y,Z>(Expression<Func<T,U,V,W,X,Y,Z,bool>> predExpr)
            {
                Contract.Requires(predExpr != null);
                return this.Where((LambdaExpression) predExpr);
            }

            public WhereClause<T> Where<U,V,W,X,Y>(Expression<Func<T,U,V,W,X,Y,bool>> predExpr)
            {
                Contract.Requires(predExpr != null);
                return this.Where((LambdaExpression) predExpr);
            }

            public WhereClause<T> Where<U,V,W,X>(Expression<Func<T,U,V,W,X,bool>> predExpr)
            {
                Contract.Requires(predExpr != null);
                return this.Where((LambdaExpression) predExpr);
            }

            public WhereClause<T> Where<U,V,W>(Expression<Func<T,U,V,W,bool>> predExpr)
            {
                Contract.Requires(predExpr != null);
                return this.Where((LambdaExpression) predExpr);
            }

            public WhereClause<T> Where<U,V>(Expression<Func<T,U,V,bool>> predExpr)
            {
                Contract.Requires(predExpr != null);
                return this.Where((LambdaExpression) predExpr);
            }

            public WhereClause<T> Where<U>(Expression<Func<T,U,bool>> predExpr)
            {
                Contract.Requires(predExpr != null);
                return this.Where((LambdaExpression) predExpr);
            }

            public WhereClause<T> Where(Expression<Func<T, bool>> predExpr)
            {
                Contract.Requires(predExpr != null);
                return this.Where((LambdaExpression) predExpr);
            }

            private WhereClause<T> Where(LambdaExpression lambda)
            {
                var pred = lambda.Body;
                return new WhereClause<T>(this, pred);
            }

            private WhereClause<T> Where()
            {
                return new WhereClause<T>(this, null);
            }

            /// <summary>
            /// Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.
            /// </summary>
            /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
            /// <param name="orderExpr">A function to extract a key from each element.</param>
            /// <typeparam name="TValue">The type of the key</typeparam>
            public OrderByClause<T> OrderBy<TValue>(Expression<Func<T, TValue>> orderExpr)
            {
                return this.Where().OrderBy(orderExpr);
            }

            /// <summary>
            /// Performs a subsequent ordering of the elements in a sequence in descending order according to a key.
            /// </summary>
            /// <returns>An <seealso cref="OrderByClause&lt;T&gt;"/>.</returns>
            /// <param name="orderExpr">A function to extract a key from each element.</param>
            /// <typeparam name="TValue">The type of the key</typeparam>
            public OrderByClause<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> orderExpr)
            {
                return this.Where().OrderByDescending(orderExpr);
            }

            /// <summary>
            /// Returns a <see cref="LimitClause&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
            /// </summary>
            /// <param name="n">The number of elements to return.</param>
            /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
            public LimitClause<T> Take(int n)
            {
                return this.Where().Take(n);
            }

            /// <summary>
            /// Returns a <see cref="LimitClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
            /// </summary>
            /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
            /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
            public LimitClause<T> Skip(int n)
            {
                return this.Where().Skip(n);
            }

            /// <summary>
            /// Returns a <see cref="LimitClause&lt;T&gt;"/> that returns the element at a specified index in the result set.
            /// </summary>
            /// <returns>The <see cref="LimitClause&lt;T&gt;"/>.</returns>
            /// <param name="index">Index.</param>
            /// <returns>A new <see cref="LimitClause&lt;T&gt;"/>.</returns>
            public LimitClause<T> ElementAt(int index)
            {
                return this.Where().ElementAt(index);
            }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents the current <see cref="SelectClause&lt;T&gt;"/>.
            /// </summary>
            /// <returns>A <see cref="System.String"/> that represents the current <see cref="SelectClause&lt;T&gt;"/>.</returns>
            public override string ToString()
            {
                return     
                    "SELECT " + string.Join(", ", select) + 
                    "\r\n" + from.ToString();
            }
        }
    }
}


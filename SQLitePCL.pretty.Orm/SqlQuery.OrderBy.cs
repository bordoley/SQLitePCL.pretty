using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;

using SQLitePCL.pretty.Orm.Attributes;
using System.Diagnostics.Contracts;
using System.Linq;

namespace SQLitePCL.pretty.Orm
{
    public static partial class SqlQuery
    {
        /// <summary>
        /// The ORDER BY clause of a SQL query.
        /// </summary>
        public sealed class OrderByClause<T> : ISqlQuery
        {
            private readonly WhereClause<T> whereClause;
            private readonly IReadOnlyList<Tuple<string, bool>> ordering;

            internal OrderByClause(WhereClause<T> whereClause, IReadOnlyList<Tuple<string, bool>> ordering)
            {
                this.whereClause = whereClause;
                this.ordering = ordering;
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
                return new LimitClause<T>(this, n);
            }

            /// <summary>
            /// Returns a <see cref="OffsetClause&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
            /// </summary>
            /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
            /// <returns>A new <see cref="OffsetClause&lt;T&gt;"/>.</returns>
            public OffsetClause<T> Skip(int n)
            {
                //If the LIMIT expression evaluates to a negative value, then there is no upper bound on the number of rows returned
                return new LimitClause<T>(this, -1).Skip(n);
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

            private OrderByClause<T> AddOrderBy<TValue>(Expression<Func<T, TValue>> orderExpr, bool asc)
            {  
                var orderBy = new List<Tuple<string, bool>>(ordering);
                orderBy.Add(orderExpr.CompileOrderByExpression(asc));
                return new OrderByClause<T>(whereClause, orderBy);
            }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents the current <see cref="LimitClause&lt;T&gt;"/>.
            /// </summary>
            /// <returns>A <see cref="System.String"/> that represents the current <see cref="LimitClause&lt;T&gt;"/>.</returns>
            public override string ToString()
            {
                return 
                    whereClause.ToString() +
                    (ordering.Count > 0 ? "\r\nORDER BY " +  string.Join(", ", ordering.Select(o => "\"" + o.Item1 + "\"" + (o.Item2 ? "" : " DESC"))) : "");
            }
        }

        private static Tuple<string, bool> CompileOrderByExpression<T, TValue>(this Expression<Func<T, TValue>> orderExpr, bool asc)
        {
            var lambda = orderExpr;

            MemberExpression mem = null;

            var unary = lambda.Body as UnaryExpression;
            if (unary != null && unary.NodeType == ExpressionType.Convert)
            {

                mem = unary.Operand as MemberExpression;
            }
            else
            {
                mem = lambda.Body as MemberExpression;
            }

            if (mem != null && (mem.Expression.NodeType == ExpressionType.Parameter))
            {
                return Tuple.Create(((PropertyInfo) mem.Member).GetColumnName(), asc);
            }

            throw new NotSupportedException("Order By does not support: " + orderExpr);
        }
    }
}


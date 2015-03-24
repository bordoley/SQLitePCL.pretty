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
        public sealed class OrderByClause<T> : ISqlQuery
        {
            private readonly WhereClause<T> whereClause;
            private readonly IReadOnlyList<Tuple<string, bool>> ordering;

            internal OrderByClause(WhereClause<T> whereClause, IReadOnlyList<Tuple<string, bool>> ordering)
            {
                this.whereClause = whereClause;
                this.ordering = ordering;
            }

            public OrderByClause<T> ThenBy<TValue>(Expression<Func<T, TValue>> orderExpr)
            {
                Contract.Requires(orderExpr != null);
                return AddOrderBy(orderExpr, true);
            }

            public OrderByClause<T> ThenByDescending<TValue>(Expression<Func<T, TValue>> orderExpr)
            {
                Contract.Requires(orderExpr != null);
                return AddOrderBy(orderExpr, false);
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
            /// </summary>
            /// <param name="n">The number of elements to return.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitClause<T> Take(int n)
            {
                Contract.Requires(n >= 0);
                return new LimitClause<T>(this, n, null);
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
            /// </summary>
            /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitClause<T> Skip(int n)
            {
                Contract.Requires(n >= 0);
                return new LimitClause<T>(this, null, n);
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that returns the element at a specified index in the result set.
            /// </summary>
            /// <returns>The <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            /// <param name="index">Index.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitClause<T> ElementAt(int index)
            {
                Contract.Requires(index >= 0);
                return Skip(index).Take(1);
            }

            private OrderByClause<T> AddOrderBy<TValue>(Expression<Func<T, TValue>> orderExpr, bool asc)
            {  
                var orderBy = new List<Tuple<string, bool>>(ordering);
                orderBy.Add(orderExpr.CompileOrderByExpression(asc));
                return new OrderByClause<T>(whereClause, orderBy);
            }

            public override string ToString()
            {
                return 
                    whereClause.ToString() +
                    (ordering.Count > 0 ? "\r\nORDER BY " +  string.Join(", ", ordering.Select(o => "\"" + o.Item1 + "\"" + (o.Item2 ? "" : " DESC"))) : "");
            }
        }
    }
}


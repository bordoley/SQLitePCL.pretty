using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;

using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.Orm
{
    public static partial class QueryBuilder
    {
        public sealed class OrderByQuery<T>
        {
            private readonly string table;
            private readonly string selection;
            private readonly Expression where;
            private readonly IReadOnlyList<Tuple<string, bool>> ordering;

            internal OrderByQuery(string table, string selection, Expression where, IReadOnlyList<Tuple<string, bool>> ordering)
            {
                this.table = table;
                this.selection = selection;
                this.where = where;
                this.ordering = ordering;
            }

            public OrderByQuery<T> ThenBy<TValue>(Expression<Func<T, TValue>> orderExpr)
            {
                return AddOrderBy(orderExpr, true);
            }

            public OrderByQuery<T> ThenByDescending<TValue>(Expression<Func<T, TValue>> orderExpr)
            {
                return AddOrderBy(orderExpr, false);
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
            /// </summary>
            /// <param name="n">The number of elements to return.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitQuery Take(int n)
            {
                return new LimitQuery(table, selection, where, this.ordering, n, null);
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
            /// </summary>
            /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitQuery Skip(int n)
            {
                return new LimitQuery(table, selection, where, this.ordering, null, n);
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that returns the element at a specified index in the result set.
            /// </summary>
            /// <returns>The <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            /// <param name="index">Index.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public string ElementAt(int index)
            {
                return Skip(index).Take(1).ToString();
            }

            private OrderByQuery<T> AddOrderBy<TValue>(Expression<Func<T, TValue>> orderExpr, bool asc)
            {  
                var orderBy = new List<Tuple<string, bool>>(ordering);
                orderBy.Add(orderExpr.CompileOrderByExpression(asc));
                return new OrderByQuery<T>(table, selection, where, orderBy);
            }

            public override string ToString()
            {
                return QueryBuilder.ToString(selection, table, where, ordering, null, null); 
            }
        }
    }
}


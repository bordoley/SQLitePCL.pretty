using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Generic;

namespace SQLitePCL.pretty.Orm
{
    public static partial class QueryBuilder
    {
        public sealed class SelectQuery<T>
        {
            private const string selection = "*";
            private readonly string table;
            private readonly Expression where;

            internal SelectQuery(string table, Expression where)
            {
                this.table = table;
                this.where = where;
            }

            public OrderByQuery<T> OrderBy<TValue>(Expression<Func<T, TValue>> orderExpr)
            {
                return CreateOrderBy(orderExpr, true);
            }

            public OrderByQuery<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> orderExpr)
            {
                return CreateOrderBy(orderExpr, false);
            }

            private OrderByQuery<T> CreateOrderBy<TValue>(Expression<Func<T, TValue>> orderExpr, bool asc)
            {  
                var orderBy = new List<Tuple<string, bool>>();
                orderBy.Add(orderExpr.CompileOrderByExpression(asc));
                return new OrderByQuery<T>(table, selection, where, orderBy);
            }

            public SelectQuery<T> Where<U,V,W,X,Y,Z>(Expression<Func<T,U,V,W,X,Y,Z,bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            public SelectQuery<T> Where<U,V,W,X,Y>(Expression<Func<T,U,V,W,X,Y,bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            public SelectQuery<T> Where<U,V,W,X>(Expression<Func<T,U,V,W,X,bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            public SelectQuery<T> Where<U,V,W>(Expression<Func<T,U,V,W,bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            public SelectQuery<T> Where<U,V>(Expression<Func<T,U,V,bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            public SelectQuery<T> Where<U>(Expression<Func<T,U,bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            public SelectQuery<T> Where(Expression<Func<T, bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            private SelectQuery<T> Where(LambdaExpression lambda)
            {
                var pred = lambda.Body;
                var where = this.where == null ? pred : Expression.AndAlso(this.where, pred);
                return new SelectQuery<T>(table, where);
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
            /// </summary>
            /// <param name="n">The number of elements to return.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitQuery Take(int n)
            {
                return new LimitQuery(table, selection, where, new List<Tuple<string, bool>>(), n, null);
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
            /// </summary>
            /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitQuery Skip(int n)
            {
                return new LimitQuery(table, selection, where, new List<Tuple<string, bool>>(), null, n);
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

            public override string ToString()
            {
                return QueryBuilder.ToString(selection, table, where, Enumerable.Empty<Tuple<string, bool>>(), null, null); 
            }
        }
    }
}


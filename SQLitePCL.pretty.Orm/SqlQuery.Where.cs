using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Generic;

namespace SQLitePCL.pretty.Orm
{
    public static partial class SqlQuery
    {
        public sealed class WhereClause<T> : ISqlQuery
        {
            private readonly string table;
            private readonly string selection;
            private readonly Expression where;

            internal WhereClause(string table, string selection, Expression where)
            {
                this.table = table;
                this.selection = selection;
                this.where = where;
            }

            public OrderByClause<T> OrderBy<TValue>(Expression<Func<T, TValue>> orderExpr)
            {
                Contract.Requires(orderExpr != null);
                return CreateOrderBy(orderExpr, true);
            }

            public OrderByClause<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> orderExpr)
            {
                Contract.Requires(orderExpr != null);
                return CreateOrderBy(orderExpr, false);
            }

            private OrderByClause<T> CreateOrderBy<TValue>(Expression<Func<T, TValue>> orderExpr, bool asc)
            {  
                Contract.Requires(orderExpr != null);

                var orderBy = new List<Tuple<string, bool>>();
                orderBy.Add(orderExpr.CompileOrderByExpression(asc));
                return new OrderByClause<T>(table, selection, where, orderBy);
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
                var where = this.where == null ? pred : Expression.AndAlso(this.where, pred);
                return new WhereClause<T>(table, selection, where);
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
            /// </summary>
            /// <param name="n">The number of elements to return.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitClause<T> Take(int n)
            {
                Contract.Requires(n >= 0);
                return new LimitClause<T>(table, selection, where, new List<Tuple<string, bool>>(), n, null);
            }

            /// <summary>
            /// Returns a <see cref="TableQuery&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
            /// </summary>
            /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
            /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
            public LimitClause<T> Skip(int n)
            {
                Contract.Requires(n >= 0);
                return new LimitClause<T>(table, selection, where, new List<Tuple<string, bool>>(), null, n);
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

            public override string ToString()
            {
                return SqlQuery.ToString(selection, table, where, Enumerable.Empty<Tuple<string, bool>>(), null, null); 
            }

            public string ToSql()
            {
                return this.ToString();
            }
        }
    }
}


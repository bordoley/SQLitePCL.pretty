using System;
using System.Linq.Expressions;
using System.Linq;

namespace SQLitePCL.pretty.Orm
{
    public static partial class QueryBuilder
    {
        public sealed class CountQuery<T>
        {
            private readonly string table;
            private readonly Expression where;

            internal CountQuery(string table, Expression where)
            {
                this.table = table;
                this.where = where;
            }

            public CountQuery<T> Where<U,V,W,X,Y,Z>(Expression<Func<U,V,W,X,Y,Z,bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            public CountQuery<T> Where<U,V,W,X,Y>(Expression<Func<U,V,W,X,Y,bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            public CountQuery<T> Where<U,V,W,X>(Expression<Func<U,V,W,X,bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            public CountQuery<T> Where<U,V,W>(Expression<Func<U,V,W,bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            public CountQuery<T> Where<U,V>(Expression<Func<U,V,bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            public CountQuery<T> Where<U>(Expression<Func<U,bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            public CountQuery<T> Where(Expression<Func<T, bool>> predExpr)
            {
                return this.Where((LambdaExpression) predExpr);
            }

            private CountQuery<T> Where(LambdaExpression lambda)
            {
                var pred = lambda.Body;
                var where = this.where == null ? pred : Expression.AndAlso(this.where, pred);
                return new CountQuery<T>(table, where);
            }

            public override string ToString()
            {
               return QueryBuilder.ToString("COUNT(*)", table, where, Enumerable.Empty<Tuple<string, bool>>(), null, null); 
            }
        }
    }
}


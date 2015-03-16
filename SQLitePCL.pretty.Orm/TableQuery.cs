//
// Copyright (c) 2009-2015 Krueger Systems, Inc.
// Copyright (c) 2015 David Bordoley
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.Orm
{

    /// <summary>
    /// An immutable builder that allows the use of LINQ like syntax to build a SQL query that can subsequently be executed against a SQLite database.
    /// </summary>
    public sealed class TableQuery<T>
    {
        private readonly string _selection;
        private readonly ITableMapping<T> _mapping;
        private readonly Expression _where;
        private readonly IReadOnlyList<Ordering> _orderBy;
        private readonly Nullable<int> _limit;
        private readonly Nullable<int> _offset;

        internal TableQuery(ITableMapping<T> mapping, string selection, Expression where, IReadOnlyList<Ordering> orderBy, Nullable<int> limit, Nullable<int> offset)
        {
            this._selection = selection;
            this._mapping = mapping;
            this._where = where;
            this._orderBy = orderBy;
            this._limit = limit;
            this._offset = offset;
        }

        /// <summary>
        /// The underlying <see cref="ITableMapping&lt;T&gt;"/> used to generate the query.
        /// </summary>
        public ITableMapping<T> Mapping { get { return _mapping; } }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
        public override string ToString()
        {
            var cmdText = "SELECT " + _selection + " FROM \"" + _mapping.TableName + "\"";

            if (this._where != null)
            {
                var w = CompileExpr(_where);
                cmdText += " WHERE " + w;
            }

            if (_orderBy.Count > 0)
            {
                var t = string.Join(", ", _orderBy.Select(o => "\"" + o.ColumnName + "\"" + (o.Ascending ? "" : " DESC")).ToArray());
                cmdText += " ORDER BY " + t;
            }

            if (_limit.HasValue)
            {
                cmdText += " LIMIT " + _limit.Value;
            }

            if (_offset.HasValue)
            {
                if (!_limit.HasValue)
                {
                    cmdText += " LIMIT -1 ";
                }
                cmdText += " OFFSET " + _offset.Value;
            }

            return cmdText;
        }

        /// <summary>
        /// Converts the query to a count query.
        /// </summary>
        /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
        public TableQuery<T> ToCountQuery()
        {
            return new TableQuery<T>(_mapping, "count(*)", _where, _orderBy, _limit, _offset);
        }

        /// <summary>
        /// Returns a <see cref="TableQuery&lt;T&gt;"/> that filters the result set based on a predicate.
        /// </summary>
        /// <param name="predExpr">A function to test each element for a condition..</param>
        /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
        public TableQuery<T> Where(Expression<Func<T, bool>> predExpr)
        {
            if (predExpr.NodeType == ExpressionType.Lambda)
            {
                var lambda = (LambdaExpression) predExpr;
                var pred = lambda.Body;

                if (_limit != null || _offset != null)
                {
                    // FIXME: Why?
                    throw new NotSupportedException("Cannot call where after a skip or a take");
                }

                var where = _where == null ? pred : Expression.AndAlso(_where, pred);
                return new TableQuery<T>(_mapping, _selection, where, _orderBy, _limit, _offset);
            }

            throw new NotSupportedException("Must be a predicate");
        }

        /// <summary>
        /// Returns a <see cref="TableQuery&lt;T&gt;"/> that limits the result set to a specified number of contiguous elements.
        /// </summary>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
        public TableQuery<T> Take(int n)
        {
            // FIXME: Implemented this different than sqlite-net
            return new TableQuery<T>(_mapping, _selection, _where, _orderBy, n, _offset);
        }

        /// <summary>
        /// Returns a <see cref="TableQuery&lt;T&gt;"/> that skips a specified number of elements in the result set and then returns the remaining elements.
        /// </summary>
        /// <param name="n">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
        public TableQuery<T> Skip(int n)
        {
            return new TableQuery<T>(_mapping, _selection, _where, _orderBy, _limit, n);
        }

        /// <summary>
        /// Returns a <see cref="TableQuery&lt;T&gt;"/> that returns the element at a specified index in the result set.
        /// </summary>
        /// <returns>The <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
        /// <param name="index">Index.</param>
        /// <returns>A new <see cref="SQLitePCL.pretty.Orm.TableQuery&lt;T&gt;"/>.</returns>
        public TableQuery<T> ElementAt(int index)
        {
            return Skip(index).Take(1);
        }

        private TableQuery<T> AddOrderBy<TValue>(Expression<Func<T, TValue>> orderExpr, bool asc)
        {
            if (orderExpr.NodeType == ExpressionType.Lambda)
            {
                var lambda = (LambdaExpression) orderExpr;

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
                    var orderBy = new List<Ordering>(_orderBy);
                    orderBy.Add(
                        new Ordering (
                            ((PropertyInfo) mem.Member).GetColumnName(),
                            asc));

                    return new TableQuery<T>(_mapping, _selection, _where, orderBy, _limit, _offset);
                }

                throw new NotSupportedException("Order By does not support: " + orderExpr);
            }

            throw new NotSupportedException("Must be a predicate");
        }

        public TableQuery<T> OrderBy<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            // FIXME: Throw an exception if order by is not empty?
            return AddOrderBy(orderExpr, true);
        }

        public TableQuery<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            // FIXME: Throw an exception if order by is not empty?
            return AddOrderBy(orderExpr, false);
        }

        public TableQuery<T> ThenBy<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            // FIXME: Throw an exception if order by is empty?
            return AddOrderBy(orderExpr, true);
        }

        public TableQuery<T> ThenByDescending<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            // FIXME: Throw an exception if order by is empty?
            return AddOrderBy(orderExpr, false);
        }

        internal static string CompileExpr(Expression expr)
        {
            return CompileExpr(expr, new List<object>()).Item1;
        }

        private static Tuple<String,object> CompileExpr(Expression expr, List<object> queryArgs)
        {
            if (expr is BinaryExpression)
            {
                var bin = (BinaryExpression)expr;
                
                var leftr = CompileExpr(bin.Left, queryArgs);
                var rightr = CompileExpr(bin.Right, queryArgs);

                //If either side is a parameter and is null,then handle the other side specially (for "is null"/"is not null")
                string text;

                if (leftr.Item1 == "?" && leftr.Item2 == null)
                {
                    text = CompileNullBinaryExpression(bin, rightr.Item1);
                }
                else if (rightr.Item1 == "?" && rightr.Item2 == null)
                {
                    text = CompileNullBinaryExpression(bin, leftr.Item1);
                }
                else
                {
                    text = "(" + leftr.Item1 + " " + GetSqlName(bin) + " " + rightr.Item1 + ")";
                }
                return Tuple.Create<String, object>(text, null);
            }

            else if (expr.NodeType == ExpressionType.Not)
            {
                var operandExpr = ((UnaryExpression)expr).Operand;
                var opr = CompileExpr(operandExpr, queryArgs);
                object val = opr.Item2;

                if (val is bool)
                {
                    val = !((bool)val);
                }

                return Tuple.Create("NOT(" + opr.Item1 + ")", val);

            } 

            else if (expr.NodeType == ExpressionType.Call) 
            {  
                var call = (MethodCallExpression) expr;
                var args = new Tuple<String,object>[call.Arguments.Count];

                var obj = call.Object != null ? CompileExpr (call.Object, queryArgs) : null;
                
                for (var i = 0; i < args.Length; i++) 
                {
                    args [i] = CompileExpr (call.Arguments[i], queryArgs);
                }
                
                var sqlCall = "";
                
                if (call.Method.Name == "Like" && args.Length == 2) 
                {
                    sqlCall = "(" + args[0].Item1 + " LIKE " + args[1].Item1 + ")";
                }

                else if (call.Method.Name == "Contains" && args.Length == 2) 
                {
                    sqlCall = "(" + args[1].Item1 + " IN " + args[0].Item1 + ")";
                }

                else if (call.Method.Name == "Contains" && args.Length == 1)
                 {
                    if (call.Object != null && call.Object.Type == typeof(string))
                    {
                        sqlCall = "(" + obj.Item1 + " LIKE ('%' || " + args [0].Item1 + " || '%'))";
                    }
                    else 
                    {
                        sqlCall = "(" + args[0].Item1 + " IN " + obj.Item1 + ")";
                    }
                }

                else if (call.Method.Name == "StartsWith" && args.Length == 1) 
                {
                    sqlCall = "(" + obj.Item1 + " LIKE (" + args [0].Item1 + " || '%'))";
                }

                else if (call.Method.Name == "EndsWith" && args.Length == 1) 
                {
                    sqlCall = "(" + obj.Item1 + " LIKE ('%' || " + args [0].Item1 + "))";
                }

                else if (call.Method.Name == "Equals" && args.Length == 1) 
                {
                    sqlCall = "(" + obj.Item1 + " = (" + args[0].Item1 + "))";
                } 

                else if (call.Method.Name == "ToLower") 
                {
                    sqlCall = "(LOWER(" + obj.Item1 + "))"; 
                } 

                else if (call.Method.Name == "ToUpper") 
                {
                    sqlCall = "(UPPER(" + obj.Item1 + "))"; 
                } 

                else 
                {
                    sqlCall = call.Method.Name.ToLower () + "(" + string.Join (",", args.Select (a => a.Item1).ToArray ()) + ")";
                }

                return Tuple.Create<String, object>(sqlCall, null);
                
            } 

            else if (expr.NodeType == ExpressionType.Constant) 
            {
                var c = (ConstantExpression) expr;
                queryArgs.Add (c.Value);

                return Tuple.Create("?", c.Value);
            } 

            else if (expr.NodeType == ExpressionType.Convert) 
            {
                var u = (UnaryExpression) expr;
                var ty = u.Type;
                var valr = CompileExpr (u.Operand, queryArgs);

                return Tuple.Create(valr.Item1, valr.Item2.ConvertTo (ty));
            } 

            else if (expr.NodeType == ExpressionType.MemberAccess) 
            {
                var mem = (MemberExpression)expr;
                
                if (mem.Expression!=null && mem.Expression.NodeType == ExpressionType.Parameter) 
                {
                    //
                    // This is a column of our table, output just the column name
                    // Need to translate it if that column name is mapped
                    //
                    var columnName = ((PropertyInfo) mem.Member).GetColumnName();
                    return Tuple.Create<String, object>( "\"" + columnName + "\"", null);
                } 

                else 
                {
                    object obj = null;

                    if (mem.Expression != null) 
                    {
                        var r = CompileExpr (mem.Expression, queryArgs);

                        if (r.Item2 == null) { throw new NotSupportedException ("Member access failed to compile expression"); }

                        if (r.Item1 == "?") { queryArgs.RemoveAt (queryArgs.Count - 1); }
                        obj = r.Item2;
                    }
                    
                    //
                    // Get the member value
                    //
                    object val = null;

                    if (mem.Member is PropertyInfo)
                    {
                        var m = (PropertyInfo)mem.Member;
                        val = m.GetValue (obj, null);
                    } 

                    else if (mem.Member is FieldInfo) 
                    {
                        var m = (FieldInfo)mem.Member;
                        val = m.GetValue (obj);
                    } 

                    else { throw new NotSupportedException ("MemberExpr: " + mem.Member.DeclaringType); }
                    
                    //
                    // Work special magic for enumerables
                    //
                    if (val != null && val is IEnumerable && !(val is string) && !(val is IEnumerable<byte>)) 
                    {
                        var sb = new StringBuilder("(");
                        var head = "";

                        foreach (var a in (IEnumerable) val) 
                        {
                            queryArgs.Add(a);
                            sb.Append(head);
                            sb.Append("?");
                            head = ",";
                        }

                        sb.Append(")");
                        return Tuple.Create(sb.ToString(), val);
                    }

                    else 
                    {
                        queryArgs.Add (val);
                        return Tuple.Create("?", val);
                    }
                }
            }

            throw new NotSupportedException ("Cannot compile: " + expr.NodeType.ToString ());
        }

        private static string CompileNullBinaryExpression(BinaryExpression expression, string parameter)
        {
            if (expression.NodeType == ExpressionType.Equal)         { return "(" + parameter + " is ?)"; }
            else if (expression.NodeType == ExpressionType.NotEqual) { return "(" + parameter + " is not ?)"; }
            else { throw new NotSupportedException("Cannot compile Null-BinaryExpression with type " + expression.NodeType.ToString()); }
        }



        private static string GetSqlName (Expression expr)
        {
            var n = expr.NodeType;

            if (n == ExpressionType.GreaterThan)             { return ">"; }
            else if (n == ExpressionType.GreaterThanOrEqual) { return ">="; } 
            else if (n == ExpressionType.LessThan)           { return "<"; } 
            else if (n == ExpressionType.LessThanOrEqual)    { return "<="; } 
            else if (n == ExpressionType.And)                { return "&"; } 
            else if (n == ExpressionType.AndAlso)            { return "and"; } 
            else if (n == ExpressionType.Or)                 { return "|"; } 
            else if (n == ExpressionType.OrElse)             { return "or"; } 
            else if (n == ExpressionType.Equal)              { return "="; } 
            else if (n == ExpressionType.NotEqual)           { return "!="; } 
            else { throw new NotSupportedException ("Cannot get SQL for: " + n); }
        }
    }

    /// <summary>
    /// Extension methods for querying a database using instances of <see cref="TableQuery&lt;T&gt;"/>
    /// </summary>
    public static class TableQuery
    {
        public static ITableMappedStatement<T> PrepareQuery<T>(this IDatabaseConnection This, TableQuery<T> query)
        {
            return new TableMappedStatement<T>(This.PrepareStatement(query.ToString()), query.Mapping);
        }

        public static IEnumerable<T> Query<T>(this IDatabaseConnection This, TableQuery<T> query)
        {
            return This.Query(query.ToString()).Select(query.Mapping.ToObject);
        }

        public static IEnumerable<T> Query<T>(this IDatabaseConnection This, TableQuery<T> query, params object[] values)
        {
            return This.Query(query.ToString(), values).Select(query.Mapping.ToObject);
        }

        public static IObservable<T> Query<T>(this IAsyncDatabaseConnection This, TableQuery<T> query)
        {
            return This.Query(query.ToString()).Select(query.Mapping.ToObject);
        }

        public static IStatement PrepareCount<T>(this IDatabaseConnection This, TableQuery<T> query)
        {
            return This.PrepareStatement(query.ToCountQuery().ToString());
        }

        public static Task<IAsyncStatement> PrepareCountAsync<T>(this IAsyncDatabaseConnection This, TableQuery<T> query)
        {
            return This.PrepareStatementAsync(query.ToCountQuery().ToString());
        }

        public static int Count<T>(this IDatabaseConnection This, TableQuery<T> query)
        {
            return This.Query(query.ToCountQuery().ToString()).SelectScalarInt().First();
        }

        public static Task<int> CountAsync<T>(this IAsyncDatabaseConnection This, TableQuery<T> query)
        {
            return This.CountAsync(query, CancellationToken.None);
        }

        public static Task<int> CountAsync<T>(this IAsyncDatabaseConnection This, TableQuery<T> query, CancellationToken ct)
        {
            return This.Query(query.ToCountQuery().ToString()).SelectScalarInt().FirstAsync().ToTask(ct);
        }

        public static int Count<T>(this IDatabaseConnection This, TableQuery<T> query, params object[] values)
        {
            return This.Query(query.ToCountQuery().ToString(), values).SelectScalarInt().First();
        }

        public static Task<int> CountAsync<T>(this IAsyncDatabaseConnection This, TableQuery<T> query, params object[] values)
        {
            return This.CountAsync(query, CancellationToken.None, values);
        }

        public static Task<int> CountAsync<T>(this IAsyncDatabaseConnection This, TableQuery<T> query, CancellationToken ct, params object[] values)
        {
            return This.Query(query.ToCountQuery().ToString(), values).SelectScalarInt().FirstAsync().ToTask(ct);
        }
    }    
}

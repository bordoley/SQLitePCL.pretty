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
using System.Reflection;
using System.Text;

namespace SQLitePCL.pretty.Orm
{
    internal sealed class Ordering
    {
        private readonly string _columnName;
        private readonly bool _ascending;

        internal Ordering(string columnName, bool ascending)
        {
            _columnName = columnName;
            _ascending = ascending;
        }

        public string ColumnName { get { return _columnName; } }
        public bool Ascending { get { return _ascending; }  }
    }        

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
            
        internal string Selection { get { return _selection; } }

        internal ITableMapping<T> Mapping { get { return _mapping; } }

        internal Expression Where { get { return _where; } }

        internal IReadOnlyList<Ordering> OrderBy { get {return _orderBy; } }

        internal Nullable<int> Limit { get { return _limit; } }        

        internal Nullable<int> Offset { get { return _offset; } }

        public override string ToString()
        {
            var cmdText = "SELECT " + _selection + " FROM \"" + this.Mapping.TableName + "\"";

            if (this._where != null)
            {
                var w = TableQuery.CompileExpr(this.Where);
                cmdText += " WHERE " + w;
            }

            if (this.OrderBy.Count > 0)
            {
                var t = string.Join(", ", this.OrderBy.Select(o => "\"" + o.ColumnName + "\"" + (o.Ascending ? "" : " DESC")).ToArray());
                cmdText += " ORDER BY " + t;
            }

            if (this.Limit.HasValue)
            {
                cmdText += " LIMIT " + this.Limit.Value;
            }

            if (this.Offset.HasValue)
            {
                if (!this.Limit.HasValue)
                {
                    cmdText += " LIMIT -1 ";
                }
                cmdText += " OFFSET " + this.Offset.Value;
            }

            return cmdText;
        }
    }

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
            return This.PrepareStatement(query.Count());
        }

        public static int Count<T>(this IDatabaseConnection This, TableQuery<T> query)
        {
            return This.Query(query.Count()).SelectScalarInt().First();
        }

        public static int Count<T>(this IDatabaseConnection This, TableQuery<T> query, params object[] values)
        {
            return This.Query(query.Count(), values).SelectScalarInt().First();
        }
                    
        private static string Count<T>(this TableQuery<T> This)
        {
            return (new TableQuery<T>(This.Mapping, "count(*)", This.Where, This.OrderBy, This.Limit, This.Offset)).ToString();
        }

        public static TableQuery<T> Where<T>(this TableQuery<T> This, Expression<Func<T, bool>> predExpr)
        {
            if (predExpr.NodeType == ExpressionType.Lambda)
            {
                var lambda = (LambdaExpression) predExpr;
                var pred = lambda.Body;

                if (This.Limit != null || This.Offset != null)
                {
                    // FIXME: Why?
                    throw new NotSupportedException("Cannot call where after a skip or a take");
                }

                var where = This.Where == null ? pred : Expression.AndAlso(This.Where, pred);
                return new TableQuery<T>(This.Mapping, This.Selection, where, This.OrderBy, This.Limit, This.Offset);
            }

            throw new NotSupportedException("Must be a predicate");
        }

        public static TableQuery<T> Take<T>(this TableQuery<T> This, int n)
        {
            // FIXME: Implemented this different than sqlite-net
            return new TableQuery<T>(This.Mapping, This.Selection, This.Where, This.OrderBy, n, This.Offset);
        }

        public static TableQuery<T> Skip<T>(this TableQuery<T> This, int n)
        {
            return new TableQuery<T>(This.Mapping, This.Selection, This.Where, This.OrderBy, This.Limit, n);
        }

        public static TableQuery<T> ElementAt<T>(this TableQuery<T> This, int index)
        {
            return This.Skip(index).Take(1);
        }

        private static TableQuery<T> AddOrderBy<T,TValue>(this TableQuery<T> This, Expression<Func<T, TValue>> orderExpr, bool asc)
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
                    var orderBy = new List<Ordering>(This.OrderBy);
                    orderBy.Add(
                        new Ordering (
                            TableMapping.PropertyToColumnName((PropertyInfo) mem.Member),
                            asc));

                    return new TableQuery<T>(This.Mapping, This.Selection, This.Where, orderBy, This.Limit, This.Offset);
                }

                throw new NotSupportedException("Order By does not support: " + orderExpr);
            }

            throw new NotSupportedException("Must be a predicate");
        }

        public static TableQuery<T> OrderBy<T,TValue>(this TableQuery<T> This, Expression<Func<T, TValue>> orderExpr)
        {
            // FIXME: Throw an exception if order by is not empty?
            return This.AddOrderBy(orderExpr, true);
        }

        public static TableQuery<T> OrderByDescending<T,TValue>(this TableQuery<T> This, Expression<Func<T, TValue>> orderExpr)
        {
            // FIXME: Throw an exception if order by is not empty?
            return This.AddOrderBy(orderExpr, false);
        }

        public static TableQuery<T> ThenBy<T,TValue>(this TableQuery<T> This, Expression<Func<T, TValue>> orderExpr)
        {
            // FIXME: Throw an exception if order by is empty?
            return This.AddOrderBy(orderExpr, true);
        }

        public static TableQuery<T> ThenByDescending<T,TValue>(this TableQuery<T> This, Expression<Func<T, TValue>> orderExpr)
        {
            // FIXME: Throw an exception if order by is empty?
            return This.AddOrderBy(orderExpr, false);
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

                return Tuple.Create(valr.Item1, valr.Item2 != null ? ConvertTo (valr.Item2, ty) : null);
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
                    var columnName = TableMapping.PropertyToColumnName((PropertyInfo) mem.Member);
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

        private static object ConvertTo (object obj, Type t)
        {
            if (obj == null) { return null; }

            var nut = Nullable.GetUnderlyingType(t) ?? t;
            
            return Convert.ChangeType (obj, nut);
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
}

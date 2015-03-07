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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
            var cmdText = "select " + _selection + " from \"" + this.Mapping.TableName + "\"";

            if (this._where != null)
            {
                var w = TableQuery.CompileExpr(this.Mapping, this.Where);
                cmdText += " where " + w;
            }

            if (this.OrderBy.Count > 0)
            {
                var t = string.Join(", ", this.OrderBy.Select(o => "\"" + o.ColumnName + "\"" + (o.Ascending ? "" : " desc")).ToArray());
                cmdText += " order by " + t;
            }

            if (this.Limit.HasValue)
            {
                cmdText += " limit " + this.Limit.Value;
            }

            if (this.Offset.HasValue)
            {
                if (!this.Limit.HasValue)
                {
                    cmdText += " limit -1 ";
                }
                cmdText += " offset " + this.Offset.Value;
            }

            return cmdText;
        }
    }

    public static class TableQuery
    {
        public static TableQuery<T> Select<T>(this ITableMapping<T> This)
        {
            return new TableQuery<T>(This, "*", null, new List<Ordering>(), null, null);
        }
            
        public static string Count<T>(this TableQuery<T> This)
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

        public static TableQuery<T> First<T>(this TableQuery<T> This)
        {
            return This.Take(1);
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
            // FIXME: Weird whats the difference from ThenBy?
            return This.AddOrderBy(orderExpr, true);
        }

        public static TableQuery<T> OrderByDescending<T,TValue>(this TableQuery<T> This, Expression<Func<T, TValue>> orderExpr)
        {
            return This.AddOrderBy(orderExpr, false);
        }

        public static TableQuery<T> ThenBy<T,TValue>(this TableQuery<T> This, Expression<Func<T, TValue>> orderExpr)
        {
            return This.AddOrderBy(orderExpr, true);
        }

        public static TableQuery<T> ThenByDescending<T,TValue>(this TableQuery<T> This, Expression<Func<T, TValue>> orderExpr)
        {
            return This.AddOrderBy(orderExpr, false);
        }

        // FIXME replace with tuple
        private class CompileResult
        {
            public string CommandText { get; set; }

            public object Value { get; set; }
        }

        internal static string CompileExpr<T>(ITableMapping<T> mapping, Expression expr)
        {
            return CompileExpr(mapping, expr, new List<object>()).CommandText;
        }

        private static CompileResult CompileExpr<T>(ITableMapping<T> Table, Expression expr, List<object> queryArgs)
        {
            if (expr == null) {
                throw new NotSupportedException ("Expression is NULL");
            } else if (expr is BinaryExpression) {
                var bin = (BinaryExpression)expr;
                
                var leftr = CompileExpr (Table, bin.Left, queryArgs);
                var rightr = CompileExpr (Table, bin.Right, queryArgs);

                //If either side is a parameter and is null, then handle the other side specially (for "is null"/"is not null")
                string text;
                if (leftr.CommandText == "?" && leftr.Value == null)
                    text = CompileNullBinaryExpression(bin, rightr);
                else if (rightr.CommandText == "?" && rightr.Value == null)
                    text = CompileNullBinaryExpression(bin, leftr);
                else
                    text = "(" + leftr.CommandText + " " + GetSqlName(bin) + " " + rightr.CommandText + ")";
                return new CompileResult { CommandText = text };
            } else if (expr.NodeType == ExpressionType.Not) {
                var operandExpr = ((UnaryExpression)expr).Operand;
                var opr = CompileExpr(Table, operandExpr, queryArgs);
                object val = opr.Value;
                if (val is bool)
                    val = !((bool) val);
                return new CompileResult
                {
                    CommandText = "NOT(" + opr.CommandText + ")",
                    Value = val
                };
            } else if (expr.NodeType == ExpressionType.Call) {
                
                var call = (MethodCallExpression)expr;
                var args = new CompileResult[call.Arguments.Count];
                var obj = call.Object != null ? CompileExpr (Table, call.Object, queryArgs) : null;
                
                for (var i = 0; i < args.Length; i++) {
                    args [i] = CompileExpr (Table, call.Arguments [i], queryArgs);
                }
                
                var sqlCall = "";
                
                if (call.Method.Name == "Like" && args.Length == 2) {
                    sqlCall = "(" + args [0].CommandText + " like " + args [1].CommandText + ")";
                }
                else if (call.Method.Name == "Contains" && args.Length == 2) {
                    sqlCall = "(" + args [1].CommandText + " in " + args [0].CommandText + ")";
                }
                else if (call.Method.Name == "Contains" && args.Length == 1) {
                    if (call.Object != null && call.Object.Type == typeof(string)) {
                        sqlCall = "(" + obj.CommandText + " like ('%' || " + args [0].CommandText + " || '%'))";
                    }
                    else {
                        sqlCall = "(" + args [0].CommandText + " in " + obj.CommandText + ")";
                    }
                }
                else if (call.Method.Name == "StartsWith" && args.Length == 1) {
                    sqlCall = "(" + obj.CommandText + " like (" + args [0].CommandText + " || '%'))";
                }
                else if (call.Method.Name == "EndsWith" && args.Length == 1) {
                    sqlCall = "(" + obj.CommandText + " like ('%' || " + args [0].CommandText + "))";
                }
                else if (call.Method.Name == "Equals" && args.Length == 1) {
                    sqlCall = "(" + obj.CommandText + " = (" + args[0].CommandText + "))";
                } else if (call.Method.Name == "ToLower") {
                    sqlCall = "(lower(" + obj.CommandText + "))"; 
                } else if (call.Method.Name == "ToUpper") {
                    sqlCall = "(upper(" + obj.CommandText + "))"; 
                } else {
                    sqlCall = call.Method.Name.ToLower () + "(" + string.Join (",", args.Select (a => a.CommandText).ToArray ()) + ")";
                }
                return new CompileResult { CommandText = sqlCall };
                
            } else if (expr.NodeType == ExpressionType.Constant) {
                var c = (ConstantExpression)expr;
                queryArgs.Add (c.Value);
                return new CompileResult {
                    CommandText = "?",
                    Value = c.Value
                };
            } else if (expr.NodeType == ExpressionType.Convert) {
                var u = (UnaryExpression)expr;
                var ty = u.Type;
                var valr = CompileExpr (Table, u.Operand, queryArgs);
                return new CompileResult {
                    CommandText = valr.CommandText,
                    Value = valr.Value != null ? ConvertTo (valr.Value, ty) : null
                };
            } else if (expr.NodeType == ExpressionType.MemberAccess) {
                var mem = (MemberExpression)expr;
                
                if (mem.Expression!=null && mem.Expression.NodeType == ExpressionType.Parameter) {
                    //
                    // This is a column of our table, output just the column name
                    // Need to translate it if that column name is mapped
                    //
                    var columnName = TableMapping.PropertyToColumnName((PropertyInfo) mem.Member);
                    return new CompileResult { CommandText = "\"" + columnName + "\"" };
                } else {
                    object obj = null;
                    if (mem.Expression != null) {
                        var r = CompileExpr (Table, mem.Expression, queryArgs);
                        if (r.Value == null) {
                            throw new NotSupportedException ("Member access failed to compile expression");
                        }
                        if (r.CommandText == "?") {
                            queryArgs.RemoveAt (queryArgs.Count - 1);
                        }
                        obj = r.Value;
                    }
                    
                    //
                    // Get the member value
                    //
                    object val = null;

                    if (mem.Member is PropertyInfo) {
                        var m = (PropertyInfo)mem.Member;
                        val = m.GetValue (obj, null);
                    } else if (mem.Member is FieldInfo) {
                        var m = (FieldInfo)mem.Member;
                        val = m.GetValue (obj);
                    } else {
                        throw new NotSupportedException ("MemberExpr: " + mem.Member.DeclaringType);
                    }
                    
                    //
                    // Work special magic for enumerables
                    //
                    if (val != null && val is System.Collections.IEnumerable && !(val is string) && !(val is System.Collections.Generic.IEnumerable<byte>)) {
                        var sb = new System.Text.StringBuilder();
                        sb.Append("(");
                        var head = "";
                        foreach (var a in (System.Collections.IEnumerable)val) {
                            queryArgs.Add(a);
                            sb.Append(head);
                            sb.Append("?");
                            head = ",";
                        }
                        sb.Append(")");
                        return new CompileResult {
                            CommandText = sb.ToString(),
                            Value = val
                        };
                    }
                    else {
                        queryArgs.Add (val);
                        return new CompileResult {
                            CommandText = "?",
                            Value = val
                        };
                    }
                }
            }
            throw new NotSupportedException ("Cannot compile: " + expr.NodeType.ToString ());
        }

        private static object ConvertTo (object obj, Type t)
        {
            Type nut = Nullable.GetUnderlyingType(t);
            
            if (nut != null) {
                if (obj == null) return null;               
                return Convert.ChangeType (obj, nut);
            } else {
                return Convert.ChangeType (obj, t);
            }
        }
            
        private static string CompileNullBinaryExpression(BinaryExpression expression, CompileResult parameter)
        {
            if (expression.NodeType == ExpressionType.Equal)
                return "(" + parameter.CommandText + " is ?)";
            else if (expression.NodeType == ExpressionType.NotEqual)
                return "(" + parameter.CommandText + " is not ?)";
            else
                throw new NotSupportedException("Cannot compile Null-BinaryExpression with type " + expression.NodeType.ToString());
        }

        private static string GetSqlName (Expression expr)
        {
            var n = expr.NodeType;
            if (n == ExpressionType.GreaterThan)
                return ">"; else if (n == ExpressionType.GreaterThanOrEqual) {
                return ">=";
            } else if (n == ExpressionType.LessThan) {
                return "<";
            } else if (n == ExpressionType.LessThanOrEqual) {
                return "<=";
            } else if (n == ExpressionType.And) {
                return "&";
            } else if (n == ExpressionType.AndAlso) {
                return "and";
            } else if (n == ExpressionType.Or) {
                return "|";
            } else if (n == ExpressionType.OrElse) {
                return "or";
            } else if (n == ExpressionType.Equal) {
                return "=";
            } else if (n == ExpressionType.NotEqual) {
                return "!=";
            } else {
                throw new NotSupportedException ("Cannot get SQL for: " + n);
            }
        }
    }
}

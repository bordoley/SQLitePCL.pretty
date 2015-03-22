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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SQLitePCL.pretty;
using SQLitePCL.pretty.Orm.Attributes;
using System.IO;

namespace SQLitePCL.pretty.Orm
{
    public static partial class QueryBuilder
    {
        public static bool Is<T>(this T This, T other = null)
            where T: class
        {
            return This.Equals(other);
        }

        public static bool Is<T>(this Nullable<T> This, Nullable<T> other = null)
            where T: struct
        {
            return This.Equals(other);
        }

        public static bool IsNot<T>(this T This, T other = null)
            where T: class
        {
            return !This.Equals(other);
        }

        public static bool IsNot<T>(this Nullable<T> This, Nullable<T> other = null)
            where T: struct
        {
            return !This.Equals(other);
        }

        public static SelectQuery<T> Select<T>(this ITableMapping<T> This)
        {
            return new SelectQuery<T>(This.TableName, null);
        }

        //public static CountQuery<long> Count(this ITableMapping This)
        //{
        //    return new CountQuery<long>(This.TableName, null);
        //}

        internal static string ToString(string selection, string table, Expression where, IEnumerable<Tuple<string, bool>> orderBy, int? limit, int? offset)
        {
            var cmdText = 
                "SELECT " + selection + 
                "\nFROM \"" + table + "\"" +
                (where != null ? "\nWHERE " + CompileExpr(where): "") +
                (orderBy.Count() > 0 ? "\nORDERBY " +  string.Join(", ", orderBy.Select(o => "\"" + o.Item1 + "\"" + (o.Item2 ? "" : " DESC"))) : "") +
                (limit.HasValue ? "\nLIMIT " + limit.Value : "") +
                (offset.HasValue ? "\nOFFSET " + offset.Value : "");
                 
            return cmdText;
        }

        private static String CompileExpr(Expression expr)
        {
            if (expr is BinaryExpression)
            {
                var bin = (BinaryExpression) expr;
                
                var leftExpr = CompileExpr(bin.Left);
                var rightExpr = CompileExpr(bin.Right);

                return "(" + leftExpr + " " + GetSqlName(bin) + " " + rightExpr + ")";
            }
            else if (expr is ParameterExpression)
            {
                var param = (ParameterExpression)expr;
                return ":" + param.Name;
            }
            else if (expr is MemberExpression)
            {
                var member = (MemberExpression) expr;

                if (member.Expression != null && member.Expression.NodeType == ExpressionType.Parameter)
                {
                    // This is a column in the table, output the column name
                    var columnName = ((PropertyInfo) member.Member).GetColumnName();
                    return "\"" + columnName + "\"";
                }
                else
                {
                    return member.EvaluateExpression().ConvertToSQLiteValue().ToSqlString();
                }
            }
            else if (expr.NodeType == ExpressionType.Not)
            {
                var operandExpr = ((UnaryExpression) expr).Operand;
                return "NOT(" + operandExpr.EvaluateExpression().ConvertToSQLiteValue().ToSqlString() + ")";
            } 
            else if (expr is ConstantExpression) 
            {
                return expr.EvaluateExpression().ConvertToSQLiteValue().ToSqlString();
            }
            else if (expr is MethodCallExpression)
            {
                var call = (MethodCallExpression) expr;
                var args = new String[call.Arguments.Count];

                var obj = call.Object != null ? CompileExpr (call.Object) : null;
                
                for (var i = 0; i < args.Length; i++) 
                {
                    args [i] = CompileExpr (call.Arguments[i]);
                }
                
                if (call.Method.Name == "Like" && args.Length == 2) 
                {
                    return "(" + args[0] + " LIKE " + args[1] + ")";
                }

                else if (call.Method.Name == "Contains" && args.Length == 2) 
                {
                    return "(" + args[1] + " IN " + args[0] + ")";
                }

                else if (call.Method.Name == "Contains" && args.Length == 1)
                 {
                    if (call.Object != null && call.Object.Type == typeof(string))
                    {
                        return "(" + obj + " LIKE ('%' || " + args[0] + " || '%'))";
                    }
                    else 
                    {
                        return "(" + args[0] + " IN " + obj + ")";
                    }
                }

                else if (call.Method.Name == "StartsWith" && args.Length == 1) 
                {
                    return "(" + obj + " LIKE (" + args[0] + " || '%'))";
                }

                else if (call.Method.Name == "EndsWith" && args.Length == 1) 
                {
                    return "(" + obj + " LIKE ('%' || " + args[0] + "))";
                }

                else if (call.Method.Name == "Equals" && args.Length == 1) 
                {
                    return "(" + obj + " = (" + args[0] + "))";
                }

                else if (call.Method.Name == "Is" && args.Length == 2)
                {
                    return "(" + args[0] + " IS " + args[1] + ")";
                }

                else if (call.Method.Name == "IsNot" && args.Length == 2)
                {
                    return "(" + args[0] + " IS NOT " + args[1] + ")";
                }
            }
            else if (expr.NodeType == ExpressionType.Convert) 
            {
                var u = (UnaryExpression) expr;
                var ty = u.Type;
                var value = EvaluateExpression(u.Operand);

                return value.ConvertTo(ty).ConvertToSQLiteValue().ToSqlString();
            } 

            throw new NotSupportedException("Cannot compile: " + expr.NodeType.ToString());
        }

        private static object EvaluateExpression(this Expression expr)
        {
            if (expr is ConstantExpression)
            {
                var c = (ConstantExpression) expr;
                return c.Value;
            }
            else if (expr is MemberExpression)
            {
                var memberExpr = (MemberExpression) expr;
                var obj = EvaluateExpression(memberExpr.Expression);
               
                if (memberExpr.Member is PropertyInfo)
                {
                    var m = (PropertyInfo) memberExpr.Member;
                    return m.GetValue(obj, null);
                }

                else if (memberExpr.Member is FieldInfo)
                {
                    var m = (FieldInfo) memberExpr.Member;
                    return m.GetValue(obj);
                }
            }

            throw new NotSupportedException("Cannot compile: " + expr.NodeType.ToString());
        }

        private static string ToSqlString(this ISQLiteValue value)
        {
            switch (value.SQLiteType)
            {
                case SQLiteType.Null:  
                    return "NULL";
                case SQLiteType.Text:
                case SQLiteType.Blob:  
                    return "\"" + value.ToString() + "\"";
                default:
                    return value.ToString();
            }
        }

        private static ISQLiteValue ConvertToSQLiteValue(this object This)
        {
            if (This == null) { return SQLiteValue.Null; }
                
            Type t = This.GetType();

            if (typeof(string) == t)                                                          { return ((string) This).ToSQLiteValue(); }
            else if (
                (typeof(Int32) == t)
                || (typeof(Boolean) == t)
                || (typeof(Byte) == t)
                || (typeof(UInt16) == t)
                || (typeof(Int16) == t)
                || (typeof(sbyte) == t)
                || (typeof(Int64) == t)
                || (typeof(UInt32) == t))                                                     { return ((long)(Convert.ChangeType(This, typeof(long)))).ToSQLiteValue(); }
            else if ((typeof(double) == t) || (typeof(float) == t) || (typeof(decimal) == t)) { return ((double)(Convert.ChangeType(This, typeof(double)))).ToSQLiteValue(); }
            else if (typeof(byte[]) == t)                                                     { return ((byte[]) This).ToSQLiteValue(); }
            else if (t.GetTypeInfo().ImplementedInterfaces.Contains(typeof(ISQLiteValue)))    { return (ISQLiteValue) This; }
            else if (This is TimeSpan)                                                        { return ((TimeSpan) This).ToSQLiteValue(); }
            else if (This is DateTime)                                                        { return ((DateTime) This).ToSQLiteValue(); }
            else if (This is DateTimeOffset)                                                  { return ((DateTimeOffset) This).ToSQLiteValue(); }
            else if (This is Guid)                                                            { return ((Guid) This).ToSQLiteValue(); }
            //else if (obj is Stream)                                                         { return ((Stream) obj); }
            else if (This is Uri)                                                             { return ((Uri) This).ToSQLiteValue(); }
            else
            {
                throw new ArgumentException("Invalid type conversion" + t);
            }
        }

        private static string GetSqlName(Expression prop)
        {
            var n = prop.NodeType;

            switch (n)
            {
                case ExpressionType.GreaterThan:        return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.LessThan:           return "<";
                case ExpressionType.LessThanOrEqual:    return "<=";
                case ExpressionType.And:                return "&";
                case ExpressionType.AndAlso:            return "and";
                case ExpressionType.Or:                 return "|";
                case ExpressionType.OrElse:             return "or";
                case ExpressionType.Equal:              return "=";
                case ExpressionType.NotEqual:           return "!=";
                default:
                    throw new NotSupportedException ("Cannot get SQL for: " + n);
            }
        }

        private static Tuple<string, bool> CompileOrderByExpression<T, TValue>(this Expression<Func<T, TValue>> orderExpr, bool asc)
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
                    return Tuple.Create(((PropertyInfo) mem.Member).GetColumnName(), asc);
                }

                throw new NotSupportedException("Order By does not support: " + orderExpr);
            }
    }

/*

        private static Tuple<String,object> CompileExpr(Expression expr, List<object> queryArgs)
        {
            else if (expr.NodeType == ExpressionType.MemberAccess) 
            {
\\


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
        }
    }*/


}

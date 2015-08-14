using System;
using System.Linq.Expressions;
using System.Reflection;
using SQLitePCL.pretty.Orm.Attributes;
using System.Linq;

namespace SQLitePCL.pretty.Orm.Sql
{
    internal static partial class SqlCompiler
    {
        private static string CompileExpr(this Expression This)
        {
            if (This is BinaryExpression)
            {
                var bin = (BinaryExpression)This;
                
                var leftExpr = bin.Left.CompileExpr();
                var rightExpr = bin.Right.CompileExpr();

                if (rightExpr == "NULL" && bin.NodeType == ExpressionType.Equal)
                {
                    if (bin.NodeType == ExpressionType.Equal)
                    {
                        return $"({leftExpr} IS NULL)";
                    }
                    else if (rightExpr == "NULL" && bin.NodeType == ExpressionType.NotEqual)
                    {
                        return $"({leftExpr} IS NOT NULL)";
                    }
                }

                return $"({leftExpr} {GetSqlName(bin)} {rightExpr})";
            }
            else if (This is ParameterExpression)
            {
                var param = (ParameterExpression)This;
                return $":{param.Name}";
            }
            else if (This is MemberExpression)
            {
                var member = (MemberExpression)This;

                if (member.Expression != null && member.Expression.NodeType == ExpressionType.Parameter)
                {
                    // This is a column in the table, output the column name
                    var tableName = TableMapping.Get(member.Expression.Type).TableName;
                    var columnName = ((PropertyInfo) member.Member).GetColumnName();
                    return $"\"{tableName}\".\"{columnName}\"";
                }
                else
                {
                    return member.EvaluateExpression().ConvertToSQLiteValue().ToSqlString();
                }
            }
            else if (This.NodeType == ExpressionType.Not)
            {
                var operandExpr = ((UnaryExpression)This).Operand;
                return $"NOT({operandExpr.CompileExpr()})";
            }
            else if (This is ConstantExpression)
            {
                return This.EvaluateExpression().ConvertToSQLiteValue().ToSqlString();
            }
            else if (This is MethodCallExpression)
            {
                var call = (MethodCallExpression)This;
                var args = new String[call.Arguments.Count];

                var obj = call.Object != null ? call.Object.CompileExpr() : null;
                
                for (var i = 0; i < args.Length; i++)
                {
                    args[i] = call.Arguments[i].CompileExpr();
                }
                
                if (call.Method.Name == "Like" && args.Length == 2)
                {
                    return $"({args[0]} LIKE {args[1]})";
                }
                else if (call.Method.Name == "Contains" && args.Length == 2)
                {
                    return $"({args[1]} IN {args[0]})";
                }
                else if (call.Method.Name == "Contains" && args.Length == 1)
                {
                    if (call.Object != null && call.Object.Type == typeof(string))
                    {
                        return $"({obj} LIKE ('%' || {args[0]} || '%'))";
                    }
                    else
                    {
                        return $"({args[0]} IN {obj})";
                    }
                }
                else if (call.Method.Name == "StartsWith" && args.Length == 1)
                {
                    return $"({obj} LIKE ({args[0]} || '%'))";
                }
                else if (call.Method.Name == "EndsWith" && args.Length == 1)
                {
                    return $"({obj} LIKE ('%' || {args[0]}))";
                }
                else if (call.Method.Name == "Equals" && args.Length == 1)
                {
                    return $"({obj} = ({args[0]}))";
                }
                else if (call.Method.Name == "Is" && args.Length == 2)
                {
                    return $"({args[0]} IS {args[1]})";
                }
                else if (call.Method.Name == "IsNot" && args.Length == 2)
                {
                    return $"({args[0]} IS NOT {args[1]})";
                }
            }
            else if (This.NodeType == ExpressionType.Convert)
            {
                var u = (UnaryExpression)This;

                // Let SQLite handle the casting for us
                var operand = u.Operand;
                return operand.CompileExpr();

                // FIXME: Might still want to support a direct cast here if the
                // operand is a constant value or a function that is evaluated
                /*
                var ty = u.Type;
                var value = EvaluateExpression(u.Operand);

                return value.ConvertTo(ty).ConvertToSQLiteValue().ToSqlString();*/
            }
            else if (This.NodeType == ExpressionType.Default)
            {
                var d = (DefaultExpression)This;
                var ty = d.Type;
                if (ty == typeof(void))
                {
                    return "";
                }
            }
            else if (This.NodeType == ExpressionType.Lambda)
            {
                var expr = (LambdaExpression) This;
                return CompileExpr(expr.Body);
            }

            throw new NotSupportedException($"Cannot compile: {This.NodeType.ToString()}");
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

            throw new NotSupportedException($"Cannot compile: {expr.NodeType.ToString()}");
        }

        private static string ToSqlString(this ISQLiteValue value)
        {
            switch (value.SQLiteType)
            {
                case SQLiteType.Null:  
                    return "NULL";
                case SQLiteType.Text:
                case SQLiteType.Blob:  
                    return $"\"{value.ToString()}\"";
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
                throw new ArgumentException($"Invalid type conversion {t}");
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
                    throw new NotSupportedException ($"Cannot get SQL for: {n}");
            }
        }
    }
}


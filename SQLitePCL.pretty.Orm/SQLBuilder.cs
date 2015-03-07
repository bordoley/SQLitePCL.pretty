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
using System.Reflection;
using System.Text;

namespace SQLitePCL.pretty.Orm
{
    [Flags]
    public enum CreateFlags
    {
        None                = 0x000,
        ImplicitPK          = 0x001,    // create a primary key for field called 'Id' (Orm.ImplicitPkName)
        ImplicitIndex       = 0x002,    // create an index for fields ending in 'Id' (Orm.ImplicitIndexSuffix)
        AllImplicit         = 0x003,    // do both above
        AutoIncPK           = 0x004,    // force PK field to be auto inc
        FullTextSearch3     = 0x100,    // create virtual table using FTS3
        FullTextSearch4     = 0x200     // create virtual table using FTS4
    }

    public static class SQLBuilder
    {
        public const string SelectAllTables = 
            @"SELECT name FROM sqlite_master
              WHERE type='table'
              ORDER BY name;";

        public const string BeginTransaction = "BEGIN TRANSACTION";

        public const string Commit = "COMMIT";

        public const string Rollback = "ROLLBACK";

        public const string DeleteAll = "DELETE FROM ?";

        public const string DropTable = "DROP TABLE If EXISTS ?";

        public const string SavePoint = "SAVEPOINT ?";

        public const string Release = "RELEASE ?";

        public static string DeleteUsingPrimaryKey(string tableName, string pkColumn)
        {
            return string.Format ("DELETE FROM \"{0}\" WHERE \"{1}\" = ?", tableName, pkColumn);
        }

        public static string Insert(string tableName, IEnumerable<string> columns)
        {
            return string.Format(
                "INSERT INTO \"{0}\"({1}) VALUES ({2})",
                tableName,
                string.Join(",", columns),
                string.Join(",", columns.Select(x => ":" + x)));
        }

        public static string InsertOrReplace(string tableName, IEnumerable<string> columns)
        {
            return string.Format(
                "INSERT OR REPLACE INTO \"{0}\"({1}) VALUES ({2})",
                tableName,
                string.Join(",", columns),
                string.Join(",", columns.Select(x => ":" + x)));
        }

        public static string CreateTable(string tableName, CreateFlags createFlags, IEnumerable<Tuple<string, TableColumnMetadata>> columns)
        {
            bool fts3 = (createFlags & CreateFlags.FullTextSearch3) != 0;
            bool fts4 = (createFlags & CreateFlags.FullTextSearch4) != 0;
            bool fts = fts3 || fts4;

            var @virtual = fts ? "VIRTUAL " : string.Empty;
            var @using = fts3 ? "USING FTS3 " : fts4 ? "USING FTS4 " : string.Empty;

            // Build query.
            var query = "CREATE " + @virtual + "TABLE IF NOT EXISTS \"" + tableName + "\" " + @using + "(\n";
            var decls = columns.Select (c => SqlDecl(c.Item1, c.Item2));
            var decl = string.Join (",\n", decls.ToArray ());
            query += decl;
            query += ")";

            return query;
        }

        private static string SqlDecl (string columnName,  TableColumnMetadata p)
        {
            string decl = "\"" + columnName + "\" " + p.DeclaredType + " ";
            
            if (p.IsPrimaryKeyPart) 
            {
                decl += "PRIMARY KEY ";
            }

            if (p.IsAutoIncrement) 
            {
                decl += "AUTOINCREMENT ";
            }

            if (p.HasNotNullConstraint) 
            {
                decl += "NOT NULL ";
            }

            if (!string.IsNullOrEmpty (p.CollationSequence)) 
            {
                decl += "COLLATE " + p.CollationSequence + " ";
            }
            
            return decl;
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
                    sqlCall = "(" + args[0].Item1 + " like " + args[1].Item1 + ")";
                }

                else if (call.Method.Name == "Contains" && args.Length == 2) 
                {
                    sqlCall = "(" + args[1].Item1 + " in " + args[0].Item1 + ")";
                }

                else if (call.Method.Name == "Contains" && args.Length == 1)
                 {
                    if (call.Object != null && call.Object.Type == typeof(string))
                    {
                        sqlCall = "(" + obj.Item1 + " like ('%' || " + args [0].Item1 + " || '%'))";
                    }
                    else 
                    {
                        sqlCall = "(" + args[0].Item1 + " in " + obj.Item1 + ")";
                    }
                }

                else if (call.Method.Name == "StartsWith" && args.Length == 1) 
                {
                    sqlCall = "(" + obj.Item1 + " like (" + args [0].Item1 + " || '%'))";
                }

                else if (call.Method.Name == "EndsWith" && args.Length == 1) 
                {
                    sqlCall = "(" + obj.Item1 + " like ('%' || " + args [0].Item1 + "))";
                }

                else if (call.Method.Name == "Equals" && args.Length == 1) 
                {
                    sqlCall = "(" + obj.Item1 + " = (" + args[0].Item1 + "))";
                } 

                else if (call.Method.Name == "ToLower") 
                {
                    sqlCall = "(lower(" + obj.Item1 + "))"; 
                } 

                else if (call.Method.Name == "ToUpper") 
                {
                    sqlCall = "(upper(" + obj.Item1 + "))"; 
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

                return Tuple.Create(valr.Item1, valr.Item2 != null ? SQLBuilder.ConvertTo (valr.Item2, ty) : null);
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
    }
}


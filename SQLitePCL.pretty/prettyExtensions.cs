/*
   Copyright 2014 David Bordoley
   Copyright 2014 Zumero, LLC

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.IO;

namespace SQLitePCL.pretty
{
    public static class SQLiteExtensions
    {
        public static void Execute(this IDatabaseConnection  db, string sql)
        {
            Contract.Requires(db != null);
            Contract.Requires(sql != null);

            using (var stmt = db.PrepareStatement(sql))
            {
                stmt.MoveNext();
            }
        }

        // allows only one statement in the sql string
        public static void Execute(this IDatabaseConnection  db, string sql, params object[] a)
        {
            Contract.Requires(db != null);
            Contract.Requires(sql != null);
            Contract.Requires(a != null);

            using (var stmt = db.PrepareStatement(sql, a))
            {
                stmt.MoveNext();
            }
        }

        public static void ExecuteAll(this IDatabaseConnection db, String sql)
        {
            Contract.Requires(db != null);
            Contract.Requires(sql != null);

            var statements = db.PrepareAll(sql);
            foreach (var stmt in statements)
            {
                using (stmt)
                {
                    stmt.MoveNext();
                }
            }
        }

        public static void Backup(this DatabaseConnection db, string dbName, DatabaseConnection destConn, string destDbName)
        {
            Contract.Requires(db != null);
            Contract.Requires(dbName != null);
            Contract.Requires(destConn != null);
            Contract.Requires(destDbName != null);

            using (var backup = db.BackupInit(dbName, destConn, destDbName))
            {
                backup.Step(-1);
            }
        }

        public static IEnumerable<IReadOnlyList<IResultSetValue>> Query(this IDatabaseConnection  db, string sql)
        {
            Contract.Requires(db != null);
            Contract.Requires(sql != null);

            object[] empty = new object[0];
            return db.Query(sql, empty);
        }

        public static IEnumerable<IReadOnlyList<IResultSetValue>> Query(this IDatabaseConnection  db, string sql, params object[] a)
        {
            Contract.Requires(db != null);
            Contract.Requires(sql != null);
            Contract.Requires(a != null);

            return new DelegatingEnumerable<IReadOnlyList<IResultSetValue>>(() => db.PrepareStatement(sql, a));
        }

        private static IEnumerator<IStatement> PrepareAllEnumerator(this IDatabaseConnection db, string sql)
        {
            Contract.Requires(db != null);
            Contract.Requires(sql != null);

            for (var next = sql; next != null;)
            {
                string tail = null;
                IStatement stmt = db.PrepareStatement(next, out tail);
                next = tail;
                yield return stmt;
            }
        }

        public static IEnumerable<IStatement> PrepareAll(this IDatabaseConnection db, string sql)
        {
            Contract.Requires(db != null);
            Contract.Requires(sql != null);

            return new DelegatingEnumerable<IStatement>(() => db.PrepareAllEnumerator(sql));
        }

        public static IStatement PrepareStatement(this IDatabaseConnection db, string sql)
        {
            Contract.Requires(db != null);
            Contract.Requires(sql != null);

            string tail = null;
            IStatement retval = db.PrepareStatement(sql, out tail);
            if (tail != null)
            {
                throw new ArgumentException("SQL contains more than one statment");
            }
            return retval;
        }

        public static IStatement PrepareStatement(this IDatabaseConnection db, string sql, params object[] a)
        {
            Contract.Requires(db != null);
            Contract.Requires(sql != null);
            Contract.Requires(a != null);

            var stmt = db.PrepareStatement(sql);
            stmt.Bind(a);
            return stmt;
        }

        public static void Bind(this IStatement stmt, params object[] a)
        {
            Contract.Requires(stmt != null);
            Contract.Requires(a != null);
            Contract.Requires(stmt.BindParameterCount == a.Length);

            var count = a.Length;
            for (int i = 0; i < count; i++)
            {
                // I miss F# pattern matching
                if (a[i] == null)
                {
                    stmt.BindNull(i);
                }
                else
                {
                    Type t = a[i].GetType();

                    if (typeof(String) == t)
                    {
                        stmt.Bind(i, (string)a[i]);
                    }
                    else if (
                        (typeof(Int32) == t)
                        || (typeof(Boolean) == t)
                        || (typeof(Byte) == t)
                        || (typeof(UInt16) == t)
                        || (typeof(Int16) == t)
                        || (typeof(sbyte) == t)
                        || (typeof(Int64) == t)
                        || (typeof(UInt32) == t))
                    {
                        stmt.Bind(i, (long)(Convert.ChangeType(a[i], typeof(long))));
                    }
                    else if (
                        (typeof(double) == t)
                        || (typeof(float) == t)
                        || (typeof(decimal) == t))
                    {
                        stmt.Bind(i, (double)(Convert.ChangeType(a[i], typeof(double))));
                    }
                    else if (typeof(byte[]) == t)
                    {
                        stmt.Bind(i, (byte[])a[i]);
                    }
                    else
                    {
                        throw new NotSupportedException("Invalid type conversion" + t);
                    }
                }
            }
        }

        public static IEnumerable<string> Columns(this IReadOnlyList<IResultSetValue> rs)
        { 
            Contract.Requires(rs != null);

            return rs.Select(value => value.ColumnName);
        }

        public static Stream ToReadWriteStream(this IResultSetValue value)
        {
            Contract.Requires(value != null);

            return value.ToStream(true);
        }

        public static Stream ToReadOnlyStream(this IResultSetValue value)
        {
            Contract.Requires(value != null);

            return value.ToStream(false);
        }

        public static void RegisterAggregateFunc<T>(this IDatabaseConnection db, String name, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            db.RegisterAggregateFunc(name, -1, seed, func, resultSelector);
        }

        public static void RegisterAggregateFunction<T>(this IDatabaseConnection db, String name, T seed, Func<T, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            db.RegisterAggregateFunc(name, 0, seed, (t, _) => func(t), resultSelector);
        }

        public static void RegisterAggregateFunc<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            db.RegisterAggregateFunc(name, 1, seed, (t, val) => func(t, val[0]), resultSelector);
        }

        public static void RegisterAggregateFunc<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            db.RegisterAggregateFunc(name, 2, seed, (t, val) => func(t, val[0], val[1]), resultSelector);
        }

        public static void RegisterAggregateFunc<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            db.RegisterAggregateFunc(name, 3, seed, (t, val) => func(t, val[0], val[1], val[2]), resultSelector);
        }

        public static void RegisterAggregateFunc<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            db.RegisterAggregateFunc(name, 4, seed, (t, val) => func(t, val[0], val[1], val[2], val[3]), resultSelector);
        }

        public static void RegisterAggregateFunc<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            db.RegisterAggregateFunc(name, 5, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4]), resultSelector);
        }

        public static void RegisterAggregateFunc<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            db.RegisterAggregateFunc(name, 6, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5]), resultSelector);
        }

        public static void RegisterAggregateFunc<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            db.RegisterAggregateFunc(name, 7, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5], val[6]), resultSelector);
        }

        public static void RegisterAggregateFunc<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            db.RegisterAggregateFunc(name, 8, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5], val[6], val[7]), resultSelector);
        }

        public static void RegisterScalarFunc(this IDatabaseConnection db, string name, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce)
        {            
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            db.RegisterScalarFunc(name, -1, val => reduce(val));
        }

        public static void RegisterScalarFunc(this IDatabaseConnection db, string name, Func<ISQLiteValue> reduce)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            db.RegisterScalarFunc(name, 0, _ => reduce());
        }

        public static void RegisterScalarFunc(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            db.RegisterScalarFunc(name, 1, val => reduce(val[0]));
        }

        public static void RegisterScalarFunc(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            db.RegisterScalarFunc(name, 2, val => reduce(val[0], val[1]));
        }

        public static void RegisterScalarFunc(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            db.RegisterScalarFunc(name, 3, val => reduce(val[0], val[1], val[2]));
        }

        public static void RegisterScalarFunc(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            db.RegisterScalarFunc(name, 4, val => reduce(val[0], val[1], val[2], val[3]));
        }

        public static void RegisterScalarFunc(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            db.RegisterScalarFunc(name, 5, val => reduce(val[0], val[1], val[2], val[3], val[4]));
        }

        public static void RegisterScalarFunc(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            db.RegisterScalarFunc(name, 6, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5]));
        }

        public static void RegisterScalarFunc(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            db.RegisterScalarFunc(name, 7, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5], val[6]));
        }

        public static void RegisterScalarFunc(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(db != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            db.RegisterScalarFunc(name, 8, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5], val[6], val[7]));
        }

        public static ISQLiteValue ToSQLiteValue(this int value) 
        {
            return SQLiteValue.Of(value);
        }

        public static ISQLiteValue ToSQLiteValue(this bool value) 
        {
            return SQLiteValue.Of(value);
        }

        public static ISQLiteValue ToSQLiteValue(this byte value) 
        {
            return SQLiteValue.Of(value);
        }

        public static ISQLiteValue ToSQLiteValue(this char value) 
        {
            return SQLiteValue.Of(value);
        }

        public static ISQLiteValue ToSQLiteValue(this sbyte value) 
        {
            return SQLiteValue.Of(value);
        }

        public static ISQLiteValue ToSQLiteValue(this UInt32 value) 
        {
            return SQLiteValue.Of(value);
        }

        public static ISQLiteValue ToSQLiteValue(this long value)
        {
            return SQLiteValue.Of(value);
        }

        public static ISQLiteValue ToSQLiteValue(this double value)
        {
            return SQLiteValue.Of(value);
        }

        public static ISQLiteValue ToSQLiteValue(this string value)
        {
            Contract.Requires(value != null);

            return SQLiteValue.Of(value);
        }

        public static ISQLiteValue ToSQLiteValue(this byte[] blob)
        {
            Contract.Requires(blob != null);

            return SQLiteValue.Of(blob);
        }
    }
}
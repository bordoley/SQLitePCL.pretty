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
using System.Linq;
using System.IO;

namespace SQLitePCL.pretty
{
    public static class SQLiteExtensions
    {
        public static void Execute(this IDatabaseConnection  db, string sql)
        {
            using (var stmt = db.PrepareStatement(sql))
            {
                stmt.MoveNext();
            }
        }

        // allows only one statement in the sql string
        public static void Execute(this IDatabaseConnection  db, string sql, params object[] a)
        {
            using (var stmt = db.PrepareStatement(sql, a))
            {
                stmt.MoveNext();
            }
        }

        public static void Backup(this DatabaseConnection db, string dbName, DatabaseConnection destConn, string destDbName)
        {
            using (var backup = db.BackupInit(dbName, destConn, destDbName))
            {
                backup.Step(-1);
            }
        }

        public static IEnumerable<IReadOnlyList<IResultSetValue>> Query(this IDatabaseConnection  db, string sql)
        {
            object[] empty = new object[0];
            return db.Query(sql, empty);
        }

        public static IEnumerable<IReadOnlyList<IResultSetValue>> Query(this IDatabaseConnection  db, string sql, params object[] a)
        {
            Preconditions.CheckNotNull(db);
            Preconditions.CheckNotNull(sql);
            Preconditions.CheckNotNull(a);

            return new EnumerableQuery(db, sql, a);
        }

        public static IStatement PrepareStatement(this IDatabaseConnection db, string sql, params object[] a)
        {
            var stmt = db.PrepareStatement(sql);
            stmt.Bind(a);
            return stmt;
        }

        public static void Bind(this IStatement stmt, params object[] a)
        {
            Preconditions.CheckNotNull(stmt);
            Preconditions.CheckNotNull(a);

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
            return rs.Select(value => value.ColumnName);
        }

        public static Stream ToReadWriteStream(this IResultSetValue value)
        {
            return value.ToStream(true);
        }

        public static Stream ToReadOnlyStream(this IResultSetValue value)
        {
            return value.ToStream(false);
        }

        public static void RegisterFunction<T>(this IDatabaseConnection db, String name, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            db.RegisterFunction(name, -1, seed, func, resultSelector);
        }

        public static void RegisterFunction<T>(this IDatabaseConnection db, String name, T seed, Func<T, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            db.RegisterFunction(name, 0, seed, (t, _) => func(t), resultSelector);
        }

        public static void RegisterFunction<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            db.RegisterFunction(name, 1, seed, (t, val) => func(t, val[0]), resultSelector);
        }

        public static void RegisterFunction<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            db.RegisterFunction(name, 2, seed, (t, val) => func(t, val[0], val[1]), resultSelector);
        }

        public static void RegisterFunction<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            db.RegisterFunction(name, 3, seed, (t, val) => func(t, val[0], val[1], val[2]), resultSelector);
        }

        public static void RegisterFunction<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            db.RegisterFunction(name, 4, seed, (t, val) => func(t, val[0], val[1], val[2], val[3]), resultSelector);
        }

        public static void RegisterFunction<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            db.RegisterFunction(name, 5, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4]), resultSelector);
        }

        public static void RegisterFunction<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            db.RegisterFunction(name, 6, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5]), resultSelector);
        }

        public static void RegisterFunction<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            db.RegisterFunction(name, 7, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5], val[6]), resultSelector);
        }

        public static void RegisterFunction<T>(this IDatabaseConnection db, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            db.RegisterFunction(name, 8, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5], val[6], val[7]), resultSelector);
        }

        public static void RegisterFunction(this IDatabaseConnection db, string name, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce)
        {
            db.RegisterFunction(name, -1, val => reduce(val));
        }

        public static void RegisterFunction(this IDatabaseConnection db, string name, Func<ISQLiteValue> reduce)
        {
            db.RegisterFunction(name, 0, _ => reduce());
        }

        public static void RegisterFunction(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue> reduce)
        {
            db.RegisterFunction(name, 1, val => reduce(val[0]));
        }

        public static void RegisterFunction(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            db.RegisterFunction(name, 2, val => reduce(val[0], val[1]));
        }

        public static void RegisterFunction(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            db.RegisterFunction(name, 3, val => reduce(val[0], val[1], val[2]));
        }

        public static void RegisterFunction(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            db.RegisterFunction(name, 4, val => reduce(val[0], val[1], val[2], val[3]));
        }

        public static void RegisterFunction(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            db.RegisterFunction(name, 5, val => reduce(val[0], val[1], val[2], val[3], val[4]));
        }

        public static void RegisterFunction(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            db.RegisterFunction(name, 6, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5]));
        }

        public static void RegisterFunction(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            db.RegisterFunction(name, 7, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5], val[6]));
        }

        public static void RegisterFunction(this IDatabaseConnection db, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            db.RegisterFunction(name, 8, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5], val[6], val[7]));
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
            return SQLiteValue.Of(value);
        }

        public static ISQLiteValue ToSQLiteValue(this byte[] blob)
        {
            return SQLiteValue.Of(blob);
        }
    }

    internal sealed class EnumerableQuery : IEnumerable<IReadOnlyList<IResultSetValue>> 
    {
        private readonly IDatabaseConnection  db;
        private readonly string sql; 
        private readonly object[] bindArgs;

        internal EnumerableQuery(IDatabaseConnection  db, string sql, object[] bindArgs)
        {
            this.db = db;
            this.sql = sql;
            this.bindArgs = bindArgs;
        }

        public IEnumerator<IReadOnlyList<IResultSetValue>> GetEnumerator()
        {
            return db.PrepareStatement(sql, bindArgs);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Provides data for <see cref="IDatabaseConnection"/> <see cref="IDatabaseConnection.Profile"/> events.
    /// </summary>
    public sealed class DatabaseProfileEventArgs : EventArgs
    {
        internal static DatabaseProfileEventArgs Create(string statement, TimeSpan executionTime)
        {
            Contract.Requires(statement != null);
            return new DatabaseProfileEventArgs(statement, executionTime);
        }

        private readonly string statement;
        private readonly TimeSpan executionTime;

        private DatabaseProfileEventArgs(string statement, TimeSpan executionTime)
        {
            this.statement = statement;
            this.executionTime = executionTime;
        }

        /// <summary>
        /// The SQL statement being profiled.
        /// </summary>
        public string Statement
        {
            get
            {
                return statement;
            }
        }

        /// <summary>
        /// The execution time of the statement.
        /// </summary>
        public TimeSpan ExecutionTime
        {
            get
            {
                return executionTime;
            }
        }
    }

    /// <summary>
    /// Provides data for <see cref="IDatabaseConnection"/> <see cref="IDatabaseConnection.Trace"/> events.
    /// </summary>
    public sealed class DatabaseTraceEventArgs : EventArgs
    {
        internal static DatabaseTraceEventArgs Create(string statement)
        {
            Contract.Requires(statement != null);
            return new DatabaseTraceEventArgs(statement);
        }

        private readonly string statement;

        private DatabaseTraceEventArgs(string statement)
        {
            this.statement = statement;
        }

        /// <summary>
        /// The SQL statement text as the statement first begins executing which caused the trace event.
        /// </summary>
        public string Statement
        {
            get
            {
                return statement;
            }
        }
    }

    /// <summary>
    /// Provides data for <see cref="IDatabaseConnection"/> <see cref="IDatabaseConnection.Update"/> events.
    /// </summary>
    public sealed class DatabaseUpdateEventArgs : EventArgs
    {
        internal static DatabaseUpdateEventArgs Create(ActionCode action, String database, string table, long rowId)
        {
            Contract.Requires(database != null);
            Contract.Requires(table != null);

            return new DatabaseUpdateEventArgs(action, database, table, rowId);
        }

        private readonly ActionCode action;
        private readonly String database;
        private readonly String table;
        private readonly long rowId;

        private DatabaseUpdateEventArgs(ActionCode action, String database, string table, long rowId)
        {
            this.action = action;
            this.database = database;
            this.table = table;
            this.rowId = rowId;
        }

        /// <summary>
        /// The SQL operation that caused the update event.
        /// </summary>
        public ActionCode Action
        {
            get
            {
                return action;
            }
        }

        /// <summary>
        /// The database containing the affected row.
        /// </summary>
        public String Database
        {
            get
            {
                return database;
            }
        }

        /// <summary>
        /// The table name containing the affected row.
        /// </summary>
        public String Table
        {
            get
            {
                return table;
            }
        }

        /// <summary>
        /// The rowid of the row updated.
        /// </summary>
        public long RowId
        {
            get
            {
                return rowId;
            }
        }
    }

    /// <summary>
    /// Extensions methods for instances of <see cref="IDatabaseConnection"/>
    /// </summary>
    public static class DatabaseConnection
    {
        /// <summary>
        /// Compiles and executes a SQL statement.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="sql">The SQL statement to compile and execute.</param>
        public static void Execute(this IDatabaseConnection This, string sql)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            using (var stmt = This.PrepareStatement(sql))
            {
                stmt.MoveNext();
            }
        }

        /// <summary>
        /// Compiles and executes a SQL statement with the provided bind parameter values.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="sql">The SQL statement to compile and execute.</param>
        /// <param name="values">The bind parameter values.</param>
        public static void Execute(this IDatabaseConnection This, string sql, params object[] values)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(values != null);

            using (var stmt = This.PrepareStatement(sql))
            {
                stmt.Bind(values);
                stmt.MoveNext();
            }
        }

        /// <summary>
        /// Compiles and executes multiple SQL statements.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="sql">One or more semicolon delimited SQL statements.</param>
        public static void ExecuteAll(this IDatabaseConnection This, String sql)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            var statements = This.PrepareAll(sql);
            foreach (var stmt in statements)
            {
                using (stmt)
                {
                    stmt.MoveNext();
                }
            }
        }

        /// <summary>
        /// Performs a full backup from <paramref name="This"/> to <paramref name="destConn"/>.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="dbName">The name of the database to backup.</param>
        /// <param name="destConn">The destination database connection.</param>
        /// <param name="destDbName">The destination database name.</param>
        public static void Backup(this SQLiteDatabaseConnection This, string dbName, SQLiteDatabaseConnection destConn, string destDbName)
        {
            Contract.Requires(This != null);
            Contract.Requires(dbName != null);
            Contract.Requires(destConn != null);
            Contract.Requires(destDbName != null);

            using (var backup = This.BackupInit(dbName, destConn, destDbName))
            {
                backup.Step(-1);
            }
        }

        /// <summary>
        /// Compiles a SQL statement, returning the an <see cref="IEnumerable&lt;T&gt;"/> of rows in the result set. 
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="sql">The SQL statement to compile and Query.</param>
        /// <returns>An <see cref="IEnumerable&lt;T&gt;"/> of rows in the result set.</returns>
        public static IEnumerable<IReadOnlyList<IResultSetValue>> Query(
            this IDatabaseConnection This, string sql)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            object[] empty = { };
            return This.Query(sql, empty);
        }

        /// <summary>
        ///  Compiles a SQL statement with provided bind parameter values,
        ///  returning the an <see cref="IEnumerable&lt;T&gt;"/> of rows in the result set. 
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="sql">The SQL statement to compile and Query.</param>
        /// <param name="values">The bind parameter values.</param>
        /// <returns>An <see cref="IEnumerable&lt;T&gt;"/> of rows in the result set.</returns>
        public static IEnumerable<IReadOnlyList<IResultSetValue>> Query(
            this IDatabaseConnection This, string sql, params object[] values)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);
            Contract.Requires(values != null);

            return new DelegatingEnumerable<IReadOnlyList<IResultSetValue>>(() =>
                {
                    var stmt = This.PrepareStatement(sql);
                    stmt.Bind(values);
                    return stmt;
                });
        }

        private static IEnumerator<IStatement> PrepareAllEnumerator(this IDatabaseConnection This, string sql)
        {
            for (var next = sql; next != null; )
            {
                string tail = null;
                IStatement stmt = This.PrepareStatement(next, out tail);
                next = tail;
                yield return stmt;
            }
        }

        /// <summary>
        /// Compiles one or more SQL statements.
        /// </summary>
        /// <param name="This">The database connection</param>
        /// <param name="sql">One or more semicolon delimited SQL statements.</param>
        /// <returns>A lazily evaluated <see cref="IEnumerable&lt;IStatement&gt;"/>.
        /// </returns>
        public static IEnumerable<IStatement> PrepareAll(this IDatabaseConnection This, string sql)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            return new DelegatingEnumerable<IStatement>(() => This.PrepareAllEnumerator(sql));
        }

        /// <summary>
        /// Compiles a SQL statement.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="sql">The SQL statement to compile.</param>
        /// <returns>The compiled statement.</returns>
        public static IStatement PrepareStatement(this IDatabaseConnection This, string sql)
        {
            Contract.Requires(This != null);
            Contract.Requires(sql != null);

            string tail = null;
            IStatement retval = This.PrepareStatement(sql, out tail);
            if (tail != null)
            {
                throw new ArgumentException("SQL contains more than one statment");
            }
            return retval;
        }

        /// <summary>
        /// Register an aggregate function that can accept any number of <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        public static void RegisterAggregateFunc<T>(this IDatabaseConnection This, String name, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            This.RegisterAggregateFunc(name, -1, seed, func, resultSelector);
        }

        /// <summary>
        /// Register an aggregate function that accepts no <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        public static void RegisterAggregateFunc<T>(this IDatabaseConnection This, String name, T seed, Func<T, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            This.RegisterAggregateFunc(name, 0, seed, (t, _) => func(t), resultSelector);
        }

        /// <summary>
        /// Register an aggregate function that accepts 1 <see href="ISQLiteValue"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        public static void RegisterAggregateFunc<T>(this IDatabaseConnection This, String name, T seed, Func<T, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            This.RegisterAggregateFunc(name, 1, seed, (t, val) => func(t, val[0]), resultSelector);
        }

        /// <summary>
        /// Register an aggregate function that accepts 2 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        public static void RegisterAggregateFunc<T>(this IDatabaseConnection This, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            This.RegisterAggregateFunc(name, 2, seed, (t, val) => func(t, val[0], val[1]), resultSelector);
        }

        /// <summary>
        /// Register an aggregate function that accepts 3 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        public static void RegisterAggregateFunc<T>(this IDatabaseConnection This, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            This.RegisterAggregateFunc(name, 3, seed, (t, val) => func(t, val[0], val[1], val[2]), resultSelector);
        }

        /// <summary>
        /// Register an aggregate function that accepts 4 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        public static void RegisterAggregateFunc<T>(this IDatabaseConnection This, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            This.RegisterAggregateFunc(name, 4, seed, (t, val) => func(t, val[0], val[1], val[2], val[3]), resultSelector);
        }

        /// <summary>
        /// Register an aggregate function that accepts 5 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        public static void RegisterAggregateFunc<T>(this IDatabaseConnection This, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            This.RegisterAggregateFunc(name, 5, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4]), resultSelector);
        }

        /// <summary>
        /// Register an aggregate function that accepts 6 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        public static void RegisterAggregateFunc<T>(this IDatabaseConnection This, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            This.RegisterAggregateFunc(name, 6, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5]), resultSelector);
        }

        /// <summary>
        /// Register an aggregate function that accepts 7 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        public static void RegisterAggregateFunc<T>(this IDatabaseConnection This, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            This.RegisterAggregateFunc(name, 7, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5], val[6]), resultSelector);
        }

        /// <summary>
        /// Register an aggregate function that accepts 8 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        public static void RegisterAggregateFunc<T>(this IDatabaseConnection This, String name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);

            This.RegisterAggregateFunc(name, 8, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5], val[6], val[7]), resultSelector);
        }

        /// <summary>
        /// Registers a scalar function that can accept any number of <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        public static void RegisterScalarFunc(this IDatabaseConnection This, string name, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            This.RegisterScalarFunc(name, -1, val => reduce(val));
        }

        /// <summary>
        /// Registers a scalar function that accepts 0 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        public static void RegisterScalarFunc(this IDatabaseConnection This, string name, Func<ISQLiteValue> reduce)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            This.RegisterScalarFunc(name, 0, _ => reduce());
        }


        /// <summary>
        /// Registers a scalar function that accepts 1 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        public static void RegisterScalarFunc(this IDatabaseConnection This, string name, Func<ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            This.RegisterScalarFunc(name, 1, val => reduce(val[0]));
        }

        /// <summary>
        /// Registers a scalar function that accepts 2 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        public static void RegisterScalarFunc(this IDatabaseConnection This, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            This.RegisterScalarFunc(name, 2, val => reduce(val[0], val[1]));
        }

        /// <summary>
        /// Registers a scalar function that accepts 3 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        public static void RegisterScalarFunc(this IDatabaseConnection This, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            This.RegisterScalarFunc(name, 3, val => reduce(val[0], val[1], val[2]));
        }

        /// <summary>
        /// Registers a scalar function that accepts 4 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        public static void RegisterScalarFunc(this IDatabaseConnection This, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            This.RegisterScalarFunc(name, 4, val => reduce(val[0], val[1], val[2], val[3]));
        }

        /// <summary>
        /// Registers a scalar function that accepts 5 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        public static void RegisterScalarFunc(this IDatabaseConnection This, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            This.RegisterScalarFunc(name, 5, val => reduce(val[0], val[1], val[2], val[3], val[4]));
        }

        /// <summary>
        /// Registers a scalar function that accepts 6 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        public static void RegisterScalarFunc(this IDatabaseConnection This, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            This.RegisterScalarFunc(name, 6, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5]));
        }

        /// <summary>
        /// Registers a scalar function that accepts 7 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        public static void RegisterScalarFunc(this IDatabaseConnection This, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            This.RegisterScalarFunc(name, 7, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5], val[6]));
        }

        /// <summary>
        /// Registers a scalar function that accepts 8 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        public static void RegisterScalarFunc(this IDatabaseConnection This, string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(This != null);
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);

            This.RegisterScalarFunc(name, 8, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5], val[6], val[7]));
        }

        /// <summary>
        /// Returns the filename associated with the database.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="database">The database name. The main database file has the name "main".</param>
        /// <returns>The attached database's filename.</returns>
        /// <exception cref="InvalidOperationException">If the database is not attached, temporary, or in memory.</exception>
        public static string GetFileName(this IDatabaseConnection This, string database)
        {
            Contract.Requires(This != null);
            Contract.Requires(database != null);

            string filename = null;

            if (This.TryGetFileName(database, out filename))
            {
                return filename;
            }

            throw new InvalidOperationException("Database is either not attached, temporary or in memory");
        }

        /// <summary>
        /// Opens the blob located by the <see cref="ColumnInfo"/> and rowid for incremental I/O as a <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="columnInfo">The ColumnInfo of the blob value.</param>
        /// <param name="rowId">The row containing the blob.</param>
        /// <param name="canWrite">
        ///     <see langwords="true"/> if the Stream should be open for both read and write operations. 
        ///     <see langwords="false"/> if the Stream should be open oly for read operations. 
        /// </param>
        /// <returns>A <see cref="System.IO.Stream"/> that can be used to synchronously write and read to and from blob.</returns>
        public static Stream OpenBlob(this IDatabaseConnection This, ColumnInfo columnInfo, long rowId, bool canWrite)
        {
            Contract.Requires(This != null);
            Contract.Requires(columnInfo != null);
            return This.OpenBlob(columnInfo.DatabaseName, columnInfo.TableName, columnInfo.OriginName, rowId, canWrite);
        }
    }

    /// <summary>
    /// An implementation of IDatabaseConnection that wraps a raw SQLite database connection.
    /// </summary>
    /// <remarks>The sole purpose of using this implementation directly is to initiate database backups.</remarks>
    public sealed class SQLiteDatabaseConnection : IDatabaseConnection
    {
        private readonly sqlite3 db;
        private readonly Dictionary<sqlite3_stmt, StatementImpl> statements = new Dictionary<sqlite3_stmt, StatementImpl>();
        private readonly IEnumerable<IStatement> statementsEnumerable;

        private bool disposed = false;

        internal SQLiteDatabaseConnection(sqlite3 db)
        {
            this.db = db;
            this.statementsEnumerable = new DelegatingEnumerable<IStatement>(() => StatementsEnumerator());

            // FIXME: Could argue that the shouldn't be setup until the first subscriber to the events
            raw.sqlite3_rollback_hook(db, v => Rollback(this, EventArgs.Empty), null);

            raw.sqlite3_trace(
                db,
                (v, stmt) => Trace(this, DatabaseTraceEventArgs.Create(stmt)),
                null);

            raw.sqlite3_profile(
                db, (v, stmt, ts) => Profile(this, DatabaseProfileEventArgs.Create(stmt, TimeSpan.FromTicks(ts))),
                null);

            raw.sqlite3_update_hook(
                db,
                (v, type, database, table, rowid) => Update(this, DatabaseUpdateEventArgs.Create((ActionCode)type, database, table, rowid)),
                null);
        }

        // We initialize the event handlers with empty delegates so that we don't
        // have add annoying null checks all over the place.
        // See: http://blogs.msdn.com/b/ericlippert/archive/2009/04/29/events-and-races.aspx
        // FIXME: One could argue that we really shouldn't initialized the callbacks
        // with sqlite3 until we actually have listeners. not sure how much it matters though
        
        /// <inheritdoc/>
        public event EventHandler Rollback = (o, e) => { };

        /// <inheritdoc/>
        public event EventHandler<DatabaseProfileEventArgs> Profile = (o, e) => { };

        /// <inheritdoc/>
        public event EventHandler<DatabaseTraceEventArgs> Trace = (o, e) => { };

        /// <inheritdoc/>
        public event EventHandler<DatabaseUpdateEventArgs> Update = (obj, args) => { };

        /// <inheritdoc/>
        public TimeSpan BusyTimeout
        {
            set
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                int rc = raw.sqlite3_busy_timeout(db, (int)value.TotalMilliseconds);
                SQLiteException.CheckOk(db, rc);
            }
        }

        /// <inheritdoc/>
        public int Changes
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return raw.sqlite3_changes(db);
            }
        }

        /// <inheritdoc/>
        public bool IsAutoCommit
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return raw.sqlite3_get_autocommit(db) == 0 ? false : true;
            }
        }

        /// <inheritdoc/>
        public long LastInsertedRowId
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return raw.sqlite3_last_insert_rowid(db);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IStatement> Statements
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return this.statementsEnumerable;
            }
        }

        private IEnumerator<IStatement> StatementsEnumerator()
        {
            sqlite3_stmt next = null;

            while (true)
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                next = raw.sqlite3_next_stmt(db, next);
                if (next != null)
                {
                    yield return statements[next];
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Initializes a backup of database to a destination database.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/backup_finish.html"/>
        /// <param name="dbName">The name of the database to backup.</param>
        /// <param name="destConn">The destination database connection.</param>
        /// <param name="destDbName">The destination database name</param>
        /// <returns>An <see cref="IDatabaseBackup"/> instance that can be used to
        /// perform the backup operation.</returns>
        public IDatabaseBackup BackupInit(string dbName, SQLiteDatabaseConnection destConn, string destDbName)
        {
            Contract.Requires(dbName != null);
            Contract.Requires(destConn != null);
            Contract.Requires(destDbName != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            sqlite3_backup backup = raw.sqlite3_backup_init(destConn.db, destDbName, db, dbName);
            return new DatabaseBackupImpl(backup);
        }

        /// <inheritdoc/>
        public bool TryGetFileName(string database, out string filename)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            filename = raw.sqlite3_db_filename(db, database);

            // If there is no attached database N on the database connection, or
            // if database N is a temporary or in-memory database, then a NULL pointer is returned.
            return !String.IsNullOrEmpty(filename);
        }

        /// <inheritdoc/>
        public Stream OpenBlob(string database, string tableName, string columnName, long rowId, bool canWrite)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            sqlite3_blob blob;
            int rc = raw.sqlite3_blob_open(db, database, tableName, columnName, rowId, canWrite ? 1 : 0, out blob);
            SQLiteException.CheckOk(db, rc);

            var length = raw.sqlite3_blob_bytes(blob);

            return new BlobStream(blob, canWrite, length);
        }

        /// <inheritdoc/>
        public IStatement PrepareStatement(string sql, out string tail)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            sqlite3_stmt stmt;
            int rc = raw.sqlite3_prepare_v2(db, sql, out stmt, out tail);
            SQLiteException.CheckOk(db, rc);

            var retval = new StatementImpl(stmt, this);
            statements.Add(stmt, retval);
            return retval;
        }

        internal void RemoveStatement(StatementImpl stmt)
        {
            statements.Remove(stmt.sqlite3_stmt);
        }

        /// <inheritdoc/>
        public void RegisterCollation(string name, Comparison<string> comparison)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            int rc = raw.sqlite3_create_collation(db, name, null, (v, s1, s2) => comparison(s1, s2));
            SQLiteException.CheckOk(db, rc);
        }

        /// <inheritdoc/>
        public void RemoveCollation(string name)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
            int rc = raw.sqlite3_create_collation(db, name, null, null);
            SQLiteException.CheckOk(db, rc);
        }

        /// <inheritdoc/>
        public void RegisterCommitHook(Func<bool> onCommit)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            raw.sqlite3_commit_hook(db, v => onCommit() ? 1 : 0, null);
        }

        /// <inheritdoc/>
        public void RemoveCommitHook()
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            raw.sqlite3_commit_hook(db, null, null);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            disposed = true;
            db.Dispose();
        }

        private sealed class CtxState<T>
        {
            private readonly T value;

            internal CtxState(T value)
            {
                this.value = value;
            }

            internal T Value
            {
                get
                {
                    return value;
                }
            }
        }

        /// <inheritdoc/>
        public void RegisterAggregateFunc<T>(string name, int nArg, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            delegate_function_aggregate_step funcStep = (ctx, user_data, args) =>
                {
                    CtxState<T> state;
                    if (ctx.state == null)
                    {
                        state = new CtxState<T>(seed);
                        ctx.state = state;
                    }
                    else
                    {
                        state = (CtxState<T>)ctx.state;
                    }

                    IReadOnlyList<ISQLiteValue> iArgs = args.Select(value => value.ToSQLiteValue()).ToList();

                    T next = func(state.Value, iArgs);
                    ctx.state = new CtxState<T>(next);
                };

            delegate_function_aggregate_final funcFinal = (ctx, user_data) =>
                {
                    CtxState<T> state = (CtxState<T>)ctx.state;

                    // FIXME: Is catching the exception really the right thing to do?
                    try
                    {
                        ISQLiteValue result = resultSelector(state.Value);
                        switch (result.SQLiteType)
                        {
                            case SQLiteType.Blob:
                                raw.sqlite3_result_blob(ctx, result.ToBlob());
                                return;

                            case SQLiteType.Null:
                                raw.sqlite3_result_null(ctx);
                                return;

                            case SQLiteType.Text:
                                raw.sqlite3_result_text(ctx, result.ToString());
                                return;

                            case SQLiteType.Float:
                                raw.sqlite3_result_double(ctx, result.ToDouble());
                                return;

                            case SQLiteType.Integer:
                                raw.sqlite3_result_int64(ctx, result.ToInt64());
                                return;
                        }
                    }
                    catch (Exception e)
                    {
                        raw.sqlite3_result_error(ctx, e.Message);
                    }
                };

            int rc = raw.sqlite3_create_function(db, name, nArg, null, funcStep, funcFinal);
            SQLiteException.CheckOk(db, rc);
        }

        /// <inheritdoc/>
        public void RegisterScalarFunc(string name, int nArg, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            int rc = raw.sqlite3_create_function(db, name, nArg, null, (ctx, ud, args) =>
                {
                    IReadOnlyList<ISQLiteValue> iArgs = args.Select(value => value.ToSQLiteValue()).ToList();

                    // FIXME: Is catching the exception really the right thing to do?
                    try
                    {
                        ISQLiteValue result = reduce(iArgs);
                        switch (result.SQLiteType)
                        {
                            case SQLiteType.Blob:
                                raw.sqlite3_result_blob(ctx, result.ToBlob());
                                return;

                            case SQLiteType.Null:
                                raw.sqlite3_result_null(ctx);
                                return;

                            case SQLiteType.Text:
                                raw.sqlite3_result_text(ctx, result.ToString());
                                return;

                            case SQLiteType.Float:
                                raw.sqlite3_result_double(ctx, result.ToDouble());
                                return;

                            case SQLiteType.Integer:
                                raw.sqlite3_result_int64(ctx, result.ToInt64());
                                return;
                        }
                    }
                    catch (Exception e)
                    {
                        raw.sqlite3_result_error(ctx, e.Message);
                    }
                });
            SQLiteException.CheckOk(db, rc);
        }

        /// <inheritdoc/>
        public void RemoveFunc(string name, int nArg)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            int rc = raw.sqlite3_create_function(db, name, nArg, null, null);
            SQLiteException.CheckOk(db, rc);
        }
    }
}

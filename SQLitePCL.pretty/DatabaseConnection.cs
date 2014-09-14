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

namespace SQLitePCL.pretty
{
    public sealed class DatabaseProfileEventArgs : EventArgs
    {
        public static DatabaseProfileEventArgs Create(string statement, TimeSpan elapsed)
        {
            Contract.Requires(statement != null);
            return new DatabaseProfileEventArgs(statement, elapsed);
        }

        private readonly string statement;
        private readonly TimeSpan elapsed;

        private DatabaseProfileEventArgs(string statement, TimeSpan elapsed)
        {
            this.statement = statement;
            this.elapsed = elapsed;
        }

        public string Statement
        {
            get
            {
                return statement;
            }
        }

        public TimeSpan ExecutionTime
        {
            get
            {
                return elapsed;
            }
        }
    }

    public sealed class DatabaseTraceEventArgs : EventArgs
    {
        public static DatabaseTraceEventArgs Create(string statement)
        {
            Contract.Requires(statement != null);
            return new DatabaseTraceEventArgs(statement);
        }

        private readonly string statement;

        private DatabaseTraceEventArgs(string statement)
        {
            this.statement = statement;
        }

        public string Statement
        {
            get
            {
                return statement;
            }
        }
    }

    public sealed class DatabaseUpdateEventArgs : EventArgs
    {
        public static DatabaseUpdateEventArgs Create(ActionCode action, String database, string table, long rowId)
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

        public ActionCode Action
        {
            get
            { 
                return action;
            }
        }

        public String Database
        {
            get
            {
                return database;
            }
        }

        public String Table
        {
            get
            {
                return table;
            }
        }

        public long RowId
        {
            get
            {
                return rowId;
            }
        }
    }

    public static class DatabaseConnection
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

        public static void Backup(this SQLiteDatabaseConnection db, string dbName, SQLiteDatabaseConnection destConn, string destDbName)
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
    }

    public sealed class SQLiteDatabaseConnection : IDatabaseConnection
    {
        private readonly sqlite3 db;
        private readonly IEnumerable<IStatement> statements;

        internal SQLiteDatabaseConnection(sqlite3 db)
        {
            this.db = db;

            statements = new DelegatingEnumerable<IStatement>(() => StatementsEnumerator());

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
        public event EventHandler Rollback = (o, e) => { };
        public event EventHandler<DatabaseProfileEventArgs> Profile = (o,e) => {};
        public event EventHandler<DatabaseTraceEventArgs> Trace = (o,e) => {};
        public event EventHandler<DatabaseUpdateEventArgs> Update = (obj, args) => {};

        public int BusyTimeout
        {
            set
            {
                int rc = raw.sqlite3_busy_timeout(db, value);
                SQLiteException.CheckOk(db, rc);
            }
        }

        public int Changes
        {
            get
            {
                return raw.sqlite3_changes(db);
            }
        }

        public bool IsAutoCommit
        { 
            get
            {
                return raw.sqlite3_get_autocommit(db) == 0 ? false : true;
            }
        }

        private IEnumerator<IStatement> StatementsEnumerator()
        {
            sqlite3_stmt next = null;
            while ((next = raw.sqlite3_next_stmt(db, next)) != null)
            {
                yield return new StatementImpl(next);
            }
        }

        public IEnumerable<IStatement> Statements 
        { 
            get
            {
                return statements;
            }
        }

        public IDatabaseBackup BackupInit(string dbName, SQLiteDatabaseConnection destConn, string destDbName)
        {
            Contract.Requires(dbName != null);
            Contract.Requires(destConn != null);
            Contract.Requires(destDbName != null);

            sqlite3_backup backup = raw.sqlite3_backup_init(destConn.db, destDbName, db, dbName);
            return new DatabaseBackupImpl(backup);
        }

        public string GetFileName(string database)
        {
            Contract.Requires(database != null);

            var filename = raw.sqlite3_db_filename(db, database);

            // If there is no attached database N on the database connection, or 
            // if database N is a temporary or in-memory database, then a NULL pointer is returned.
            if (filename == null)
            {
                throw new InvalidOperationException("Database is either not attached, temporary or in memory");
            }

            return filename;
        }

        public IStatement PrepareStatement(string sql, out string tail)
        {
            Contract.Requires(sql != null);

            sqlite3_stmt stmt;
            int rc = raw.sqlite3_prepare_v2(db, sql, out stmt, out tail);
            SQLiteException.CheckOk(db, rc);

            return new StatementImpl(stmt);
        }

        public void RegisterCollation(string name, Comparison<string> comparison)
        {
            Contract.Requires(name != null);
            Contract.Requires(comparison != null);

            int rc = raw.sqlite3_create_collation(db, name, null, (v, s1, s2) => comparison(s1, s2));
            SQLiteException.CheckOk(db, rc);
        }

        public void RegisterCommitHook(Func<bool> onCommit)
        {
            Contract.Requires(onCommit != null);

            raw.sqlite3_commit_hook(db, v => onCommit() ? 1 : 0, null);
        }

        public void Dispose()
        {
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

        public void RegisterAggregateFunc<T>(string name, int nArg, T seed, Func<T, IReadOnlyList<ISQLiteValue>,T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);
            Contract.Requires(nArg >= -1);

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
            SQLiteException.CheckOk(rc);
        }

        public void RegisterScalarFunc(string name, int nArg, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce)
        {
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);
            Contract.Requires(nArg >= -1);

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
            SQLiteException.CheckOk(rc);
        }
    }
}
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
using System.Text;

namespace SQLitePCL.pretty
{
    public struct SQLiteVersion : IComparable<SQLiteVersion>
    {
        internal static SQLiteVersion Of(int version)
        {
            int release = version % 1000;
            int minor = (version / 1000) % 1000; 
            int major = version / 1000000;
            return new SQLiteVersion(major, minor, release);
        }

        private readonly int major;
        private readonly int minor;
        private readonly int release;

        internal SQLiteVersion(int major, int minor, int release)
        {
            this.major = major;
            this.minor = minor;
            this.release = release;
        }

        public int Major
        {
            get
            {
                return major;
            }
        }

        public int Minor
        {
            get
            {
                return minor;
            }
        }

        public int Release
        {
            get
            {
                return release;
            }
        }

        public int ToInt()
        {
            return (this.Major * 1000000 + this.Minor * 1000 + this.Release); 
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", major, minor, release);
        }

        public int CompareTo(SQLiteVersion other)
        {
            return this.ToInt().CompareTo(other.ToInt());
        }
    }

    public static class SQLite3
    {
        public static IEnumerable<String> CompilerOptions
        {
            get
            {
                for (int i = 0;; i++)
                {
                    var option = raw.sqlite3_compileoption_get(i);
                    if (option != null)
                    {
                        yield return option;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private static readonly SQLiteVersion version = SQLiteVersion.Of(raw.sqlite3_libversion_number());

        public static SQLiteVersion Version
        {
            get
            {
                return version;
            }
        }

        public static string SourceId
        {
            get
            {
                return raw.sqlite3_sourceid();
            }
        }

        public static long MemoryUsed
        {
            get
            {
                return raw.sqlite3_memory_used();
            }
        }

        public static long MemoryHighWater
        {
            get
            {
                return raw.sqlite3_memory_highwater(0);
            }
        }

        public static bool CompileOptionUsed(string option)
        {
            return raw.sqlite3_compileoption_used(option) == 0 ? false : true;
        }

        public static DatabaseConnection Open(string filename)
        {
            Preconditions.CheckNotNull(filename);

            sqlite3 db;
            int rc = raw.sqlite3_open(filename, out db);
            SQLiteException.CheckOk(rc);

            return new DatabaseConnection(db);
        }

        public static DatabaseConnection Open(string filename, ConnectionFlags flags, string vfs)
        {
            Preconditions.CheckNotNull(filename);

            sqlite3 db;
            int rc = raw.sqlite3_open_v2(filename, out db, (int)flags, vfs);
            SQLiteException.CheckOk(rc);

            return new DatabaseConnection(db);
        }

        public static void ResetMemoryHighWater()
        {
            raw.sqlite3_memory_highwater(1);
        }

        public static bool IsCompleteStatement(string sql)
        {
            Preconditions.CheckNotNull(sql);
            return raw.sqlite3_complete(sql) == 0 ? false : true;
        }
    }

    public sealed class DatabaseConnection : IDatabaseConnection
    {
        private readonly sqlite3 db;

        internal DatabaseConnection(sqlite3 db)
        {
            this.db = db;

            // FIXME: Could argue that the shouldn't be setup until the first subscriber to the events
            raw.sqlite3_rollback_hook(db, v => Rollback(), null);
            raw.sqlite3_trace(db, (v, stmt) => Trace(stmt), null);
            raw.sqlite3_profile(db, (v, stmt, ts) => Profile(stmt, TimeSpan.FromTicks(ts)), null); 
            raw.sqlite3_update_hook(db, (v, type, database, table, rowid) => Update((ActionCode)type, database, table, rowid), null);
        }

        // We initialize the event handlers with empty delegates so that we don't
        // have add annoying null checks all over the place.
        // See: http://blogs.msdn.com/b/ericlippert/archive/2009/04/29/events-and-races.aspx
        // FIXME: One could argue that we really shouldn't initialized the callbacks
        // with sqlite3 until we actually have listeners. not sure how much it matters though
        public event Action Rollback = () => { };
        public event Action<String,TimeSpan> Profile = (stmt, TimeSpan) => {};
        public event Action<String> Trace = (stmt) => {};

        public event Action<ActionCode,string, string,long> Update = (type, database, table, rowid) => {};

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

        public IDatabaseBackup BackupInit(string dbName, DatabaseConnection destConn, string destDbName)
        {
            Preconditions.CheckNotNull(dbName);
            Preconditions.CheckNotNull(destConn);
            Preconditions.CheckNotNull(destDbName);

            sqlite3_backup backup = raw.sqlite3_backup_init(destConn.db, destDbName, db, dbName);
            return new DatabaseBackup(backup);
        }

        public IEnumerator<IStatement> GetEnumerator()
        {
            sqlite3_stmt next = null;
            while ((next = raw.sqlite3_next_stmt(db, next)) != null)
            {
                yield return new Statement(next);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public string GetFileName(string database)
        {
            Preconditions.CheckNotNull(database);

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
            Preconditions.CheckNotNull(sql);

            sqlite3_stmt stmt;
            int rc = raw.sqlite3_prepare_v2(db, sql, out stmt, out tail);
            SQLiteException.CheckOk(db, rc);

            return new Statement(stmt);
        }

        public void RegisterCollation(string name, Comparison<string> comparison)
        {
            Preconditions.CheckNotNull(name);
            Preconditions.CheckNotNull(comparison);

            int rc = raw.sqlite3_create_collation(db, name, null, (v, s1, s2) => comparison(s1, s2));
            SQLiteException.CheckOk(db, rc);
        }

        public void RegisterCommitHook(Func<bool> onCommit)
        {
            Preconditions.CheckNotNull(onCommit);

            raw.sqlite3_commit_hook(db, v => onCommit() ? 1 : 0, null);
        }

        // FIXME: The Raw db.Dispose method is calling sqlite3_close not close_v2.
        // Should probably open a bug.
        // From the docs:
        // If the database connection is associated with unfinalized prepared statements
        // or unfinished sqlite3_backup objects then sqlite3_close() will leave the
        // database connection open and return SQLITE_BUSY. If sqlite3_close_v2() is called
        // with unfinalized prepared statements and/or unfinished sqlite3_backups, then the
        // database connection becomes an unusable "zombie" which will automatically be deallocated
        // when the last prepared statement is finalized or the last sqlite3_backup is finished.
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

        public void RegisterFunction<T>(string name, int nArg, T seed, Func<T, IReadOnlyList<ISQLiteValue>,T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Preconditions.CheckNotNull(name);
            Preconditions.CheckNotNull(func);
            Preconditions.CheckNotNull(resultSelector);
            Preconditions.CheckArgument(nArg >= -1);

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

                IReadOnlyList<ISQLiteValue> iArgs = args.Select(value => SQLiteValue.Of(value)).ToList();

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
                        case SQLiteType.String:
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

        public void RegisterFunction(string name, int nArg, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce)
        {
            Preconditions.CheckNotNull(name);
            Preconditions.CheckNotNull(reduce);
            Preconditions.CheckArgument(nArg >= -1);

            int rc = raw.sqlite3_create_function(db, name, nArg, null, (ctx, ud, args) =>
                {
                    IReadOnlyList<ISQLiteValue> iArgs = args.Select(value => SQLiteValue.Of(value)).ToList();

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
                            case SQLiteType.String:
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

    public sealed class SQLiteException : Exception
    {
        internal static void CheckOk(int rc)
        {
            string msg = "";

            if (SQLite3.Version.CompareTo(SQLiteVersion.Of(3007015)) >= 0)
            {
                msg = raw.sqlite3_errstr(rc);
            }

            if (raw.SQLITE_OK != rc)
                throw SQLiteException.Create(rc, rc, msg);
        }

        internal static void CheckOk(sqlite3 db, int rc)
        {
            int extended = raw.sqlite3_extended_errcode(db);
            if (raw.SQLITE_OK != rc)
                throw SQLiteException.Create(rc, extended, raw.sqlite3_errmsg(db));
        }

        internal static void CheckOk(sqlite3_stmt stmt, int rc)
        {
            CheckOk(raw.sqlite3_db_handle(stmt), rc);
        }

        internal static SQLiteException Create(int rc, int extended, string msg)
        {
            return Create((ErrorCode)rc, (ErrorCode)extended, msg);
        }

        internal static SQLiteException Create(ErrorCode rc, ErrorCode extended, string msg)
        {
            return new SQLiteException(rc, extended, msg);
        }

        private readonly ErrorCode errorCode;
        private readonly ErrorCode extendedErrorCode;
        private readonly string errmsg;

        private SQLiteException(ErrorCode errorCode, ErrorCode extendedErrorCode, string msg)
        {
            this.errorCode = errorCode;
            this.extendedErrorCode = extendedErrorCode;
            errmsg = msg;
        }

        public ErrorCode ErrorCode
        {
            get
            {
                return errorCode;
            }
        }

        public ErrorCode ExtendedErrorCode
        {
            get
            {
                return extendedErrorCode;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}\r\n{2}", errorCode, errmsg, base.ToString());
        }
    }
}
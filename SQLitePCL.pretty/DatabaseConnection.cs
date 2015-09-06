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
using System.IO;
using System.Linq;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Provides data for <see cref="SQLiteDatabaseConnection"/> <see cref="SQLiteDatabaseConnection.Profile"/> events.
    /// </summary>
    public sealed class DatabaseProfileEventArgs : EventArgs
    {
        internal static DatabaseProfileEventArgs Create(string statement, TimeSpan executionTime)
        {
            Contract.Requires(statement != null);
            return new DatabaseProfileEventArgs(statement, executionTime);
        }

        /// <summary>
        /// The SQL statement being profiled.
        /// </summary>
        public string Statement { get; }

        /// <summary>
        /// The execution time of the statement.
        /// </summary>
        public TimeSpan ExecutionTime { get; }

        private DatabaseProfileEventArgs(string statement, TimeSpan executionTime)
        {
            this.Statement = statement;
            this.ExecutionTime = executionTime;
        }
    }

    /// <summary>
    /// Provides data for <see cref="SQLiteDatabaseConnection"/> <see cref="SQLiteDatabaseConnection.Trace"/> events.
    /// </summary>
    public sealed class DatabaseTraceEventArgs : EventArgs
    {
        internal static DatabaseTraceEventArgs Create(string statement)
        {
            Contract.Requires(statement != null);
            return new DatabaseTraceEventArgs(statement);
        }

        /// <summary>
        /// The SQL statement text as the statement first begins executing which caused the trace event.
        /// </summary>
        public string Statement { get; }

        private DatabaseTraceEventArgs(string statement)
        {
            this.Statement = statement;
        }
    }

    /// <summary>
    /// Provides data for <see cref="SQLiteDatabaseConnection"/> <see cref="SQLiteDatabaseConnection.Update"/> events.
    /// </summary>
    public sealed class DatabaseUpdateEventArgs : EventArgs
    {
        internal static DatabaseUpdateEventArgs Create(ActionCode action, String database, string table, long rowId)
        {
            Contract.Requires(database != null);
            Contract.Requires(table != null);

            return new DatabaseUpdateEventArgs(action, database, table, rowId);
        }

        /// <summary>
        /// The SQL operation that caused the update event.
        /// </summary>
        public ActionCode Action { get; }

        /// <summary>
        /// The database containing the affected row.
        /// </summary>
        public string Database { get; }

        /// <summary>
        /// The table name containing the affected row.
        /// </summary>
        public string Table { get; }

        /// <summary>
        /// The rowid of the row updated.
        /// </summary>
        public long RowId { get; }

        private DatabaseUpdateEventArgs(ActionCode action, String database, string table, long rowId)
        {
            this.Action = action;
            this.Database = database;
            this.Table = table;
            this.RowId = rowId;
        }
    }

    /// <summary>
    /// Extensions methods for <see cref="IDatabaseConnection"/>
    /// </summary>
    public static partial class DatabaseConnection
    {
        /// <summary>
        /// Checkpoint the database name <paramref name="dbName"/>.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="dbName">The name of the database.</param>
        /// <seealso href="https://www.sqlite.org/c3ref/wal_checkpoint.html"/>
        public static void WalCheckPoint(this IDatabaseConnection This, string dbName)
        {
            Contract.Requires(This != null);

            int nLog;
            int nCkpt;

            This.WalCheckPoint(dbName, WalCheckPointMode.Passive, out nLog, out nCkpt);
        }

        /*
        /// <summary>
        /// Runs a checkpoint on all databases on the connection.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <seealso href="https://www.sqlite.org/c3ref/wal_checkpoint.html"/>
        public static void WalCheckPointAll(this IDatabaseConnection This)
        {
            Contract.Requires(This != null);

            int nLog;
            int nCkpt;

            // If parameter zDb is NULL or points to a zero length string, then the specified operation is 
            // attempted on all WAL databases attached to database connection db
            This.WalCheckPoint("", WalCheckPointMode.Passive, out nLog, out nCkpt);
        }*/

        /// <summary>
        /// Compiles and executes a SQL statement.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="sql">The SQL statement to compile and execute.</param>
        public static void Execute(this IDatabaseConnection This, string sql)
        {
            Contract.Requires(This != null);

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
        public static void ExecuteAll(this IDatabaseConnection This, string sql)
        {
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

            return new DelegatingEnumerable<IReadOnlyList<IResultSetValue>>(() =>
                {
                    return This.PrepareStatement(sql);
                });
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
                string tail;
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

            string tail;
            IStatement retval = This.PrepareStatement(sql, out tail);
            if (tail != null)
            {
                throw new ArgumentException("SQL contains more than one statment");
            }
            return retval;
        }



        /// <summary>
        /// Returns the filename associated with the database.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="database">The database name. The main database file has the name "main".</param>
        /// <returns>The attached database's filename.</returns>
        /// <exception cref="InvalidOperationException">If the database is not attached, temporary, or in memory.</exception>
        public static string GetFileName(this SQLiteDatabaseConnection This, string database)
        {
            Contract.Requires(This != null);

            string filename;

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

        /*
        /// <summary>
        ///  Returns metadata about a specific column of a specific database table,
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="columnInfo">The ColumnInfo of the column whose metadata should be retrieved.</param>
        /// <returns>The metadata of the column specified by columnInfo.</returns>
        public static TableColumnMetadata GetTableColumnMetadata(this IDatabaseConnection This, ColumnInfo columnInfo)
        {
            Contract.Requires(This != null);
            Contract.Requires(columnInfo != null);

            return This.GetTableColumnMetadata(columnInfo.DatabaseName, columnInfo.TableName, columnInfo.OriginName);
        }*/

        /// <summary>
        /// Executes the SQLite VACUUM command
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <seealso href="https://www.sqlite.org/lang_vacuum.html"/>
        public static void Vacuum(this IDatabaseConnection This) =>
            This.Execute(SQLBuilder.Vacuum);
    }
        
    internal class DelegatingDatabaseConnection : IDatabaseConnection, IDisposable
    {
        private readonly IDatabaseConnection db;

        private bool disposed = false;

        internal DelegatingDatabaseConnection(IDatabaseConnection db)
        {
            this.db = db;
        }

        public bool IsAutoCommit
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return db.IsAutoCommit;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return db.IsReadOnly;
            }
        }

        public int Changes
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return db.Changes;
            }
        }

        public int TotalChanges
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return db.TotalChanges;
            }
        }

        public long LastInsertedRowId
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return db.LastInsertedRowId;
            }
        }

        public void WalCheckPoint(string dbName, WalCheckPointMode mode, out int nLog, out int nCkpt)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
            db.WalCheckPoint(dbName, mode, out nLog, out nCkpt);
        }
            
        public void Interrupt()
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
            db.Interrupt();
        } 

        public bool IsDatabaseReadOnly(string dbName)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
            return db.IsDatabaseReadOnly(dbName);
        }

        public TableColumnMetadata GetTableColumnMetadata(string dbName, string tableName, string columnName)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
            return db.GetTableColumnMetadata(dbName, tableName, columnName);
        }

        public Stream OpenBlob(string database, string tableName, string columnName, long rowId, bool canWrite)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
            return db.OpenBlob(database, tableName, columnName, rowId, canWrite);
        }

        public IStatement PrepareStatement(string sql, out string tail)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return db.PrepareStatement(sql, out tail);
        }

        public void Status(DatabaseConnectionStatusCode statusCode, out int current, out int highwater, bool reset)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
            this.db.Status(statusCode, out current, out highwater, reset);
        }

        public void Dispose()
        {
            // Guard against someone taking a reference to this and trying to use it outside of
            // the Use function delegate
            disposed = true;
            // We don't actually own the database connection so its not disposed
        }
    }
}

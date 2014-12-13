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

namespace SQLitePCL.pretty
{
    /// <summary>
    /// A connection to a SQLite database.
    /// </summary>
    /// <seealso href="https://sqlite.org/c3ref/sqlite3.html"/>
    [ContractClass(typeof(IDatabaseConnectionContract))]
    public interface IDatabaseConnection : IDisposable
    {
        /// <summary>
        /// Occurs whenever a transaction is rolled back on the database connection.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/commit_hook.html"/>
        event EventHandler Rollback;

        /// <summary>
        /// Tracing event that occurs at various times when <see cref="IStatement"/>is running.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/profile.html"/>
        event EventHandler<DatabaseTraceEventArgs> Trace;

        /// <summary>
        /// Profiling event that occurs when a <see cref="IStatement"/> finishes.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/profile.html"/>
        event EventHandler<DatabaseProfileEventArgs> Profile;

        /// <summary>
        /// Occurs whenever a row is updated, inserted or deleted in a rowid table.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/update_hook.html"/>
        event EventHandler<DatabaseUpdateEventArgs> Update;

        /// <summary>
        /// Returns true if the given database connection is in autocommit mode,
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/get_autocommit.html"/>
        bool IsAutoCommit { get; }

        /// <summary>
        /// Returns the number of database rows that were changed, inserted
        /// or deleted by the most recently completed <see cref="IStatement"/>.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/changes.html"/>
        int Changes { get; }

        /// <summary>
        /// Returns the number of row changes caused by INSERT,
        /// UPDATE or DELETE statements since the database connection was opened.
        /// </summary>
        /// <seealso href="https://www.sqlite.org/c3ref/total_changes.html"/>
        int TotalChanges { get; }

        /// <summary>
        /// Returns the rowid of the most recent successful INSERT into a rowid or virtual table.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/last_insert_rowid.html"/>
        long LastInsertedRowId { get; }

        /// <summary>
        /// An enumeration of the connection's currently opened statements in the order they were prepared.
        /// </summary>
        IEnumerable<IStatement> Statements { get; }

        /// <summary>
        /// Returns metadata about a specific column of a specific database table,
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="columnName">The column name.</param>
        /// <returns>The column metadata.</returns>
        /// <seealso href="https://www.sqlite.org/c3ref/table_column_metadata.html"/>
        TableColumnMetadata GetTableColumnMetadata(string dbName, string tableName, string columnName);

        /// <summary>
        /// Determine whether a database is readonly.
        /// </summary>
        /// <seealso href="https://www.sqlite.org/c3ref/db_readonly.html"/>
        /// <param name="dbName">The database name.</param>
        /// <returns><see langword="true"/> if the database is readonly, otherwise <see langword="false"/>.</returns>
        bool IsReadOnly(string dbName);

        /// <summary>
        /// Run a checkpoint operation on a WAL database on the connection. The specific operation
        /// is determined by the value of the <paramref name="mode"/> parameter.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="mode">The checkpoint mode to use.</param>
        /// <param name="nLog">Returns the total number of frames in the log file before returning.</param>
        /// <param name="nCkpt">Return the total number of checkpointed frames.</param>
        void WalCheckPoint(string dbName, WalCheckPointMode mode, out int nLog, out int nCkpt);

        /// <summary>
        /// Opens the blob located by the a database, table, column, and rowid for incremental I/O as a <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/blob_open.html"/>
        /// <param name="database">The database containing the blob.</param>
        /// <param name="tableName">The table containing the blob.</param>
        /// <param name="columnName">The column containing the blob.</param>
        /// <param name="rowId">The row containing the blob.</param>
        /// <param name="canWrite">
        ///     <see langwords="true"/> if the Stream should be open for both read and write operations.
        ///     <see langwords="false"/> if the Stream should be open oly for read operations.
        /// </param>
        /// <returns>A <see cref="System.IO.Stream"/> that can be used to synchronously write and read to and from blob.</returns>
        Stream OpenBlob(string database, string tableName, string columnName, long rowId, bool canWrite);

        /// <summary>
        /// Compiles a SQL statement into an <see cref="IStatement"/>.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/prepare.html"/>
        /// <param name="sql">The statement to compiled.</param>
        /// <param name="tail">Additional text beyond past the end of the first SQL statement.</param>
        /// <returns>The compiled <see cref="IStatement"/></returns>
        IStatement PrepareStatement(string sql, out string tail);
    }

    /// <summary>
    /// Represents a single SQL statement.
    /// </summary>
    /// <seealso href="https://sqlite.org/c3ref/stmt.html"/>
    [ContractClass(typeof(IStatementContract))]
    public interface IStatement : IEnumerator<IReadOnlyList<IResultSetValue>>
    {
        /// <summary>
        /// An <see cref="IReadOnlyOrderedDictionary&lt;TKey, TValue&gt;"/> of the statement's bind parameters
        /// keyed by the parameter name. Note when accessing by index the first parameter
        /// is zero-based indexed unlike in the native SQLite APIs that are one-based indexed.
        /// </summary>
        IReadOnlyOrderedDictionary<string, IBindParameter> BindParameters { get; }

        /// <summary>
        /// An <see cref="IReadOnlyList&lt;T&gt;"/> of the columns in the statement's resultset.
        /// </summary>
        IReadOnlyList<ColumnInfo> Columns { get; }

        /// <summary>
        /// The text string used to prepare the statement.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/sql.html"/>
        string SQL { get; }

        /// <summary>
        /// <see langword="true"/> if the statement is readonly, otherwise <see langword="false"/>.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/stmt_readonly.html"/>
        bool IsReadOnly { get; }

        /// <summary>
        /// <see langwords="true"/> if the statement is busy, otherwise <see langwords="false"/>.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/stmt_busy.html"/>
        bool IsBusy { get; }

        /// <summary>
        /// Resets this statements bindings to <see cref="SQLiteValue.Null"/>.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/clear_bindings.html"/>
        void ClearBindings();
    }

    /// <summary>
    /// Represents an indexed collection of key/value pairs.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the read-only dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the read-only dictionary.</typeparam>
    public interface IReadOnlyOrderedDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than 0
        /// -or-
        /// <paramref name="index"/> is equal to or greater than <see cref="IReadOnlyCollection&lt;T&gt;.Count"/>.
        /// </exception>
        TValue this[int index] { get; }
    }

    /// <summary>
    /// An indexed bind parameter in a <see cref="IStatement"/>.
    /// </summary>
    [ContractClass(typeof(IBindParameterContract))]
    public interface IBindParameter
    {
        /// <summary>
        /// The bind paramter name.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/bind_parameter_name.html"/>
        string Name { get; }

        /// <summary>
        /// Binds the parameter to a byte array.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// <param name="blob">The byte array to bind.</param>
        void Bind(byte[] blob);

        /// <summary>
        /// Binds the parameter to a double.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// <param name="val">The double to bind.</param>
        void Bind(double val);

        /// <summary>
        /// Binds the parameter to an int.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// <param name="val">The int to bind.</param>
        void Bind(int val);

        /// <summary>
        /// Binds the parameter to a long.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// <param name="val">The long to bind.</param>
        void Bind(long val);

        /// <summary>
        /// Binds the parameter to a string.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// <param name="text">The text to bind.</param>
        void Bind(string text);

        /// <summary>
        /// Binds the parameter to null.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/bind_blob.html"/>
        void BindNull();

        /// <summary>
        /// Binds the parameter to a blob of length N that is filled with zeroes.
        /// Zeroblobs are intended to serve as placeholders for BLOBs whose
        /// content is later written using <see cref="IDatabaseConnection.OpenBlob"/>.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// <param name="size">The length of the blob in bytes.</param>
        void BindZeroBlob(int size);
    }

    /// <summary>
    /// SQLite dynamically type value.
    /// </summary>
    /// <seealso href="https://sqlite.org/c3ref/value.html"/>
    public interface ISQLiteValue
    {
        /// <summary>
        /// The underlying <see cref="SQLiteType"/>  of the value.
        /// </summary>
        SQLiteType SQLiteType { get; }

        /// <summary>
        /// The length of the value subject to SQLite value casting rules.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/column_blob.html"/>
        int Length { get; }

        /// <summary>
        /// Returns the SQLiteValue as a byte array. Casting if necessary.
        /// </summary>
        byte[] ToBlob();

        /// <summary>
        /// Returns the SQLiteValue as a double. Casting if necessary.
        /// </summary>
        double ToDouble();

        /// <summary>
        /// Returns the SQLiteValue as an int. Casting if necessary.
        /// </summary>
        int ToInt();

        /// <summary>
        /// Returns the SQLiteValue as a long. Casting if necessary.
        /// </summary>
        long ToInt64();

        /// <summary>
        /// Returns the SQLiteValue as a string. Casting if necessary.
        /// </summary>
        string ToString();
    }

    /// <summary>
    /// An <see cref="ISQLiteValue"/> that includes <see cref="ColumnInfo"/> about the value.
    /// </summary>
    public interface IResultSetValue : ISQLiteValue
    {
        /// <summary>
        /// The value's column info.
        /// </summary>
        ColumnInfo ColumnInfo { get; }
    }

    /// <summary>
    /// Interface to the SQLite backup api.
    /// </summary>
    /// <seealso href="https://sqlite.org/c3ref/backup.html"/>
    public interface IDatabaseBackup : IDisposable
    {
        /// <summary>
        /// The total number of pages in the source database file.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/backup_finish.html#sqlite3backuppagecount"/>
        int PageCount { get; }

        /// <summary>
        /// The number of pages still to be backed up.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/backup_finish.html#sqlite3backupremaining"/>
        int RemainingPages { get; }

        /// <summary>
        /// Copies up to nPage  between the source and destination databases.
        /// If nPages is negative, all remaining source pages are copied.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/backup_finish.html#sqlite3backupstep"/>
        /// <param name="nPages"></param>
        /// <returns></returns>
        bool Step(int nPages);
    }
}

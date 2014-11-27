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
        /// Sets the connection busy timeout when waiting for a locked table.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/busy_timeout.html"/>
        TimeSpan BusyTimeout { set; }

        /// <summary>
        /// Returns the number of database rows that were changed, inserted 
        /// or deleted by the most recently completed <see cref="IStatement"/>.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/changes.html"/>
        int Changes { get; }

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
        /// Returns the filename associated with the database if available.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/db_filename.html"/>
        /// <param name="database">The database name. The main database file has the name "main".</param>
        /// <param name="filename">When this method returns, contains the filename if there is an
        /// attached database that is not temporary or in memory. Otherwise null. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the database has a filename, otherwise <see langword="false"/>.
        /// </returns>
        bool TryGetFileName(string database, out string filename);

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

        /// <summary>
        /// Add or modify a collation function to the connection.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/create_collation.html"/>
        /// <param name="name">The function name.</param>
        /// <param name="comparison">A string comparison function.</param>
        void RegisterCollation(string name, Comparison<string> comparison);

        /// <summary>
        /// A callback function to be invoked whenever a transaction is committed.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/commit_hook.html"/>
        /// <param name="onCommit">A function that returns <see langwords="true"/> 
        /// if the commit should be rolled back, otherwise <see langwords="false"/></param>
        void RegisterCommitHook(Func<bool> onCommit);

        /// <summary>
        /// Registers an aggregate function.
        /// </summary>
        /// <see href="https://sqlite.org/c3ref/create_function.html"/>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="nArg">The number of <see cref="ISQLiteValue"/> instances the function takes or -1 if it may take any number of arguments.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        void RegisterAggregateFunc<T>(string name, int nArg, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector);

        /// <summary>
        /// Registers a scalar function.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/create_function.html"/>
        /// <param name="name">The function name.</param>
        /// <param name="nArg">The number of arguments the function takes or -1 if it may take any number of arguments.</param>
        /// <param name="reduce">A reduction function.</param>
        void RegisterScalarFunc(string name, int nArg, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce);
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
    /// An <see cref="ISQLiteValue"/> that includes <see cref="IColumnInfo"/> about the value.
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

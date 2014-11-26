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
    /// <see href="https://sqlite.org/c3ref/sqlite3.html"/>
    /// </summary>
    [ContractClass(typeof(IDatabaseConnectionContract))]
    public interface IDatabaseConnection : IDisposable
    {
        /// <summary>
        /// Occurs whenever a transaction is rolled back on the database connection.
        /// <see href="https://sqlite.org/c3ref/commit_hook.html"/>
        /// </summary>
        event EventHandler Rollback;

        /// <summary>
        /// Tracing event that occurs at various times when <see cref="IStatement"/>is running.
        /// <see href="https://sqlite.org/c3ref/profile.html"/>
        /// </summary>
        event EventHandler<DatabaseTraceEventArgs> Trace;

        /// <summary>
        /// Profiling event that occurs when a <see cref="IStatement"/> finishes.
        /// <see href="https://sqlite.org/c3ref/profile.html"/>
        /// </summary>
        event EventHandler<DatabaseProfileEventArgs> Profile;

        /// <summary>
        /// Occurs whenever a row is updated, inserted or deleted in a rowid table. 
        /// <see href="https://sqlite.org/c3ref/update_hook.html"/>
        /// </summary>
        event EventHandler<DatabaseUpdateEventArgs> Update;

        /// <summary>
        /// Returns true if the given database connection is in autocommit mode,
        /// <see href="https://sqlite.org/c3ref/get_autocommit.html"/>
        /// </summary>
        bool IsAutoCommit { get; }

        /// <summary>
        /// Sets the connection busy timeout when waiting for a locked table.
        /// <see href="https://sqlite.org/c3ref/busy_timeout.html"/>
        /// </summary>
        TimeSpan BusyTimeout { set; }

        /// <summary>
        /// Returns the number of database rows that were changed, inserted 
        /// or deleted by the most recently completed <see cref="IStatement"/>.
        /// <see href="https://sqlite.org/c3ref/changes.html"/>
        /// </summary>
        int Changes { get; }

        /// <summary>
        /// Returns the rowid of the most recent successful INSERT into a rowid or virtual table.
        /// <see href="https://sqlite.org/c3ref/last_insert_rowid.html"/>
        /// </summary>
        long LastInsertedRowId { get; }

        /// <summary>
        /// An enumeration of the connection's currently opened statements in the order they were prepared.
        /// </summary>
        IEnumerable<IStatement> Statements { get; }

        /// <summary>
        /// Returns the filename associated with the database if available.
        /// <see href="https://sqlite.org/c3ref/db_filename.html"/>
        /// </summary>
        /// <param name="database">The database name. The main database file has the name "main".</param>
        /// <param name="filename">When this method returns, contains the filename if there is an
        /// attached database that is not temporary or in memory. Otherwise null. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the database has a filename, otherwise <see langword="false"/>.
        /// </returns>
        bool TryGetFileName(string database, out string filename);

        /// <summary>
        /// Opens the blob located by the a database, table, column, and rowid for incremental I/O as a <see cref="System.IO.Stream"/>.
        /// <see href="https://sqlite.org/c3ref/blob_open.html"/>
        /// </summary>
        /// <param name="database"></param>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="rowId"></param>
        /// <param name="canWrite">
        ///     <see langwords="true"/> if the Stream should be open for both read and write operations. 
        ///     <see langwords="false"/> if the Stream should be open oly for read operations. 
        /// </param>
        /// <returns>A <see cref="System.IO.Stream"/> that can be used to synchronously write and read to and from blob.</returns>
        Stream OpenBlob(string database, string tableName, string columnName, long rowId, bool canWrite);

        /// <summary>
        /// Compiles a SQL statement into an <see cref="IStatement"/>.
        /// <see href="https://sqlite.org/c3ref/prepare.html"/>
        /// </summary>
        /// <param name="sql">The statement to compiled.</param>
        /// <param name="tail">Additional text beyond past the end of the first SQL statement.</param>
        /// <returns>The compiled <see cref="IStatement"/></returns>
        IStatement PrepareStatement(string sql, out string tail);

        /// <summary>
        /// Add or modify a collation function to the connection.
        /// <see href="https://sqlite.org/c3ref/create_collation.html"/>
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="comparison">A string comparison function.</param>
        void RegisterCollation(string name, Comparison<string> comparison);

        /// <summary>
        /// A callback function to be invoked whenever a transaction is committed.
        /// <see href="https://sqlite.org/c3ref/commit_hook.html"/>
        /// </summary>
        /// <param name="onCommit">A function that returns <see langwords="true"/> 
        /// if the commit should be rolled back, otherwise <see langwords="false"/></param>
        void RegisterCommitHook(Func<bool> onCommit);

        /// <summary>
        /// Registers an aggregate function.
        /// <see href="https://sqlite.org/c3ref/create_function.html"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="nArg">The number of arguments the function takes or -1 if it may take any number of arguments.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        void RegisterAggregateFunc<T>(string name, int nArg, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector);

        /// <summary>
        /// Registers a scalar function.
        /// <see href="https://sqlite.org/c3ref/create_function.html"/>
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="nArg">The number of arguments the function takes or -1 if it may take any number of arguments.</param>
        /// <param name="reduce">A reduction function.</param>
        void RegisterScalarFunc(string name, int nArg, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce);
    }

    /// <summary>
    /// Represents a single SQL statement.
    /// <see href="https://sqlite.org/c3ref/stmt.html"/>
    /// </summary>
    [ContractClass(typeof(IStatementContract))]
    public interface IStatement : IEnumerator<IReadOnlyList<IResultSetValue>>
    {
        /// <summary>
        /// An <see cref="IReadonlyOrderedDictionary"/> of the statement's bind parameters
        /// keyed by the parameter name. Note when accessing by index the first parameter 
        /// is zero-based indexed unlike in the native SQLite APIs that are one-based indexed.
        /// </summary>
        IReadOnlyOrderedDictionary<string, IBindParameter> BindParameters { get; }

        /// <summary>
        /// An <see cref="IReadonlyList"/> of the columns in the statement's resultset.
        /// </summary>
        IReadOnlyList<IColumnInfo> Columns { get; }

        /// <summary>
        /// The text string used to prepare the statement.
        /// <see href="https://sqlite.org/c3ref/sql.html"/>
        /// </summary>
        string SQL { get; }

        /// <summary>
        /// <see langwords="true"/> if the statement is readonly, otherwise <see langwords="false"/>.
        /// <see href="https://sqlite.org/c3ref/stmt_readonly.html"/>
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// <see langwords="true"/> if the statement is busy, otherwise <see langwords="false"/>.
        /// <see href="https://sqlite.org/c3ref/stmt_busy.html"/>
        /// </summary>
        bool IsBusy { get; }
        
        /// <summary>
        /// Resets this statements bindings to <see cref="SQLiteValue.Null"/>.
        /// <see href="https://sqlite.org/c3ref/clear_bindings.html"/>
        /// </summary>
        void ClearBindings();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public interface IReadOnlyOrderedDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
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
        /// <see href="https://sqlite.org/c3ref/bind_parameter_name.html"/>
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Binds the parameter to a byte array.
        /// <see href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// </summary>
        /// <param name="blob">The byte array to bind.</param>
        void Bind(byte[] blob);

        /// <summary>
        /// Binds the parameter to a double.
        /// <see href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// </summary>
        /// <param name="val">The double to bind.</param>
        void Bind(double val);

        /// <summary>
        /// Binds the parameter to an int.
        /// <see href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// </summary>
        /// <param name="val">The int to bind.</param>
        void Bind(int val);

        /// <summary>
        /// Binds the parameter to a long.
        /// <see href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// </summary>
        /// <param name="val">The long to bind.</param>
        void Bind(long val);

        /// <summary>
        /// Binds the parameter to a string.
        /// <see href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// </summary>
        /// <param name="text">The text to bind.</param>
        void Bind(string text);

        /// <summary>
        /// Binds the parameter to null.
        /// <see href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// </summary>
        void BindNull();

        /// <summary>
        /// Binds the parameter to a blob of length N that is filled with zeroes.
        /// Zeroblobs are intended to serve as placeholders for BLOBs whose 
        /// content is later written using <see cref="IDatabaseConnection.OpenBlob"/>. 
        /// <see href="https://sqlite.org/c3ref/bind_blob.html"/>
        /// </summary>
        /// <param name="size">The length of the blob in bytes.</param>
        void BindZeroBlob(int size);
    }

    /// <summary>
    /// Represents information about a single column in <see cref="IStatement"/> result set.
    /// </summary>
    public interface IColumnInfo
    {
        /// <summary>
        /// The column name.
        /// <see href="https://sqlite.org/c3ref/column_name.html"/>
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The database that is the origin of this particular result column.
        /// <see href="https://sqlite.org/c3ref/column_database_name.html"/>
        /// </summary>
        string DatabaseName { get; }

        /// <summary>
        /// The column that is the origin of this particular result column.
        /// <see href="https://sqlite.org/c3ref/column_database_name.html"/>
        /// </summary>
        string OriginName { get; }

        /// <summary>
        ///  The table that is the origin of this particular result column.
        /// <see href="https://sqlite.org/c3ref/column_database_name.html"/>
        /// </summary>
        string TableName { get; }
    }

    /// <summary>
    /// SQLite dynamically type value.
    /// <see href="https://sqlite.org/c3ref/value.html"/>
    /// </summary>
    public interface ISQLiteValue
    {
        /// <summary>
        /// 
        /// </summary>
        SQLiteType SQLiteType { get; }

        /// <summary>
        /// 
        /// </summary>
        int Length { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        byte[] ToBlob();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        double ToDouble();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        int ToInt();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        long ToInt64();

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        string ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IResultSetValue : ISQLiteValue
    {
        /// <summary>
        /// 
        /// </summary>
        IColumnInfo ColumnInfo { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IDatabaseBackup : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        int PageCount { get; }

        /// <summary>
        /// 
        /// </summary>
        int RemainingPages { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nPages"></param>
        /// <returns></returns>
        bool Step(int nPages);
    }
}

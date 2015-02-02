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
using System.IO;

namespace SQLitePCL.pretty
{
    [ContractClassFor(typeof(IDatabaseConnection))]
    internal abstract class IDatabaseConnectionContract : IDatabaseConnection
    {
        public abstract event EventHandler Rollback;

        public abstract event EventHandler<DatabaseTraceEventArgs> Trace;

        public abstract event EventHandler<DatabaseProfileEventArgs> Profile;

        public abstract event EventHandler<DatabaseUpdateEventArgs> Update;

        public abstract bool IsAutoCommit { get; }

        public abstract bool IsReadOnly { get; }

        public abstract int Changes { get; }

        public abstract int TotalChanges { get; }

        public abstract long LastInsertedRowId { get; }

        public abstract IEnumerable<IStatement> Statements { get; }

        public void WalCheckPoint(string dbName, WalCheckPointMode mode, out int nLog, out int nCkpt)
        {
            Contract.Requires(dbName != null);
            nLog = default(int);
            nCkpt = default(int);
        }

        public bool IsDatabaseReadOnly(string dbName)
        {
            Contract.Requires(dbName != null);
            return default(bool);
        }

        public abstract void Dispose();

        public TableColumnMetadata GetTableColumnMetadata(string dbName, string tableName, string columnName)
        {
            Contract.Requires(dbName != null);
            Contract.Requires(tableName != null);
            Contract.Requires(columnName != null);

            return default(TableColumnMetadata);
        }

        public Stream OpenBlob(string database, string tableName, string columnName, long rowId, bool canWrite)
        {
            Contract.Requires(database != null);
            Contract.Requires(tableName != null);
            Contract.Requires(columnName != null);
            return default(Stream);
        }

        public IStatement PrepareStatement(string sql, out string tail)
        {
            Contract.Requires(sql != null);
            tail = default(string);
            return default(IStatement);
        }

        public abstract void Status(DatabaseConnectionStatusCode statusCode, out int current, out int highwater, bool reset);
    }

    [ContractClassFor(typeof(IStatement))]
    internal abstract class IStatementContract : IStatement
    {
        public abstract IReadOnlyOrderedDictionary<string, IBindParameter> BindParameters { get; }

        public abstract bool IsBusy { get; }

        public abstract bool IsReadOnly { get; }

        public abstract IReadOnlyList<ColumnInfo> Columns { get; }

        public abstract IReadOnlyList<IResultSetValue> Current { get; }

        object IEnumerator.Current
        {
            get
            {
                return this.Current;
            }
        }

        public abstract string SQL { get; }

        public abstract void ClearBindings();

        public abstract void Dispose();

        public abstract bool MoveNext();

        public abstract void Reset();

        public abstract int Status(StatementStatusCode statusCode, bool reset);
    }

    [ContractClassFor(typeof(IBindParameter))]
    internal abstract class IBindParameterContract : IBindParameter
    {
        public abstract string Name { get; }

        public void Bind(byte[] blob)
        {
            Contract.Requires(blob != null);
        }

        public void Bind(double val)
        {
        }

        public void Bind(int val)
        {
        }

        public void Bind(long val)
        {
        }

        public void Bind(string text)
        {
            Contract.Requires(text != null);
        }

        public void BindNull()
        {
        }

        public void BindZeroBlob(int size)
        {
            Contract.Requires(size >= 0);
        }
    }
}

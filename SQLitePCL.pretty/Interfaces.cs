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
    [ContractClass(typeof(IDatabaseConnectionContract))]
    public interface IDatabaseConnection : IDisposable
    {
        event EventHandler Rollback;

        event EventHandler<DatabaseTraceEventArgs> Trace;

        event EventHandler<DatabaseProfileEventArgs> Profile;

        event EventHandler<DatabaseUpdateEventArgs> Update;

        bool IsAutoCommit { get; }

        TimeSpan BusyTimeout { set; }

        int Changes { get; }

        long LastInsertedRowId { get; }

        IEnumerable<IStatement> Statements { get; }

        bool TryGetFileName(string database, out string filename);

        Stream OpenBlob(string database, string tableName, string columnName, long rowId, bool canWrite);

        IStatement PrepareStatement(string sql, out string tail);

        void RegisterCollation(string name, Comparison<string> comparison);

        void RegisterCommitHook(Func<bool> onCommit);

        void RegisterAggregateFunc<T>(string name, int nArg, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector);

        void RegisterScalarFunc(string name, int nArg, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce);
    }

    [ContractClass(typeof(IStatementContract))]
    public interface IStatement : IEnumerator<IReadOnlyList<IResultSetValue>>
    {
        int BindParameterCount { get; }

        string SQL { get; }

        bool ReadOnly { get; }

        bool Busy { get; }

        void Bind(int index, byte[] blob);

        void Bind(int index, double val);

        void Bind(int index, int val);

        void Bind(int index, long val);

        void Bind(int index, string text);

        void BindNull(int index);

        void BindZeroBlob(int index, int size);

        void ClearBindings();

        bool TryGetBindParameterIndex(string parameter, out int index);

        string GetBindParameterName(int index);
    }

    public interface ISQLiteValue
    {
        SQLiteType SQLiteType { get; }

        // The length of the value in bytes
        int Length { get; }

        byte[] ToBlob();

        double ToDouble();

        int ToInt();

        long ToInt64();

        string ToString();
    }

    public interface IResultSetValue : ISQLiteValue
    {
        string ColumnName { get; }

        string ColumnDatabaseName { get; }

        String ColumnOriginName { get; }

        string ColumnTableName { get; }
    }

    public interface IDatabaseBackup : IDisposable
    {
        int PageCount { get; }

        int RemainingPages { get; }

        bool Step(int nPages);
    }
}

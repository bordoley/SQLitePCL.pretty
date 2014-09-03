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

namespace SQLitePCL.pretty
{
    public interface IDatabaseConnection : IEnumerable<IStatement>, IDisposable
    {
        event Action Rollback;

        event Action<String> Trace;

        event Action<String, TimeSpan> Profile;

        event Action<ActionCode, string, string,long> Update;

        bool IsAutoCommit { get; }

        int BusyTimeout { set; }

        int Changes { get; }

        string GetFileName(string database);

        IStatement PrepareStatement(string sql, out string tail);

        void RegisterCollation(string name, Comparison<string> comparison);

        void RegisterCommitHook(Func<bool> onCommit);

        void RegisterFunction<T>(string name, int nArg, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector);
    
        void RegisterFunction(string name, int nArg, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce);
    }

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

        int GetBindParameterIndex(string parameter);

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

        Stream ToStream(bool canWrite);
    }

    public interface IDatabaseBackup : IDisposable
    {
        int PageCount { get; }

        int RemainingPages { get; }

        bool Step(int nPages);
    }
}
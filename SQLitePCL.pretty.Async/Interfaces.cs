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
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    public interface IAsyncDatabaseConnection : IDisposable
    {
        IObservable<DatabaseTraceEventArgs> Trace { get; }

        IObservable<DatabaseProfileEventArgs> Profile { get; }

        IObservable<DatabaseUpdateEventArgs> Update { get; }

        Task ExecuteAll(string sql);

        Task Execute(string sql, params object[] a);

        Task<String> GetFilenameAsync(string database);

        //Task<T> OpenBlobAsync<T>(string database, string tableName, string columnName, long rowId, Func<Stream, T> selector, bool canWrite = false);

        Task<Tuple<IASyncStatement,String>> PrepareStatementAsync(string sql);

        IObservable<T> QueryAndSelect<T>(string sql, Func<IReadOnlyList<IResultSetValue>, T> selector, params object[] a);
    }

    public interface IASyncStatement : IDisposable
    {
        Task<int> GetBindParameterCountAsync();

        Task<string> GetSQLAsync();

        Task<bool> IsReadOnlyAsync();

        Task BindAsync(int index, byte[] blob);

        Task BindAsync(int index, double val);

        Task BindAsync(int index, int val);

        Task BindAsync(int index, long val);

        Task BindAsync(int index, string text);

        Task BindNullAsync(int index);

        Task BindZeroBlobAsync(int index, int size);

        Task ClearBindingsAsync();

        Task BindAsync(params object[] a);

        Task<int> GetBindParameterIndexAsync(string parameter);

        Task<string> GetBindParameterNameAsync(int index);

        IObservable<T> Select<T>(Func<IReadOnlyList<IResultSetValue>, T> selector);
    }
}

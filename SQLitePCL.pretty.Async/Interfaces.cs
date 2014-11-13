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

        IObservable<T> Use<T>(Func<IDatabaseConnection, IObservable<T>> f);

        Task<T> Use<T>(Func<IDatabaseConnection, T> f);

        Task<Tuple<IAsyncStatement, string>> PrepareStatement(string sql);
    }

    public interface IAsyncStatement : IDisposable
    {
        IObservable<T> Use<T>(Func<IStatement, IObservable<T>> f);
        Task<T> Use<T>(Func<IStatement, T> f);
    }
}

/*
   Copyright 2014 David Bordoley

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
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    [ContractClassFor(typeof(IAsyncDatabaseConnection))]
    internal abstract class IAsyncDatabaseConnectionContract : IAsyncDatabaseConnection
    {
        public abstract void Dispose();

        public abstract Task DisposeAsync();

        public IObservable<T> Use<T>(Func<IDatabaseConnection, CancellationToken, IEnumerable<T>> f)
        {
            Contract.Requires(f != null);

            return default(IObservable<T>);
        }
    }

    [ContractClassFor(typeof(IAsyncStatement))]
    internal abstract class IAsyncStatementContract : IAsyncStatement
    {
        public abstract void Dispose();

        public IObservable<T> Use<T>(Func<IStatement, CancellationToken, IEnumerable<T>> f)
        {
            Contract.Requires(f != null);

            return default(IObservable<T>);
        }
    }
}

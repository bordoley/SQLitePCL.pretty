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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    public static class AsyncStatement
    {
        public static Task Reset(this IAsyncStatement This, CancellationToken cancellationToken)
        {
            return This.Use(stmt =>
                {
                    ((IEnumerator)stmt).Reset();
                    return Enumerable.Empty<Unit>();
                }).ToTask(cancellationToken);
        }

        public static Task Reset(this IAsyncStatement This)
        {
            return Reset(This, CancellationToken.None);
        }
    }

    internal class AsyncStatementImpl : IAsyncStatement
    {
        private readonly IStatement stmt;
        private readonly IAsyncDatabaseConnection conn;

        private volatile bool disposed = false;

        internal AsyncStatementImpl(IStatement stmt, IAsyncDatabaseConnection conn)
        {
            this.stmt = stmt;
            this.conn = conn;
        }

        public IObservable<T> Use<T>(Func<IStatement, IEnumerable<T>> f)
        {
            Contract.Requires(f != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return conn.Use(_ => f(this.stmt));
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            conn.Use( _  =>
                {
                    stmt.Dispose();
                }).Wait();
        }
    }
}

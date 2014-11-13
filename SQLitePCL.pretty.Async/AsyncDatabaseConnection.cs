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
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    public static class AsyncDatabaseConnection
    {
        public static IAsyncDatabaseConnection AsAsyncDatabaseConnection(this IDatabaseConnection This)
        {
            Contract.Requires(This != null);
            return new AsyncDatabaseConnectionImpl(This);
        }
    }

    internal sealed class AsyncDatabaseConnectionImpl : IAsyncDatabaseConnection
    {
        private readonly OperationsQueue queue = new OperationsQueue(TaskPoolScheduler.Default);

        private readonly IDatabaseConnection conn;
        private readonly IObservable<DatabaseTraceEventArgs> trace;
        private readonly IObservable<DatabaseProfileEventArgs> profile;
        private readonly IObservable<DatabaseUpdateEventArgs> update;

        private volatile bool disposed = false;

        internal AsyncDatabaseConnectionImpl(IDatabaseConnection conn)
        {
            this.conn = conn;
            this.trace = Observable.FromEventPattern<DatabaseTraceEventArgs>(conn, "Trace").Select(e => e.EventArgs);
            this.profile = Observable.FromEventPattern<DatabaseProfileEventArgs>(conn, "Profile").Select(e => e.EventArgs);
            this.update = Observable.FromEventPattern<DatabaseUpdateEventArgs>(conn, "Update").Select(e => e.EventArgs);
        }

        public IObservable<DatabaseTraceEventArgs> Trace
        {
            get 
            {
                return trace;
            }
        }

        public IObservable<DatabaseProfileEventArgs> Profile
        {
            get
            {
                return profile;
            }
        }

        public IObservable<DatabaseUpdateEventArgs> Update
        {
            get 
            {
                return update;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            this.disposed = true;
            queue.EnqueueOperation(() =>
            {
                this.conn.Dispose();
            }).Wait();
        }

        Task ExecuteAll(string sql)
        {
            return queue.EnqueueOperation(() =>
                {
                    this.conn.ExecuteAll(sql);
                });
        }

        Task Execute(string sql, params object[] a)
        {
            return queue.EnqueueOperation(() =>
                {
                    this.conn.Execute(sql, a);
                });
        }

        public Task<string> GetFilenameAsync(string database)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    return this.conn.GetFileName(database);
                });
        }

        public Task<Tuple<IASyncStatement, string>> PrepareStatement(string sql)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    string tail = null;
                    var stmt = this.conn.PrepareStatement(sql, out tail);
                    return Tuple.Create<IASyncStatement, string>(new AsyncStatementImpl(stmt, queue), tail);
                });
        }

        public IObservable<T> QueryAndSelect<T>(string sql, Func<IReadOnlyList<IResultSetValue>, T> selector, params object[] a)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueObservableOperation(() => 
                {
                    return this.conn.Query(sql,a).Select(selector).ToObservable();   
                });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    internal class AsyncStatementImpl : IASyncStatement
    {
        private readonly IStatement stmt;
        private readonly OperationsQueue queue;

        private volatile bool disposed = false;
        
        internal AsyncStatementImpl(IStatement stmt, OperationsQueue queue)
        {
            this.stmt = stmt;
            this.queue = queue;
        }

        public Task<int> GetBindParameterCountAsync()
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    return stmt.BindParameterCount;
                });
        }

        public Task<string> GetSQLAsync()
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    return stmt.SQL;
                });
        }

        public Task<bool> IsReadOnlyAsync()
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    // FIXME: Rename in IStatement
                    return stmt.ReadOnly;
                });
        }

        public Task BindAsync(int index, byte[] blob)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    stmt.Bind(index, blob);
                });
        }

        public Task BindAsync(int index, double val)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    stmt.Bind(index, val);
                });
        }

        public Task BindAsync(int index, int val)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    stmt.Bind(index, val);
                });
        }

        public Task BindAsync(int index, long val)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    stmt.Bind(index, val);
                });
        }

        public Task BindAsync(int index, string text)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    stmt.Bind(index, text);
                });
        }

        public Task BindNullAsync(int index)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    stmt.BindNull(index);
                });
        }

        public Task BindZeroBlobAsync(int index, int size)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    stmt.BindZeroBlob(index, size);
                });
        }

        public Task ClearBindingsAsync()
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    stmt.ClearBindings();
                });
        }

        public Task BindAsync(params object[] a)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    stmt.Bind(a);
                });
        }

        public Task<int> GetBindParameterIndexAsync(string parameter)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    // FIXME: Make this an extension method in Statement.cs
                    int index = -1;
                    if (stmt.TryGetBindParameterIndex(parameter, out index))
                    {
                        return index;
                    }

                    throw new InvalidOperationException("Invalid parameter: " + parameter);
                });
        }

        public Task<string> GetBindParameterNameAsync(int index)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() =>
                {
                    return stmt.GetBindParameterName(index);
                });
        }

        public IObservable<T> Select<T>(Func<IReadOnlyList<IResultSetValue>, T> selector)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueObservableOperation(() =>
                {
                    return new DelegatingEnumerable<IReadOnlyList<IResultSetValue>>(() => this.stmt)
                        .Select(selector)
                        .ToObservable();    
                });
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            queue.EnqueueOperation(() =>
                {
                    stmt.Dispose();
                }).Wait();
        }
    }
}

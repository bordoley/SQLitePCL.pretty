using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    public static class AsyncStatement
    {
        public static Task Use(this IAsyncStatement This, Action<IStatement> f)
        {
            Contract.Requires(This != null);
            Contract.Requires(f != null);

            return This.Use(conn =>
                {
                    f(conn);
                    return Unit.Default;
                });
        }

        public static Task<int> GetBindParameterCountAsync(this IAsyncStatement This)
        {
            Contract.Requires(This != null);

            return This.Use(stmt =>
                {
                    return stmt.BindParameterCount;
                });
        }

        public static Task<string> GetSQLAsync(this IAsyncStatement This)
        {
            Contract.Requires(This != null);

            return This.Use(stmt =>
                {
                    return stmt.SQL;
                });
        }

        public static Task<bool> IsReadOnlyAsync(this IAsyncStatement This)
        {
            Contract.Requires(This != null);

            return This.Use(stmt =>
                {
                    // FIXME: Rename in IStatement
                    return stmt.ReadOnly;
                });
        }

        public static Task BindAsync(this IAsyncStatement This, int index, byte[] blob)
        {
            Contract.Requires(This != null);

            return This.Use(stmt =>
                {
                    stmt.Bind(index, blob);
                });
        }

        public static Task BindAsync(this IAsyncStatement This, int index, double val)
        {
            Contract.Requires(This != null);

            return This.Use(stmt =>
                {
                    stmt.Bind(index, val);
                });
        }

        public static Task BindAsync(this IAsyncStatement This, int index, int val)
        {
            Contract.Requires(This != null);

            return This.Use(stmt =>
                {
                    stmt.Bind(index, val);
                });
        }

        public static Task BindAsync(this IAsyncStatement This, int index, long val)
        {
            Contract.Requires(This != null);

            return This.Use(stmt =>
                {
                    stmt.Bind(index, val);
                });
        }

        public static Task BindAsync(this IAsyncStatement This, int index, string text)
        {
            Contract.Requires(This != null);
            Contract.Requires(text != null);

            return This.Use(stmt =>
                {
                    stmt.Bind(index, text);
                });
        }

        public static Task BindNullAsync(this IAsyncStatement This, int index)
        {
            Contract.Requires(This != null);

            return This.Use(stmt =>
                {
                    stmt.BindNull(index);
                }); 
        }

        public static Task BindZeroBlobAsync(this IAsyncStatement This, int index, int size)
        {
            Contract.Requires(This != null);

            return This.Use(stmt =>
                {
                    stmt.BindZeroBlob(index, size);
                });
        }

        public static Task ClearBindingsAsync(this IAsyncStatement This)
        {
            Contract.Requires(This != null);

            return This.Use(stmt =>
                {
                    stmt.ClearBindings();
                });
        }

        public static Task BindAsync(this IAsyncStatement This, params object[] a)
        {
            Contract.Requires(This != null);
            Contract.Requires(a != null);

            return This.Use(stmt =>
                {
                    stmt.Bind(a);
                });
        }

        public static Task<int> GetBindParameterIndexAsync(this IAsyncStatement This, string parameter)
        {
            Contract.Requires(This != null);
            Contract.Requires(parameter != null);

            return This.Use(stmt =>
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

        public static Task<string> GetBindParameterNameAsync(this IAsyncStatement This, int index)
        {
            Contract.Requires(This != null);

            return This.Use(stmt =>
                {
                    return stmt.GetBindParameterName(index);
                });
        }

        public static IObservable<T> Select<T>(this IAsyncStatement This, Func<IReadOnlyList<IResultSetValue>, T> selector)
        {
            Contract.Requires(This != null);
            Contract.Requires(selector != null);

            return This.Use(stmt =>
                {
                    return new DelegatingEnumerable<IReadOnlyList<IResultSetValue>>(() => stmt)
                        .Select(selector)
                        .ToObservable();
                });
        }

        public static Task Reset(this IAsyncStatement This)
        {
            return This.Use(stmt =>
                {
                    stmt.Reset();
                });
        }
    }

    internal class AsyncStatementImpl : IAsyncStatement
    {
        private readonly IStatement stmt;
        private readonly OperationsQueue queue;

        private volatile bool disposed = false;
        
        internal AsyncStatementImpl(IStatement stmt, OperationsQueue queue)
        {
            this.stmt = stmt;
            this.queue = queue;
        }

        public IObservable<T> Use<T>(Func<IStatement, IObservable<T>> f)
        {
            Contract.Requires(f != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueObservableOperation(() => f(this.stmt));
        }

        public Task<T> Use<T>(Func<IStatement, T> f)
        {
            Contract.Requires(f != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return queue.EnqueueOperation(() => f(this.stmt));
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

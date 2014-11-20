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
        public static Task Use(
            this IAsyncStatement This,
            Action<IStatement> f,
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(f != null);

            return This.Use(conn =>
            {
                f(conn);
                return Enumerable.Empty<Unit>();
            }, cancellationToken);
        }

        public static Task Use(this IAsyncStatement This, Action<IStatement> f)
        {
            return Use(This, f, CancellationToken.None);
        }

        public static Task<T> Use<T>(
            this IAsyncStatement This,
            Func<IStatement, T> f,
            CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            Contract.Requires(f != null);

            return This.Use(conn => Enumerable.Repeat(f(conn), 1)).ToTask(cancellationToken);
        }

        public static Task<T> Use<T>(this IAsyncStatement This, Func<IStatement, T> f)
        {
            return Use(This, f, CancellationToken.None);
        }
    }

    internal class AsyncStatementImpl : IAsyncStatement
    {
        private readonly IStatement stmt;
        private readonly IAsyncDatabaseConnection conn;

        private bool disposed = false;

        internal AsyncStatementImpl(IStatement stmt, IAsyncDatabaseConnection conn)
        {
            this.stmt = stmt;
            this.conn = conn;
        }

        public IObservable<T> Use<T>(Func<IStatement, IEnumerable<T>> f)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return conn.Use(_ => f(new StatementWrapper(this.stmt)));
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

     internal sealed class StatementWrapper : IStatement
     {
         private readonly IStatement stmt;

         internal StatementWrapper(IStatement stmt)
         {
             this.stmt = stmt;
         }

         public IReadOnlyOrderedDictionary<string, IBindParameter> BindParameters
         {
             get 
             {
                 return stmt.BindParameters;
             }
         }

         public IReadOnlyList<IColumnInfo> Columns
         {
             get 
             {
                 return stmt.Columns;
             }
         }

         public string SQL
         {
             get 
             {
                 return stmt.SQL;
             }
         }

         public bool IsReadOnly
         {
             get 
             {
                 return stmt.IsReadOnly;
             }
         }

         public bool IsBusy
         {
             get 
             {
                 return stmt.IsBusy;
             }
         }

         public void ClearBindings()
         {
             stmt.ClearBindings();
         }

         public IReadOnlyList<IResultSetValue> Current
         {
             get 
             {
                 return stmt.Current;
             }
         }

         object IEnumerator.Current
         {
             get 
             {
                 return this.Current;
             }
         }

         public bool MoveNext()
         {
             return stmt.MoveNext();
         }

         public void Reset()
         {
             stmt.Reset();
         }

         public void Dispose()
         {
             // No-op
         }
     }
}

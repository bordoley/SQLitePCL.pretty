using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace SQLitePCL.pretty.Orm
{
    public static class TableMappedStatement
    { 
        public static void Bind<T>(this ITableMappedStatement<T> This, T obj)
        {
            This.Bind(This.Mapping, obj);
        }

        public static void Execute<T>(this ITableMappedStatement<T> This, T obj)
        {
            Contract.Requires(This != null);
            Contract.Requires(obj != null);

            This.Reset();
            This.ClearBindings();
            This.Bind(obj);
            This.MoveNext();
        }

        private static IEnumerator<T> Enumerate<T>(this ITableMappedStatement<T> This)
        {
            while (This.MoveNext())
            {
                yield return This.Current;
            }
        }

        public static IEnumerable<T> Query<T>(this ITableMappedStatement<T> This)
        {
            Contract.Requires(This != null);

            return new DelegatingEnumerable<T>(() => 
                {
                    This.Reset();

                    // Prevent the statement from being disposed when the enumerator is disposed
                    return This.Enumerate();
                });
        }

        public static IEnumerable<T> Query<T>(this ITableMappedStatement<T> This, T obj)
        {
            Contract.Requires(This != null);
            Contract.Requires(obj != null);

            return new DelegatingEnumerable<T>(() => 
                {
                    This.Reset();
                    This.ClearBindings();
                    This.Bind(obj);

                    // Prevent the statement from being disposed when the enumerator is disposed
                    return This.Enumerate();
                });
        }

        public static IEnumerable<T> Query<T>(
            this ITableMappedStatement<T> This, 
            params object[] values)
        {
            Contract.Requires(This != null);
            Contract.Requires(values != null);

            return new DelegatingEnumerable<T>(() => 
                {
                    This.Reset();
                    This.ClearBindings();
                    This.Bind(values);

                    // Prevent the statement from being disposed when the enumerator is disposed
                    return This.Enumerate();
                });
        }
    }

    internal sealed class TableMappedStatement<T> : ITableMappedStatement<T>
    {
        private readonly IStatement deleg;
        private readonly ITableMapping<T> mapping;

        internal TableMappedStatement(IStatement deleg, ITableMapping<T> mapping)
        {
            this.deleg = deleg;
            this.mapping = mapping;
        }

        public ITableMapping<T> Mapping { get { return mapping; } }

        public void ClearBindings()
        {
            deleg.ClearBindings();
        }

        public int Status(StatementStatusCode statusCode, bool reset)
        {
            return deleg.Status(statusCode, reset);
        }

        public IReadOnlyOrderedDictionary<string, IBindParameter> BindParameters
        {
            get
            {
                return deleg.BindParameters;
            }
        }

        public IReadOnlyList<ColumnInfo> Columns
        {
            get
            {
                return deleg.Columns;
            }
        }

        public string SQL
        {
            get
            {
                return deleg.SQL;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return deleg.IsReadOnly;
            }
        }

        public bool IsBusy
        {
            get
            {
                return deleg.IsBusy;
            }
        }

        public void Dispose()
        {
            deleg.Dispose();
        }

        public bool MoveNext()
        {
            return deleg.MoveNext();
        }

        public void Reset()
        {
            deleg.Reset();
        }

        object IEnumerator.Current
        {
            get
            {
                return deleg.Current;
            }
        }

        IReadOnlyList<IResultSetValue> IEnumerator<IReadOnlyList<IResultSetValue>>.Current
        {
            get
            {
                return deleg.Current;
            }
        }

        T ITableMappedStatement<T>.Current
        {
            get
            {
                return mapping.ToObject(((IEnumerator<IReadOnlyList<IResultSetValue>>) this).Current);
            }
        }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Linq;

namespace SQLitePCL.pretty.Orm
{
    /// <summary>
    /// Extension methods for <see cref="ITableMappedStatement&lt;T&gt;"/> instances.
    /// </summary>
    public static class TableMappedStatement
    {
        /// <summary>
        /// Binds the statement bind variables by name to the corresponding properties on the object <paramref name="obj"/>.
        /// </summary>
        /// <param name="This">The statement.</param>
        /// <param name="obj">The object to bind.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static void Bind<T>(this ITableMappedStatement<T> This, T obj)
        {
            Contract.Requires(This != null);
            Contract.Requires(obj != null);

            This.Bind(This.Mapping, obj);
        }

        /// <summary>
        /// Executes the <see cref="ITableMappedStatement&lt;T&gt;"/> with provided bind object.
        /// </summary>
        /// <remarks>Note that this method resets and clears the existing bindings, before
        /// binding the new values and executing the statement.</remarks>
        /// <param name="This">The statement.</param>
        /// <param name="obj">The object to bind.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static void Execute<T>(this ITableMappedStatement<T> This, T obj)
        {
            Contract.Requires(This != null);
            Contract.Requires(obj != null);

            This.Reset();
            This.ClearBindings();
            This.Bind(obj);
            This.MoveNext();
        }

        /// <summary>
        /// Queries the database using the provided <see cref="ITableMappedStatement&lt;T&gt;"/>.
        /// </summary>
        /// <param name="This">The statement.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        /// <returns>An <see cref="IEnumerable&lt;T&gt;"/> of objects of type <paramref name="T"/>.</returns>
        public static IEnumerable<T> Query<T>(this ITableMappedStatement<T> This)
        {
            Contract.Requires(This != null);
            return ((IStatement) This).Query().Select(This.Mapping.ToObject);
        }

        /// <summary>
        /// Queries the database using the provided <see cref="ITableMappedStatement&lt;T&gt;"/> and provided bind variables.
        /// </summary>
        /// <param name="This">The statement.</param>
        /// <param name="values">The position indexed values to bind.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static IEnumerable<T> Query<T>(
            this ITableMappedStatement<T> This, 
            params object[] values)
        {
            Contract.Requires(This != null);
            Contract.Requires(values != null);

            return ((IStatement) This).Query(values).Select(This.Mapping.ToObject);
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
    }
}


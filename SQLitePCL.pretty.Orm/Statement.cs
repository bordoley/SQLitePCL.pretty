using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reactive.Linq;
using System.Reflection;

using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.Orm
{
    /// <summary>
    /// Extension methods for <see cref="IStatement"/> instances.
    /// </summary>
    public static class Statement
    {
        /// <summary>
        /// Binds the statement bind variables by name to the corresponding properties on the object <paramref name="obj"/>.
        /// </summary>
        /// <param name="This">The statement.</param>
        /// <param name="obj">The object to bind.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static void BindProperties<T>(this IStatement This, T obj)
        {
            Contract.Requires(This != null);
            Contract.Requires(obj != null);

            var columns = TableMapping.Get<T>().Columns;
            foreach (var column in columns)
            {
                var key = ":" + column.Key;
                if (This.BindParameters.ContainsKey(key))
                {
                    var value = column.Value.Property.GetValue(obj);
                    This.BindParameters[key].Bind(value);
                }
            }
        }

        /// <summary>
        /// Executes the <see cref="IStatement"/> with provided bind object.
        /// </summary>
        /// <remarks>Note that this method resets and clears the existing bindings, before
        /// binding the new values and executing the statement.</remarks>
        /// <param name="This">The statement.</param>
        /// <param name="obj">The object to bind.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static void ExecuteWithProperties<T>(this IStatement This, T obj)
        {
            Contract.Requires(This != null);
            Contract.Requires(obj != null);

            This.Reset();
            This.ClearBindings();
            This.BindProperties(obj);
            This.MoveNext();
        }
    }
}


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
using System.Diagnostics.Contracts;
using System.IO;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Extension methods for instances of <see cref="IStatement"/>.
    /// </summary>
    public static class Statement
    {
        /// <summary>
        /// Binds the position indexed values in <paramref name="values"/> to the 
        /// corresponding bind parameters in <paramref name="This"/>.
        /// </summary>
        /// <remarks>
        /// Bind parameters may be <see langword="null"/>, any numeric type, or an instance of <see cref="string"/>,
        /// byte[], or <see cref="Stream"/>. 
        /// </remarks>
        /// <param name="This">The statement.</param>
        /// <param name="values">The position indexed values to bind.</param>
        /// <exception cref="ArgumentException">
        /// If the <see cref="Type"/> of the value is not supported 
        /// -or- 
        /// A non-readable stream is provided as a value.</exception>
        public static void Bind(this IStatement This, params object[] values)
        {
            Contract.Requires(This != null);
            Contract.Requires(values != null);
            Contract.Requires(This.BindParameters.Count == values.Length);

            var count = values.Length;
            for (int i = 0; i < count; i++)
            {
                This.BindParameters[i].Bind(values[i]);
            }
        }

        /*
        /// <summary>
        /// Bind the statement parameters to the key-value pairs in <paramref name="pairs"/>.
        /// </summary>
        /// <remarks>
        /// Bind parameters may be <see langword="null"/>, any numeric type, or an instance of <see cref="string"/>,
        /// byte[], or <see cref="Stream"/>. 
        /// </remarks>
        /// <param name="This">The statement.</param>
        /// <param name="pairs">An enumerable of keyvalue pairs keyed by bind parameter name.</param>
        internal static void Bind(this IStatement This, IEnumerable<KeyValuePair<string,object>> pairs)
        {
            Contract.Requires(This != null);
            Contract.Requires(pairs != null);

            foreach (var kvp in pairs)
            {
                This.BindParameters[kvp.Key].Bind(kvp.Value);
            }
        }*/

        /// <summary>
        /// Executes the <see cref="IStatement"/> with provided bind parameter values.
        /// </summary>
        /// <remarks>Note that this method resets and clears the existing bindings, before
        /// binding the new values and executing the statement.</remarks>
        /// <param name="This">The statements.</param>
        /// <param name="values">The position indexed values to bind.</param>
        public static void Execute(this IStatement This, params object[] values)
        {
            Contract.Requires(This != null);
            Contract.Requires(values != null);

            This.Reset();
            This.ClearBindings();
            This.Bind(values);
            This.MoveNext();
        }
    }
}

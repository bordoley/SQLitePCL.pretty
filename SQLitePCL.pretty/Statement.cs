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
        /// Binds the position indexed values in <paramref name="a"/> to the 
        /// corresponding bind parameters in <paramref name="stmt"/>.
        /// </summary>
        /// <remarks>
        /// Bind parameters may be <see langword="null"/>, any numeric type, or an instance of <see cref="string"/>,
        /// byte[], or <see cref="Stream"/>. 
        /// </remarks>
        /// <param name="stmt">The statement to bind the values to.</param>
        /// <param name="a">The position indexed values to bind.</param>
        /// <exception cref="ArgumentException">
        /// If the <see cref="Type"/> of the value is not supported 
        /// -or- 
        /// A non-readable stream is provided as a value.</exception>
        public static void Bind(this IStatement stmt, params object[] a)
        {
            Contract.Requires(stmt != null);
            Contract.Requires(a != null);
            Contract.Requires(stmt.BindParameters.Count == a.Length);

            var count = a.Length;
            for (int i = 0; i < count; i++)
            {
                stmt.BindParameters[i].Bind(a[i]);
            }
        }

        // FIXME: Make public
        internal static void Bind(this IStatement stmt, IEnumerable<KeyValuePair<string,object>> pairs)
        {
            Contract.Requires(stmt != null);
            Contract.Requires(pairs != null);

            foreach (var kvp in pairs)
            {
                stmt.BindParameters[kvp.Key].Bind(kvp.Value);
            }
        }

        // FIXME: Make public
        internal static void Execute(this IStatement stmt, params object[] a)
        {
            Contract.Requires(stmt != null);
            Contract.Requires(a != null);

            stmt.Reset();
            stmt.ClearBindings();
            stmt.Bind(a);
            stmt.MoveNext();
        }
    }
}

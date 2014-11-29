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
using System.Diagnostics.Contracts;
using System.IO;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Extension methods for instances of <see cref="IBindParameter"/>.
    /// </summary>
    public static class BindParameter
    {
        /// <summary>
        /// Bind the parameter to a value based upon its runtime type.
        /// </summary>
        /// <param name="This">The bind parameter.</param>
        /// <param name="obj">
        /// An object that is either <see langword="null"/> or any numeric type, <see cref="string"/>,
        /// byte[], or <see cref="Stream"/>. 
        /// </param>
        /// <exception cref="ArgumentException">
        /// If the <see cref="Type"/> of the value is not supported 
        /// -or- 
        /// A non-readable stream is provided as a value.
        /// </exception>
        public static void Bind(this IBindParameter This, object obj)
        {
            // I miss F# pattern matching
            if (obj == null)
            {
                This.BindNull();
                return;
            }

            Type t = obj.GetType();

            if (typeof(string) == t)
            {
                This.Bind((string)obj);
            }
            else if (
                (typeof(Int32) == t)
                || (typeof(Boolean) == t)
                || (typeof(Byte) == t)
                || (typeof(UInt16) == t)
                || (typeof(Int16) == t)
                || (typeof(sbyte) == t)
                || (typeof(Int64) == t)
                || (typeof(UInt32) == t))
            {
                This.Bind((long)(Convert.ChangeType(obj, typeof(long))));
            }
            else if (
                (typeof(double) == t)
                || (typeof(float) == t)
                || (typeof(decimal) == t))
            {
                This.Bind((double)(Convert.ChangeType(obj, typeof(double))));
            }
            else if (typeof(byte[]) == t)
            {
                This.Bind((byte[])obj);
            }
            else if (obj is Stream)
            {
                var stream = (Stream)obj;
                if (!stream.CanRead)
                {
                    throw new ArgumentException("Stream is not readable");
                }

                This.BindZeroBlob((int)stream.Length);
            }
            else
            {
                throw new ArgumentException("Invalid type conversion" + t);
            }
        }
    }
}

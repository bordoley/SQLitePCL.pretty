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
    // FIXME: Make public
    internal static class BindParameter
    {
        public static void Bind(this IBindParameter This, object a)
        {
            Contract.Requires(This != null);

            // I miss F# pattern matching
            if (a == null)
            {
                This.BindNull();
                return;
            }

            Type t = a.GetType();

            if (typeof(string) == t)
            {
                This.Bind((string)a);
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
                This.Bind((long)(Convert.ChangeType(a, typeof(long))));
            }
            else if (
                (typeof(double) == t)
                || (typeof(float) == t)
                || (typeof(decimal) == t))
            {
                This.Bind((double)(Convert.ChangeType(a, typeof(double))));
            }
            else if (typeof(byte[]) == t)
            {
                This.Bind((byte[])a);
            }
            else if (a is Stream)
            {
                var stream = (Stream)a;
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

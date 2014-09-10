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

namespace SQLitePCL.pretty
{
    public static class Statement
    {
        public static void Bind(this IStatement stmt, params object[] a)
        {
            Contract.Requires(stmt != null);
            Contract.Requires(a != null);
            Contract.Requires(stmt.BindParameterCount == a.Length);

            var count = a.Length;
            for (int i = 0; i < count; i++)
            {
                // I miss F# pattern matching
                if (a[i] == null)
                {
                    stmt.BindNull(i);
                }
                else
                {
                    Type t = a[i].GetType();

                    if (typeof(String) == t)
                    {
                        stmt.Bind(i, (string)a[i]);
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
                        stmt.Bind(i, (long)(Convert.ChangeType(a[i], typeof(long))));
                    }
                    else if (
                        (typeof(double) == t)
                        || (typeof(float) == t)
                        || (typeof(decimal) == t))
                    {
                        stmt.Bind(i, (double)(Convert.ChangeType(a[i], typeof(double))));
                    }
                    else if (typeof(byte[]) == t)
                    {
                        stmt.Bind(i, (byte[])a[i]);
                    }
                    else
                    {
                        throw new NotSupportedException("Invalid type conversion" + t);
                    }
                }
            }
        }
    }
}
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
using System.Linq;
using System.Reflection;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Extension methods for instances of <see cref="IBindParameter"/>.
    /// </summary>
    public static class BindParameter
    {
        /// <summary>
        /// Bind the parameter to an <see cref="bool"/>.
        /// </summary>
        /// <param name="This">The bind parameter.</param>
        /// <param name="value">A <see cref="bool"/>.</param>
        public static void Bind(this IBindParameter This, bool value)
        {
            Contract.Requires(This != null);
            This.Bind(Convert.ToInt64(value));
        }

        /// <summary>
        /// Bind the parameter to an <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="This">The bind parameter.</param>
        /// <param name="value">A <see cref="TimeSpan"/>.</param>
        public static void Bind(this IBindParameter This, TimeSpan value)
        {
            Contract.Requires(This != null);
            This.Bind(value.Ticks);
        }

        /// <summary>
        /// Bind the parameter to an <see cref="DateTime"/>.
        /// </summary>
        /// <param name="This">The bind parameter.</param>
        /// <param name="value">A <see cref="DateTime"/>.</param>
        public static void Bind(this IBindParameter This, DateTime value)
        {
            Contract.Requires(This != null);
            This.Bind(value.Ticks);
        }

        /// <summary>
        /// Bind the parameter to an <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="This">The bind parameter.</param>
        /// <param name="value">A <see cref="DateTimeOffset"/>.</param>
        public static void Bind(this IBindParameter This, DateTimeOffset value)
        {
            Contract.Requires(This != null);
            This.Bind(value.ToOffset(TimeSpan.Zero).Ticks);
        }

        /// <summary>
        /// Bind the parameter to an <see cref="decimal"/>.
        /// </summary>
        /// <param name="This">The bind parameter.</param>
        /// <param name="value">A <see cref="decimal"/>.</param>
        public static void Bind(this IBindParameter This, decimal value)
        {
            Contract.Requires(This != null);
            This.Bind(Convert.ToDouble(value));
        }            

        /// <summary>
        /// Bind the parameter to an <see cref="Guid"/>.
        /// </summary>
        /// <param name="This">The bind parameter.</param>
        /// <param name="value">A <see cref="Guid"/>.</param>
        public static void Bind(this IBindParameter This, Guid value)
        {
            Contract.Requires(This != null);
            This.Bind(value.ToString());
        }

        /// <summary>
        /// Bind the parameter to an <see cref="Stream"/>.
        /// </summary>
        /// <param name="This">The bind parameter.</param>
        /// <param name="value">A <see cref="Stream"/>.</param>
        public static void Bind(this IBindParameter This, Stream value)
        {
            Contract.Requires(This != null);
            Contract.Requires(value != null);

            if (!value.CanRead)
            {
                throw new ArgumentException("Stream is not readable");
            }

            // FIXME: Stream.Length is Int64, better to take max int
            This.BindZeroBlob((int) value.Length);
        }

        /// <summary>
        /// Bind the parameter to a <see cref="Uri"/>.
        /// </summary>
        /// <param name="This">The bind parameter.</param>
        /// <param name="value">A <see cref="Stream"/>.</param>
        public static void Bind(this IBindParameter This, Uri value)
        {
            Contract.Requires(This != null);
            Contract.Requires(value != null);

            This.Bind(value.ToString());
        }
                    
        /// <summary>
        /// Bind the parameter to a value based upon its runtime type.
        /// </summary>
        /// <param name="This">The bind parameter.</param>
        /// <param name="obj">
        /// An object that is either <see langword="null"/> or any numeric type, <see cref="string"/>,
        /// byte[], <see cref="ISQLiteValue"/> or <see cref="Stream"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If the <see cref="Type"/> of the value is not supported
        /// -or-
        /// A non-readable stream is provided as a value.
        /// </exception>
        public static void Bind(this IBindParameter This, object obj)
        {
            Contract.Requires(This != null);

            // I miss F# pattern matching
            if (obj == null)
            {
                This.BindNull();
                return;
            }
                
            Type t = obj.GetType();

            if (typeof(string) == t) { This.Bind((string)obj); }
            else if (
                (typeof(Int32) == t)
                || (typeof(Boolean) == t)
                || (typeof(Byte) == t)
                || (typeof(UInt16) == t)
                || (typeof(Int16) == t)
                || (typeof(sbyte) == t)
                || (typeof(Int64) == t)
                || (typeof(UInt32) == t))                                                     { This.Bind((long)(Convert.ChangeType(obj, typeof(long)))); }
            else if ((typeof(double) == t) || (typeof(float) == t) || (typeof(decimal) == t)) { This.Bind((double)(Convert.ChangeType(obj, typeof(double)))); }
            else if (typeof(byte[]) == t)                                                     { This.Bind((byte[]) obj); }
            else if (t.GetTypeInfo().ImplementedInterfaces.Contains(typeof(ISQLiteValue)))    { This.Bind((ISQLiteValue)obj); }
            else if (obj is TimeSpan)                                                         { This.Bind((TimeSpan)obj); }
            else if (obj is DateTime)                                                         { This.Bind((DateTime) obj); }
            else if (obj is DateTimeOffset)                                                   { This.Bind((DateTimeOffset) obj); }
            else if (obj is Guid)                                                             { This.Bind((Guid) obj); }
            else if (obj is Stream)                                                           { This.Bind((Stream) obj); }
            else if (obj is Uri)                                                              { This.Bind((Uri) obj); }
            else
            {
                throw new ArgumentException("Invalid type conversion" + t);
            }
        }

        /// <summary>
        /// Bind the parameter to an <see cref="ISQLiteValue"/>.
        /// </summary>
        /// <param name="This">The bind parameter.</param>
        /// <param name="value">A <see cref="ISQLiteValue"/>.</param>
        public static void Bind(this IBindParameter This, ISQLiteValue value)
        {
            Contract.Requires(This != null);
            Contract.Requires(value != null);

            switch (value.SQLiteType)
            {
                case SQLiteType.Blob:
                    if (value is ZeroBlob)
                    {
                        This.BindZeroBlob(value.Length);
                    }
                    else
                    {
                        This.Bind(value.ToBlob());
                    }
                    return;

                case SQLiteType.Null:
                    This.BindNull();
                    return;

                case SQLiteType.Text:
                    This.Bind(value.ToString());
                    return;

                case SQLiteType.Float:
                    This.Bind(value.ToDouble());
                    return;

                case SQLiteType.Integer:
                    This.Bind(value.ToInt64());
                    return;
            }
        }
    }
}

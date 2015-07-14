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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Extensions methods for <see cref="IStatement"/> result set rows.
    /// </summary>
    public static class ResultSet
    {
        /// <summary>
        /// Selects the value in the first column of the result set row.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<IResultSetValue> SelectScalar(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {   
            Contract.Requires(This != null);
            return This.Select(x => x.First());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="int"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<int> SelectScalarInt(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToInt());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="long"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<long> SelectScalarInt64(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToInt64());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="string"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<string> SelectScalarString(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToString());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="byte"/> array.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<byte[]> SelectScalarBlob(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToBlob());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="double"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<double> SelectScalarDouble(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToDouble());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="bool"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<bool> SelectScalarBool(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToBool());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="float"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<float> SelectScalarFloat(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToFloat());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="TimeSpan"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<TimeSpan> SelectScalarTimeSpan(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToTimeSpan());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="DateTime"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<DateTime> SelectScalarDateTime(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToDateTime());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<DateTimeOffset> SelectScalarDateTimeOffset(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToDateTimeOffset());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="uint"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<uint> SelectScalarUInt32(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToUInt32());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="decimal"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<decimal> SelectScalarDecimal(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToDecimal());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="byte"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<byte> SelectScalarByte(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToByte());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="UInt16"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<UInt16> SelectScalarUInt16(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToUInt16());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="short"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<short> SelectScalarShort(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToShort());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="sbyte"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<sbyte> SelectScalarSByte(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToSByte());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="Guid"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<Guid> SelectScalarGuid(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToGuid());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="Uri"/>.
        /// </summary>
        /// <returns>An IObservable of the scalar values.</returns>
        /// <param name="This">An observable of result set rows.</param>
        public static IObservable<Uri> SelectScalarUri(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            Contract.Requires(This != null);
            return This.SelectScalar().Select(x => x.ToUri());
        }
    }
}

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
using System.Diagnostics.Contracts;
using System.Reactive.Linq;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Extensions methods for <see cref="IStatement"/> result set rows.
    /// </summary>
    public static class ResultSet
    {
        public static IObservable<IResultSetValue> SelectScalar(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.Select(x => x[0]);
        }

        public static IObservable<int> SelectScalarInt(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToInt());
        }

        public static IObservable<long> SelectScalarInt64(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToInt64());
        }

        public static IObservable<string> SelectScalarString(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToString());
        }

        public static IObservable<byte[]> SelectScalarBlob(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToBlob());
        }

        public static IObservable<double> SelectScalarDouble(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToDouble());
        }

        public static IObservable<bool> SelectScalarBool(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToBool());
        }

        public static IObservable<float> SelectScalarFloat(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToFloat());
        }

        public static IObservable<TimeSpan> SelectScalarTimeSpan(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToTimeSpan());
        }

        public static IObservable<DateTime> SelectScalarDateTime(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToDateTime());
        }

        public static IObservable<DateTimeOffset> SelectScalarDateTimeOffset(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToDateTimeOffset());
        }

        public static IObservable<uint> SelectScalarUInt32(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToUInt32());
        }

        public static IObservable<decimal> SelectScalarDecimal(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToDecimal());
        }

        public static IObservable<byte> SelectScalarByte(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToByte());
        }

        public static IObservable<UInt16> SelectScalarUInt16(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToUInt16());
        }

        public static IObservable<short> SelectScalarShort(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToShort());
        }

        public static IObservable<sbyte> SelectScalarSByte(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToSByte());
        }

        public static IObservable<Guid> SelectScalarGuid(this IObservable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToGuid());
        }
    }
}

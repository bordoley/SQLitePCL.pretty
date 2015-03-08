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
using System.Linq;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Extensions methods for <see cref="IStatement"/> result set rows.
    /// </summary>
    public static class ResultSet
    {
        public static IEnumerable<IResultSetValue> SelectScalar(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.Select(x => x[0]);
        }

        public static IEnumerable<int> SelectScalarInt(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToInt());
        }

        public static IEnumerable<long> SelectScalarInt64(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToInt64());
        }

        public static IEnumerable<string> SelectScalarString(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToString());
        }

        public static IEnumerable<byte[]> SelectScalarBlob(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToBlob());
        }

        public static IEnumerable<double> SelectScalarDouble(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToDouble());
        }

        public static IEnumerable<bool> SelectScalarBool(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToBool());
        }

        public static IEnumerable<float> SelectScalarFloat(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToFloat());
        }

        public static IEnumerable<TimeSpan> SelectScalarTimeSpan(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToTimeSpan());
        }

        public static IEnumerable<DateTime> SelectScalarDateTime(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToDateTime());
        }

        public static IEnumerable<DateTimeOffset> SelectScalarDateTimeOffset(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToDateTimeOffset());
        }

        public static IEnumerable<uint> SelectScalarUInt32(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToUInt32());
        }

        public static IEnumerable<decimal> SelectScalarDecimal(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToDecimal());
        }

        public static IEnumerable<byte> SelectScalarByte(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToByte());
        }

        public static IEnumerable<UInt16> SelectScalarUInt16(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToUInt16());
        }

        public static IEnumerable<short> SelectScalarShort(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToShort());
        }

        public static IEnumerable<sbyte> SelectScalarSByte(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToSByte());
        }

        public static IEnumerable<Guid> SelectScalarGuid(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToGuid());
        }

        /// <summary>
        /// Returns an <see cref="IReadOnlyList&lt;IColumnInfo&gt;"/> of columns from a result set row.
        /// </summary>
        /// <param name="This">A row in the result set.</param>
        /// <returns>An <see cref="IReadOnlyList&lt;IColumnInfo&gt;"/> of the result set columns.</returns>
        public static IReadOnlyList<ColumnInfo> Columns(this IReadOnlyList<IResultSetValue> This)
        {
            Contract.Requires(This != null);

            return new ResultSetColumnsListImpl(This);
        }

        private sealed class ResultSetColumnsListImpl : IReadOnlyList<ColumnInfo>
        {
            private readonly IReadOnlyList<IResultSetValue> rs;

            internal ResultSetColumnsListImpl(IReadOnlyList<IResultSetValue> rs)
            {
                this.rs = rs;
            }

            public ColumnInfo this[int index]
            {
                get
                {
                    return rs[index].ColumnInfo;
                }
            }

            public int Count
            {
                get
                {
                    return rs.Count;
                }
            }

            public IEnumerator<ColumnInfo> GetEnumerator()
            {
                return rs.Select(val => val.ColumnInfo).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}

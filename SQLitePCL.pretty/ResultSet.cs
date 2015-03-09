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
        /// <summary>
        /// Selects the value in the first column of the result set row.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<IResultSetValue> SelectScalar(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.Select(x => x[0]);
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="int"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<int> SelectScalarInt(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToInt());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="long"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<long> SelectScalarInt64(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToInt64());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="string"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<string> SelectScalarString(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToString());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="byte"/> array.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<byte[]> SelectScalarBlob(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToBlob());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="double"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<double> SelectScalarDouble(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToDouble());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="bool"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<bool> SelectScalarBool(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToBool());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="float"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<float> SelectScalarFloat(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToFloat());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="TimeSpan"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<TimeSpan> SelectScalarTimeSpan(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToTimeSpan());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="DateTime"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<DateTime> SelectScalarDateTime(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToDateTime());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<DateTimeOffset> SelectScalarDateTimeOffset(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToDateTimeOffset());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="uint"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<uint> SelectScalarUInt32(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToUInt32());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="decimal"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<decimal> SelectScalarDecimal(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToDecimal());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="byte"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<byte> SelectScalarByte(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToByte());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="UInt16"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<UInt16> SelectScalarUInt16(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToUInt16());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="short"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<short> SelectScalarShort(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToShort());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="sbyte"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
        public static IEnumerable<sbyte> SelectScalarSByte(this IEnumerable<IReadOnlyList<IResultSetValue>> This)
        {
            return This.SelectScalar().Select(x => x.ToSByte());
        }

        /// <summary>
        /// Selects the value in the first column of the result set row as a <see cref="Guid"/>.
        /// </summary>
        /// <returns>An IEnumerable of the scalar values.</returns>
        /// <param name="This">This.</param>
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

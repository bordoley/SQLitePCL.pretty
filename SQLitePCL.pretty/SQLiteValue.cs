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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Extensions methods for creating instances of <see cref="ISQLiteValue"/>.
    /// </summary>
    public static class SQLiteValue
    {
        private static readonly ISQLiteValue _null = new NullValue();

        /// <summary>
        /// The SQLite null value.
        /// </summary>
        public static ISQLiteValue Null
        {
            get
            {
                return _null;
            }
        }

        /// <summary>
        /// Create a SQLite zeroblob of the specified length.
        /// </summary>
        /// <remarks>This method does not allocate any memory,
        /// therefore the zero blobs can safely be of arbitrary length.</remarks>
        /// <param name="length">The length of the zero blob</param>
        /// <returns>An ISQLiteValue representing the zero blob.</returns>
        public static ISQLiteValue ZeroBlob(int length)
        {
            Contract.Requires(length >= 0);
            return new ZeroBlob(length);
        }

        /// <summary>
        /// Converts an <see cref="int"/> to an <see cref="ISQLiteValue"/>.
        /// </summary>
        /// <param name="This">The value to convert</param>
        /// <returns>A ISQLiteValue representing the value.</returns>
        public static ISQLiteValue ToSQLiteValue(this int This)
        {
            return ToSQLiteValue((long)This);
        }

        public static ISQLiteValue ToSQLiteValue(this short This)
        {
            return ToSQLiteValue((long)This);
        }

        /// <summary>
        /// Converts a <see cref="bool"/> to an <see cref="ISQLiteValue"/>.
        /// </summary>
        /// <param name="This">The value to convert</param>
        /// <returns>A ISQLiteValue representing the value.</returns>
        public static ISQLiteValue ToSQLiteValue(this bool This)
        {
            return ToSQLiteValue((long)(Convert.ChangeType(This, typeof(long))));
        }

        /// <summary>
        /// Converts a <see cref="byte"/> to an <see cref="ISQLiteValue"/>.
        /// </summary>
        /// <param name="This">The value to convert</param>
        /// <returns>A ISQLiteValue representing the value.</returns>
        public static ISQLiteValue ToSQLiteValue(this byte This)
        {
            return ToSQLiteValue((long)(Convert.ChangeType(This, typeof(long))));
        }

        /// <summary>
        /// Converts a <see cref="char"/> to an <see cref="ISQLiteValue"/>.
        /// </summary>
        /// <param name="This">The value to convert</param>
        /// <returns>A ISQLiteValue representing the value.</returns>
        public static ISQLiteValue ToSQLiteValue(this char This)
        {
            return ToSQLiteValue((long)(Convert.ChangeType(This, typeof(long))));
        }

        /// <summary>
        /// Converts a <see cref="sbyte"/> to an <see cref="ISQLiteValue"/>.
        /// </summary>
        /// <param name="This">The value to convert</param>
        /// <returns>A ISQLiteValue representing the value.</returns>
        public static ISQLiteValue ToSQLiteValue(this sbyte This)
        {
            return ToSQLiteValue((long)(Convert.ChangeType(This, typeof(long))));
        }

        /// <summary>
        /// Converts a <see cref="UInt32"/> to an <see cref="ISQLiteValue"/>.
        /// </summary>
        /// <param name="This">The value to convert</param>
        /// <returns>A ISQLiteValue representing the value.</returns>
        public static ISQLiteValue ToSQLiteValue(this UInt32 This)
        {
            return ToSQLiteValue((long)(Convert.ChangeType(This, typeof(long))));
        }

        public static ISQLiteValue ToSQLiteValue(this UInt16 This)
        {
            return Convert.ToInt64(This).ToSQLiteValue();
        }

        /// <summary>
        /// Converts a <see cref="long"/> to an <see cref="ISQLiteValue"/>.
        /// </summary>
        /// <param name="This">The value to convert</param>
        /// <returns>A ISQLiteValue representing the value.</returns>
        public static ISQLiteValue ToSQLiteValue(this long This)
        {
            return new IntValue(This);
        }

        /// <summary>
        /// Converts a <see cref="double"/> to an <see cref="ISQLiteValue"/>.
        /// </summary>
        /// <param name="This">The value to convert</param>
        /// <returns>A ISQLiteValue representing the value.</returns>
        public static ISQLiteValue ToSQLiteValue(this double This)
        {
            return new FloatValue(This);
        }

        public static ISQLiteValue ToSQLiteValue(this float This)
        {
            return Convert.ToDouble(This).ToSQLiteValue();
        }

        /// <summary>
        /// Converts a <see cref="string"/> to an <see cref="ISQLiteValue"/>.
        /// </summary>
        /// <param name="This">The value to convert</param>
        /// <returns>A ISQLiteValue representing the value.</returns>
        public static ISQLiteValue ToSQLiteValue(this string This)
        {
            Contract.Requires(This != null);
            return new StringValue(This);
        }

        /// <summary>
        /// Converts a byte array to an <see cref="ISQLiteValue"/>.
        /// </summary>
        /// <param name="This">The value to convert</param>
        /// <returns>A ISQLiteValue representing the blob.</returns>
        public static ISQLiteValue ToSQLiteValue(this byte[] This)
        {
            Contract.Requires(This != null);
            return new BlobValue(This);
        }

        public static ISQLiteValue ToSQLiteValue(this TimeSpan This)
        {
            return This.Ticks.ToSQLiteValue();
        }

        public static ISQLiteValue ToSQLiteValue(this DateTime This)
        {
            return This.Ticks.ToSQLiteValue();
        }

        public static ISQLiteValue ToSQLiteValue(this DateTimeOffset This)
        {
            return This.ToOffset(TimeSpan.Zero).Ticks.ToSQLiteValue();
        }

        public static ISQLiteValue ToSQLiteValue(this decimal This)
        {
            return Convert.ToDouble(This).ToSQLiteValue();
        }            

        public static ISQLiteValue ToSQLiteValue(this Guid This)
        {
            return This.ToString().ToSQLiteValue();
        }

        internal static ISQLiteValue ToSQLiteValue(this sqlite3_value This)
        {
            return new NativeValue(This);
        }

        internal static IResultSetValue ResultSetValueAt(this StatementImpl This, int index)
        {
            return new ResultSetValueImpl(This, index);
        }

        internal static void SetResult(this sqlite3_context ctx, ISQLiteValue value)
        {
            switch (value.SQLiteType)
            {
                case SQLiteType.Blob:
                    if (value is ZeroBlob)
                    {
                        raw.sqlite3_result_zeroblob(ctx, value.Length);
                    }
                    else
                    {
                        raw.sqlite3_result_blob(ctx, value.ToBlob());
                    }
                    return;

                case SQLiteType.Null:
                    raw.sqlite3_result_null(ctx);
                    return;

                case SQLiteType.Text:
                    raw.sqlite3_result_text(ctx, value.ToString());
                    return;

                case SQLiteType.Float:
                    raw.sqlite3_result_double(ctx, value.ToDouble());
                    return;

                case SQLiteType.Integer:
                    raw.sqlite3_result_int64(ctx, value.ToInt64());
                    return;
            }
        }

        public static bool ToBool(this ISQLiteValue value)
        {
            return value.ToInt() != 0;
        }

        public static float ToFloat(this ISQLiteValue value)
        {
            return (float) value.ToDouble();
        }

        public static TimeSpan ToTimeSpan(this ISQLiteValue value)
        {
            return new TimeSpan(value.ToInt64());
        }

        public static DateTime ToDateTime(this ISQLiteValue value)
        {
            return new DateTime (value.ToInt64());
        }

        public static DateTimeOffset ToDateTimeOffset(this ISQLiteValue value)
        {
            return new DateTimeOffset(value.ToInt64(), TimeSpan.Zero);
        }

        public static UInt32 ToUInt32(this ISQLiteValue value)
        {
            return (uint) value.ToInt64();
        }

        public static decimal ToDecimal(this ISQLiteValue value)
        {
            return (decimal) value.ToDouble();
        }

        public static byte ToByte(this ISQLiteValue value)
        {
            return (byte) value.ToInt();
        }

        public static UInt16 ToUInt16(this ISQLiteValue value)
        {
            return (ushort) value.ToInt();
        }

        public static short ToShort(this ISQLiteValue value)
        {
            return (short) value.ToInt();
        }

        public static sbyte ToSByte(this ISQLiteValue value)
        {
            return (sbyte) value.ToInt();
        }

        public static Guid ToGuid(this ISQLiteValue value)
        {
            var text = value.ToString();
            return new Guid(text);
        }

        internal static object ToObject(this ISQLiteValue value, Type clrType)
        {
            if (value.SQLiteType == SQLiteType.Null)    { return null; } 
            else if (clrType == typeof(String))         { return value.ToString(); } 
            else if (clrType == typeof(Int32))          { return value.ToInt(); } 
            else if (clrType == typeof(Boolean))        { return value.ToBool(); } 
            else if (clrType == typeof(double))         { return value.ToDouble(); } 
            else if (clrType == typeof(float))          { return value.ToFloat(); } 
            else if (clrType == typeof(TimeSpan))       { return value.ToTimeSpan(); } 
            else if (clrType == typeof(DateTime))       { return value.ToDateTime(); } 
            else if (clrType == typeof(DateTimeOffset)) { return value.ToDateTimeOffset(); }
            else if (clrType.GetTypeInfo().IsEnum)      { return value.ToInt(); } 
            else if (clrType == typeof(Int64))          { return value.ToInt64(); } 
            else if (clrType == typeof(UInt32))         { return value.ToUInt32(); } 
            else if (clrType == typeof(decimal))        { return value.ToDecimal(); } 
            else if (clrType == typeof(Byte))           { return value.ToByte(); } 
            else if (clrType == typeof(UInt16))         { return value.ToUInt16(); } 
            else if (clrType == typeof(Int16))          { return value.ToShort(); } 
            else if (clrType == typeof(sbyte))          { return value.ToSByte(); } 

            // FIXME: Bonus points clrType == Stream
            else if (clrType == typeof(byte[]))         { return value.ToBlob(); } 
            else if (clrType == typeof(Guid))           { return value.ToGuid(); } 

            else 
            {
                throw new NotSupportedException ("Don't know how to read " + clrType);
            }
        }
    }

    internal sealed class NativeValue : ISQLiteValue
    {
        private readonly sqlite3_value value;

        internal NativeValue(sqlite3_value value)
        {
            this.value = value;
        }

        public SQLiteType SQLiteType
        {
            get
            {
                return (SQLiteType)raw.sqlite3_value_type(value);
            }
        }

        public int Length
        {
            get
            {
                return raw.sqlite3_value_bytes(value);
            }
        }

        public byte[] ToBlob()
        {
            return raw.sqlite3_value_blob(value) ?? new byte[0];
        }

        public double ToDouble()
        {
            return raw.sqlite3_value_double(value);
        }

        public int ToInt()
        {
            return raw.sqlite3_value_int(value);
        }

        public long ToInt64()
        {
            return raw.sqlite3_value_int64(value);
        }

        public override string ToString()
        {
            return raw.sqlite3_value_text(value) ?? "";
        }
    }

    // Type coercion rules
    // http://www.sqlite.org/capi3ref.html#sqlite3_column_blob
    internal struct NullValue : ISQLiteValue
    {
        public SQLiteType SQLiteType
        {
            get
            {
                return SQLiteType.Null;
            }
        }

        public int Length
        {
            get
            {
                return 0;
            }
        }

        public byte[] ToBlob()
        {
            return new byte[0];
        }

        public double ToDouble()
        {
            return 0.0;
        }

        public int ToInt()
        {
            return 0;
        }

        public long ToInt64()
        {
            return 0;
        }

        public override string ToString()
        {
            return "";
        }
    }

    internal struct IntValue : ISQLiteValue
    {
        private readonly long value;

        internal IntValue(long value)
        {
            this.value = value;
        }

        public SQLiteType SQLiteType
        {
            get
            {
                return SQLiteType.Integer;
            }
        }

        public int Length
        {
            get
            {
                return this.ToBlob().Length;
            }
        }

        public byte[] ToBlob()
        {
            return this.ToString().ToSQLiteValue().ToBlob();
        }

        public double ToDouble()
        {
            return value;
        }

        public int ToInt()
        {
            return (int)value;
        }

        public long ToInt64()
        {
            return value;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }

    internal struct FloatValue : ISQLiteValue
    {
        private readonly double value;

        internal FloatValue(double value)
        {
            this.value = value;
        }

        public SQLiteType SQLiteType
        {
            get
            {
                return SQLiteType.Float;
            }
        }

        public int Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        // Casting to a BLOB consists of first casting the value to TEXT
        // in the encoding of the database connection, then interpreting
        // the resulting byte sequence as a BLOB instead of as TEXT.
        public byte[] ToBlob()
        {
            throw new NotSupportedException();
        }

        public double ToDouble()
        {
            return value;
        }

        public int ToInt()
        {
            return (int)this.ToInt64();
        }

        public long ToInt64()
        {
            if (this.value > Int64.MaxValue)
            {
                return Int64.MaxValue;
            }
            else if (this.value < Int64.MinValue)
            {
                return Int64.MinValue;
            }
            else
            {
                return (long)value;
            }
        }

        public override string ToString()
        {
            throw new NotSupportedException();
        }
    }

    internal class StringValue : ISQLiteValue
    {
        private static readonly Regex isNegative = new Regex("^[ ]*[-]");
        private static readonly Regex stringToDoubleCast = new Regex("^[ ]*([-]?)[0-9]+([.][0-9]+)?");
        private static readonly Regex stringToLongCast = new Regex("^[ ]*([-]?)[0-9]+");

        private readonly string value;

        internal StringValue(string value)
        {
            this.value = value;
        }

        public SQLiteType SQLiteType
        {
            get
            {
                return SQLiteType.Text;
            }
        }

        public int Length
        {
            get
            {
                return this.ToBlob().Length;
            }
        }

        public byte[] ToBlob()
        {
            return Encoding.UTF8.GetBytes(value);
        }

        // When casting a TEXT value to REAL, the longest possible prefix
        // of the value that can be interpreted as a real number is extracted
        // from the TEXT value and the remainder ignored. Any leading spaces
        // in the TEXT value are ignored when converging from TEXT to REAL.
        // If there is no prefix that can be interpreted as a real number,
        // the result of the conversion is 0.0.
        public double ToDouble()
        {
            var result = stringToDoubleCast.Match(this.value);
            if (result.Success)
            {
                double parsed;
                if (Double.TryParse(result.Value, out parsed))
                {
                    return parsed;
                }
            }

            return 0.0;
        }

        public int ToInt()
        {
            return (int)this.ToInt64();
        }

        // When casting a TEXT value to INTEGER, the longest possible prefix of
        // the value that can be interpreted as an integer number is extracted from
        // the TEXT value and the remainder ignored. Any leading spaces in the TEXT
        // value when converting from TEXT to INTEGER are ignored. If there is no
        // prefix that can be interpreted as an integer number, the result of the
        // conversion is 0. The CAST operator understands decimal integers only —
        // conversion of hexadecimal integers stops at the "x" in the "0x" prefix
        // of the hexadecimal integer string and thus result of the CAST is always zero.
        public long ToInt64()
        {
            var result = stringToLongCast.Match(this.value);
            if (result.Success)
            {
                long parsed;
                if (long.TryParse(result.Value, out parsed))
                {
                    return parsed;
                }
                else if (isNegative.Match(this.value).Success)
                {
                    return long.MinValue;
                }
                else
                {
                    return long.MaxValue;
                }
            }

            return 0;
        }

        public override string ToString()
        {
            return value;
        }
    }

    internal class BlobValue : ISQLiteValue
    {
        private readonly byte[] value;

        internal BlobValue(byte[] value)
        {
            this.value = value;
        }

        public SQLiteType SQLiteType
        {
            get
            {
                return SQLiteType.Blob;
            }
        }

        public int Length
        {
            get
            {
                return value.Length;
            }
        }

        public byte[] ToBlob()
        {
            return value;
        }

        // When casting a BLOB value to a REAL, the value is first converted to TEXT.
        public double ToDouble()
        {
            return this.ToString().ToSQLiteValue().ToDouble();
        }

        public int ToInt()
        {
            return (int)this.ToInt64();
        }

        // When casting a BLOB value to INTEGER, the value is first converted to TEXT.
        public long ToInt64()
        {
            return this.ToString().ToSQLiteValue().ToInt64();
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(value, 0, value.Length);
        }
    }

    internal sealed class ResultSetValueImpl : IResultSetValue
    {
        private readonly StatementImpl stmt;
        private readonly int index;

        internal ResultSetValueImpl(StatementImpl stmt, int index)
        {
            this.stmt = stmt;
            this.index = index;
        }

        public ColumnInfo ColumnInfo
        {
            get
            {
                return ColumnInfo.Create(stmt, index);
            }
        }

        public SQLiteType SQLiteType
        {
            get
            {
                return (SQLiteType)raw.sqlite3_column_type(stmt.sqlite3_stmt, index);
            }
        }

        public int Length
        {
            get
            {
                return raw.sqlite3_column_bytes(stmt.sqlite3_stmt, index);
            }
        }

        public byte[] ToBlob()
        {
            return raw.sqlite3_column_blob(stmt.sqlite3_stmt, index) ?? new byte[0];
        }

        public double ToDouble()
        {
            return raw.sqlite3_column_double(stmt.sqlite3_stmt, index);
        }

        public int ToInt()
        {
            return raw.sqlite3_column_int(stmt.sqlite3_stmt, index);
        }

        public long ToInt64()
        {
            return raw.sqlite3_column_int64(stmt.sqlite3_stmt, index);
        }

        public override string ToString()
        {
            return raw.sqlite3_column_text(stmt.sqlite3_stmt, index) ?? "";
        }
    }

    internal class ZeroBlob : ISQLiteValue
    {
        private readonly int length;

        internal ZeroBlob(int length)
        {
            this.length = length;
        }

        public SQLiteType SQLiteType
        {
            get { return SQLiteType.Blob; }
        }

        public int Length
        {
            get { return length; }
        }

        public byte[] ToBlob()
        {
            return new byte[length];
        }

        public double ToDouble()
        {
            return 0;
        }

        public int ToInt()
        {
            return 0;
        }

        public long ToInt64()
        {
            return 0;
        }

        public override string ToString()
        {
            return "";
        }
    }
}

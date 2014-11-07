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
using System.Text;
using System.Text.RegularExpressions;

namespace SQLitePCL.pretty
{
    public static class SQLiteValue
    {
        private static readonly ISQLiteValue _null = new NullValue();

        public static ISQLiteValue Null
        {
            get
            {
                return _null;
            }
        }

        public static ISQLiteValue ToSQLiteValue(this int value)
        {
            return ToSQLiteValue((long)value);
        }

        public static ISQLiteValue ToSQLiteValue(this bool value)
        {
            return ToSQLiteValue((long)(Convert.ChangeType(value, typeof(long))));
        }

        public static ISQLiteValue ToSQLiteValue(this byte value)
        {
            return ToSQLiteValue((long)(Convert.ChangeType(value, typeof(long))));
        }

        public static ISQLiteValue ToSQLiteValue(this char value)
        {
            return ToSQLiteValue((long)(Convert.ChangeType(value, typeof(long))));
        }

        public static ISQLiteValue ToSQLiteValue(this sbyte value)
        {
            return ToSQLiteValue((long)(Convert.ChangeType(value, typeof(long))));
        }

        public static ISQLiteValue ToSQLiteValue(this UInt32 value)
        {
            return ToSQLiteValue((long)(Convert.ChangeType(value, typeof(long))));
        }

        public static ISQLiteValue ToSQLiteValue(this long value)
        {
            return new IntValue(value);
        }

        public static ISQLiteValue ToSQLiteValue(this double value)
        {
            return new FloatValue(value);
        }

        public static ISQLiteValue ToSQLiteValue(this string value)
        {
            Contract.Requires(value != null);
            return new StringValue(value);
        }

        public static ISQLiteValue ToSQLiteValue(this byte[] blob)
        {
            Contract.Requires(blob != null);
            return new BlobValue(blob);
        }

        internal static ISQLiteValue ToSQLiteValue(this sqlite3_value value)
        {
            return new NativeValue(value);
        }
    }

    public static class ResultSetValue
    {
        internal static IResultSetValue ResultSetValueAt(this sqlite3_stmt stmt, int index)
        {
            return new ResultSetValueImpl(stmt, index);
        }

        public static Stream ToReadWriteStream(this IResultSetValue value)
        {
            Contract.Requires(value != null);

            return value.ToStream(true);
        }

        public static Stream ToReadOnlyStream(this IResultSetValue value)
        {
            Contract.Requires(value != null);

            return value.ToStream(false);
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
            return raw.sqlite3_value_blob(value);
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
            return raw.sqlite3_value_text(value);
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
            return null;
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
            return null;
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
                return this.ToBlob().Length;
            }
        }

        // Casting to a BLOB consists of first casting the value to TEXT
        // in the encoding of the database connection, then interpreting
        // the resulting byte sequence as a BLOB instead of as TEXT.
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
            return (long)value;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }

    internal class StringValue : ISQLiteValue
    {
        private static Regex isNegative = new Regex("^[ ]*[-]");
        private static Regex stringToDoubleCast = new Regex("^[ ]*([-]?)[0-9]+([.][0-9]+)?");
        private static Regex stringToLongCast = new Regex("^[ ]*([-]?)[0-9]+");

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
        private readonly sqlite3_stmt stmt;
        private readonly int index;

        internal ResultSetValueImpl(sqlite3_stmt stmt, int index)
        {
            this.stmt = stmt;
            this.index = index;
        }

        public string ColumnName
        {
            get
            {
                return raw.sqlite3_column_name(stmt, index);
            }
        }

        public string ColumnDatabaseName
        {
            get
            {
                return raw.sqlite3_column_database_name(stmt, index);
            }
        }

        public String ColumnOriginName
        {
            get
            {
                return raw.sqlite3_column_origin_name(stmt, index);
            }
        }

        public string ColumnTableName
        {
            get
            {
                return raw.sqlite3_column_table_name(stmt, index);
            }
        }

        public SQLiteType SQLiteType
        {
            get
            {
                return (SQLiteType)raw.sqlite3_column_type(stmt, index);
            }
        }

        public int Length
        {
            get
            {
                return raw.sqlite3_column_bytes(stmt, index);
            }
        }

        public byte[] ToBlob()
        {
            return raw.sqlite3_column_blob(stmt, index);
        }

        public double ToDouble()
        {
            return raw.sqlite3_column_double(stmt, index);
        }

        public int ToInt()
        {
            return raw.sqlite3_column_int(stmt, index);
        }

        public long ToInt64()
        {
            return raw.sqlite3_column_int64(stmt, index);
        }

        public override string ToString()
        {
            return raw.sqlite3_column_text(stmt, index);
        }

        public Stream ToStream(bool canWrite)
        {
            sqlite3 db = raw.sqlite3_db_handle(stmt);
            string sdb = raw.sqlite3_column_database_name(stmt, index);
            string tableName = raw.sqlite3_column_table_name(stmt, index);
            string columnName = raw.sqlite3_column_name(stmt, index);
            long rowId = raw.sqlite3_last_insert_rowid(db);

            sqlite3_blob blob;
            int rc = raw.sqlite3_blob_open(db, sdb, tableName, columnName, rowId, canWrite ? 1 : 0, out blob);
            SQLiteException.CheckOk(stmt, rc);

            return new BlobStream(blob, canWrite);
        }
    }
}

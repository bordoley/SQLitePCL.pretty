/*
   Copyright 2014 David Bordoley

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

using Xunit;
using System;
using System.Linq;
using System.Text;

namespace SQLitePCL.pretty.tests
{
    public class SQLiteValueTests : TestBase
    {
        private void compare(ISQLiteValue expected, ISQLiteValue test)
        {
            Assert.Equal(expected.Length, test.Length);
            Assert.Equal(expected.SQLiteType, test.SQLiteType);

            // FIXME: Testing double equality is imprecise
            //Assert.Equal(expected.ToDouble(), test.ToDouble());
            Assert.Equal(expected.ToInt64(), test.ToInt64());
            Assert.Equal(expected.ToInt(), test.ToInt());
            Assert.Equal(expected.ToString(), test.ToString());
            Assert.Equal(expected.ToBlob(), test.ToBlob());
        }

        [Fact]
        public void TestToSQLiteValueExtensions()
        {
            short testShort = 2;
            Assert.Equal(testShort, testShort.ToSQLiteValue().ToShort());

            byte testByte = 2;
            Assert.Equal(testByte, testByte.ToSQLiteValue().ToByte());

            float testFloat = 2.0f;
            Assert.Equal(testFloat, testFloat.ToSQLiteValue().ToFloat());

            TimeSpan testTimeSpan = new TimeSpan(100);
            Assert.Equal(testTimeSpan, testTimeSpan.ToSQLiteValue().ToTimeSpan());

            DateTime testDateTime = DateTime.Now;
            Assert.Equal(testDateTime, testDateTime.ToSQLiteValue().ToDateTime());

            DateTimeOffset testDateTimeOffset = new DateTimeOffset(100, TimeSpan.Zero);
            Assert.Equal(testDateTimeOffset, testDateTimeOffset.ToSQLiteValue().ToDateTimeOffset());

            decimal testDecimal = 2.2m;
            Assert.Equal(testDecimal, testDecimal.ToSQLiteValue().ToDecimal());

            Guid testGuid = Guid.NewGuid();
            Assert.Equal(testGuid, testGuid.ToSQLiteValue().ToGuid());

            ushort testUShort = 1;
            Assert.Equal(testUShort, testUShort.ToSQLiteValue().ToUInt16());

            sbyte testSByte = 1;
            Assert.Equal(testSByte, testSByte.ToSQLiteValue().ToSByte());

            Uri uri = new Uri("http://www.example.com/path/to/resource?querystring#fragment");
            Assert.Equal(uri, uri.ToSQLiteValue().ToUri());
        }

        [Fact]
        public void TestToSQLiteValue()
        {
            Assert.Equal(false.ToSQLiteValue().ToInt(), 0);
            Assert.NotEqual(true.ToSQLiteValue().ToInt(), 0);

            byte b = 8;
            Assert.Equal(b.ToSQLiteValue().ToInt(), b);

            char c = 'c';
            Assert.Equal(c.ToSQLiteValue().ToInt(), (long) c);

            sbyte sb = 8;
            Assert.Equal(sb.ToSQLiteValue().ToInt(), sb);

            uint u = 8;
            Assert.Equal(u.ToSQLiteValue().ToUInt32(), u);
        }

        [Fact]
        public void TestNullValue()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                using (var stmt = db.PrepareStatement("SELECT null;"))
                {
                    stmt.MoveNext();
                    var expected = stmt.Current.First();
                    compare(expected, SQLiteValue.Null);
                }
            }
        }

        [Fact]
        public void TestFloatValue()
        {
            double[] tests =
            {
                1,
                1.0,
                1.11,
                1.7E+3,
                -195489100.8377,
                1.12345678901234567E100,
                -1.12345678901234567E100
            };

            using (var db = SQLite3.OpenInMemory())
            {
                foreach (var test in tests)
                {
                    db.Execute("CREATE TABLE foo (x real);");
                    db.Execute("INSERT INTO foo (x) VALUES (?)", test);

                    var rows = db.Query("SELECT x FROM foo;");
                    foreach (var row in rows)
                    {
                        var expected = row.Single();
                        var result = test.ToSQLiteValue();

                        Assert.Throws<NotSupportedException>(() => { var x = result.Length; });
                        Assert.Throws<NotSupportedException>(() => { result.ToString(); });
                        Assert.Throws<NotSupportedException>(() => { result.ToBlob(); });

                        Assert.Equal(expected.SQLiteType, result.SQLiteType);
                        Assert.Equal(expected.ToInt64(), result.ToInt64());
                        Assert.Equal(expected.ToInt(), result.ToInt());
                    }

                    db.Execute("DROP TABLE foo;");
                }
            }
        }

        [Fact]
        public void TestIntValue()
        {
            long[] tests =
                {
                    2147483647, // Max int
                    -2147483648, // Min int
                    9223372036854775807, // Max Long
                    -9223372036854775808, // Min Long
                    -1234
                };

            using (var db = SQLite3.OpenInMemory())
            {
                foreach (var test in tests)
                {
                    db.Execute("CREATE TABLE foo (x int);");
                    db.Execute("INSERT INTO foo (x) VALUES (?)", test);

                    var rows = db.Query("SELECT x FROM foo;");
                    foreach (var row in rows)
                    {
                        compare(row.Single(), test.ToSQLiteValue());
                    }

                    db.Execute("DROP TABLE foo;");
                }
            }
        }

        [Fact]
        public void TestBlobValue()
        {
            string[] tests =
                {
                    "",
                    "  1234.56",
                    " 1234.abasd",
                    "abacdd\u10FFFF",
                    "2147483647", // Max int
                    "-2147483648", // Min int
                    "9223372036854775807", // Max Long
                    "-9223372036854775808", // Min Long
                    "9923372036854775809", // Greater than max long
                    "-9923372036854775809", // Less than min long
                    "3147483648", // Long
                    "-1234",
                    // "1111111111111111111111" SQLite's result in this case is undefined
                };

            using (var db = SQLite3.OpenInMemory())
            {
                foreach (var test in tests.Select(test => Encoding.UTF8.GetBytes(test)))
                {
                    db.Execute("CREATE TABLE foo (x blob);");
                    db.Execute("INSERT INTO foo (x) VALUES (?)", test);

                    var rows = db.Query("SELECT x FROM foo;");
                    foreach (var row in rows)
                    {
                        compare(row.Single(), test.ToSQLiteValue());
                    }

                    db.Execute("DROP TABLE foo;");
                }
            }
        }

        [Fact]
        public void TestZeroBlob()
        {
            int[] tests = { 0, 1, 2, 10 };

            using (var db = SQLite3.OpenInMemory())
            {
                foreach (var test in tests.Select(SQLiteValue.ZeroBlob))
                {
                    db.Execute("CREATE TABLE foo (x blob);");
                    db.Execute("INSERT INTO foo (x) VALUES (?)", test);

                    foreach (var row in db.Query("SELECT x FROM foo;"))
                    {
                        compare(row.First(), test);
                    }
                    db.Execute("DROP TABLE foo;");
                }
            }
        }

        [Fact]
        public void TestStringValue()
        {
            string[] tests =
                {
                    "  1234.56",
                    " 1234.abasd",
                    "abacdd\u10FFFF",
                    "2147483647", // Max int
                    "-2147483648", // Min int
                    "9223372036854775807", // Max Long
                    "-9223372036854775808", // Min Long
                    "9923372036854775809", // Greater than max long
                    "-9923372036854775809", // Less than min long
                    "3147483648", // Long
                    "-1234",
                    // "1111111111111111111111" SQLite's result in this case is undefined
                };

            using (var db = SQLite3.OpenInMemory())
            {
                foreach (var test in tests)
                {
                    db.Execute("CREATE TABLE foo (x text);");
                    db.Execute("INSERT INTO foo (x) VALUES (?)", test);

                    var rows = db.Query("SELECT x FROM foo;");
                    foreach (var row in rows)
                    {
                        compare(row.Single(), test.ToSQLiteValue());
                    }

                    db.Execute("DROP TABLE foo;");
                }
            }
        }

        [Fact]
        public void TestResultSetValue()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (w int, x text, y real, z blob, n text);");

                byte[] blob = { 1, 2 };
                db.Execute("INSERT INTO foo (w, x, y, z, n) VALUES (?,?,?,?,?)", 32, "hello", 3.14, blob, null);

                using (var stmt = db.PrepareStatement("SELECT * from foo"))
                {
                    stmt.MoveNext();
                    var row = stmt.Current;

                    Assert.Equal(row[0].ColumnInfo.DatabaseName, "main");
                    Assert.Equal(row[0].ColumnInfo.TableName, "foo");
                    Assert.Equal(row[0].ColumnInfo.OriginName, "w");
                    Assert.Equal(row[0].ColumnInfo.Name, "w");
                    Assert.Equal(row[0].SQLiteType, SQLiteType.Integer);
                    Assert.Equal(row[0].ToInt(), 32);

                    Assert.Equal(row[1].ColumnInfo.DatabaseName, "main");
                    Assert.Equal(row[1].ColumnInfo.TableName, "foo");
                    Assert.Equal(row[1].ColumnInfo.OriginName, "x");
                    Assert.Equal(row[1].ColumnInfo.Name, "x");
                    Assert.Equal(row[1].SQLiteType, SQLiteType.Text);
                    Assert.Equal(row[1].ToString(), "hello");

                    Assert.Equal(row[2].ColumnInfo.DatabaseName, "main");
                    Assert.Equal(row[2].ColumnInfo.TableName, "foo");
                    Assert.Equal(row[2].ColumnInfo.OriginName, "y");
                    Assert.Equal(row[2].ColumnInfo.Name, "y");
                    Assert.Equal(row[2].SQLiteType, SQLiteType.Float);
                    Assert.Equal(row[2].ToDouble(), 3.14);

                    Assert.Equal(row[3].ColumnInfo.DatabaseName, "main");
                    Assert.Equal(row[3].ColumnInfo.TableName, "foo");
                    Assert.Equal(row[3].ColumnInfo.OriginName, "z");
                    Assert.Equal(row[3].ColumnInfo.Name, "z");
                    Assert.Equal(row[3].SQLiteType, SQLiteType.Blob);
                    Assert.Equal(row[3].ToBlob(), blob);

                    Assert.Equal(row[4].ColumnInfo.DatabaseName, "main");
                    Assert.Equal(row[4].ColumnInfo.TableName, "foo");
                    Assert.Equal(row[4].ColumnInfo.OriginName, "n");
                    Assert.Equal(row[4].ColumnInfo.Name, "n");
                    Assert.Equal(row[4].SQLiteType, SQLiteType.Null);
                }

                using (var stmt = db.PrepareStatement("SELECT w AS mario FROM foo;"))
                {
                    stmt.MoveNext();
                    var row = stmt.Current;

                    Assert.Equal(row[0].ColumnInfo.OriginName, "w");
                    Assert.Equal(row[0].ColumnInfo.Name, "mario");
                }
            }
        }
    }
}

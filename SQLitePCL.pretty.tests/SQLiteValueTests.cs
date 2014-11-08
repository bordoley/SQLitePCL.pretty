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

using System;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class SQLiteValueTests
    {
        private void compare(ISQLiteValue expected, ISQLiteValue test)
        {
            Assert.AreEqual(expected.Length, test.Length);
            Assert.AreEqual(expected.SQLiteType, test.SQLiteType);

            // FIXME: Testing double equality is imprecise
            //Assert.AreEqual(expected.ToDouble(), test.ToDouble());
            Assert.AreEqual(expected.ToInt64(), test.ToInt64());
            Assert.AreEqual(expected.ToInt(), test.ToInt());
            Assert.AreEqual(expected.ToString(), test.ToString());

            var expectedBlob = expected.ToBlob();
            var testBlog = test.ToBlob();

            if (expectedBlob == null)
            {
                Assert.IsNull(testBlog);
            }
            else 
            {
                Assert.IsNotNull(testBlog);
                CollectionAssert.AreEqual(expectedBlob, testBlog);
            }
        }

        [Test]
        public void TestNullValue()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                using (var stmt = db.PrepareStatement("SELECT null;"))
                {
                    var expected = stmt.Current.First();
                    compare(expected, SQLiteValue.Null);
                }
            }
        }

        [Test]
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

            using (var db = SQLite3.Open(":memory:"))
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

                        Assert.Throws(typeof(NotSupportedException), () => { var x = result.Length; });
                        Assert.Throws(typeof(NotSupportedException), () => { result.ToString(); });
                        Assert.Throws(typeof(NotSupportedException), () => { result.ToBlob(); });

                        Assert.AreEqual(expected.SQLiteType, result.SQLiteType);
                        Assert.AreEqual(expected.ToInt64(), result.ToInt64());
                        Assert.AreEqual(expected.ToInt(), result.ToInt());
                    }

                    db.Execute("DROP TABLE foo;");
                }
            }
        }

        [Test]
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

            using (var db = SQLite3.Open(":memory:"))
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

        [Test]
        public void TestBlobValue()
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

            using (var db = SQLite3.Open(":memory:"))
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

        [Test]
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

            using (var db = SQLite3.Open(":memory:"))
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

        [Test]
        public void TestResultSetValue()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (w int, x text, y real, z blob, n text);");

                byte[] blob = { 1, 2 };
                db.Execute("INSERT INTO foo (w, x, y, z, n) VALUES (?,?,?,?,?)", 32, "hello", 3.14, blob, null);

                using (var stmt = db.PrepareStatement("SELECT * from foo"))
                {
                    stmt.MoveNext();
                    var row = stmt.Current;

                    Assert.AreEqual(row[0].ColumnDatabaseName, "main");
                    Assert.AreEqual(row[0].ColumnTableName, "foo");
                    Assert.AreEqual(row[0].ColumnOriginName, "w");
                    Assert.AreEqual(row[0].ColumnName, "w");
                    Assert.AreEqual(row[0].SQLiteType, SQLiteType.Integer);
                    Assert.AreEqual(row[0].ToInt(), 32);

                    Assert.AreEqual(row[1].ColumnDatabaseName, "main");
                    Assert.AreEqual(row[1].ColumnTableName, "foo");
                    Assert.AreEqual(row[1].ColumnOriginName, "x");
                    Assert.AreEqual(row[1].ColumnName, "x");
                    Assert.AreEqual(row[1].SQLiteType, SQLiteType.Text);
                    Assert.AreEqual(row[1].ToString(), "hello");

                    Assert.AreEqual(row[2].ColumnDatabaseName, "main");
                    Assert.AreEqual(row[2].ColumnTableName, "foo");
                    Assert.AreEqual(row[2].ColumnOriginName, "y");
                    Assert.AreEqual(row[2].ColumnName, "y");
                    Assert.AreEqual(row[2].SQLiteType, SQLiteType.Float);
                    Assert.AreEqual(row[2].ToDouble(), 3.14);

                    Assert.AreEqual(row[3].ColumnDatabaseName, "main");
                    Assert.AreEqual(row[3].ColumnTableName, "foo");
                    Assert.AreEqual(row[3].ColumnOriginName, "z");
                    Assert.AreEqual(row[3].ColumnName, "z");
                    Assert.AreEqual(row[3].SQLiteType, SQLiteType.Blob);
                    Assert.That(Enumerable.SequenceEqual(row[3].ToBlob(), blob));

                    Assert.AreEqual(row[4].ColumnDatabaseName, "main");
                    Assert.AreEqual(row[4].ColumnTableName, "foo");
                    Assert.AreEqual(row[4].ColumnOriginName, "n");
                    Assert.AreEqual(row[4].ColumnName, "n");
                    Assert.AreEqual(row[4].SQLiteType, SQLiteType.Null);
                }

                using (var stmt = db.PrepareStatement("SELECT w AS mario FROM foo;"))
                {
                    stmt.MoveNext();
                    var row = stmt.Current;

                    Assert.AreEqual(row[0].ColumnOriginName, "w");
                    Assert.AreEqual(row[0].ColumnName, "mario");
                }      
            }
        }
    }
}

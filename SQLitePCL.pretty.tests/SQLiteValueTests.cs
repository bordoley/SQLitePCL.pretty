using System;
using System.Linq;

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
                        compare(row.First(), test.ToSQLiteValue());
                    }
                    
                    db.Execute("DROP TABLE foo;");
                }
            }
        }
    }
}

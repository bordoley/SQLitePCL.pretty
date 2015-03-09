using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SQLitePCL.pretty.tests
{   
    [TestFixture]
    public partial class ResultSet
    {
        [Test]
        public void TestScalars()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                var query = db.Query("SELECT 1;");

                Assert.True(query.SelectScalarBool().First());
                Assert.AreEqual(query.SelectScalarByte().First(), 1);
                Assert.AreEqual(query.SelectScalarDateTime().First(), new DateTime(1));
                Assert.AreEqual(query.SelectScalarDateTimeOffset().First(), new DateTimeOffset(1, TimeSpan.Zero));
                Assert.AreEqual(query.SelectScalarDecimal().First(), 1m);
                Assert.AreEqual(query.SelectScalarDouble().First(), 1);
                Assert.AreEqual(query.SelectScalarFloat().First(), 1);
                Assert.AreEqual(query.SelectScalarInt().First(), 1);
                Assert.AreEqual(query.SelectScalarInt64().First(), 1);
                Assert.AreEqual(query.SelectScalarSByte().First(), 1);
                Assert.AreEqual(query.SelectScalarShort().First(), 1);
                Assert.AreEqual(query.SelectScalarString().First(), "1");
                Assert.AreEqual(query.SelectScalarTimeSpan().First(), new TimeSpan(1));
                Assert.AreEqual(query.SelectScalarUInt16().First(), 1);
                Assert.AreEqual(query.SelectScalarUInt32().First(), 1);

                var guid = Guid.NewGuid();
                query = db.Query("SELECT ?", guid);
                Assert.AreEqual(query.SelectScalarGuid().First(), guid);

                var blob = Encoding.UTF8.GetBytes("ab");
                var resultBlob = db.Query("SELECT ?", blob).SelectScalarBlob().First();
                Assert.AreEqual(Encoding.UTF8.GetString(resultBlob), "ab");
            }
        }
    }
}


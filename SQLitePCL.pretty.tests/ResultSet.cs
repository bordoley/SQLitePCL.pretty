using Xunit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SQLitePCL.pretty.tests
{   
    public partial class ResultSet : TestBase
    {
        [Fact]
        public void TestScalars()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                var query = db.Query("SELECT 1;");

                Assert.True(query.SelectScalarBool().First());
                Assert.Equal(query.SelectScalarByte().First(), 1);
                Assert.Equal(query.SelectScalarDateTime().First(), new DateTime(1));
                Assert.Equal(query.SelectScalarDateTimeOffset().First(), new DateTimeOffset(1, TimeSpan.Zero));
                Assert.Equal(query.SelectScalarDecimal().First(), 1m);
                Assert.Equal(query.SelectScalarDouble().First(), 1);
                Assert.Equal(query.SelectScalarFloat().First(), 1);
                Assert.Equal(query.SelectScalarInt().First(), 1);
                Assert.Equal(query.SelectScalarInt64().First(), 1);
                Assert.Equal(query.SelectScalarSByte().First(), 1);
                Assert.Equal(query.SelectScalarShort().First(), 1);
                Assert.Equal(query.SelectScalarString().First(), "1");
                Assert.Equal(query.SelectScalarTimeSpan().First(), new TimeSpan(1));
                Assert.Equal(query.SelectScalarUInt16().First(), 1);
                Assert.Equal(query.SelectScalarUInt32().First(), (uint) 1);

                var guid = Guid.NewGuid();
                query = db.Query("SELECT ?", guid);
                Assert.Equal(query.SelectScalarGuid().First(), guid);

                var uri = new Uri("http://www.example.com/path/to/resource");
                query = db.Query("SELECT ?", uri);
                Assert.Equal(query.SelectScalarUri().First(), uri);

                var blob = Encoding.UTF8.GetBytes("ab");
                var resultBlob = db.Query("SELECT ?", blob).SelectScalarBlob().First();
                Assert.Equal(Encoding.UTF8.GetString(resultBlob, 0, resultBlob.Length), "ab");
            }
        }
    }
}


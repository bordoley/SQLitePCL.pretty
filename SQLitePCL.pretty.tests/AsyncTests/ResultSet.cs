using Xunit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

namespace SQLitePCL.pretty.tests
{   
    public partial class ResultSet
    {
        [Fact]
        public async Task TestScalarsAsync()
        {
            using (var db = SQLite3.OpenInMemory().AsAsyncDatabaseConnection())
            {
                var query = db.Query("SELECT 1;");

                var resultBool = await query.SelectScalarBool().FirstAsync();
                Assert.True(resultBool);

                var resultByte = await query.SelectScalarByte().FirstAsync();
                Assert.Equal(resultByte, 1);

                var resultDateTime = await query.SelectScalarDateTime().FirstAsync();
                Assert.Equal(resultDateTime, new DateTime(1));

                var resultDto = await query.SelectScalarDateTimeOffset().FirstAsync();
                Assert.Equal(resultDto, new DateTimeOffset(1, TimeSpan.Zero));

                var resultDecimal = await query.SelectScalarDecimal().FirstAsync();
                Assert.Equal(resultDecimal, 1m);

                var resultDouble = await query.SelectScalarDouble().FirstAsync();
                Assert.Equal(resultDouble, 1);

                var resultInt = await query.SelectScalarInt().FirstAsync();
                Assert.Equal(resultInt, 1);

                var resultInt64 = await query.SelectScalarInt64().FirstAsync();
                Assert.Equal(resultInt64, 1);

                var resultSByte = await query.SelectScalarSByte().FirstAsync();
                Assert.Equal(resultSByte, 1);

                var resultShort = await query.SelectScalarShort().FirstAsync();
                Assert.Equal(resultShort, 1);

                var resultString = await query.SelectScalarString().FirstAsync();
                Assert.Equal(resultString, "1");

                var resultTimeSpan = await query.SelectScalarTimeSpan().FirstAsync();
                Assert.Equal(resultTimeSpan, new TimeSpan(1));

                var resultUInt16 = await query.SelectScalarUInt16().FirstAsync();
                Assert.Equal(resultUInt16, 1);

                var resultUInt32 = await query.SelectScalarUInt32().FirstAsync();
                Assert.Equal(resultUInt32, (uint) 1);

                var resultFloat = await query.SelectScalarFloat().FirstAsync();
                Assert.Equal(resultFloat, 1);

                var guid = Guid.NewGuid();
                var resultGuid = await db.Query("SELECT ?", guid).SelectScalarGuid().FirstAsync();
                Assert.Equal(resultGuid , guid);

                var uri = new Uri("http://www.example.com/path/to/resource");
                var resultUri = await db.Query("SELECT ?", uri).SelectScalarUri().FirstAsync();
                Assert.Equal(resultUri, uri);

                var blob = Encoding.UTF8.GetBytes("ab");
                var resultBlob = await db.Query("SELECT ?", blob).SelectScalarBlob().FirstAsync();
                Assert.Equal(Encoding.UTF8.GetString(resultBlob, 0, resultBlob.Length), "ab");
            }
        }
    }
}
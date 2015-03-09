using NUnit.Framework;
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
    [TestFixture]
    public partial class ResultSet
    {
        [Test]
        public async Task TestScalarsAsync()
        {
            using (var db = SQLite3.OpenInMemory().AsAsyncDatabaseConnection())
            {
                var query = db.Query("SELECT 1;");

                var resultBool = await query.SelectScalarBool().FirstAsync();
                Assert.True(resultBool);

                var resultByte = await query.SelectScalarByte().FirstAsync();
                Assert.AreEqual(resultByte, 1);

                var resultDateTime = await query.SelectScalarDateTime().FirstAsync();
                Assert.AreEqual(resultDateTime, new DateTime(1));

                var resultDto = await query.SelectScalarDateTimeOffset().FirstAsync();
                Assert.AreEqual(resultDto, new DateTimeOffset(1, TimeSpan.Zero));

                var resultDecimal = await query.SelectScalarDecimal().FirstAsync();
                Assert.AreEqual(resultDecimal, 1m);

                var resultDouble = await query.SelectScalarDouble().FirstAsync();
                Assert.AreEqual(resultDouble, 1);

                var resultInt = await query.SelectScalarInt().FirstAsync();
                Assert.AreEqual(resultInt, 1);

                var resultInt64 = await query.SelectScalarInt64().FirstAsync();
                Assert.AreEqual(resultInt64, 1);

                var resultSByte = await query.SelectScalarSByte().FirstAsync();
                Assert.AreEqual(resultSByte, 1);

                var resultShort = await query.SelectScalarShort().FirstAsync();
                Assert.AreEqual(resultShort, 1);

                var resultString = await query.SelectScalarString().FirstAsync();
                Assert.AreEqual(resultString, "1");

                var resultTimeSpan = await query.SelectScalarTimeSpan().FirstAsync();
                Assert.AreEqual(resultTimeSpan, new TimeSpan(1));

                var resultUInt16 = await query.SelectScalarUInt16().FirstAsync();
                Assert.AreEqual(resultUInt16, 1);

                var resultUInt32 = await query.SelectScalarUInt32().FirstAsync();
                Assert.AreEqual(resultUInt32, 1);

                var resultFloat = await query.SelectScalarFloat().FirstAsync();
                Assert.AreEqual(resultFloat, 1);

                var guid = Guid.NewGuid();
                var resultGuid = await db.Query("SELECT ?", guid).SelectScalarGuid().FirstAsync();
                Assert.AreEqual(resultGuid , guid);

                var blob = Encoding.UTF8.GetBytes("ab");
                var resultBlob = await db.Query("SELECT ?", blob).SelectScalarBlob().FirstAsync();
                Assert.AreEqual(Encoding.UTF8.GetString(resultBlob), "ab");
            }
        }
    }
}
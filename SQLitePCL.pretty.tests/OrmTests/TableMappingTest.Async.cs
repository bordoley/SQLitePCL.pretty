using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;
using Ignore = SQLitePCL.pretty.Orm.Attributes.IgnoreAttribute;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public partial class TableMappingTests
    {
        [Test]
        public async Task TestDeleteAllAsync()
        {
            var table = TableMapping.Create<TestObject>(
                            () => testObjectBuilder.Value, 
                            o => ((TestObject.Builder)o).Build());

            using (var db = SQLite3.OpenInMemory().AsAsyncDatabaseConnection())
            {
               
                var objects = new List<TestObject>()
                {
                    new TestObject.Builder() { Value = "Hello1" }.Build(),
                    new TestObject.Builder() { Value = "Hello2" }.Build(),
                    new TestObject.Builder() { Value = "Hello3" }.Build()
                };

                await db.InitTableAsync(table);
                var notDeleted = await db.Use(x => x.InsertOrReplace(table, new TestObject.Builder() { Value = "NotDeleted" }.Build()));
                var inserted = await db.InsertOrReplaceAllAsync(table, objects);
                var deleted = await db.DeleteAllAsync(table, inserted.Values.Select(x => x.Id.Value));
                CollectionAssert.AreEquivalent(inserted.Values, deleted.Values);

                var found = await db.FindAllAsync(table, inserted.Values.Select(x => x.Id.Value));
                Assert.IsEmpty(found);

                await db.Use(x =>
                    {
                        TestObject found2;
                        
                        if (x.TryFind(table, notDeleted.Id.Value, out found2))
                        {
                            Assert.AreEqual(found2, notDeleted);
                        }
                    });

                await db.DeleteAllRowsAsync(table);

                await db.Use(x =>
                    {
                        TestObject notFound;
                        Assert.IsFalse(x.TryFind(table, notDeleted.Id.Value, out notFound));
                    });
            }
        }

        [Test]
        public async Task TestDropTableAsync()
        {
            var table = TableMapping.Create<TestObject>(
                            () => testObjectBuilder.Value, 
                            o => ((TestObject.Builder)o).Build());

            using (var db = SQLite3.OpenInMemory().AsAsyncDatabaseConnection())
            {
                var tableLookup =
                    @"SELECT name FROM sqlite_master
                      WHERE type='table' AND name='TestObject'
                      ORDER BY name;";

                // Table doesn't exist
                await db.DropTableIfExistsAsync(table);

                await db.InitTableAsync(table);
                var count = await db.Query(tableLookup).Count();
                Assert.Greater(count, 0);

                await db.DropTableIfExistsAsync(table);

                count = await db.Query(tableLookup).Count();
                Assert.AreEqual(count, 0);
            }
        }
    }
}


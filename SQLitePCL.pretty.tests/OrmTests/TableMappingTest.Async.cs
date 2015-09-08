using Xunit;
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
    public partial class TableMappingTests
    {
        [Fact]
        public async Task TestDeleteAllAsync()
        {
            var orm = Orm.ResultSet.RowToObject(
                        () => testObjectBuilder.Value,
                        o => ((TestObject.Builder)o).Build());

            using (var db = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
               
                var objects = new List<TestObject>()
                {
                    new TestObject.Builder() { Value = "Hello1" }.Build(),
                    new TestObject.Builder() { Value = "Hello2" }.Build(),
                    new TestObject.Builder() { Value = "Hello3" }.Build()
                };

                await db.InitTableAsync<TestObject>();
                var notDeleted = await db.Use(x => x.InsertOrReplace(new TestObject.Builder() { Value = "NotDeleted" }.Build(), orm));
                var inserted = await db.InsertOrReplaceAllAsync(objects, orm);
                var deleted = await db.DeleteAllAsync(inserted.Values.Select(x => x.Id.Value), orm);
                Assert.Equal(inserted.Values, deleted.Values);

                var found = await db.FindAllAsync(inserted.Values.Select(x => x.Id.Value), orm);
                Assert.Empty(found);

                await db.Use(x =>
                    {
                        TestObject found2;
                        
                        if (x.TryFind(notDeleted.Id.Value, orm, out found2))
                        {
                            Assert.Equal(found2, notDeleted);
                        }
                    });

                await db.DeleteAllRowsAsync<TestObject>();

                await db.Use(x =>
                    {
                        TestObject notFound;
                        Assert.False(x.TryFind(notDeleted.Id.Value, orm, out notFound));
                    });
            }
        }

        [Fact]
        public async Task TestDropTableAsync()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                var tableLookup =
                    @"SELECT name FROM sqlite_master
                      WHERE type='table' AND name='TestObject'
                      ORDER BY name;";

                // Table doesn't exist
                await db.DropTableIfExistsAsync<TestObject>();

                await db.InitTableAsync<TestObject>();
                var count = await db.Query(tableLookup).Count();
                Assert.True(count > 0);

                await db.DropTableIfExistsAsync<TestObject>();

                count = await db.Query(tableLookup).Count();
                Assert.Equal(count, 0);
            }
        }
    }
}


using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;
using Ignore = SQLitePCL.pretty.Orm.Attributes.IgnoreAttribute;

namespace SQLitePCL.pretty.tests
{
    public partial class TableMappingTests : TestBase
    {
        // Per thread builder to limit the number of instances.
        private static readonly ThreadLocal<TestObject.Builder> testObjectBuilder = new ThreadLocal<TestObject.Builder>(() => new TestObject.Builder());

        public sealed class TestObject : IEquatable<TestObject>
        {
            public class Builder
            {
                private long? id;
                private string value;

                public long? Id { get { return id; } set { this.id = value; } }
                public string Value { get { return value; } set { this.value = value; } }

                public TestObject Build()
                {
                    return new TestObject(id, value);
                }
            }

            private long? id;
            private string value; 

            private TestObject(long? id, string value)
            {
                this.id = id;
                this.value = value;
            }

            [PrimaryKey]
            public long? Id { get { return id; } }

            public string Value { get { return value; } }

            public bool Equals(TestObject other)
            {
                return this.Id == other.Id && 
                    this.Value == other.Value;
            }

            public override bool Equals(object other)
            {
                return other is TestObject && this.Equals((TestObject)other);
            }

            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 31 + this.Id.GetHashCode();
                hash = hash * 31 + this.Value.GetHashCode();

                return hash;
            }
        }

        [CompositeIndex(true, "Id", "Value")]
        public sealed class TestMutableObject
        {
            [PrimaryKey]
            public long? Id { get; set; }

            [Indexed]
            public string Value { get; set; }
        }

        [CompositeIndex(true, "Id", "Value")]
        [Table("TestMutableObject")]
        public sealed class TestMutableObjectUpdated
        {
            [PrimaryKey]
            public long? Id { get; set; }

            [Indexed]
            public string Value { get; set; }

            public int Another { get; set; }

            [NotNull("default")]
            public string NotNullReference { get; set; }
        }

        [Fact]
        public void TestInitTable()
        {
            var table = TableMapping.Get<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                var dbIndexes = db.GetIndexInfo(table.TableName);
                Assert.Equal(table.Indexes, dbIndexes);

                var dbColumns = db.GetTableInfo(table.TableName);
                Assert.Equal(
                    table.Columns.Select(x => new KeyValuePair<string,TableColumnMetadata>(x.Key, x.Value.Metadata)),
                    dbColumns);
            }
        }

        [Fact]
        public void TestInitTableWithMigration()
        {
            var tableOriginal = TableMapping.Get<TestMutableObject>();
            var tableOriginalOrm = Orm.ResultSet.RowToObject<TestMutableObject>();

            var tableNew = TableMapping.Get<TestMutableObjectUpdated>();
            var tableNewOrm = Orm.ResultSet.RowToObject<TestMutableObjectUpdated>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestMutableObject>();
                var dbIndexes = db.GetIndexInfo(tableOriginal.TableName);
                Assert.Equal(tableOriginal.Indexes, dbIndexes);

                var dbColumns = db.GetTableInfo(tableOriginal.TableName);
                Assert.Equal(
                    tableOriginal.Columns.Select(x => new KeyValuePair<string,TableColumnMetadata>(x.Key, x.Value.Metadata)),
                    dbColumns);

                var insertedOriginal = db.InsertOrReplace(new TestMutableObject() { Value = "test" }, tableOriginalOrm);

                db.InitTable<TestMutableObjectUpdated>();
                dbIndexes = db.GetIndexInfo(tableOriginal.TableName);
                Assert.Equal(tableNew.Indexes, dbIndexes);

                dbColumns = db.GetTableInfo(tableOriginal.TableName);
                Assert.Equal(
                    tableNew.Columns.Select(x => new KeyValuePair<string,TableColumnMetadata>(x.Key, x.Value.Metadata)),
                    dbColumns);

                TestMutableObjectUpdated found;
                if (db.TryFind<TestMutableObjectUpdated>(insertedOriginal.Id.Value, tableNewOrm, out found))
                {
                    Assert.Equal(found.NotNullReference, "default");
                }
                else
                { 
                    Assert.True(false);
                }
            }
        }

        [Fact]
        public void TestInsertOrReplace()
        {
            var orm = Orm.ResultSet.RowToObject(
                        () => testObjectBuilder.Value, 
                        o => ((TestObject.Builder)o).Build());
                            
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();

                var inserted = db.InsertOrReplace(new TestObject.Builder() { Value = "Hello" }.Build(), orm);
                var changed = new TestObject.Builder() { Id = inserted.Id, Value = "Goodbye" }.Build();
                var replaced = db.InsertOrReplace(changed, orm);

                Assert.Equal(inserted.Id, replaced.Id);
                Assert.Equal(replaced.Value, "Goodbye");
            }
        }

        [Fact]
        public void TestInsertOrReplaceAll()
        {
            var orm = Orm.ResultSet.RowToObject(
                        () => testObjectBuilder.Value, 
                        o => ((TestObject.Builder)o).Build());

            using (var db = SQLite3.OpenInMemory())
            {
               
                var objects = new List<TestObject>()
                    {
                        new TestObject.Builder() { Value = "Hello1" }.Build(),
                        new TestObject.Builder() { Value = "Hello2" }.Build(),
                        new TestObject.Builder() { Value = "Hello3" }.Build()
                    };

                db.InitTable<TestObject>();
                var result = db.InsertOrReplaceAll(objects, orm);
                Assert.Equal(
                    result.Values.Select(x => x.Value), 
                    objects.Select(x => x.Value));

                // FIXMe: Need to assert all items are unique
                //(result.Values.Select(x => x.Id));

                var changed = new List<TestObject>()
                    {
                        new TestObject.Builder() { Id = result.Values.ElementAt(0).Id, Value = "Goodbye1" }.Build(),
                        new TestObject.Builder() { Id = result.Values.ElementAt(1).Id, Value = "Goodbye2" }.Build(),
                        new TestObject.Builder() { Id = result.Values.ElementAt(2).Id, Value = "Goodye3" }.Build()
                    };
                var replaced = db.InsertOrReplaceAll(changed, orm);
                Assert.Equal(
                    replaced.Values.Select(x => x.Id),
                    result.Values.Select(x => x.Id));
                Assert.Equal(
                    replaced.Values.Select(x => x.Value),
                    changed.Select(x => x.Value));
            }
        }

        [Fact]
        public void TestDelete()
        {
            var orm = Orm.ResultSet.RowToObject(
                        () => testObjectBuilder.Value, 
                        o => ((TestObject.Builder)o).Build());

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();

                var inserted = db.InsertOrReplace(new TestObject.Builder() { Value = "Hello" }.Build(), orm);
                TestObject deleted;
                if (db.TryDelete(inserted.Id.Value, orm, out deleted))
                {
                    Assert.Equal(inserted, deleted);
                }
                else
                {
                    Assert.True(false);
                }

                if (db.TryDelete(1000000, orm, out deleted))
                {
                    Assert.True(false);
                }

                TestObject lookup;
                Assert.False(db.TryFind(inserted.Id.Value, orm, out lookup));
            }
        }

        [Fact]
        public void TestDeleteAll()
        {
            var orm = Orm.ResultSet.RowToObject(
                        () => testObjectBuilder.Value, 
                        o => ((TestObject.Builder)o).Build());

            using (var db = SQLite3.OpenInMemory())
            {
               
                var objects = new List<TestObject>()
                {
                    new TestObject.Builder() { Value = "Hello1" }.Build(),
                    new TestObject.Builder() { Value = "Hello2" }.Build(),
                    new TestObject.Builder() { Value = "Hello3" }.Build()
                };

                db.InitTable<TestObject>();
                var notDeleted = db.InsertOrReplace(new TestObject.Builder() { Value = "NotDeleted" }.Build(), orm);
                var inserted = db.InsertOrReplaceAll(objects, orm);
                var deleted = db.DeleteAll(inserted.Values.Select(x => x.Id.Value), orm);
                Assert.Equal(inserted.Values, deleted.Values);

                var found = db.FindAll(inserted.Values.Select(x => x.Id.Value), orm);
                Assert.Empty(found);

                TestObject found2;
                if (db.TryFind(notDeleted.Id.Value, orm, out found2))
                {
                    Assert.Equal(found2, notDeleted);
                }

                db.DeleteAllRows<TestObject>();

                TestObject notFound;
                Assert.False(db.TryFind(notDeleted.Id.Value, orm, out notFound));
            }
        }

        [Fact]
        public void TestDropTable()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                var tableLookup =
                    @"SELECT name FROM sqlite_master
                      WHERE type='table' AND name='TestObject'
                      ORDER BY name;";

                // Table doesn't exist
                db.DropTableIfExists<TestObject>();

                db.InitTable<TestObject>();
                Assert.True(db.Query(tableLookup).Count() > 0);

                db.DropTableIfExists<TestObject>();
                Assert.Equal(db.Query(tableLookup).Count(), 0);
            }
        }
    }
}


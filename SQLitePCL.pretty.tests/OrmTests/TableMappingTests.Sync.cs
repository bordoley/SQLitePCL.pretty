using NUnit.Framework;
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
    [TestFixture]
    public partial class TableMappingTests
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

        [Test]
        public void TestInitTable()
        {
            var table = TableMapping.Create<TestObject>(
                            () => testObjectBuilder.Value, 
                            o => ((TestObject.Builder)o).Build());

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                var dbIndexes = db.GetIndexInfo(table.TableName);
                CollectionAssert.AreEquivalent(table.Indexes, dbIndexes);

                var dbColumns = db.GetTableInfo(table.TableName);
                CollectionAssert.AreEquivalent(
                    table.Columns.Select(x => new KeyValuePair<string,TableColumnMetadata>(x.Key, x.Value.Metadata)),
                    dbColumns);
            }
        }

        [Test]
        public void TestInitTableWithMigration()
        {
            var tableOriginal = TableMapping.Create<TestMutableObject>();
            var tableNew = TableMapping.Create<TestMutableObjectUpdated>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(tableOriginal);
                var dbIndexes = db.GetIndexInfo(tableOriginal.TableName);
                CollectionAssert.AreEquivalent(tableOriginal.Indexes, dbIndexes);

                var dbColumns = db.GetTableInfo(tableOriginal.TableName);
                CollectionAssert.AreEquivalent(
                    tableOriginal.Columns.Select(x => new KeyValuePair<string,TableColumnMetadata>(x.Key, x.Value.Metadata)),
                    dbColumns);

                var insertedOriginal = db.InsertOrReplace(tableOriginal, new TestMutableObject() { Value = "test" });

                db.InitTable(tableNew);
                dbIndexes = db.GetIndexInfo(tableOriginal.TableName);
                CollectionAssert.AreEquivalent(tableNew.Indexes, dbIndexes);

                dbColumns = db.GetTableInfo(tableOriginal.TableName);
                CollectionAssert.AreEquivalent(
                    tableNew.Columns.Select(x => new KeyValuePair<string,TableColumnMetadata>(x.Key, x.Value.Metadata)),
                    dbColumns);

                TestMutableObjectUpdated found;
                if (db.TryFind<TestMutableObjectUpdated>(tableNew, insertedOriginal.Id.Value, out found))
                {
                    Assert.AreEqual(found.NotNullReference, "default");
                }
                else
                { 
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void TestInsertOrReplace()
        {
            var table = TableMapping.Create<TestObject>(
                            () => testObjectBuilder.Value, 
                            o => ((TestObject.Builder)o).Build());

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var inserted = db.InsertOrReplace(table, new TestObject.Builder() { Value = "Hello" }.Build());
                var changed = new TestObject.Builder() { Id = inserted.Id, Value = "Goodbye" }.Build();
                var replaced = db.InsertOrReplace(table, changed);

                Assert.AreEqual(inserted.Id, replaced.Id);
                Assert.AreEqual(replaced.Value, "Goodbye");
            }
        }

        [Test]
        public void TestInsertOrReplaceAll()
        {
            var table = TableMapping.Create<TestObject>(
                ()=> testObjectBuilder.Value, 
                o => ((TestObject.Builder) o).Build());

            using (var db = SQLite3.OpenInMemory())
            {
               
                var objects = new List<TestObject>()
                    {
                        new TestObject.Builder() { Value = "Hello1" }.Build(),
                        new TestObject.Builder() { Value = "Hello2" }.Build(),
                        new TestObject.Builder() { Value = "Hello3" }.Build()
                    };

                db.InitTable(table);
                var result = db.InsertOrReplaceAll(table, objects);
                CollectionAssert.AreEquivalent(
                    result.Values.Select(x => x.Value), 
                    objects.Select(x => x.Value));
                CollectionAssert.AllItemsAreUnique(result.Values.Select(x => x.Id));

                var changed = new List<TestObject>()
                    {
                        new TestObject.Builder() { Id = result.Values.ElementAt(0).Id, Value = "Goodbye1" }.Build(),
                        new TestObject.Builder() { Id = result.Values.ElementAt(1).Id, Value = "Goodbye2" }.Build(),
                        new TestObject.Builder() { Id = result.Values.ElementAt(2).Id, Value = "Goodye3" }.Build()
                    };
                var replaced = db.InsertOrReplaceAll(table, changed);
                CollectionAssert.AreEquivalent(
                    replaced.Values.Select(x => x.Id),
                    result.Values.Select(x => x.Id));
                CollectionAssert.AreEquivalent(
                    replaced.Values.Select(x => x.Value),
                    changed.Select(x => x.Value));
            }
        }

        [Test]
        public void TestDelete()
        {
            var table = TableMapping.Create<TestObject>(
                            () => testObjectBuilder.Value, 
                            o => ((TestObject.Builder)o).Build());

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var inserted = db.InsertOrReplace(table, new TestObject.Builder() { Value = "Hello" }.Build());
                TestObject deleted;
                if (db.TryDelete(table, inserted.Id.Value, out deleted))
                {
                    Assert.AreEqual(inserted, deleted);
                }
                else
                {
                    Assert.Fail();
                }

                if (db.TryDelete(table, 1000000, out deleted))
                {
                    Assert.Fail();
                }

                TestObject lookup;
                Assert.IsFalse(db.TryFind(table, inserted.Id.Value, out lookup));
            }
        }

        [Test]
        public void TestDeleteAll()
        {
            var table = TableMapping.Create<TestObject>(
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

                db.InitTable(table);
                var notDeleted = db.InsertOrReplace(table, new TestObject.Builder() { Value = "NotDeleted" }.Build());
                var inserted = db.InsertOrReplaceAll(table, objects);
                var deleted = db.DeleteAll(table, inserted.Values.Select(x => x.Id.Value));
                CollectionAssert.AreEquivalent(inserted.Values, deleted.Values);

                var found = db.FindAll(table, inserted.Values.Select(x => x.Id.Value));
                Assert.IsEmpty(found);

                TestObject found2;
                if (db.TryFind(table, notDeleted.Id.Value, out found2))
                {
                    Assert.AreEqual(found2, notDeleted);
                }

                db.DeleteAllRows(table);

                TestObject notFound;
                Assert.IsFalse(db.TryFind(table, notDeleted.Id.Value, out notFound));
            }
        }

        [Test]
        public void TestDropTable()
        {
            var table = TableMapping.Create<TestObject>(
                            () => testObjectBuilder.Value, 
                            o => ((TestObject.Builder)o).Build());

            using (var db = SQLite3.OpenInMemory())
            {
                var tableLookup =
                    @"SELECT name FROM sqlite_master
                      WHERE type='table' AND name='TestObject'
                      ORDER BY name;";

                // Table doesn't exist
                db.DropTableIfExists(table);

                db.InitTable(table);
                Assert.Greater(db.Query(tableLookup).Count(), 0);

                db.DropTableIfExists(table);
                Assert.AreEqual(db.Query(tableLookup).Count(), 0);
            }
        }
    }
}


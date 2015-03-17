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

                var insertedOriginal = db.Insert(tableOriginal, new TestMutableObject() { Value = "test" });

                db.InitTable(tableNew);
                dbIndexes = db.GetIndexInfo(tableOriginal.TableName);
                CollectionAssert.AreEquivalent(tableNew.Indexes, dbIndexes);

                dbColumns = db.GetTableInfo(tableOriginal.TableName);
                CollectionAssert.AreEquivalent(
                    tableNew.Columns.Select(x => new KeyValuePair<string,TableColumnMetadata>(x.Key, x.Value.Metadata)),
                    dbColumns);

                TestMutableObjectUpdated found;
                if (db.TryFind<TestMutableObjectUpdated>(tableNew, insertedOriginal.Id, out found))
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
        public void TestInsert()
        {
            var table = TableMapping.Create<TestObject>(
                            () => testObjectBuilder.Value, 
                            o => ((TestObject.Builder)o).Build());

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                var hello = db.Insert(table, new TestObject.Builder() { Value = "Hello" }.Build());
                Assert.IsNotNull(hello.Id);
                Assert.AreEqual(hello.Value, "Hello");

                TestObject lookupHello; 
                if (db.TryFind(table, hello.Id, out lookupHello))
                {
                    Assert.AreEqual(hello, lookupHello);
                }
                else
                {
                    Assert.Fail();
                }

                Assert.Throws<SQLiteException>(() => db.Insert(table, hello));

                var objectWithId = new TestObject.Builder() { Id = 100, Value = "Hello" }.Build();
                Assert.AreEqual(objectWithId, db.Insert(table, objectWithId));
            }

            var mutableTable = TableMapping.Create<TestMutableObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(mutableTable);
                var hello = db.Insert(mutableTable, new TestMutableObject() { Value = "Hello" });
                Assert.IsNotNull(hello.Id);
                Assert.AreEqual(hello.Value, "Hello");

                TestMutableObject lookupHello; 
                if (db.TryFind(mutableTable, hello.Id, out lookupHello))
                {
                    Assert.AreEqual(hello.Id, lookupHello.Id);
                    Assert.AreEqual(hello.Value, lookupHello.Value);
                }
                else
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void TestInsertAll()
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
                var result = db.InsertAll(table, objects);
                CollectionAssert.AreEquivalent(
                    result.Select(x => x.Value), 
                    objects.Select(x => x.Value));
                CollectionAssert.AllItemsAreUnique(result.Select(x => x.Id));

                Assert.Throws<SQLiteException>(() => db.InsertAll(table, result));
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

                var inserted = db.Insert(table, new TestObject.Builder() { Value = "Hello" }.Build());
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
                var result = db.InsertAll(table, objects);
                CollectionAssert.AreEquivalent(
                    result.Select(x => x.Value), 
                    objects.Select(x => x.Value));
                CollectionAssert.AllItemsAreUnique(result.Select(x => x.Id));

                var changed = new List<TestObject>()
                    {
                        new TestObject.Builder() { Id = result[0].Id, Value = "Goodbye1" }.Build(),
                        new TestObject.Builder() { Id = result[1].Id, Value = "Goodbye2" }.Build(),
                        new TestObject.Builder() { Id = result[2].Id, Value = "Goodye3" }.Build()
                    };
                var replaced = db.InsertOrReplaceAll(table, changed);
                CollectionAssert.AreEquivalent(
                    replaced.Select(x => x.Id),
                    result.Select(x => x.Id));
                CollectionAssert.AreEquivalent(
                    replaced.Select(x => x.Value),
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

                var inserted = db.Insert(table, new TestObject.Builder() { Value = "Hello" }.Build());
                var deleted = db.Delete(table, inserted);
                Assert.AreEqual(inserted, deleted);

                TestObject lookup;
                Assert.IsFalse(db.TryFind(table, inserted.Id, out lookup));
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
                var notDeleted = db.Insert(table, new TestObject.Builder() { Value = "NotDeleted" }.Build());
                var inserted = db.InsertAll(table, objects);
                var deleted = db.DeleteAll(table, inserted);
                CollectionAssert.AreEquivalent(inserted, deleted);

                var found = db.FindAll(table, inserted.Select(x => x.Id));
                foreach (var o in found)
                {
                    Assert.IsNull(o);
                }

                TestObject found2;
                if (db.TryFind(table, notDeleted.Id, out found2))
                {
                    Assert.AreEqual(found2, notDeleted);
                }

                db.DeleteAll(table);

                TestObject notFound;
                Assert.IsFalse(db.TryFind(table, notDeleted.Id, out notFound));
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

                db.InitTable(table);
                Assert.Greater(db.Query(tableLookup).Count(), 0);

                db.DropTable(table);
                Assert.AreEqual(db.Query(tableLookup).Count(), 0);
            }
        }
    }
}


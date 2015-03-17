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

                db.InitTable(tableNew);
                dbIndexes = db.GetIndexInfo(tableOriginal.TableName);
                CollectionAssert.AreEquivalent(tableNew.Indexes, dbIndexes);

                dbColumns = db.GetTableInfo(tableOriginal.TableName);
                CollectionAssert.AreEquivalent(
                    tableNew.Columns.Select(x => new KeyValuePair<string,TableColumnMetadata>(x.Key, x.Value.Metadata)),
                    dbColumns);
            }
        }

        [Test]
        public void TestInsert()
        {
            var table = TableMapping.Create<TestObject>(
                ()=> testObjectBuilder.Value, 
                o => ((TestObject.Builder) o).Build());

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                var hello = db.Insert(table, new TestObject.Builder() { Value = "Hello" }.Build());
                Assert.IsNotNull(hello.Id);
                Assert.AreEqual(hello.Value, "Hello");

                var lookupHello = db.Find(table, hello.Id);
                Assert.AreEqual(hello, lookupHello);
            }

            var mutableTable = TableMapping.Create<TestMutableObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(mutableTable);
                var hello = db.Insert(mutableTable, new TestMutableObject() { Value = "Hello" });
                Assert.IsNotNull(hello.Id);
                Assert.AreEqual(hello.Value, "Hello");

                var lookupHello = db.Find(mutableTable, hello.Id);
                Assert.AreEqual(hello.Id, lookupHello.Id);
                Assert.AreEqual(hello.Value, lookupHello.Value);
            }
        }
    }
}


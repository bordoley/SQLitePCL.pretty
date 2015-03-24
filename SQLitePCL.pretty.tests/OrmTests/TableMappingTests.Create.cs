using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;
using Ignore = SQLitePCL.pretty.Orm.Attributes.IgnoreAttribute;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public partial class TableMappingTests
    {
        [CompositeIndex("NotNull", "Collated")]
        [CompositeIndex("named", true, "Uri", "B")]
        public class TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName
        {
            [PrimaryKey]
            public long? Id { get; set; }

            [Indexed()]
            public Uri Uri { get; set; }

            [Column("B")]
            [Indexed(true)]
            public string A { get; set; }

            [Ignore]
            public Stream Ignored { get; set; }

            [NotNull("")]
            public byte[] NotNull { get; set; }

            [Collation("Fancy Collation")]
            public string Collated { get; set; }

            public DateTime Value { get; set; }

            public float AFloat { get; set; }

            public int? NullableInt { get; set; }
        }

        [Test]
        public void TestCreate()
        {
            var table = TableMapping.Create<TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName>();

            Assert.AreEqual(table.TableName, "TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName");

            var expectedColumns = new string[] { "Id", "Uri", "B", "NotNull", "Collated", "Value", "AFloat", "NullableInt" };
            CollectionAssert.AreEquivalent(expectedColumns, table.Columns.Keys);

            var expectedIndexes = new Dictionary<string,IndexInfo> 
                {
                    { "TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName_Uri", new IndexInfo(false, new string[] { "Uri" }) },
                    { "TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName_B",   new IndexInfo(true, new string[] { "B" }) },
                    { "TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName_NotNull_Collated", new IndexInfo(false, new string[] { "NotNull", "Collated" }) },
                    { "named", new IndexInfo(true, new string[] { "Uri", "B"}) }
                };

            var indexes = table.Indexes;
            CollectionAssert.AreEquivalent(indexes, expectedIndexes);

            var idMapping = table.Columns["Id"];
            // Subtle touch, but nullables are ignored in the CLR type.
            Assert.AreEqual(idMapping.ClrType, typeof(Nullable<long>));
            // No way to test the PropertyInfo directly
            Assert.AreEqual(idMapping.Metadata.CollationSequence, "BINARY");
            Assert.AreEqual(idMapping.Metadata.DeclaredType, "INTEGER");
            Assert.IsTrue(idMapping.Metadata.HasNotNullConstraint);
            Assert.IsTrue(idMapping.Metadata.IsAutoIncrement);
            Assert.IsTrue(idMapping.Metadata.IsPrimaryKeyPart);

            var uriMapping = table.Columns["Uri"];
            Assert.AreEqual(uriMapping.ClrType, typeof(Uri));
            // No way to test the PropertyInfo directly
            Assert.AreEqual(uriMapping.Metadata.CollationSequence, "BINARY");
            Assert.AreEqual(uriMapping.Metadata.DeclaredType, "TEXT");
            Assert.IsFalse(uriMapping.Metadata.HasNotNullConstraint);
            Assert.IsFalse(uriMapping.Metadata.IsAutoIncrement);
            Assert.IsFalse(uriMapping.Metadata.IsPrimaryKeyPart);

            var bMapping = table.Columns["B"];
            Assert.AreEqual(bMapping.ClrType, typeof(string));
            // No way to test the PropertyInfo directly
            Assert.AreEqual(bMapping.Metadata.CollationSequence, "BINARY");
            Assert.AreEqual(bMapping.Metadata.DeclaredType, "TEXT");
            Assert.IsFalse(bMapping.Metadata.HasNotNullConstraint);
            Assert.IsFalse(bMapping.Metadata.IsAutoIncrement);
            Assert.IsFalse(bMapping.Metadata.IsPrimaryKeyPart);

            Assert.False(table.Columns.ContainsKey("Ignored"));

            var notNullMapping = table.Columns["NotNull"];
            Assert.AreEqual(notNullMapping.ClrType, typeof(byte[]));
            // No way to test the PropertyInfo directly
            Assert.AreEqual(notNullMapping.Metadata.CollationSequence, "BINARY");
            Assert.AreEqual(notNullMapping.Metadata.DeclaredType, "BLOB");
            Assert.IsTrue(notNullMapping.Metadata.HasNotNullConstraint);
            Assert.IsFalse(notNullMapping.Metadata.IsAutoIncrement);
            Assert.IsFalse(notNullMapping.Metadata.IsPrimaryKeyPart);

            var collatedMapping = table.Columns["Collated"];
            Assert.AreEqual(collatedMapping.ClrType, typeof(string));
            // No way to test the PropertyInfo directly
            Assert.AreEqual(collatedMapping.Metadata.CollationSequence, "Fancy Collation");
            Assert.AreEqual(collatedMapping.Metadata.DeclaredType, "TEXT");
            Assert.IsFalse(collatedMapping.Metadata.HasNotNullConstraint);
            Assert.IsFalse(collatedMapping.Metadata.IsAutoIncrement);
            Assert.IsFalse(collatedMapping.Metadata.IsPrimaryKeyPart);

            var valueMapping = table.Columns["Value"];
            Assert.AreEqual(valueMapping.ClrType, typeof(DateTime));
            // No way to test the PropertyInfo directly
            Assert.AreEqual(valueMapping.Metadata.CollationSequence, "BINARY");
            Assert.AreEqual(valueMapping.Metadata.DeclaredType, "INTEGER");
            Assert.IsTrue(valueMapping.Metadata.HasNotNullConstraint);
            Assert.IsFalse(valueMapping.Metadata.IsAutoIncrement);
            Assert.IsFalse(valueMapping.Metadata.IsPrimaryKeyPart);

            var aFloatMapping = table.Columns["AFloat"];
            Assert.AreEqual(aFloatMapping.ClrType, typeof(float));
            // No way to test the PropertyInfo directly
            Assert.AreEqual(aFloatMapping.Metadata.CollationSequence, "BINARY");
            Assert.AreEqual(aFloatMapping.Metadata.DeclaredType, "REAL");
            Assert.IsTrue(aFloatMapping.Metadata.HasNotNullConstraint);
            Assert.IsFalse(aFloatMapping.Metadata.IsAutoIncrement);
            Assert.IsFalse(aFloatMapping.Metadata.IsPrimaryKeyPart);

            var nullable = table.Columns["NullableInt"];
            Assert.AreEqual(nullable.ClrType, typeof(Nullable<int>));
            // No way to test the PropertyInfo directly
            Assert.AreEqual(nullable.Metadata.CollationSequence, "BINARY");
            Assert.AreEqual(nullable.Metadata.DeclaredType, "INTEGER");
            Assert.IsFalse(nullable.Metadata.HasNotNullConstraint);
            Assert.IsFalse(nullable.Metadata.IsAutoIncrement);
            Assert.IsFalse(nullable.Metadata.IsPrimaryKeyPart);
        }

        [Table("ExplicitTableName")]
        public class TestObjectWithExplicitTableName
        {
            [PrimaryKey]
            public long? Id { get; set; }
        }

        [Test]
        public void TestCreateWithExplicitTableName()
        {
            var tableWithExplicitName = TableMapping.Create<TestObjectWithExplicitTableName>();
            Assert.AreEqual(tableWithExplicitName.TableName, "ExplicitTableName");
        }

        public class TestObjectWithNonAutoIncrementPrimaryKey
        {
            [PrimaryKey]
            public long Id { get; set; }
        }

        [Test]
        public void TestCreateWithNonAutoIncrementPrimaryKey()
        {
            var table = TableMapping.Create<TestObjectWithNonAutoIncrementPrimaryKey>();

            var id = table.Columns["Id"];
            Assert.AreEqual(id.ClrType, typeof(long));
            // No way to test the PropertyInfo directly
            Assert.AreEqual(id.Metadata.CollationSequence, "BINARY");
            Assert.AreEqual(id.Metadata.DeclaredType, "INTEGER");
            Assert.IsTrue(id.Metadata.HasNotNullConstraint);
            Assert.IsFalse(id.Metadata.IsAutoIncrement);
            Assert.IsTrue(id.Metadata.IsPrimaryKeyPart);
        }

        public class TestObjectWithUnsupportedPropertyType
        {
            [PrimaryKey]
            public long? Id { get; set; }

            public object Unsupported { get; set; }
        }

        public class TestObjectWithNoPrimaryKey
        {
        }

        [CompositeIndex("id", "a")] 
        public class TestObjectWithBadCompositeIndex
        {
            [PrimaryKey]
            public long? Id { get; set; }
        }

        public class TestObjectWithBadCompositePrimaryKey
        {
            [PrimaryKey]
            public long? Id { get; set; }

            [PrimaryKey]
            public long? Id2 { get; set; }
        }

        public class TestObjectWithBadNullableDateTimePrimaryKey
        {
            [PrimaryKey]
            public DateTime? Id { get; set; }
        }

        [Test]
        public void TestCreateWithInvalidTableTypes()
        {
            Assert.Throws<NotSupportedException>(() => TableMapping.Create<TestObjectWithUnsupportedPropertyType>());
            Assert.Throws<ArgumentException>(() => TableMapping.Create<TestObjectWithNoPrimaryKey>());
            Assert.Throws<ArgumentException>(() => TableMapping.Create<TestObjectWithBadCompositeIndex>());
            Assert.Throws<ArgumentException>(() => TableMapping.Create<TestObjectWithBadCompositePrimaryKey>());
            Assert.Throws<ArgumentException>(() => TableMapping.Create<TestObjectWithBadNullableDateTimePrimaryKey>());
        }


        public class TestParentObject
        {
            [PrimaryKey]
            public long Id { get; set; }
        }

        public class TestChildObject
        {
            [PrimaryKey]
            public long Id { get; set; }

            [ForeignKey(typeof(TestParentObject))]
            public long ParentId { get; set; }
        }

        public class TestBadChildObject
        {
            [PrimaryKey]
            public long Id { get; set; }

            [ForeignKey(typeof(TestParentObject))]
            public string ParentId { get; set; }
        }

        [Test]
        public void TestForeignKeyConstraints()
        {
            var childTableSelector = Orm.Orm.ResultSetRowToObject<TestChildObject>();
            var parentTableSelector = Orm.Orm.ResultSetRowToObject<TestParentObject>();

            Assert.Throws<ArgumentException>(() => TableMapping.Create<TestBadChildObject>());

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestChildObject>();
                db.InitTable<TestParentObject>();

                // Foreign Key constraint
                Assert.Throws<SQLiteException>(() => db.InsertOrReplace(new TestChildObject(){ ParentId = 1 }, childTableSelector));
                Assert.AreEqual(db.Query("SELECT count(*) FROM " + TableMapping.Create<TestChildObject>().TableName).SelectScalarInt().First(), 0);

                db.InsertOrReplace(new TestParentObject() { Id = 100 }, parentTableSelector);
                db.InsertOrReplace(new TestChildObject() { ParentId = 100 }, childTableSelector);
                Assert.AreEqual(db.Query("SELECT count(*) FROM " + TableMapping.Create<TestChildObject>().TableName).SelectScalarInt().First(), 1);

                // Foreign Key Constraint causes delete to fail
                TestParentObject deleted;
                Assert.Throws<SQLiteException>(() => db.TryDelete(100, parentTableSelector, out deleted));
            }
        } 
    }
}


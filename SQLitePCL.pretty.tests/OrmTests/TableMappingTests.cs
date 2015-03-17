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
    public class TableMappingTests
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

            [NotNull]
            public byte[] NotNull { get; set; }

            [Collation("Fancy Collation")]
            public string Collated { get; set; }

            public DateTime Value { get; set; }

            public float AFloat { get; set; }
        }

        [Test]
        public void TestCreate()
        {
            var table = TableMapping.Create<TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName>();

            Assert.AreEqual(table.TableName, "TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName");

            var expectedColumns = new string[] { "Id", "Uri", "B", "NotNull", "Collated", "Value", "AFloat" };
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
            Assert.AreEqual(idMapping.ClrType, typeof(long));
            // No way to test the PropertyInfo directly
            Assert.IsTrue(idMapping.Metadata.CollationSequence.Length == 0);
            Assert.AreEqual(idMapping.Metadata.DeclaredType, "INTEGER");
            Assert.IsTrue(idMapping.Metadata.HasNotNullConstraint);
            Assert.IsTrue(idMapping.Metadata.IsAutoIncrement);
            Assert.IsTrue(idMapping.Metadata.IsPrimaryKeyPart);

            var uriMapping = table.Columns["Uri"];
            Assert.AreEqual(uriMapping.ClrType, typeof(Uri));
            // No way to test the PropertyInfo directly
            Assert.IsTrue(uriMapping.Metadata.CollationSequence.Length == 0);
            Assert.AreEqual(uriMapping.Metadata.DeclaredType, "TEXT");
            Assert.IsFalse(uriMapping.Metadata.HasNotNullConstraint);
            Assert.IsFalse(uriMapping.Metadata.IsAutoIncrement);
            Assert.IsFalse(uriMapping.Metadata.IsPrimaryKeyPart);

            var bMapping = table.Columns["B"];
            Assert.AreEqual(bMapping.ClrType, typeof(string));
            // No way to test the PropertyInfo directly
            Assert.IsTrue(bMapping.Metadata.CollationSequence.Length == 0);
            Assert.AreEqual(bMapping.Metadata.DeclaredType, "TEXT");
            Assert.IsFalse(bMapping.Metadata.HasNotNullConstraint);
            Assert.IsFalse(bMapping.Metadata.IsAutoIncrement);
            Assert.IsFalse(bMapping.Metadata.IsPrimaryKeyPart);

            Assert.False(table.Columns.ContainsKey("Ignored"));

            var notNullMapping = table.Columns["NotNull"];
            Assert.AreEqual(notNullMapping.ClrType, typeof(byte[]));
            // No way to test the PropertyInfo directly
            Assert.IsTrue(notNullMapping.Metadata.CollationSequence.Length == 0);
            Assert.AreEqual(notNullMapping.Metadata.DeclaredType, "BLOB");
            Assert.IsTrue(notNullMapping.Metadata.HasNotNullConstraint);
            Assert.IsFalse(notNullMapping.Metadata.IsAutoIncrement);
            Assert.IsFalse(notNullMapping.Metadata.IsPrimaryKeyPart);

            var valueMapping = table.Columns["Value"];
            Assert.AreEqual(valueMapping.ClrType, typeof(DateTime));
            // No way to test the PropertyInfo directly
            Assert.IsTrue(valueMapping.Metadata.CollationSequence.Length == 0);
            Assert.AreEqual(valueMapping.Metadata.DeclaredType, "INTEGER");
            Assert.IsTrue(valueMapping.Metadata.HasNotNullConstraint);
            Assert.IsFalse(valueMapping.Metadata.IsAutoIncrement);
            Assert.IsFalse(valueMapping.Metadata.IsPrimaryKeyPart);

            var aFloatMapping = table.Columns["AFloat"];
            Assert.AreEqual(aFloatMapping.ClrType, typeof(float));
            // No way to test the PropertyInfo directly
            Assert.IsTrue(aFloatMapping.Metadata.CollationSequence.Length == 0);
            Assert.AreEqual(aFloatMapping.Metadata.DeclaredType, "REAL");
            Assert.IsTrue(aFloatMapping.Metadata.HasNotNullConstraint);
            Assert.IsFalse(aFloatMapping.Metadata.IsAutoIncrement);
            Assert.IsFalse(aFloatMapping.Metadata.IsPrimaryKeyPart);
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

        public class TestObjectWithMultiplePrimaryKeys
        {
            [PrimaryKey]
            public int PrimaryKey1 { get; set;}

            [PrimaryKey]
            public string PrimaryKey2 { get; set;}

            [PrimaryKey]
            public DateTime PrimaryKey3 { get; set;}
        }

        [Test]
        public void TestCreateWithMultiplePrimaryKeyColumns()
        {   
            var table = TableMapping.Create<TestObjectWithMultiplePrimaryKeys>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
            }
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

        [Test]
        public void TestCreateWithInvalidTableTypes()
        {
            Assert.Throws<NotSupportedException>(() => TableMapping.Create<TestObjectWithUnsupportedPropertyType>());
            Assert.Throws<ArgumentException>(() => TableMapping.Create<TestObjectWithNoPrimaryKey>());
        }

    }
}


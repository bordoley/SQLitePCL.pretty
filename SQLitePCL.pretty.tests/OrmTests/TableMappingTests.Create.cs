using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.tests
{
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

        [Fact]
        public void TestCreate()
        {
            var table = TableMapping.Get<TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName>();

            Assert.Equal(table.TableName, "TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName");

            var expectedColumns = new string[] { "Id", "Uri", "B", "NotNull", "Collated", "Value", "AFloat", "NullableInt" };
            Assert.Equal(expectedColumns, table.Columns.Keys);

            var expectedIndexes = new Dictionary<string,IndexInfo> 
                {
                    { "TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName_Uri", new IndexInfo(false, new string[] { "Uri" }) },
                    { "TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName_B",   new IndexInfo(true, new string[] { "B" }) },
                    { "TestObjectWithAutoIncrementPrimaryKeyAndDefaultTableName_NotNull_Collated", new IndexInfo(false, new string[] { "NotNull", "Collated" }) },
                    { "named", new IndexInfo(true, new string[] { "Uri", "B"}) }
                };

            var indexes = table.Indexes;
            Assert.Equal(indexes, expectedIndexes);

            var idMapping = table.Columns["Id"];
            // Subtle touch, but nullables are ignored in the CLR type.
            Assert.Equal(idMapping.ClrType, typeof(Nullable<long>));
            // No way to test the PropertyInfo directly
            Assert.Equal(idMapping.Metadata.CollationSequence, "BINARY");
            Assert.Equal(idMapping.Metadata.DeclaredType, "INTEGER");
            Assert.True(idMapping.Metadata.HasNotNullConstraint);
            Assert.True(idMapping.Metadata.IsAutoIncrement);
            Assert.True(idMapping.Metadata.IsPrimaryKeyPart);

            var uriMapping = table.Columns["Uri"];
            Assert.Equal(uriMapping.ClrType, typeof(Uri));
            // No way to test the PropertyInfo directly
            Assert.Equal(uriMapping.Metadata.CollationSequence, "BINARY");
            Assert.Equal(uriMapping.Metadata.DeclaredType, "TEXT");
            Assert.False(uriMapping.Metadata.HasNotNullConstraint);
            Assert.False(uriMapping.Metadata.IsAutoIncrement);
            Assert.False(uriMapping.Metadata.IsPrimaryKeyPart);

            var bMapping = table.Columns["B"];
            Assert.Equal(bMapping.ClrType, typeof(string));
            // No way to test the PropertyInfo directly
            Assert.Equal(bMapping.Metadata.CollationSequence, "BINARY");
            Assert.Equal(bMapping.Metadata.DeclaredType, "TEXT");
            Assert.False(bMapping.Metadata.HasNotNullConstraint);
            Assert.False(bMapping.Metadata.IsAutoIncrement);
            Assert.False(bMapping.Metadata.IsPrimaryKeyPart);

            Assert.False(table.Columns.ContainsKey("Ignored"));

            var notNullMapping = table.Columns["NotNull"];
            Assert.Equal(notNullMapping.ClrType, typeof(byte[]));
            // No way to test the PropertyInfo directly
            Assert.Equal(notNullMapping.Metadata.CollationSequence, "BINARY");
            Assert.Equal(notNullMapping.Metadata.DeclaredType, "BLOB");
            Assert.True(notNullMapping.Metadata.HasNotNullConstraint);
            Assert.False(notNullMapping.Metadata.IsAutoIncrement);
            Assert.False(notNullMapping.Metadata.IsPrimaryKeyPart);

            var collatedMapping = table.Columns["Collated"];
            Assert.Equal(collatedMapping.ClrType, typeof(string));
            // No way to test the PropertyInfo directly
            Assert.Equal(collatedMapping.Metadata.CollationSequence, "Fancy Collation");
            Assert.Equal(collatedMapping.Metadata.DeclaredType, "TEXT");
            Assert.False(collatedMapping.Metadata.HasNotNullConstraint);
            Assert.False(collatedMapping.Metadata.IsAutoIncrement);
            Assert.False(collatedMapping.Metadata.IsPrimaryKeyPart);

            var valueMapping = table.Columns["Value"];
            Assert.Equal(valueMapping.ClrType, typeof(DateTime));
            // No way to test the PropertyInfo directly
            Assert.Equal(valueMapping.Metadata.CollationSequence, "BINARY");
            Assert.Equal(valueMapping.Metadata.DeclaredType, "INTEGER");
            Assert.True(valueMapping.Metadata.HasNotNullConstraint);
            Assert.False(valueMapping.Metadata.IsAutoIncrement);
            Assert.False(valueMapping.Metadata.IsPrimaryKeyPart);

            var aFloatMapping = table.Columns["AFloat"];
            Assert.Equal(aFloatMapping.ClrType, typeof(float));
            // No way to test the PropertyInfo directly
            Assert.Equal(aFloatMapping.Metadata.CollationSequence, "BINARY");
            Assert.Equal(aFloatMapping.Metadata.DeclaredType, "REAL");
            Assert.True(aFloatMapping.Metadata.HasNotNullConstraint);
            Assert.False(aFloatMapping.Metadata.IsAutoIncrement);
            Assert.False(aFloatMapping.Metadata.IsPrimaryKeyPart);

            var nullable = table.Columns["NullableInt"];
            Assert.Equal(nullable.ClrType, typeof(Nullable<int>));
            // No way to test the PropertyInfo directly
            Assert.Equal(nullable.Metadata.CollationSequence, "BINARY");
            Assert.Equal(nullable.Metadata.DeclaredType, "INTEGER");
            Assert.False(nullable.Metadata.HasNotNullConstraint);
            Assert.False(nullable.Metadata.IsAutoIncrement);
            Assert.False(nullable.Metadata.IsPrimaryKeyPart);
        }

        [Table("ExplicitTableName")]
        public class TestObjectWithExplicitTableName
        {
            [PrimaryKey]
            public long? Id { get; set; }
        }

        [Fact]
        public void TestCreateWithExplicitTableName()
        {
            var tableWithExplicitName = TableMapping.Get<TestObjectWithExplicitTableName>();
            Assert.Equal(tableWithExplicitName.TableName, "ExplicitTableName");
        }

        public class TestObjectWithNonAutoIncrementPrimaryKey
        {
            [PrimaryKey]
            public long Id { get; set; }
        }

        [Fact]
        public void TestCreateWithNonAutoIncrementPrimaryKey()
        {
            var table = TableMapping.Get<TestObjectWithNonAutoIncrementPrimaryKey>();

            var id = table.Columns["Id"];
            Assert.Equal(id.ClrType, typeof(long));
            // No way to test the PropertyInfo directly
            Assert.Equal(id.Metadata.CollationSequence, "BINARY");
            Assert.Equal(id.Metadata.DeclaredType, "INTEGER");
            Assert.True(id.Metadata.HasNotNullConstraint);
            Assert.False(id.Metadata.IsAutoIncrement);
            Assert.True(id.Metadata.IsPrimaryKeyPart);
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

        [Fact]
        public void TestCreateWithInvalidTableTypes()
        {
            Assert.Throws<NotSupportedException>(() => TableMapping.Get<TestObjectWithUnsupportedPropertyType>());
            Assert.Throws<ArgumentException>(() => TableMapping.Get<TestObjectWithNoPrimaryKey>());
            Assert.Throws<ArgumentException>(() => TableMapping.Get<TestObjectWithBadCompositeIndex>());
            Assert.Throws<ArgumentException>(() => TableMapping.Get<TestObjectWithBadCompositePrimaryKey>());
            Assert.Throws<ArgumentException>(() => TableMapping.Get<TestObjectWithBadNullableDateTimePrimaryKey>());
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

        [Fact]
        public void TestForeignKeyConstraints()
        {
            var childTableSelector = Orm.ResultSet.RowToObject<TestChildObject>();
            var parentTableSelector = Orm.ResultSet.RowToObject<TestParentObject>();

            Assert.Throws<ArgumentException>(() => TableMapping.Get<TestBadChildObject>());

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestChildObject>();
                db.InitTable<TestParentObject>();

                // Foreign Key constraint
                Assert.Throws<SQLiteException>(() => db.InsertOrReplace(new TestChildObject(){ ParentId = 1 }, childTableSelector));
                Assert.Equal(db.Query("SELECT count(*) FROM " + TableMapping.Get<TestChildObject>().TableName).SelectScalarInt().First(), 0);

                db.InsertOrReplace(new TestParentObject() { Id = 100 }, parentTableSelector);
                db.InsertOrReplace(new TestChildObject() { ParentId = 100 }, childTableSelector);
                Assert.Equal(db.Query("SELECT count(*) FROM " + TableMapping.Get<TestChildObject>().TableName).SelectScalarInt().First(), 1);

                // Foreign Key Constraint causes delete to fail
                TestParentObject deleted;
                Assert.Throws<SQLiteException>(() => db.TryDelete(100, parentTableSelector, out deleted));
            }
        } 
    }
}


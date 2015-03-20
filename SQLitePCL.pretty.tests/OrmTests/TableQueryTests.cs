using NUnit.Framework;
using System;
using System.Linq;

using SQLitePCL.pretty;
using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class TableQueryTests
    {
        public class TestWhereObject
        {
            [PrimaryKey]
            public long? Id { get; set; }
            public bool Flag { get; set; }
            public int Cost { get; set;}
            public String Name { get; set; }
        }

        [Test]
        public void TestWhereBoolean()
        {
            var table = TableMapping.Create<TestWhereObject>();
            var whereFlagEqualsQuery = table.Query().Where(x => x.Flag == default(bool));

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var objects = Enumerable.Range(0, 10).Select(i => new TestWhereObject() { Flag = (i % 3 == 0), Name = String.Format("TestObject{0}", i) });
                db.InsertOrReplaceAll(table, objects);

                using (var countStmt = db.PrepareCountStatement(whereFlagEqualsQuery))
                {
                    Assert.AreEqual(4, countStmt.Query(true).SelectScalarInt().First());
                    Assert.AreEqual(6, countStmt.Query(false).SelectScalarInt().First());
                }     
            }
        }

        [Test]
        public void TestWhereNot()
        {
            var table = TableMapping.Create<TestWhereObject>();
            var whereFlagEqualsQuery = table.Query().Where(x => x.Flag == default(bool));

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var objects = Enumerable.Range(0, 10).Select(i => new TestWhereObject() { Flag = (i % 3 == 0), Name = String.Format("TestObject{0}", i) });
                db.InsertOrReplaceAll(table, objects);

                using (var countStmt = db.PrepareCountStatement(whereFlagEqualsQuery))
                {
                    Assert.AreEqual(4, countStmt.Query(true).SelectScalarInt().First());
                    Assert.AreEqual(6, countStmt.Query(false).SelectScalarInt().First());
                }     
            }
        }

        [Test]
        public void TestWhereGreaterThan()
        {
            var table = TableMapping.Create<TestWhereObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
            
                db.InsertOrReplace(table, new TestWhereObject(){ Cost = 20, Name = "A" });
                db.InsertOrReplace(table, new TestWhereObject(){ Cost = 10 });
            
                Assert.AreEqual(2, db.Count(table.Query()));

                var query = table.Query().Where(p => p.Cost > default(int));
                var r = db.Query(query, 15).ToList();
                Assert.AreEqual(1, r.Count);
                Assert.AreEqual("A", r[0].Name);
            }
        }

        public class TestCollationObject
        {
            [PrimaryKey]
            public long? Id { get; set; }
            
            public string CollateDefault { get; set; }
            
            [Collation("BINARY")]
            public string CollateBinary { get; set; }
            
            [Collation("RTRIM")]
            public string CollateRTrim { get; set; }
            
            [Collation("NOCASE")]
            public string CollateNoCase { get; set; }
        }

        [Test]
        public void TestCollate()
        {
            var obj = new TestCollationObject() {
                CollateDefault = "Alpha ",
                CollateBinary = "Alpha ",
                CollateRTrim = "Alpha ",
                CollateNoCase = "Alpha ",
            };

            var table = TableMapping.Create<TestCollationObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, obj);  

                using (var colDefaultStmt = db.PrepareStatement(table.Query().Where(o => o.CollateDefault == default(string))))
                using (var colBinaryStmt = db.PrepareStatement(table.Query().Where(o => o.CollateBinary == default(string))))
                using (var colRTrimStmt = db.PrepareStatement(table.Query().Where(o => o.CollateRTrim == default(string))))
                using (var colNoCaseStmt = db.PrepareStatement(table.Query().Where(o => o.CollateNoCase == default(string))))
                {
                    Assert.AreEqual(1, colDefaultStmt.Query("Alpha ").Count());
                    Assert.AreEqual(0, colDefaultStmt.Query("ALPHA ").Count());
                    Assert.AreEqual(0, colDefaultStmt.Query("Alpha").Count());
                    Assert.AreEqual(0, colDefaultStmt.Query("ALPHA").Count());

                    Assert.AreEqual(1, colBinaryStmt.Query("Alpha ").Count());
                    Assert.AreEqual(0, colBinaryStmt.Query("ALPHA ").Count());
                    Assert.AreEqual(0, colBinaryStmt.Query("Alpha").Count());
                    Assert.AreEqual(0, colBinaryStmt.Query("ALPHA").Count());

                    Assert.AreEqual(1, colRTrimStmt.Query("Alpha ").Count());
                    Assert.AreEqual(0, colRTrimStmt.Query("ALPHA ").Count());
                    Assert.AreEqual(1, colRTrimStmt.Query("Alpha").Count());
                    Assert.AreEqual(0, colRTrimStmt.Query("ALPHA").Count());

                    Assert.AreEqual(1, colNoCaseStmt.Query("Alpha ").Count());
                    Assert.AreEqual(1, colNoCaseStmt.Query("ALPHA ").Count());
                    Assert.AreEqual(0, colNoCaseStmt.Query("Alpha").Count());
                    Assert.AreEqual(0, colNoCaseStmt.Query("ALPHA").Count());
                }
            }
        }
    }
}


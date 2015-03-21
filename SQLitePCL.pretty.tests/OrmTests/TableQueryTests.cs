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
        public class TestObject
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
            var table = TableMapping.Create<TestObject>();
            var whereFlagEqualsQuery = table.Query().Where(x => x.Flag == default(bool));

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var objects = Enumerable.Range(0, 10).Select(i => new TestObject() { Flag = (i % 3 == 0), Name = String.Format("TestObject{0}", i) });
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
            var table = TableMapping.Create<TestObject>();
            var whereFlagEqualsQuery = table.Query().Where(x => x.Flag == default(bool));

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var objects = Enumerable.Range(0, 10).Select(i => new TestObject() { Flag = (i % 3 == 0), Name = String.Format("TestObject{0}", i) });
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
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
            
                db.InsertOrReplace(table, new TestObject(){ Cost = 20, Name = "A" });
                db.InsertOrReplace(table, new TestObject(){ Cost = 10 });
            
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

        [Test]
        public void TestWhereContainsConstantData()
        {
            var table = TableMapping.Create<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                int n = 20;
                var cq = Enumerable.Range(1, n).Select(i => new TestObject() { Name = i.ToString() });
                db.InsertOrReplaceAll(table, cq);
                
                var tensq = new string[] { "0", "10", "20" };           
                var tens = db.Query(table.Query().Where(o => tensq.Contains(o.Name)), tensq).ToList();  
                Assert.AreEqual(2, tens.Count);
                
                var moreq = new string[] { "0", "x", "99", "10", "20", "234324" };          
                var more = db.Query(table.Query().Where(o => moreq.Contains(o.Name)), moreq).ToList();
                Assert.AreEqual(2, more.Count);
            }
        }
        
        [Test]
        public void TestWhereContainsQueriedData()
        {
            var table = TableMapping.Create<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                int n = 20;
                var cq = Enumerable.Range(1, n).Select(i => new TestObject() { Name = i.ToString() });
                db.InsertOrReplaceAll(table, cq);

                var tensq = new string[] { "0", "10", "20" };  
                var tens = db.Query(table.Query().Where(o => tensq.Contains(o.Name)), tensq).ToList();  
                Assert.AreEqual(2, tens.Count);
                
                var moreq = new string[] { "0", "x", "99", "10", "20", "234324" };          
                var more = db.Query(table.Query().Where(o => moreq.Contains(o.Name)), moreq).ToList();
                Assert.AreEqual(2, more.Count);

                // https://github.com/praeclarum/sqlite-net/issues/28

                // FIXME: The original test used ToList() which seems like a a legimate choice.
                // Need to add overrides that support either parameterized arrays or IEnumerable
                var moreq2 = moreq.ToArray ();
                var more2 = db.Query(table.Query().Where(o => moreq2.Contains(o.Name)), moreq2).ToList();
                Assert.AreEqual(2, more2.Count);
            }        
        }

        [Test]
        public void TestWhereStringStartsWith()
        {
            var table = TableMapping.Create<TestObject>();
            var startsWith = table.Query().Where(x => x.Name.StartsWith(default(string)));

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var prods = new[]
                {
                    new TestObject{ Name = "Foo" },
                    new TestObject { Name = "Bar" },
                    new TestObject { Name = "Foobar" },
                };

                db.InsertOrReplaceAll(table, prods);

                using (var stmt = db.PrepareStatement(startsWith))
                {
                    var fs = stmt.Query("F").ToList();
                    Assert.AreEqual (2, fs.Count);

                    var bs = stmt.Query("B").ToList();
                    Assert.AreEqual (1, bs.Count);
                }
            }
        }
        
        [Test]
        public void TestWhereStringEndsWith ()
        {
            var table = TableMapping.Create<TestObject>();
            var endsWith = table.Query().Where(x => x.Name.EndsWith(default(string)));

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var prods = new[]
                {
                    new TestObject { Name = "Foo" },
                    new TestObject { Name = "Bar" },
                    new TestObject { Name = "Foobar" },
                };

                db.InsertOrReplaceAll(table, prods);

                using (var stmt = db.PrepareStatement(endsWith))
                {
                    var ars = stmt.Query("ar").ToList();
                    Assert.AreEqual (2, ars.Count);

                    var os = stmt.Query("o").ToList();
                    Assert.AreEqual (1, os.Count);
                }
            }
        }
        
        [Test]
        public void TestWhereStringContains ()
        {
            var table = TableMapping.Create<TestObject>();
            var contains = table.Query().Where(x => x.Name.Contains(default(string)));

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var prods = new[]
                {
                    new TestObject { Name = "Foo" },
                    new TestObject { Name = "Bar" },
                    new TestObject { Name = "Foobar" },
                };

                db.InsertOrReplaceAll(table, prods);

                using (var stmt = db.PrepareStatement(contains))
                {
                    var os = stmt.Query("o").ToList();
                    Assert.AreEqual (2, os.Count);

                    var _as = stmt.Query("a").ToList();
                    Assert.AreEqual (2, _as.Count);
                }
            }
        }
        
        [Test]
        public void TestSkip()
        {
            var table = TableMapping.Create<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var n = 100;
            
                var objs = Enumerable.Range(1, n).Select(i => new TestObject() { Cost = i}).ToList();
                var numIn = objs.Count;
                        
                objs = db.InsertOrReplaceAll(table, objs).Values.ToList();       
                Assert.AreEqual(numIn, objs.Count, "Num inserted must = num objects");
            
                var query = table.Query().OrderBy(o => o.Cost);

                var qs1 = query.Skip(1);    
                var s1 = db.Query(qs1).ToList();        
                Assert.AreEqual(n - 1, s1.Count);
                Assert.AreEqual(2, s1[0].Cost);
            
                var qs5 = query.Skip(5);            
                var s5 = db.Query(qs5).ToList();
                Assert.AreEqual(n - 5, s5.Count);
                Assert.AreEqual(6, s5[0].Cost);
            }
        }


        public class NullableTestObject
        {
            [PrimaryKey]
            public long? ID { get; set; }

            public Nullable<int> NullableInt { get; set; }
            public Nullable<float> NullableFloat { get; set; }

            //Strings are allowed to be null by default
            public string StringData { get; set; }
        }


        [Test]
        public void TestWhereNotNull()
        {
            var table = TableMapping.Create<NullableTestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var withNull = new NullableTestObject() { NullableInt = null };
                var with0 = new NullableTestObject() { NullableInt = 0 };
                var with1 = new NullableTestObject() { NullableInt = 1 };
                var withMinus1 = new NullableTestObject() { NullableInt = -1 };

                db.InsertOrReplace(table, withNull);
                db.InsertOrReplace(table, with0);
                db.InsertOrReplace(table, with1);
                db.InsertOrReplace(table, withMinus1);

                var resultsQuery = table.Query().Where(x => x.NullableInt != default(int?)).OrderBy(x => x.ID);
                var results = db.Query(resultsQuery, default(int?)).ToArray();

                Assert.AreEqual(3, results.Length);

                Assert.AreEqual(with0.NullableInt, results[0].NullableInt);
                Assert.AreEqual(with1.NullableInt, results[1].NullableInt);
                Assert.AreEqual(withMinus1.NullableInt, results[2].NullableInt);
            }
        }

        [Test]
        public void TestWhereNull()
        {
            var table = TableMapping.Create<NullableTestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var withNull = new NullableTestObject() { NullableInt = null };
                var with0 = new NullableTestObject() { NullableInt = 0 };
                var with1 = new NullableTestObject() { NullableInt = 1 };
                var withMinus1 = new NullableTestObject() { NullableInt = -1 };

                db.InsertOrReplace(table, withNull);
                db.InsertOrReplace(table, with0);
                db.InsertOrReplace(table, with1);
                db.InsertOrReplace(table, withMinus1);

                var resultsQuery = table.Query().Where(x => x.NullableInt == default(int?)).OrderBy(x => x.ID);
                var results = db.Query(resultsQuery, default(int?)).ToArray();

                Assert.AreEqual(1, results.Length);
                Assert.AreEqual(withNull.NullableInt, results[0].NullableInt);
            }
        }

        [Test]
        public void TestWhereStringNull()
        {
            var table = TableMapping.Create<NullableTestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var withNull = new NullableTestObject() { StringData = null };
                var withEmpty = new NullableTestObject() { StringData = "" };
                var withData = new NullableTestObject() { StringData = "data" };

                db.InsertOrReplace(table, withNull);
                db.InsertOrReplace(table, withEmpty);
                db.InsertOrReplace(table, withData);

                var resultsQuery = table.Query().Where(x => x.StringData == default(string)).OrderBy(x => x.ID);
                var results = db.Query(resultsQuery, default(string)).ToArray();

                Assert.AreEqual(1, results.Length);
                Assert.AreEqual(withNull.StringData, results[0].StringData);
            }
        }

        [Test]
        public void TestWhereStringNotNull()
        {

            var table = TableMapping.Create<NullableTestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var withNull = new NullableTestObject() { StringData = null };
                var withEmpty = new NullableTestObject() { StringData = "" };
                var withData = new NullableTestObject() { StringData = "data" };

                db.InsertOrReplace(table, withNull);
                db.InsertOrReplace(table, withEmpty);
                db.InsertOrReplace(table, withData);

                var resultsQuery = table.Query().Where(x => x.StringData != default(string)).OrderBy(x => x.ID);
                var results = db.Query(resultsQuery, default(string)).ToArray();

                Assert.AreEqual(2, results.Length);
                Assert.AreEqual(withEmpty.StringData, results[0].StringData);
                Assert.AreEqual(withData.StringData, results[1].StringData);
            }
        }

        [Test]
        public void TestWhereEquals()
        {
            var table = TableMapping.Create<TestObject>();
            var nameQuery = table.Query().Where(o => o.Name.Equals(default(string)));
            var idQuery = table.Query().Where(o => o.Id.Equals(default(int)));
            var costQuery = table.Query().Where(o => o.Cost.Equals(default(int)));

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var n = 20;
                var cq = Enumerable.Range(1, n).Select(i => 
                    new TestObject
                    {
                        Name = Convert.ToString(i), 
                        Cost = i
                    });

                db.InsertOrReplaceAll(table, cq);

                var results = db.Query(nameQuery, "10").ToList();
                Assert.AreEqual(results.Count(), 1);
                Assert.AreEqual(results.FirstOrDefault().Name, "10");

                results = db.Query(idQuery, 10).ToList();
                Assert.AreEqual(results.Count(), 1);
                Assert.AreEqual(results.FirstOrDefault().Name, "10");

                results = db.Query(costQuery, 2).ToList();
                Assert.AreEqual(results.Count(), 1);
                Assert.AreEqual(results.FirstOrDefault().Name, "2");
            }
        }

        [Test]
        public void TestOrderBy()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplaceAll(table, new TestObject[] { 
                    new TestObject() { Cost = 3},
                    new TestObject() { Cost = 1},
                    new TestObject() { Cost = 2},
                });

                db.Query(table.Query().OrderBy(x => x.Cost)).Aggregate(0, (acc, obj) =>
                    {
                        Assert.GreaterOrEqual(obj.Cost, acc);
                        return obj.Cost;
                    });

                Assert.Throws<NotSupportedException>(() => table.Query().OrderBy(x => x.Cost).OrderBy(x => x.Id));

                db.Query(table.Query().OrderByDescending(x => x.Cost)).Aggregate(100, (acc, obj) =>
                {
                    Assert.LessOrEqual(obj.Cost, acc);
                    return obj.Cost;
                });

                Assert.Throws<NotSupportedException>(() => table.Query().OrderBy(x => x.Cost).OrderByDescending(x => x.Id));

            }
        }

        [Test]
        public void TestThenBy()
        { 
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplaceAll(table, new TestObject[] { 
                    new TestObject() { Flag = true, Cost = 3},
                    new TestObject() { Flag = true, Cost = 1},
                    new TestObject() { Flag = true, Cost = 2},
                    new TestObject() { Flag = false, Cost = 3},
                    new TestObject() { Flag = false, Cost = 1},
                    new TestObject() { Flag = false, Cost = 2},
                });

                db.Query(table.Query().OrderBy(x => x.Cost).ThenBy(x => x.Flag)).Aggregate(
                    new TestObject() { Cost = 0},
                    (acc, obj) =>
                    {
                        Assert.GreaterOrEqual(obj.Cost, acc.Cost);

                        if (obj.Cost == acc.Cost)
                        {
                            Assert.True(obj.Flag);
                            Assert.False(acc.Flag);
                        }

                        return obj;
                    });

                db.Query(table.Query().OrderByDescending(x => x.Cost).ThenByDescending(x => x.Flag)).Aggregate(
                    new TestObject() { Cost = 100 },
                    (acc, obj) =>
                    {
                        Assert.LessOrEqual(obj.Cost, acc.Cost);

                        if (obj.Cost == acc.Cost)
                        {
                            Assert.False(obj.Flag);
                            Assert.True(acc.Flag);
                        }

                        return obj;
                    });

                Assert.Throws<NotSupportedException>(() => table.Query().ThenBy(x => x.Cost));
                Assert.Throws<NotSupportedException>(() => table.Query().ThenByDescending(x => x.Cost));
            }
        }

        [Test]
        public void TestElementAt()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplaceAll(table, new TestObject[] { 
                    new TestObject() { Cost = 3},
                    new TestObject() { Cost = 1},
                    new TestObject() { Cost = 2},
                });

                Assert.AreEqual(
                    db.Query(table.Query().OrderBy(x => x.Cost).ElementAt(2)).First().Cost, 3);
            }
        }
    }
}


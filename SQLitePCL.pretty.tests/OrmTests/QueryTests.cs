using System;
using System.Linq;

using SQLitePCL.pretty;
using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

using NUnit.Framework;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class QueryBuilder_SelectQuery_Tests
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
        public void TestWhereGreaterThanPropertyValue()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 0 });
                db.InsertOrReplace(table, new TestObject() { Cost = 6 });

                var aTuple = Tuple.Create("key", 1);
                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Cost <= aTuple.Item2);

                Assert.AreEqual(db.Query(query).Select(table.ToObject).Count(), 1);
            }
        }

        [Test]
        public void TestWhereLessThanConstant()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 0 });
                db.InsertOrReplace(table, new TestObject() { Cost = 6 });

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Cost < 5);

                Assert.AreEqual(db.Query(query).Select(table.ToObject).Count(), 1);
            }
        }

        [Test]
        public void TestWhereLessThanOrEqualToBindParam()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 0 });
                db.InsertOrReplace(table, new TestObject() { Cost = 6 });
                db.InsertOrReplace(table, new TestObject() { Cost = 7 });

                var query = SqlQuery.From<TestObject>().Select().Where<int>((x, cost) => x.Cost <= cost);

                Assert.AreEqual(db.Query(query, 6).Select(table.ToObject).Count(), 2);
                Assert.AreEqual(db.Query(query, 5).Select(table.ToObject).Count(), 1);
                Assert.AreEqual(db.Query(query, 7).Select(table.ToObject).Count(), 3);
            }
        }

        [Test]
        public void TestWhereGreaterThanOrEqual()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 0 });
                db.InsertOrReplace(table, new TestObject() { Cost = 6 });
                db.InsertOrReplace(table, new TestObject() { Cost = 7 });

                var query = SqlQuery.From<TestObject>().Select().Where<int>((x, cost) => x.Cost >= cost);

                Assert.AreEqual(db.Query(query, 6).Select(table.ToObject).Count(), 2);
                Assert.AreEqual(db.Query(query, 5).Select(table.ToObject).Count(), 2);
                Assert.AreEqual(db.Query(query, 7).Select(table.ToObject).Count(), 1);
            }
        }

        [Test]
        public void TestWhereOr()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 0, Flag = true});
                db.InsertOrReplace(table, new TestObject() { Cost = 6, Flag = false });
                db.InsertOrReplace(table, new TestObject() { Cost = 7, Flag = false });

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Cost == 0 || x.Flag == false);

                Assert.AreEqual(db.Query(query).Select(table.ToObject).Count(), 3);
            }
        }

        [Test]
        public void TestMultipleBindParams()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 0, Flag = true, Name = "bob" });
                db.InsertOrReplace(table, new TestObject() { Cost = 6, Flag = false, Name = null });
                db.InsertOrReplace(table, new TestObject() { Cost = 7, Flag = true, Name = "bob" });

                var query = 
                    SqlQuery.From<TestObject>().Select()
                         .Where<int, bool, string>((x, cost, flag, name) => 
                            x.Cost > cost && x.Flag == flag && x.Name.IsNot(name));

                Assert.AreEqual(db.Query(query, 0, false, null).Select(table.ToObject).Count(), 0);
                Assert.AreEqual(db.Query(query, 0, true, null).Select(table.ToObject).Count(), 1);
                Assert.AreEqual(db.Query(query, -1, true, null).Select(table.ToObject).Count(), 2);
            }
        }

        [Test]
        public void TestMultipleBindParamsBoundByName()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 0, Flag = true, Name = "bob" });
                db.InsertOrReplace(table, new TestObject() { Cost = 6, Flag = false, Name = null });
                db.InsertOrReplace(table, new TestObject() { Cost = 7, Flag = true, Name = "bob" });

                var query = 
                    SqlQuery.From<TestObject>().Select()
                         .Where<int, bool, string>((x, cost, flag, name) => 
                            x.Cost > cost && x.Flag == flag && x.Name.IsNot(name));

                using (var stmt = db.PrepareStatement(query))
                {
                    stmt.BindParameters[":cost"].Bind(0);
                    stmt.BindParameters[":flag"].Bind(true);
                    stmt.BindParameters[":name"].BindNull();

                    Assert.AreEqual(stmt.Query().Count(), 1);
                }
            }
        }

        [Test]
        public void TestMultipleWhereCalls()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 0, Flag = true });
                db.InsertOrReplace(table, new TestObject() { Cost = 6, Flag = false });
                db.InsertOrReplace(table, new TestObject() { Cost = 7, Flag = true });

                var query =
                    SqlQuery.From<TestObject>().Select()
                         .Where<int>((x, cost) => x.Cost < cost).Where<bool>((x, flag) => x.Flag != flag);

                Assert.AreEqual(db.Query(query, 7, false).Select(table.ToObject).Count(), 1);
                Assert.AreEqual(db.Query(query, 7, true).Select(table.ToObject).Count(), 1);
                Assert.AreEqual(db.Query(query, 8, false).Select(table.ToObject).Count(), 2);
            }
        }

        [Test]
        public void TestWhereContains()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Name = "Bob" });
                db.InsertOrReplace(table, new TestObject() { Name = "oBob" });
                db.InsertOrReplace(table, new TestObject() { Name = "Bobo" });
                db.InsertOrReplace(table, new TestObject() { Name = "Boo" });

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Name.Contains("bob"));
                Assert.AreEqual(db.Query(query).Count(), 3);
            }
        } 

        [Test]
        public void TestWhereStartsWith()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Name = "Bob" });
                db.InsertOrReplace(table, new TestObject() { Name = "oBob" });
                db.InsertOrReplace(table, new TestObject() { Name = "Bobo" });
                db.InsertOrReplace(table, new TestObject() { Name = "Boo" });

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Name.StartsWith("bo"));
                Assert.AreEqual(db.Query(query).Count(), 3);
            }
        }

        [Test]
        public void TestWhereEndsWith()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Name = "Bob" });
                db.InsertOrReplace(table, new TestObject() { Name = "oBob" });
                db.InsertOrReplace(table, new TestObject() { Name = "Bobo" });
                db.InsertOrReplace(table, new TestObject() { Name = "Boo" });

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Name.EndsWith("ob"));
                Assert.AreEqual(db.Query(query).Count(), 2);
            }
        }

        [Test]
        public void TestWhereEndsIsNull()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 100 });
                db.InsertOrReplace(table, new TestObject() { Cost = 100, Name = "Bob" });

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Name.Is(null));
                Assert.AreEqual(db.Query(query).Count(), 1);
            }
        }

        [Test]
        public void TestWhereEndsIsNotNull()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 100 });
                db.InsertOrReplace(table, new TestObject() { Cost = 100, Name = "Bob" });

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Name.IsNot(null));
                Assert.AreEqual(db.Query(query).Count(), 1);
            }
        }

        [Test]
        public void TestWhereEqual()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 100 });
                db.InsertOrReplace(table, new TestObject() { Cost = 100, Name = "Bob" });

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Name.Equals("Bob"));
                Assert.AreEqual(db.Query(query).Count(), 1);
            }
        }

        [Test]
        public void TestWhereNot()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Flag = true });
                db.InsertOrReplace(table, new TestObject() { Flag = false});

                var query = SqlQuery.From<TestObject>().Select().Where<bool>((x, y) => x.Flag == !y);
                Assert.AreEqual(db.Query(query, true).Count(), 1);
            }
        }

        [Test]
        public void TestOrderBy()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 0, Flag = false });
                db.InsertOrReplace(table, new TestObject() { Cost = 0, Flag = true });
                db.InsertOrReplace(table, new TestObject() { Cost = 1, Flag = false });
                db.InsertOrReplace(table, new TestObject() { Cost = 1, Flag = true });
                db.InsertOrReplace(table, new TestObject() { Cost = 2, Flag = false });
                db.InsertOrReplace(table, new TestObject() { Cost = 2, Flag = true });

                var query = SqlQuery.From<TestObject>().Select().OrderBy(x => x.Cost).ThenByDescending(x => x.Flag);
                db.Query(query).Select(table.ToObject).Aggregate(
                    new TestObject() { Cost = -1, Flag = false },
                    (acc, obj) => 
                        {
                            if (obj.Cost > acc.Cost)
                            {
                                Assert.IsTrue(obj.Flag);
                                Assert.IsFalse(acc.Flag);
                            }
                            else
                            {
                                Assert.IsFalse(obj.Flag);
                                Assert.IsTrue(acc.Flag);
                            }
                            return obj;
                        });

            }
        }

        [Test]
        public void TestElementAt()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Cost = 0, Flag = false });
                db.InsertOrReplace(table, new TestObject() { Cost = 0, Flag = true });
                db.InsertOrReplace(table, new TestObject() { Cost = 1, Flag = false });
                db.InsertOrReplace(table, new TestObject() { Cost = 1, Flag = true });
                db.InsertOrReplace(table, new TestObject() { Cost = 2, Flag = false });
                db.InsertOrReplace(table, new TestObject() { Cost = 2, Flag = true });

                var query = SqlQuery.From<TestObject>().Select().OrderBy(x => x.Cost).ThenBy(x => x.Flag).ElementAt(3);
                var result = db.Query(query).Select(table.ToObject).First();
                Assert.AreEqual(result.Cost, 1);
                Assert.AreEqual(result.Flag, true);
            }
        }

        [Test]
        public void TestWhereWithCast()
        {
            var table = TableMapping.Create<TestObject>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                db.InsertOrReplace(table, new TestObject() { Flag = false });
                db.InsertOrReplace(table, new TestObject() { Flag = false });

                object falsey = false;
                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Flag == (bool) falsey);
                Assert.AreEqual(db.Query(query).Count(), 2);
            }
        }
    }
}


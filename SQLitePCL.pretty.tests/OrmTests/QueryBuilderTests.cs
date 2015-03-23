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
                var query = table.Select().Where(x => x.Cost <= aTuple.Item2).ToString();

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

                var query = table.Select().Where(x => x.Cost < 5).ToString();

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

                var query = table.Select().Where<int>((x, cost) => x.Cost <= cost).ToString();

                Assert.AreEqual(db.Query(query, 6).Select(table.ToObject).Count(), 2);
                Assert.AreEqual(db.Query(query, 5).Select(table.ToObject).Count(), 1);
                Assert.AreEqual(db.Query(query, 7).Select(table.ToObject).Count(), 3);
                Assert.AreEqual(db.Query(query, 6).Select(table.ToObject).Count(), 2);
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
                    table.Select()
                         .Where<int, bool, string>((x, cost, flag, name) => 
                            x.Cost > cost && x.Flag == flag && x.Name.IsNot(name))
                         .ToString();

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
                    table.Select()
                         .Where<int, bool, string>((x, cost, flag, name) => 
                            x.Cost > cost && x.Flag == flag && x.Name.IsNot(name))
                         .ToString();

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
                    table.Select()
                         .Where<int>((x, cost) => x.Cost < cost).Where<bool>((x, flag) => x.Flag != flag)
                         .ToString();

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

                var query = table.Select().Where(x => x.Name.Contains("bob")).ToString();
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

                var query = table.Select().Where(x => x.Name.StartsWith("bo")).ToString();
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

                var query = table.Select().Where(x => x.Name.EndsWith("ob")).ToString();
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

                var query = table.Select().Where(x => x.Name.Is(null)).ToString();
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

                var query = table.Select().Where(x => x.Name.IsNot(null)).ToString();
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

                var query = table.Select().Where(x => x.Name.Equals("Bob")).ToString();
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

                var query = table.Select().Where<bool>((x, y) => x.Flag == !y).ToString();
                Assert.AreEqual(db.Query(query, true).Count(), 1);
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
                var query = table.Select().Where(x => x.Flag == (bool) falsey).ToString();
                Assert.AreEqual(db.Query(query).Count(), 2);
            }
        }

        public void TestSelectQuery()
        {

            var table = TableMapping.Create<TestObject>();


            var query3 = table.Select().Where<int,bool>((x, cost, flag) => x.Cost == cost || flag != x.Flag);
            var query4 = table.Select().Where<int,bool>((x, cost, flag) => x.Cost == cost || flag != x.Flag && x.Cost > cost);
            //.OrderBy(x => x.Cost).Skip(7).Take(4);



            var aUri = new Uri("http://www.example.com/path/to/a/resource");
            var query7 = table.Select().Where(x => x.Name == aUri.AbsolutePath);

            var falsey = false;
            var query8 = table.Select().Where(x => x.Flag == !falsey);

            // should fail. Can't deconstruct non table mapping values.
            var query9 = table.Select().Where<Tuple<string, Tuple<int, string>>>((x, y) => x.Name == y.Item1);

            var query13 = table.Select().Where(x => x.Name.Equals("bob"));

            var query14 = table.Select().Where(x => x.Name.Is(null));
            var query15 = table.Select().Where(x => x.Name.IsNot(null));
            var query16 = table.Select().Where<object>((x, y) => x.Name.IsNot(y));

            object falseyObj = false;
            var query17 = table.Select().Where(x => x.Flag == (bool) falsey);


            var 
            result = query3.ToString();
            result = query4.ToString();
            result = query7.ToString();
            result = query8.ToString();
            result = query9.ToString();
            result = query13.ToString();
            result = query14.ToString();
            result = query15.ToString();
            result = query16.ToString();
            result = query17.ToString();
        }
    }
}


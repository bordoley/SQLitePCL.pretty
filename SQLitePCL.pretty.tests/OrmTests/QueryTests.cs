using System;
using System.Linq;

using SQLitePCL.pretty;
using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

using Xunit;

namespace SQLitePCL.pretty.tests
{
    public partial class SqlQueryTests : TestBase
    {
        public class TestObject
        {
            [PrimaryKey]
            public long? Id { get; set; }
            public bool Flag { get; set; }
            public int Cost { get; set;}
            public String Name { get; set; }
        }

        [Fact]
        public void TestWhereGreaterThanPropertyValue()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Cost = 0 }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 6 }, orm);

                var aTuple = Tuple.Create("key", 1);
                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Cost <= aTuple.Item2);

                Assert.Equal(db.Query(query).Select(orm).Count(), 1);
            }
        }

        [Fact]
        public void TestWhereLessThanConstant()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Cost = 0 }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 6 }, orm);

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Cost < 5);

                Assert.Equal(db.Query(query).Select(orm).Count(), 1);
            }
        }

        [Fact]
        public void TestWhereLessThanOrEqualToBindParam()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Cost = 0 }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 6 }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 7 }, orm);

                var query = SqlQuery.From<TestObject>().Select().Where<int>((x, cost) => x.Cost <= cost);

                Assert.Equal(db.Query(query, 6).Select(orm).Count(), 2);
                Assert.Equal(db.Query(query, 5).Select(orm).Count(), 1);
                Assert.Equal(db.Query(query, 7).Select(orm).Count(), 3);
            }
        }

        [Fact]
        public void TestWhereGreaterThanOrEqual()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Cost = 0 }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 6 }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 7 }, orm);

                var query = SqlQuery.From<TestObject>().Select().Where<int>((x, cost) => x.Cost >= cost);

                Assert.Equal(db.Query(query, 6).Select(orm).Count(), 2);
                Assert.Equal(db.Query(query, 5).Select(orm).Count(), 2);
                Assert.Equal(db.Query(query, 7).Select(orm).Count(), 1);
            }
        }

        [Fact]
        public void TestWhereOr()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Cost = 0, Flag = true}, orm);
                db.InsertOrReplace(new TestObject() { Cost = 6, Flag = false }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 7, Flag = false }, orm);

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Cost == 0 || x.Flag == false);

                Assert.Equal(db.Query(query).Select(orm).Count(), 3);
            }
        }

        [Fact]
        public void TestMultipleBindParams()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Cost = 0, Flag = true, Name = "bob" }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 6, Flag = false, Name = null }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 7, Flag = true, Name = "bob" }, orm);

                var query = 
                    SqlQuery.From<TestObject>().Select()
                         .Where<int, bool, string>((x, cost, flag, name) => 
                            x.Cost > cost && x.Flag == flag && x.Name.IsNot(name));

                Assert.Equal(db.Query(query, 0, false, null).Select(orm).Count(), 0);
                Assert.Equal(db.Query(query, 0, true, null).Select(orm).Count(), 1);
                Assert.Equal(db.Query(query, -1, true, null).Select(orm).Count(), 2);
            }
        }

        [Fact]
        public void TestMultipleBindParamsBoundByName()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Cost = 0, Flag = true, Name = "bob" }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 6, Flag = false, Name = null }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 7, Flag = true, Name = "bob" }, orm);

                var query = 
                    SqlQuery.From<TestObject>().Select()
                         .Where<int, bool, string>((x, cost, flag, name) => 
                            x.Cost > cost && x.Flag == flag && x.Name.IsNot(name));

                using (var stmt = db.PrepareStatement(query))
                {
                    stmt.BindParameters[":cost"].Bind(0);
                    stmt.BindParameters[":flag"].Bind(true);
                    stmt.BindParameters[":name"].BindNull();

                    Assert.Equal(stmt.Query().Count(), 1);
                }
            }
        }

        [Fact]
        public void TestWhereContains()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Name = "Bob" }, orm);
                db.InsertOrReplace(new TestObject() { Name = "oBob" }, orm);
                db.InsertOrReplace(new TestObject() { Name = "Bobo" }, orm);
                db.InsertOrReplace(new TestObject() { Name = "Boo" }, orm);

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Name.Contains("bob"));
                Assert.Equal(db.Query(query).Count(), 3);
            }
        } 

        [Fact]
        public void TestWhereStartsWith()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Name = "Bob" }, orm);
                db.InsertOrReplace(new TestObject() { Name = "oBob" }, orm);
                db.InsertOrReplace(new TestObject() { Name = "Bobo" }, orm);
                db.InsertOrReplace(new TestObject() { Name = "Boo" }, orm);

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Name.StartsWith("bo"));
                Assert.Equal(db.Query(query).Count(), 3);
            }
        }

        [Fact]
        public void TestWhereEndsWith()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Name = "Bob" }, orm);
                db.InsertOrReplace(new TestObject() { Name = "oBob" }, orm);
                db.InsertOrReplace(new TestObject() { Name = "Bobo" }, orm);
                db.InsertOrReplace(new TestObject() { Name = "Boo" }, orm);

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Name.EndsWith("ob"));
                Assert.Equal(db.Query(query).Count(), 2);
            }
        }

        [Fact]
        public void TestWhereEndsIsNull()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Cost = 100 }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 100, Name = "Bob" }, orm);

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Name.Is(null));
                Assert.Equal(db.Query(query).Count(), 1);
            }
        }

        [Fact]
        public void TestWhereEndsIsNotNull()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Cost = 100 }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 100, Name = "Bob" }, orm);

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Name.IsNot(null));
                Assert.Equal(db.Query(query).Count(), 1);
            }
        }

        [Fact]
        public void TestWhereEqual()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Cost = 100 }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 100, Name = "Bob" }, orm);

                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Name.Equals("Bob"));
                Assert.Equal(db.Query(query).Count(), 1);
            }
        }

        [Fact]
        public void TestWhereNot()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Flag = true }, orm);
                db.InsertOrReplace(new TestObject() { Flag = false}, orm);

                var query = SqlQuery.From<TestObject>().Select().Where<bool>((x, y) => x.Flag == !y);
                Assert.Equal(db.Query(query, true).Count(), 1);
            }
        }

        [Fact]
        public void TestOrderBy()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Cost = 0, Flag = false }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 0, Flag = true }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 1, Flag = false }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 1, Flag = true }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 2, Flag = false }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 2, Flag = true }, orm);

                var query = SqlQuery.From<TestObject>().Select().OrderBy(x => x.Cost).ThenByDescending(x => x.Flag);
                db.Query(query).Select(orm).Aggregate(
                    new TestObject() { Cost = -1, Flag = false },
                    (acc, obj) => 
                        {
                            if (obj.Cost > acc.Cost)
                            {
                                Assert.True(obj.Flag);
                                Assert.False(acc.Flag);
                            }
                            else
                            {
                                Assert.False(obj.Flag);
                                Assert.True(acc.Flag);
                            }
                            return obj;
                        });

            }
        }

        [Fact]
        public void TestElementAt()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Cost = 0, Flag = false }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 0, Flag = true }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 1, Flag = false }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 1, Flag = true }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 2, Flag = false }, orm);
                db.InsertOrReplace(new TestObject() { Cost = 2, Flag = true }, orm);

                var query = SqlQuery.From<TestObject>().Select().OrderBy(x => x.Cost).ThenBy(x => x.Flag).ElementAt(3);
                var result = db.Query(query).Select(orm).First();
                Assert.Equal(result.Cost, 1);
                Assert.Equal(result.Flag, true);
            }
        }

        [Fact]
        public void TestWhereWithCast()
        {
            var orm = Orm.ResultSet.RowToObject<TestObject>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable<TestObject>();
                db.InsertOrReplace(new TestObject() { Flag = false }, orm);
                db.InsertOrReplace(new TestObject() { Flag = false }, orm);

                object falsey = false;
                var query = SqlQuery.From<TestObject>().Select().Where(x => x.Flag == (bool) falsey);
                Assert.Equal(db.Query(query).Count(), 2);
            }
        }
    }
}


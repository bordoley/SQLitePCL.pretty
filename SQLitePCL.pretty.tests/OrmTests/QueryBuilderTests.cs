using System;
using System.Linq;

using SQLitePCL.pretty;
using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

using NUnit.Framework;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class QueryBuilderTests
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
        public void TestSelectQuery()
        {
            var table = TableMapping.Create<TestObject>();
            var query1 = table.Select().Where<int>((x, cost) => x.Cost == cost);
            var query2 = table.Select().Where<int>((x, cost) => x.Cost == cost).Where<bool>((x, flag) => flag != x.Flag);
            var query3 = table.Select().Where<int,bool>((x, cost, flag) => x.Cost == cost || flag != x.Flag);
            var query4 = table.Select().Where<int,bool>((x, cost, flag) => x.Cost == cost || flag != x.Flag && x.Cost > cost);
            var query5 = table.Select().Where(x => x.Cost < 5).OrderBy(x => x.Cost).Skip(7).Take(4);

            var aTuple = Tuple.Create("key", 1);
            var query6 = table.Select().Where(x => x.Cost <= aTuple.Item2);

            var aUri = new Uri("http://www.example.com/path/to/a/resource");
            var query7 = table.Select().Where(x => x.Name == aUri.AbsolutePath);

            var falsey = false;
            var query8 = table.Select().Where(x => x.Flag == !falsey);

            // should fail. Can't deconstruct non table mapping values.
            var query9 = table.Select().Where<Tuple<string, Tuple<int, string>>>((x, y) => x.Name == y.Item1);

            var query10 = table.Select().Where(x => x.Name.Contains("bob"));

            var query11 = table.Select().Where(x => x.Name.StartsWith("bob"));

            var query12 = table.Select().Where(x => x.Name.EndsWith("bob"));

            var query13 = table.Select().Where(x => x.Name.Equals("bob"));

            var query14 = table.Select().Where(x => x.Name.Is(null));
            var query15 = table.Select().Where(x => x.Name.IsNot(null));
            var query16 = table.Select().Where<object>((x, y) => x.Name.IsNot(y));

            object falseyObj = false;
            var query17 = table.Select().Where(x => x.Flag == (bool) falsey);


            var result = query1.ToString();
            result = query2.ToString();
            result = query3.ToString();
            result = query4.ToString();
            result = query5.ToString();
            result = query6.ToString();
            result = query7.ToString();
            result = query8.ToString();
            result = query9.ToString();
            result = query10.ToString();
            result = query11.ToString();
            result = query12.ToString();
            result = query13.ToString();
            result = query14.ToString();
            result = query15.ToString();
            result = query16.ToString();
            result = query17.ToString();
        }
    }
}


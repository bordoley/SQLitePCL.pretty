using System;
using System.Linq;
using NUnit.Framework;
using SQLitePCL.pretty.Orm;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class CreateTableTests
    {
        [Test]
        public void TestCreateTableMapping()
        {
            var productMap = TableMapping.Create<Product>();
            var selectAllProduct = productMap.Select().ToString();
            var productCount = productMap.Select().Count().ToString();

            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute(productMap.CreateTable());
                using (var stmt = db.PrepareStatement(productMap.Insert()))
                {
                    var product = new Product()
                    {
                        Name = "TestProduct",
                        Price = 5.55m
                    };
                    stmt.Execute<Product>(productMap, product);

                    Console.WriteLine(db.Query(productCount).Select(x => x.First().ToInt()).First());

                    product = new Product()
                    {
                        Id = 2,
                        Name = "TestProduct",
                        Price = 5.55m
                    };

                    stmt.Execute<Product>(productMap, product);

                    Console.WriteLine(db.Query(productCount).Select(x => x.First().ToInt()).First());
                }

                foreach (var p in db.Query("SELECT * from Product").Select(productMap.ToObject))
                {
                    Console.WriteLine(String.Join(", ", p));
                }
            }
        }
    }
}


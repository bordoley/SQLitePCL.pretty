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
            var selectAllProduct = productMap.CreateQuery().Where(x => x.Id == default(int));

            var productCount = 
                (from product in productMap.CreateQuery()
                where product.Id == default(int)
                select product);

            //productMap.Select().Count();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(productMap);

                using (var stmt = db.PrepareInsert(productMap))
                {
                    var product = new Product()
                    {
                        Name = "TestProduct",
                        Price = 5.55m
                    };
                    stmt.Execute<Product>(product);

                    Console.WriteLine(db.Count(productCount));

                    product = new Product()
                    {
                        Id = 2,
                        Name = "TestProduct",
                        Price = 5.55m
                    };

                    stmt.Execute<Product>(product);

                    Console.WriteLine(db.Count(productCount));
                }

                foreach (var p in db.Query(selectAllProduct))
                {
                    Console.WriteLine(String.Join(", ", p));
                }
            }
        }
    }
}


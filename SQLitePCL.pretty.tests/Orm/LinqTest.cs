// Copyright (c) 2009-2015 Krueger Systems, Inc.
// Copyright (c) 2015 David Bordoley
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SQLitePCL.pretty;
using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

using NUnit.Framework;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class LinqTest
    {
        private readonly ITableMapping<Product> productTable = TableMapping.Create<Product>();
        private readonly ITableMapping<Order> orderTable = TableMapping.Create<Order>();
        private readonly ITableMapping<OrderLine> orderLineTable = TableMapping.Create<OrderLine>();
        private readonly ITableMapping<OrderHistory> orderHistoryTable = TableMapping.Create<OrderHistory>();

        private void InitDb(IDatabaseConnection db)
        {
            db.InitTable(productTable);
            db.InitTable(orderTable);
            db.InitTable(orderLineTable);
            db.InitTable(orderHistoryTable);
        }
            
        [Test]
        public void FunctionParameter()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                InitDb(db);

                db.Insert(productTable, new Product { Name = "A", Price = 20 });
            
                db.Insert(productTable, new Product { Name = "B", Price = 10 });
            

                Func<decimal, List<Product>> GetProductsWithPriceAtLeast = (decimal val) =>
                {
                    var query = productTable.CreateQuery().Where(p => p.Price > default(decimal));
                    return db.Query(query, val).ToList();
                }; 
                
                var r = GetProductsWithPriceAtLeast(15);
                Assert.AreEqual(1, r.Count);
                Assert.AreEqual("A", r[0].Name);
            }
        }
        
        [Test]
        public void WhereGreaterThan()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                InitDb(db);
            
                db.Insert(productTable, new Product { Name = "A", Price = 20 });
            
                db.Insert(productTable, new Product { Name = "B", Price = 10 });
            
                Assert.AreEqual(2, db.Count(productTable.CreateQuery()));

                var query = productTable.CreateQuery().Where(p => p.Price > default(int));
                var r = db.Query(query, 15).ToList();
                Assert.AreEqual(1, r.Count);
                Assert.AreEqual("A", r[0].Name);
            }
        }

        [Test]
        public void OrderByCast()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                InitDb(db);

                db.Insert(productTable, new Product { Name = "A", TotalSales = 1 });
                db.Insert(productTable, new Product { Name = "B", TotalSales = 100 });

                var nocast = db.Query(productTable.CreateQuery().OrderByDescending(p => p.TotalSales)).ToList ();
                Assert.AreEqual (2, nocast.Count);
                Assert.AreEqual ("B", nocast [0].Name);

                var cast = db.Query(productTable.CreateQuery().OrderByDescending(p => (int)p.TotalSales)).ToList ();
                Assert.AreEqual (2, cast.Count);
                Assert.AreEqual ("B", cast[0].Name);       
            }
        }

        /*
        public class Issue96_B
        {
            [ AutoIncrement, PrimaryKey]
            public int ID { get; set; }
            public string CustomerName { get; set; }
        }
        
        public class Issue96_C
        {
            [ AutoIncrement, PrimaryKey]
            public int ID { get; set; }
            public string SupplierName { get; set; }
        }*/

        public class Issue96_A
        {
            [ AutoIncrement, PrimaryKey]
            public int? ID { get; set; }
            public string AddressLine { get; set; }
            
            [Indexed]
            public int? ClassB { get; set; }
            [Indexed]
            public int? ClassC { get; set; }
        }

        [Test]
        public void Issue96_NullabelIntsInQueries()
        {
            var table = TableMapping.Create<Issue96_A>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
            
                var id = 42;

                db.Insert(table, new Issue96_A { ClassB = id });
                db.Insert(table, new Issue96_A { ClassB = null });
                db.Insert(table, new Issue96_A { ClassB = null });
                db.Insert(table, new Issue96_A {  ClassB = null });

                var query = table.CreateQuery().Where(p => p.ClassB == default(int?));

                Assert.AreEqual(1, db.Count(query, id));
                Assert.AreEqual(3, db.Count(query, default(int?)));
            }
        }

        public class Issue303_A
        {
            [PrimaryKey, NotNull]
            public int Id { get; set; }
            public string Name { get; set; }
        }
            
        [Test]
        public void Issue303_WhereNot_A()
        {
            var table = TableMapping.Create<Issue303_A>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
            
                db.Insert(table, new Issue303_A { Id = 1, Name = "aa" });
                db.Insert(table, new Issue303_A { Id = 2, Name = null });
                db.Insert(table, new Issue303_A { Id = 3, Name = "test" });
                db.Insert(table, new Issue303_A { Id = 4, Name = null });

                var query = table.CreateQuery().Where(p => !(p.Name == default(string)));

                var r = db.Query(query, default(string)).ToList();
                Assert.AreEqual(2, r.Count);
                Assert.AreEqual(1, r[0].Id);
                Assert.AreEqual(3, r[1].Id);
            }
        }

        public class Issue303_B
        {
            [PrimaryKey, NotNull]
            public int Id { get; set; }
            public bool Flag { get; set; }
        }

        [Test]
        public void Issue303_WhereNot_B()
        {
            var table = TableMapping.Create<Issue303_B>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                db.Insert(table, new Issue303_B { Id = 1, Flag = true });
                db.Insert(table, new Issue303_B { Id = 2, Flag = false });
                db.Insert(table, new Issue303_B { Id = 3, Flag = true });
                db.Insert(table, new Issue303_B { Id = 4, Flag = false });

                var query = table.CreateQuery().Where(p => !p.Flag);

                var r = db.Query(query).ToList();

                Assert.AreEqual(2, r.Count);
                Assert.AreEqual(2, r[0].Id);
                Assert.AreEqual(4, r[1].Id);
            }
        }
    }
}

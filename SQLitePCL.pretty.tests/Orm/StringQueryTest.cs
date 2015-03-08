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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using SQLitePCL.pretty;
using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class StringQueryTest
    {        
        [Test]
        public void StartsWith()
        {
            var table = TableMapping.Create<Product>();
            var startsWith = table.CreateQuery().Where(x => x.Name.StartsWith(default(string)));

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var prods = new[]
                {
                    new Product { Name = "Foo" },
                    new Product { Name = "Bar" },
                    new Product { Name = "Foobar" },
                };

                db.InsertAll(table, prods);

                using (var stmt = db.PrepareQuery(startsWith))
                {
                    var fs = stmt.Query("F").ToList();
                    Assert.AreEqual (2, fs.Count);

                    var bs = stmt.Query("B").ToList();
                    Assert.AreEqual (1, bs.Count);
                }
            }
        }
        
        [Test]
        public void EndsWith ()
        {
            var table = TableMapping.Create<Product>();
            var endsWith = table.CreateQuery().Where(x => x.Name.EndsWith(default(string)));

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var prods = new[]
                {
                    new Product { Name = "Foo" },
                    new Product { Name = "Bar" },
                    new Product { Name = "Foobar" },
                };

                db.InsertAll(table, prods);

                using (var stmt = db.PrepareQuery(endsWith))
                {
                    var ars = stmt.Query("ar").ToList();
                    Assert.AreEqual (2, ars.Count);

                    var os = stmt.Query("o").ToList();
                    Assert.AreEqual (1, os.Count);
                }
            }
        }
        
        [Test]
        public void Contains ()
        {
            var table = TableMapping.Create<Product>();
            var contains = table.CreateQuery().Where(x => x.Name.Contains(default(string)));

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var prods = new[]
                {
                    new Product { Name = "Foo" },
                    new Product { Name = "Bar" },
                    new Product { Name = "Foobar" },
                };

                db.InsertAll(table, prods);

                using (var stmt = db.PrepareQuery(contains))
                {
                    var os = stmt.Query("o").ToList();
                    Assert.AreEqual (2, os.Count);

                    var _as = stmt.Query("a").ToList();
                    Assert.AreEqual (2, _as.Count);
                }
            }
        }
    }
}

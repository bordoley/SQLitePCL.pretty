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
    public class UnicodeTest
    {
        [Test]
        public void Insert()
        {
            var table = TableMapping.Create<Product>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                string testString = "\u2329\u221E\u232A";
                db.Insert(table, (new Product { Name = testString }));

                var p =  db.Find(table, 1);
                Assert.AreEqual (testString, p.Name);
            }
        }
        
        [Test]
        public void Query()
        {
            var table = TableMapping.Create<Product>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);
                string testString = "\u2329\u221E\u232A";
                db.Insert(table, (new Product { Name = testString }));

                var query = table.CreateQuery().Where(p => p.Name == default(string));

                var resultSet = db.Query(query, testString).ToList();
                Assert.AreEqual (1, resultSet.Count);
                Assert.AreEqual (testString, resultSet[0].Name);
            }
        }
    }
}

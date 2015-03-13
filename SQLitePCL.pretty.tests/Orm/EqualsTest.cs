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
using System.Linq;
using NUnit.Framework;

using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    class EqualsTest
    {
        public abstract class TestObjBase<T>
        {
            [AutoIncrement, PrimaryKey]
            public int? Id { get; set; }

            public T Data { get; set; }

            public DateTime Date { get; set; }
        }

        public class TestObjString : TestObjBase<string> { }

        [Test]
        public void CanCompareAnyField()
        {
            var table = TableMapping.Create<TestObjString>();
            var dataQuery = table.CreateQuery().Where(o => o.Data.Equals(default(string)));
            var idQuery = table.CreateQuery().Where(o => o.Id.Equals(default(int)));
            var dateQuery = table.CreateQuery().Where(o => o.Date.Equals(default(DateTime)));

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var n = 20;
                var cq = Enumerable.Range(1, n).Select(i => 
                    new TestObjString
                    {
                        Data = Convert.ToString(i), 
                        Date = new DateTime(2013, 1, i)
                    });

                db.InsertAll(table, cq);

                var results = db.Query(dataQuery, "10").ToList();
                Assert.AreEqual(results.Count(), 1);
                Assert.AreEqual(results.FirstOrDefault().Data, "10");

                results = db.Query(idQuery, 10).ToList();
                Assert.AreEqual(results.Count(), 1);
                Assert.AreEqual(results.FirstOrDefault().Data, "10");

                var date = new DateTime(2013, 1, 10);
                results = db.Query(dateQuery, date).ToList();
                Assert.AreEqual(results.Count(), 1);
                Assert.AreEqual(results.FirstOrDefault().Data, "10");
            }
        }
    }
}

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
    public class DeleteTest
    {
        class TestTable
        {
            [PrimaryKey, AutoIncrement]
            public int? Id { get; set; }
            public int Datum { get; set; }
            public string Test { get; set;}
        }

        [Test]
        public void DeleteEntityOne()
        {
            var table = TableMapping.Create<TestTable>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var count = 100;
                var items = Enumerable.Range (0, count).Select(i => new TestTable { Datum = 1000 + i, Test = "Hello World" });
                var first = db.InsertAll(table, items).First();

                var deleted = db.Delete(table, first);

                Assert.AreEqual(deleted.Id, first.Id);
                Assert.AreEqual(count - 1, db.Count(table.CreateQuery()));
            }
        }

        [Test]
        public void DeletePKOne ()
        {
            var table = TableMapping.Create<TestTable>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var count = 100;
                var items = Enumerable.Range (0, count).Select(i => new TestTable { Datum = 1000 + i, Test = "Hello World" });
                var first = db.InsertAll(table, items).First();

                var deleted = db.Delete(table, first);

                Assert.AreEqual(deleted.Id, first.Id);
                Assert.AreEqual(count - 1, db.Count(table.CreateQuery()));
            }
        }

        [Test]
        public void DeletePKNone()
        {
            var table = TableMapping.Create<TestTable>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var count = 100;
                var items = Enumerable.Range(0, count).Select(i => new TestTable { Datum = 1000 + i, Test = "Hello World" });
                db.InsertAll(table, items);

                var notInTable = new TestTable { Id = 348597,  Datum = 1000, Test = "Hello World" };
                var deleted = db.Delete(table, notInTable);
                Assert.IsNull(deleted);
               
                Assert.AreEqual(count, db.Count(table.CreateQuery()));
            }
        }

        [Test]
        public void DeleteAll ()
        {
            var table = TableMapping.Create<TestTable>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var count = 100;
                var items = Enumerable.Range (0, count).Select(i => new TestTable { Datum = 1000 + i, Test = "Hello World" });
                var first = db.InsertAll(table, items).First();

                db.DeleteAll(table);

                Assert.AreEqual(0, db.Count(table.CreateQuery()));
            }
        }

        /*
        [Test]
        public void DeleteAllWithPredicate()
        {
            var table = TableMapping.Create<TestTable>();
            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var count = 100;
                var items = Enumerable.Range (0, count).Select(i => new TestTable { Datum = 1000 + i, Test = "Hello World" });
                var first = db.InsertAll(table, items).First();

                db.DeleteAll(table);

                Assert.AreEqual(0, db.Count(table.CreateQuery()));
            }

            var r = db.Table<TestTable>().Delete(p => p.Test == "Hello World");

            Assert.AreEqual (Count, r);
            Assert.AreEqual (0, db.Table<TestTable> ().Count ());
        }

        [Test]
        public void DeleteAllWithPredicateHalf()
        {
            var db = CreateDb();
            db.Insert(new TestTable() { Datum = 1, Test = "Hello World 2" }); 

            var r = db.Table<TestTable>().Delete(p => p.Test == "Hello World");

            Assert.AreEqual (Count, r);
            Assert.AreEqual (1, db.Table<TestTable> ().Count ());
        }*/
    }
}


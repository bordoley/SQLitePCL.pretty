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

using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.tests
{    
    [TestFixture]
    public class InsertTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int? Id { get; set; }
            public String Text { get; set; }

            public override string ToString ()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }

        }

        public class TestObj2
        {
            [PrimaryKey]
            public int? Id { get; set; }
            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }

        }

        public class OneColumnObj
        {
            [AutoIncrement, PrimaryKey]
            public int? Id { get; set; }
        }

        public class UniqueObj
        {
            [PrimaryKey]
            public int? Id { get; set; }
        }

        private readonly ITableMapping<TestObj> testObjTable = TableMapping.Create<TestObj>();
        private readonly ITableMapping<TestObj2> testObj2Table = TableMapping.Create<TestObj2>();
        private readonly ITableMapping<OneColumnObj> oneColumnObjTable = TableMapping.Create<OneColumnObj>();
        private readonly ITableMapping<UniqueObj> uniqueObjTable = TableMapping.Create<UniqueObj>();

        private void InitTables(IDatabaseConnection db)
        {
            db.InitTable(testObjTable);
            db.InitTable(testObj2Table);
            db.InitTable(oneColumnObjTable);
            db.InitTable(uniqueObjTable);
        }

        [Test]
        public void InsertALot()
        {
            int n = 10000;
            var objs = Enumerable.Range(1, n).Select(i => new TestObj() { Text = "I am" });

            using (var db = SQLite3.OpenInMemory())
            {
                InitTables(db);

                var insertResult = db.InsertAll<TestObj>(testObjTable, objs).ToList();
                var inObjs = db.Query(testObjTable.CreateQuery()).ToArray();

                for (var i = 0; i < inObjs.Length; i++)
                {
                    Assert.AreEqual(i + 1, insertResult[i].Id);
                    Assert.AreEqual(i + 1, inObjs[i].Id);
                    Assert.AreEqual("I am", inObjs[i].Text);
                }
                
                var numCount = db.Count(testObjTable.CreateQuery());            
                Assert.AreEqual(numCount, n, "Num counted must = num objects");
            }
        }

        /*
        [Test]
        public void InsertTwoTimes()
        {
            var obj1 = new TestObj() { Text = "GLaDOS loves testing!" };
            var obj2 = new TestObj() { Text = "Keep testing, just keep testing" };


            var numIn1 = _db.Insert(obj1);
            var numIn2 = _db.Insert(obj2);
            Assert.AreEqual(1, numIn1);
            Assert.AreEqual(1, numIn2);

            var result = _db.Query<TestObj>("select * from TestObj").ToList();
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(obj1.Text, result[0].Text);
            Assert.AreEqual(obj2.Text, result[1].Text);
        }

        [Test]
        public void InsertIntoTwoTables()
        {
            var obj1 = new TestObj() { Text = "GLaDOS loves testing!" };
            var obj2 = new TestObj2() { Text = "Keep testing, just keep testing" };

            var numIn1 = _db.Insert(obj1);
            Assert.AreEqual(1, numIn1);
            var numIn2 = _db.Insert(obj2);
            Assert.AreEqual(1, numIn2);

            var result1 = _db.Query<TestObj>("select * from TestObj").ToList();
            Assert.AreEqual(numIn1, result1.Count);
            Assert.AreEqual(obj1.Text, result1.First().Text);

            var result2 = _db.Query<TestObj>("select * from TestObj2").ToList();
            Assert.AreEqual(numIn2, result2.Count);
        }

        [Test]
        public void InsertWithExtra()
        {
            var obj1 = new TestObj2() { Id=1, Text = "GLaDOS loves testing!" };
            var obj2 = new TestObj2() { Id=1, Text = "Keep testing, just keep testing" };
            var obj3 = new TestObj2() { Id=1, Text = "Done testing" };

            _db.Insert(obj1);
            
            
            try {
                _db.Insert(obj2);
                Assert.Fail("Expected unique constraint violation");
            }
            catch (SQLiteException) {
            }
            _db.Insert(obj2, "OR REPLACE");


            try {
                _db.Insert(obj3);
                Assert.Fail("Expected unique constraint violation");
            }
            catch (SQLiteException) {
            }
            _db.Insert(obj3, "OR IGNORE");

            var result = _db.Query<TestObj>("select * from TestObj2").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(obj2.Text, result.First().Text);
        }

        [Test]
        public void InsertIntoOneColumnAutoIncrementTable()
        {
            var obj = new OneColumnObj();
            _db.Insert(obj);

            var result = _db.Get<OneColumnObj>(1);
            Assert.AreEqual(1, result.Id);
        }

        [Test]
        public void InsertAllSuccessOutsideTransaction()
        {
            var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();

            _db.InsertAll(testObjects);

            Assert.AreEqual(testObjects.Count, _db.Table<UniqueObj>().Count());
        }

        [Test]
        public void InsertAllFailureOutsideTransaction()
        {
            var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();
            testObjects[testObjects.Count - 1].Id = 1; // causes the insert to fail because of duplicate key

            ExceptionAssert.Throws<SQLiteException>(() => _db.InsertAll(testObjects));

            Assert.AreEqual(0, _db.Table<UniqueObj>().Count());
        }

        [Test]
        public void InsertAllSuccessInsideTransaction()
        {
            var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();

            _db.RunInTransaction(() => {
                _db.InsertAll(testObjects);
            });

            Assert.AreEqual(testObjects.Count, _db.Table<UniqueObj>().Count());
        }

        [Test]
        public void InsertAllFailureInsideTransaction()
        {
            var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();
            testObjects[testObjects.Count - 1].Id = 1; // causes the insert to fail because of duplicate key

            ExceptionAssert.Throws<SQLiteException>(() => _db.RunInTransaction(() => {
                _db.InsertAll(testObjects);
            }));

            Assert.AreEqual(0, _db.Table<UniqueObj>().Count());
        }

        [Test]
        public void InsertOrReplace ()
        {
            _db.Trace = true;
            _db.InsertAll (from i in Enumerable.Range(1, 20) select new TestObj { Text = "#" + i });

            Assert.AreEqual (20, _db.Table<TestObj> ().Count ());

            var t = new TestObj { Id = 5, Text = "Foo", };
            _db.InsertOrReplace (t);

            var r = (from x in _db.Table<TestObj> () orderby x.Id select x).ToList ();
            Assert.AreEqual (20, r.Count);
            Assert.AreEqual ("Foo", r[4].Text);
        }*/
    }
}

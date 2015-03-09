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
    public class GuidTests {
        public class TestObj 
        {
            [PrimaryKey]
            public Guid? Id { get; set; }

            public String Text { get; set; }

            public override string ToString() 
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }
        }

        [Test]
        public void ShouldPersistAndReadGuid()
        {
            var table = TableMapping.Create<TestObj>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var obj1 = new TestObj() { Id = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623"), Text = "First Guid Object" };
                var obj2 = new TestObj() { Id = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6"), Text = "Second Guid Object" };

                db.Insert(table, obj1);
                db.Insert(table, obj2);

                var result = db.Query(table.CreateQuery()).ToList();

                Assert.AreEqual(2, result.Count);
                Assert.AreEqual(obj1.Text, result[0].Text);
                Assert.AreEqual(obj2.Text, result[1].Text);

                Assert.AreEqual(obj1.Id, result[0].Id);
                Assert.AreEqual(obj2.Id, result[1].Id);
            }
        }
        /*
        [Test]
        public void AutoGuid_HasGuid()
        {
            var db = new SQLiteConnection(TestPath.GetTempFileName());
            db.CreateTable<TestObj>(CreateFlags.AutoIncPK);

            var guid1 = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623");
            var guid2 = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6");

            var obj1 = new TestObj() { Id = guid1, Text = "First Guid Object" };
            var obj2 = new TestObj() { Id = guid2, Text = "Second Guid Object" };

            var numIn1 = db.Insert(obj1);
            var numIn2 = db.Insert(obj2);
            Assert.AreEqual(guid1, obj1.Id);
            Assert.AreEqual(guid2, obj2.Id);

            db.Close();
        }

        [Test]
        public void AutoGuid_EmptyGuid()
        {
            var db = new SQLiteConnection(TestPath.GetTempFileName());
            db.CreateTable<TestObj>(CreateFlags.AutoIncPK);

            var guid1 = new Guid("36473164-C9E4-4CDF-B266-A0B287C85623");
            var guid2 = new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6");

            var obj1 = new TestObj() { Text = "First Guid Object" };
            var obj2 = new TestObj() { Text = "Second Guid Object" };

            Assert.AreEqual(Guid.Empty, obj1.Id);
            Assert.AreEqual(Guid.Empty, obj2.Id);

            var numIn1 = db.Insert(obj1);
            var numIn2 = db.Insert(obj2);
            Assert.AreNotEqual(Guid.Empty, obj1.Id);
            Assert.AreNotEqual(Guid.Empty, obj2.Id);
            Assert.AreNotEqual(obj1.Id, obj2.Id);

            db.Close();
        }*/
    }
}

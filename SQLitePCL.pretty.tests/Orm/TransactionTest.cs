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
    public class TransactionTest
    {

        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int? Id { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}]", Id);
            }
        }

        public class TransactionTestException : Exception
        {
        }

        [Test]
        public void SuccessfulSavepointTransaction()
        {
            var table = TableMapping.Create<TestObj>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var testObjects =
                    db.InsertAll(
                        table,
                        Enumerable.Range(1, 20).Select(i => new TestObj())).ToList();

                db.RunInTransaction(_ =>
                    {
                        db.Delete(table, testObjects[0]);
                        db.Delete(table, testObjects[1]);
                        db.Insert(table, new TestObj());
                    });

                Assert.AreEqual(testObjects.Count - 1, db.Count(table.CreateQuery()));
            }
        }

        [Test]
        public void FailSavepointTransaction()
        {
            var table = TableMapping.Create<TestObj>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var testObjects =
                    db.InsertAll(
                        table,
                        Enumerable.Range(1, 20).Select(i => new TestObj())).ToList();

                try 
                {
                    db.RunInTransaction(_ =>
                        {
                            db.Delete(table, testObjects[0]);

                            throw new TransactionTestException();
                        });
                }
                catch (TransactionTestException)  { /* ignore */ }

                Assert.AreEqual(testObjects.Count, db.Count(table.CreateQuery()));
            }
        }
            
        [Test]
        public void SuccessfulNestedSavepointTransaction()
        {
            var table = TableMapping.Create<TestObj>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var testObjects =
                    db.InsertAll(
                        table,
                        Enumerable.Range(1, 20).Select(i => new TestObj())).ToList();

                db.RunInTransaction(_ =>
                {
                    db.Delete(table, testObjects[0]);

                    db.RunInTransaction(__ => 
                        db.Delete(table, testObjects[1]));
                });

                Assert.AreEqual(testObjects.Count - 2, db.Count(table.CreateQuery()));
            }
        }
            
        [Test]
        public void FailNestedSavepointTransaction()
        {
            var table = TableMapping.Create<TestObj>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var testObjects =
                    db.InsertAll(
                        table,
                        Enumerable.Range(1, 20).Select(i => new TestObj())).ToList();

                try
                {
                    db.RunInTransaction(_ =>
                        {
                            db.Delete(table, testObjects[0]);

                            db.RunInTransaction(__ =>
                                {
                                    db.Delete(table, testObjects[1]);
                                    throw new TransactionTestException();
                                });
                        });
                }
                catch (TransactionTestException) { /* ignore */ }

                Assert.AreEqual(testObjects.Count, db.Count(table.CreateQuery()));
            }
        }
    }
}


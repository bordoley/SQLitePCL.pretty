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
    public class SkipTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int? Id { get; set; }
            public int Order { get; set; }

            public override string ToString ()
            {
                return string.Format("[TestObj: Id={0}, Order={1}]", Id, Order);
            }
        }
        
        [Test]
        public void Skip()
        {
            var table = TableMapping.Create<TestObj>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var n = 100;
            
                var objs = Enumerable.Range(1, n).Select(i => new TestObj() { Order = i}).ToList();
                var numIn = objs.Count;
                        
                objs = db.InsertAll(table, objs).ToList();       
                Assert.AreEqual(numIn, objs.Count, "Num inserted must = num objects");
            
                var query = table.CreateQuery().OrderBy(o => o.Order);

                var qs1 = query.Skip(1);    
                var s1 = db.Query(qs1).ToList();        
                Assert.AreEqual(n - 1, s1.Count);
                Assert.AreEqual(2, s1[0].Order);
            
                var qs5 = query.Skip(5);            
                var s5 = db.Query(qs5).ToList();
                Assert.AreEqual(n - 5, s5.Count);
                Assert.AreEqual(6, s5[0].Order);
            }
        }
    }
}

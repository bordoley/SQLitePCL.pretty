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
    public class ContainsTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int? Id { get; set; }
			
			public string Name { get; set; }
			
            public override string ToString ()
            {
            	return string.Format("[TestObj: Id={0}, Name={1}]", Id, Name);
            }
        }
		
        [Test]
        public void ContainsConstantData()
        {
            var table = TableMapping.Create<TestObj>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                int n = 20;
                var cq = Enumerable.Range(1, n).Select(i => new TestObj() { Name = i.ToString() });
                db.InsertAll(table, cq);
                
                var tensq = new string[] { "0", "10", "20" };           
                var tens = db.Query(table.CreateQuery().Where(o => tensq.Contains(o.Name)), tensq).ToList();  
                Assert.AreEqual(2, tens.Count);
                
                var moreq = new string[] { "0", "x", "99", "10", "20", "234324" };          
                var more = db.Query(table.CreateQuery().Where(o => moreq.Contains(o.Name)), moreq).ToList();
                Assert.AreEqual(2, more.Count);
            }
        }
		
		[Test]
        public void ContainsQueriedData()
        {
            var table = TableMapping.Create<TestObj>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                int n = 20;
                var cq = Enumerable.Range(1, n).Select(i => new TestObj() { Name = i.ToString() });
                db.InsertAll(table, cq);

                var tensq = new string[] { "0", "10", "20" };  
                var tens = db.Query(table.CreateQuery().Where(o => tensq.Contains(o.Name)), tensq).ToList();  
                Assert.AreEqual(2, tens.Count);
                
                var moreq = new string[] { "0", "x", "99", "10", "20", "234324" };          
                var more = db.Query(table.CreateQuery().Where(o => moreq.Contains(o.Name)), moreq).ToList();
                Assert.AreEqual(2, more.Count);

                // https://github.com/praeclarum/sqlite-net/issues/28

                // FIXME: The original test used ToList() which seems like a a legimate choice.
                // Need to add overrides that support either parameterized arrays or IEnumerable
                var moreq2 = moreq.ToArray ();
                var more2 = db.Query(table.CreateQuery().Where(o => moreq2.Contains(o.Name)), moreq2).ToList();
                Assert.AreEqual(2, more2.Count);
            }		
        }
    }
}

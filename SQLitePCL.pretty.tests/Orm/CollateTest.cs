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
using SQLite;

using NUnit.Framework;

using SQLitePCL.pretty;
using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

namespace SQLite.Tests
{    
    [TestFixture]
    public class CollateTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
			
			public string CollateDefault { get; set; }
			
			[Collation("BINARY")]
			public string CollateBinary { get; set; }
			
			[Collation("RTRIM")]
			public string CollateRTrim { get; set; }
			
			[Collation("NOCASE")]
			public string CollateNoCase { get; set; }

            public override string ToString ()
            {
            	return string.Format("[TestObj: Id={0}]", Id);
            }
        }

        [Test]
        public void Collate()
        {
			var obj = new TestObj() {
				CollateDefault = "Alpha ",
				CollateBinary = "Alpha ",
				CollateRTrim = "Alpha ",
				CollateNoCase = "Alpha ",
			};

            var table = TableMapping.Create<TestObj>();

            using (var db = SQLite3.Open(":memory:"))
            {
                db.InitTable(table);
                db.Insert(table, obj);  

                using (var colDefaultStmt = db.PrepareQuery(table.CreateQuery().Where(o => o.CollateDefault == "")))
                using (var colBinaryStmt = db.PrepareQuery(table.CreateQuery().Where(o => o.CollateBinary =="")))
                using (var colRTrimStmt = db.PrepareQuery(table.CreateQuery().Where(o => o.CollateRTrim == "")))
                using (var colNoCaseStmt = db.PrepareQuery(table.CreateQuery().Where(o => o.CollateNoCase == "")))
                {
                    Assert.AreEqual(1, colDefaultStmt.Query("Alpha ").Count());
                    Assert.AreEqual(0, colDefaultStmt.Query("ALPHA ").Count());
                    Assert.AreEqual(0, colDefaultStmt.Query("Alpha").Count());
                    Assert.AreEqual(0, colDefaultStmt.Query("ALPHA").Count());

                    Assert.AreEqual(1, colBinaryStmt.Query("Alpha ").Count());
                    Assert.AreEqual(0, colBinaryStmt.Query("ALPHA ").Count());
                    Assert.AreEqual(0, colBinaryStmt.Query("Alpha").Count());
                    Assert.AreEqual(0, colBinaryStmt.Query("ALPHA").Count());

                    Assert.AreEqual(1, colRTrimStmt.Query("Alpha ").Count());
                    Assert.AreEqual(0, colRTrimStmt.Query("ALPHA ").Count());
                    Assert.AreEqual(1, colRTrimStmt.Query("Alpha").Count());
                    Assert.AreEqual(0, colRTrimStmt.Query("ALPHA").Count());


                    Assert.AreEqual(1, colNoCaseStmt.Query("Alpha ").Count());
                    Assert.AreEqual(1, colNoCaseStmt.Query("ALPHA ").Count());
                    Assert.AreEqual(0, colNoCaseStmt.Query("Alpha").Count());
                    Assert.AreEqual(0, colNoCaseStmt.Query("ALPHA").Count());
                }
            }
        }
    }
}

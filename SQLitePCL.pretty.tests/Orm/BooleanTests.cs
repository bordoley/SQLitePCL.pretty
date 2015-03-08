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
    public class BooleanTests
    {
        public class VO
        {
            // FIXME: This is a difference from SQLite-NET need to document
            [AutoIncrement, PrimaryKey]
            public int? ID { get; set; }
            public bool Flag { get; set; }
            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("VO:: ID:{0} Flag:{1} Text:{2}", ID, Flag, Text);
            }
        }
            
        [Test]
        public void TestBoolean()
        {
            var table = TableMapping.Create<VO>();
            const string countWithFlag = "SELECT COUNT(*) FROM VO Where Flag = ?";

            using (var db = SQLite3.Open(":memory:"))
            {
                db.InitTable(table);

                var objects = Enumerable.Range(0, 10).Select(i => new VO() { Flag = (i % 3 == 0), Text = String.Format("VO{0}", i) });
                db.InsertAll(table, objects);

                using (var countStmt = db.PrepareStatement(countWithFlag))
                {
                    // FIXME: Kind of unwieldy. Maybe add FirstInt(), FirstString(), etc. or
                    // SelectScalar()  or both
                    Assert.AreEqual(4, countStmt.Query(true).SelectScalarInt().First());
                    Assert.AreEqual(6, countStmt.Query(false).SelectScalarInt().First());
                }     
            }
        }
    }
}


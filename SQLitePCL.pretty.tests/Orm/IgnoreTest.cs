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

using IgnoreAttribute = SQLitePCL.pretty.Orm.Attributes.IgnoreAttribute;

namespace SQLitePCL.pretty.tests
{    
    [TestFixture]
    public class IgnoreTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int? Id { get; set; }

            public string Text { get; set; }

            [Ignore]
            public Dictionary<int, string> Edibles
            { 
                get { return this._edibles; }
                set { this._edibles = value; }
            } 
            
            protected Dictionary<int, string> _edibles = new Dictionary<int, string>();

            [Ignore]
            public string IgnoredText { get; set; }

            public override string ToString ()
            {
                return string.Format("[TestObj: Id={0}]", Id);
            }
        }

        [Test]
        public void MappingIgnoreColumn ()
        {
            var table = TableMapping.Create<TestObj> ();
            Assert.AreEqual (2, table.Count());
        }

        [Test]
        public void InsertSucceeds()
        {
            var table = TableMapping.Create<TestObj> ();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var o = new TestObj { Text = "Hello", IgnoredText = "World" };
                o = db.Insert (table, o);

                Assert.IsNotNull(o.Id);
            }   
        }

        [Test]
        public void GetDoesntHaveIgnores()
        {
            var table = TableMapping.Create<TestObj>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var o = new TestObj { Text = "Hello", IgnoredText = "World" };
                var result = db.Insert(table, o);

                var oo = db.Find(table, result.Id);
                Assert.AreEqual(o.Text, oo.Text);
                Assert.IsNull(oo.IgnoredText);
            }
        }
    }
}

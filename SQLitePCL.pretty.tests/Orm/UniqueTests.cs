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
    public class UniqueIndexTest
    {
        public class TheOne 
        {
            [PrimaryKey, AutoIncrement]
            public int? ID { get; set; }

            [Unique ("UX_Uno", 0)]
            public int Uno { get; set; }

            [Unique ("UX_Dos", 1)]
            public int Dos { get; set; }

            [Unique ("UX_Dos", 2)]
            public int Tres { get; set; }

            [Indexed ("UX_Uno_bool", 3, true)]
            public int Cuatro { get; set; }

            [Indexed ("UX_Dos_bool", 4, true)]
            public int Cinco { get; set; }

            [Indexed ("UX_Dos_bool", 5, true)]
            public int Seis { get; set; }
        }

        public class IndexColumns 
        {
            public int seqno { get; set; } 

            public int cid { get; set; } 

            public string name { get; set; } 
        }

        public class IndexInfo {
            public int seq { get; set; } 

            public string name { get; set; } 

            public bool unique { get; set; }
        }

        [Test]
        public void CreateUniqueIndexes()
        {
            var table = TableMapping.Create<TheOne>();

            using (var db = SQLite3.OpenInMemory())
            {
                
                db.InitTable(table);
                var indexes = db.GetIndexInfo(table.TableName);

                foreach (var index in table.Indexes)
                {
                    Assert.That(indexes.Contains(index));
                }

                foreach (var index in indexes)
                {
                    Assert.That(table.Indexes.Contains(index));
                }
            }
        }
    }
}


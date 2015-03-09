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
	public class MigrationTest
	{
		[Table ("Test")]
		class LowerId 
        {
			public int Id { get; set; }
		}

		[Table ("Test")]
		class UpperId 
        {
			public int ID { get; set; }
		}

		[Test]
		public void UpperAndLowerColumnNames ()
		{
            var lowerTable = TableMapping.Create<LowerId>();
            var upperTable = TableMapping.Create<UpperId>();

			using (var db = SQLite3.OpenInMemory()) 
            {
				db.InitTable(lowerTable);
				db.InitTable(upperTable);

				var cols = db.GetTableInfo("Test");
				Assert.That (cols.Count, Is.EqualTo (1));
				Assert.That (cols.First().Key, Is.EqualTo ("Id"));
			}
		}
	}
}

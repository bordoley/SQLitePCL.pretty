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
    public class ByteArrayTests
    {
        public class ByteArrayClass
        {
            public static void AssertEquals(ByteArrayClass This, ByteArrayClass other)
            {
                // Another spot we're we are different from SQLite-net. 
                // We don't mutate the caller of insert so these are never equal;
                // Assert.AreEqual(other.ID, This.ID);
                
                if (other.bytes == null || This.bytes == null) 
                {
                    Assert.IsNull (other.bytes);
                    Assert.IsNull (This.bytes);
                }
                else 
                {
                    Assert.AreEqual(other.bytes.Length, This.bytes.Length);
                    for (var i = 0; i < This.bytes.Length; i++) 
                    {
                        Assert.AreEqual(other.bytes[i], This.bytes[i]);
                    }
                }
            }

            // FIXME: This is a difference from SQLite-NET need to document
            [PrimaryKey, AutoIncrement]
            public int? ID { get; set; }

            public byte[] bytes { get; set; }
        }
            
        [Test]
        [Description("Create objects with various byte arrays and check they can be stored and retrieved correctly")]
        public void ByteArrays()
        {
            //Byte Arrays for comparisson
            var byteArrays = new ByteArrayClass[]
            {
                new ByteArrayClass() { bytes = new byte[] { 1, 2, 3, 4, 250, 252, 253, 254, 255 } }, //Range check
                new ByteArrayClass() { bytes = new byte[] { 0 } }, //null bytes need to be handled correctly
                new ByteArrayClass() { bytes = new byte[] { 0, 0 } },
                new ByteArrayClass() { bytes = new byte[] { 0, 1, 0 } },
                new ByteArrayClass() { bytes = new byte[] { 1, 0, 1 } },
                new ByteArrayClass() { bytes = new byte[] { } }, //Empty byte array should stay empty (and not become null)
                new ByteArrayClass() { bytes = null } //Null should be supported
            };

            var table = TableMapping.Create<ByteArrayClass>();
            var orderedById = table.CreateQuery().OrderBy(x => x.ID);

            using (var db = SQLite3.Open(":memory:"))
            {
                db.InitTable(table);

                //Insert all of the ByteArrayClass
                foreach (var b in byteArrays)
                {
                    db.Insert(table, b);
                }

                var fetchedByteArrays = db.Query(orderedById).ToArray();

                Assert.AreEqual(fetchedByteArrays.Length, byteArrays.Length);
                //Check they are the same
                for (int i = 0; i < byteArrays.Length; i++)
                {
                    ByteArrayClass.AssertEquals(byteArrays[i], fetchedByteArrays[i]);
                }
            }
        }
    }
}


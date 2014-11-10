/*
   Copyright 2014 David Bordoley

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.IO;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class Example
    {
        [Test]
        public void DoExample()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.ExecuteAll(
                    @"CREATE TABLE foo (w int, x float, y string, z blob);
                      INSERT INTO foo (w,x,y,z) VALUES (0, 0, '', null);");

                var stream = new MemoryStream(Encoding.UTF8.GetBytes("I'm a byte stream"));

                db.Execute("INSERT INTO foo (w, x, y, z) VALUES (?, ?, ?, ?)", 1, 1.1, "hello", stream);
                db.Query("SELECT rowid, z FROM foo where rowid = ?", db.LastInsertedRowId)
                    .Select(row =>
                        {
                            using (var dst = db.OpenBlob(row[1], row[0].ToInt64(), true))
                            {
                                stream.CopyTo(dst);
                            }
                            return row;
                        })
                    .FirstOrDefault();

                db.Query("SELECT rowid, * FROM foo")
                    .Select(row =>
                        {
                            Console.Write(
                                row[0].ToInt64() + ": " + 
                                row[1].ToInt() + ", " +
                                row[2].ToInt64() + ", " +
                                row[3].ToString() + ", ");

                            if (row[4].SQLiteType == SQLiteType.Null)
                            {
                                Console.Write("null\n");
                            }
                            else
                            {
                                using (var blob = db.OpenBlob(row[4], row[0].ToInt64()))
                                {
                                    var str = new StreamReader(blob).ReadToEnd();
                                    Console.Write(str + "\n");
                                }
                            }
                            return row;
                        })
                    .LastOrDefault();
                
                stream.Dispose();

                // Blob dispose is broken: https://github.com/ericsink/SQLitePCL.raw/pull/9
                // so tryig to drop the table causes an exception.
                //db.Execute("DROP TABLE foo;");
            }
        }
    }
}

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

using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

namespace SQLitePCL.pretty.tests
{
    public class Example
    {
        [Fact]
        public void DoExample()
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("I'm a byte stream")))
            using (var db = SQLite3.OpenInMemory())
            {
                db.ExecuteAll(
                    @"CREATE TABLE foo (w int, x float, y string, z blob);
                      INSERT INTO foo (w,x,y,z) VALUES (0, 0, '', null);");

                db.Execute("INSERT INTO foo (w, x, y, z) VALUES (?, ?, ?, ?)", 1, 1.1, "hello", stream);

                var dst = db.Query("SELECT rowid, z FROM foo where rowid = ?", db.LastInsertedRowId)
                            .Select(row => db.OpenBlob(row[1].ColumnInfo, row[0].ToInt64(), true))
                            .First();

                using (dst) { stream.CopyTo(dst); }

                foreach (var row in db.Query("SELECT rowid, * FROM foo"))
                {
                    Console.Write(
                                row[0].ToInt64() + ": " +
                                row[1].ToInt() + ", " +
                                row[2].ToInt64() + ", " +
                                row[3].ToString() + ", ");

                    if (row[4].SQLiteType == SQLiteType.Null)
                    {
                        Console.Write("null\n");
                        continue;
                    }

                    using (var blob = db.OpenBlob(row[4].ColumnInfo, row[0].ToInt64(), false))
                    {
                        var str = new StreamReader(blob).ReadToEnd();
                        Console.Write(str + "\n");
                    }
                }

                db.Execute("DROP TABLE foo;");
            }
        }

        [Fact]
        public async Task DoExampleAsync()
        {
            using (var db = SQLite3.OpenInMemory().AsAsyncDatabaseConnection())
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("I'm a byte stream")))
            {
                await db.ExecuteAllAsync(
                    @"CREATE TABLE foo (w int, x float, y string, z blob);
                        INSERT INTO foo (w,x,y,z) VALUES (0, 0, '', null);");

                await db.ExecuteAsync("INSERT INTO foo (w, x, y, z) VALUES (?, ?, ?, ?)", 1, 1.1, "hello", stream);

                var dst =
                    await db.Query("SELECT rowid, z FROM foo where y = 'hello'")
                            .Select(row => db.OpenBlobAsync(row[1].ColumnInfo, row[0].ToInt64(), true))
                            .FirstAsync()
                            .ToTask()
                            .Unwrap();

                using (dst)
                {
                    await stream.CopyToAsync(dst);
                }

                await db.Query("SELECT rowid, * FROM foo")
                        .Select(row =>
                            row[0].ToInt64() + ": " +
                            row[1].ToInt() + ", " +
                            row[2].ToInt64() + ", " +
                            row[3].ToString() + ", " +
                            row[4].ToString())
                        .Do(Console.WriteLine);
            }
        }
    }
}

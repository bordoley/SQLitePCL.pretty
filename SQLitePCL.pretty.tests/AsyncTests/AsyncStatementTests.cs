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

using NUnit.Framework;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class AsyncStatementTests
    {
        [Test]
        public async Task TestIStatementDispose()
        {
            using (var adb = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                var aStmt = await adb.PrepareStatementAsync("SELECT ?, ?, ?");
                await aStmt.Use(stmt =>
                    {
                        stmt.Dispose();
                        Assert.Throws<ObjectDisposedException>(() => { var x = stmt.BindParameters; });
                        Assert.Throws<ObjectDisposedException>(() => { var x = stmt.Columns; });
                        Assert.Throws<ObjectDisposedException>(() => { var x = stmt.Current; });
                        Assert.Throws<ObjectDisposedException>(() => { var x = stmt.SQL; });
                        Assert.Throws<ObjectDisposedException>(() => { var x = stmt.IsReadOnly; });
                        Assert.Throws<ObjectDisposedException>(() => { var x = stmt.IsBusy; });
                        Assert.Throws<ObjectDisposedException>(() => { stmt.ClearBindings(); });
                        Assert.Throws<ObjectDisposedException>(() => { stmt.MoveNext(); });
                        Assert.Throws<ObjectDisposedException>(() => { stmt.Reset(); });
                    });

                await aStmt.Use(stmt => 
                    {
                        // Assert that the statement is not disposed, despite the previous user disposing it's instance.
                        Assert.DoesNotThrow(() => { var x = stmt.IsReadOnly; });
                    });
            }
        }

        [Test]
        public async Task TestUse()
        {
            using (var db = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                await db.ExecuteAsync("CREATE TABLE foo (x int);");

                var aStmt = await db.PrepareStatementAsync("INSERT INTO foo (x) VALUES (?);");
                await aStmt.Use(stmt => Enumerable.Range(0, 1000))
                    .Scan(Tuple.Create<int, int>(-1, -1), (x, y) => Tuple.Create(x.Item1 + 1, y))
                    .Do(result =>
                    {
                        Assert.AreEqual(result.Item2, result.Item1);
                    });

                var anotherUse = aStmt.Use(stmt => Enumerable.Range(0, 1000));

                aStmt.Dispose();

                Assert.Throws<ObjectDisposedException>(() => aStmt.Use(stmt => Enumerable.Range(0, 1000)));
                Assert.Throws<ObjectDisposedException>(async () => { await anotherUse; });
                Assert.Throws<ObjectDisposedException>(() => aStmt.Use(stmt => { }));

                var bStmt = await db.PrepareStatementAsync("SELECT 2");
                int mutable = 0;
                await bStmt.Use(stmt =>
                    {
                        stmt.MoveNext();
                        mutable = stmt.Current[0].ToInt();
                    });
                Assert.AreEqual(mutable, 2);
            }
        }

        [Test]
        public async Task TestIStatementEnumerator()
        {
            using (var db = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                await db.ExecuteAllAsync(
                    @"CREATE TABLE foo (x int, y int, z int);
                      INSERT INTO foo (x, y, z) VALUES (1, 2, 3);");

                var aStmt = await db.PrepareStatementAsync("SELECT x, y, z from foo");
                await aStmt.Use(stmt => 
                    {
                        Assert.True(stmt.MoveNext());
                        var row = stmt.Current;
                        Assert.AreEqual(row[0].ToInt(), 1);
                        Assert.AreEqual(row[1].ToInt(), 2);
                        Assert.AreEqual(row[2].ToInt(), 3);

                        Assert.False(stmt.MoveNext());
                        Assert.False(stmt.MoveNext());

                        stmt.Reset();
                        Assert.True(stmt.MoveNext());
                    });
            }
        }

        [Test]
        public async Task TestIStatementBindings()
        {
            using (var db = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                var aStmt = await db.PrepareStatementAsync("SELECT :a as a, :b as b, :c as c");
                await aStmt.Use(stmt =>
                    {
                        Assert.False(stmt.IsBusy);

                        stmt.BindParameters[0].Bind(0);
                        stmt.BindParameters[1].Bind("1");
                        stmt.BindParameters[2].Bind(2);

                        Assert.AreEqual(stmt.BindParameters[":a"].Name, ":a");
                        Assert.AreEqual(stmt.BindParameters[":b"].Name, ":b");
                        Assert.AreEqual(stmt.BindParameters[":c"].Name, ":c");

                        stmt.MoveNext();
                        var row = stmt.Current;

                        Assert.True(stmt.IsBusy);

                        Assert.AreEqual(row[0].ToInt(), 0);
                        Assert.AreEqual(row[1].ToInt(), 1);
                        Assert.AreEqual(row[2].ToString(), "2");

                        stmt.Reset();
                        Assert.False(stmt.IsBusy);
                        stmt.MoveNext();
                        row = stmt.Current;

                        Assert.AreEqual(row[0].ToInt(), 0);
                        Assert.AreEqual(row[1].ToInt(), 1);
                        Assert.AreEqual(row[2].ToString(), "2");

                        stmt.ClearBindings();
                        Assert.True(stmt.IsBusy);
                        stmt.Reset();
                        Assert.False(stmt.IsBusy);
                        stmt.MoveNext();
                        row = stmt.Current;

                        Assert.AreEqual(row[0].SQLiteType, SQLiteType.Null);
                        Assert.AreEqual(row[1].SQLiteType, SQLiteType.Null);
                        Assert.AreEqual(row[2].SQLiteType, SQLiteType.Null);

                        Assert.False(stmt.MoveNext());
                    });
            }
        }

        [Test]
        public async Task TestIStatementColumns()
        {
            using (var db = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                var aStmt = await db.PrepareStatementAsync("SELECT :a as a, :b as b, :c as c");
                await aStmt.Use(stmt => 
                    {
                        Assert.AreEqual(stmt.Columns[0].Name, "a");
                        Assert.AreEqual(stmt.Columns[1].Name, "b");
                        Assert.AreEqual(stmt.Columns[2].Name, "c");
                    });
            }
        }
    }
}

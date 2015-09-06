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
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SQLitePCL.pretty.tests
{
    public class AsyncStatementTests
    {
        [Fact]
        public async Task TestIStatementDispose()
        {
            using (var adb = SQLiteDatabaseConnectionBuilder.InMemory().BuildAsyncDatabaseConnection())
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
                        Assert.Throws<ObjectDisposedException>(() => { stmt.Status(StatementStatusCode.AutoIndex, false); });
                    });

                await aStmt.Use(stmt =>
                    {
                        // Assert that the statement is not disposed, despite the previous user disposing it's instance.
                        { var x = stmt.IsReadOnly; };
                    });
            }
        }

        [Fact]
        public async Task TestUse()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory().BuildAsyncDatabaseConnection())
            {
                await db.ExecuteAsync("CREATE TABLE foo (x int);");

                var aStmt = await db.PrepareStatementAsync("INSERT INTO foo (x) VALUES (?);");
                await aStmt.Use(stmt => Enumerable.Range(0, 1000))
                    .Scan(Tuple.Create<int, int>(-1, -1), (x, y) => Tuple.Create(x.Item1 + 1, y))
                    .Do(result =>
                    {
                        Assert.Equal(result.Item2, result.Item1);
                    });

                var anotherUse = aStmt.Use(stmt => Enumerable.Range(0, 1000));

                aStmt.Dispose();

                // Test double dispose
                aStmt.Dispose();

                Assert.Throws<ObjectDisposedException>(() => aStmt.Use(stmt => Enumerable.Range(0, 1000)));
                Assert.Throws<ObjectDisposedException>(() => anotherUse.Subscribe());
                Assert.ThrowsAsync<ObjectDisposedException>(async () => await aStmt.Use(stmt => { }));

                var bStmt = await db.PrepareStatementAsync("SELECT 2");
                int mutable = 0;
                await bStmt.Use(stmt =>
                    {
                        stmt.MoveNext();
                        mutable = stmt.Current[0].ToInt();
                    });
                Assert.Equal(mutable, 2);
            }
        }

        [Fact]
        public async Task TestIStatementEnumerator()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory().BuildAsyncDatabaseConnection())
            {
                await db.ExecuteAllAsync(
                    @"CREATE TABLE foo (x int, y int, z int);
                      INSERT INTO foo (x, y, z) VALUES (1, 2, 3);");

                var aStmt = await db.PrepareStatementAsync("SELECT x, y, z from foo");
                await aStmt.Use(stmt =>
                    {
                        Assert.True(stmt.MoveNext());
                        var row = stmt.Current;
                        Assert.Equal(row[0].ToInt(), 1);
                        Assert.Equal(row[1].ToInt(), 2);
                        Assert.Equal(row[2].ToInt(), 3);

                        Assert.False(stmt.MoveNext());
                        Assert.False(stmt.MoveNext());

                        stmt.Reset();
                        Assert.True(stmt.MoveNext());
                    });
            }
        }

        [Fact]
        public async Task TestIStatementBindings()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory().BuildAsyncDatabaseConnection())
            {
                var aStmt = await db.PrepareStatementAsync("SELECT :a as a, :b as b, :c as c");
                await aStmt.Use(stmt =>
                    {
                        Assert.False(stmt.IsBusy);

                        stmt.BindParameters[0].Bind(0);
                        stmt.BindParameters[1].Bind("1");
                        stmt.BindParameters[2].Bind(2);

                        Assert.Equal(stmt.BindParameters[":a"].Name, ":a");
                        Assert.Equal(stmt.BindParameters[":b"].Name, ":b");
                        Assert.Equal(stmt.BindParameters[":c"].Name, ":c");

                        stmt.MoveNext();
                        var row = stmt.Current;

                        Assert.True(stmt.IsBusy);

                        Assert.Equal(row[0].ToInt(), 0);
                        Assert.Equal(row[1].ToInt(), 1);
                        Assert.Equal(row[2].ToString(), "2");

                        stmt.Reset();
                        Assert.False(stmt.IsBusy);
                        stmt.MoveNext();
                        row = stmt.Current;

                        Assert.Equal(row[0].ToInt(), 0);
                        Assert.Equal(row[1].ToInt(), 1);
                        Assert.Equal(row[2].ToString(), "2");

                        stmt.ClearBindings();
                        Assert.True(stmt.IsBusy);
                        stmt.Reset();
                        Assert.False(stmt.IsBusy);
                        stmt.MoveNext();
                        row = stmt.Current;

                        Assert.Equal(row[0].SQLiteType, SQLiteType.Null);
                        Assert.Equal(row[1].SQLiteType, SQLiteType.Null);
                        Assert.Equal(row[2].SQLiteType, SQLiteType.Null);

                        Assert.False(stmt.MoveNext());
                    });
            }
        }

        [Fact]
        public async Task TestIStatementColumns()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory().BuildAsyncDatabaseConnection())
            {
                var aStmt = await db.PrepareStatementAsync("SELECT :a as a, :b as b, :c as c");
                await aStmt.Use(stmt =>
                    {
                        Assert.Equal(stmt.Columns[0].Name, "a");
                        Assert.Equal(stmt.Columns[1].Name, "b");
                        Assert.Equal(stmt.Columns[2].Name, "c");
                    });
            }
        }

        [Fact]
        public async Task TestExecuteAsync()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory().BuildAsyncDatabaseConnection())
            {
                await db.ExecuteAsync("CREATE TABLE foo (x int)");
                var aStmt = await db.PrepareStatementAsync("INSERT INTO foo (x) VALUES (?)");
                for (int i = 0; i < 100; i++)
                {
                    await aStmt.ExecuteAsync(i);
                }

                var count = await db.Query("SELECT COUNT(*) from foo").SelectScalarInt().FirstAsync();
                Assert.Equal(count, 100);
            }
        }

        [Fact]
        public async Task TestQuery()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory().BuildAsyncDatabaseConnection())
            {
                await db.ExecuteAsync("CREATE TABLE foo (v int);");

                using (var stmt = await db.PrepareStatementAsync("INSERT INTO foo (v) VALUES (?)"))
                {
                    foreach (var i in Enumerable.Range(0, 100))
                    {
                        await stmt.ExecuteAsync(i);
                    }
                }

                using (var stmt = await db.PrepareStatementAsync("SELECT * from FOO WHERE v < ?"))
                {
                    var result = await stmt.Query(50).Count();
                    Assert.Equal(result, 50);
                }

                using (var stmt = await db.PrepareStatementAsync("SELECT * from FOO WHERE v < 50"))
                {
                    var result = await stmt.Query().Count();
                    Assert.Equal(result, 50);
                }
            }
        }
    }
}

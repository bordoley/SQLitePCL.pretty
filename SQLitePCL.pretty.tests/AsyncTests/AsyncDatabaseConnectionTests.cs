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
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty.tests
{
    public class AsyncDatabaseConnectionTests : TestBase
    {
        [Fact]
        public async Task TestProfileEvent()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                var statement = "CREATE TABLE foo (x int);";
                db.Profile.Subscribe(e =>
                    {
                        Assert.Equal(statement, e.Statement);
                        Assert.True(TimeSpan.MinValue < e.ExecutionTime);
                    });

                await db.ExecuteAsync(statement);
            }
        }

        [Fact]
        public async Task TestTraceEvent()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                var statement = "CREATE TABLE foo (x int);";
                db.Trace.Subscribe(e =>
                    {
                        Assert.Equal(statement, e.Statement);
                    });

                await db.ExecuteAsync(statement);

                statement = "INSERT INTO foo (x) VALUES (1);";
                await db.ExecuteAsync(statement);
            }
        }

        [Fact]
        public async Task TestUpdateEvent()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                var currentAction = ActionCode.CreateTable;
                var rowid = 1;

                db.Update.Subscribe(e =>
                    {
                        Assert.Equal(currentAction, e.Action);
                        Assert.Equal("main", e.Database);
                        Assert.Equal("foo", e.Table);
                        Assert.Equal(rowid, e.RowId);
                    });

                currentAction = ActionCode.CreateTable;
                rowid = 1;
                await db.ExecuteAsync("CREATE TABLE foo (x int);");

                currentAction = ActionCode.Insert;
                rowid = 1;
                await db.ExecuteAsync("INSERT INTO foo (x) VALUES (1);");

                currentAction = ActionCode.Insert;
                rowid = 2;
                await db.ExecuteAsync("INSERT INTO foo (x) VALUES (2);");

                currentAction = ActionCode.DropTable;
                rowid = 2;
                await db.ExecuteAsync("DROP TABLE foo");
            }
        }

        [Fact]
        public async Task TestUse()
        {
            using (var adb = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                await adb.Use(db => Enumerable.Range(0, 1000))
                    .Scan(Tuple.Create(-1, -1), (x, y) => Tuple.Create(x.Item1 + 1, y))
                    .Do(result =>
                        {
                            Assert.Equal(result.Item2, result.Item1);
                        });

                int expected = 10;
                var t = await adb.Use(db =>
                    {
                        return db.Query("Select ?", expected).SelectScalarInt().First();
                    });
                Assert.Equal(t, expected);

                var anotherUse = adb.Use(db => Enumerable.Range(0, 1000));

                adb.Dispose();

                // Test double dispose
                adb.Dispose();

                Assert.Throws<ObjectDisposedException>(() => adb.Use(db => Enumerable.Range(0, 1000)));
                Assert.Throws<ObjectDisposedException>(() => anotherUse.Subscribe());
                await Assert.ThrowsAsync<ObjectDisposedException>(async () => await adb.Use(db => { }));
            }
        }
            
        [Fact]
        public async Task TestIDatabaseConnectionDispose()
        {
            using (var adb = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                await adb.ExecuteAsync("CREATE TABLE foo (x int);");

                IDatabaseConnection disposedDb = null;

                await adb.Use(db =>
                {
                    // Assert these don't throw
                    { var x = db.IsAutoCommit; }
                    { var x = db.IsReadOnly; }
                    { var x = db.Changes; }
                    { var x = db.TotalChanges; }
                    { var x = db.LastInsertedRowId; };
                    { var x = db.IsDatabaseReadOnly("main"); };
                    { using (var stmt = db.PrepareStatement("SELECT x FROM foo;")) { } };

                    int current;
                    int highwater;
                    { db.Status(DatabaseConnectionStatusCode.CacheMiss, out current, out highwater, false); };
                    { db.WalCheckPoint("main"); };

                    disposedDb = db;
                });

                Assert.Throws<ObjectDisposedException>(() => { var x = disposedDb.IsAutoCommit; });
                Assert.Throws<ObjectDisposedException>(() => { var x = disposedDb.IsReadOnly; });
                Assert.Throws<ObjectDisposedException>(() => { var x = disposedDb.Changes; });
                Assert.Throws<ObjectDisposedException>(() => { var x = disposedDb.TotalChanges; });
                Assert.Throws<ObjectDisposedException>(() => { var x = disposedDb.LastInsertedRowId; });
                Assert.Throws<ObjectDisposedException>(() => { var x = disposedDb.IsDatabaseReadOnly("main"); });
                Assert.Throws<ObjectDisposedException>(() => { var x = disposedDb.OpenBlob("", "", "", 0, true); });
                Assert.Throws<ObjectDisposedException>(() => { var x = disposedDb.GetTableColumnMetadata("", "", ""); });
                Assert.Throws<ObjectDisposedException>(() => disposedDb.PrepareStatement("SELECT x FROM foo;"));

                int current2;
                int highwater2;
                Assert.Throws<ObjectDisposedException>(() => { disposedDb.Status(DatabaseConnectionStatusCode.CacheMiss, out current2, out highwater2, false); });
                Assert.Throws<ObjectDisposedException>(() => { disposedDb.WalCheckPoint("main"); });

                await adb.Use(db =>
                    {
                        // Assert that the database is not disposed, despite the previous user disposing it's instance.
                        { var x = db.IsAutoCommit; };
                    });

                // Test that subscribe doesn't throw after Dispose() is called.
                var disposedObservable = adb.Use(db => Enumerable.Empty<Unit>());
                adb.Dispose();
                await disposedObservable.Materialize()
                    .Do(x =>
                        {
                            Assert.Equal(x.Kind, NotificationKind.OnError);
                            Assert.IsAssignableFrom<ObjectDisposedException>(x.Exception);
                        });
            }
        }

        [Fact]
        public void TestUseCancelled()
        {
            using (var adb = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                Assert.ThrowsAsync<TaskCanceledException>(async () => await adb.Use((db, ct) => { }, cts.Token));

                cts = new CancellationTokenSource();
                Assert.ThrowsAsync<TaskCanceledException>(async () => await adb.Use((db, ct) => { cts.Cancel(); }, cts.Token));
            }
        }

        [Fact]
        public async Task TestPrepareAllAsync()
        {
            using (var adb = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                await adb.ExecuteAsync("CREATE TABLE foo (x int);");
                var stmts =
                    await adb.PrepareAllAsync(
                        "SELECT * FROM foo;" +
                        "SELECT x FROM foo;" +
                        "SELECT rowid, x FROM foo;");
                Assert.Equal(stmts.Count, 3);

                var stmt0 = await stmts[0].Use<string>(stmt => stmt.SQL);
                var stmt1 = await stmts[1].Use<string>(stmt => stmt.SQL);
                var stmt2 = await stmts[2].Use<string>(stmt => stmt.SQL);

                Assert.Equal(stmt0, "SELECT * FROM foo;");
                Assert.Equal(stmt1, "SELECT x FROM foo;");
                Assert.Equal(stmt2, "SELECT rowid, x FROM foo;");
            }
        }

        [Fact]
        public async Task TestQuery()
        {
            using (var adb = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                var _0 = "hello";
                var _1 = 1;
                var _2 = true;

                var result =
                    await adb.Query("Select ?, ?, ?", _0, _1, _2)
                        .Select(row => Tuple.Create(row[0].ToString(), row[1].ToInt(), row[2].ToInt()))
                        .FirstAsync();

                Assert.Equal(result.Item1, _0);
                Assert.Equal(result.Item2, _1);
                Assert.Equal(result.Item3 != 0, _2);
            }
        }

        [Fact]
        public void TestStatementCancellation()
        {
            var builder = SQLiteDatabaseConnectionBuilder.InMemory.With(progressHandlerInterval: 1);

            using (var adb =  builder.BuildAsyncDatabaseConnection(TaskPoolScheduler.Default))
            {
                var cts = new CancellationTokenSource();
                Assert.ThrowsAsync<TaskCanceledException>(async () =>
                    await adb.Use((db, ct) => 
                        {
                            cts.Cancel();
                            db.Execute("Select 1;");
                            Assert.True(false, "Expected exception to be thrown.");
                        }, cts.Token));
            }
        }
    }
}

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
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class AsyncDatabaseConnectionTests
    {
        [Test]
        public async Task TestProfileEvent()
        {
            using (var db = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                var statement = "CREATE TABLE foo (x int);";
                db.Profile.Subscribe(e =>
                    {
                        Assert.AreEqual(statement, e.Statement);
                        Assert.Less(TimeSpan.MinValue, e.ExecutionTime);
                    });

                await db.ExecuteAsync(statement);
            }
        }

        [Test]
        public async Task TestTraceEvent()
        {
            using (var db = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                var statement = "CREATE TABLE foo (x int);";
                db.Trace.Subscribe(e =>
                    {
                        Assert.AreEqual(statement, e.Statement);
                    });

                await db.ExecuteAsync(statement);

                statement = "INSERT INTO foo (x) VALUES (1);";
                await db.ExecuteAsync(statement);
            }
        }

        [Test]
        public async Task TestUpdateEvent()
        {
            using (var db = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                var currentAction = ActionCode.CreateTable;
                var rowid = 1;

                db.Update.Subscribe(e =>
                    {
                        Assert.AreEqual(currentAction, e.Action);
                        Assert.AreEqual("main", e.Database);
                        Assert.AreEqual("foo", e.Table);
                        Assert.AreEqual(rowid, e.RowId);
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

        [Test]
        public async Task TestUse()
        {
            using (var adb = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                await adb.Use(db => Enumerable.Range(0, 1000))
                    .Scan(Tuple.Create<int, int>(-1, -1), (x, y) => Tuple.Create(x.Item1 + 1, y))
                    .Do(result => 
                        {
                            Assert.AreEqual(result.Item2, result.Item1);
                        });

                int expected = 10;
                var t = await adb.Use(db =>
                    {
                        return db.Query("Select ?", expected).Select(row => row[0].ToInt()).First();
                    });
                Assert.AreEqual(t, expected);

                var anotherUse = adb.Use(db => Enumerable.Range(0, 1000));;
                
                adb.Dispose();

                Assert.Throws<ObjectDisposedException>(() => adb.Use(db => Enumerable.Range(0, 1000)));
                Assert.Throws<ObjectDisposedException>(async () => { await anotherUse; });
                Assert.Throws<ObjectDisposedException>(() => adb.Use(db => { }));
            }
        }

        [Test]
        public async Task TestIDatabaseConnectionDispose()
        { 
            using (var adb = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                await adb.ExecuteAsync("CREATE TABLE foo (x int);");

                await adb.Use(db => {
                    Assert.DoesNotThrow(() => { var x = db.IsAutoCommit; });
                    Assert.DoesNotThrow(() => { var x = db.Changes; });
                    Assert.DoesNotThrow(() => { var x = db.LastInsertedRowId; });
                    Assert.DoesNotThrow(() => { var x = db.Statements; });
                    Assert.DoesNotThrow(() => { using (var stmt = db.PrepareStatement("SELECT x FROM foo;")) { } });

                    string filename;
                    Assert.DoesNotThrow(() => { var x = db.TryGetFileName("main", out filename); });
 
                    db.Dispose();

                    Assert.Throws<ObjectDisposedException>(() => { var x = db.IsAutoCommit; });
                    Assert.Throws<ObjectDisposedException>(() => { var x = db.Changes; });
                    Assert.Throws<ObjectDisposedException>(() => { var x = db.LastInsertedRowId; });
                    Assert.Throws<ObjectDisposedException>(() => { var x = db.Statements; });
                    Assert.Throws<ObjectDisposedException>(() => { var x = db.OpenBlob("", "", "", 0, true); });
                    Assert.Throws<ObjectDisposedException>(() => db.PrepareStatement("SELECT x FROM foo;"));
                    Assert.Throws<ObjectDisposedException>(() => { var x = db.TryGetFileName("main", out filename); });
                });

                await adb.Use(db =>
                    {
                        // Assert that the database is not disposed, despite the previous user disposing it's instance.
                        Assert.DoesNotThrow(() => { var x = db.IsAutoCommit; });
                    });

                // Test that subscribe doesn't throw after Dispose() is called.
                var disposedObservable = adb.Use(db => Enumerable.Empty<Unit>());
                adb.Dispose();
                await disposedObservable.Materialize()
                    .Do(x =>
                        {
                            Assert.AreEqual(x.Kind, NotificationKind.OnError);
                            Assert.IsInstanceOf<ObjectDisposedException>(x.Exception);
                        });
            }
        }

        [Test]
        public async Task TestIDatabaseConnectionUnsupportedMethods()
        {
            using (var adb = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                await adb.ExecuteAsync("CREATE TABLE foo (x int);");

                await adb.Use(db => {
                    Assert.Throws(typeof(NotSupportedException), () => db.Rollback += (o, e) => { });
                    Assert.Throws(typeof(NotSupportedException), () => db.Rollback -= (o, e) => { });
                    Assert.Throws(typeof(NotSupportedException), () => db.Trace += (o, e) => { });
                    Assert.Throws(typeof(NotSupportedException), () => db.Trace -= (o, e) => { });
                    Assert.Throws(typeof(NotSupportedException), () => db.Profile += (o, e) => { });
                    Assert.Throws(typeof(NotSupportedException), () => db.Profile -= (o, e) => { });
                    Assert.Throws(typeof(NotSupportedException), () => db.Update += (o, e) => { });
                    Assert.Throws(typeof(NotSupportedException), () => db.Update -= (o, e) => { });
                    Assert.Throws(typeof(NotSupportedException), () => db.BusyTimeout = TimeSpan.MaxValue);

                    Assert.Throws(typeof(NotSupportedException), () => db.RegisterCollation("test", (a, b) => 1));
                    Assert.Throws(typeof(NotSupportedException), () => db.RemoveCollation("test"));
                    Assert.Throws(typeof(NotSupportedException), () => db.RegisterCommitHook(() => false));
                    Assert.Throws(typeof(NotSupportedException), () => db.RemoveCommitHook());
                    Assert.Throws(typeof(NotSupportedException), () => db.RegisterAggregateFunc("test", "", (string a, ISQLiteValue b) => a, a => a.ToSQLiteValue()));
                    Assert.Throws(typeof(NotSupportedException), () => db.RegisterScalarFunc("test", (a, b) => a));
                    Assert.Throws(typeof(NotSupportedException), () => db.RemoveFunc("test", 2));
                });
            }
        }

        [Test]
        public void TestUseCancelled()
        {
            using (var adb = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                Assert.That(async () => await adb.Use(_ => { }, cts.Token), Throws.TypeOf<TaskCanceledException>());

                cts = new CancellationTokenSource();
                Assert.That(async () => await adb.Use(_ => { cts.Cancel(); }, cts.Token), Throws.TypeOf<TaskCanceledException>());
            }
        }

        [Test]
        public async Task TestPrepareAllAsync()
        {
            using (var adb = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                await adb.ExecuteAsync("CREATE TABLE foo (x int);");
                var stmts = 
                    await adb.PrepareAllAsync(
                        "SELECT * FROM foo;" +
                        "SELECT x FROM foo;" +
                        "SELECT rowid, x FROM foo;");
                Assert.AreEqual(stmts.Count, 3);

                var stmt0 = await stmts[0].Use<String>(stmt => stmt.SQL);
                var stmt1 = await stmts[1].Use<String>(stmt => stmt.SQL);
                var stmt2 = await stmts[2].Use<String>(stmt => stmt.SQL);

                Assert.AreEqual(stmt0, "SELECT * FROM foo;");
                Assert.AreEqual(stmt1, "SELECT x FROM foo;");
                Assert.AreEqual(stmt2, "SELECT rowid, x FROM foo;");
            }
        }

        [Test]
        public async Task TestQuery()
        {
            using (var adb = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                var _0 = "hello";
                var _1 = 1;
                var _2 = true;

                var result =
                    await adb.Query("Select ?, ?, ?", _0, _1, _2)
                        .Select(row => Tuple.Create(row[0].ToString(), row[1].ToInt(), row[2].ToInt()))
                        .FirstAsync();

                Assert.AreEqual(result.Item1, _0);
                Assert.AreEqual(result.Item2, _1);
                Assert.AreEqual(result.Item3 != 0, _2);
            }
        }
    }
}

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

                var anotherUse = adb.Use(db => Enumerable.Range(0, 1000));
                
                adb.Dispose();

                Assert.Throws(typeof(ObjectDisposedException), () => adb.Use(db => Enumerable.Range(0, 1000)));
                Assert.Throws(typeof(ObjectDisposedException), async () => { await anotherUse; });
            }
        }
    }
}

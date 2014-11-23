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
        // The using pattern results in deadlocks when IAsyncDatabaseConnection.Use() is awaited
        // on a task pool thread. The issue is that the task scheduler will schedule calls to 
        // Dispose() immediately on the same thread as callback function to Use is run on, 
        // resulting in Dispose blocking, waiting for the connections internal queue of work to 
        // complete, which never happens, as Dispose() is blocking completion (ie. deadlock). 
        // To work around this in unit test we add this synchronous test runner function.
        //
        // Note for most user code this isn't actually a problem as await calls made from the 
        // application event loop always result in the continuation being queued on the callers
        // eventloop synchronization context by default.
        internal static void DoAsyncTest(Func<IAsyncDatabaseConnection, Task> f)
        {
            using (var db = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
            {
                f(db).Wait();
            }
        }


        [Test]
        public void TestUse()
        {
            DoAsyncTest(async db =>
                {
                    Console.WriteLine("root thread:" + Thread.CurrentThread.ManagedThreadId);
                    db.Use(conn =>
                        {
                            Console.WriteLine("excute thread:" + Thread.CurrentThread.ManagedThreadId);
                            conn.ExecuteAll(
                                @"CREATE TABLE foo (x int);
                                  INSERT INTO foo (x) VALUES (1);
                                  INSERT INTO foo (x) VALUES (2);
                                  INSERT INTO foo (x) VALUES (3);");
                        }).Wait();

                    var x = db.Query("SELECT * FROM foo;")
                                .Select(result =>
                                    {
                                        Console.WriteLine("x result thread" + Thread.CurrentThread.ManagedThreadId + " " + result[0].ToInt());
                                        return result[0].ToInt();
                                    });

                    var y = db.Query("SELECT * FROM foo;")
                                .Select(result =>
                                    {
                                        Console.WriteLine("y result thread" + Thread.CurrentThread.ManagedThreadId + " " + result[0].ToInt());
                                        return result[0].ToInt();
                                    });

                    await Task.WhenAll(y.ToTask(), x.ToTask(), y.ToTask(), x.ToTask(), y.ToTask());
                });
        }
    }
}

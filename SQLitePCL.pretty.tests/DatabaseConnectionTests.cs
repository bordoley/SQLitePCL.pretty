/*
   Copyright 2014 David Bordoley
   Copyright 2014 Zumero, LLC

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
using System.IO;
using System.Linq;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class SQLiteDatabaseConnectionTests
    {
        [Test]
        public void TestDispose()
        {
            var db = SQLite3.Open(":memory:");

            // This is for test purposes only, never prepare a statement and dispose the db before the statements.
            db.PrepareStatement("Select 1");
            db.PrepareStatement("Select 2");

            var stmtEnumerator = db.Statements.GetEnumerator();

            db.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { stmtEnumerator.MoveNext(); });

            Assert.Throws<ObjectDisposedException>(() => { db.BusyTimeout = TimeSpan.MaxValue; });
            Assert.Throws<ObjectDisposedException>(() => { var x = db.Changes; });
            Assert.Throws<ObjectDisposedException>(() => { var x = db.IsAutoCommit; });
            Assert.Throws<ObjectDisposedException>(() => { var x = db.LastInsertedRowId; });
            Assert.Throws<ObjectDisposedException>(() => { var x = db.Statements; });

            using (var db2 = SQLite3.Open(":memory:"))
            {
                Assert.Throws<ObjectDisposedException>(() => { db.Backup("main", db2, "main"); });
            }

            Assert.Throws<ObjectDisposedException>(() => { var x = db.GetFileName("main"); });
            Assert.Throws<ObjectDisposedException>(() => { var x = db.OpenBlob("db", "tn", "cn", 0, false); });
            Assert.Throws<ObjectDisposedException>(() => { var x = db.PrepareStatement("SELECT 1"); });
            Assert.Throws<ObjectDisposedException>(() => { db.RegisterCollation("test", (a, b) => 1); });
            Assert.Throws<ObjectDisposedException>(() => { db.RemoveCollation("test"); });
            Assert.Throws<ObjectDisposedException>(() => { db.RegisterCommitHook(() => false); });
            Assert.Throws<ObjectDisposedException>(() => { db.RemoveCommitHook(); });
            Assert.Throws<ObjectDisposedException>(() => { db.RegisterAggregateFunc("name", null, (string a, ISQLiteValue b) => a, t => "".ToSQLiteValue()); });
            Assert.Throws<ObjectDisposedException>(() => { db.RegisterScalarFunc("name", () => "p".ToSQLiteValue()); });
            Assert.Throws<ObjectDisposedException>(() => { db.RemoveFunc("name", 0); });
        }

        [Test]
        public void TestRollbackEvent()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                var rollbacks = 0;

                db.Rollback += (o, e) => rollbacks++;
                Assert.AreEqual(rollbacks, 0);

                db.ExecuteAll(
                    @"CREATE TABLE foo (x int);
                      INSERT INTO foo (x) VALUES (1);
                      BEGIN TRANSACTION;
                      INSERT INTO foo (x) VALUES (2);
                      ROLLBACK TRANSACTION;
                      BEGIN TRANSACTION;
                      INSERT INTO foo (x) VALUES (2);
                      ROLLBACK TRANSACTION;");

                Assert.AreEqual(rollbacks, 2);
            }
        }

        [Test]
        public void TestProfileEvent()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                var statement = "CREATE TABLE foo (x int);";
                db.Profile += (o, e) =>
                    {
                        Assert.AreEqual(statement, e.Statement);
                        Assert.Less(TimeSpan.MinValue, e.ExecutionTime);
                    };

                db.Execute(statement);
            }
        }

        [Test]
        public void TestTraceEvent()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                var statement = "CREATE TABLE foo (x int);";
                db.Trace += (o, e) =>
                    {
                        Assert.AreEqual(statement, e.Statement);
                    };

                db.Execute(statement);

                statement = "INSERT INTO foo (x) VALUES (1);";
                db.Execute(statement);
            }
        }

        [Test]
        public void TestUpdateEvent()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                var currentAction = ActionCode.CreateTable;
                var rowid = 1;

                db.Update += (o, e) =>
                    {
                        Assert.AreEqual(currentAction, e.Action);
                        Assert.AreEqual("main", e.Database);
                        Assert.AreEqual("foo", e.Table);
                        Assert.AreEqual(rowid, e.RowId);
                    };

                currentAction = ActionCode.CreateTable;
                rowid = 1;
                db.Execute("CREATE TABLE foo (x int);");

                currentAction = ActionCode.Insert;
                rowid = 1;
                db.Execute("INSERT INTO foo (x) VALUES (1);");

                currentAction = ActionCode.Insert;
                rowid = 2;
                db.Execute("INSERT INTO foo (x) VALUES (2);");

                currentAction = ActionCode.DropTable;
                rowid = 2;
                db.Execute("DROP TABLE foo");
            }
        }

        [Test]
        public void TestBusyTimeout()
        {
            //Assert.Fail("Implement me");
        }

        [Test]
        public void TestChanges()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int);");
                db.Execute("INSERT INTO foo (x) VALUES (1);");
                Assert.AreEqual(db.Changes, 1);
                db.Execute("INSERT INTO foo (x) VALUES (2);");
                db.Execute("INSERT INTO foo (x) VALUES (3);");
                db.Execute("UPDATE foo SET x=5;");
                Assert.AreEqual(db.Changes, 3);
            }
        }

        [Test]
        public void TestIsAutoCommit()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                Assert.IsTrue(db.IsAutoCommit);
                db.Execute("BEGIN TRANSACTION;");
                Assert.IsFalse(db.IsAutoCommit);
            }
        }

        [Test]
        public void TestSetBusyTimeout()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                // FIXME: Not the best test without Asserts.
                db.BusyTimeout = TimeSpan.MinValue;
                db.BusyTimeout = new TimeSpan(100);
            }
        }

        [Test]
        public void TestStatements()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                Assert.AreEqual(db.Statements.Count(), 0);

                using (IStatement stmt0 = db.PrepareStatement("SELECT 5;"),
                                  stmt1 = db.PrepareStatement("SELECT 6;"),
                                  stmt2 = db.PrepareStatement("SELECT 7;"))
                {
                    Assert.AreEqual(db.Statements.Count(), 3);

                    IStatement[] stmts = { stmt2, stmt1, stmt0 };

                    // IStatement can't sanely implement equality at the
                    // interface level. Doing so would tightly bind the
                    // interface to the underlying SQLite implementation
                    // which we don't want to do.
                    foreach (var pair in Enumerable.Zip(stmts, db.Statements, (a, b) => Tuple.Create(a.SQL, b.SQL)))
                    {
                        Assert.AreEqual(pair.Item1, pair.Item2);
                    }
                }
            }
        }

        [Test]
        public void TestTryGetFileName()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int);");
                string filename = null;
                Assert.False(db.TryGetFileName("foo", out filename));
                Assert.IsNull(filename);
            }

            var tempFile = Path.GetTempFileName();
            using (var db = SQLite3.Open(tempFile))
            {
                db.Execute("CREATE TABLE foo (x int);");
                string filename = null;
                Assert.True(db.TryGetFileName("main", out filename));
                Assert.AreEqual(tempFile, filename);
            }
            File.Delete(tempFile);
        }

        [Test]
        public void TestRegisterCollation()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterCollation("e2a", (string s1, string s2) =>
                {
                    s1 = s1.Replace('e', 'a');
                    s2 = s2.Replace('e', 'a');
                    return string.Compare(s1, s2);
                });

                db.Execute("CREATE TABLE foo (x text COLLATE e2a);");
                db.Execute("INSERT INTO foo (x) VALUES ('b')");
                db.Execute("INSERT INTO foo (x) VALUES ('c')");
                db.Execute("INSERT INTO foo (x) VALUES ('d')");
                db.Execute("INSERT INTO foo (x) VALUES ('e')");
                db.Execute("INSERT INTO foo (x) VALUES ('f')");

                string top =
                    db.Query("SELECT x FROM foo ORDER BY x ASC LIMIT 1;")
                        .Select(x => x[0].ToString())
                        .First();

                Assert.AreEqual(top, "e");

                db.RemoveCollation("e2a");
                Assert.Throws<SQLiteException>(() => db.Execute("CREATE TABLE bar (x text COLLATE e2a);"));
            }
        }

        [Test]
        public void TestRegisterCommitHook()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                var commits = 0;
                db.RegisterCommitHook(() =>
                    {
                        commits++;
                        return false;
                    });

                db.Execute("CREATE TABLE foo (x int);");
                db.Execute("INSERT INTO foo (x) VALUES (1);");

                Assert.AreEqual(2, commits);

                db.RemoveCommitHook();
                db.Execute("INSERT INTO foo (x) VALUES (1);");
                Assert.AreEqual(2, commits);
            }

            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int);");
                db.RegisterCommitHook(() =>
                {
                    return true;
                });

                try
                {
                    db.Execute("INSERT INTO foo (x) VALUES (1);");
                    Assert.Fail();
                }
                catch (SQLiteException e)
                {
                    Assert.AreEqual(ErrorCode.ConstraintCommitHook, e.ExtendedErrorCode);
                }

                var count =
                    db.Query("SELECT COUNT(*) from foo")
                        .Select(row => row.First().ToInt())
                        .First();
                Assert.AreEqual(0, count);
            }
        }

        [Test]
        public void TestRegisterAggregateFunc()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterAggregateFunc<Tuple<long, long>>("sum_plus_count", Tuple.Create(0L, 0L),
                    (Tuple<long, long> acc, ISQLiteValue arg) => Tuple.Create(acc.Item1 + arg.ToInt64(), acc.Item2 + 1L),
                    (Tuple<long, long> acc) => (acc.Item1 + acc.Item2).ToSQLiteValue());
                
                db.Execute("CREATE TABLE foo (x int);");
                for (int i = 0; i < 5; i++)
                {
                    db.Execute("INSERT INTO foo (x) VALUES (?);", i);
                }
                long c = db.Query("SELECT sum_plus_count(x) FROM foo;").Select(row => row[0].ToInt64()).First();
                Assert.AreEqual(c, (0 + 1 + 2 + 3 + 4) + 5);

                db.RemoveFunc("sum_plus_count", 1);
                Assert.Throws<SQLiteException>(() => 
                    db.Query("SELECT sum_plus_count(x) FROM foo;")
                        .Select(row => row[0].ToInt64())
                        .First());
            }
        }

        [Test]
        public void TestRegisterScalarFunc()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterScalarFunc("count_nulls", (IReadOnlyList<ISQLiteValue> vals) =>
                    vals.Where(val => val.SQLiteType == SQLiteType.Null).Count().ToSQLiteValue());

                Assert.AreEqual(0, db.Query("SELECT count_nulls(1,2,3,4,5,6,7,8);").Select(v => v[0].ToInt()).First());
                Assert.AreEqual(0, db.Query("SELECT count_nulls();").Select(v => v[0].ToInt()).First());
                Assert.AreEqual(1, db.Query("SELECT count_nulls(null);").Select(v => v[0].ToInt()).First());
                Assert.AreEqual(2, db.Query("SELECT count_nulls(1,null,3,null,5);").Select(v => v[0].ToInt()).First());

                db.RemoveFunc("count_nulls", -1);
                Assert.Throws<SQLiteException>(() =>
                    db.Query("SELECT count_nulls(1,2,3,4,5,6,7,8);")
                        .Select(row => row[0].ToInt64())
                        .First());
            }

            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterScalarFunc("count_args", (IReadOnlyList<ISQLiteValue> values) => values.Count.ToSQLiteValue());
                Assert.AreEqual(8, db.Query("SELECT count_args(1,2,3,4,5,6,7,8);").Select(v => v[0].ToInt()).First());
                Assert.AreEqual(0, db.Query("SELECT count_args();").Select(v => v[0].ToInt()).First());
                Assert.AreEqual(1, db.Query("SELECT count_args(null);").Select(v => v[0].ToInt()).First());
            }

            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterScalarFunc("len_as_blobs", (IReadOnlyList<ISQLiteValue> values) =>
                    values.Where(v => v.SQLiteType != SQLiteType.Null).Aggregate(0, (acc, val) => acc + val.Length).ToSQLiteValue());
                Assert.AreEqual(0, db.Query("SELECT len_as_blobs();").Select(v => v[0].ToInt()).First());
                Assert.AreEqual(0, db.Query("SELECT len_as_blobs(null);").Select(v => v[0].ToInt()).First());
                Assert.IsTrue(8 <= db.Query("SELECT len_as_blobs(1,2,3,4,5,6,7,8);").Select(v => v[0].ToInt()).First());
            }

            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterScalarFunc("my_concat", (IReadOnlyList<ISQLiteValue> values) =>
                    string.Join("", values.Select(v => v.ToString())).ToSQLiteValue());
                Assert.AreEqual("foobar", db.Query("SELECT my_concat('foo', 'bar');").Select(v => v[0].ToString()).First());
                Assert.AreEqual("abc", db.Query("SELECT my_concat('a', 'b', 'c');").Select(v => v[0].ToString()).First());
            }

            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterScalarFunc("my_mean", (IReadOnlyList<ISQLiteValue> values) =>
                    (values.Aggregate(0d, (acc, v) => acc + v.ToDouble()) / values.Count).ToSQLiteValue());

                var result = db.Query("SELECT my_mean(1,2,3,4,5,6,7,8);").Select(rs => rs[0].ToDouble()).First();
                Assert.IsTrue(result >= (36 / 8));
                Assert.IsTrue(result <= (36 / 8 + 1));
            }

            using (var db = SQLite3.Open(":memory:"))
            {
                int val = 5;
                db.RegisterScalarFunc("makeblob", (ISQLiteValue v) =>
                {
                    byte[] b = new byte[v.ToInt()];
                    for (int i = 0; i < b.Length; i++)
                    {
                        b[i] = (byte)(i % 256);
                    }
                    return b.ToSQLiteValue();
                });

                var c = db.Query("SELECT makeblob(?);", val).Select(rs => rs[0].ToBlob()).First();
                Assert.AreEqual(c.Length, val);
            }

            using (var db = SQLite3.Open(":memory:"))
            {
                int val = 5;

                db.RegisterScalarFunc("cube", (ISQLiteValue x) => (x.ToInt64() * x.ToInt64() * x.ToInt64()).ToSQLiteValue());
                var c = db.Query("SELECT cube(?);", val).Select(rs => rs[0].ToInt64()).First();
                Assert.AreEqual(c, val * val * val);
            }
        }
    }
}

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
    public class DatabaseBackupTests
    {
        [Test]
        public void TestBackupWithPageStepping()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int);");
                foreach (int i in Enumerable.Range(0, 1000))
                {
                    db.Execute("INSERT INTO foo (x) VALUES (?);", i);
                }

                using (var db2 = SQLite3.Open(":memory:"))
                {
                    using (var backup = db.BackupInit("main", db2, "main"))
                    {
                        Assert.AreEqual(0, backup.RemainingPages);
                        Assert.AreEqual(0, backup.PageCount);

                        backup.Step(1);
                        var remainingPages = backup.RemainingPages;

                        while (backup.Step(1))
                        {
                            Assert.Less(backup.RemainingPages, remainingPages);
                            remainingPages = backup.RemainingPages;
                        }

                        Assert.IsFalse(backup.Step(2));
                        Assert.IsFalse(backup.Step(-1));
                        Assert.AreEqual(backup.RemainingPages, 0);
                        Assert.IsTrue(backup.PageCount > 0);
                    }
                }
            }
        }

        [Test]
        public void TestBackup()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int);");
                foreach (int i in Enumerable.Range(0, 1000))
                {
                    db.Execute("INSERT INTO foo (x) VALUES (?);", i);
                }

                using (var db2 = SQLite3.Open(":memory:"))
                {
                    using (var backup = db.BackupInit("main", db2, "main"))
                    {
                        Assert.AreEqual(0, backup.RemainingPages);
                        Assert.AreEqual(0, backup.PageCount);

                        Assert.IsFalse(backup.Step(-1));

                        Assert.AreEqual(backup.RemainingPages, 0);
                        Assert.IsTrue(backup.PageCount > 0);
                    }
                }
            }
        }
    }

    [TestFixture]
    public class StatementTests
    {
        [Test]
        public void TestBusy()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.ExecuteAll(
                    @"CREATE TABLE foo (x int);
                      INSERT INTO foo (x) VALUES (1);
                      INSERT INTO foo (x) VALUES (2);
                      INSERT INTO foo (x) VALUES (3);");

                using (var stmt = db.PrepareStatement("SELECT x FROM foo;"))
                {
                    Assert.IsFalse(stmt.IsBusy);
                    stmt.MoveNext();
                    Assert.IsTrue(stmt.IsBusy);
                    stmt.MoveNext();
                    Assert.IsTrue(stmt.IsBusy);
                    stmt.MoveNext();
                    Assert.IsTrue(stmt.IsBusy);
                    stmt.MoveNext();
                    Assert.IsFalse(stmt.IsBusy);
                }
            }
        }

        [Test]
        public void TestBindParameterCount()
        {
            Tuple<String, int>[] tests =
            {
                Tuple.Create("CREATE TABLE foo (x int)", 0),
                Tuple.Create("CREATE TABLE foo2 (x int, y int)", 0),
                Tuple.Create("select * from foo", 0),
                Tuple.Create("INSERT INTO foo (x) VALUES (?)", 1),
                Tuple.Create("INSERT INTO foo2 (x, y) VALUES (?, ?)", 2)
            };

            using (var db = SQLite3.Open(":memory:"))
            {
                foreach (var test in tests)
                {
                    using (var stmt = db.PrepareStatement(test.Item1))
                    {
                        Assert.AreEqual(test.Item2, stmt.BindParameters.Count);
                        stmt.MoveNext();
                    }
                }
            }
        }

        [Test]
        public void TestReadOnly()
        {
            Tuple<String, bool>[] tests =
            {
                Tuple.Create("CREATE TABLE foo (x int)", false),
                Tuple.Create("CREATE TABLE foo2 (x int, y int)", false),
                Tuple.Create("select * from foo", true),
                Tuple.Create("INSERT INTO foo (x) VALUES (?)", false),
                Tuple.Create("INSERT INTO foo2 (x, y) VALUES (?, ?)", false)
            };

            using (var db = SQLite3.Open(":memory:"))
            {
                foreach (var test in tests)
                {
                    using (var stmt = db.PrepareStatement(test.Item1))
                    {
                        Assert.AreEqual(test.Item2, stmt.IsReadOnly);
                        stmt.MoveNext();
                    }
                }
            }
        }

        [Test]
        public void TestGetSQL()
        {
            String[] sql =
            {
                "CREATE TABLE foo (x int)",
                "INSERT INTO foo (x) VALUES (1)",
                "INSERT INTO foo (x) VALUES (2)",
                "INSERT INTO foo (x) VALUES (3)",
                "SELECT x FROM foo",
            };

            using (var db = SQLite3.Open(":memory:"))
            {
                foreach (var sqlStmt in sql)
                {
                    using (var stmt = db.PrepareStatement(sqlStmt))
                    {
                        stmt.MoveNext();

                        Assert.AreEqual(sqlStmt, stmt.SQL);
                    }
                }
            }
        }

        [Test]
        public void TestTryGetBindParameterIndex()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int, v int, t text, d real, b blob, q blob);");

                using (var stmt = db.PrepareStatement("INSERT INTO foo (x,v,t,d,b,q) VALUES (:x,:v,:t,:d,:b,:q)"))
                {
                    Action<string> test = (string key) =>
                        {
                            var param = stmt.BindParameters[key];
                            Assert.AreEqual(key, param.Name);
                        };

                    Assert.Throws(typeof(KeyNotFoundException), () => test(":m"));
                    test(":x");
                    test(":v");
                    test(":t");
                    test(":d");
                    test(":b");
                    test(":q");
                }
            }
        }

        [Test]
        public void TestGetBindParameterName()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int, v int, t text, d real, b blob, q blob);");

                using (var stmt = db.PrepareStatement("INSERT INTO foo (x,v,t,d,b,q) VALUES (:x,:v,:t,:d,:b,:q)"))
                {
                    Assert.AreEqual(stmt.BindParameters[0].Name, ":x");
                    Assert.AreEqual(stmt.BindParameters[1].Name, ":v");
                    Assert.AreEqual(stmt.BindParameters[2].Name, ":t");
                    Assert.AreEqual(stmt.BindParameters[3].Name, ":d");
                    Assert.AreEqual(stmt.BindParameters[4].Name, ":b");
                    Assert.AreEqual(stmt.BindParameters[5].Name, ":q");
                }
            }
        }

        [Test]
        public void TestClearBindings()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int, v int);");

                using (var stmt = db.PrepareStatement("INSERT INTO foo (x,v) VALUES (:x,:v)"))
                {
                    stmt.BindParameters[0].Bind(1);
                    stmt.BindParameters[1].Bind(2);
                    stmt.MoveNext();

                    stmt.Reset();
                    stmt.ClearBindings();
                    stmt.MoveNext();
                }

                var last =
                    db.Query("SELECT * from FOO")
                        .Select(row => Tuple.Create(row[0].ToInt(), row[1].ToInt()))
                        .Last();

                Assert.AreEqual(last.Item1, 0);
                Assert.AreEqual(last.Item2, 0);
            }
        }
    }

    [TestFixture]
    public class ResultSetTests
    {
        [Test]
        public void TestCount()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.ExecuteAll(
                    @"CREATE TABLE foo (x int, v int);
                      INSERT INTO foo (x, v) VALUES (1, 2);
                      INSERT INTO foo (x, v) VALUES (2, 3);");

                foreach (var row in db.Query("select * from foo"))
                {
                    Assert.AreEqual(row.Count, 2);
                }

                foreach (var row in db.Query("select x from foo"))
                {
                    Assert.AreEqual(row.Count, 1);
                }
            }
        }

        [Test]
        public void TestBracketOp()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.ExecuteAll(
                    @"CREATE TABLE foo (x int, v int);
                      INSERT INTO foo (x, v) VALUES (1, 2);
                      INSERT INTO foo (x, v) VALUES (2, 3);");

                foreach (var row in db.Query("select * from foo"))
                {
                    Assert.Throws(typeof(ArgumentOutOfRangeException), () => { var x = row[-1]; });
                    Assert.Throws(typeof(ArgumentOutOfRangeException), () => { var x = row[row.Count]; });

                    Assert.AreEqual(row[0].SQLiteType, SQLiteType.Integer);
                    Assert.AreEqual(row[1].SQLiteType, SQLiteType.Integer);
                }
            }
        }
    }

    [TestFixture]
    public class BlobStreamTests
    {
        [Test]
        public void TestRead()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                byte[] bytes = new byte[1000];
                Random random = new Random();
                random.NextBytes(bytes);

                db.Execute("CREATE TABLE foo (x blob);");
                db.Execute("INSERT INTO foo (x) VALUES(?);", bytes);

                db.Query("SELECT rowid, x FROM foo;")
                    .Select(row =>
                        {
                            using (var stream = db.OpenBlob(row[1], row[0].ToInt64()))
                            {
                                Assert.True(stream.CanRead);
                                Assert.False(stream.CanWrite);

                                for (int i = 0; i < stream.Length; i++)
                                {
                                    int b = stream.ReadByte();
                                    Assert.AreEqual(bytes[i], b);
                                }
                            }

                            return row;
                        }).First();
            }
        }

        [Test]
        public void TestWrite()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                byte[] bytes = new byte[1000];
                Random random = new Random();
                random.NextBytes(bytes);

                var source = new MemoryStream(bytes);

                db.Execute("CREATE TABLE foo (x blob);");
                db.Execute("INSERT INTO foo (x) VALUES(?);", source);
                db.Query("SELECT rowid, x FROM foo")
                    .Select(row =>
                        {
                            using (var stream = db.OpenBlob(row[1], row[0].ToInt64(), true))
                            {
                                Assert.True(stream.CanRead);
                                Assert.True(stream.CanWrite);
                                source.CopyTo(stream);
                            }
                            return row;
                        })
                    .First();

                db.Query("SELECT rowid, x FROM foo;")
                    .Select(row =>
                        {
                            using (var stream = db.OpenBlob(row[1], row[0].ToInt64()))
                            {
                                for (int i = 0; i < stream.Length; i++)
                                {
                                    int b = stream.ReadByte();
                                    Assert.AreEqual(bytes[i], b);
                                }
                            }

                            return row;
                        }).First();
            }
        }
    }
}

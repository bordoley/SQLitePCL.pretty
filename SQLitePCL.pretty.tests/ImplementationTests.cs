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
        public void TestDispose()
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
                    var backup = db.BackupInit("main", db2, "main");
                    backup.Dispose();

                    Assert.Throws<ObjectDisposedException>(() => { var x = backup.PageCount; });
                    Assert.Throws<ObjectDisposedException>(() => { var x = backup.RemainingPages; });
                    Assert.Throws<ObjectDisposedException>(() => { backup.Step(1); });
                }
            }
        }

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

                using (var db3 = SQLite3.Open(":memory:"))
                {
                    db.Backup("main", db3, "main");
                    var backupResults = Enumerable.Zip(
                        db.Query("SELECT x FROM foo"),
                        db3.Query("SELECT x FROM foo"),
                        Tuple.Create);

                    foreach (var pair in backupResults)
                    {
                        Assert.AreEqual(pair.Item1[0].ToInt(), pair.Item2[0].ToInt());
                    }
                }
            }
        }
    }

    [TestFixture]
    public class StatementTests
    {
        [Test]
        public void TestDispose()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                var stmt = db.PrepareStatement("SELECT 1");
                stmt.Dispose();

                // Test double dispose
                stmt.Dispose();

                Assert.Throws<ObjectDisposedException>(() => { var x = stmt.BindParameters; });
                Assert.Throws<ObjectDisposedException>(() => { var x = stmt.Columns; });
                Assert.Throws<ObjectDisposedException>(() => { var x = stmt.Current; });
                Assert.Throws<ObjectDisposedException>(() => { var x = stmt.SQL; });
                Assert.Throws<ObjectDisposedException>(() => { var x = stmt.IsBusy; });
                Assert.Throws<ObjectDisposedException>(() => { var x = stmt.IsReadOnly; });
                Assert.Throws<ObjectDisposedException>(() => { stmt.ClearBindings(); });
                Assert.Throws<ObjectDisposedException>(() => { stmt.MoveNext(); });
                Assert.Throws<ObjectDisposedException>(() => { stmt.Reset(); });
            }
        }

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
        public void TestGetBindParameters()
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

                    Assert.AreEqual(stmt.BindParameters[":x"].Name, ":x");
                    Assert.AreEqual(stmt.BindParameters[":v"].Name, ":v");
                    Assert.AreEqual(stmt.BindParameters[":t"].Name, ":t");
                    Assert.AreEqual(stmt.BindParameters[":d"].Name, ":d");
                    Assert.AreEqual(stmt.BindParameters[":b"].Name, ":b");
                    Assert.AreEqual(stmt.BindParameters[":q"].Name, ":q");

                    Assert.True(stmt.BindParameters.ContainsKey(":x"));
                    Assert.False(stmt.BindParameters.ContainsKey(":nope"));
                    Assert.AreEqual(stmt.BindParameters.Keys.Count(), 6);
                    Assert.AreEqual(stmt.BindParameters.Values.Count(), 6);

                    Assert.Throws<KeyNotFoundException>(() => { var x = stmt.BindParameters[":nope"]; });
                    Assert.Throws<ArgumentOutOfRangeException>(() => { var x = stmt.BindParameters[-1]; });
                    Assert.Throws<ArgumentOutOfRangeException>(() => { var x = stmt.BindParameters[100]; });
                }
            }
        }

        [Test]
        public void TestExecute()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (v int);");
                using (var stmt = db.PrepareStatement("INSERT INTO foo (v) VALUES (?)"))
                {
                    foreach (var i in Enumerable.Range(0, 100))
                    {
                        stmt.Execute(i);
                    }
                }

                foreach (var result in db.Query("SELECT v FROM foo ORDER BY 1").Select((v, index) => Tuple.Create(index, v[0].ToInt())))
                {
                    Assert.AreEqual(result.Item1, result.Item2);
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

        [Test]
        public void TestGetColumns()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                var count = 0;
                var stmt = db.PrepareStatement("SELECT 1 as a, 2 as a, 3 as a");
                foreach (var column in stmt.Columns)
                {
                    count++;
                    Assert.AreEqual(column.Name, "a");
                }

                Assert.Throws<ArgumentOutOfRangeException>(() => { var x = stmt.Columns[-1]; });
                Assert.Throws<ArgumentOutOfRangeException>(() => { var x = stmt.Columns[3]; });

                Assert.AreEqual(count, stmt.Columns.Count);
            }
        }
    }

    [TestFixture]
    public class BindParameters
    {
        [Test]
        public void TestBindObject()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (v int);");
                using (var stmt = db.PrepareStatement("INSERT INTO foo (v) VALUES (?)"))
                {
                    var stream = new MemoryStream();
                    stream.Dispose();
                    Assert.Throws<ArgumentException>(() => stmt.BindParameters[0].Bind(stream));
                    Assert.Throws<ArgumentException>(() => stmt.BindParameters[0].Bind(new object()));
                }
            }
        }

        [Test]
        public void TestBindSQLiteValue()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (v int);");
                using (var stmt = db.PrepareStatement("SELECT ?"))
                {
                    var param = stmt.BindParameters[0];
                    param.Bind(SQLiteValue.Null);
                    stmt.MoveNext();
                    var result = stmt.Current.First();
                    Assert.AreEqual(result.SQLiteType, SQLiteType.Null);

                    stmt.Reset();
                    param.Bind(new byte[0].ToSQLiteValue());
                    stmt.MoveNext();
                    result = stmt.Current.First();
                    Assert.AreEqual(result.SQLiteType, SQLiteType.Blob);
                    CollectionAssert.AreEqual(result.ToBlob(), new Byte[0]);

                    stmt.Reset();
                    param.Bind("test".ToSQLiteValue());
                    stmt.MoveNext();
                    result = stmt.Current.First();
                    Assert.AreEqual(result.SQLiteType, SQLiteType.Text);
                    Assert.AreEqual(result.ToString(), "test");

                    stmt.Reset();
                    param.Bind((1).ToSQLiteValue());
                    stmt.MoveNext();
                    result = stmt.Current.First();
                    Assert.AreEqual(result.SQLiteType, SQLiteType.Integer);
                    Assert.AreEqual(result.ToInt64(), 1);

                    stmt.Reset();
                    param.Bind((0.0).ToSQLiteValue());
                    stmt.MoveNext();
                    result = stmt.Current.First();
                    Assert.AreEqual(result.SQLiteType, SQLiteType.Float);
                    Assert.AreEqual(result.ToInt(), 0);
                }
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
                    Assert.Throws<ArgumentOutOfRangeException>(() => { var x = row[-1]; });
                    Assert.Throws<ArgumentOutOfRangeException>(() => { var x = row[row.Count]; });

                    Assert.AreEqual(row[0].SQLiteType, SQLiteType.Integer);
                    Assert.AreEqual(row[1].SQLiteType, SQLiteType.Integer);
                }
            }
        }

        [Test]
        public void TestColumns()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                foreach (var row in db.Query("SELECT 1 as a, 2 as b"))
                {
                    var columns = row.Columns();
                    Assert.AreEqual(columns[0].Name, "a");
                    Assert.AreEqual(columns[1].Name, "b");
                    Assert.AreEqual(columns.Count, 2);

                    var count = row.Columns().Count();

                    Assert.AreEqual(count, 2);
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

                var stream =
                    db.Query("SELECT rowid, x FROM foo;")
                        .Select(row => db.OpenBlob(row[1].ColumnInfo, row[0].ToInt64(), false))
                        .First();

                using (stream)
                {
                    Assert.True(stream.CanRead);
                    Assert.False(stream.CanWrite);
                    Assert.True(stream.CanSeek);

                    for (int i = 0; i < stream.Length; i++)
                    {
                        int b = stream.ReadByte();
                        Assert.AreEqual(bytes[i], b);
                    }

                    // Test input validation
                    Assert.Throws<ArgumentNullException>(() => stream.Read(null, 0, 1));
                    Assert.Throws<ArgumentException>(() => stream.Read(new byte[10], 6, 6));
                    Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(new byte[10], -5, 6));
                    Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(new byte[10], 5, -4));

                    // Since this is a read only stream, this is a good chance to test that writing fails
                    Assert.Throws<NotSupportedException>(() => stream.WriteByte(0));
                }
            }
        }

        [Test]
        public void TestDispose()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x blob);");
                db.Execute("INSERT INTO foo (x) VALUES(?);", "data");
                var blob =
                    db.Query("SELECT rowid, x FROM foo")
                        .Select(row => db.OpenBlob(row[1].ColumnInfo, row[0].ToInt64(), true))
                        .First();
                blob.Dispose();

                // Test double dispose doesn't crash
                blob.Dispose();

                Assert.Throws<ObjectDisposedException>(() => { var x = blob.Length; });
                Assert.Throws<ObjectDisposedException>(() => { var x = blob.Position; });
                Assert.Throws<ObjectDisposedException>(() => { blob.Position = 10; });
                Assert.Throws<ObjectDisposedException>(() => { blob.Read(new byte[10], 0, 2); });
                Assert.Throws<ObjectDisposedException>(() => { blob.Write(new byte[10], 0, 1); });
                Assert.Throws<ObjectDisposedException>(() => { blob.Seek(0, SeekOrigin.Begin); });
            }
        }

        [Test]
        public void TestSeek()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x blob);");
                db.Execute("INSERT INTO foo (x) VALUES(?);", "data");
                var blob =
                    db.Query("SELECT rowid, x FROM foo")
                        .Select(row => db.OpenBlob(row[1].ColumnInfo, row[0].ToInt64(), true))
                        .First();
                using (blob)
                {
                    Assert.True(blob.CanSeek);
                    Assert.Throws<NotSupportedException>(() => blob.SetLength(10));
                    Assert.Throws<ArgumentOutOfRangeException>(() => { blob.Position = -1; });
                    Assert.DoesNotThrow(() => { blob.Position = 100; });

                    // Test input validation
                    blob.Position = 5;
                    Assert.Throws<IOException>(() => blob.Seek(-10, SeekOrigin.Begin));
                    Assert.AreEqual(blob.Position, 5);
                    Assert.Throws<IOException>(() => blob.Seek(-10, SeekOrigin.Current));
                    Assert.AreEqual(blob.Position, 5);
                    Assert.Throws<IOException>(() => blob.Seek(-100, SeekOrigin.End));
                    Assert.AreEqual(blob.Position, 5);
                    Assert.Throws<ArgumentException>(() => blob.Seek(-100, (SeekOrigin)10));
                    Assert.AreEqual(blob.Position, 5);

                    blob.Seek(0, SeekOrigin.Begin);
                    Assert.AreEqual(blob.Position, 0);

                    blob.Seek(0, SeekOrigin.End);
                    Assert.AreEqual(blob.Position, blob.Length);

                    blob.Position = 5;
                    blob.Seek(2, SeekOrigin.Current);
                    Assert.AreEqual(blob.Position, 7);
                }
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

                var stream =
                    db.Query("SELECT rowid, x FROM foo")
                        .Select(row => db.OpenBlob(row[1].ColumnInfo, row[0].ToInt64(), true))
                        .First();

                using (stream)
                {
                    // Test input validation
                    Assert.Throws<ArgumentNullException>(() => stream.Write(null, 1, 1));
                    Assert.Throws<ArgumentException>(() => stream.Write(new byte[10], 6, 6));
                    Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(new byte[10], -5, 6));
                    Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(new byte[10], 5, -4));

                    Assert.True(stream.CanRead);
                    Assert.True(stream.CanWrite);
                    source.CopyTo(stream);

                    stream.Position = 0;

                    for (int i = 0; i < stream.Length; i++)
                    {
                        int b = stream.ReadByte();
                        Assert.AreEqual(bytes[i], b);
                    }

                    // Test writing after the end of the stream
                    // Assert that nothing changes.
                    stream.Position = stream.Length;
                    stream.Write(new byte[10], 0, 10);
                    stream.Position = 0;
                    for (int i = 0; i < stream.Length; i++)
                    {
                        int b = stream.ReadByte();
                        Assert.AreEqual(bytes[i], b);
                    }
                }
            }
        }
    }
}

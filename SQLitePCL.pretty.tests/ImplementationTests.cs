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

using Xunit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SQLitePCL.pretty.tests
{
    public class DatabaseBackupTests : TestBase
    {
        [Fact]
        public void TestDispose()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (x int);");
                foreach (int i in Enumerable.Range(0, 1000))
                {
                    db.Execute("INSERT INTO foo (x) VALUES (?);", i);
                }

                IDatabaseBackup notDisposedBackup;
                
                using (var db2 = SQLite3.OpenInMemory())
                {
                    var backup = db.BackupInit("main", db2, "main");
                    backup.Dispose();

                    Assert.Throws<ObjectDisposedException>(() => { var x = backup.PageCount; });
                    Assert.Throws<ObjectDisposedException>(() => { var x = backup.RemainingPages; });
                    Assert.Throws<ObjectDisposedException>(() => { backup.Step(1); });

                    notDisposedBackup = db.BackupInit("main", db2, "main");
                }

                // Ensure diposing the database connection automatically disposes the backup as well.
                Assert.Throws<ObjectDisposedException>(() => { var x = notDisposedBackup.PageCount; });

                // Test double disposing doesn't result in exceptions.
                notDisposedBackup.Dispose();
            }
        }

        [Fact]
        public void TestBackupWithPageStepping()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (x int);");
                foreach (int i in Enumerable.Range(0, 1000))
                {
                    db.Execute("INSERT INTO foo (x) VALUES (?);", i);
                }

                using (var db2 = SQLite3.OpenInMemory())
                {
                    using (var backup = db.BackupInit("main", db2, "main"))
                    {
                        Assert.Equal(0, backup.RemainingPages);
                        Assert.Equal(0, backup.PageCount);

                        backup.Step(1);
                        var remainingPages = backup.RemainingPages;

                        while (backup.Step(1))
                        {
                            Assert.True(backup.RemainingPages < remainingPages);
                            remainingPages = backup.RemainingPages;
                        }

                        Assert.False(backup.Step(2));
                        Assert.False(backup.Step(-1));
                        Assert.Equal(backup.RemainingPages, 0);
                        Assert.True(backup.PageCount > 0);
                    }
                }
            }
        }

        [Fact]
        public void TestBackup()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (x int);");
                foreach (int i in Enumerable.Range(0, 1000))
                {
                    db.Execute("INSERT INTO foo (x) VALUES (?);", i);
                }

                using (var db2 = SQLite3.OpenInMemory())
                {
                    using (var backup = db.BackupInit("main", db2, "main"))
                    {
                        Assert.Equal(0, backup.RemainingPages);
                        Assert.Equal(0, backup.PageCount);

                        Assert.False(backup.Step(-1));

                        Assert.Equal(backup.RemainingPages, 0);
                        Assert.True(backup.PageCount > 0);
                    }
                }

                using (var db3 = SQLite3.OpenInMemory())
                {
                    db.Backup("main", db3, "main");
                    var backupResults = Enumerable.Zip(
                        db.Query("SELECT x FROM foo"),
                        db3.Query("SELECT x FROM foo"),
                        Tuple.Create);

                    foreach (var pair in backupResults)
                    {
                        Assert.Equal(pair.Item1[0].ToInt(), pair.Item2[0].ToInt());
                    }
                }
            }
        }
    }

    public class StatementTests : TestBase
    {
        [Fact]
        public void TestCurrent()
        {
            using (var db = SQLite3.OpenInMemory())
            using (var stmt = db.PrepareStatement("SELECT 1"))
            {
                stmt.MoveNext();
                Assert.Equal(stmt.Current[0].ToInt(), 1);

                var ienumCurrent = ((IEnumerator)stmt).Current;
                var ienumResultSet = (IReadOnlyList<IResultSetValue>) ienumCurrent;
                Assert.Equal(ienumResultSet[0].ToInt(), 1);
            }
        }

        [Fact]
        public void TestDispose()
        {
            using (var db = SQLite3.OpenInMemory())
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
                Assert.Throws<ObjectDisposedException>(() => { stmt.Status(StatementStatusCode.Sort, false); });
            }
        }

        [Fact]
        public void TestBusy()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.ExecuteAll(
                    @"CREATE TABLE foo (x int);
                      INSERT INTO foo (x) VALUES (1);
                      INSERT INTO foo (x) VALUES (2);
                      INSERT INTO foo (x) VALUES (3);");

                using (var stmt = db.PrepareStatement("SELECT x FROM foo;"))
                {
                    Assert.False(stmt.IsBusy);
                    stmt.MoveNext();
                    Assert.True(stmt.IsBusy);
                    stmt.MoveNext();
                    Assert.True(stmt.IsBusy);
                    stmt.MoveNext();
                    Assert.True(stmt.IsBusy);
                    stmt.MoveNext();
                    Assert.False(stmt.IsBusy);
                }
            }
        }

        [Fact]
        public void TestBindParameterCount()
        {
            Tuple<string, int>[] tests =
            {
                Tuple.Create("CREATE TABLE foo (x int)", 0),
                Tuple.Create("CREATE TABLE foo2 (x int, y int)", 0),
                Tuple.Create("select * from foo", 0),
                Tuple.Create("INSERT INTO foo (x) VALUES (?)", 1),
                Tuple.Create("INSERT INTO foo2 (x, y) VALUES (?, ?)", 2)
            };

            using (var db = SQLite3.OpenInMemory())
            {
                foreach (var test in tests)
                {
                    using (var stmt = db.PrepareStatement(test.Item1))
                    {
                        Assert.Equal(test.Item2, stmt.BindParameters.Count);
                        stmt.MoveNext();
                    }
                }
            }
        }

        [Fact]
        public void TestReadOnly()
        {
            Tuple<string, bool>[] tests =
            {
                Tuple.Create("CREATE TABLE foo (x int)", false),
                Tuple.Create("CREATE TABLE foo2 (x int, y int)", false),
                Tuple.Create("select * from foo", true),
                Tuple.Create("INSERT INTO foo (x) VALUES (?)", false),
                Tuple.Create("INSERT INTO foo2 (x, y) VALUES (?, ?)", false)
            };

            using (var db = SQLite3.OpenInMemory())
            {
                foreach (var test in tests)
                {
                    using (var stmt = db.PrepareStatement(test.Item1))
                    {
                        Assert.Equal(test.Item2, stmt.IsReadOnly);
                        stmt.MoveNext();
                    }
                }
            }
        }

        [Fact]
        public void TestGetSQL()
        {
            string[] sql =
            {
                "CREATE TABLE foo (x int)",
                "INSERT INTO foo (x) VALUES (1)",
                "INSERT INTO foo (x) VALUES (2)",
                "INSERT INTO foo (x) VALUES (3)",
                "SELECT x FROM foo",
            };

            using (var db = SQLite3.OpenInMemory())
            {
                foreach (var sqlStmt in sql)
                {
                    using (var stmt = db.PrepareStatement(sqlStmt))
                    {
                        stmt.MoveNext();

                        Assert.Equal(sqlStmt, stmt.SQL);
                    }
                }
            }
        }

        [Fact]
        public void TestGetBindParameters()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (x int, v int, t text, d real, b blob, q blob);");

                using (var stmt = db.PrepareStatement("INSERT INTO foo (x,v,t,d,b,q) VALUES (:x,:v,:t,:d,:b,:q)"))
                {
                    Assert.Equal(stmt.BindParameters[0].Name, ":x");
                    Assert.Equal(stmt.BindParameters[1].Name, ":v");
                    Assert.Equal(stmt.BindParameters[2].Name, ":t");
                    Assert.Equal(stmt.BindParameters[3].Name, ":d");
                    Assert.Equal(stmt.BindParameters[4].Name, ":b");
                    Assert.Equal(stmt.BindParameters[5].Name, ":q");

                    Assert.Equal(stmt.BindParameters[":x"].Name, ":x");
                    Assert.Equal(stmt.BindParameters[":v"].Name, ":v");
                    Assert.Equal(stmt.BindParameters[":t"].Name, ":t");
                    Assert.Equal(stmt.BindParameters[":d"].Name, ":d");
                    Assert.Equal(stmt.BindParameters[":b"].Name, ":b");
                    Assert.Equal(stmt.BindParameters[":q"].Name, ":q");

                    Assert.True(stmt.BindParameters.ContainsKey(":x"));
                    Assert.False(stmt.BindParameters.ContainsKey(":nope"));
                    Assert.Equal(stmt.BindParameters.Keys.Count(), 6);
                    Assert.Equal(stmt.BindParameters.Values.Count(), 6);

                    Assert.Throws<KeyNotFoundException>(() => { var x = stmt.BindParameters[":nope"]; });
                    Assert.Throws<ArgumentOutOfRangeException>(() => { var x = stmt.BindParameters[-1]; });
                    Assert.Throws<ArgumentOutOfRangeException>(() => { var x = stmt.BindParameters[100]; });

                    Assert.NotNull(((IEnumerable) stmt.BindParameters).GetEnumerator());
                }
            }
        }

        [Fact]
        public void TestExecute()
        {
            using (var db = SQLite3.OpenInMemory())
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
                    Assert.Equal(result.Item1, result.Item2);
                }
            }
        }

        [Fact]
        public void TestQuery()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (v int);");
                using (var stmt = db.PrepareStatement("INSERT INTO foo (v) VALUES (?)"))
                {
                    foreach (var i in Enumerable.Range(0, 100))
                    {
                        stmt.Execute(i);
                    }
                }

                using (var stmt = db.PrepareStatement("SELECT * from FOO WHERE v < ?"))
                {
                    var result = stmt.Query(50).Count();

                    // Ensure that enumerating the Query Enumerable doesn't dispose the stmt
                    { var x = stmt.IsBusy; }
                    Assert.Equal(result, 50);
                }

                using (var stmt = db.PrepareStatement("SELECT * from FOO WHERE v < 50"))
                {
                    var result = stmt.Query().Count();

                    // Ensure that enumerating the Query Enumerable doesn't dispose the stmt
                    { var x = stmt.IsBusy; }
                    Assert.Equal(result, 50);
                }
            }
        }

        [Fact]
        public void TestClearBindings()
        {
            using (var db = SQLite3.OpenInMemory())
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

                Assert.Equal(last.Item1, 0);
                Assert.Equal(last.Item2, 0);
            }
        }

        [Fact]
        public void TestGetColumns()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                var count = 0;
                var stmt = db.PrepareStatement("SELECT 1 as a, 2 as a, 3 as a");
                foreach (var column in stmt.Columns)
                {
                    count++;
                    Assert.Equal(column.Name, "a");
                }

                Assert.Throws<ArgumentOutOfRangeException>(() => { var x = stmt.Columns[-1]; });
                Assert.Throws<ArgumentOutOfRangeException>(() => { var x = stmt.Columns[3]; });

                Assert.Equal(count, stmt.Columns.Count);
            }
        }

        [Fact]
        public void TestStatus()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (x int);");

                using (var stmt = db.PrepareStatement("SELECT x FROM foo"))
                {
                    stmt.MoveNext();

                    int vmStep = stmt.Status(StatementStatusCode.VirtualMachineStep, false);
                    Assert.True(vmStep > 0);

                    int vmStep2 = stmt.Status(StatementStatusCode.VirtualMachineStep, true);
                    Assert.Equal(vmStep, vmStep2);

                    int vmStep3 = stmt.Status(StatementStatusCode.VirtualMachineStep, false);
                    Assert.Equal(0, vmStep3);
                }
            }
        }
    }

    public class BindParameters
    {
        [Fact]
        public void TestBindOnDisposedStatement()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (v int);");

                IReadOnlyOrderedDictionary<string, IBindParameter> bindParams;

                using (var stmt = db.PrepareStatement("INSERT INTO foo (v) VALUES (?)"))
                {
                    bindParams = stmt.BindParameters;
                }

                Assert.Throws<ObjectDisposedException>(() => { var x = bindParams[0]; });
            }
        }

        [Fact]
        public void TestBindObject()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (v int);");
                using (var stmt = db.PrepareStatement("INSERT INTO foo (v) VALUES (?)"))
                {
                    var stream = new MemoryStream();
                    stream.Dispose();
                    Assert.Throws<ArgumentException>(() => stmt.BindParameters[0].Bind(stream));
                    Assert.Throws<ArgumentException>(() => stmt.BindParameters[0].Bind(new object()));
                }

                using (var stmt = db.PrepareStatement("SELECT ?"))
                {
                    stmt.Reset();
                    stmt.ClearBindings();
                    stmt.BindParameters[0].Bind((object) DateTime.MaxValue);
                    stmt.MoveNext();
                    Assert.Equal(stmt.Current[0].ToDateTime(), DateTime.MaxValue);

                    stmt.Reset();
                    stmt.ClearBindings();
                    stmt.BindParameters[0].Bind((object) DateTimeOffset.MaxValue);
                    stmt.MoveNext();
                    Assert.Equal(stmt.Current[0].ToDateTimeOffset(), DateTimeOffset.MaxValue);

                    stmt.Reset();
                    stmt.ClearBindings();
                    stmt.BindParameters[0].Bind((object) TimeSpan.Zero);
                    stmt.MoveNext();
                    Assert.Equal(stmt.Current[0].ToTimeSpan(), TimeSpan.Zero);
                }
            }
        }

        [Fact]
        public void TestBindExtensions()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                using (var stmt = db.PrepareStatement("SELECT ?"))
                {
                    stmt.Reset();
                    stmt.ClearBindings();
                    stmt.BindParameters[0].Bind(true);
                    stmt.MoveNext();
                    Assert.Equal(stmt.Current[0].ToBool(), true);

                    stmt.Reset();
                    stmt.ClearBindings();
                    stmt.BindParameters[0].Bind(TimeSpan.Zero);
                    stmt.MoveNext();
                    Assert.Equal(stmt.Current[0].ToTimeSpan(), TimeSpan.Zero);

                    stmt.Reset();
                    stmt.ClearBindings();
                    stmt.BindParameters[0].Bind(1.1m);
                    stmt.MoveNext();
                    Assert.Equal(stmt.Current[0].ToDecimal(), new Decimal(1.1));

                    stmt.Reset();
                    stmt.ClearBindings();
                    stmt.BindParameters[0].Bind(DateTime.MaxValue);
                    stmt.MoveNext();
                    Assert.Equal(stmt.Current[0].ToDateTime(), DateTime.MaxValue);

                    stmt.Reset();
                    stmt.ClearBindings();
                    stmt.BindParameters[0].Bind(DateTimeOffset.MaxValue);
                    stmt.MoveNext();
                    Assert.Equal(stmt.Current[0].ToDateTimeOffset(), DateTimeOffset.MaxValue);
                }
            }
        }

        [Fact]
        public void TestBindSQLiteValue()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (v int);");
                using (var stmt = db.PrepareStatement("SELECT ?"))
                {
                    var param = stmt.BindParameters[0];
                    param.Bind(SQLiteValue.Null);
                    stmt.MoveNext();
                    var result = stmt.Current.First();
                    Assert.Equal(result.SQLiteType, SQLiteType.Null);

                    stmt.Reset();
                    param.Bind(new byte[0].ToSQLiteValue());
                    stmt.MoveNext();
                    result = stmt.Current.First();
                    Assert.Equal(result.SQLiteType, SQLiteType.Blob);
                    Assert.Equal(result.ToBlob(), new Byte[0]);

                    stmt.Reset();
                    param.Bind("test".ToSQLiteValue());
                    stmt.MoveNext();
                    result = stmt.Current.First();
                    Assert.Equal(result.SQLiteType, SQLiteType.Text);
                    Assert.Equal(result.ToString(), "test");

                    stmt.Reset();
                    param.Bind((1).ToSQLiteValue());
                    stmt.MoveNext();
                    result = stmt.Current.First();
                    Assert.Equal(result.SQLiteType, SQLiteType.Integer);
                    Assert.Equal(result.ToInt64(), 1);

                    stmt.Reset();
                    param.Bind((0.0).ToSQLiteValue());
                    stmt.MoveNext();
                    result = stmt.Current.First();
                    Assert.Equal(result.SQLiteType, SQLiteType.Float);
                    Assert.Equal(result.ToInt(), 0);
                }
            }
        }
    }

    public class ResultSetTests
    {
        [Fact]
        public void TestCount()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.ExecuteAll(
                    @"CREATE TABLE foo (x int, v int);
                      INSERT INTO foo (x, v) VALUES (1, 2);
                      INSERT INTO foo (x, v) VALUES (2, 3);");

                foreach (var row in db.Query("select * from foo"))
                {
                    Assert.Equal(row.Count, 2);
                }

                foreach (var row in db.Query("select x from foo"))
                {
                    Assert.Equal(row.Count, 1);
                }
            }
        }

        [Fact]
        public void TestBracketOp()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.ExecuteAll(
                    @"CREATE TABLE foo (x int, v int);
                      INSERT INTO foo (x, v) VALUES (1, 2);
                      INSERT INTO foo (x, v) VALUES (2, 3);");

                foreach (var row in db.Query("select * from foo"))
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => { var x = row[-1]; });
                    Assert.Throws<ArgumentOutOfRangeException>(() => { var x = row[row.Count]; });

                    Assert.Equal(row[0].SQLiteType, SQLiteType.Integer);
                    Assert.Equal(row[1].SQLiteType, SQLiteType.Integer);
                }
            }
        }

        [Fact]
        public void TestColumns()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                foreach (var row in db.Query("SELECT 1 as a, 2 as b"))
                {
                    var columns = row.Columns();
                    Assert.Equal(columns[0].Name, "a");
                    Assert.Equal(columns[1].Name, "b");
                    Assert.Equal(columns.Count, 2);

                    var count = row.Columns().Count();

                    Assert.Equal(count, 2);
                }
            }
        }
    }

    public class BlobStreamTests
    {
        [Fact]
        public void TestRead()
        {
            using (var db = SQLite3.OpenInMemory())
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
                        Assert.Equal(bytes[i], b);
                    }

                    // Since this is a read only stream, this is a good chance to test that writing fails
                    Assert.Throws<NotSupportedException>(() => stream.WriteByte(0));
                }
            }
        }

        [Fact]
        public void TestDispose()
        {
            Stream notDisposedStream;

            using (var db = SQLite3.OpenInMemory())
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

                notDisposedStream =
                    db.Query("SELECT rowid, x FROM foo;")
                        .Select(row => db.OpenBlob(row[1].ColumnInfo, row[0].ToInt64(), false))
                        .First();
            }

            // Test that disposing the connection disposes the stream
            Assert.Throws<ObjectDisposedException>(() => { var x = notDisposedStream.Length; });
        }

        [Fact]
        public void TestSeek()
        {
            using (var db = SQLite3.OpenInMemory())
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
                    { blob.Position = 100; }

                    // Test input validation
                    blob.Position = 5;
                    Assert.Throws<IOException>(() => blob.Seek(-10, SeekOrigin.Begin));
                    Assert.Equal(blob.Position, 5);
                    Assert.Throws<IOException>(() => blob.Seek(-10, SeekOrigin.Current));
                    Assert.Equal(blob.Position, 5);
                    Assert.Throws<IOException>(() => blob.Seek(-100, SeekOrigin.End));
                    Assert.Equal(blob.Position, 5);
                    Assert.Throws<ArgumentException>(() => blob.Seek(-100, (SeekOrigin)10));
                    Assert.Equal(blob.Position, 5);

                    blob.Seek(0, SeekOrigin.Begin);
                    Assert.Equal(blob.Position, 0);

                    blob.Seek(0, SeekOrigin.End);
                    Assert.Equal(blob.Position, blob.Length);

                    blob.Position = 5;
                    blob.Seek(2, SeekOrigin.Current);
                    Assert.Equal(blob.Position, 7);
                }
            }
        }

        [Fact]
        public void TestWrite()
        {
            using (var db = SQLite3.OpenInMemory())
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
                    Assert.True(stream.CanRead);
                    Assert.True(stream.CanWrite);
                    source.CopyTo(stream);

                    stream.Position = 0;

                    for (int i = 0; i < stream.Length; i++)
                    {
                        int b = stream.ReadByte();
                        Assert.Equal(bytes[i], b);
                    }

                    // Test writing after the end of the stream
                    // Assert that nothing changes.
                    stream.Position = stream.Length;
                    stream.Write(new byte[10], 0, 10);
                    stream.Position = 0;
                    for (int i = 0; i < stream.Length; i++)
                    {
                        int b = stream.ReadByte();
                        Assert.Equal(bytes[i], b);
                    }
                }
            }
        }
    }
}

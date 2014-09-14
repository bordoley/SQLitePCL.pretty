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

using System;
using System.Collections.Generic;
using System.Linq;
using SQLitePCL.pretty;

#if USE_NUNIT
using NUnit.Framework;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestContext = System.Object;
using TestProperty = NUnit.Framework.PropertyAttribute;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
#elif WINDOWS_PHONE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#elif NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace SQLitePCL.pretty.tests
{
    [TestClass]
    public class test_cases
    {
        [TestMethod]
        public void test_bind_parameter_index()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int, v int, t text, d real, b blob, q blob);");

                using (var stmt = db.PrepareStatement("INSERT INTO foo (x,v,t,d,b,q) VALUES (:x,:v,:t,:d,:b,:q)"))
                {
                    Assert.IsTrue(stmt.ReadOnly == false);

                    Assert.AreEqual(stmt.BindParameterCount, 6);

                    Assert.AreEqual(stmt.GetBindParameterIndex(":m"), -1);

                    Assert.AreEqual(stmt.GetBindParameterIndex(":x"), 0);
                    Assert.AreEqual(stmt.GetBindParameterIndex(":v"), 1);
                    Assert.AreEqual(stmt.GetBindParameterIndex(":t"), 2);
                    Assert.AreEqual(stmt.GetBindParameterIndex(":d"), 3);
                    Assert.AreEqual(stmt.GetBindParameterIndex(":b"), 4);
                    Assert.AreEqual(stmt.GetBindParameterIndex(":q"), 5);

                    Assert.AreEqual(stmt.GetBindParameterName(0), ":x");
                    Assert.AreEqual(stmt.GetBindParameterName(1), ":v");
                    Assert.AreEqual(stmt.GetBindParameterName(2), ":t");
                    Assert.AreEqual(stmt.GetBindParameterName(3), ":d");
                    Assert.AreEqual(stmt.GetBindParameterName(4), ":b");
                    Assert.AreEqual(stmt.GetBindParameterName(5), ":q");
                }
            }
        }

        [TestMethod]
        public void test_blob_read()
        {
            var ver = SQLite3.Version;
            Console.WriteLine(SQLite3.Version);

            using (var db = SQLite3.Open(":memory:"))
            {
                byte[] bytes = new byte[] {9,9,9,9,9};

                db.Execute("CREATE TABLE foo (b blob);");
                using ( var stmt = db.PrepareStatement("INSERT INTO foo (b) VALUES (:x)") )
                {
                    stmt.Bind(0, bytes);
                    stmt.MoveNext();
                }

                using (var stmt = db.PrepareStatement("SELECT b FROM foo;"))
                {
                    stmt.MoveNext();
                    stmt.MoveNext();
                    stmt.MoveNext();

                    var row = stmt.Current;
                    var count = row.Count;
                    var byteArr = row[0].ToBlob();
                    Assert.AreEqual(byteArr.Length, bytes.Length);
                    Assert.That(Enumerable.SequenceEqual(bytes, byteArr));

                    // FIXME: This is failing but I suspect its a mac issue
                    //using (var blob = row.GetResultAsReadOnlyStream(0))
                    //{
                    //    Assert.AreEqual(bytes.Length, blob.Length);
                    //    for (int i = 0; i < blob.Length; i++) 
                    //   {
                    //        int b = blob.ReadByte();
                    //        Assert.Equals(9, b);
                    //    }
                    //}
                }
            }
        }

        [TestMethod]
        public void test_get_autocommit()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                Assert.IsTrue(db.IsAutoCommit);
                db.Execute("BEGIN TRANSACTION;");
                Assert.IsFalse(db.IsAutoCommit);
            }
        }

        [TestMethod]
        public void test_libversion()
        {
            string sourceid = SQLite3.SourceId;
            Assert.IsTrue(sourceid != null);
            Assert.IsTrue(sourceid.Length > 0);

            string libversion = SQLite3.Version.ToString();
            Assert.IsTrue(libversion != null);
            Assert.IsTrue(libversion.Length > 0);
            Assert.AreEqual(libversion[0], '3');

            int libversion_number = SQLite3.Version.ToInt();
            Assert.AreEqual(libversion_number / 1000000, 3);
        }

        [TestMethod]
        public void test_sqlite3_memory()
        {
            long memory_used = SQLite3.MemoryUsed;
            long memory_highwater = SQLite3.MemoryHighWater;
            #if not
            // these asserts fail on the iOS builtin sqlite.  not sure
            // why.  not sure the asserts are worth doing anyway.
            Assert.IsTrue(memory_used > 0);
            Assert.IsTrue(memory_highwater >= memory_used);
            #endif
        }
            
        [TestMethod]
        public void test_backup()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.ExecuteAll(
                    @"CREATE TABLE foo (x text);
                      INSERT INTO foo (x) VALUES ('b');
                      INSERT INTO foo (x) VALUES ('c');
                      INSERT INTO foo (x) VALUES ('d');
                      INSERT INTO foo (x) VALUES ('e');");
                db.Execute("INSERT INTO foo (x) VALUES ('f')");

                using (var db2 = SQLite3.Open(":memory:"))
                {
                    using (var bak = db.BackupInit("main", db2, "main"))
                    {
                        bak.Step(-1);
                        Assert.AreEqual(bak.RemainingPages, 0);
                        Assert.IsTrue(bak.PageCount > 0);
                    }
                }
            }
        }

        [TestMethod]
        public void test_compileoption()
        {
            foreach (var opt in SQLite3.CompilerOptions)
            {
                bool used = SQLite3.CompileOptionUsed(opt);
                Assert.IsTrue(used);
            }
        }

        [TestMethod]
        public void test_create_table_temp_db()
        {
            using (var db = SQLite3.Open(""))
            {
                db.Execute("CREATE TABLE foo (x int);");
            }
        }

        [TestMethod]
        public void test_create_table_file()
        {
            string name;
            using (var db = SQLite3.Open(":memory:"))
            {
                using (var stmt = db.PrepareStatement("SELECT lower(hex(randomblob(16)));"))
                {
                    stmt.MoveNext();
                    name = "tmp" + stmt.Current[0].ToString();
                }
            }
            string filename;
            using (var db = SQLite3.Open(name))
            {
                db.Execute("CREATE TABLE foo (x int);");
                filename = db.GetFileName("main");
            }

            // TODO verify the filename is what we expect

            // FIXME: This isn't a straight wrapper around a SQLite API and
            // the vfs api in general seems like it will get more work,
            // so not exposing it using a pretty api. users can always use the ugly one.
            // see: https://github.com/ericsink/SQLitePCL.raw/commit/9eb0b2ae514374f8cf44a90c20972aa6622b4112
            int rc = SQLitePCL.raw.sqlite3__vfs__delete(null, filename, 1);
            Assert.AreEqual(raw.SQLITE_OK, rc);
        }

        [TestMethod]
        public void test_error()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int UNIQUE);");
                db.Execute("INSERT INTO foo (x) VALUES (3);");
                bool fail = false;
                try
                {
                    db.Execute("INSERT INTO foo (x) VALUES (3);");
                }
                catch (SQLiteException e)
                {
                    fail = true;

                    Assert.AreEqual((int) e.ErrorCode, raw.SQLITE_CONSTRAINT);
                    Assert.AreEqual((int) e.ExtendedErrorCode, raw.SQLITE_CONSTRAINT_UNIQUE, "Extended error codes for SQLITE_CONSTRAINT were added in 3.7.16");
                }
                Assert.IsTrue(fail);
            }
        }

        [TestMethod]
        public void test_create_table_memory_db()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int);");
            }
        }

        [TestMethod]
        public void test_open_v2()
        {
            using (var db = SQLite3.Open(":memory:", ConnectionFlags.ReadWrite | ConnectionFlags.Create, null))
            {
                db.Execute("CREATE TABLE foo (x int);");
            }
        }

        [TestMethod]
        public void test_create_table_explicit_close()
        {
            var db = SQLite3.Open(":memory:");
            db.Execute("CREATE TABLE foo (x int);");
            db.Dispose(); // Pretty interface only support dispose no close method
        }

        [TestMethod]
        public void test_count()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int);");
                db.Execute("INSERT INTO foo (x) VALUES (1);");
                db.Execute("INSERT INTO foo (x) VALUES (2);");
                db.Execute("INSERT INTO foo (x) VALUES (3);");

                using (var stmt = db.PrepareStatement("SELECT COUNT(*) FROM foo"))
                {
                    stmt.MoveNext();
                    int c = stmt.Current[0].ToInt();
                    Assert.AreEqual(c, 3);
                }
            }
        }
            
        [TestMethod]
        public void test_stmt_complete()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int);");

                Assert.IsFalse(SQLite3.IsCompleteStatement("SELECT x FROM"));
                Assert.IsFalse(SQLite3.IsCompleteStatement("SELECT"));
                Assert.IsFalse(SQLite3.IsCompleteStatement("INSERT INTO"));
                Assert.IsFalse(SQLite3.IsCompleteStatement("SELECT x FROM foo"));

                Assert.IsTrue(SQLite3.IsCompleteStatement("SELECT x FROM foo;"));
                Assert.IsTrue(SQLite3.IsCompleteStatement("SELECT COUNT(*) FROM foo;"));
                Assert.IsTrue(SQLite3.IsCompleteStatement("SELECT 5;"));
            }
        } 
            
        [TestMethod]
        public void test_next_stmt()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                Assert.AreEqual(db.Statements.Count(), 0); 

                using (var stmt = db.PrepareStatement("SELECT 5;"))
                {
                    Assert.AreEqual(db.Statements.Count(), 1);

                    var firstStmt = db.Statements.First();

                    // IStatement can't sanely implement equality at the 
                    // interface level. Doing so would tightly bind the 
                    // interface to the underlying SQLite implementation
                    // which we don't want to do.
                    Assert.AreEqual(stmt.SQL, firstStmt.SQL); 
                }
            }
        }

        [TestMethod]
        public void test_stmt_busy()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int);");
                db.Execute("INSERT INTO foo (x) VALUES (1);");
                db.Execute("INSERT INTO foo (x) VALUES (2);");
                db.Execute("INSERT INTO foo (x) VALUES (3);");
                const string sql = "SELECT x FROM foo";
                using (var stmt = db.PrepareStatement(sql))
                {
                    Assert.AreEqual(sql, stmt.SQL);

                    Assert.IsFalse(stmt.Busy);
                    stmt.MoveNext();
                    Assert.IsTrue(stmt.Busy);
                    stmt.MoveNext();
                    Assert.IsTrue(stmt.Busy);
                    stmt.MoveNext();
                    Assert.IsTrue(stmt.Busy);
                    stmt.MoveNext();
                    Assert.IsFalse(stmt.Busy);
                }
            }
        }
            
        [TestMethod]
        public void test_changes()
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

        [TestMethod]
        public void test_explicit_prepare()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int);");
                const int num = 7;
                using (var stmt = db.PrepareStatement("INSERT INTO foo (x) VALUES (?)"))
                {
                    for (int i=0; i<num; i++)
                    {
                        stmt.Reset();
                        stmt.ClearBindings();
                        stmt.Bind(0, i);
                        stmt.MoveNext();
                    }
                }

                using (var stmt = db.PrepareStatement("SELECT COUNT(*) FROM foo"))
                {
                    stmt.MoveNext();
                    int c = stmt.Current[0].ToInt();
                    Assert.AreEqual(c, num);
                }
            }
        }

        [TestMethod]
        public void test_exec_with_tail()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                try 
                {
                    db.Execute("CREATE TABLE foo (x int);INSERT INTO foo (x) VALUES (1);");
                    Assert.Fail();
                } 
                catch (ArgumentException)
                {
                }
            }
        }

        [TestMethod]
        public void test_column_origin()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int, v int, t text, d real, b blob, q blob);");

                byte[] blob;
                using (var stmt = db.PrepareStatement("SELECT randomblob(5);"))
                {
                    stmt.MoveNext();
                    blob = stmt.Current[0].ToBlob();
                }
                     
                db.Execute("INSERT INTO foo (x,v,t,d,b,q) VALUES (?,?,?,?,?,?)", 32, 44, "hello", 3.14, blob, null);

                #if not
                // maybe we should just let this fail so we can
                // see the differences between running against the built-in
                // sqlite vs a recent version?
                if (1 == raw.sqlite3_compileoption_used("ENABLE_COLUMN_METADATA"))
                #endif
                {
                    using (var stmt = db.PrepareStatement("SELECT x AS mario FROM foo;"))
                    {
                        stmt.MoveNext();
                        var row = stmt.Current;

                        Assert.IsTrue(stmt.ReadOnly);

                        // FIXME: These test fail on mac but whatever
                        // Assert.AreEqual(row[0].ColumnDatabaseName, "main");
                        // Assert.AreEqual(row[0].ColumnTableName, "foo");
                        // Assert.AreEqual(row[0].ColumnOriginName, "x");
                        Assert.AreEqual(row[0].ColumnName, "mario");
                        Assert.AreEqual(row[0].SQLiteType, SQLiteType.Integer);
                    }
                }
            }
        }

        [TestMethod]
        public void test_row()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x int, v int, t text, d real, b blob, q blob);");

                byte[] blob = db.Query("SELECT randomblob(5);").Select(row => row[0].ToBlob()).First();

                db.Execute("INSERT INTO foo (x,v,t,d,b,q) VALUES (?,?,?,?,?,?)", 32, 44, "hello", 3.14, blob, null);
                foreach (var r in db.Query("SELECT x,v,t,d,b,q FROM foo;"))
                {

                    Assert.AreEqual(r[1].ToInt(), 44);
                    Assert.AreEqual(r[2].ToString(), "hello");
                    Assert.AreEqual(r[3].ToDouble(), 3.14);
                    Assert.AreEqual(r[4].ToBlob().Length, blob.Length);
                    for (int i=0; i<blob.Length; i++)
                    {
                        Assert.AreEqual(r[4].ToBlob()[i], blob[i]);
                    }
                    Assert.AreEqual(r[5].ToBlob(), null);
                }

                using (var stmt = db.PrepareStatement("SELECT x,v,t,d,b,q FROM foo;"))
                {
                    stmt.MoveNext();

                    // Statement and Result don't expose this api
                    // Assert.AreEqual(stmt.db_handle(), db);

                    var row = stmt.Current;

                    Assert.AreEqual(row[0].ToInt(), 32);
                    Assert.AreEqual(row[1].ToInt64(), 44);
                    Assert.AreEqual(row[2].ToString(), "hello");
                    Assert.AreEqual(row[3].ToDouble(), 3.14);

                    byte[] b2 = row[4].ToBlob();
                    Assert.AreEqual(b2.Length, blob.Length);
                    for (int i=0; i<blob.Length; i++)
                    {
                        Assert.AreEqual(b2[i], blob[i]);
                    }
                        
                    Assert.AreEqual(row[5].SQLiteType, SQLiteType.Null);

                    Assert.AreEqual(row[0].ColumnName, "x");
                    Assert.AreEqual(row[1].ColumnName, "v");
                    Assert.AreEqual(row[2].ColumnName, "t");
                    Assert.AreEqual(row[3].ColumnName, "d");
                    Assert.AreEqual(row[4].ColumnName, "b");
                    Assert.AreEqual(row[5].ColumnName, "q");
                }
            }
        }

        //This API not supported in pretty. Use the IENumerator over the rows instead
        /*[TestMethod]
        public void test_exec_with_callback()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.Execute("CREATE TABLE foo (x text);");
                db.Execute("INSERT INTO foo (x) VALUES ('b')");
                db.Execute("INSERT INTO foo (x) VALUES ('c')");
                db.Execute("INSERT INTO foo (x) VALUES ('d')");
                db.Execute("INSERT INTO foo (x) VALUES ('e')");
                db.Execute("INSERT INTO foo (x) VALUES ('f')");
                string errmsg;
                work w = new work();
                db.Execute("SELECT x FROM foo", my_cb, w, out errmsg);
                Assert.AreEqual(w.count, 5);
            }
        }*/

        [TestMethod]
        public void test_collation()
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

                string top = db.Query("SELECT x FROM foo ORDER BY x ASC LIMIT 1;").Select(x => x[0].ToString()).First();
                Assert.AreEqual(top, "e");
            }
        }

        [TestMethod]
        public void test_cube()
        {
            int val = 5;

            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterScalarFunc("cube", (ISQLiteValue x) => (x.ToInt64() * x.ToInt64() * x.ToInt64()).ToSQLiteValue());
                var c = db.Query("SELECT cube(?);", val).Select(rs => rs[0].ToInt64()).First();
                Assert.AreEqual(c, val * val * val);
            }
        }
 
        [TestMethod]
        public void test_makeblob()
        {
            int val = 5;
            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterScalarFunc("makeblob", (ISQLiteValue v) =>
                    {
                        byte[] b = new byte[v.ToInt()];
                        for (int i = 0; i < b.Length; i++)
                        {
                            b[i] = (byte) (i % 256);
                        }
                        return b.ToSQLiteValue();
                    });
                        
                var c = db.Query("SELECT makeblob(?);", val).Select(rs => rs[0].ToBlob()).First();
                Assert.AreEqual(c.Length, val);
            }
        }

        [TestMethod]
        public void test_scalar_mean_double()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterScalarFunc("my_mean", (IReadOnlyList<ISQLiteValue> values) =>
                    (values.Aggregate(0d, (acc, v) => acc + v.ToDouble()) / values.Count).ToSQLiteValue());


                var result = db.Query("SELECT my_mean(1,2,3,4,5,6,7,8);").Select(rs => rs[0].ToDouble()).First();
                Assert.IsTrue(result >= (36 / 8));
                Assert.IsTrue(result <= (36 / 8 + 1));
            }
        }

        [TestMethod]
        public void test_countargs()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterScalarFunc("count_args", (IReadOnlyList<ISQLiteValue> values) => values.Count.ToSQLiteValue());
                Assert.AreEqual(8, db.Query("SELECT count_args(1,2,3,4,5,6,7,8);").Select(v => v[0].ToInt()).First());
                Assert.AreEqual(0, db.Query("SELECT count_args();").Select(v => v[0].ToInt()).First());
                Assert.AreEqual(1, db.Query("SELECT count_args(null);").Select(v => v[0].ToInt()).First());
            }
        }

        [TestMethod]
        public void test_countnullargs()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterScalarFunc("count_nulls", (IReadOnlyList<ISQLiteValue> vals) =>
                    vals.Where(val => val.SQLiteType == SQLiteType.Null).Count().ToSQLiteValue());

                Assert.AreEqual(0, db.Query("SELECT count_nulls(1,2,3,4,5,6,7,8);").Select(v => v[0].ToInt()).First());
                Assert.AreEqual(0, db.Query("SELECT count_nulls();").Select(v => v[0].ToInt()).First());
                Assert.AreEqual(1, db.Query("SELECT count_nulls(null);").Select(v => v[0].ToInt()).First());
                Assert.AreEqual(2, db.Query("SELECT count_nulls(1,null,3,null,5);").Select(v => v[0].ToInt()).First());
            }
        }

        [TestMethod]
        public void test_len_as_blobs()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterScalarFunc("len_as_blobs", (IReadOnlyList<ISQLiteValue> values) => 
                    values.Where(v => v.SQLiteType != SQLiteType.Null).Aggregate(0, (acc, val) => acc + val.Length).ToSQLiteValue());
                Assert.AreEqual(0, db.Query("SELECT len_as_blobs();").Select(v => v[0].ToInt()).First());
                Assert.AreEqual(0, db.Query("SELECT len_as_blobs(null);").Select(v => v[0].ToInt()).First());
                Assert.IsTrue(8 <= db.Query("SELECT len_as_blobs(1,2,3,4,5,6,7,8);").Select(v => v[0].ToInt()).First());
            }
        }

        [TestMethod]
        public void test_concat()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterScalarFunc("my_concat", (IReadOnlyList<ISQLiteValue> values) =>
                    string.Join("", values.Select(v => v.ToString())).ToSQLiteValue());
                Assert.AreEqual("foobar", db.Query("SELECT my_concat('foo', 'bar');").Select(v => v[0].ToString()).First());
                Assert.AreEqual("abc", db.Query("SELECT my_concat('a', 'b', 'c');").Select(v => v[0].ToString()).First());
            }
        }

        [TestMethod]
        public void test_sum_plus_count()
        {
            using (var db = SQLite3.Open(":memory:"))
            {
                db.RegisterAggregateFunc<Tuple<long,long>>("sum_plus_count", Tuple.Create(0L, 0L), 
                    (Tuple<long,long> acc, ISQLiteValue arg) => Tuple.Create(acc.Item1 + arg.ToInt64(), acc.Item2 + 1L),
                    (Tuple<long,long> acc) => (acc.Item1 + acc.Item2).ToSQLiteValue());
                db.Execute("CREATE TABLE foo (x int);");
                for (int i= 0; i < 5; i++)
                {
                    db.Execute("INSERT INTO foo (x) VALUES (?);", i);
                }
                long c = db.Query("SELECT sum_plus_count(x) FROM foo;").Select(row => row[0].ToInt64()).First();
                Assert.AreEqual(c, (0 + 1 + 2 + 3 + 4) + 5);
            }
        }

        [TestMethod]
        public void test_hooks()
        {
            int count_commits = 0;
            int count_rollbacks = 0;
            int count_updates = 0;
            int count_traces = 0;
            int count_profiles = 0;

            using (var db = SQLite3.Open(":memory:"))
            {
                Assert.AreEqual(count_commits, 0);
                Assert.AreEqual(count_rollbacks, 0);
                Assert.AreEqual(count_updates, 0);
                Assert.AreEqual(count_traces, 0);
                Assert.AreEqual(count_profiles, 0);

                db.RegisterCommitHook(() =>
                    {
                        count_commits++;
                        return false;
                    });

                db.Rollback += (o,e) => count_rollbacks++;
                db.Update += (o,e) => count_updates++;
                db.Trace += (o,e) => count_traces++;
                db.Profile += (o,e) => count_profiles++;

                db.Execute("CREATE TABLE foo (x int);");

                Assert.AreEqual(count_commits, 1);
                Assert.AreEqual(count_rollbacks, 0);
                Assert.AreEqual(count_updates, 0);
                Assert.AreEqual(count_traces, 1);
                Assert.AreEqual(count_profiles, 1);

                db.Execute("INSERT INTO foo (x) VALUES (1);");

                Assert.AreEqual(count_commits, 2);
                Assert.AreEqual(count_rollbacks, 0);
                Assert.AreEqual(count_updates, 1);
                Assert.AreEqual(count_traces, 2);
                Assert.AreEqual(count_profiles, 2);

                db.Execute("BEGIN TRANSACTION;");

                Assert.AreEqual(count_commits, 2);
                Assert.AreEqual(count_rollbacks, 0);
                Assert.AreEqual(count_updates, 1);
                Assert.AreEqual(count_traces, 3);
                Assert.AreEqual(count_profiles, 3);

                db.Execute("INSERT INTO foo (x) VALUES (2);");

                Assert.AreEqual(count_commits, 2);
                Assert.AreEqual(count_rollbacks, 0);
                Assert.AreEqual(count_updates, 2);
                Assert.AreEqual(count_traces, 4);
                Assert.AreEqual(count_profiles, 4);

                db.Execute("ROLLBACK TRANSACTION;");

                Assert.AreEqual(count_commits, 2);
                Assert.AreEqual(count_rollbacks, 1);
                Assert.AreEqual(count_updates, 2);
                Assert.AreEqual(count_traces, 5);
                Assert.AreEqual(count_profiles, 5);
            }
        }
    }
}
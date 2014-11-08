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

#if USE_NUNIT

using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

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
        public void test_blob_read()
        {
            var ver = SQLite3.Version;

            using (var db = SQLite3.Open(":memory:"))
            {
                byte[] bytes = new byte[] { 9, 9, 9, 9, 9 };

                db.Execute("CREATE TABLE foo (b blob);");
                using (var stmt = db.PrepareStatement("INSERT INTO foo (b) VALUES (:x)"))
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
                    using (var blob = row[0].ToReadOnlyStream())
                    {
                        Assert.AreEqual(bytes.Length, blob.Length);
                        for (int i = 0; i < blob.Length; i++)
                        {
                            int b = blob.ReadByte();
                            Assert.AreEqual(9, b);
                        }
                    }
                }
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

                    Assert.AreEqual(e.ErrorCode, ErrorCode.Constraint);
                    Assert.AreEqual(e.ExtendedErrorCode, ErrorCode.ConstraintUnique, "Extended error codes for SQLITE_CONSTRAINT were added in 3.7.16");
                }
                Assert.IsTrue(fail);
            }
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
                    for (int i = 0; i < blob.Length; i++)
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
                    for (int i = 0; i < blob.Length; i++)
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
    }
}

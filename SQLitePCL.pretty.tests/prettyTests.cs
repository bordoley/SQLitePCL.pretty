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
        public void test_error()
        {
            using (var db = SQLite3.OpenInMemory())
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
            using (var db = SQLite3.OpenInMemory())
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
            using (var db = SQLite3.OpenInMemory())
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
    }
}

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

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class SQLite3Tests
    {
        [Test]
        public void TestCompileOptions()
        {
            foreach (var opt in SQLite3.CompilerOptions)
            {
                bool used = SQLite3.CompileOptionUsed(opt);
                Assert.IsTrue(used);
            }
        }

        [Test]
        public void TestMemory()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (x int);");

                Assert.IsTrue(SQLite3.MemoryUsed > 0);
                Assert.IsTrue(SQLite3.MemoryHighWater >= SQLite3.MemoryUsed);

                SQLite3.ResetMemoryHighWater();
                Assert.AreEqual(SQLite3.MemoryUsed, SQLite3.MemoryHighWater);
            }
        }

        [Test]
        public void TestOpen()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (x int);");
            }

            using (var db = SQLite3.Open(":memory:", ConnectionFlags.ReadOnly, null))
            {
                Assert.Throws<SQLiteException>(() => db.Execute("CREATE TABLE foo (x int);"));
            }
        }

        [Test]
        public void TestIsCompleteStatement()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                Assert.IsFalse(SQLite3.IsCompleteStatement("SELECT x FROM"));
                Assert.IsFalse(SQLite3.IsCompleteStatement("SELECT"));
                Assert.IsFalse(SQLite3.IsCompleteStatement("INSERT INTO"));
                Assert.IsFalse(SQLite3.IsCompleteStatement("SELECT x FROM foo"));

                Assert.IsTrue(SQLite3.IsCompleteStatement("SELECT x FROM foo;"));
                Assert.IsTrue(SQLite3.IsCompleteStatement("SELECT COUNT(*) FROM foo;"));
                Assert.IsTrue(SQLite3.IsCompleteStatement("SELECT 5;"));
            }
        }

        [Test]
        public void TestSourceId()
        {
            string sourceid = SQLite3.SourceId;
            Assert.IsTrue(sourceid != null);
            Assert.IsTrue(sourceid.Length > 0);
        }

        [Test]
        public void TestVersion()
        {
            var version = SQLite3.Version;
            Assert.AreEqual(version.Major, 3);
        }

        [Test]
        public void TestStatus()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                int current;
                int highwater;
                SQLite3.Status(SQLiteStatusCode.MemoryUsed, out current, out highwater, false);

                Assert.IsTrue(current > 0);
                Assert.IsTrue(highwater > 0);
            }
        }

        [Test]
        public void TestEnableSharedCache()
        { 
            SQLite3.EnableSharedCache = true;
            SQLite3.EnableSharedCache = false;
        }
    }
}

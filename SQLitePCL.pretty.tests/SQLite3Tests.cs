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

namespace SQLitePCL.pretty.tests
{
    public class SQLite3Tests
    {
        [Fact]
        public void TestCompileOptions()
        {
            foreach (var opt in SQLite3.CompilerOptions)
            {
                bool used = SQLite3.CompileOptionUsed(opt);
                Assert.True(used);
            }
        }

        [Fact]
        public void TestMemory()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                db.Execute("CREATE TABLE foo (x int);");

                Assert.True(SQLite3.MemoryUsed > 0);
                Assert.True(SQLite3.MemoryHighWater >= SQLite3.MemoryUsed);

                SQLite3.ResetMemoryHighWater();
                Assert.Equal(SQLite3.MemoryUsed, SQLite3.MemoryHighWater);
            }
        }

        [Fact]
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

        [Fact]
        public void TestIsCompleteStatement()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                Assert.False(SQLite3.IsCompleteStatement("SELECT x FROM"));
                Assert.False(SQLite3.IsCompleteStatement("SELECT"));
                Assert.False(SQLite3.IsCompleteStatement("INSERT INTO"));
                Assert.False(SQLite3.IsCompleteStatement("SELECT x FROM foo"));

                Assert.True(SQLite3.IsCompleteStatement("SELECT x FROM foo;"));
                Assert.True(SQLite3.IsCompleteStatement("SELECT COUNT(*) FROM foo;"));
                Assert.True(SQLite3.IsCompleteStatement("SELECT 5;"));
            }
        }

        [Fact]
        public void TestSourceId()
        {
            string sourceid = SQLite3.SourceId;
            Assert.True(sourceid != null);
            Assert.True(sourceid.Length > 0);
        }

        [Fact]
        public void TestVersion()
        {
            var version = SQLite3.Version;
            Assert.Equal(version.Major, 3);
        }

        [Fact]
        public void TestStatus()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                int current;
                int highwater;
                SQLite3.Status(SQLiteStatusCode.MemoryUsed, out current, out highwater, false);

                Assert.True(current > 0);
                Assert.True(highwater > 0);
            }
        }

        [Fact]
        public void TestEnableSharedCache()
        { 
            SQLite3.EnableSharedCache = true;
            SQLite3.EnableSharedCache = false;
        }
    }
}

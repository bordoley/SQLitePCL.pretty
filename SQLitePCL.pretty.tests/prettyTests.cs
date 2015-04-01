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

using Xunit;

namespace SQLitePCL.pretty.tests
{
    public class test_cases
    {
        [Fact]
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

                    Assert.Equal(e.ErrorCode, ErrorCode.Constraint);

                    // "Extended error codes for SQLITE_CONSTRAINT were added in 3.7.16"
                    Assert.Equal(e.ExtendedErrorCode, ErrorCode.ConstraintUnique);
                }
                Assert.True(fail);
            }
        }

        [Fact]
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
                    Assert.Equal(c, 3);
                }
            }
        }

        [Fact]
        public void test_exec_with_tail()
        {
            using (var db = SQLite3.OpenInMemory())
            {
                try
                {
                    db.Execute("CREATE TABLE foo (x int);INSERT INTO foo (x) VALUES (1);");
                    Assert.True(false);
                }
                catch (ArgumentException)
                {
                }
            }
        }
    }
}

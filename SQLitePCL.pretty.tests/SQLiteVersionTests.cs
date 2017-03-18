/*
   Copyright 2014 David Bordoley

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

namespace SQLitePCL.pretty.tests
{
    public class SQLiteVersionTests : TestBase
    {
        [Fact]
        public void TestEquality()
        {
            Assert.True(SQLiteVersion.Of(3080911).Equals(SQLiteVersion.Of(3080911)));
            Assert.True(SQLiteVersion.Of(3080911).Equals((object)SQLiteVersion.Of(3080911)));
            Assert.True(SQLiteVersion.Of(3080911) == SQLiteVersion.Of(3080911));
            Assert.False(SQLiteVersion.Of(3080911) != SQLiteVersion.Of(3080911));

            SQLiteVersion[] notEqualTests =
            {
                SQLiteVersion.Of(3080911),
                SQLiteVersion.Of(2080911),
                SQLiteVersion.Of(3070911),
                SQLiteVersion.Of(3080910)
            };

            for (int i = 0; i < notEqualTests.Length; i++)
            {
                for (int j = i + 1; j < notEqualTests.Length; j++)
                {
                    Assert.False(notEqualTests[i].Equals(notEqualTests[j]));
                    Assert.False(notEqualTests[i].Equals((object)notEqualTests[j]));
                    Assert.False(notEqualTests[i] == notEqualTests[j]);
                    Assert.True(notEqualTests[i] != notEqualTests[j]);
                }
            }

            Assert.False(SQLiteVersion.Of(3080911).Equals(null));
            Assert.False(SQLiteVersion.Of(3080911).Equals(""));
        }

        [Fact]
        public void TestGetHashcode()
        {
            SQLiteVersion[] equalObjects =
            {
                SQLiteVersion.Of(3080911),
                SQLiteVersion.Of(3080911),
                SQLiteVersion.Of(3080911)
            };

            for (int i = 0; i < equalObjects.Length; i++)
            {
                for (int j = i + 1; j < equalObjects.Length; j++)
                {
                    Assert.Equal(equalObjects[i].GetHashCode(), equalObjects[j].GetHashCode());
                }
            }
        }

        [Fact]
        public void TestComparison()
        {
            Assert.Equal(0, SQLiteVersion.Of(3080911).CompareTo(SQLiteVersion.Of(3080911)));

            Assert.True(SQLiteVersion.Of(3080911) < SQLiteVersion.Of(3080912));
            Assert.True(SQLiteVersion.Of(3080911) < SQLiteVersion.Of(3081911));
            Assert.True(SQLiteVersion.Of(3080911) < SQLiteVersion.Of(4080911));

            Assert.Throws<ArgumentException>(() => SQLiteVersion.Of(3080911).CompareTo(null));
            Assert.Throws<ArgumentException>(() => SQLiteVersion.Of(3080911).CompareTo(""));

            Assert.True(SQLiteVersion.Of(3080911) > SQLiteVersion.Of(3080910));
            Assert.True(SQLiteVersion.Of(3080911) >= SQLiteVersion.Of(3080911));
            Assert.True(SQLiteVersion.Of(3080911) >= SQLiteVersion.Of(3080910));

            Assert.False(SQLiteVersion.Of(3080911) < SQLiteVersion.Of(3080910));
            Assert.True(SQLiteVersion.Of(3080911) <= SQLiteVersion.Of(3080911));
            Assert.False(SQLiteVersion.Of(3080911) < SQLiteVersion.Of(3080910));
        }

        [Fact]
        public void TestToInt()
        {
            Assert.Equal(SQLiteVersion.Of(3080911).ToInt(), 3080911);
        }

        [Fact]
        public void TestToString()
        {
            Tuple<int, string>[] tests =
            {
                Tuple.Create(3008007, "3.8.7"),
                Tuple.Create(44008007, "44.8.7"),
                Tuple.Create(3008080, "3.8.80"),
                Tuple.Create(3088007, "3.88.7")
            };

            foreach (var test in tests)
            {
                Assert.Equal(test.Item2, SQLiteVersion.Of(test.Item1).ToString());
            }
        }
    }
}

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
    public class ColumnInfoTests
    {
        [Fact]
        public void TestEquality()
        {
            Tuple<string, string, string, string, string>[] tests =
                {
                    Tuple.Create("", "", "", "", ""),
                    Tuple.Create("name","", "","", ""),
                    Tuple.Create("name","db", "", "", ""),
                    Tuple.Create("name","db", "table", "", ""),
                    Tuple.Create("name","db", "table", "column", ""),
                    Tuple.Create("name","db", "table", "column", "Variant"),
                };

            Assert.False(new ColumnInfo("", "", "", "", "").Equals(null));
            Assert.False(new ColumnInfo("", "", "", "", "").Equals(new Object()));

            for (int i = 0; i < tests.Length; i++)
            {
                for (int j = 0; j < tests.Length; j++)
                {
                    var fst = new ColumnInfo(tests[i].Item1, tests[i].Item2, tests[i].Item3, tests[i].Item4, tests[i].Item5);
                    var snd = new ColumnInfo(tests[j].Item1, tests[j].Item2, tests[j].Item3, tests[j].Item4, tests[j].Item5);

                    if (i == j)
                    {
                        Assert.True(fst.Equals(fst));
                        Assert.True(snd.Equals(snd));
                        Assert.Equal(fst, snd);
                        Assert.True(fst == snd);
                        Assert.False(fst != snd);
                    }
                    else
                    {
                        Assert.NotEqual(fst, snd);
                        Assert.False(fst == snd);
                        Assert.True(fst != snd);
                    }
                }
            }
        }

        [Fact]
        public void TestGetHashcode()
        {
            Tuple<string, string, string, string, string>[] tests =
                {
                    Tuple.Create("", "", "", "", ""),
                    Tuple.Create("name","", "", "", ""),
                    Tuple.Create("name","db", "", "", ""),
                    Tuple.Create("name","db", "table", "", ""),
                    Tuple.Create("name","db", "table", "column", ""),
                    Tuple.Create("name","db", "table", "column", "Variant"),
                };

            foreach (var test in tests)
            {
                var fst = new ColumnInfo(test.Item1, test.Item2, test.Item3, test.Item4, test.Item5);
                var snd = new ColumnInfo(test.Item1, test.Item2, test.Item3, test.Item4, test.Item5);

                Assert.Equal(fst.GetHashCode(), snd.GetHashCode());
            }
        }

        [Fact]
        public void TestComparison()
        {
            ColumnInfo[] tests =
                {
                    new ColumnInfo("","", "","", ""),
                    new ColumnInfo("name","", "", "", ""),
                    new ColumnInfo("name","db", "", "", ""),
                    new ColumnInfo("name","db", "table", "", ""),
                    new ColumnInfo("name","db", "table", "column", ""),
                    new ColumnInfo("name","db", "table", "column", "Variant"),
                };

            for (int i = 0; i < tests.Length; i++)
            {
                for (int j = 0; j < tests.Length; j++)
                {
                    if (i < j)
                    {
                        Assert.True(tests[i].CompareTo(tests[j]) < 0);
                        Assert.True(((IComparable)tests[i]).CompareTo(tests[j]) < 0);

                        Assert.True(tests[i] < tests[j]);
                        Assert.True(tests[i] <= tests[j]);
                        Assert.False(tests[i] > tests[j]);
                        Assert.False(tests[i] >= tests[j]);
                    }
                    else if (i == j)
                    {
                        Assert.Equal(tests[i].CompareTo(tests[j]), 0);
                        Assert.Equal(((IComparable)tests[i]).CompareTo(tests[j]), 0);

                        Assert.True(tests[i] >= tests[j]);
                        Assert.True(tests[i] <= tests[j]);
                        Assert.False(tests[i] > tests[j]);
                        Assert.False(tests[i] < tests[j]);
                    }
                    else
                    {
                        Assert.True(tests[i].CompareTo(tests[j]) > 0);
                        Assert.True(((IComparable)tests[i]).CompareTo(tests[j]) > 0);

                        Assert.True(tests[i] > tests[j]);
                        Assert.True(tests[i] >= tests[j]);
                        Assert.False(tests[i] < tests[j]);
                        Assert.False(tests[i] <= tests[j]);
                    }
                }
            }

            ColumnInfo nullColumnInfo = null;
            Assert.Equal(new ColumnInfo("", "", "", "", "").CompareTo(nullColumnInfo), 1);

            object nullObj = null;
            Assert.Equal(((IComparable)new ColumnInfo("", "", "", "", "")).CompareTo(nullObj), 1);
        }
    }
}

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

using NUnit.Framework;
using System;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class ColumnInfoTests
    {
        [Test]
        public void TestEquality()
        {
            Tuple<string, string, string, string>[] tests =
                {
                    Tuple.Create("", "", "", ""),
                    Tuple.Create("name","", "",""),
                    Tuple.Create("name","db", "", ""),
                    Tuple.Create("name","db", "table", ""),
                    Tuple.Create("name","db", "table", "column"),
                };

            Assert.False(new ColumnInfo("", "", "", "").Equals(null));
            Assert.False(new ColumnInfo("", "", "", "").Equals(new Object()));

            for (int i = 0; i < tests.Length; i++)
            {
                for (int j = 0; j < tests.Length; j++)
                {
                    var fst = new ColumnInfo(tests[i].Item1, tests[i].Item2, tests[i].Item3, tests[i].Item4);
                    var snd = new ColumnInfo(tests[j].Item1, tests[j].Item2, tests[j].Item3, tests[j].Item4);

                    if (i == j)
                    {
                        Assert.True(fst.Equals(fst));
                        Assert.True(snd.Equals(snd));
                        Assert.AreEqual(fst, snd);
                        Assert.True(fst == snd);
                        Assert.False(fst != snd);
                    }
                    else
                    {
                        Assert.AreNotEqual(fst, snd);
                        Assert.False(fst == snd);
                        Assert.True(fst != snd);
                    }
                }
            }
        }

        [Test]
        public void TestGetHashcode()
        {
            Tuple<string, string, string, string>[] tests =
                {
                    Tuple.Create("", "", "", ""),
                    Tuple.Create("name","", "",""),
                    Tuple.Create("name","db", "", ""),
                    Tuple.Create("name","db", "table", ""),
                    Tuple.Create("name","db", "table", "column"),
                };

            for (int i = 0; i < tests.Length; i++)
            {
                var fst =  new ColumnInfo(tests[i].Item1, tests[i].Item2, tests[i].Item3, tests[i].Item4);
                var snd = new ColumnInfo(tests[i].Item1, tests[i].Item2, tests[i].Item3, tests[i].Item4);

                Assert.AreEqual(fst.GetHashCode(), snd.GetHashCode());
            }
        }

        [Test]
        public void TestComparison()
        {
            ColumnInfo[] tests =
                {
                    new ColumnInfo("","", "",""),
                    new ColumnInfo("name","", "",""),
                    new ColumnInfo("name","db", "", ""),
                    new ColumnInfo("name","db", "table", ""),
                    new ColumnInfo("name","db", "table", "column"),
                };

            for (int i = 0; i < tests.Length; i++)
            {
                for(int j = 0; j < tests.Length; j++)
                {
                    if (i < j)
                    {
                        Assert.Less(tests[i].CompareTo(tests[j]), 0);
                        Assert.Less(((IComparable)tests[i]).CompareTo(tests[j]), 0);

                        Assert.True(tests[i] < tests[j]);
                        Assert.True(tests[i] <= tests[j]);
                        Assert.False(tests[i] > tests[j]);
                        Assert.False(tests[i] >= tests[j]);
                    }
                    else if (i == j)
                    {
                        Assert.AreEqual(tests[i].CompareTo(tests[j]), 0);
                        Assert.AreEqual(((IComparable)tests[i]).CompareTo(tests[j]), 0);

                        Assert.True(tests[i] >= tests[j]);
                        Assert.True(tests[i] <= tests[j]);
                        Assert.False(tests[i] > tests[j]);
                        Assert.False(tests[i] < tests[j]);
                    }
                    else
                    {
                        Assert.Greater(tests[i].CompareTo(tests[j]), 0);
                        Assert.Greater(((IComparable)tests[i]).CompareTo(tests[j]), 0);

                        Assert.True(tests[i] > tests[j]);
                        Assert.True(tests[i] >= tests[j]);
                        Assert.False(tests[i] < tests[j]);
                        Assert.False(tests[i] <= tests[j]);
                    }
                }
            }

            ColumnInfo nullColumnInfo = null;
            Assert.AreEqual(new ColumnInfo("", "", "", "").CompareTo(nullColumnInfo), 1);

            object nullObj = null;
            Assert.AreEqual(((IComparable) new ColumnInfo("", "", "", "")).CompareTo(nullObj), 1); 
        }
    }
}

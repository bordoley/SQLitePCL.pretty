using Xunit;
using System;

namespace SQLitePCL.pretty.tests
{
    public class TableColumnMetadataTests : TestBase
    {
        [Fact]
        public void TestEquality()
        {
            Tuple<string, string, bool, bool, bool>[] tests =
                {
                    Tuple.Create("", "", false, false, false),
                    Tuple.Create("a", "", false, false, false),
                    Tuple.Create("a", "b", false, false, false),
                    Tuple.Create("a", "b", true, false, false),
                    Tuple.Create("a", "b", true, true, false),
                    Tuple.Create("a", "b", true, true, true),
                };

            Assert.False(new TableColumnMetadata("", "", false, false, false).Equals(null));
            Assert.False(new TableColumnMetadata("", "", false, false, false).Equals(new Object()));

            for (int i = 0; i < tests.Length; i++)
            {
                for (int j = 0; j < tests.Length; j++)
                {
                    var fst = new TableColumnMetadata(tests[i].Item1, tests[i].Item2, tests[i].Item3, tests[i].Item4, tests[i].Item5);
                    var snd = new TableColumnMetadata(tests[j].Item1, tests[j].Item2, tests[j].Item3, tests[j].Item4, tests[j].Item5);

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
            Tuple<string, string, bool, bool, bool>[] tests =
                {
                    Tuple.Create("", "", false, false, false),
                    Tuple.Create("a", "", false, false, false),
                    Tuple.Create("a", "b", false, false, false),
                    Tuple.Create("a", "b", true, false, false),
                    Tuple.Create("a", "b", true, true, false),
                    Tuple.Create("a", "b", true, true, true),
                };

            foreach (var test in tests)
            {
                var fst = new TableColumnMetadata(test.Item1, test.Item2, test.Item3, test.Item4, test.Item5);
                var snd = new TableColumnMetadata(test.Item1, test.Item2, test.Item3, test.Item4, test.Item5);

                Assert.Equal(fst.GetHashCode(), snd.GetHashCode());
            }
        }

        [Fact]
        public void TestComparison()
        {
            TableColumnMetadata[] tests =
                {
                    new TableColumnMetadata("", "", false, false, false),
                    new TableColumnMetadata("a", "", false, false, false),
                    new TableColumnMetadata("a", "b", false, false, false),
                    new TableColumnMetadata("a", "b", true, false, false),
                    new TableColumnMetadata("a", "b", true, true, false),
                    new TableColumnMetadata("a", "b", true, true, true),
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

            TableColumnMetadata nullColumnInfo = null;
            Assert.Equal(new TableColumnMetadata("", "", false, false, false).CompareTo(nullColumnInfo), 1);

            object nullObj = null;
            Assert.Equal(((IComparable)new TableColumnMetadata("", "", false, false, false)).CompareTo(nullObj), 1);
        }
    }
}

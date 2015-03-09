// Copyright (c) 2009-2015 Krueger Systems, Inc.
// Copyright (c) 2015 David Bordoley
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Linq;
using NUnit.Framework;

using SQLitePCL.pretty;
using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.tests
{
    [TestFixture]
    public class NullableTest
    {
        public class NullableIntClass
        {
            [PrimaryKey, AutoIncrement]
            public int? ID { get; set; }

            public Nullable<int> NullableInt { get; set; }
        }

        [Test]
        [Description("Create a table with a nullable int column then insert and select against it")]
        public void NullableInt()
        {
            var table = TableMapping.Create<NullableIntClass>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var withNull = new NullableIntClass() { NullableInt = null };
                var with0 = new NullableIntClass() { NullableInt = 0 };
                var with1 = new NullableIntClass() { NullableInt = 1 };
                var withMinus1 = new NullableIntClass() { NullableInt = -1 };

                db.Insert(table, withNull);
                db.Insert(table, with0);
                db.Insert(table, with1);
                db.Insert(table, withMinus1);

                var resultsQuery = table.CreateQuery().OrderBy(x => x.ID);
                var results = db.Query(resultsQuery).ToArray();

                Assert.AreEqual(4, results.Length);
                Assert.AreEqual(withNull.NullableInt, results[0].NullableInt);
                Assert.AreEqual(with0.NullableInt, results[1].NullableInt);
                Assert.AreEqual(with1.NullableInt, results[2].NullableInt);
                Assert.AreEqual(withMinus1.NullableInt, results[3].NullableInt);
            }
        }

        public class NullableFloatClass
        {
            [PrimaryKey, AutoIncrement]
            public int? ID { get; set; }

            public Nullable<float> NullableFloat { get; set; }
        }

        [Test]
        [Description("Create a table with a nullable int column then insert and select against it")]
        public void NullableFloat()
        {
            var table = TableMapping.Create<NullableFloatClass>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var withNull = new NullableFloatClass() { NullableFloat = null };
                var with0 = new NullableFloatClass() { NullableFloat = 0 };
                var with1 = new NullableFloatClass() { NullableFloat = 1 };
                var withMinus1 = new NullableFloatClass() { NullableFloat = -1 };

                db.Insert(table, withNull);
                db.Insert(table, with0);
                db.Insert(table, with1);
                db.Insert(table, withMinus1);

                var resultsQuery = table.CreateQuery().OrderBy(x => x.ID);
                var results = db.Query(resultsQuery).ToArray();

                Assert.AreEqual(4, results.Length);

                Assert.AreEqual(withNull.NullableFloat, results[0].NullableFloat);
                Assert.AreEqual(with0.NullableFloat, results[1].NullableFloat);
                Assert.AreEqual(with1.NullableFloat, results[2].NullableFloat);
                Assert.AreEqual(withMinus1.NullableFloat, results[3].NullableFloat);
            }
        }
            
        public class StringClass
        {
            [PrimaryKey, AutoIncrement]
            public int? ID { get; set; }

            //Strings are allowed to be null by default
            public string StringData { get; set; }
        }

        [Test]
        public void NullableString()
        {
            var table = TableMapping.Create<StringClass>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var withNull = new StringClass() { StringData = null };
                var withEmpty = new StringClass() { StringData = "" };
                var withData = new StringClass() { StringData = "data" };

                db.Insert(table, withNull);
                db.Insert(table, withEmpty);
                db.Insert(table, withData);

                var resultsQuery = table.CreateQuery().OrderBy(x => x.ID);
                var results = db.Query(resultsQuery).ToArray();

                Assert.AreEqual(3, results.Length);

                Assert.AreEqual(withNull.StringData, results[0].StringData);
                Assert.AreEqual(withEmpty.StringData, results[1].StringData);
                Assert.AreEqual(withData.StringData, results[2].StringData);
            }
        }

        [Test]
        public void WhereNotNull()
        {
            var table = TableMapping.Create<NullableIntClass>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var withNull = new NullableIntClass() { NullableInt = null };
                var with0 = new NullableIntClass() { NullableInt = 0 };
                var with1 = new NullableIntClass() { NullableInt = 1 };
                var withMinus1 = new NullableIntClass() { NullableInt = -1 };

                db.Insert(table, withNull);
                db.Insert(table, with0);
                db.Insert(table, with1);
                db.Insert(table, withMinus1);

                var resultsQuery = table.CreateQuery().Where(x => x.NullableInt != default(int?)).OrderBy(x => x.ID);
                var results = db.Query(resultsQuery, default(int?)).ToArray();

                Assert.AreEqual(3, results.Length);

                Assert.AreEqual(with0.NullableInt, results[0].NullableInt);
                Assert.AreEqual(with1.NullableInt, results[1].NullableInt);
                Assert.AreEqual(withMinus1.NullableInt, results[2].NullableInt);
            }
        }

        [Test]
        public void WhereNull()
        {
            var table = TableMapping.Create<NullableIntClass>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var withNull = new NullableIntClass() { NullableInt = null };
                var with0 = new NullableIntClass() { NullableInt = 0 };
                var with1 = new NullableIntClass() { NullableInt = 1 };
                var withMinus1 = new NullableIntClass() { NullableInt = -1 };

                db.Insert(table, withNull);
                db.Insert(table, with0);
                db.Insert(table, with1);
                db.Insert(table, withMinus1);

                var resultsQuery = table.CreateQuery().Where(x => x.NullableInt == default(int?)).OrderBy(x => x.ID);
                var results = db.Query(resultsQuery, default(int?)).ToArray();

                Assert.AreEqual(1, results.Length);
                Assert.AreEqual(withNull.NullableInt, results[0].NullableInt);
            }
        }

        [Test]
        public void StringWhereNull()
        {
            var table = TableMapping.Create<StringClass>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var withNull = new StringClass() { StringData = null };
                var withEmpty = new StringClass() { StringData = "" };
                var withData = new StringClass() { StringData = "data" };

                db.Insert(table, withNull);
                db.Insert(table, withEmpty);
                db.Insert(table, withData);

                var resultsQuery = table.CreateQuery().Where(x => x.StringData == default(string)).OrderBy(x => x.ID);
                var results = db.Query(resultsQuery, default(string)).ToArray();

                Assert.AreEqual(1, results.Length);
                Assert.AreEqual(withNull.StringData, results[0].StringData);
            }
        }

        [Test]
        public void StringWhereNotNull()
        {

            var table = TableMapping.Create<StringClass>();

            using (var db = SQLite3.OpenInMemory())
            {
                db.InitTable(table);

                var withNull = new StringClass() { StringData = null };
                var withEmpty = new StringClass() { StringData = "" };
                var withData = new StringClass() { StringData = "data" };

                db.Insert(table, withNull);
                db.Insert(table, withEmpty);
                db.Insert(table, withData);

                var resultsQuery = table.CreateQuery().Where(x => x.StringData != default(string)).OrderBy(x => x.ID);
                var results = db.Query(resultsQuery, default(string)).ToArray();

                Assert.AreEqual(2, results.Length);
                Assert.AreEqual(withEmpty.StringData, results[0].StringData);
                Assert.AreEqual(withData.StringData, results[1].StringData);
            }
        }
    }
}

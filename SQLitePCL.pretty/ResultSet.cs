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

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Extensions methods for <see cref="IStatement"/> result set rows.
    /// </summary>
    public static class ResultSet
    {
        public static IReadOnlyList<IColumnInfo> Columns(this IReadOnlyList<IResultSetValue> rs)
        {
            Contract.Requires(rs != null);

            return new ResultSetColumnsListImpl(rs);
        }

        internal sealed class ResultSetColumnsListImpl : IReadOnlyList<IColumnInfo>
        {
            private readonly IReadOnlyList<IResultSetValue> rs;

            internal ResultSetColumnsListImpl(IReadOnlyList<IResultSetValue> rs)
            {
                this.rs = rs;
            }

            public IColumnInfo this[int index]
            {
                get
                {
                    return rs[index].ColumnInfo;
                }
            }

            public int Count
            {
                get
                {
                    return rs.Count;
                }
            }

            public IEnumerator<IColumnInfo> GetEnumerator()
            {
                return rs.Select(val => val.ColumnInfo).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}

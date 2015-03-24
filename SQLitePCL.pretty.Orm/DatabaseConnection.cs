//
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
using System.Collections.Generic;
using System.Linq;

namespace SQLitePCL.pretty.Orm
{
    // FIXME: Tempted to get rid of this enum all together in favor convention over configuration.
    [Flags]
    internal enum CreateFlags
    {
        None                    = 0x000,
        FullTextSearch3         = 0x100,    // create virtual table using FTS3
        FullTextSearch4         = 0x200     // create virtual table using FTS4
    }

    /// <summary>
    /// Extensions methods for instances of <see cref="SQLitePCL.pretty.IDatabaseConnection"/>.
    /// </summary>
    public static partial class DatabaseConnection
    {
        internal static void CreateTableIfNotExists(this IDatabaseConnection conn, string tableName, CreateFlags createFlags, IReadOnlyDictionary<string, ColumnMapping> columns)
        {
            var query = SQLBuilder.CreateTableIfNotExists(tableName, createFlags, columns);
            conn.Execute(query);
        }

        /// <summary>
        /// Creates a SQLite prepared statement that can be used to find a row in the table by its rowid. 
        /// </summary>
        /// <returns>The SQLite prepared statement.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableName">The table name.</param>
        internal static IStatement PrepareFindByRowId(this IDatabaseConnection This, string tableName)
        {
            return This.PrepareStatement(SQLBuilder.FindByRowID(tableName));
        }

        /// <summary>
        /// Gets the table column info.
        /// </summary>
        /// <returns>The an <see cref="IReadOnlyDictionary&lt;TKey,TValue&gt;"/> of column names to <see cref="TableColumnMetadata"/>.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableName">The table name.</param>
        internal static IReadOnlyDictionary<string, TableColumnMetadata> GetTableInfo(this IDatabaseConnection This, string tableName)
        {
            // FIXME: Would be preferable to return an actual immutable data structure so that we could cache this result.
            var retval = new Dictionary<string, TableColumnMetadata>();
            foreach (var row in This.Query(SQLBuilder.GetTableInfo(tableName)))
            {
                var column = row[1].ToString();
                retval.Add(column, This.GetTableColumnMetadata(null, tableName, column));
            }
            return retval;
        }
    }
}


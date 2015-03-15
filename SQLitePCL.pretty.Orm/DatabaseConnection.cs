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

    public sealed class IndexInfo : IEquatable<IndexInfo>
    {
        /// <summary>
        /// Indicates whether the two IndexInfo instances are equal to each other.
        /// </summary>
        /// <param name="x">A IndexInfo instance.</param>
        /// <param name="y">A IndexInfo instance.</param>
        /// <returns><see langword="true"/> if the two instances are equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator ==(IndexInfo x, IndexInfo y)
        {
            return x.Equals(y);
        }

        /// <summary>
        /// Indicates whether the two IndexInfo instances are not equal each other.
        /// </summary>
        /// <param name="x">A IndexInfo instance.</param>
        /// <param name="y">A IndexInfo instance.</param>
        /// <returns><see langword="true"/> if the two instances are not equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator !=(IndexInfo x, IndexInfo y)
        {
            return !(x == y);
        }

        private readonly string name;
        private readonly bool unique;
        private readonly IReadOnlyList<string> columns;

        internal IndexInfo(string name, bool unique, IReadOnlyList<string> columns)
        {
            this.name = name;
            this.unique = unique;
            this.columns = columns;
        }

        public string Name { get { return name; } }

        public bool Unique { get { return unique; } }

        public IEnumerable<string> Columns { get { return columns; } }

        public bool Equals(IndexInfo other)
        {
            return (this.Name == other.Name) && 
                (this.Unique == other.Unique) &&
                // FIXME: Ideally we use immutable lists that implement correct equality semantics
                (this.Columns.SequenceEqual(other.Columns));
        }
            
        public override bool Equals(object other)
        {
            return other is IndexInfo && this == (IndexInfo)other;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + this.Name.GetHashCode();
            hash = hash * 31 + this.Unique.GetHashCode();

            // FIXME: This sucks. Prefer to use actual immutable collections.
            foreach (var col in this.Columns)
            {
                hash =  hash * 31 + col.GetHashCode();
            }
            return hash;
        }
    }

    internal static partial class DatabaseConnection
    {
        internal static void CreateTableIfNotExists(this IDatabaseConnection conn, string tableName, CreateFlags createFlags, IEnumerable<Tuple<string, TableColumnMetadata>> columns)
        {
            var query = CreateTableIfNotExists(tableName, createFlags, columns);
            conn.Execute(query);
        }

        internal static IStatement PrepareDelete(this IDatabaseConnection This, string table, string primaryKeyColumn)
        {
            return This.PrepareStatement(SQLBuilder.DeleteUsingPrimaryKey(table, primaryKeyColumn));
        }

        /*
        internal static void Delete(this IDatabaseConnection This, string table, string primaryKeyColumn, object primaryKey)
        {
            This.Execute(SQLBuilder.DeleteUsingPrimaryKey(table, primaryKeyColumn), primaryKey);
        }*/ 

        internal static void DeleteAll(this IDatabaseConnection This, string table)
        {
            This.Execute(SQLBuilder.DeleteAll(table));
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

        /*
        /// <summary>
        /// Finds a row by it's SQLite rowid.
        /// </summary>
        /// <returns>An IEnumerable that contains either 1 or 0 results depending on whether the rowid was found in the table.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="rowid">The rowid of the row to fetch.</param>
        internal static IEnumerable<IReadOnlyList<IResultSetValue>> FindByRowId(this IDatabaseConnection This, string tableName, long rowid)
        {
            return This.Query(SQLBuilder.FindByRowID(tableName), rowid);
        }*/

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

        // FIXME: SHould really be in SQLbuilder, but moved out for now do to the createFlags
        internal static string CreateTableIfNotExists(string tableName, CreateFlags createFlags, IEnumerable<Tuple<string, TableColumnMetadata>> columns)
        {
            bool fts3 = (createFlags & CreateFlags.FullTextSearch3) != 0;
            bool fts4 = (createFlags & CreateFlags.FullTextSearch4) != 0;
            bool fts = fts3 || fts4;

            var @virtual = fts ? "VIRTUAL " : string.Empty;
            var @using = fts3 ? "USING FTS3 " : fts4 ? "USING FTS4 " : string.Empty;

            // Build query.
            var query = "CREATE " + @virtual + "TABLE IF NOT EXISTS \"" + tableName + "\" " + @using + "(\n";
            var decls = columns.Select (c => SQLBuilder.SqlDecl(c.Item1, c.Item2));
            var decl = string.Join (",\n", decls.ToArray ());
            query += decl;
            query += ")";

            return query;
        }
    }
}


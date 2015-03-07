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
    [Flags]
    public enum CreateFlags
    {
        None                = 0x000,
        ImplicitPK          = 0x001,    // create a primary key for field called 'Id' (Orm.ImplicitPkName)
        ImplicitIndex       = 0x002,    // create an index for fields ending in 'Id' (Orm.ImplicitIndexSuffix)
        AllImplicit         = 0x003,    // do both above
        AutoIncPK           = 0x004,    // force PK field to be auto inc
        FullTextSearch3     = 0x100,    // create virtual table using FTS3
        FullTextSearch4     = 0x200     // create virtual table using FTS4
    }

    public static class SQLBuilder
    {
        public const string SelectAllTables = 
            @"SELECT name FROM sqlite_master
              WHERE type='table'
              ORDER BY name;";

        public const string BeginTransaction = "BEGIN TRANSACTION";

        public const string Commit = "COMMIT";

        public const string Rollback = "ROLLBACK";

        public static string Insert(string tableName, IEnumerable<string> columns)
        {
            return string.Format(
                "INSERT INTO \"{0}\"({1}) VALUES ({2})",
                tableName,
                string.Join(",", columns),
                string.Join(",", columns.Select(x => ":" + x)));
        }

        public static string InsertOrReplace(string tableName, IEnumerable<string> columns)
        {
            return string.Format(
                "INSERT OR REPLACE INTO \"{0}\"({1}) VALUES ({2})",
                tableName,
                string.Join(",", columns),
                string.Join(",", columns.Select(x => ":" + x)));
        }

        public static string CreateTable(string tableName, CreateFlags createFlags, IEnumerable<Tuple<string, TableColumnMetadata>> columns)
        {
            bool fts3 = (createFlags & CreateFlags.FullTextSearch3) != 0;
            bool fts4 = (createFlags & CreateFlags.FullTextSearch4) != 0;
            bool fts = fts3 || fts4;

            var @virtual = fts ? "VIRTUAL " : string.Empty;
            var @using = fts3 ? "USING FTS3 " : fts4 ? "USING FTS4 " : string.Empty;

            // Build query.
            var query = "CREATE " + @virtual + "TABLE IF NOT EXISTS \"" + tableName + "\" " + @using + "(\n";
            var decls = columns.Select (c => SqlDecl(c.Item1, c.Item2));
            var decl = string.Join (",\n", decls.ToArray ());
            query += decl;
            query += ")";

            return query;
        }

        private static string SqlDecl (string columnName,  TableColumnMetadata p)
        {
            string decl = "\"" + columnName + "\" " + p.DeclaredType + " ";
            
            if (p.IsPrimaryKeyPart) 
            {
                decl += "PRIMARY KEY ";
            }

            if (p.IsAutoIncrement) 
            {
                decl += "AUTOINCREMENT ";
            }

            if (p.HasNotNullConstraint) 
            {
                decl += "NOT NULL ";
            }

            if (!string.IsNullOrEmpty (p.CollationSequence)) 
            {
                decl += "COLLATE " + p.CollationSequence + " ";
            }
            
            return decl;
        }
    }
}


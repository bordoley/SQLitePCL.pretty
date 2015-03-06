using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLitePCL.pretty.Orm
{
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


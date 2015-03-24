using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLitePCL.pretty.Orm
{
    internal static class SQLBuilder
    {
        internal static string GetTableInfo (string tableName)
        {
            return "PRAGMA TABLE_INFO(\"" + tableName + "\")";         
        }

        internal static string SelectWhereColumnEquals(string tableName, string columnName)
        {
            return string.Format("SELECT * FROM \"{0}\" WHERE \"{1}\" = ?", tableName, columnName);
        }

        internal static string FindByRowID(string tableName)
        {
            return string.Format("SELECT * FROM \"{0}\" WHERE ROWID = ?", tableName);
        }

        internal static string AlterTableAddColumn(string tableName, string columnName, ColumnMapping columnMapping)
        {
            return string.Format("ALTER TABLE \"{0}\" ADD COLUMN {1}", tableName, SqlDecl(columnName, columnMapping));
        }

        internal static string DeleteUsingPrimaryKey(string tableName, string pkColumn)
        {
            return string.Format("DELETE FROM \"{0}\" WHERE \"{1}\" = ?", tableName, pkColumn);
        }

        internal static string InsertOrReplace(string tableName, IEnumerable<string> columns)
        {
            return string.Format(
                "INSERT OR REPLACE INTO \"{0}\" ({1}) VALUES ({2})",
                tableName,
                string.Join(",", columns.Select(x => "\"" + x + "\"")),

                // FIXME: Might need to quote this for some cases. Test!!!
                string.Join(",", columns.Select(x => ":" + x)));
        }

        internal static string CreateIndex(string indexName, string tableName, IEnumerable<string> columnNames, bool unique)
        {
            const string sqlFormat = "CREATE {2} INDEX IF NOT EXISTS \"{3}\" ON \"{0}\"(\"{1}\")";
            return String.Format(sqlFormat, tableName, string.Join ("\", \"", columnNames), unique ? "UNIQUE" : "", indexName);
        }

        internal static string NameIndex(string tableName, IEnumerable<string> columnNames)
        {
            return tableName + "_" + string.Join ("_", columnNames);
        }

        internal static string NameIndex(string tableName, string columnName)
        {
            return NameIndex(tableName, new String[] { columnName});
        }

        internal static string ListIndexes(string tableName)
        {
            return string.Format("PRAGMA INDEX_LIST (\"{0}\")", tableName);
        }

        internal static string IndexInfo(string indexName)
        {
            return string.Format("PRAGMA INDEX_INFO (\"{0}\")", indexName);
        }

        internal static string CreateTableIfNotExists(string tableName, CreateFlags createFlags, IReadOnlyDictionary<string, ColumnMapping> columns)
        {
            bool fts3 = (createFlags & CreateFlags.FullTextSearch3) != 0;
            bool fts4 = (createFlags & CreateFlags.FullTextSearch4) != 0;
            bool fts = fts3 || fts4;

            var @virtual = fts ? "VIRTUAL " : string.Empty;
            var @using = fts3 ? "USING FTS3 " : fts4 ? "USING FTS4 " : string.Empty;

            // Build query.
            var query = "CREATE " + @virtual + "TABLE IF NOT EXISTS \"" + tableName + "\" " + @using + "(\n";
            var decls = columns.Select(c => SQLBuilder.SqlDecl(c.Key, c.Value));
            var decl = string.Join(",\n", decls.ToArray());
            query += decl;
            var fkconstraints = 
                string.Join(
                    ",\n", 
                    columns.Where(x => x.Value.ForeignKeyConstraint != null).Select(x =>
                        string.Format("FOREIGN KEY(\"{0}\") REFERENCES \"{1}\"(\"{2}\")",
                             x.Key, 
                             x.Value.ForeignKeyConstraint.TableName,
                             x.Value.ForeignKeyConstraint.ColumnName)));
            query += (fkconstraints.Length != 0) ? "," + fkconstraints : "";
            query += ")";

            return query;
        }

        internal static string SqlDecl(string columnName, ColumnMapping columnMapping)
        {
            string decl = "\"" + columnName + "\" " + columnMapping.Metadata.DeclaredType + " ";

            if (columnMapping.Metadata.IsPrimaryKeyPart && columnMapping.Metadata.IsAutoIncrement)
            {
                decl += "PRIMARY KEY AUTOINCREMENT NOT NULL ";
            }
            else if (columnMapping.Metadata.IsPrimaryKeyPart)
            {
                decl += "PRIMARY KEY NOT NULL ";
            }
            else if (columnMapping.Metadata.HasNotNullConstraint) 
            {
                decl += "NOT NULL DEFAULT \"" + columnMapping.DefaultValue.ToString() + "\" ";
            }

            if (!string.IsNullOrEmpty (columnMapping.Metadata.CollationSequence)) 
            {
                decl += "COLLATE " + columnMapping.Metadata.CollationSequence + " ";
            }
            
            return decl;
        }

        internal static string DropTableIfExists(string tableName)
        {
            return string.Format("DROP TABLE IF EXISTS \"{0}\"", tableName);
        }

        internal static string DeleteAll(string tableName)
        {
            return string.Format("DELETE FROM \"{0}\"", tableName);
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLitePCL.pretty.Orm
{
    internal static class SQLBuilder
    {
        internal const string ReIndex = "REINDEX";

        internal static string AlterTableRename(string table, string newName)
        {
            return string.Format("ALTER TABLE \"{0}\" RENAME TO \"{1}\"", table, newName);
        }

        internal static string DeleteAll(string tableName)
        {
            return string.Format("DELETE FROM \"{0}\"", tableName);
        }

        internal static string DropTable(string tableName)
        {
            return string.Format("DROP TABLE \"{0}\"", tableName);
        }

        internal static string DropTableIfExists(string tableName)
        {
            return string.Format("DROP TABLE If EXISTS \"{0}\"", tableName);
        }

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

        internal static string AlterTableAddColumn(string tableName, string columnName, TableColumnMetadata metadata)
        {
            return string.Format("ALTER TABLE \"{0}\" ADD COLUMN {1}", tableName, SqlDecl(columnName, metadata));
        }

        internal static string DeleteUsingPrimaryKey(string tableName, string pkColumn)
        {
            return string.Format("DELETE FROM \"{0}\" WHERE \"{1}\" = ?", tableName, pkColumn);
        }

        internal static string Update(string tableName, IEnumerable<string> columns, string pkColumn)
        {
            return string.Format (
                "UPDATE \"{0}\" SET {1} WHERE {2} = ? ", 
                tableName, 
                string.Join (",", columns.Where(x => x != pkColumn).Select(x => "\"" + x + "\" = :" + x).ToArray ()), 
                pkColumn);
        }

        internal static string Insert(string tableName, IEnumerable<string> columns)
        {
            return string.Format(
                "INSERT INTO \"{0}\" ({1}) VALUES ({2})",
                tableName,
                string.Join(",", columns.Select(x => "\"" + x + "\"")),

                // FIXME: Might need to quote this for some cases. Test!!!
                string.Join(",", columns.Select(x => ":" + x)));
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

        internal static string CreateIndex(string indexName, string tableName, string columnName, bool unique)
        {
            return CreateIndex(indexName, tableName, new string[] { columnName }, unique);
        }

        internal static string CreateIndex(string tableName, string columnName, bool unique)
        {
            return CreateIndex(NameIndex(tableName, new String[] { columnName}), tableName, columnName, unique);
        }

        internal static string CreateIndex(string tableName, IEnumerable<string> columnNames, bool unique)
        {
            return CreateIndex(NameIndex(tableName, columnNames), tableName, columnNames, unique);
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

        internal static string ReIndexWithName(string name)
        {
            return "REINDEX " + name;
        }

        internal static string SqlDecl (string columnName, TableColumnMetadata metadata)
        {
            string decl = "\"" + columnName + "\" " + metadata.DeclaredType + " ";
            
            if (metadata.IsPrimaryKeyPart) 
            {
                decl += "PRIMARY KEY ";
            }

            if (metadata.IsAutoIncrement) 
            {
                decl += "AUTOINCREMENT ";
            }

            if (metadata.HasNotNullConstraint) 
            {
                decl += "NOT NULL ";
            }

            if (!string.IsNullOrEmpty (metadata.CollationSequence)) 
            {
                decl += "COLLATE " + metadata.CollationSequence + " ";
            }
            
            return decl;
        }
    }
}


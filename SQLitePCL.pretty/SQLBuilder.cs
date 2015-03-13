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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

// FIXME: Seperate namespace so that we don't overrun the main namespace with non-core types.
namespace SQLitePCL.pretty
{
    internal static class SQLBuilder
    {
        internal const string SelectAllTables = 
            @"SELECT name FROM sqlite_master
              WHERE type='table'
              ORDER BY name;";

        internal const string BeginTransaction = "BEGIN TRANSACTION";

        internal const string Commit = "COMMIT";

        internal const string ReIndex = "REINDEX";

        internal const string Rollback = "ROLLBACK";

        internal const string Vacuum = "VACUUM";

        internal static string AlterTableRename(string table, string newName)
        {
            return string.Format("ALTER TABLE \"{0}\" RENAME TO \"{1}\"", table, newName);
        }

        internal static string BeginTransactionWithMode(TransactionMode mode)
        {
            switch (mode)
            {
                case TransactionMode.Deferred:
                    return "BEGIN DEFERRED TRANSACTION";
                case TransactionMode.Exclusive:
                    return "BEGIN EXCLUSIVE TRANSACTION";
                case TransactionMode.Immediate:
                    return "BEGIN IMMEDIATE TRANSACTION";
                default:
                    throw new ArgumentException("Not a valid TransactionMode");
            }
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

        internal static string SavePoint(string savePoint)
        {
            return "SAVEPOINT " + savePoint;
        }

        internal static string Release(string savePoint)
        {
            return "RELEASE " + savePoint;
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

        internal static string RollbackTo(string savepoint)
        {
            return "ROLLBACK TO " + savepoint;
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


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
using System.Threading;

namespace SQLitePCL.pretty
{
    [Flags]
    public enum CreateFlags
    {
        None                    = 0x000,
        ImplicitPrimaryKey      = 0x001,    // create a primary key for field called 'Id' (Orm.ImplicitPkName)
        ImplicitIndex           = 0x002,    // create an index for fields ending in 'Id' (Orm.ImplicitIndexSuffix)
        AllImplicit             = 0x003,    // do both above
        AutoIncrementPrimaryKey = 0x004,    // force PK field to be auto inc
        FullTextSearch3         = 0x100,    // create virtual table using FTS3
        FullTextSearch4         = 0x200     // create virtual table using FTS4
    }

    public static class DatabaseConnection
    {
        private static readonly ThreadLocal<Random> _rand = new ThreadLocal<Random>(() => new Random());

        public static void CreateTableIfNotExists(this IDatabaseConnection conn, string tableName, CreateFlags createFlags, IEnumerable<Tuple<string, TableColumnMetadata>> columns)
        {
            var query = SQLBuilder.CreateTableIfNotExists(tableName, createFlags, columns);
            conn.Execute(query);
        }

        public static IStatement PrepareDelete(this IDatabaseConnection This, string table, string primaryKeyColumn)
        {
            return This.PrepareStatement(SQLBuilder.DeleteUsingPrimaryKey(table, primaryKeyColumn));
        }

        public static void Delete(this IDatabaseConnection This, string table, string primaryKeyColumn, object primaryKey)
        {
            This.Execute(SQLBuilder.DeleteUsingPrimaryKey(table, primaryKeyColumn), primaryKey);
        } 

        public static void DeleteAll(this IDatabaseConnection This, string table)
        {
            This.Execute(SQLBuilder.DeleteAll, table);
        }

        public static void DropTable(this IDatabaseConnection This, string table)
        {
            This.Execute(SQLBuilder.DropTable, table);
        }

        public static void BeginTransaction(this IDatabaseConnection This)
        {
            This.TryOrRollback(db => db.Execute(SQLBuilder.BeginTransaction));
        }

        public static string SaveTransactionPoint(this IDatabaseConnection This)
        {
            var savePoint = "S" + _rand.Value.Next (short.MaxValue);
            This.TryOrRollback(db => db.Execute(SQLBuilder.SavePoint, savePoint));
            return savePoint;
        }

        public static void Release(this IDatabaseConnection This, string savepoint)
        {
            This.Execute(SQLBuilder.Release, savepoint);
        }

        public static void Commit(this IDatabaseConnection This)
        {
            This.Execute(SQLBuilder.Commit);
        }

        public static void RollbackTo(this IDatabaseConnection This, string savepoint)
        {
            This.Execute(SQLBuilder.RollbackTo(savepoint));
        }

        public static void Rollback(this IDatabaseConnection This)
        {
            This.Execute(SQLBuilder.Rollback);
        }

        public static void CreateIndex(this IDatabaseConnection This, string indexName, string tableName, IEnumerable<string> columnNames, bool unique)
        {
            This.Execute(SQLBuilder.CreateIndex(indexName, tableName, columnNames, unique));
        }

        public static void CreateIndex(this IDatabaseConnection This, string indexName, string tableName, string columnName, bool unique)
        {
            This.Execute(SQLBuilder.CreateIndex(indexName, tableName, columnName, unique));
        }

        public static void CreateIndex(this IDatabaseConnection This, string tableName, string columnName, bool unique)
        {
            This.Execute(SQLBuilder.CreateIndex(tableName, columnName, unique));
        }

        public static void CreateIndex(this IDatabaseConnection This, string tableName, IEnumerable<string> columnNames, bool unique)
        {
            This.Execute(SQLBuilder.CreateIndex(tableName,columnNames, unique));
        }

        public static IReadOnlyDictionary<string, TableColumnMetadata> GetTableInfo(this IDatabaseConnection This, string tableName)
        {
            // FIXME: Would be preferable to return an actual immutable data structure so that we could cache this result.
            var retval = new Dictionary<string, TableColumnMetadata>();
            foreach (var row in This.Query(SQLBuilder.TableInfo, tableName))
            {
                var column = row[1].ToString();
                retval.Add(column, This.GetTableColumnMetadata(null, tableName, column));
            }
            return retval;
        }

        public static void RunInTransaction(this IDatabaseConnection This, Action<IDatabaseConnection> action)
        {
            This.RunInTransaction(db => 
                {
                    action(db);
                    return Enumerable.Empty<object>();
                });
        }

        public static T RunInTransaction<T>(this IDatabaseConnection This, Func<IDatabaseConnection, T> func)
        {
            return This.RunInTransaction(db => Enumerable.Repeat(func(db), 1)).First();
        }

        public static IEnumerable<T> RunInTransaction<T>(this IDatabaseConnection This, Func<IDatabaseConnection, IEnumerable<T>> func)
        {
            try 
            {
                var savePoint = This.SaveTransactionPoint ();
                var retval = func(This);
                This.Release (savePoint);
                return retval;
            } 
            catch (Exception) 
            {
                Rollback(This);
                throw;
            }
        }

        private static void TryOrRollback(this IDatabaseConnection This, Action<IDatabaseConnection> action)
        {
            try
            {
                action(This);
            }
            catch (SQLiteException e)
            {
                switch (e.ErrorCode)
                {   
                    // It is recommended that applications respond to the errors listed below 
                    // by explicitly issuing a ROLLBACK command.
                    case ErrorCode.IOError:
                    case ErrorCode.Full:
                    case ErrorCode.Busy:
                    case ErrorCode.NoMemory:
                    case ErrorCode.Interrupt:
                        Rollback(This);
                        break;
                }

                throw;
            }
        }
    }
}


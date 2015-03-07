using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SQLitePCL.pretty.Orm
{
    public static class DatabaseConnection
    {
        private static readonly ThreadLocal<Random> _rand = new ThreadLocal<Random>(() => new Random());

        public static void CreateTable(this IDatabaseConnection conn, string tableName, CreateFlags createFlags, IEnumerable<Tuple<string, TableColumnMetadata>> columns)
        {
            var query = SQLBuilder.CreateTable(tableName, createFlags, columns);
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
            This.Execute (SQLBuilder.DropTable, table);
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
            This.Execute("ROLLBACK TO " + savepoint);
        }

        public static void Rollback(this IDatabaseConnection This)
        {
            This.Execute("ROLLBACK");
        }

        public static void RunInTransaction (this IDatabaseConnection This, Action<IDatabaseConnection> action)
        {
            try 
            {
                var savePoint = This.SaveTransactionPoint ();
                action (This);
                This.Release (savePoint);
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
                    //    by explicitly issuing a ROLLBACK command.
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


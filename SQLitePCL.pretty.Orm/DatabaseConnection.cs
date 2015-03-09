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
            
        public bool Equals(object other)
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
            This.Execute(SQLBuilder.DeleteAll(table));
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
            This.TryOrRollback(db => db.Execute(SQLBuilder.SavePoint(savePoint)));
            return savePoint;
        }

        public static void Release(this IDatabaseConnection This, string savepoint)
        {
            This.Execute(SQLBuilder.Release(savepoint));
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

        public static IEnumerable<IndexInfo> GetIndexInfo(this IDatabaseConnection This, string tableName)
        {
            return This.RunInTransaction(db =>
                db.Query(SQLBuilder.ListIndexes(tableName))
                    .Select(row => 
                        {
                            var indexName = row[1].ToString();
                            var unique = row[2].ToBool();

                            var columns = 
                                db.Query(SQLBuilder.IndexInfo(indexName))
                                  .Select(x => Tuple.Create(x[0].ToInt(),x[2].ToString()))
                                  .OrderBy(x => x.Item1)
                                  .Select(x => x.Item2)
                                  .ToList();
                            return new IndexInfo(indexName, unique, columns);
                        }).ToList());
        }

        public static IReadOnlyDictionary<string, TableColumnMetadata> GetTableInfo(this IDatabaseConnection This, string tableName)
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
            var savePoint = This.SaveTransactionPoint ();

            try 
            {
                var retval = func(This);
                This.Release (savePoint);
                return retval;
            } 
            catch (Exception) 
            {
                // FIXME: This differs from SQLite-Net. SQLite-net they keep track of the transaction depth
                // and then control whether to issue a rollback command. We don't so always rollback
                // to the savepoint and let the exception propogate.
                // We could consider tracking the transaction depth but its a little painful to do
                // as an extension property.
                This.RollbackTo(savePoint);
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


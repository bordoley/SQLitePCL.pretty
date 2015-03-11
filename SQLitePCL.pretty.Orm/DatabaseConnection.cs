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
    // FIXME: Tempted to get rid of this enum all together in favor convention over configuration.
    [Flags]
    public enum CreateFlags
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
            This.Execute(SQLBuilder.DropTableIfExists(table));
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

        public static void Vacuum(this IDatabaseConnection This)
        {
            This.Execute(SQLBuilder.Vacuum);
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

        /// <summary>
        /// Creates a SQLite prepared statement that can be used to find a row in the table by its rowid. 
        /// </summary>
        /// <returns>The SQLite prepared statement.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableName">The table name.</param>
        public static IStatement PrepareFindByRowId(this IDatabaseConnection This, string tableName)
        {
            return This.PrepareStatement(SQLBuilder.FindByRowID(tableName));
        }

        /// <summary>
        /// Finds a row by it's SQLite rowid.
        /// </summary>
        /// <returns>An IEnumerable that contains either 1 or 0 results depending on whether the rowid was found in the table.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="rowid">The rowid of the row to fetch.</param>
        public static IEnumerable<IReadOnlyList<IResultSetValue>> FindByRowId(this IDatabaseConnection This, string tableName, long rowid)
        {
            return This.Query(SQLBuilder.FindByRowID(tableName), rowid);
        }

        /// <summary>
        /// Gets the table column info.
        /// </summary>
        /// <returns>The an <see cref="IReadOnlyDictionary&lt;TKey,TValue&gt;"/> of column names to <see cref="TableColumnMetadata"/>.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="tableName">The table name.</param>
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

        /// <summary>
        ///  Runs the action <paramref name="action"/> in a transaction.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="action">The Action to run in a transaction.</param>
        public static void RunInTransaction(this IDatabaseConnection This, Action<IDatabaseConnection> action)
        {
            This.RunInTransaction(db => 
                {
                    action(db);
                    return Enumerable.Empty<object>();
                });
        }


        /// <summary>
        /// Runs the function <paramref name="f"/> in a transaction and returns function result.
        /// </summary>
        /// <returns>The function result.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="f">
        /// The function to run in a transaction.
        /// </param>
        /// <typeparam name="T">The result type.</typeparam>
        public static T RunInTransaction<T>(this IDatabaseConnection This, Func<IDatabaseConnection, T> f)
        {
            var savePoint = This.SaveTransactionPoint ();

            try 
            {
                var retval = f(This);
                This.Release (savePoint);
                return retval;
            } 
            catch (Exception) 
            {
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


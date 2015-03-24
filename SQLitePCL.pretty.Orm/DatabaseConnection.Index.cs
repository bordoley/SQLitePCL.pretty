using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLitePCL.pretty.Orm
{
    public static partial class DatabaseConnection
    {   
        internal static void CreateIndex(this IDatabaseConnection This, string indexName, string tableName, IEnumerable<string> columnNames, bool unique)
        {
            This.Execute(SQLBuilder.CreateIndex(indexName, tableName, columnNames, unique));
        }

        /*
        internal static void CreateIndex(this IDatabaseConnection This, string indexName, string tableName, string columnName, bool unique)
        {
            This.Execute(SQLBuilder.CreateIndex(indexName, tableName, columnName, unique));
        }

        internal static void CreateIndex(this IDatabaseConnection This, string tableName, string columnName, bool unique)
        {
            This.Execute(SQLBuilder.CreateIndex(tableName, columnName, unique));
        }

        internal static void CreateIndex(this IDatabaseConnection This, string tableName, IEnumerable<string> columnNames, bool unique)
        {
            This.Execute(SQLBuilder.CreateIndex(tableName,columnNames, unique));
        }*/

        /*
        /// <summary>
        /// Rebuilds all indexes in all attached databases.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <seealso href="https://www.sqlite.org/lang_reindex.html"/>
        internal static void ReIndex(this IDatabaseConnection This)
        {
            This.Execute(SQLBuilder.ReIndex);
        }

        /// <summary>
        /// Delete and recreate indexes from scratch. Useful when the definition of a collation sequence has changed.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">
        /// Either a collation sequence name, or a table or index name optionally prefixed by a database name.
        /// </param>
        /// <seealso href="https://www.sqlite.org/lang_reindex.html"/>
        internal static void ReIndex(this IDatabaseConnection This, string name)
        {
            This.Execute(SQLBuilder.ReIndexWithName(name));
        }*/

        internal static IReadOnlyDictionary<string, IndexInfo> GetIndexInfo(this IDatabaseConnection This, string tableName)
        {
            return This.RunInTransaction(db =>
                {
                    var retval = new Dictionary<string,IndexInfo>();

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

                                retval.Add(indexName, new IndexInfo(unique, columns));

                                return row;
                            }).LastOrDefault();

                    return retval;
                });
            
        }
    }
}


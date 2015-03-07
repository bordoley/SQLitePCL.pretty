using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLitePCL.pretty.Orm
{
    public static class DatabaseConnection
    {
        public static void CreateTable(this IDatabaseConnection conn, string tableName, CreateFlags createFlags, IEnumerable<Tuple<string, TableColumnMetadata>> columns)
        {
            var query = SQLBuilder.CreateTable(tableName, createFlags, columns);
            conn.Execute(query);
        }
    }
}


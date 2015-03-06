using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLitePCL.pretty.Orm
{
    [Flags]
    public enum CreateFlags
    {
        None                = 0x000,
        ImplicitPK          = 0x001,    // create a primary key for field called 'Id' (Orm.ImplicitPkName)
        ImplicitIndex       = 0x002,    // create an index for fields ending in 'Id' (Orm.ImplicitIndexSuffix)
        AllImplicit         = 0x003,    // do both above
        AutoIncPK           = 0x004,    // force PK field to be auto inc
        FullTextSearch3     = 0x100,    // create virtual table using FTS3
        FullTextSearch4     = 0x200     // create virtual table using FTS4
    }

    public static class DatabaseConnection
    {
        public static void CreateTable(this IDatabaseConnection conn, string tableName, CreateFlags createFlags, IEnumerable<Tuple<string, TableColumnMetadata>> columns)
        {
            var query = SQLBuilder.CreateTable(tableName, createFlags, columns);
            conn.Execute(query);
        }
    }
}


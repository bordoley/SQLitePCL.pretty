using System;
using System.Collections.Generic;

namespace SQLitePCL.pretty
{
    public static partial class DatabaseConnection
    {   
        /// <summary>
        /// Rebuilds all indexes in all attached databases.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <seealso href="https://www.sqlite.org/lang_reindex.html"/>
        public static void ReIndex(this IDatabaseConnection This)
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
        public static void ReIndex(this IDatabaseConnection This, string name)
        {
            This.Execute(SQLBuilder.ReIndexWithName(name));
        }
    }
}


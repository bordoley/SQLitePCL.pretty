/*
   Copyright 2014 David Bordoley
   Copyright 2014 Zumero, LLC

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace SQLitePCL.pretty.Orm
{
    internal static partial class DatabaseConnection
    {
        /// <summary>
        /// Drops the table if it exists. Otherwise this is a no-op.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="table">The table name.</param>
        /// <seealso href="https://www.sqlite.org/lang_droptable.html"/>
        internal static void DropTableIfExists(this IDatabaseConnection This, string table)
        {
            This.Execute(SQLBuilder.DropTableIfExists(table));
        }
    }
}
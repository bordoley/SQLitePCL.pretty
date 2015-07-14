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
using System.Threading.Tasks;
using System.Threading;

namespace SQLitePCL.pretty.Orm
{
    public static partial class DatabaseConnection
    {
        /// <summary>
        /// Creates or migrate a table in the database for the given table mapping, creating indexes if needed.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static void InitTable<T>(this IDatabaseConnection This)
        {
            Contract.Requires(This != null);

            This.RunInTransaction(db =>
                {
                    var tableMapping = TableMapping.Get<T>();
                    db.CreateTableIfNotExists(tableMapping.TableName, CreateFlags.None, tableMapping.Columns);

                    if (db.Changes != 0)
                    {
                        db.MigrateTable(tableMapping);
                    }

                    foreach (var index in tableMapping.Indexes) 
                    {
                        db.CreateIndex(index.Key, tableMapping.TableName, index.Value.Columns, index.Value.Unique);
                    }
                });
        }


        private static void MigrateTable(this IDatabaseConnection This, TableMapping tableMapping)
        {
            var existingCols = This.GetTableInfo(tableMapping.TableName);
            
            var toBeAdded = new List<KeyValuePair<string, ColumnMapping>> ();

            foreach (var p in tableMapping.Columns) 
            {
                if (!existingCols.ContainsKey(p.Key)) { toBeAdded.Add (p); }
            }
            
            foreach (var p in toBeAdded) 
            {
                This.Execute (SQLBuilder.AlterTableAddColumn(tableMapping.TableName, p.Key, p.Value));
            }
        }
    }

    public static partial class AsyncDatabaseConnection
    {
        /// <summary>
        /// Creates or migrate a table in the database for the given table mapping, creating indexes if needed.
        /// </summary>
        /// <returns>A task that completes once the table is succesfully created and is ready for use.</returns>
        /// <param name="This">The database connection</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task InitTableAsync<T>(this IAsyncDatabaseConnection This, CancellationToken cancellationToken)
        {
            Contract.Requires(This != null);
            return This.Use((db, ct) => db.InitTable<T>(), cancellationToken);
        }

        /// <summary>
        /// Creates or migrate a table in the database for the given table mapping, creating indexes if needed.
        /// </summary>
        /// <returns>A task that completes once the table is succesfully created and is ready for use.</returns>
        /// <param name="This">The database connection</param>
        /// <typeparam name="T">The mapped type.</typeparam>
        public static Task InitTableAsync<T>(this IAsyncDatabaseConnection This)
        {
            Contract.Requires(This != null);
            return This.InitTableAsync<T>(CancellationToken.None);
        }
    }
}
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

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Static methods for opening instances of <see cref="IDatabaseConnection"/>
    /// and for accessing static SQLite3 properties, and functions.
    /// </summary>
    public static class SQLite3
    {
        private static IEnumerator<string> CompilerOptionsEnumerator()
        {
            for (int i = 0; ; i++)
            {
                var option = raw.sqlite3_compileoption_get(i);
                if (option != null)
                {
                    yield return option;
                }
                else
                {
                    break;
                }
            }
        }

        private static readonly IEnumerable<string> compilerOptions =
            new DelegatingEnumerable<string>(CompilerOptionsEnumerator);

        /// <summary>
        /// The SQLite compiler options that were defined at compile time.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/compileoption_get.html"/>
        public static IEnumerable<string> CompilerOptions
        {
            get
            {
                return compilerOptions;
            }
        }

        private static readonly SQLiteVersion version = SQLiteVersion.Of(raw.sqlite3_libversion_number());

        /// <summary>
        /// Enables or disables the sharing of the database cache and schema data structures
        /// between connections to the same database.
        /// </summary>
        /// <seealso href="https://www.sqlite.org/c3ref/enable_shared_cache.html"/>
        public static bool EnableSharedCache
        {
            set
            {
                int rc = raw.sqlite3_enable_shared_cache(value ? 1 : 0);
                SQLiteException.CheckOk(rc);
            }
        }

        /// <summary>
        /// The SQLite version.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/libversion.html"/>
        public static SQLiteVersion Version
        {
            get
            {
                return version;
            }
        }

        /// <summary>
        /// The SQLite source id.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/libversion.html"/>
        public static string SourceId
        {
            get
            {
                return raw.sqlite3_sourceid();
            }
        }

        /// <summary>
        /// Returns the number of bytes of memory currently outstanding (malloced but not freed) by SQLite.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/memory_highwater.html"/>
        public static long MemoryUsed
        {
            get
            {
                return raw.sqlite3_memory_used();
            }
        }

        /// <summary>
        /// Returns the maximum value of <see cref="MemoryUsed"/> since the high-water mark was last reset.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/memory_highwater.html"/>
        public static long MemoryHighWater
        {
            get
            {
                return raw.sqlite3_memory_highwater(0);
            }
        }

        /// <summary>
        /// Indicates whether the specified option was defined at compile time.
        /// </summary>
        /// <param name="option">The SQLite compile option. The SQLITE_ prefix may be omitted.</param>
        /// <returns><see langword="true"/> if the compile option is use, otherwise <see langword="false"/></returns>
        /// <seealso href="https://sqlite.org/c3ref/compileoption_get.html"/>
        public static bool CompileOptionUsed(string option)
        {
            Contract.Requires(option != null);
            return raw.sqlite3_compileoption_used(option) != 0;
        }

        /// <summary>
        /// Opens a SQLite database.
        /// </summary>
        /// <param name="filename">The database filename.</param>
        /// <returns>A <see cref="SQLiteDatabaseConnection"/> instance.</returns>
        /// <seealso href="https://sqlite.org/c3ref/open.html"/>
        public static SQLiteDatabaseConnection Open(string filename)
        {
            Contract.Requires(filename != null);

            sqlite3 db;
            int rc = raw.sqlite3_open(filename, out db);
            SQLiteException.CheckOk(db, rc);

            return new SQLiteDatabaseConnection(db);
        }

        /// <summary>
        /// Opens a SQLite database.
        /// </summary>
        /// <param name="filename">The database filename.</param>
        /// <param name="flags"><see cref="ConnectionFlags"/> used to defined if the database is readonly,
        /// read/write and whether a new database file should be created if it does not already exist.</param>
        /// <param name="vfs">
        /// The name of the sqlite3_vfs object that defines the operating system interface
        /// that the new database connection should use. If <see langword="null"/>, then
        /// the default sqlite3_vfs object is used.</param>
        /// <returns>A <see cref="SQLiteDatabaseConnection"/> instance.</returns>
        /// <seealso href="https://sqlite.org/c3ref/open.html"/>
        public static SQLiteDatabaseConnection Open(string filename, ConnectionFlags flags, string vfs)
        {
            Contract.Requires(filename != null);

            sqlite3 db;
            int rc = raw.sqlite3_open_v2(filename, out db, (int)flags, vfs);
            SQLiteException.CheckOk(rc);

            return new SQLiteDatabaseConnection(db);
        }

        /// <summary>
        /// Reset the memory high-water mark to the current value of <see cref="MemoryUsed"/>.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/memory_highwater.html"/>
        public static void ResetMemoryHighWater()
        {
            raw.sqlite3_memory_highwater(1);
        }

        /// <summary>
        /// Determines if the text provided forms a complete SQL statement.
        /// </summary>
        /// <param name="sql">The text to evaluate.</param>
        /// <returns><see langword="true"/> if the text forms a complete SQL
        /// statement, otherwise <see langword="false"/>.</returns>
        public static bool IsCompleteStatement(string sql)
        {
            Contract.Requires(sql != null);
            return raw.sqlite3_complete(sql) != 0;
        }

        /// <summary>
        /// Retrieve runtime status information about the 
        /// performance of SQLite, and optionally to reset various highwater marks.
        /// </summary>
        /// <seealso href="https://www.sqlite.org/c3ref/status.html"/>
        /// <param name="statusCode">The specific parameter to measure.</param>
        /// <param name="reset">If <see langword="true"/>, then the highest record value is reset.</param>
        /// <returns></returns>
        public static SQLiteStatusResult Status(SQLiteStatusCode statusCode, bool reset)
        {
            int pCurrent;
            int pHighwater;
            int rc = raw.sqlite3_status((int)statusCode, out pCurrent, out pHighwater, reset ? 1 : 0);
            SQLiteException.CheckOk(rc);

            return new SQLiteStatusResult(pCurrent, pHighwater);
        }
    }
}

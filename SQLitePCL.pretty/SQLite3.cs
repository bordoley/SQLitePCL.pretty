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
    public static class SQLite3
    {
        private static IEnumerator<String> compilerOptionsEnumerator()
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

        private static readonly IEnumerable<String> compilerOptions =
            new DelegatingEnumerable<String>(() => compilerOptionsEnumerator());

        public static IEnumerable<String> CompilerOptions
        {
            get
            {
                return compilerOptions;
            }
        }

        private static readonly SQLiteVersion version = SQLiteVersion.Of(raw.sqlite3_libversion_number());

        public static SQLiteVersion Version
        {
            get
            {
                return version;
            }
        }

        public static string SourceId
        {
            get
            {
                return raw.sqlite3_sourceid();
            }
        }

        public static long MemoryUsed
        {
            get
            {
                return raw.sqlite3_memory_used();
            }
        }

        public static long MemoryHighWater
        {
            get
            {
                return raw.sqlite3_memory_highwater(0);
            }
        }

        public static bool CompileOptionUsed(string option)
        {
            Contract.Requires(option != null);
            return raw.sqlite3_compileoption_used(option) == 0 ? false : true;
        }

        public static SQLiteDatabaseConnection Open(string filename)
        {
            Contract.Requires(filename != null);

            sqlite3 db;
            int rc = raw.sqlite3_open(filename, out db);
            SQLiteException.CheckOk(rc);

            return new SQLiteDatabaseConnection(db);
        }

        public static SQLiteDatabaseConnection Open(string filename, ConnectionFlags flags, string vfs)
        {
            Contract.Requires(filename != null);

            sqlite3 db;
            int rc = raw.sqlite3_open_v2(filename, out db, (int)flags, vfs);
            SQLiteException.CheckOk(rc);

            return new SQLiteDatabaseConnection(db);
        }

        public static void ResetMemoryHighWater()
        {
            raw.sqlite3_memory_highwater(1);
        }

        public static bool IsCompleteStatement(string sql)
        {
            Contract.Requires(sql != null);
            return raw.sqlite3_complete(sql) == 0 ? false : true;
        }
    }
}

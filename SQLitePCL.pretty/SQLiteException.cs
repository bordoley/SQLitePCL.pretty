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

namespace SQLitePCL.pretty
{
    public sealed class SQLiteException : Exception
    {
        internal static void CheckOk(int rc)
        {
            string msg = "";

            if (SQLite3.Version.CompareTo(SQLiteVersion.Of(3007015)) >= 0)
            {
                msg = raw.sqlite3_errstr(rc);
            }

            if (raw.SQLITE_OK != rc)
            {
                throw SQLiteException.Create(rc, rc, msg);
            }
        }

        internal static void CheckOk(sqlite3 db, int rc)
        {
            int extended = raw.sqlite3_extended_errcode(db);
            if (raw.SQLITE_OK != rc)
            {
                throw SQLiteException.Create(rc, extended, raw.sqlite3_errmsg(db));
            }
        }

        internal static void CheckOk(sqlite3_stmt stmt, int rc)
        {
            CheckOk(raw.sqlite3_db_handle(stmt), rc);
        }

        internal static SQLiteException Create(int rc, int extended, string msg)
        {
            return Create((ErrorCode)rc, (ErrorCode)extended, msg);
        }

        internal static SQLiteException Create(ErrorCode rc, ErrorCode extended, string msg)
        {
            return new SQLiteException(rc, extended, msg);
        }

        private readonly ErrorCode errorCode;
        private readonly ErrorCode extendedErrorCode;
        private readonly string errmsg;

        private SQLiteException(ErrorCode errorCode, ErrorCode extendedErrorCode, string msg)
        {
            this.errorCode = errorCode;
            this.extendedErrorCode = extendedErrorCode;
            errmsg = msg;
        }

        public ErrorCode ErrorCode
        {
            get
            {
                return errorCode;
            }
        }

        public ErrorCode ExtendedErrorCode
        {
            get
            {
                return extendedErrorCode;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}\r\n{2}", errorCode, errmsg, base.ToString());
        }
    }
}

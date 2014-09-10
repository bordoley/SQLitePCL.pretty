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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;

namespace SQLitePCL.pretty
{
    internal sealed class DatabaseBackupImpl : IDatabaseBackup
    {
        private readonly sqlite3_backup backup;

        internal DatabaseBackupImpl(sqlite3_backup backup)
        {
            this.backup = backup;
        }

        public int PageCount 
        { 
            get
            {
                return raw.sqlite3_backup_pagecount(backup);
            }
        }

        public int RemainingPages 
        { 
            get
            {
                return raw.sqlite3_backup_remaining(backup);
            }
        }

        public void Dispose()
        {
            int rc = raw.sqlite3_backup_finish(backup);
            SQLiteException.CheckOk(rc);
        }

        public bool Step (int nPages)
        {
            int rc = raw.sqlite3_backup_step(backup, nPages);

            if (rc == raw.SQLITE_OK)
            {
                return true;
            }
            else if (rc == raw.SQLITE_DONE)
            {
                return false;
            }
            else
            { 
                SQLiteException.CheckOk(rc);
                // Never happens, exception always thrown.
                return false;
            }
        }
    }

    internal sealed class StatementImpl: IStatement 
    {
        private readonly sqlite3_stmt stmt;
        private readonly IReadOnlyList<IResultSetValue> current;

        internal StatementImpl(sqlite3_stmt stmt)
        {
            this.stmt = stmt;
            this.current = new ResultSetImpl(stmt);
        }

        public int BindParameterCount 
        { 
            get 
            { 
                return raw.sqlite3_bind_parameter_count(stmt); 
            }
        } 

        public string SQL 
        { 
            get 
            { 
                return raw.sqlite3_sql(stmt); 
            }
        }

        public bool ReadOnly 
        { 
            get
            {
                return raw.sqlite3_stmt_readonly(stmt) == 0 ? false : true;
            }
        }

        public bool Busy
        { 
            get
            {
                return raw.sqlite3_stmt_busy(stmt) == 0 ? false : true;
            }
        }

        public void Bind(int index, byte[] blob)
        {
            Contract.Requires(blob != null);
            Contract.Requires(index >= 0);
            Contract.Requires(index < this.BindParameterCount);

            int rc = raw.sqlite3_bind_blob(stmt, index+1, blob);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void Bind(int index, double val)
        {
            Contract.Requires(index >= 0);
            Contract.Requires(index < this.BindParameterCount);

            int rc = raw.sqlite3_bind_double(stmt, index+1, val);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void Bind(int index, int val)
        {
            Contract.Requires(index >= 0);
            Contract.Requires(index < this.BindParameterCount);

            int rc = raw.sqlite3_bind_int(stmt, index+1, val);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void Bind(int index, long val)
        {
            Contract.Requires(index >= 0);
            Contract.Requires(index < this.BindParameterCount);

            int rc = raw.sqlite3_bind_int64(stmt, index+1, val);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void Bind(int index, string text)
        {
            Contract.Requires(text != null);
            Contract.Requires(index >= 0);
            Contract.Requires(index < this.BindParameterCount);

            int rc = raw.sqlite3_bind_text(stmt, index+1, text);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void BindNull(int index)
        {
            Contract.Requires(index >= 0);
            Contract.Requires(index < this.BindParameterCount);

            int rc = raw.sqlite3_bind_null(stmt, index+1);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void BindZeroBlob(int index, int size)
        {
            Contract.Requires(index >= 0);
            Contract.Requires(index < this.BindParameterCount);
            Contract.Requires(size >= 0);

            int rc = raw.sqlite3_bind_zeroblob(stmt, index+1, size);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void ClearBindings()
        {
            int rc = raw.sqlite3_clear_bindings(stmt);
            SQLiteException.CheckOk(stmt, rc);
        }

        public int GetBindParameterIndex(string parameter)
        {
            Contract.Requires(parameter != null);

            return raw.sqlite3_bind_parameter_index(stmt, parameter) - 1;
        }

        public string GetBindParameterName(int index)
        {
            Contract.Requires(index >= 0);
            Contract.Requires(index < this.BindParameterCount);

            return raw.sqlite3_bind_parameter_name(stmt, index + 1);
        }

        public void Dispose()
        {
            stmt.Dispose();
        }

        public IReadOnlyList<IResultSetValue> Current 
        {
            get
            {
                return current;
            }
        }

        Object IEnumerator.Current 
        {
            get 
            { 
                return current;
            }
        }

        public bool MoveNext()
        {
            int rc = raw.sqlite3_step(stmt);
            if (rc == raw.SQLITE_DONE)
            { 
                return false;
            }
            else if (rc == raw.SQLITE_ROW)
            {
                return true;
            }
            else
            {
                SQLiteException.CheckOk(stmt, rc);
                // Never gets returned
                return false;
            }
        }

        public void Reset()
        {
            int rc = raw.sqlite3_reset(stmt);
            SQLiteException.CheckOk(stmt, rc);
        }
    }
        
    internal sealed class ResultSetImpl : IReadOnlyList<IResultSetValue>  
    {
        private readonly sqlite3_stmt stmt;

        internal ResultSetImpl(sqlite3_stmt stmt)
        {
            this.stmt = stmt;
        }

        public int Count 
        { 
            get
            {
                return raw.sqlite3_column_count(stmt);
            }
        }

        public IEnumerator<IResultSetValue> GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IResultSetValue this[int index]
        {
            get
            {
                Contract.Requires(index >= 0);
                Contract.Requires(index < this.Count);

                return stmt.ResultSetValueAt(index);
            }
        }
    }

    internal sealed class BlobStream : Stream 
    {
        private readonly sqlite3_blob blob;
        private readonly bool canWrite;
        private long position = 0;

        internal BlobStream(sqlite3_blob blob, bool canWrite)
        {
            this.blob = blob;
            this.canWrite = canWrite;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return canWrite; }
        }

        public override long Length 
        { 
            get { return raw.sqlite3_blob_bytes(blob); } 
        }

        public override long Position 
        { 
            get { return this.position; }
            set { this.position = value; }
        }

        protected override void Dispose(bool disposing)
        {
            int rc = raw.sqlite3_blob_close(blob);
            SQLiteException.CheckOk(rc);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(offset >= 0);
            Contract.Requires(count >= 0);

            if (offset == 0)
            {
                int numBytes = (int)Math.Min(this.Length - this.Position, count);

                int rc = raw.sqlite3_blob_read(blob, buffer, (int)this.Position, numBytes);
                SQLiteException.CheckOk(rc);

                this.Position += numBytes;
                return numBytes;
            }
            else
            {
                // FIXME: No way to read from an offset in the buffer
                // https://github.com/ericsink/SQLitePCL.raw/issues/3

                byte[] newBuffer = new byte[count-offset];


                int numBytes = (int)Math.Min(this.Length - this.Position, count);
                int rc = raw.sqlite3_blob_read(blob, newBuffer, (int)this.Position, numBytes);
                SQLiteException.CheckOk(rc);

                System.Array.Copy(newBuffer, 0, buffer, offset, count);

                this.Position += numBytes;
                return numBytes;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(offset >= 0);
            Contract.Requires(count >= 0);

            if (this.Length - this.Position < count)
            {
                return;
            }

            if (offset == 0)
            {
                // FIXME: The IO.Stream spec doesn't specify this method to throw an exception if writing fails
                int rc = raw.sqlite3_blob_write(blob, buffer, count, (int)this.Position);
                SQLiteException.CheckOk(rc);
            }
            else
            {
                // FIXME: No way to read from an offset in the buffer
                // https://github.com/ericsink/SQLitePCL.raw/issues/3
                byte[] newBuffer = new byte[count - offset];
                System.Array.Copy(buffer, offset, newBuffer, 0, count);
                int rc = raw.sqlite3_blob_write(blob, newBuffer, count, (int)this.Position);
                SQLiteException.CheckOk(rc);
            }
        }
    }

    internal sealed class DelegatingEnumerable<T> : IEnumerable<T>
    {
        private readonly Func<IEnumerator<T>> deleg;

        internal DelegatingEnumerable(Func<IEnumerator<T>> deleg)
        {
            this.deleg = deleg;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return deleg();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
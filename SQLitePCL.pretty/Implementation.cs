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

namespace SQLitePCL.pretty
{
    internal sealed class DatabaseBackupImpl : IDatabaseBackup
    {
        private readonly sqlite3_backup backup;

        private bool disposed = false;

        internal DatabaseBackupImpl(sqlite3_backup backup)
        {
            this.backup = backup;
        }

        public int PageCount
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return raw.sqlite3_backup_pagecount(backup);
            }
        }

        public int RemainingPages
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return raw.sqlite3_backup_remaining(backup);
            }
        }

        public void Dispose()
        {
            disposed = true;
            backup.Dispose();
        }

        public bool Step(int nPages)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

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

    internal sealed class StatementImpl : IStatement
    {
        private readonly sqlite3_stmt stmt;
        private readonly IReadOnlyList<IResultSetValue> current;

        private bool disposed = false;

        internal StatementImpl(sqlite3_stmt stmt)
        {
            this.stmt = stmt;
            this.current = new ResultSetImpl(stmt);
        }

        public int BindParameterCount
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return raw.sqlite3_bind_parameter_count(stmt);
            }
        }

        public string SQL
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return raw.sqlite3_sql(stmt);
            }
        }

        public bool ReadOnly
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return raw.sqlite3_stmt_readonly(stmt) == 0 ? false : true;
            }
        }

        public bool Busy
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return raw.sqlite3_stmt_busy(stmt) == 0 ? false : true;
            }
        }

        public void Bind(int index, byte[] blob)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            int rc = raw.sqlite3_bind_blob(stmt, index + 1, blob);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void Bind(int index, double val)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            int rc = raw.sqlite3_bind_double(stmt, index + 1, val);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void Bind(int index, int val)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            int rc = raw.sqlite3_bind_int(stmt, index + 1, val);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void Bind(int index, long val)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            int rc = raw.sqlite3_bind_int64(stmt, index + 1, val);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void Bind(int index, string text)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            int rc = raw.sqlite3_bind_text(stmt, index + 1, text);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void BindNull(int index)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            int rc = raw.sqlite3_bind_null(stmt, index + 1);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void BindZeroBlob(int index, int size)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            int rc = raw.sqlite3_bind_zeroblob(stmt, index + 1, size);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void ClearBindings()
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            int rc = raw.sqlite3_clear_bindings(stmt);
            SQLiteException.CheckOk(stmt, rc);
        }

        public bool TryGetBindParameterIndex(string parameter, out int index)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            index = raw.sqlite3_bind_parameter_index(stmt, parameter) - 1;
            return index >= 0;
        }

        public string GetBindParameterName(int index)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            return raw.sqlite3_bind_parameter_name(stmt, index + 1);
        }

        public void Dispose()
        {
            disposed = true;
            stmt.Dispose();
        }

        public IReadOnlyList<IResultSetValue> Current
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return current;
            }
        }

        Object IEnumerator.Current
        {
            get
            {
                return this.Current;
            }
        }

        public bool MoveNext()
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

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
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

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
                if (index < 0 || index >= this.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return stmt.ResultSetValueAt(index);
            }
        }
    }

    internal sealed class BlobStream : Stream
    {
        private static void CheckOkOrThrowIOException(int rc)
        {
            try
            {
                SQLiteException.CheckOk(rc);
            }
            catch (SQLiteException e)
            {
                throw new IOException("Received SQLiteExcepction", e);
            }
        }

        private readonly sqlite3_blob blob;
        private readonly bool canWrite;

        private bool disposed = false;
        private long position = 0;

        internal BlobStream(sqlite3_blob blob, bool canWrite)
        {
            this.blob = blob;
            this.canWrite = canWrite;
        }

        public override bool CanRead
        {
            get 
            {
                return !disposed; 
            }
        }

        public override bool CanSeek
        {
            get 
            {
                return false; 
            }
        }

        public override bool CanWrite
        {
            get 
            {
                return !disposed && canWrite; 
            }
        }

        public override long Length
        {
            get 
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return raw.sqlite3_blob_bytes(blob); 
            }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        // http://msdn.microsoft.com/en-us/library/system.idisposable(v=vs.110).aspx
        protected override void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                // Free any other managed objects here. 
                blob.Dispose();
            }

            // Free any unmanaged objects here. 

            disposed = true;
            base.Dispose(disposing);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            if (buffer == null) { throw new ArgumentNullException(); }
            if (offset + count > buffer.Length) { new ArgumentException(); }
            if (offset < 0 ) { throw new ArgumentOutOfRangeException(); }
            if (count < 0 ) { throw new ArgumentOutOfRangeException(); }

            int numBytes = (int)Math.Min(this.Length - position, count);
            int rc = raw.sqlite3_blob_read(blob, buffer, offset, numBytes, (int)position);
            CheckOkOrThrowIOException(rc);

            position += numBytes;
            return numBytes;
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
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
            if (!canWrite) { throw new NotSupportedException(); }

            if (buffer == null) { throw new ArgumentNullException(); }
            if (offset + count > buffer.Length) { new ArgumentException(); }
            if (offset < 0 ) { throw new ArgumentOutOfRangeException(); }
            if (count < 0 ) { throw new ArgumentOutOfRangeException(); }

            int numBytes = (int)Math.Min(this.Length - position, count);

            int rc = raw.sqlite3_blob_write(blob, buffer, offset, numBytes, (int)position);

            CheckOkOrThrowIOException(rc);

            position += numBytes;
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

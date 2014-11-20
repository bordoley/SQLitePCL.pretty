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
using System.Linq;
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
        private readonly SQLiteDatabaseConnection db;
        private readonly IReadOnlyOrderedDictionary<string, IBindParameter> bindParameters;
        private readonly IReadOnlyList<IColumnInfo> columns;
        private readonly IReadOnlyList<IResultSetValue> current;

        private bool disposed = false;

        internal StatementImpl(sqlite3_stmt stmt, SQLiteDatabaseConnection db)
        {
            this.stmt = stmt;
            this.db = db;

            this.bindParameters = new BindParameterOrderedDictionaryImpl(this);
            this.columns = new ColumnsListImpl(this);
            this.current = new ResultSetImpl(this);
        }

        internal sqlite3_stmt sqlite3_stmt
        {
            get 
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return stmt;
            }
        }

        public IReadOnlyOrderedDictionary<string, IBindParameter> BindParameters
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return bindParameters;
            }
        }

        public IReadOnlyList<IColumnInfo> Columns 
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                
                return columns;
            }
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

        public string SQL
        {
            get
            {
                return raw.sqlite3_sql(this.sqlite3_stmt);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return raw.sqlite3_stmt_readonly(this.sqlite3_stmt) == 0 ? false : true;
            }
        }

        public bool IsBusy
        {
            get
            {
                return raw.sqlite3_stmt_busy(this.sqlite3_stmt) == 0 ? false : true;
            }
        }

        public void ClearBindings()
        {
            int rc = raw.sqlite3_clear_bindings(this.sqlite3_stmt);
            SQLiteException.CheckOk(this.sqlite3_stmt, rc);
        }

        public void Dispose()
        {
            db.RemoveStatement(this);
            disposed = true;
            stmt.Dispose();
        }

        public bool MoveNext()
        {
            int rc = raw.sqlite3_step(this.sqlite3_stmt);
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
                SQLiteException.CheckOk(this.sqlite3_stmt, rc);
                // Never gets returned
                return false;
            }
        }

        public void Reset()
        {
            int rc = raw.sqlite3_reset(this.sqlite3_stmt);
            SQLiteException.CheckOk(stmt, rc);
        }
    }

    internal sealed class BindParameterOrderedDictionaryImpl : IReadOnlyOrderedDictionary<string, IBindParameter>
    {
        private readonly StatementImpl stmt;

        internal BindParameterOrderedDictionaryImpl(StatementImpl stmt)
        {
            this.stmt = stmt;
        }

        private bool TryGetBindParameterIndex(string parameter, out int index)
        {
            index = raw.sqlite3_bind_parameter_index(stmt.sqlite3_stmt, parameter) - 1;
            return index >= 0;
        }

        public IBindParameter this[int index]
        {
            get 
            {
                if (index < 0 || index >= this.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return new BindParameterImpl(stmt.sqlite3_stmt, index);
            }
        }

        public IBindParameter this[string key]
        {
            get
            {
                IBindParameter value = null;
                if (this.TryGetValue(key, out value))
                {
                    return value;
                }

                throw new KeyNotFoundException();
            }
        }

        public int Count
        {
            get 
            {
                return raw.sqlite3_bind_parameter_count(stmt.sqlite3_stmt);
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                return this.Select(pair => pair.Key);
            }
        }

        public IEnumerable<IBindParameter> Values
        {
            get
            {
                return this.Select(pair => pair.Value);
            }
        }

        public bool ContainsKey(string key)
        {
            int i;
            return this.TryGetBindParameterIndex(key, out i);
        }

        public IEnumerator<KeyValuePair<string, IBindParameter>> GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
            {
                var next = this[i];
                yield return new KeyValuePair<string, IBindParameter>(next.Name, next);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public bool TryGetValue(string key, out IBindParameter value)
        {
            int i = 0;
            if (this.TryGetBindParameterIndex(key, out i))
            {
                value = this[i];
                return true;
            }

            value = null;
            return false;
        }
    }

    internal sealed class BindParameterImpl : IBindParameter
    {
        private readonly sqlite3_stmt stmt;
        private readonly int index;

        internal BindParameterImpl(sqlite3_stmt stmt, int index)
        {
            this.stmt = stmt;
            this.index = index;
        }

        public string Name
        {
            get 
            {
                return raw.sqlite3_bind_parameter_name(stmt, index + 1);
            }
        }

        public void Bind(byte[] blob)
        {
            int rc = raw.sqlite3_bind_blob(stmt, index + 1, blob);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void Bind(double val)
        {
            int rc = raw.sqlite3_bind_double(stmt, index + 1, val);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void Bind(int val)
        {
            int rc = raw.sqlite3_bind_int(stmt, index + 1, val);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void Bind(long val)
        {
            int rc = raw.sqlite3_bind_int64(stmt, index + 1, val);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void Bind(string text)
        {
            int rc = raw.sqlite3_bind_text(stmt, index + 1, text);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void BindNull()
        {
            int rc = raw.sqlite3_bind_null(stmt, index + 1);
            SQLiteException.CheckOk(stmt, rc);
        }

        public void BindZeroBlob(int size)
        {
            int rc = raw.sqlite3_bind_zeroblob(stmt, index + 1, size);
            SQLiteException.CheckOk(stmt, rc);
        }
    }

    internal sealed class ColumnsListImpl : IReadOnlyList<IColumnInfo>
    {
        private readonly StatementImpl stmt;

        internal ColumnsListImpl(StatementImpl stmt)
        {
            this.stmt = stmt;
        }

        public IColumnInfo this[int index]
        {
            get 
            { 
                if (index < 0 || index >= this.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return new ColumnInfoImpl(stmt, index);
            }
        }

        public int Count
        {
            get 
            {
                return raw.sqlite3_column_count(stmt.sqlite3_stmt);
            }
        }

        public IEnumerator<IColumnInfo> GetEnumerator()
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
    }

    internal sealed class ColumnInfoImpl : IColumnInfo
    {
        private readonly StatementImpl stmt;
        private readonly int index;

        internal ColumnInfoImpl(StatementImpl stmt, int index)
        {
            this.stmt = stmt;
            this.index = index;
        }

        public string Name
        {
            get
            {
                return raw.sqlite3_column_name(stmt.sqlite3_stmt, index);
            }
        }

        public string DatabaseName
        {
            get 
            {
                return raw.sqlite3_column_database_name(stmt.sqlite3_stmt, index);
            }
        }

        public string OriginName
        {
            get 
            {
                return raw.sqlite3_column_origin_name(stmt.sqlite3_stmt, index);
            }
        }

        public string TableName
        {
            get 
            {
                return raw.sqlite3_column_table_name(stmt.sqlite3_stmt, index);
            }
        }
    }

    internal sealed class ResultSetImpl : IReadOnlyList<IResultSetValue>
    {
        private readonly StatementImpl stmt;

        internal ResultSetImpl(StatementImpl stmt)
        {
            this.stmt = stmt;
        }

        public int Count
        {
            get
            {
                return raw.sqlite3_column_count(stmt.sqlite3_stmt);
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

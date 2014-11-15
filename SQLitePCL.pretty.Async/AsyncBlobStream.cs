/*
   Copyright 2014 David Bordoley

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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    internal sealed class AsyncBlobStream : Stream
    {
        private readonly Stream blobStream;
        private readonly IAsyncDatabaseConnection queue;
        private readonly long length;

        private bool disposed = false;

        internal AsyncBlobStream(Stream blobStream, IAsyncDatabaseConnection queue, long length)
        {
            this.blobStream = blobStream;
            this.queue = queue;
            this.length = length;
        }

        public override bool CanRead
        {
            get { return blobStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return blobStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return blobStream.CanWrite; }
        }

        public override long Length
        {
            get 
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                return length; 
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
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
                queue.Use(db =>
                    {
                        blobStream.Dispose();
                    }).Wait();
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

            var t = this.ReadAsync(buffer, offset, count);
            t.Wait();
            return t.Result;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            if (buffer == null) { throw new ArgumentNullException(); }
            if (offset + count > buffer.Length) { new ArgumentException(); }
            if (offset < 0) { throw new ArgumentOutOfRangeException(); }
            if (count < 0) { throw new ArgumentOutOfRangeException(); }

            return queue.Use(db =>
                {
                    return blobStream.Read(buffer, offset, count);
                }, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            this.WriteAsync(buffer, offset, count).Wait();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
            if (!this.CanWrite) { throw new NotSupportedException(); }

            if (buffer == null) { throw new ArgumentNullException(); }
            if (offset + count > buffer.Length) { new ArgumentException(); }
            if (offset < 0) { throw new ArgumentOutOfRangeException(); }
            if (count < 0) { throw new ArgumentOutOfRangeException(); }

            return queue.Use(db =>
                {
                    blobStream.Write(buffer, offset, count);
                }, cancellationToken);
        }
    }
}

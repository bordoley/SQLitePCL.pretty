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

using Xunit;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace SQLitePCL.pretty.tests
{
    public class AsyncBlobStreamTests
    {
        [Fact]
        public async Task TestDispose()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                await db.ExecuteAsync("CREATE TABLE foo (x blob);");
                await db.ExecuteAsync("INSERT INTO foo (x) VALUES(?);", "data");
                var blob =
                    await db.Query("SELECT rowid, x FROM foo")
                        .Select(row => db.OpenBlobAsync(row[1].ColumnInfo, row[0].ToInt64(), true))
                        .FirstAsync().ToTask().Unwrap();
                blob.Dispose();

                // Test double dispose doesn't crash
                blob.Dispose();

                Assert.Throws<ObjectDisposedException>(() => { var x = blob.Length; });
                Assert.Throws<ObjectDisposedException>(() => { var x = blob.Position; });
                Assert.Throws<ObjectDisposedException>(() => { blob.Position = 10; });
                Assert.Throws<ObjectDisposedException>(() => { blob.Read(new byte[10], 0, 2); });
                Assert.Throws<ObjectDisposedException>(() => { blob.Write(new byte[10], 0, 1); });
                Assert.Throws<ObjectDisposedException>(() => { blob.Seek(0, SeekOrigin.Begin); });
            }
        }

        [Fact]
        public async Task TestRead()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                byte[] bytes = new byte[1000];
                Random random = new Random();
                random.NextBytes(bytes);

                await db.ExecuteAsync("CREATE TABLE foo (x blob);");
                await db.ExecuteAsync("INSERT INTO foo (x) VALUES(?);", bytes);

                var stream =
                    await db.Query("SELECT rowid, x FROM foo;")
                        .Select(row => db.OpenBlobAsync(row[1].ColumnInfo, row[0].ToInt64()))
                        .FirstAsync().ToTask().Unwrap();

                using (stream)
                {
                    Assert.True(stream.CanRead);
                    Assert.False(stream.CanWrite);
                    Assert.True(stream.CanSeek);

                    for (int i = 0; i < stream.Length; i++)
                    {
                        byte[] bite = new byte[1];
                        await stream.ReadAsync(bite, 0, 1);
                        Assert.Equal(bytes[i], bite[0]);
                    }

                    // Since this is a read only stream, this is a good chance to test that writing fails
                    Assert.Throws<NotSupportedException>(() => stream.WriteByte(0));
                }
            }
        }

        [Fact]
        public async Task TestWrite()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                byte[] bytes = new byte[1000];
                Random random = new Random();
                random.NextBytes(bytes);

                var source = new MemoryStream(bytes);

                await db.ExecuteAsync("CREATE TABLE foo (x blob);");
                await db.ExecuteAsync("INSERT INTO foo (x) VALUES(?);", source);

                var stream =
                    await db.Query("SELECT rowid, x FROM foo")
                        .Select(row => db.OpenBlobAsync(row[1].ColumnInfo, row[0].ToInt64(), true))
                        .FirstAsync().ToTask().Unwrap();

                using (stream)
                {
                    Assert.True(stream.CanRead);
                    Assert.True(stream.CanWrite);
                    await source.CopyToAsync(stream);

                    stream.Position = 0;

                    for (int i = 0; i < stream.Length; i++)
                    {
                        byte[] bite = new byte[1];
                        await stream.ReadAsync(bite, 0, 1);
                        Assert.Equal(bytes[i], bite[0]);
                    }

                    // Test writing after the end of the stream
                    // Assert that nothing changes.
                    stream.Position = stream.Length;
                    await stream.WriteAsync(new byte[10], 0, 10);
                    stream.Position = 0;
                    for (int i = 0; i < stream.Length; i++)
                    {
                        byte[] bite = new byte[1];
                        await stream.ReadAsync(bite, 0, 1);
                        Assert.Equal(bytes[i], bite[0]);
                    }
                }
            }
        }

        [Fact]
        public async Task TestSeek()
        {
            using (var db = SQLiteDatabaseConnectionBuilder.InMemory.BuildAsyncDatabaseConnection())
            {
                await db.ExecuteAsync("CREATE TABLE foo (x blob);");
                await db.ExecuteAsync("INSERT INTO foo (x) VALUES(?);", "data");
                var blob =
                    await db.Query("SELECT rowid, x FROM foo")
                        .Select(row => db.OpenBlobAsync(row[1].ColumnInfo, row[0].ToInt64(), true))
                        .FirstAsync().ToTask().Unwrap();
                using (blob)
                {
                    Assert.True(blob.CanSeek);
                    Assert.Throws<NotSupportedException>(() => blob.SetLength(10));

                    // Assert does not throw
                    blob.Position = 100;

                    // Test input validation
                    blob.Position = 5;
                    Assert.Throws<IOException>(() => blob.Seek(-10, SeekOrigin.Begin));
                    Assert.Equal(blob.Position, 5);
                    Assert.Throws<IOException>(() => blob.Seek(-10, SeekOrigin.Current));
                    Assert.Equal(blob.Position, 5);
                    Assert.Throws<IOException>(() => blob.Seek(-100, SeekOrigin.End));
                    Assert.Equal(blob.Position, 5);
                    Assert.Throws<ArgumentException>(() => blob.Seek(-100, (SeekOrigin)10));
                    Assert.Equal(blob.Position, 5);

                    blob.Seek(0, SeekOrigin.Begin);
                    Assert.Equal(blob.Position, 0);

                    blob.Seek(0, SeekOrigin.End);
                    Assert.Equal(blob.Position, blob.Length);

                    blob.Position = 5;
                    blob.Seek(2, SeekOrigin.Current);
                    Assert.Equal(blob.Position, 7);
                }
            }
        }
    }
}

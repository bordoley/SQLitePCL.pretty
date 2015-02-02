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
    /// <summary>
    /// SQLite fundamental datatypes.
    /// </summary>
    /// <seealso href="https://sqlite.org/c3ref/c_blob.html"/>
    public enum SQLiteType
    {
        /// <summary>
        /// 64-bit signed integer
        /// </summary>
        Integer = raw.SQLITE_INTEGER,

        /// <summary>
        /// 64-bit IEEE floating point number
        /// </summary>
        Float = raw.SQLITE_FLOAT,

        /// <summary>
        /// String
        /// </summary>
        Text = raw.SQLITE_TEXT,

        /// <summary>
        /// Blob
        /// </summary>
        Blob = raw.SQLITE_BLOB,

        /// <summary>
        /// Null
        /// </summary>
        Null = raw.SQLITE_NULL
    }

    /// <summary>
    /// Flags For File Open Operations
    /// </summary>
    /// <seealso  href="https://sqlite.org/c3ref/c_open_autoproxy.html"/>
    [Flags]
    public enum ConnectionFlags
    {
        /// <summary>
        /// Opens the database as readonly.
        /// </summary>
        ReadOnly = raw.SQLITE_OPEN_READONLY,

        /// <summary>
        /// Open the database is for reading and writing if possible.
        /// </summary>
        ReadWrite = raw.SQLITE_OPEN_READWRITE,

        /// <summary>
        /// Creates the database if it does not already exist.
        /// </summary>
        Create = raw.SQLITE_OPEN_CREATE,

        /// <summary>
        /// Enables URI filename interpretation.
        /// </summary>
        Uri = raw.SQLITE_OPEN_URI,

        /// <summary>
        ///
        /// </summary>
        Memory = raw.SQLITE_OPEN_MEMORY,

        /// <summary>
        /// Opens the database connection in the multi-thread threading mode as
        /// long as the single-thread mode has not been set at compile-time or start-time.
        /// </summary>
        NoMutex = raw.SQLITE_OPEN_NOMUTEX,

        /// <summary>
        /// Opens the database connection in the serialized threading mode unless
        /// single-thread was previously selected at compile-time or start-time.
        /// </summary>
        FullMutex = raw.SQLITE_OPEN_FULLMUTEX,

        /// <summary>
        /// Causes the database connection to be eligible to use shared cache mode,
        /// regardless of whether or not shared cache is enabled.
        /// </summary>
        SharedCached = raw.SQLITE_OPEN_SHAREDCACHE,

        /// <summary>
        /// Causes the database connection to not participate in shared cache mode even if it is enabled.
        /// </summary>
        PrivateCache = raw.SQLITE_OPEN_PRIVATECACHE
    }

    /// <summary>
    /// SQLite Action Codes
    /// </summary>
    /// <seealso href="https://sqlite.org/c3ref/c_alter_table.html"/>
    public enum ActionCode
    {
        /// <summary>
        ///
        /// </summary>
        CreateIndex = raw.SQLITE_CREATE_INDEX,

        /// <summary>
        ///
        /// </summary>
        CreateTable = raw.SQLITE_CREATE_TABLE,

        /// <summary>
        ///
        /// </summary>
        CreateTempIndex = raw.SQLITE_CREATE_TEMP_INDEX,

        /// <summary>
        ///
        /// </summary>
        CreateTempTable = raw.SQLITE_CREATE_TEMP_TABLE,

        /// <summary>
        ///
        /// </summary>
        CreateTempTrigger = raw.SQLITE_CREATE_TEMP_TRIGGER,

        /// <summary>
        ///
        /// </summary>
        CreateTempView = raw.SQLITE_CREATE_TEMP_VIEW,

        /// <summary>
        ///
        /// </summary>
        CreateTrigger = raw.SQLITE_CREATE_TRIGGER,

        /// <summary>
        ///
        /// </summary>
        CreateView = raw.SQLITE_CREATE_VIEW,

        /// <summary>
        ///
        /// </summary>
        Delete = raw.SQLITE_DELETE,

        /// <summary>
        ///
        /// </summary>
        DropIndex = raw.SQLITE_DROP_INDEX,

        /// <summary>
        ///
        /// </summary>
        DropTable = raw.SQLITE_DROP_TABLE,

        /// <summary>
        ///
        /// </summary>
        DropTempIndex = raw.SQLITE_DROP_TEMP_INDEX,

        /// <summary>
        ///
        /// </summary>
        DropTempTable = raw.SQLITE_DROP_TEMP_TABLE,

        /// <summary>
        ///
        /// </summary>
        DropTempTrigger = raw.SQLITE_DROP_TEMP_TRIGGER,

        /// <summary>
        ///
        /// </summary>
        DropTempView = raw.SQLITE_DROP_TEMP_VIEW,

        /// <summary>
        ///
        /// </summary>
        DropTrigger = raw.SQLITE_DROP_TRIGGER,

        /// <summary>
        ///
        /// </summary>
        DropView = raw.SQLITE_DROP_VIEW,

        /// <summary>
        ///
        /// </summary>
        Insert = raw.SQLITE_INSERT,

        /// <summary>
        ///
        /// </summary>
        Pragma = raw.SQLITE_PRAGMA,

        /// <summary>
        ///
        /// </summary>
        Read = raw.SQLITE_READ,

        /// <summary>
        ///
        /// </summary>
        Select = raw.SQLITE_SELECT,

        /// <summary>
        ///
        /// </summary>
        Transaction = raw.SQLITE_TRANSACTION,

        /// <summary>
        ///
        /// </summary>
        Update = raw.SQLITE_UPDATE,

        /// <summary>
        ///
        /// </summary>
        Attach = raw.SQLITE_ATTACH,

        /// <summary>
        ///
        /// </summary>
        Detach = raw.SQLITE_DETACH,

        /// <summary>
        ///
        /// </summary>
        AlterTable = raw.SQLITE_ALTER_TABLE,

        /// <summary>
        ///
        /// </summary>
        ReIndex = raw.SQLITE_REINDEX,

        /// <summary>
        ///
        /// </summary>
        Analyze = raw.SQLITE_ANALYZE,

        /// <summary>
        ///
        /// </summary>
        CreateVTable = raw.SQLITE_CREATE_VTABLE,

        /// <summary>
        ///
        /// </summary>
        DropVTable = raw.SQLITE_DROP_VTABLE,

        /// <summary>
        ///
        /// </summary>
        Function = raw.SQLITE_FUNCTION,

        /// <summary>
        ///
        /// </summary>
        SavePoint = raw.SQLITE_SAVEPOINT,

        /// <summary>
        ///
        /// </summary>
        Copy = raw.SQLITE_COPY,

        /// <summary>
        ///
        /// </summary>
        Recursive = raw.SQLITE_RECURSIVE
    }

    /// <summary>
    /// SQLite Result and Error Codes
    /// </summary>
    /// <seealso href="https://sqlite.org/rescode.html"/>
    public enum ErrorCode
    {
        /// <summary>
        /// The operation was successful and that there were no errors. Most other result codes indicate an error.
        /// </summary>
        Ok = raw.SQLITE_OK,

        /// <summary>
        /// Generic error code that is used when no other more specific error code is available.
        /// </summary>
        Error = raw.SQLITE_ERROR,

        /// <summary>
        /// Indicates an internal malfunction.
        /// </summary>
        Internal = raw.SQLITE_INTERNAL,

        /// <summary>
        /// Indicates that the requested access mode for a newly created database could not be provided.
        /// </summary>
        Perm = raw.SQLITE_PERM,

        /// <summary>
        /// Indicates that an operation was aborted prior to completion, usually be application request.
        /// </summary>
        Abort = raw.SQLITE_ABORT,

        /// <summary>
        /// Indicates that the database file could not be written (or in some cases read) because of
        /// concurrent activity by some other database connection, usually a database connection in a separate process.
        /// </summary>
        Busy = raw.SQLITE_BUSY,

        /// <summary>
        /// Indicates that a write operation could not continue because of a conflict within the
        /// same database connection or a conflict with a different database connection that uses a shared cache.
        /// </summary>
        Locked = raw.SQLITE_LOCKED,

        /// <summary>
        /// Indicates that SQLite was unable to allocate all the memory it needed to complete the operation.
        /// </summary>
        NoMemory = raw.SQLITE_NOMEM,

        /// <summary>
        /// An attempt was made to alter some data for which the current database connection does not have write permission.
        /// </summary>
        ReadOnly = raw.SQLITE_READONLY,

        /// <summary>
        /// Indicates that an operation was interrupted.
        /// </summary>
        Interrupt = raw.SQLITE_INTERRUPT,

        /// <summary>
        /// The operation could not finish because the operating system reported an I/O error.
        /// </summary>
        IOError = raw.SQLITE_IOERR,

        /// <summary>
        /// Indicates that the database file has been corrupted.
        /// </summary>
        Corrupt = raw.SQLITE_CORRUPT,

        /// <summary>
        ///
        /// </summary>
        NotFound = raw.SQLITE_NOTFOUND,

        /// <summary>
        /// Indicates that a write could not complete because the disk is full.
        /// </summary>
        Full = raw.SQLITE_FULL,

        /// <summary>
        /// Indicates that SQLite was unable to open a file.
        /// </summary>
        CannotOpen = raw.SQLITE_CANTOPEN,

        /// <summary>
        /// Indicates a problem with the file locking protocol used by SQLite.
        /// </summary>
        Protocol = raw.SQLITE_PROTOCOL,

        /// <summary>
        /// Result code is not currently used.
        /// </summary>
        Empty = raw.SQLITE_EMPTY,

        /// <summary>
        /// Indicates that the database schema has changed.
        /// </summary>
        Schema = raw.SQLITE_SCHEMA,

        /// <summary>
        /// Indicates that a string or BLOB was too large.
        /// </summary>
        TooBig = raw.SQLITE_TOOBIG,

        /// <summary>
        /// Indicates that an SQL constraint violation occurred while trying to process an SQL statement.
        /// </summary>
        Constraint = raw.SQLITE_CONSTRAINT,

        /// <summary>
        /// Indicates a datatype mismatch.
        /// </summary>
        Mismatch = raw.SQLITE_MISMATCH,

        /// <summary>
        /// Returned if the application uses any SQLite interface in a way that is undefined or unsupported.
        /// </summary>
        Misuse = raw.SQLITE_MISUSE,

        /// <summary>
        /// Returned on systems that do not support large files when the database grows to be
        /// larger than what the filesystem can handle.
        /// </summary>
        NoLFS = raw.SQLITE_NOLFS,

        /// <summary>
        /// Returned when the authorizer callback indicates that an SQL statement being prepared is not authorized.
        /// </summary>
        NotAuthorized = raw.SQLITE_AUTH,

        /// <summary>
        /// Error code is not currently used by SQLite.
        /// </summary>
        Format = raw.SQLITE_FORMAT,

        /// <summary>
        /// Indicates that the parameter number argument to one of the sqlite3_bind routines is out of range.
        /// </summary>
        Range = raw.SQLITE_RANGE,

        /// <summary>
        /// Indicates that the file being opened does not appear to be an SQLite database file.
        /// </summary>
        NotDatabase = raw.SQLITE_NOTADB,

        /// <summary>
        ///
        /// </summary>
        Notice = raw.SQLITE_NOTICE,

        /// <summary>
        ///
        /// </summary>
        Warning = raw.SQLITE_WARNING,

        /// <summary>
        /// Indicates that another row of output is available.
        /// </summary>
        Row = raw.SQLITE_ROW,

        /// <summary>
        /// Indicates that an operation has completed.
        /// </summary>
        Done = raw.SQLITE_DONE,

        /// <summary>
        /// Indicates an I/O error in the VFS layer while trying to read from a file on disk.
        /// </summary>
        IOErrorRead = raw.SQLITE_IOERR_READ,

        /// <summary>
        /// Indicates that a read attempt in the VFS layer was unable to obtain as many bytes as was requested.
        /// </summary>
        IOErrorShortRead = raw.SQLITE_IOERR_SHORT_READ,

        /// <summary>
        /// Indicates an I/O error in the VFS layer while trying to write into a file on disk.
        /// </summary>
        IOErrorWrite = raw.SQLITE_IOERR_WRITE,

        /// <summary>
        /// Indicates an I/O error in the VFS layer while trying to flush previously
        /// written content out of OS and/or disk-control buffers and into persistent storage.
        /// </summary>
        IOErrorFSync = raw.SQLITE_IOERR_FSYNC,

        /// <summary>
        /// Indicates an I/O error in the VFS layer while trying to invoke fsync() on a directory.
        /// </summary>
        IOErrorDirFSync = raw.SQLITE_IOERR_DIR_FSYNC,

        /// <summary>
        /// Indicates an I/O error in the VFS layer while trying to truncate a file to a smaller size.
        /// </summary>
        IOErrorTruncate = raw.SQLITE_IOERR_TRUNCATE,

        /// <summary>
        /// Indicates an I/O error in the VFS layer while trying to invoke fstat()
        /// (or the equivalent) on a file in order to determine information
        /// such as the file size or access permissions.
        /// </summary>
        IOErrorFStat = raw.SQLITE_IOERR_FSTAT,

        /// <summary>
        /// Indicates an I/O error within xUnlock method on the sqlite3_io_methods object.
        /// </summary>
        IOErrorUnlock = raw.SQLITE_IOERR_UNLOCK,

        /// <summary>
        /// Indicates an I/O error within xLock method on the sqlite3_io_methods object while trying to obtain a read lock.
        /// </summary>
        IOErrorReadLock = raw.SQLITE_IOERR_RDLOCK,

        /// <summary>
        /// Indicates an I/O error within xDelete method on the sqlite3_vfs object.
        /// </summary>
        IOErrorDelete = raw.SQLITE_IOERR_DELETE,

        /// <summary>
        /// Error code is no longer used.
        /// </summary>
        IOErrorBlocked = raw.SQLITE_IOERR_BLOCKED,

        /// <summary>
        /// Indicates that an operation could not be completed due to the inability to allocate sufficient memory.
        /// </summary>
        IOErrorNoMemory = raw.SQLITE_IOERR_NOMEM,

        /// <summary>
        /// iIndicates an I/O error within the xAccess method on the sqlite3_vfs object.
        /// </summary>
        IOErrorAccess = raw.SQLITE_IOERR_ACCESS,

        /// <summary>
        /// Indicates an I/O error within the xCheckReservedLock method on the sqlite3_io_methods object.
        /// </summary>
        IOErrorCheckReservedLock = raw.SQLITE_IOERR_CHECKRESERVEDLOCK,

        /// <summary>
        /// Indicates an I/O error in the advisory file locking logic.
        /// </summary>
        IOErrorLock = raw.SQLITE_IOERR_LOCK,

        /// <summary>
        /// Indicates an I/O error within the xClose method on the sqlite3_io_methods object.
        /// </summary>
        IOErrorClose = raw.SQLITE_IOERR_CLOSE,

        /// <summary>
        /// Error code is no longer used.
        /// </summary>
        IOErrorDirClose = raw.SQLITE_IOERR_DIR_CLOSE,

        /// <summary>
        /// Indicates an I/O error within the xShmMap method on the sqlite3_io_methods
        /// object while trying to open a new shared memory segment.
        /// </summary>
        IOErrorShmOpen = raw.SQLITE_IOERR_SHMOPEN,

        /// <summary>
        /// Indicates an I/O error within the xShmMap method on the sqlite3_io_methods
        /// object while trying to resize an existing shared memory segment.
        /// </summary>
        IOErrorShmSize = raw.SQLITE_IOERR_SHMSIZE,

        /// <summary>
        /// Error code is no longer used.
        /// </summary>
        IOErrorShmLock = raw.SQLITE_IOERR_SHMLOCK,

        /// <summary>
        /// Indicating an I/O error within the xShmMap method on the sqlite3_io_methods object while trying to map a shared memory segment into the process address space.
        /// </summary>
        IOErrorShmMap = raw.SQLITE_IOERR_SHMMAP,

        /// <summary>
        /// Indicates an I/O error within the xRead or xWrite methods on the sqlite3_io_methods
        /// object while trying to seek a file descriptor to the beginning point of the file
        /// where the read or write is to occur.
        /// </summary>
        IOErrorSeek = raw.SQLITE_IOERR_SEEK,

        /// <summary>
        /// Indicates that the xDelete method on the sqlite3_vfs object
        /// failed because the file being deleted does not exist.
        /// </summary>
        IOErrorDeleteNoEnt = raw.SQLITE_IOERR_DELETE_NOENT,

        /// <summary>
        /// Indicates an I/O error within the xFetch or xUnfetch methods on the
        /// sqlite3_io_methods object while trying to map or unmap part of the
        /// database file into the process address space.
        /// </summary>
        IOErrorMMap = raw.SQLITE_IOERR_MMAP,

        /// <summary>
        /// Indicates that the VFS is unable to determine a suitable
        /// directory in which to place temporary files.
        /// </summary>
        IOErrorGetTempPath = raw.SQLITE_IOERR_GETTEMPPATH,

        /// <summary>
        /// Used only by Cygwin VFS to indicate that the cygwin_conv_path()
        /// system call failed.
        /// </summary>
        IOErrorConvPath = raw.SQLITE_IOERR_CONVPATH,

        /// <summary>
        /// Indicates that a locking conflict has occurred due to contention with a different
        /// database connection that happens to hold a shared cache with the database connection
        /// to which the error was returned.
        /// </summary>
        LockSharedCache = raw.SQLITE_LOCKED_SHAREDCACHE,

        /// <summary>
        ///
        /// </summary>
        BusyRecovery = raw.SQLITE_BUSY_RECOVERY,

        /// <summary>
        /// Occurs on WAL mode databases when a database connection tries to promote a read
        /// transaction into a write transaction but finds that another database connection has
        /// already written to the database and thus invalidated prior reads.
        /// </summary>
        BusySnapShot = raw.SQLITE_BUSY_SNAPSHOT,

        /// <summary>
        /// Error code is no longer used.
        /// </summary>
        CannotOpenNoTempDirectory = raw.SQLITE_CANTOPEN_NOTEMPDIR,

        /// <summary>
        /// Indicates that a file open operation failed because the file is really a directory.
        /// </summary>
        CannotOpenIsDirectory = raw.SQLITE_CANTOPEN_ISDIR,

        /// <summary>
        /// Indicates that a file open operation failed because the operating
        /// system was unable to convert the filename into a full pathname.
        /// </summary>
        CannotOpenFullPath = raw.SQLITE_CANTOPEN_FULLPATH,

        /// <summary>
        /// Used only by Cygwin VFS and indicating that the cygwin_conv_path()
        /// system call failed while trying to open a file.
        /// </summary>
        CannotOpenConvPath = raw.SQLITE_CANTOPEN_CONVPATH,

        /// <summary>
        /// Indicates that content in the virtual table is corrupt.
        /// </summary>
        CorruptVTab = raw.SQLITE_CORRUPT_VTAB,

        /// <summary>
        /// Indicates that a WAL mode database cannot be opened because the database
        /// file needs to be recovered and recovery requires write access but only
        /// read access is available.
        /// </summary>
        ReadonlyRecovery = raw.SQLITE_READONLY_RECOVERY,

        /// <summary>
        /// Indicates that SQLite is unable to obtain a read lock on a WAL mode
        /// database because the shared-memory file associated with that database is read-only.
        /// </summary>
        ReadonlyCannotLock = raw.SQLITE_READONLY_CANTLOCK,

        /// <summary>
        /// Indicates that a database cannot be opened because it has a hot journal
        /// that needs to be rolled back but cannot because the database is readonly.
        /// </summary>
        ReadonlyRollback = raw.SQLITE_READONLY_ROLLBACK,

        /// <summary>
        /// Indicates that a database cannot be modified because the database file
        /// has been moved since it was opened, and so any attempt to modify the database
        /// might result in database corruption if the processes crashes because the
        /// rollback journal would not be correctly named.
        /// </summary>
        ReadonlyDatabaseMoved = raw.SQLITE_READONLY_DBMOVED,

        /// <summary>
        /// Indicates that an SQL statement aborted because the transaction that
        /// was active when the SQL statement first started was rolled back.
        /// </summary>
        AbortRollback = raw.SQLITE_ABORT_ROLLBACK,

        /// <summary>
        /// Indicates that a CHECK constraint failed
        /// </summary>
        ConstraintCheck = raw.SQLITE_CONSTRAINT_CHECK,

        /// <summary>
        /// Indicates that a commit hook callback returned true, thus
        /// causing the SQL statement to be rolled back.
        /// </summary>
        ConstraintCommitHook = raw.SQLITE_CONSTRAINT_COMMITHOOK,

        /// <summary>
        /// Indicates that a foreign key constraint failed.
        /// </summary>
        ConstraintForeignKey = raw.SQLITE_CONSTRAINT_FOREIGNKEY,

        /// <summary>
        /// Not currently used by the SQLite core.
        /// </summary>
        ConstraintFunction = raw.SQLITE_CONSTRAINT_FUNCTION,

        /// <summary>
        /// Indicates that a NOT NULL constraint failed.
        /// </summary>
        ConstraintNotNull = raw.SQLITE_CONSTRAINT_NOTNULL,

        /// <summary>
        /// Indicates that a PRIMARY KEY constraint failed.
        /// </summary>
        ConstraintPrimaryKey = raw.SQLITE_CONSTRAINT_PRIMARYKEY,

        /// <summary>
        /// Indicates that a RAISE function within a trigger fired, causing the SQL statement to abort
        /// </summary>
        ConstraintTrigger = raw.SQLITE_CONSTRAINT_TRIGGER,

        /// <summary>
        /// Indicates that a UNIQUE constraint failed.
        /// </summary>
        ConstraintUnique = raw.SQLITE_CONSTRAINT_UNIQUE,

        /// <summary>
        /// Not currently used by the SQLite core.
        /// </summary>
        ConstraintVTab = raw.SQLITE_CONSTRAINT_VTAB,

        /// <summary>
        /// Indicates that a rowid is not unique.
        /// </summary>
        ConstraintRowId = raw.SQLITE_CONSTRAINT_ROWID,

        /// <summary>
        ///
        /// </summary>
        NoticeRecoverWal = raw.SQLITE_NOTICE_RECOVER_WAL,

        /// <summary>
        ///
        /// </summary>
        NoticeRecoverRollback = raw.SQLITE_NOTICE_RECOVER_ROLLBACK,

        /// <summary>
        ///
        /// </summary>
        WarningAutoIndex = raw.SQLITE_WARNING_AUTOINDEX
    }

    /// <summary>
    /// WAL checkpoint modes.
    /// </summary>
    public enum WalCheckPointMode
    {
        /// <summary>
        /// Checkpoint as many frames as possible without waiting for any database
        /// readers or writers to finish. Sync the db file if all frames in the
        /// log are checkpointed.
        /// </summary>
        Passive = raw.SQLITE_CHECKPOINT_PASSIVE,

        /// <summary>
        /// This mode blocks until there is no database writer and all readers are
        /// reading from the most recent database snapshot. It then checkpoints all
        /// frames in the log file and syncs the database file. This call blocks database
        /// writers while it is running, but not database readers.
        /// </summary>
        Full = raw.SQLITE_CHECKPOINT_FULL,

        /// <summary>
        /// This mode works the same way as <see cref="Full"/>, except after checkpointing
        /// the log file it blocks until all readers are reading from the database file
        /// only. This ensures that the next client to write to the database file restarts
        /// the log file from the beginning. This call blocks database writers while it
        /// is running, but not database readers.
        /// </summary>
        Restart = raw.SQLITE_CHECKPOINT_RESTART,
    }

    /// <summary>
    /// These constants designate various run-time status parameters that can be 
    /// returned by <see cref="SQLite3.Status"/>.
    /// </summary>
    /// <seealso href="https://www.sqlite.org/c3ref/c_status_malloc_count.html"/>
    public enum SQLiteStatusCode
    {
        /// <summary>
        /// This parameter is the current amount of memory checked out using sqlite3_malloc(), 
        /// either directly or indirectly. The figure includes calls made to sqlite3_malloc() 
        /// by the application and internal memory usage by the SQLite library. Scratch 
        /// memory controlled by SQLITE_CONFIG_SCRATCH and auxiliary page-cache memory 
        /// controlled by SQLITE_CONFIG_PAGECACHE is not included in this parameter. 
        /// The amount returned is the sum of the allocation sizes as reported by the xSize 
        /// method in sqlite3_mem_methods.
        /// </summary>
        MemoryUsed = raw.SQLITE_STATUS_MEMORY_USED,

        /// <summary>
        /// This parameter returns the number of pages used out of the pagecache memory allocator
        /// that was configured using SQLITE_CONFIG_PAGECACHE. The value returned is in pages, 
        /// not in bytes.
        /// </summary>
        PageCacheUsed = raw.SQLITE_STATUS_PAGECACHE_USED,

        /// <summary>
        /// This parameter returns the number of bytes of page cache allocation which could 
        /// not be satisfied by the SQLITE_CONFIG_PAGECACHE buffer and where forced to overflow 
        /// to sqlite3_malloc(). The returned value includes allocations that overflowed 
        /// because they where too large (they were larger than the "sz" parameter to 
        /// SQLITE_CONFIG_PAGECACHE) and allocations that overflowed because no space was
        /// left in the page cache.
        /// </summary>
        PageCacheOverflow = raw.SQLITE_STATUS_PAGECACHE_OVERFLOW,

        /// <summary>
        /// This parameter returns the number of allocations used out of the scratch memory 
        /// allocator configured using SQLITE_CONFIG_SCRATCH. The value returned is in 
        /// allocations, not in bytes. Since a single thread may only have one scratch 
        /// allocation outstanding at time, this parameter also reports the number of threads 
        /// using scratch memory at the same time.
        /// </summary>
        ScratchUsed = raw.SQLITE_STATUS_SCRATCH_USED,

        /// <summary>
        /// This parameter returns the number of bytes of scratch memory allocation which 
        /// could not be satisfied by the SQLITE_CONFIG_SCRATCH buffer and where forced 
        /// to overflow to sqlite3_malloc(). The values returned include overflows because 
        /// the requested allocation was too larger (that is, because the requested allocation 
        /// was larger than the "sz" parameter to SQLITE_CONFIG_SCRATCH) and because no 
        /// scratch buffer slots were available.
        /// </summary>
        ScratchOverflow = raw.SQLITE_STATUS_SCRATCH_OVERFLOW,

        /// <summary>
        /// This parameter records the largest memory allocation request handed to 
        /// sqlite3_malloc() or sqlite3_realloc() (or their internal equivalents). 
        /// Only the value returned in the *pHighwater parameter to sqlite3_status() is 
        /// of interest. The value written into the *pCurrent parameter is undefined.
        /// </summary>
        MallocSize = raw.SQLITE_STATUS_MALLOC_SIZE,

        /// <summary>
        /// This parameter records the deepest parser stack. It is only meaningful if 
        /// SQLite is compiled with YYTRACKMAXSTACKDEPTH.
        /// </summary>
        ParserStack = raw.SQLITE_STATUS_PARSER_STACK,

        /// <summary>
        /// This parameter records the largest memory allocation request handed to 
        /// pagecache memory allocator. Only the value returned in the *pHighwater 
        /// parameter to sqlite3_status() is of interest. The value written into the 
        /// *pCurrent parameter is undefined.
        /// </summary>
        PageCacheSize = raw.SQLITE_STATUS_PAGECACHE_SIZE,

        /// <summary>
        /// This parameter records the largest memory allocation request handed to 
        /// scratch memory allocator. Only the value returned in the *pHighwater 
        /// parameter to sqlite3_status() is of interest. The value written into 
        /// the *pCurrent parameter is undefined.
        /// </summary>
        ScratchSize = raw.SQLITE_STATUS_SCRATCH_SIZE,

        /// <summary>
        /// This parameter records the number of separate memory allocations 
        /// currently checked out.
        /// </summary>
        MallocCount = raw.SQLITE_STATUS_MALLOC_COUNT,
    }

    /// <summary>
    /// These constants designate various run-time status parameters that can be 
    /// returned by <see cref="IDatabaseConnection.Status"/>.
    /// </summary>
    /// <seealso href="https://www.sqlite.org/c3ref/c_dbstatus_options.html"/>
    public enum DatabaseConnectionStatusCode
    {
        /// <summary>
        /// This parameter returns the number of lookaside memory slots currently checked out.
        /// </summary>
        LookAsideUsed = raw.SQLITE_DBSTATUS_LOOKASIDE_USED,

        /// <summary>
        /// This parameter returns the approximate number of bytes of heap memory used by 
        /// all pager caches associated with the database connection. The highwater mark 
        /// associated with SQLITE_DBSTATUS_CACHE_USED is always 0.
        /// </summary>
        CacheUsed = raw.SQLITE_DBSTATUS_CACHE_USED,

        /// <summary>
        /// This parameter returns the approximate number of bytes of heap memory used 
        /// to store the schema for all databases associated with the connection - main, 
        /// temp, and any ATTACH-ed databases. The full amount of memory used by the 
        /// schemas is reported, even if the schema memory is shared with other database 
        /// connections due to shared cache mode being enabled. The highwater mark associated 
        /// with SQLITE_DBSTATUS_SCHEMA_USED is always 0.
        /// </summary>
        SchemaUsed = raw.SQLITE_DBSTATUS_SCHEMA_USED,

        /// <summary>
        /// This parameter returns the approximate number of bytes of heap and lookaside 
        /// memory used by all prepared statements associated with the database connection. 
        /// The highwater mark associated with SQLITE_DBSTATUS_STMT_USED is always 0.
        /// </summary>
        StatementUsed = raw.SQLITE_DBSTATUS_STMT_USED,

        /// <summary>
        /// This parameter returns the number malloc attempts that were satisfied using 
        /// lookaside memory. Only the high-water value is meaningful; the current value is always zero.
        /// </summary>
        LookAsideHit = raw.SQLITE_DBSTATUS_LOOKASIDE_HIT,

        /// <summary>
        /// This parameter returns the number malloc attempts that might have been satisfied 
        /// using lookaside memory but failed due to the amount of memory requested being larger 
        /// than the lookaside slot size. Only the high-water value is meaningful; the current 
        /// value is always zero.
        /// </summary>
        LookAsideMissSize = raw.SQLITE_DBSTATUS_LOOKASIDE_MISS_SIZE,

        /// <summary>
        /// This parameter returns the number malloc attempts that might have been satisfied 
        /// using lookaside memory but failed due to all lookaside memory already being in use. 
        /// Only the high-water value is meaningful; the current value is always zero.
        /// </summary>
        LookAsideMissFull = raw.SQLITE_DBSTATUS_LOOKASIDE_MISS_FULL,

        /// <summary>
        /// This parameter returns the number of pager cache misses that have occurred. 
        /// The highwater mark associated with SQLITE_DBSTATUS_CACHE_MISS is always 0.
        /// </summary>
        CacheMiss = raw.SQLITE_DBSTATUS_CACHE_MISS,

        /// <summary>
        /// This parameter returns the number of dirty cache entries that have been written 
        /// to disk. Specifically, the number of pages written to the wal file in wal mode 
        /// databases, or the number of pages written to the database file in rollback mode 
        /// databases. Any pages written as part of transaction rollback or database recovery 
        /// operations are not included. If an IO or other error occurs while writing a page 
        /// to disk, the effect on subsequent SQLITE_DBSTATUS_CACHE_WRITE requests is undefined. 
        /// The highwater mark associated with SQLITE_DBSTATUS_CACHE_WRITE is always 0.
        /// </summary>
        CacheWrite = raw.SQLITE_DBSTATUS_CACHE_WRITE,

        /// <summary>
        /// This parameter returns zero for the current value if and only if all foreign key 
        /// constraints (deferred or immediate) have been resolved. The highwater mark is always 0.
        /// </summary>
        DeferedFKS = raw.SQLITE_DBSTATUS_DEFERRED_FKS
    }

    /// <summary>
    /// Integer codes that name counter values associated with the <see cref="IStatement.Status"/> interface. 
    /// </summary>
    /// <seealso href="https://www.sqlite.org/c3ref/c_stmtstatus_counter.html"/>
    public enum StatementStatusCode
    {
        /// <summary>
        /// This is the number of times that SQLite has stepped forward in a table as part 
        /// of a full table scan. Large numbers for this counter may indicate opportunities 
        /// for performance improvement through careful use of indices.
        /// </summary>
        FullscanStep = raw.SQLITE_STMTSTATUS_FULLSCAN_STEP,
        
        /// <summary>
        /// This is the number of sort operations that have occurred. A non-zero value 
        /// in this counter may indicate an opportunity to improvement performance through 
        /// careful use of indices.
        /// </summary>
        Sort = raw.SQLITE_STMTSTATUS_SORT,
        
        /// <summary>
        /// This is the number of rows inserted into transient indices that were created 
        /// automatically in order to help joins run faster. A non-zero value in this 
        /// counter may indicate an opportunity to improvement performance by adding 
        /// permanent indices that do not need to be reinitialized each time the statement 
        /// is run.
        /// </summary>
        AutoIndex = raw.SQLITE_STMTSTATUS_AUTOINDEX,
        
        /// <summary>
        /// This is the number of virtual machine operations executed by the prepared statement 
        /// if that number is less than or equal to 2147483647. The number of virtual machine 
        /// operations can be used as a proxy for the total work done by the prepared statement. 
        /// If the number of virtual machine operations exceeds 2147483647 then the value returned
        /// by this statement status code is undefined.
        /// </summary>
        VirtualMachineStep = raw.SQLITE_STMTSTATUS_VM_STEP
    }
}

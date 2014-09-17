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
    public enum SQLiteType
    {
        Integer = raw.SQLITE_INTEGER,
        Float = raw.SQLITE_FLOAT,
        Text = raw.SQLITE_TEXT,
        Blob = raw.SQLITE_BLOB,
        Null = raw.SQLITE_NULL
    }

    [Flags]
    public enum ConnectionFlags
    {
        // Only includes flags suitable for sqlite3_open_v2
        ReadOnly = raw.SQLITE_OPEN_READONLY,
        ReadWrite = raw.SQLITE_OPEN_READWRITE,
        Create = raw.SQLITE_OPEN_CREATE,
        Uri = raw.SQLITE_OPEN_URI,
        Memory = raw.SQLITE_OPEN_MEMORY,
        NoMutex = raw.SQLITE_OPEN_NOMUTEX,
        FullMutex = raw.SQLITE_OPEN_FULLMUTEX,
        SharedCached = raw.SQLITE_OPEN_SHAREDCACHE,
        PrivateCache = raw.SQLITE_OPEN_PRIVATECACHE
    }

    // Added to support IDatabaseConnection.Update
    public enum ActionCode
    {
        CreateIndex = raw.SQLITE_CREATE_INDEX,
        CreateTable = raw.SQLITE_CREATE_TABLE,
        CreateTempIndex = raw.SQLITE_CREATE_TEMP_INDEX,
        CreateTempTable = raw.SQLITE_CREATE_TEMP_TABLE,
        CreateTempTrigger = raw.SQLITE_CREATE_TEMP_TRIGGER,
        CreateTempView = raw.SQLITE_CREATE_TEMP_VIEW,
        CreateTrigger = raw.SQLITE_CREATE_TRIGGER,
        CreateView = raw.SQLITE_CREATE_VIEW,
        Delete = raw.SQLITE_DELETE,
        DropIndex = raw.SQLITE_DROP_INDEX,
        DropTable = raw.SQLITE_DROP_TABLE,
        DropTempIndex = raw.SQLITE_DROP_TEMP_INDEX,
        DropTempTable = raw.SQLITE_DROP_TEMP_TABLE,
        DropTempTrigger = raw.SQLITE_DROP_TEMP_TRIGGER,
        DropTempView = raw.SQLITE_DROP_TEMP_VIEW,
        DropTrigger = raw.SQLITE_DROP_TRIGGER,
        DropView = raw.SQLITE_DROP_VIEW,
        Insert = raw.SQLITE_INSERT,
        Pragma = raw.SQLITE_PRAGMA,
        Read = raw.SQLITE_READ,
        Select = raw.SQLITE_SELECT,
        Transaction = raw.SQLITE_TRANSACTION,
        Update = raw.SQLITE_UPDATE,
        Attach = raw.SQLITE_ATTACH,
        Detach = raw.SQLITE_DETACH,
        AlterTable = raw.SQLITE_ALTER_TABLE,
        ReIndex = raw.SQLITE_REINDEX,
        Analyze = raw.SQLITE_ANALYZE,
        CreateVTable = raw.SQLITE_CREATE_VTABLE,
        DropVTable = raw.SQLITE_DROP_VTABLE,
        Function = raw.SQLITE_FUNCTION,
        SavePoint = raw.SQLITE_SAVEPOINT,
        Copy = raw.SQLITE_COPY, 
        Recursive = raw.SQLITE_RECURSIVE
    }

    public enum ErrorCode
    {
        Ok = raw.SQLITE_OK,
        Error = raw.SQLITE_ERROR,
        Internal = raw.SQLITE_INTERNAL,
        Perm = raw.SQLITE_PERM,
        Abort = raw.SQLITE_ABORT,
        Busy = raw.SQLITE_BUSY,
        Locked = raw.SQLITE_LOCKED,
        NoMemory = raw.SQLITE_NOMEM,
        ReadOnly = raw.SQLITE_READONLY,
        Interrupt = raw.SQLITE_INTERRUPT,
        IOError = raw.SQLITE_IOERR,
        Corrupt = raw.SQLITE_CORRUPT,
        NotFound = raw.SQLITE_NOTFOUND,
        Full = raw.SQLITE_FULL,
        CannotOpen = raw.SQLITE_CANTOPEN,
        Protocol = raw.SQLITE_PROTOCOL,
        Empty = raw.SQLITE_EMPTY,
        Schema = raw.SQLITE_SCHEMA,
        TooBig = raw.SQLITE_TOOBIG,
        Constraint = raw.SQLITE_CONSTRAINT,
        Mismatch = raw.SQLITE_MISMATCH,
        Misuse = raw.SQLITE_MISUSE,
        NoLFS = raw.SQLITE_NOLFS,
        NotAuthorized = raw.SQLITE_AUTH,
        Format = raw.SQLITE_FORMAT,
        Range = raw.SQLITE_RANGE,
        NotDatabase = raw.SQLITE_NOTADB,
        Notice = raw.SQLITE_NOTICE,
        Warning = raw.SQLITE_WARNING,
        Row = raw.SQLITE_ROW,
        Done = raw.SQLITE_DONE,

        IOErrorRead = raw.SQLITE_IOERR_READ,
        IOErrorShortRead = raw.SQLITE_IOERR_SHORT_READ,
        IOErrorWrite = raw.SQLITE_IOERR_WRITE,
        IOErrorFSync = raw.SQLITE_IOERR_FSYNC,
        IOErrorDirFSync = raw.SQLITE_IOERR_DIR_FSYNC,
        IOErrorTruncate = raw.SQLITE_IOERR_TRUNCATE,
        IOErrorFState = raw.SQLITE_IOERR_FSTAT,
        IOErrorUnlock = raw.SQLITE_IOERR_UNLOCK,
        IOErrorRDLock = raw.SQLITE_IOERR_RDLOCK,
        IOErrorDelete = raw.SQLITE_IOERR_DELETE,
        IOErrorBlocked = raw.SQLITE_IOERR_BLOCKED,
        IOErrorNoMem = raw.SQLITE_IOERR_NOMEM,
        IOErrorAccess = raw.SQLITE_IOERR_ACCESS,
        IOErrorCheckReservedLock = raw.SQLITE_IOERR_CHECKRESERVEDLOCK,
        IOErrorLock = raw.SQLITE_IOERR_LOCK,
        IOErrorClose = raw.SQLITE_IOERR_CLOSE,
        IOErrorDirClose = raw.SQLITE_IOERR_DIR_CLOSE,
        IOErrorShmOpen = raw.SQLITE_IOERR_SHMOPEN,
        IOErrorShmSize = raw.SQLITE_IOERR_SHMSIZE,
        IOErrorShmLock = raw.SQLITE_IOERR_SHMLOCK,
        IOErrorShmMap = raw.SQLITE_IOERR_SHMMAP,
        IOErrorSeek = raw.SQLITE_IOERR_SEEK,
        IOErrorNoEnt = raw.SQLITE_IOERR_DELETE_NOENT,
        IOErrorMMap = raw.SQLITE_IOERR_MMAP,
        IOErrorGetTempPath = raw.SQLITE_IOERR_GETTEMPPATH,
        IOErrorConvPath = raw.SQLITE_IOERR_CONVPATH,

        LockSharedCache = raw.SQLITE_LOCKED_SHAREDCACHE,

        BusyRecovery = raw.SQLITE_BUSY_RECOVERY,
        BusySnapShot = raw.SQLITE_BUSY_SNAPSHOT,

        CannotOpenNoTempDirectory = raw.SQLITE_CANTOPEN_NOTEMPDIR,
        CannotOpenIsDirectory = raw.SQLITE_CANTOPEN_ISDIR,
        CannotOpenFullPath = raw.SQLITE_CANTOPEN_FULLPATH,
        CannotOpenConvPath = raw.SQLITE_CANTOPEN_CONVPATH,

        CorruptVTab = raw.SQLITE_CORRUPT_VTAB,

        ReadonlyRecovery = raw.SQLITE_READONLY_RECOVERY,
        ReadonlyCannotLock = raw.SQLITE_READONLY_CANTLOCK,
        ReadonlyRollback = raw.SQLITE_READONLY_ROLLBACK,
        ReadonlyDatabaseMoved = raw.SQLITE_READONLY_DBMOVED,

        AbortRollback = raw.SQLITE_ABORT_ROLLBACK,

        ConstraintCheck = raw.SQLITE_CONSTRAINT_CHECK,
        ConstraintCommitHook = raw.SQLITE_CONSTRAINT_COMMITHOOK,
        ConstraingtoreignKey = raw.SQLITE_CONSTRAINT_FOREIGNKEY,
        ConstraintFunction = raw.SQLITE_CONSTRAINT_FUNCTION,
        ConstraintNotNull = raw.SQLITE_CONSTRAINT_NOTNULL,
        ConstraintPrimvaryKey = raw.SQLITE_CONSTRAINT_PRIMARYKEY,
        ConstraintTrigger = raw.SQLITE_CONSTRAINT_TRIGGER,
        ConstraintUnique = raw.SQLITE_CONSTRAINT_UNIQUE,
        ConstraintVTab = raw.SQLITE_CONSTRAINT_VTAB,
        ConstraintRowId = raw.SQLITE_CONSTRAINT_ROWID,

        NoticeRecoverWal = raw.SQLITE_NOTICE_RECOVER_WAL,
        NoticeRecoverRollback = raw.SQLITE_NOTICE_RECOVER_ROLLBACK,

        WarningAutoIndex= raw.SQLITE_WARNING_AUTOINDEX
    }
}
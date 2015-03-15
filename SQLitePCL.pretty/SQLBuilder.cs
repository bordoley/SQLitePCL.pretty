//
// Copyright (c) 2009-2015 Krueger Systems, Inc.
// Copyright (c) 2015 David Bordoley
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

// FIXME: Seperate namespace so that we don't overrun the main namespace with non-core types.
namespace SQLitePCL.pretty
{
    internal static class SQLBuilder
    {
        internal const string SelectAllTables = 
            @"SELECT name FROM sqlite_master
              WHERE type='table'
              ORDER BY name;";

        internal const string BeginTransaction = "BEGIN TRANSACTION";

        internal const string CommitTransaction = "COMMIT TRANSACTION";

        internal const string RollbackTransaction = "ROLLBACK TRANSACTION";

        internal const string Vacuum = "VACUUM";

        internal static string BeginTransactionWithMode(TransactionMode mode)
        {
            switch (mode)
            {
                case TransactionMode.Deferred:
                    return "BEGIN DEFERRED TRANSACTION";
                case TransactionMode.Exclusive:
                    return "BEGIN EXCLUSIVE TRANSACTION";
                case TransactionMode.Immediate:
                    return "BEGIN IMMEDIATE TRANSACTION";
                default:
                    throw new ArgumentException("Not a valid TransactionMode");
            }
        }

        internal static string SavePoint(string savePoint)
        {
            return "SAVEPOINT " + savePoint;
        }

        internal static string Release(string savePoint)
        {
            return "RELEASE " + savePoint;
        }
       
        internal static string RollbackTransactionTo(string savepoint)
        {
            return "ROLLBACK TRANSACTION TO " + savepoint;
        }
    } 
}


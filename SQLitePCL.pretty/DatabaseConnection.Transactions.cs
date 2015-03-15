// Copyright (c) 2015 David Bordoley
// Copyright (c) 2009-2015 Krueger Systems, Inc.
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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Database transaction modes.
    /// </summary>
    /// <seealso href="https://www.sqlite.org/lang_transaction.html"/>
    public enum TransactionMode
    {
        /// <summary>
        /// No locks are acquired on the database until the database is first accessed.
        /// </summary>
        Deferred,

        /// <summary>
        /// RESERVED locks are acquired on all databases as soon as the BEGIN command is 
        /// executed, without waiting for the database to be used.
        /// </summary>
        Immediate, 

        /// <summary>
        /// EXCLUSIVE locks acquired on all databases. No other database connection except for read_uncommitted connections 
        /// will be able to read the database and no other connection without exception will be able to write the database 
        /// until the transaction is complete.
        /// </summary>
        Exclusive
    }    

    internal class StackRef
    {
        public StackRef()
        {
            this.Stack = Stack<string>.Empty;
        }

        public Stack<string> Stack { get; set; }
    }

    public static partial class DatabaseConnection
    {
        private static readonly ThreadLocal<Random> _rand = new ThreadLocal<Random>(() => new Random());

        private static readonly ConditionalWeakTable<IDatabaseConnection, StackRef> transactionStackTable = 
            new ConditionalWeakTable<IDatabaseConnection, StackRef>();


        private static StackRef GetTransactionStackRef(this IDatabaseConnection This)
        {
            return transactionStackTable.GetValue(This, _ => new StackRef());
        }

        private static Stack<string> GetTransactionStack(this IDatabaseConnection This)
        {
            return This.GetTransactionStackRef().Stack;
        }

        private static void ResetTransactionStack(this IDatabaseConnection This)
        {
            This.GetTransactionStackRef().Stack = Stack<string>.Empty;
        }

        private static void PushSavePoint(this IDatabaseConnection This, string savepoint)
        {
            var stackref = This.GetTransactionStackRef();
            stackref.Stack = This.GetTransactionStack().Push(savepoint);
        }

        private static void PopTransactionStackToSavePoint(this IDatabaseConnection This, string savepoint)
        {
            var transactionStack = This.GetTransactionStack();
            var result = transactionStack;
            while (result.Head != savepoint)
            {
                if (result.IsEmpty())
                { 
                    throw new ArgumentException("savePoint is not valid, and should be the result of a call to SaveTransactionPoint.", "savePoint");
                }
                result = result.Tail;
            }

            This.GetTransactionStackRef().Stack = result;
        }

        /// <summary>
        /// Begins a SQLite transaction using the default transaction mode.
        /// </summary>
        /// <param name="This">The database connection.</param>
        public static void BeginTransaction(this IDatabaseConnection This)
        {
            This.BeginTransaction(TransactionMode.Deferred);
        }

        /// <summary>
        /// Begins a SQLite transaction using the specified transaction mode.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="mode">The transaction mode.</param>
        public static void BeginTransaction(this IDatabaseConnection This, TransactionMode mode)
        {
            Contract.Requires(This != null);

            var transactionStack = This.GetTransactionStack();
            if (!transactionStack.IsEmpty()) { throw new InvalidOperationException(); }

            This.Execute(SQLBuilder.BeginTransactionWithMode(mode));
            This.PushSavePoint(SQLBuilder.BeginTransaction);
        }

        /// <summary>
        /// Executes the SQLite SAVEPOINT command, starting a new transaction with a name.
        /// </summary>
        /// <returns>The savepoint name.</returns>
        /// <param name="This">The database connection.</param>
        public static string SaveTransactionPoint(this IDatabaseConnection This)
        {
            Contract.Requires(This != null);

            var savePoint = "S" + _rand.Value.Next (short.MaxValue);
            This.Execute(SQLBuilder.SavePoint(savePoint));
            This.PushSavePoint(savePoint);
            return savePoint;
        }

        /// <summary>
        /// Executes the SQLite RELEASE command with the given savepoint (similar to COMMIT).
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="savepoint">The savepoint</param>
        /// <seealso href="https://www.sqlite.org/lang_savepoint.html"/>
        public static void ReleaseTransaction(this IDatabaseConnection This, string savepoint)
        {
            Contract.Requires(This != null);
            Contract.Requires(savepoint != null);

            This.PopTransactionStackToSavePoint(savepoint);
            This.Execute(SQLBuilder.Release(savepoint));
        }

        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        /// <param name="This">The database connection.</param>
        public static void CommitTransaction(this IDatabaseConnection This)
        {
            Contract.Requires(This != null);

            This.ResetTransactionStack();
            This.Execute(SQLBuilder.CommitTransaction);
        }

        /// <summary>
        /// Rollbacks the current transaction to a specific savepoint.
        /// </summary>
        /// <param name="This">This.</param>
        /// <param name="savepoint">The savepoint.</param>
        /// <seealso href="https://www.sqlite.org/lang_transaction.html"/>
        public static void RollbackTransactionTo(this IDatabaseConnection This, string savepoint)
        {
            Contract.Requires(This != null);

            This.PopTransactionStackToSavePoint(savepoint);
            This.Execute(SQLBuilder.RollbackTransactionTo(savepoint));
        }

        /// <summary>
        /// Rollback the current transaction.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <seealso href="https://www.sqlite.org/lang_transaction.html"/>
        public static void RollbackTransaction(this IDatabaseConnection This)
        {
            Contract.Requires(This != null);
            This.ResetTransactionStack();
            This.Execute(SQLBuilder.RollbackTransaction);
        }

        /// <summary>
        /// Runs the action <paramref name="action"/> in a transaction.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="action">The Action to run in a transaction.</param>
        public static void RunInTransaction(this IDatabaseConnection This, Action<IDatabaseConnection> action)
        {
            Contract.Requires(This != null);
            Contract.Requires(action != null);

            This.RunInTransaction<object>(db => 
                {
                    action(db);
                    return null;
                });
        }

        /// <summary>
        /// Runs the function <paramref name="f"/> in a transaction and returns function result.
        /// </summary>
        /// <returns>The function result.</returns>
        /// <param name="This">The database connection.</param>
        /// <param name="f">
        /// The function to run in a transaction.
        /// </param>
        /// <typeparam name="T">The result type.</typeparam>
        public static T RunInTransaction<T>(this IDatabaseConnection This, Func<IDatabaseConnection, T> f)
        {
            Contract.Requires(This != null);
            Contract.Requires(f != null);

            bool shouldDoFullRollBack = false;
            if (This.GetTransactionStack().IsEmpty())
            {
                shouldDoFullRollBack = true;
                This.BeginTransaction();
            }

            var savePoint = This.SaveTransactionPoint();

            try
            {
                var retval = f(This);
                This.ReleaseTransaction(savePoint);
                return retval;
            }
            catch (Exception)
            {
                if (shouldDoFullRollBack)
                {
                    This.RollbackTransaction();
                }
                else
                {
                    This.RollbackTransactionTo(savePoint);
                }
                throw;
            }
        }

        /// <summary>
        /// Attempt to execute the action <paramref name="action"/> and if any exception is thrown, 
        /// executes a rollback command.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="action">The action.</param>
        public static void TryOrRollback(this IDatabaseConnection This, Action<IDatabaseConnection> action)
        {
            Contract.Requires(This != null);
            Contract.Requires(action != null);

            This.TryOrRollback<object>(_ => 
                { 
                    action(This); 
                    return null; 
                });
        }

        /// <summary>
        /// Attempt to call the function <paramref name="f"/> and if any exception is thrown, 
        /// executes a rollback command.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="f">The action.</param>
        /// <typeparam name="T">The result type.</typeparam>
        public static T TryOrRollback<T>(this IDatabaseConnection This, Func<IDatabaseConnection, T> f)
        {
            Contract.Requires(This != null);
            Contract.Requires(f != null);

            try
            {
                return f(This);
            }
            catch (Exception)
            {
                This.RollbackTransaction();
                throw;
            }
        }
    }
}
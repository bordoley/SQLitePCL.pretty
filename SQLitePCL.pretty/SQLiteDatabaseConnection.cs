using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// SQLite database connection builder.
    /// </summary>
    public sealed class SQLiteDatabaseConnectionBuilder
    {
        /// <summary>
        /// Returns a <see cref="SQLiteDatabaseConnectionBuilder"/> instance that uses the 
        /// specified file.
        /// </summary>
        /// <param name="filename">The database filename.</param>
        public static SQLiteDatabaseConnectionBuilder Create(string filename)
        {
            Contract.Requires(filename != null);
            return new SQLiteDatabaseConnectionBuilder(filename);
        }

        /// <summary>
        /// Returns a <see cref="SQLiteDatabaseConnectionBuilder"/> instance that uses 
        /// an in memory database.
        /// </summary>
        public static SQLiteDatabaseConnectionBuilder InMemory()
        {
            return new SQLiteDatabaseConnectionBuilder(":memory:");
        }
            
        private readonly IDictionary<Tuple<string, int>, Tuple<delegate_function_aggregate_step, delegate_function_aggregate_final>> aggFuncs =
            new Dictionary<Tuple<string, int>, Tuple<delegate_function_aggregate_step, delegate_function_aggregate_final>>();
            
        private readonly IDictionary<Tuple<string, int>, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue>> scalarFuncs =
            new Dictionary<Tuple<string, int>, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue>>();

        private readonly IDictionary<string, Comparison<string>> collationFuncs =
            new Dictionary<string, Comparison<string>>();

        private int autoCheckPointCount = 0;
        private Func<ActionCode, string, string, string, string, AuthorizerReturnCode> authorizer = null;
        private TimeSpan busyTimeout = TimeSpan.MinValue;
        private Func<bool> commitHook = null;
        private ConnectionFlags connectionFlags = ConnectionFlags.ReadWrite | ConnectionFlags.Create;
        private string fileName;
        private Func<bool> progressHandler = null;
        private int progressHandlerInterval = 100;
        private string vfs = null;

        internal SQLiteDatabaseConnectionBuilder(string fileName)
        {
            this.fileName = fileName;
        }

        /// <summary>
        /// Sets an authorizer callback function. May be null.
        /// </summary>
        /// <seealso href="https://www.sqlite.org/c3ref/set_authorizer.html"/>
        public Func<ActionCode, string, string, string, string, AuthorizerReturnCode> Authorizer { set { this.authorizer = value; } }

        /// <summary>
        /// Causes any database on the database connection to automatically checkpoint
        /// after committing a transaction if there are <paramref name="n"/> or
        /// more frames in the write-ahead log file.
        /// </summary>
        /// <param name="n">The number of frames in the write-ahead log that should trigger a checkpoint.</param>
        /// <seealso href="https://www.sqlite.org/c3ref/wal_autocheckpoint.html"/>
        public int AutoCheckPointCount 
        {
            set
            {  
                Contract.Requires(value >= 0);
                this.autoCheckPointCount = value;
            }
        }

        /// <summary>
        /// Sets the connection busy timeout when waiting for a locked table.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/busy_timeout.html"/>
        public TimeSpan BusyTimeout
        {
            set
            {
                Contract.Requires(value.TotalMilliseconds <= Int32.MaxValue);
                this.busyTimeout = value;
            }
        }

        /// <summary>
        /// A callback function to be invoked whenever a transaction is committed that returns <see langwords="true"/>
        /// if the commit should be rolled back, otherwise <see langwords="false"/>. May be null.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/commit_hook.html"/>
        public Func<bool> CommitHook { set { this.commitHook = value; } }

        /// <summary>
        /// <see cref="ConnectionFlags"/> used to defined if the database is readonly,
        /// read/write and whether a new database file should be created if it does not already exist.
        /// </summary>
        public ConnectionFlags ConnectionFlags { set { this.connectionFlags = value; } }

        /// <summary>
        /// Sets the database filename.
        /// </summary>
        public string FileName
        {
            set
            {
                Contract.Requires(value != null);
                this.FileName = value;
            }
        }

        /// <summary>
        /// Sets a callback function to be invoked periodically during
        /// database operations, providing a mechanism to interrupt the current operation.
        /// </summary>
        public Func<bool> ProgressHandler { set { this.progressHandler = value; } }

        /// <summary>
        /// Sets approximate number of virtual machine instructions that are evaluated between successive invocations of the ProgressHandler callback.
        /// </summary>
        public int ProgressHandlerInterval
        { 
            set
            { 
                Contract.Requires(value > 0);
                this.progressHandlerInterval = value;
            }
        }

        public string VFS { set { this.vfs = value; } }

        private sealed class CtxState<T>
        {
            internal T Value { get; }

            internal CtxState(T value)
            {
                this.Value = value;
            }
        }

        private void AddAggregateFunc(string name, int nArg, delegate_function_aggregate_step step, delegate_function_aggregate_final final)
        {
            var key = Tuple.Create(name, nArg);
            Contract.Requires(!aggFuncs.ContainsKey(key));
            Contract.Requires(!scalarFuncs.ContainsKey(key));

            this.aggFuncs.Add(key, Tuple.Create(step, final));
        }

        private void AddAggregateFunc<T>(string name, int nArg, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(name != null);
            Contract.Requires(func != null);
            Contract.Requires(resultSelector != null);
            Contract.Requires(nArg >= -1);

            delegate_function_aggregate_step funcStep = (ctx, _, args) =>
                {
                    CtxState<T> state;
                    if (ctx.state == null)
                    {
                        state = new CtxState<T>(seed);
                        ctx.state = state;
                    }
                    else
                    {
                        state = (CtxState<T>)ctx.state;
                    }

                    IReadOnlyList<ISQLiteValue> iArgs = args.Select(value => value.ToSQLiteValue()).ToList();

                    T next = func(state.Value, iArgs);
                    ctx.state = new CtxState<T>(next);
                };

            delegate_function_aggregate_final funcFinal = (ctx, _) =>
                {
                    CtxState<T> state = (CtxState<T>)ctx.state;

                    // FIXME: https://github.com/ericsink/SQLitePCL.raw/issues/30
                    ISQLiteValue result = resultSelector(state.Value);
                    ctx.SetResult(result);
                };

            this.AddAggregateFunc(name, nArg, funcStep, funcFinal);
        }

        /// <summary>
        /// Add an aggregate function that can accept any number of <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        public void AddAggregateFunc<T>(string name, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector) =>
            this.AddAggregateFunc(name, -1, seed, func, resultSelector);

        /// <summary>
        /// Add an aggregate function that accepts no <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        public void AddAggregateFunc<T>(String name, T seed, Func<T, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            this.AddAggregateFunc(name, 0, seed, (t, _) => func(t), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 1 <see href="ISQLiteValue"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        public void AddAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            this.AddAggregateFunc(name, 1, seed, (t, val) => func(t, val[0]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 2 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        public void AddAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            this.AddAggregateFunc(name, 2, seed, (t, val) => func(t, val[0], val[1]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 3 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        public void AddAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            this.AddAggregateFunc(name, 3, seed, (t, val) => func(t, val[0], val[1], val[2]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 4 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        public void AddAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            this.AddAggregateFunc(name, 4, seed, (t, val) => func(t, val[0], val[1], val[2], val[3]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 5 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        public void AddAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            this.AddAggregateFunc(name, 5, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 6 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        public void AddAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            this.AddAggregateFunc(name, 6, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 7 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        public void AddAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            this.AddAggregateFunc(name, 7, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5], val[6]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 8 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        public void AddAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            this.AddAggregateFunc(name, 8, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5], val[6], val[7]), resultSelector);
        }

        /// <summary>
        /// Add or modify a collation function to the connection.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/create_collation.html"/>
        /// <param name="name">The function name.</param>
        /// <param name="comparison">A string comparison function.</param>
        public void AddCollation(string name, Comparison<string> comparison)
        {
            Contract.Requires(name != null);
            Contract.Requires(comparison != null);
            Contract.Requires(!this.collationFuncs.ContainsKey(name));

            this.collationFuncs.Add(name, comparison);
        }

        /// <summary>
        /// Adds a scalar function.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/create_function.html"/>
        /// <param name="name">The function name.</param>
        /// <param name="nArg">The number of arguments the function takes or -1 if it may take any number of arguments.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        private void AddScalarFunc(string name, int nArg, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce)
        {
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);
            Contract.Requires(nArg >= -1);

            var key = Tuple.Create(name, nArg);
            Contract.Requires(!aggFuncs.ContainsKey(key));
            Contract.Requires(!scalarFuncs.ContainsKey(key));

            scalarFuncs.Add(key, reduce);
        }

        /// <summary>
        /// Adds a scalar function that can accept any number of <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        public void AddScalarFunc(string name, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce) =>
            this.AddScalarFunc(name, -1, reduce);

        /// <summary>
        /// Adds a scalar function that accepts 0 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        public void AddScalarFunc(string name, Func<ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            this.AddScalarFunc(name, 0, _ => reduce());
        }

        /// <summary>
        /// Adds a scalar function that accepts 1 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        public void AddScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            this.AddScalarFunc(name, 1, val => reduce(val[0]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 2 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        public void AddScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            this.AddScalarFunc(name, 2, val => reduce(val[0], val[1]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 3 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        public void AddScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            this.AddScalarFunc(name, 3, val => reduce(val[0], val[1], val[2]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 4 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        public void AddScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            this.AddScalarFunc(name, 4, val => reduce(val[0], val[1], val[2], val[3]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 5 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        public void AddScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            this.AddScalarFunc(name, 5, val => reduce(val[0], val[1], val[2], val[3], val[4]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 6 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        public void AddScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            this.AddScalarFunc(name, 6, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 7 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        public void AddScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            this.AddScalarFunc(name, 7, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5], val[6]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 8 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        public void AddScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            this.AddScalarFunc(name, 8, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5], val[6], val[7]));
        }

        /// <summary>
        /// Build a <see cref="SQLiteDatabaseConnection"/> instance. 
        /// </summary>
        public SQLiteDatabaseConnection Build()
        {
            sqlite3 db;
            {
                int rc = raw.sqlite3_open_v2(this.fileName, out db, (int)this.connectionFlags, this.vfs);
                SQLiteException.CheckOk(db, rc);
            }

            foreach (var f in this.aggFuncs)
            {
                var name = f.Key.Item1;
                var nArg = f.Key.Item2;
                var funcStep = f.Value.Item1;
                var funcFinal = f.Value.Item2;

                int rc = raw.sqlite3_create_function(db, name, nArg, null, funcStep, funcFinal);
                SQLiteException.CheckOk(db, rc);
            }

            var authorizer = this.authorizer;
            if (authorizer != null)
            {
                int rc = raw.sqlite3_set_authorizer(db, (o, actionCode, p0, p1, dbName, triggerOrView) =>
                    (int)authorizer((ActionCode)actionCode, p0, p1, dbName, triggerOrView), null);
                SQLiteException.CheckOk(rc);  
            }

            if (this.autoCheckPointCount > 0)
            {
                int rc = raw.sqlite3_wal_autocheckpoint(db, this.autoCheckPointCount);
                SQLiteException.CheckOk(db, rc);
            }

            if (this.busyTimeout.TotalMilliseconds > 0)
            {
                int rc = raw.sqlite3_busy_timeout(db, (int) this.busyTimeout.TotalMilliseconds);
                SQLiteException.CheckOk(db, rc);
            }

            foreach (var collation in this.collationFuncs)
            {
                var name = collation.Key;
                var comparison = collation.Value;

                int rc = raw.sqlite3_create_collation(db, name, null, (v, s1, s2) => comparison(s1, s2));
                SQLiteException.CheckOk(db, rc);
            }

            var commitHook = this.commitHook;
            if (commitHook != null)
            {
                raw.sqlite3_commit_hook(db, v => commitHook() ? 1 : 0, null);
            }

            var progressHandler = this.progressHandler;
            if (progressHandler != null)
            {
                raw.sqlite3_progress_handler(db, this.progressHandlerInterval, _ => progressHandler() ? 1 : 0, null);
            }

            foreach (var f in this.scalarFuncs)
            {
                var name = f.Key.Item1;
                var nArg = f.Key.Item2;
                var reduce = f.Value;

                int rc = raw.sqlite3_create_function(db, name, nArg, null, (ctx, ud, args) =>
                    {
                        IReadOnlyList<ISQLiteValue> iArgs = args.Select(value => value.ToSQLiteValue()).ToList();

                        // FIXME: https://github.com/ericsink/SQLitePCL.raw/issues/30
                        ISQLiteValue result = reduce(iArgs);
                        ctx.SetResult(result);
                    });
                SQLiteException.CheckOk(db, rc);
            }

            return new SQLiteDatabaseConnection(db);
        }

        /// <summary>
        /// Clears all collation functions.
        /// </summary>
        public void ClearCollations()
        {
            collationFuncs.Clear();
        }

        /// <summary>
        /// Clears all aggregate and scalar functions.
        /// </summary>
        public void ClearFuncs()
        {
            scalarFuncs.Clear();
            aggFuncs.Clear();
        }

        internal SQLiteDatabaseConnectionBuilder Clone()
        {
            var builder = new SQLiteDatabaseConnectionBuilder(this.fileName);
            builder.ConnectionFlags = this.connectionFlags;
            builder.VFS = this.vfs;

            foreach (var f in this.aggFuncs)
            {
                var name = f.Key.Item1;
                var nArg = f.Key.Item2;
                var funcStep = f.Value.Item1;
                var funcFinal = f.Value.Item2;

                builder.AddAggregateFunc(name, nArg, funcStep, funcFinal);
            }

            builder.Authorizer = this.authorizer;
            builder.AutoCheckPointCount = this.autoCheckPointCount;
            builder.BusyTimeout = this.busyTimeout;

            foreach (var collation in this.collationFuncs)
            {
                var name = collation.Key;
                var comparison = collation.Value;
                builder.AddCollation(name, comparison);
            }

            builder.CommitHook = this.commitHook;
            builder.ProgressHandler = this.progressHandler;
            builder.ProgressHandlerInterval = this.progressHandlerInterval;

            foreach (var f in this.scalarFuncs)
            {
                var name = f.Key.Item1;
                var nArg = f.Key.Item2;
                var reduce = f.Value;

                builder.AddScalarFunc(name, nArg, reduce);
            }

            return builder;
        }

        /// <summary>
        /// Removes the specified collation function.
        /// </summary>
        /// <returns><c>true</c>, if the collation function was removed, <c>false</c> otherwise.</returns>
        /// <param name="name">The collation function name.</param>
        public bool RemoveCollation(string name)
        {
            Contract.Requires(name != null);
            return collationFuncs.Remove(name);
        }

        /// <summary>
        /// Removes the specified aggregate or scalar function.
        /// </summary>
        /// <returns><c>true</c>, if the function was removed, <c>false</c> otherwise.</returns>
        /// <param name="name">The name of the function.</param>
        /// <param name="nArg">The number of arguments to the function.</param>
        public bool RemoveFunc(string name, int nArg)
        {
            Contract.Requires(name != null);
            Contract.Requires(nArg >= -1);

            var key = Tuple.Create(name, nArg);

            return scalarFuncs.Remove(key) || aggFuncs.Remove(key);
        }
    }

    /// <summary>
    /// An implementation of IDatabaseConnection that wraps a raw SQLite database connection.
    /// </summary>
    public sealed class SQLiteDatabaseConnection : IDatabaseConnection, IDisposable
    {
        private readonly sqlite3 db;
        private readonly OrderedSet<StatementImpl> statements = new OrderedSet<StatementImpl>();

        private bool disposed = false;

        internal SQLiteDatabaseConnection(sqlite3 db)
        {
            this.db = db;

            // FIXME: Could argue that the shouldn't be setup until the first subscriber to the events
            raw.sqlite3_rollback_hook(db, v => Rollback(this, EventArgs.Empty), null);

            raw.sqlite3_trace(
                db,
                (v, stmt) => Trace(this, DatabaseTraceEventArgs.Create(stmt)),
                null);

            raw.sqlite3_profile(
                db, (v, stmt, ts) => Profile(this, DatabaseProfileEventArgs.Create(stmt, TimeSpan.FromTicks(ts))),
                null);

            raw.sqlite3_update_hook(
                db,
                (v, type, database, table, rowid) => Update(this, DatabaseUpdateEventArgs.Create((ActionCode)type, database, table, rowid)),
                null);
        }

        // We initialize the event handlers with empty delegates so that we don't
        // have add annoying null checks all over the place.
        // See: http://blogs.msdn.com/b/ericlippert/archive/2009/04/29/events-and-races.aspx
        // FIXME: One could argue that we really shouldn't initialized the callbacks
        // with sqlite3 until we actually have listeners. not sure how much it matters though

        /// <summary>
        /// Occurs whenever a transaction is rolled back on the database connection.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/commit_hook.html"/>
        public event EventHandler Rollback = (o, e) => { };

        /// <summary>
        /// Profiling event that occurs when a <see cref="IStatement"/> finishes.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/profile.html"/>
        public event EventHandler<DatabaseProfileEventArgs> Profile = (o, e) => { };

        /// <summary>
        /// Tracing event that occurs at various times when <see cref="IStatement"/>is running.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/profile.html"/>
        public event EventHandler<DatabaseTraceEventArgs> Trace = (o, e) => { };

        /// <summary>
        /// Occurs whenever a row is updated, inserted or deleted in a rowid table.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/update_hook.html"/>
        public event EventHandler<DatabaseUpdateEventArgs> Update = (o, e) => { };
            
        internal event EventHandler Disposing = (o, e) => { };

        /// <inheritdoc/>
        public bool IsAutoCommit
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return raw.sqlite3_get_autocommit(db) != 0;
            }
        }

        /// <inheritdoc/>
        public bool IsReadOnly
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return raw.sqlite3_db_readonly(db, null) != 0;
            }
        }

        /// <inheritdoc/>
        public int Changes
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return raw.sqlite3_changes(db);
            }
        }

        /// <inheritdoc/>
        public int TotalChanges
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return raw.sqlite3_total_changes(db);
            }
        }

        /// <inheritdoc/>
        public long LastInsertedRowId
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
                return raw.sqlite3_last_insert_rowid(db);
            }
        }   

        /// <summary>
        /// An enumeration of the connection's currently opened statements in the order they were prepared.
        /// </summary>
        public IEnumerable<IStatement> Statements
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

                // Reverse the order of the statements to match the order returned by SQLite.
                // Side benefit of preventing callers from being able to cast the statement 
                // list and do evil things see: http://stackoverflow.com/a/491591
                return this.statements.Reverse();
            }
        }

        /// <summary>
        /// Initializes a backup of database to a destination database.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/backup_finish.html"/>
        /// <param name="dbName">The name of the database to backup.</param>
        /// <param name="destConn">The destination database connection.</param>
        /// <param name="destDbName">The destination database name</param>
        /// <returns>An <see cref="IDatabaseBackup"/> instance that can be used to
        /// perform the backup operation.</returns>
        public IDatabaseBackup BackupInit(string dbName, SQLiteDatabaseConnection destConn, string destDbName)
        {
            Contract.Requires(dbName != null);
            Contract.Requires(destConn != null);
            Contract.Requires(destDbName != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            sqlite3_backup backup = raw.sqlite3_backup_init(destConn.db, destDbName, db, dbName);
            var result = new DatabaseBackupImpl(backup, this, destConn);
            return result;
        }

        /// <inheritdoc/>
        public TableColumnMetadata GetTableColumnMetadata(string dbName, string tableName, string columnName)
        {
            Contract.Requires(tableName != null);
            Contract.Requires(columnName != null);

            string dataType;
            string collSeq;
            int notNull;
            int primaryKey;
            int autoInc;

            int rc = raw.sqlite3_table_column_metadata(db, dbName, tableName, columnName, out dataType, out collSeq, out notNull, out primaryKey, out autoInc);
            SQLiteException.CheckOk(db, rc);
            return new TableColumnMetadata(dataType, collSeq, notNull != 0, primaryKey != 0, autoInc != 0);
        }

        /// <inheritdoc/>
        public void Interrupt()
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
            raw.sqlite3_interrupt(db);
        }   

        /// <inheritdoc/>
        public bool IsDatabaseReadOnly(string dbName)
        {
            Contract.Requires(dbName != null);
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
            int rc = raw.sqlite3_db_readonly(db, dbName);
            switch (rc)
            {
                case 1:
                    return true;
                case 0:
                    return false;
                default:
                    throw new ArgumentException("Not the name of a database on the connection.");
            }
        }

        /// <summary>
        /// Returns the filename associated with the database if available.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/db_filename.html"/>
        /// <param name="database">The database name. The main database file has the name "main".</param>
        /// <param name="filename">When this method returns, contains the filename if there is an
        /// attached database that is not temporary or in memory. Otherwise null.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the database has a filename, otherwise <see langword="false"/>.
        /// </returns>
        public bool TryGetFileName(string database, out string filename)
        {
            Contract.Requires(database != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            filename = raw.sqlite3_db_filename(db, database);

            // If there is no attached database N on the database connection, or
            // if database N is a temporary or in-memory database, then a NULL pointer is returned.
            return !String.IsNullOrEmpty(filename);
        }

        /// <inheritdoc/>
        public Stream OpenBlob(string database, string tableName, string columnName, long rowId, bool canWrite)
        {
            Contract.Requires(database != null);
            Contract.Requires(tableName != null);
            Contract.Requires(columnName != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            sqlite3_blob blob;
            int rc = raw.sqlite3_blob_open(db, database, tableName, columnName, rowId, canWrite ? 1 : 0, out blob);
            SQLiteException.CheckOk(db, rc);

            var length = raw.sqlite3_blob_bytes(blob);
            var result = new BlobStream(blob, this, canWrite, length);
            return result;
        }

        /// <inheritdoc/>
        public IStatement PrepareStatement(string sql, out string tail)
        {
            Contract.Requires(sql != null);

            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            sqlite3_stmt stmt;
            int rc = raw.sqlite3_prepare_v2(db, sql, out stmt, out tail);
            SQLiteException.CheckOk(db, rc);

            var result = new StatementImpl(stmt, this);
            statements.Add(result);
            return result;
        }

        internal void RemoveStatement(StatementImpl stmt)
        {
            statements.Remove(stmt);
        }

        /// <inheritdoc/>
        public void Status(DatabaseConnectionStatusCode statusCode, out int current, out int highwater, bool reset)
        {
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }

            int rc = raw.sqlite3_db_status(db, (int)statusCode, out current, out highwater, reset ? 1 : 0);
            SQLiteException.CheckOk(rc);
        }

        /// <inheritdoc/>
        public void WalCheckPoint(string dbName, WalCheckPointMode mode, out int nLog, out int nCkpt)
        {
            Contract.Requires(dbName != null);
            if (disposed) { throw new ObjectDisposedException(this.GetType().FullName); }
            int rc = raw.sqlite3_wal_checkpoint_v2(db, dbName, (int) mode, out nLog, out nCkpt);
            SQLiteException.CheckOk(db, rc);
        }
         
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed) { return; }

            this.Disposing(this, null);

            disposed = true;

            //FIXME: Handle errors?
            raw.sqlite3_close(db);
        }

        /// <inheritdoc/>
        ~SQLiteDatabaseConnection()
        {
            Dispose(false);
        }
    }
}


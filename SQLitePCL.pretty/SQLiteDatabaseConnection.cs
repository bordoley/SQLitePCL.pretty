using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// An immutable threadsafe builder that can be used to create <see cref="SQLiteDatabaseConnection"/> instances.
    /// </summary>
    public sealed class SQLiteDatabaseConnectionBuilder
    {
        /// <summary>
        /// Returns a <see cref="SQLiteDatabaseConnectionBuilder"/> that 
        /// creates in memory <see cref="SQLiteDatabaseConnection"/> instances.
        /// </summary>
        public static SQLiteDatabaseConnectionBuilder InMemory { get; } =
            SQLiteDatabaseConnectionBuilder.Create(":memory:");

        /// <summary>
        /// Creates <see cref="SQLiteDatabaseConnectionBuilder"/> instances with the provided parameters.
        /// </summary>
        /// <param name="fileName">The filename of the database file.</param>
        /// <param name="autoCheckPointCount">
        ///     The number of frames in the write-ahead log file that causes any database on
        ///     database connection D to automatically checkpoint. 
        ///     See: <see href="https://www.sqlite.org/c3ref/wal_autocheckpoint.html"/>
        /// </param>
        /// <param name="authorizer">
        ///     The authorizer callback used by created <see cref="SQLiteDatabaseConnection"/> instances.
        ///     See: <see href="https://www.sqlite.org/c3ref/set_authorizer.html"/>
        /// </param>
        /// <param name="busyTimeout">
        ///    The specified amount of time a connection sleeps when a table is locked. 
        ///    See: <see href="https://www.sqlite.org/c3ref/busy_timeout.html"/>
        /// </param>
        /// <param name="commitHook">
        ///     A callback function to be invoked whenever a transaction is committed.
        ///     See: <see href="https://www.sqlite.org/c3ref/commit_hook.html"/>
        /// </param>
        /// <param name="connectionFlags">
        ///     The <see cref="ConnectionFlags"/> used when creating <see cref="SQLiteDatabaseConnection"/> instances.
        /// </param>
        /// <param name="progressHandler">
        ///     A callback function to be invoked periodically during long running calls to the database connection.
        ///     See: <see href="https://www.sqlite.org/c3ref/progress_handler.html"/>
        /// </param>
        /// <param name="progressHandlerInterval">
        ///     The approximate number of virtual machine instructions that are evaluated between 
        ///     successive invocations of the <paramref name="progressHandler"/> callback.
        ///     See: <see href="https://www.sqlite.org/c3ref/progress_handler.html"/>
        /// </param>
        /// <param name="vfs">
        /// 
        /// </param>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public static SQLiteDatabaseConnectionBuilder Create(
                string fileName,
                int autoCheckPointCount = 0,
                Func<ActionCode, string, string, string, string, AuthorizerReturnCode> authorizer = null,
                TimeSpan busyTimeout = default(TimeSpan),
                Func<bool> commitHook = null,
                ConnectionFlags connectionFlags = ConnectionFlags.ReadWrite | ConnectionFlags.Create,
                Func<bool> progressHandler = null,
                int progressHandlerInterval = 100,
                string vfs = null) =>
            SQLiteDatabaseConnectionBuilder.Create(
                fileName,
                autoCheckPointCount,
                authorizer,
                busyTimeout,
                commitHook,
                connectionFlags,
                progressHandler,
                progressHandlerInterval,
                vfs,
                new Dictionary<Tuple<string, int>, Tuple<delegate_function_aggregate_step, delegate_function_aggregate_final>>(),
                new Dictionary<Tuple<string, int>, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue>>(),
                new Dictionary<string, Comparison<string>>());

        private static SQLiteDatabaseConnectionBuilder Create(
            string fileName,
            int autoCheckPointCount,
            Func<ActionCode, string, string, string, string, AuthorizerReturnCode> authorizer,
            TimeSpan busyTimeout,
            Func<bool> commitHook,
            ConnectionFlags connectionFlags,
            Func<bool> progressHandler,
            int progressHandlerInterval,
            string vfs,
            IDictionary<Tuple<string, int>, Tuple<delegate_function_aggregate_step, delegate_function_aggregate_final>> aggFuncs,
            IDictionary<Tuple<string, int>, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue>> scalarFuncs,
            IDictionary<string, Comparison<string>> collationFuncs) 
        {
            Contract.Requires(fileName != null);
            Contract.Requires(autoCheckPointCount >= 0);
            Contract.Requires(busyTimeout.TotalMilliseconds <= Int32.MaxValue);
            Contract.Requires(progressHandlerInterval > 0);

            return new SQLiteDatabaseConnectionBuilder(
                fileName,
                autoCheckPointCount,
                authorizer,
                busyTimeout,
                commitHook,
                connectionFlags,
                progressHandler,
                progressHandlerInterval,
                vfs,
                aggFuncs,
                scalarFuncs,
                collationFuncs);
        }
  
        private readonly IDictionary<Tuple<string, int>, Tuple<delegate_function_aggregate_step, delegate_function_aggregate_final>> aggFuncs;
        private readonly IDictionary<Tuple<string, int>, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue>> scalarFuncs;
        private readonly IDictionary<string, Comparison<string>> collationFuncs;

        private readonly int autoCheckPointCount;
        private readonly Func<ActionCode, string, string, string, string, AuthorizerReturnCode> authorizer;
        private readonly TimeSpan busyTimeout;
        private readonly Func<bool> commitHook;
        private readonly ConnectionFlags connectionFlags;
        private readonly string fileName;
        private readonly Func<bool> progressHandler;
        private readonly int progressHandlerInterval;
        private readonly string vfs;

        private SQLiteDatabaseConnectionBuilder(
            string fileName,
            int autoCheckPointCount,
            Func<ActionCode, string, string, string, string, AuthorizerReturnCode> authorizer,
            TimeSpan busyTimeout,
            Func<bool> commitHook,
            ConnectionFlags connectionFlags,
            Func<bool> progressHandler,
            int progressHandlerInterval,
            string vfs,
            IDictionary<Tuple<string, int>, Tuple<delegate_function_aggregate_step, delegate_function_aggregate_final>> aggFuncs,
            IDictionary<Tuple<string, int>, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue>> scalarFuncs,
            IDictionary<string, Comparison<string>> collationFuncs)
        {
            this.fileName = fileName;
            this.autoCheckPointCount = autoCheckPointCount;
            this.authorizer = authorizer;
            this.busyTimeout = busyTimeout;
            this.commitHook = commitHook;
            this.connectionFlags = connectionFlags;
            this.progressHandler = progressHandler;
            this.progressHandlerInterval = progressHandlerInterval;
            this.vfs = vfs;

            this.aggFuncs = aggFuncs;
            this.scalarFuncs = scalarFuncs;
            this.collationFuncs = collationFuncs;
        }

        /// <summary>
        /// Build a <see cref="SQLiteDatabaseConnection"/> instance. 
        /// </summary>
        /// <returns>A <see cref="SQLiteDatabaseConnection"/> instance.</returns>
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
                
            if (this.authorizer != null)
            {
                int rc = raw.sqlite3_set_authorizer(db, (o, actionCode, p0, p1, dbName, triggerOrView) =>
                    (int)this.authorizer((ActionCode)actionCode, p0, p1, dbName, triggerOrView), null);
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

            if (this.commitHook != null)
            {
                raw.sqlite3_commit_hook(db, v => this.commitHook() ? 1 : 0, null);
            }
                
            if (this.progressHandler != null)
            {
                raw.sqlite3_progress_handler(db, this.progressHandlerInterval, _ => this.progressHandler() ? 1 : 0, null);
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
        /// Creates a new <see cref="SQLiteDatabaseConnectionBuilder"/> with the provided parameters.
        /// </summary>
        /// 
        /// <param name="fileName">The filename of the database file.</param>
        /// <param name="autoCheckPointCount">
        ///     The number of frames in the write-ahead log file that causes any database on
        ///     database connection D to automatically checkpoint. 
        ///     See: <see href="https://www.sqlite.org/c3ref/wal_autocheckpoint.html"/>
        /// </param>
        /// <param name="authorizer">
        ///     The authorizer callback used by created <see cref="SQLiteDatabaseConnection"/> instances.
        ///     See: <see href="https://www.sqlite.org/c3ref/set_authorizer.html"/>
        /// </param>
        /// <param name="busyTimeout">
        ///    The specified amount of time a connection sleeps when a table is locked. 
        ///    See: <see href="https://www.sqlite.org/c3ref/busy_timeout.html"/>
        /// </param>
        /// <param name="commitHook">
        ///     A callback function to be invoked whenever a transaction is committed.
        ///     See: <see href="https://www.sqlite.org/c3ref/commit_hook.html"/>
        /// </param>
        /// <param name="connectionFlags">
        ///     The <see cref="ConnectionFlags"/> used when creating <see cref="SQLiteDatabaseConnection"/> instances.
        /// </param>
        /// <param name="progressHandler">
        ///     A callback function to be invoked periodically during long running calls to the database connection.
        ///     See: <see href="https://www.sqlite.org/c3ref/progress_handler.html"/>
        /// </param>
        /// <param name="progressHandlerInterval">
        ///     The approximate number of virtual machine instructions that are evaluated between 
        ///     successive invocations of the <paramref name="progressHandler"/> callback.
        ///     See: <see href="https://www.sqlite.org/c3ref/progress_handler.html"/>
        /// </param>
        /// <param name="vfs">
        /// 
        /// </param>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder With(
                string fileName = null,
                int? autoCheckPointCount = null,
                Func<ActionCode, string, string, string, string, AuthorizerReturnCode> authorizer = null,
                TimeSpan? busyTimeout = null,
                Func<bool> commitHook = null,
                ConnectionFlags? connectionFlags = null,
                Func<bool> progressHandler = null,
                int? progressHandlerInterval = null,
                string vfs = null) => 
            SQLiteDatabaseConnectionBuilder.Create(
                fileName ?? this.fileName,
                autoCheckPointCount ?? this.autoCheckPointCount,
                authorizer ?? this.authorizer,
                busyTimeout ?? this.busyTimeout,
                commitHook ?? this.commitHook,
                connectionFlags ?? this.connectionFlags,
                progressHandler ?? this.progressHandler,
                progressHandlerInterval ?? this.progressHandlerInterval,
                vfs ?? this.vfs,
                this.aggFuncs,
                this.scalarFuncs,
                this.collationFuncs);

        /// <summary>
        /// Creates a new <see cref="SQLiteDatabaseConnectionBuilder"/> without the specified parameter.
        /// </summary>
        /// <param name="authorizer">
        ///     if <see langword="true"/> removes the authorizer callback 
        ///     from the new <see cref="SQLiteDatabaseConnectionBuilder"/> instance.
        /// </param>
        /// <param name="commitHook">
        ///     if <see langword="true"/> removes the commitHook callback 
        ///     from the new <see cref="SQLiteDatabaseConnectionBuilder"/> instance.
        /// </param>
        /// <param name="progressHandler">
        ///     if <see langword = "true" /> removes the progressHandler callback
        ///     from the new <see cref="SQLiteDatabaseConnectionBuilder"/> instance.
        /// </param>
        /// <param name="vfs">
        ///
        /// </param>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder Without(
                bool authorizer = false,
                bool commitHook = false,
                bool progressHandler = false,
                bool vfs = false) => 
            new SQLiteDatabaseConnectionBuilder(
                this.fileName,
                this.autoCheckPointCount,
                authorizer ? null : this.authorizer,
                this.busyTimeout,
                commitHook ? null : this.commitHook,
                this.connectionFlags,
                progressHandler ? null : this.progressHandler,
                this.progressHandlerInterval,
                vfs ? null : this.vfs,
                this.aggFuncs,
                this.scalarFuncs,
                this.collationFuncs);

        private SQLiteDatabaseConnectionBuilder WithAggregateFunc(string name, int nArg, delegate_function_aggregate_step step, delegate_function_aggregate_final final)
        {
            Contract.Requires(name != null);
            Contract.Requires(nArg >= -1);
            var key = Tuple.Create(name, nArg);

            var scalarFuncs = this.scalarFuncs;
            if (this.scalarFuncs.ContainsKey(key))
            {
                scalarFuncs = new Dictionary<Tuple<string, int>, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue>>(this.scalarFuncs);
                scalarFuncs.Remove(key);
            }

            var aggFuncs = new Dictionary<Tuple<string, int>, Tuple<delegate_function_aggregate_step, delegate_function_aggregate_final>>(this.aggFuncs);
            aggFuncs[key] = Tuple.Create(step, final);

            return new SQLiteDatabaseConnectionBuilder(
                this.fileName,
                this.autoCheckPointCount,
                this.authorizer,
                this.busyTimeout,
                this.commitHook,
                this.connectionFlags,
                this.progressHandler,
                this.progressHandlerInterval,
                this.vfs,
                aggFuncs,
                scalarFuncs,
                this.collationFuncs);
        }

        private sealed class CtxState<T>
        {
            internal T Value { get; }

            internal CtxState(T value)
            {
                this.Value = value;
            }
        }

        private SQLiteDatabaseConnectionBuilder WithAggregateFunc<T>(string name, int nArg, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector)
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

            return this.WithAggregateFunc(name, nArg, funcStep, funcFinal);
        }

        /// <summary>
        /// Add an aggregate function that can accept any number of <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithAggregateFunc<T>(string name, T seed, Func<T, IReadOnlyList<ISQLiteValue>, T> func, Func<T, ISQLiteValue> resultSelector) =>
            this.WithAggregateFunc(name, -1, seed, func, resultSelector);

        /// <summary>
        /// Add an aggregate function that accepts no <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithAggregateFunc<T>(String name, T seed, Func<T, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            return this.WithAggregateFunc(name, 0, seed, (t, _) => func(t), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 1 <see href="ISQLiteValue"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            return this.WithAggregateFunc(name, 1, seed, (t, val) => func(t, val[0]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 2 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            return this.WithAggregateFunc(name, 2, seed, (t, val) => func(t, val[0], val[1]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 3 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            return this.WithAggregateFunc(name, 3, seed, (t, val) => func(t, val[0], val[1], val[2]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 4 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            return this.WithAggregateFunc(name, 4, seed, (t, val) => func(t, val[0], val[1], val[2], val[3]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 5 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            return this.WithAggregateFunc(name, 5, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 6 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            return this.WithAggregateFunc(name, 6, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 7 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            return this.WithAggregateFunc(name, 7, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5], val[6]), resultSelector);
        }

        /// <summary>
        /// Add an aggregate function that accepts 8 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the accumulator value.</typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <remarks>Note: The functions <paramref name="func"/> and <paramref name="resultSelector"/> are assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithAggregateFunc<T>(string name, T seed, Func<T, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, T> func, Func<T, ISQLiteValue> resultSelector)
        {
            Contract.Requires(func != null);
            return this.WithAggregateFunc(name, 8, seed, (t, val) => func(t, val[0], val[1], val[2], val[3], val[4], val[5], val[6], val[7]), resultSelector);
        }

        /// <summary>
        /// Returns a new <see cref="SQLiteDatabaseConnectionBuilder"/> with the provided collation function.
        /// </summary>
        /// <param name="name">The collation name.</param>
        /// <param name="comparison">The collation function.</param>
        /// <seealso href="https://www.sqlite.org/c3ref/create_collation.html"/>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithCollation(string name, Comparison<string> comparison)
        {
            Contract.Requires(name != null);
            Contract.Requires(comparison != null);

            var collationFuncs = new Dictionary<string, Comparison<string>>(this.collationFuncs);
            collationFuncs[name] = comparison;

            return new SQLiteDatabaseConnectionBuilder(
                this.fileName,
                this.autoCheckPointCount,
                this.authorizer,
                this.busyTimeout,
                this.commitHook,
                this.connectionFlags,
                this.progressHandler,
                this.progressHandlerInterval,
                this.vfs,
                this.aggFuncs,
                this.scalarFuncs,
                collationFuncs);
        }

        /// <summary>
        /// Adds a scalar function.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/create_function.html"/>
        /// <param name="name">The function name.</param>
        /// <param name="nArg">The number of arguments the function takes or -1 if it may take any number of arguments.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        private SQLiteDatabaseConnectionBuilder WithScalarFunc(string name, int nArg, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce)
        {
            Contract.Requires(name != null);
            Contract.Requires(reduce != null);
            Contract.Requires(nArg >= -1);

            var key = Tuple.Create(name, nArg);

            var aggFuncs = this.aggFuncs;
            if (this.aggFuncs.ContainsKey(key))
            {
                aggFuncs = new Dictionary<Tuple<string, int>, Tuple<delegate_function_aggregate_step, delegate_function_aggregate_final>>(this.aggFuncs);
                aggFuncs.Remove(key);
            }

            var scalarFuncs = new Dictionary<Tuple<string, int>, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue>>(this.scalarFuncs);
            scalarFuncs[key] = reduce;

            return new SQLiteDatabaseConnectionBuilder(
                this.fileName,
                this.autoCheckPointCount,
                this.authorizer,
                this.busyTimeout,
                this.commitHook,
                this.connectionFlags,
                this.progressHandler,
                this.progressHandlerInterval,
                this.vfs,
                aggFuncs,
                scalarFuncs,
                this.collationFuncs);
        }

        /// <summary>
        /// Adds a scalar function that can accept any number of <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithScalarFunc(string name, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue> reduce) =>
            this.WithScalarFunc(name, -1, reduce);

        /// <summary>
        /// Adds a scalar function that accepts 0 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithScalarFunc(string name, Func<ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            return this.WithScalarFunc(name, 0, _ => reduce());
        }

        /// <summary>
        /// Adds a scalar function that accepts 1 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            return this.WithScalarFunc(name, 1, val => reduce(val[0]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 2 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            return this.WithScalarFunc(name, 2, val => reduce(val[0], val[1]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 3 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            return this.WithScalarFunc(name, 3, val => reduce(val[0], val[1], val[2]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 4 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            return this.WithScalarFunc(name, 4, val => reduce(val[0], val[1], val[2], val[3]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 5 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            return this.WithScalarFunc(name, 5, val => reduce(val[0], val[1], val[2], val[3], val[4]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 6 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            return this.WithScalarFunc(name, 6, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 7 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            return this.WithScalarFunc(name, 7, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5], val[6]));
        }

        /// <summary>
        /// Adds a scalar function that accepts 8 <see href="ISQLiteValue"/> instances.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="reduce">A reduction function.</param>
        /// <remarks>Note: The function <paramref name="reduce"/> is assumed to be pure and their results may be cached and reused.</remarks>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithScalarFunc(string name, Func<ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue, ISQLiteValue> reduce)
        {
            Contract.Requires(reduce != null);
            return this.WithScalarFunc(name, 8, val => reduce(val[0], val[1], val[2], val[3], val[4], val[5], val[6], val[7]));
        }

        /// <summary>
        /// Returns a <see cref="SQLiteDatabaseConnectionBuilder"/> without the specified collation function.
        /// </summary>
        /// <param name="name">The name of the collation function.</param>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithoutCollation(string name)
        {
            Contract.Requires(name != null);

            var collationFuncs = this.collationFuncs;

            if (collationFuncs.ContainsKey(name))
            {
                collationFuncs = new Dictionary<string, Comparison<string>>(this.collationFuncs);
                collationFuncs.Remove(name);
            }

            return new SQLiteDatabaseConnectionBuilder(
                this.fileName,
                this.autoCheckPointCount,
                this.authorizer,
                this.busyTimeout,
                this.commitHook,
                this.connectionFlags,
                this.progressHandler,
                this.progressHandlerInterval,
                this.vfs,
                this.aggFuncs,
                this.scalarFuncs,
                collationFuncs);
        }

        /// <summary>
        /// Returns a <see cref="SQLiteDatabaseConnectionBuilder"/> without the specified function.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="nArg">The arity of the function.</param>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithoutFunc(string name, int nArg)
        {
            Contract.Requires(name != null);
            Contract.Requires(nArg >= -1);

            var key = Tuple.Create(name, nArg);
            var aggFuncs = this.aggFuncs;
            var scalarFuncs = this.scalarFuncs;

            if (aggFuncs.ContainsKey(key))
            {
                aggFuncs = new Dictionary<Tuple<string, int>, Tuple<delegate_function_aggregate_step, delegate_function_aggregate_final>>(this.aggFuncs);
                aggFuncs.Remove(key);
            }

            if (this.scalarFuncs.ContainsKey(key))
            {
                scalarFuncs = new Dictionary<Tuple<string, int>, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue>>(this.scalarFuncs);
                scalarFuncs.Remove(key);
            }

            return new SQLiteDatabaseConnectionBuilder(
                this.fileName,
                this.autoCheckPointCount,
                this.authorizer,
                this.busyTimeout,
                this.commitHook,
                this.connectionFlags,
                this.progressHandler,
                this.progressHandlerInterval,
                this.vfs,
                aggFuncs,
                scalarFuncs,
                this.collationFuncs);
        }

        /// <summary>
        /// Returns a <see cref="SQLiteDatabaseConnectionBuilder"/> without any collation functions.
        /// </summary>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithoutCollations() => 
            new SQLiteDatabaseConnectionBuilder(
                this.fileName,
                this.autoCheckPointCount,
                this.authorizer,
                this.busyTimeout,
                this.commitHook,
                this.connectionFlags,
                this.progressHandler,
                this.progressHandlerInterval,
                this.vfs,
                this.aggFuncs,
                this.scalarFuncs,
                new Dictionary<string, Comparison<string>>());

        /// <summary>
        /// Returns a <see cref="SQLiteDatabaseConnectionBuilder"/> without any aggregate or scalar functions.
        /// </summary>
        /// <returns>A <see cref="SQLiteDatabaseConnectionBuilder"/> instance.</returns>
        public SQLiteDatabaseConnectionBuilder WithoutFuncs() =>
            new SQLiteDatabaseConnectionBuilder(
                this.fileName,
                this.autoCheckPointCount,
                this.authorizer,
                this.busyTimeout,
                this.commitHook,
                this.connectionFlags,
                this.progressHandler,
                this.progressHandlerInterval,
                this.vfs,
                new Dictionary<Tuple<string, int>, Tuple<delegate_function_aggregate_step, delegate_function_aggregate_final>>(),
                new Dictionary<Tuple<string, int>, Func<IReadOnlyList<ISQLiteValue>, ISQLiteValue>>(),
                this.collationFuncs);
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

        /// <summary>
        ///  Causes any pending database operation to abort and return at its earliest opportunity.
        /// </summary>
        /// <seealso href="https://www.sqlite.org/c3ref/interrupt.html"/>
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


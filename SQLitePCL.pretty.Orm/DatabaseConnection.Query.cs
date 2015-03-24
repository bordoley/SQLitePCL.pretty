using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace SQLitePCL.pretty.Orm
{
    public static partial class DatabaseConnection
    {
        /// <summary>
        /// Compiles a SQL query.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="query">The SQL query to compile.</param>
        /// <returns>The compiled statement.</returns>
        public static IStatement PrepareStatement(this IDatabaseConnection This, ISqlQuery query)
        {
            Contract.Requires(This != null);
            Contract.Requires(query != null);
            return This.PrepareStatement(query.ToString());
        }

        /// <summary>
        /// Compiles a SQL query, returning the an <see cref="IEnumerable&lt;T&gt;"/> of rows in the result set.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="query">The SQL statement to compile and Query.</param>
        /// <returns>An <see cref="IEnumerable&lt;T&gt;"/> of rows in the result set.</returns>
        public static IEnumerable<IReadOnlyList<IResultSetValue>> Query(this IDatabaseConnection This, ISqlQuery query)
        {
            Contract.Requires(This != null);
            Contract.Requires(query != null);
            return This.Query(query.ToString());
        }

        /// <summary>
        ///  Compiles a SQL statement with provided bind parameter values,
        ///  returning the an <see cref="IEnumerable&lt;T&gt;"/> of rows in the result set.
        /// </summary>
        /// <param name="This">The database connection.</param>
        /// <param name="query">The SQL statement to compile and Query.</param>
        /// <param name="values">The bind parameter values.</param>
        /// <returns>An <see cref="IEnumerable&lt;T&gt;"/> of rows in the result set.</returns>
        public static IEnumerable<IReadOnlyList<IResultSetValue>> Query(
            this IDatabaseConnection This, ISqlQuery query, params object[] values)
        {
            Contract.Requires(This != null);
            Contract.Requires(query != null);
            Contract.Requires(values != null);
            return This.Query(query.ToString(), values);
        }
    }

    public static partial class AsyncDatabaseConnection
    {
        /// <summary>
        /// Returns a cold observable that compiles a SQL query
        /// that publishes the rows in the result set for each subscription.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="query">The SQL query to compile and Query.</param>
        /// <returns>A cold observable of rows in the result set.</returns>
        public static IObservable<IReadOnlyList<IResultSetValue>> Query(this IAsyncDatabaseConnection This, ISqlQuery query)
        {
            Contract.Requires(This != null);
            Contract.Requires(query != null);
            return This.Query(query.ToString());
        }

        /// <summary>
        /// Returns a cold observable that compiles a SQL query with
        /// provided bind parameter values, that publishes the rows in the result
        /// set for each subscription.
        /// </summary>
        /// <param name="This">The asynchronous database connection.</param>
        /// <param name="query">The SQL query to compile and Query.</param>
        /// <param name="values">The bind parameter values.</param>
        /// <returns>A cold observable of rows in the result set.</returns>
        public static IObservable<IReadOnlyList<IResultSetValue>> Query(
            this IAsyncDatabaseConnection This, ISqlQuery query, params object[] values)
        {
            Contract.Requires(This != null);
            Contract.Requires(query != null);
            Contract.Requires(values != null);
            return This.Query(query.ToString(), values);
        }
    }
}


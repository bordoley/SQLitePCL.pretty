using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace SQLitePCL.pretty.Orm
{
    public static partial class DatabaseConnection
    {
        public static IStatement PrepareStatement(this IDatabaseConnection This, ISqlQuery query)
        {
            Contract.Requires(This != null);
            Contract.Requires(query != null);
            return This.PrepareStatement(query.ToString());
        }

        public static IEnumerable<IReadOnlyList<IResultSetValue>> Query(this IDatabaseConnection This, ISqlQuery query)
        {
            Contract.Requires(This != null);
            Contract.Requires(query != null);
            return This.Query(query.ToString());
        }

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
        public static IObservable<IReadOnlyList<IResultSetValue>> Query(this IAsyncDatabaseConnection This, ISqlQuery query)
        {
            Contract.Requires(This != null);
            Contract.Requires(query != null);
            return This.Query(query.ToString());
        }

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


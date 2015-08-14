using System;
using System.Linq.Expressions;


namespace SQLitePCL.pretty.Orm.Sql
{
    internal static partial class SqlCompiler
    {
        internal static String CompileOffsetClause(this Expression This) =>
            $"OFFSET {This.CompileExpr()}";
    }

    /// <summary>
    /// The OFFSET clause of a SQL query.
    /// </summary>
    public abstract class OffsetClause : ISqlQuery
    {
        private readonly LimitClause limit;
        private readonly string offset;

        internal OffsetClause(LimitClause limit, string offset)
        {
            this.limit = limit;
            this.offset = offset;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.OffsetClause"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.OffsetClause"/>.</returns>
        public override string ToString() =>
            $"{limit}\r\n{offset}";
    }

    /// <summary>
    /// The OFFSET clause of a SQL query.
    /// </summary>
    public sealed class OffsetClause<T> : OffsetClause
    {
        internal OffsetClause(LimitClause limit, string offset) : base(limit, offset)
        {
        }
    }

    /// <summary>
    /// The OFFSET clause of a SQL query.
    /// </summary>
    public sealed class OffsetClause<T1,T2> : OffsetClause
    {
        internal OffsetClause(LimitClause limit, string offset) : base(limit, offset)
        {
        }
    }

    /// <summary>
    /// The OFFSET clause of a SQL query.
    /// </summary>
    public sealed class OffsetClause<T1,T2,T3> : OffsetClause
    {
        internal OffsetClause(LimitClause limit, string offset) : base(limit, offset)
        {
        }
    }

    /// <summary>
    /// The OFFSET clause of a SQL query.
    /// </summary>
    public sealed class OffsetClause<T1,T2,T3,T4> : OffsetClause
    {
        internal OffsetClause(LimitClause limit, string offset) : base(limit, offset)
        {
        }
    }
}


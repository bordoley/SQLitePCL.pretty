using System;
using System.Linq;
using System.Linq.Expressions;

namespace SQLitePCL.pretty.Orm.Sql
{
    internal static partial class SqlCompiler
    {   
        internal static string CompileFromClause(params Type[] types)
        {
            return "FROM " + string.Join(", ", types.Select(x => "\"" + TableMapping.Get(x).TableName + "\""));
        }
    }

    /// <summary>
    /// The FROM clause of a SQL query.
    /// </summary>
    public abstract class FromClause
    {
        private readonly string from;

        internal FromClause(string from)
        {
            this.from = from;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.FromClause"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.FromClause"/>.</returns>
        public override string ToString()
        {
            return from;
        }
    }

    /// <summary>
    /// The FROM clause of a SQL query.
    /// </summary>
    public sealed class FromClause<T> : FromClause
    {
        internal FromClause(string clause) : base(clause)
        {
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        public SelectClause<T> Select()
        {
            return new SelectClause<T>(
                this, 
                SqlCompiler.SelectAllColumnsClause(typeof(T)));
        }
    }

    /// <summary>
    /// The FROM clause of a SQL query.
    /// </summary>
    public sealed class FromClause<T1,T2> : FromClause
    {
        internal FromClause(string clause) : base(clause)
        {
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        public SelectClause<T1,T2> Select()
        {
            return new SelectClause<T1,T2>(
                this, 
                SqlCompiler.SelectAllColumnsClause(typeof(T1), typeof(T2)));
        }
    }

    /// <summary>
    /// The FROM clause of a SQL query.
    /// </summary>
    public sealed class FromClause<T1,T2,T3> : FromClause
    {
        internal FromClause(string clause) : base(clause)
        {
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        public SelectClause<T1,T2,T3> Select()
        {
            return new SelectClause<T1,T2,T3>(
                this, 
                SqlCompiler.SelectAllColumnsClause(typeof(T1), typeof(T2), typeof(T3)));
        }
    }

    /// <summary>
    /// The FROM clause of a SQL query.
    /// </summary>
    public sealed class FromClause<T1,T2,T3,T4> : FromClause
    {
        internal FromClause(string clause) : base(clause)
        {
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        public SelectClause<T1,T2,T3,T4> Select()
        {
            return new SelectClause<T1,T2,T3,T4>(
                this, 
                SqlCompiler.SelectAllColumnsClause(typeof(T1), typeof(T2), typeof(T3), typeof(T4)));
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SQLitePCL.pretty.Orm.Sql
{
    internal static partial class SqlCompiler
    {   
        internal static string CompileJoinClause(bool left, Type table, Expression expr)
        {
            var tableName = TableMapping.Get(table).TableName;
            return (left ? "LEFT JOIN " : "INNER JOIN ") + "\"" + tableName + "\" ON " + expr.CompileExpr();
        }
    }

    /// <summary>
    /// The JOIN clause of a SQL query.
    /// </summary>
    public abstract class JoinClause
    {
        private readonly FromClause from;
        private readonly IReadOnlyList<string> join;

        internal JoinClause(FromClause from, IReadOnlyList<string> join)
        {
            this.from = from;
            this.join = join;
        }

        internal FromClause From { get { return from; } }
        internal IReadOnlyList<string> JoinConstraints { get { return join; } }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.JoinClause"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.Sql.JoinClause"/>.</returns>
        public override string ToString() =>
            from + (join.Count == 0 ? "" : $"\r\n{string.Join("\r\n", join)}");
    }

    internal sealed class JoinClause<T> : JoinClause
    {
        internal JoinClause(FromClause from, IReadOnlyList<string> join) : base(from, join)
        {
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        public SelectClause<T> Select() =>
            new SelectClause<T>(
                this, 
                SqlCompiler.SelectAllColumnsClause(false, typeof(T)));

        /// <summary>
        /// Select all columns from the table returning only distinct rows.
        /// </summary>
        public SelectClause<T> SelectDistinct() =>
            new SelectClause<T>(
                this, 
                SqlCompiler.SelectAllColumnsClause(true, typeof(T)));
    }

    /// <summary>
    /// The JOIN clause of a SQL query.
    /// </summary>
    public sealed class JoinClause<T1,T2> : JoinClause
    {
        internal JoinClause(FromClause from, IReadOnlyList<string> join) : base(from, join)
        {
        }

        public JoinClause<T1,T2,T3> Join<T3>(Expression<Func<T1,T2,T3,bool>> predExpr)
        {
            var joins = new List<String>(this.JoinConstraints);
            joins.Add(SqlCompiler.CompileJoinClause(false, typeof(T3), predExpr));
            return new JoinClause<T1,T2,T3>(this.From, joins);
        }

        public JoinClause<T1,T2,T3> LeftJoin<T3>(Expression<Func<T1,T2,T3,bool>> predExpr)
        {
            var joins = new List<String>(this.JoinConstraints);
            joins.Add(SqlCompiler.CompileJoinClause(true, typeof(T3), predExpr));
            return new JoinClause<T1,T2,T3>(this.From, joins);
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        public SelectClause<T1,T2> Select() =>
            new SelectClause<T1,T2>(
                this, 
                SqlCompiler.SelectAllColumnsClause(false, typeof(T1), typeof(T2)));

        /// <summary>
        /// Select all columns from the table returning only distinct rows.
        /// </summary>
        public SelectClause<T1,T2> SelectDistinct() =>
            new SelectClause<T1,T2>(
                this, 
                SqlCompiler.SelectAllColumnsClause(true, typeof(T1), typeof(T2)));
    }

    /// <summary>
    /// The JOIN clause of a SQL query.
    /// </summary>
    public sealed class JoinClause<T1,T2,T3> : JoinClause
    {
        internal JoinClause(FromClause from, IReadOnlyList<string> join) : base(from, join)
        {
        }

        public JoinClause<T1,T2,T3,T4> Join<T4>(Expression<Func<T1,T2,T3,T4,bool>> predExpr)
        {
            var joins = new List<String>(this.JoinConstraints);
            joins.Add(SqlCompiler.CompileJoinClause(false, typeof(T4), predExpr));
            return new JoinClause<T1,T2,T3,T4>(this.From, joins);
        }

        public JoinClause<T1,T2,T3,T4> LeftJoin<T4>(Expression<Func<T1,T2,T3,T4,bool>> predExpr)
        {
            var joins = new List<String>(this.JoinConstraints);
            joins.Add(SqlCompiler.CompileJoinClause(true, typeof(T4), predExpr));
            return new JoinClause<T1,T2,T3,T4>(this.From, joins);
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        public SelectClause<T1,T2,T3> Select() =>
            new SelectClause<T1,T2,T3>(
                this, 
                SqlCompiler.SelectAllColumnsClause(false, typeof(T1), typeof(T2), typeof(T3)));

        /// <summary>
        /// Select all columns from the table returning only distinct rows.
        /// </summary>
        public SelectClause<T1,T2,T3> SelectDistinct() =>
            new SelectClause<T1,T2,T3>(
                this, 
                SqlCompiler.SelectAllColumnsClause(true, typeof(T1), typeof(T2), typeof(T3)));
    }

    /// <summary>
    /// The JOIN clause of a SQL query.
    /// </summary>
    public sealed class JoinClause<T1,T2,T3,T4> : JoinClause
    {
        internal JoinClause(FromClause from, IReadOnlyList<string> join) : base(from, join)
        {
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        public SelectClause<T1,T2,T3,T4> Select() =>
            new SelectClause<T1,T2,T3,T4>(
                this, 
                SqlCompiler.SelectAllColumnsClause(false, typeof(T1), typeof(T2), typeof(T3), typeof(T4)));

        /// <summary>
        /// Select all columns from the table returning only distinct rows.
        /// </summary>
        public SelectClause<T1,T2,T3,T4> SelectDistinct() =>
            new SelectClause<T1,T2,T3,T4>(
                this, 
                SqlCompiler.SelectAllColumnsClause(true, typeof(T1), typeof(T2), typeof(T3), typeof(T4)));
    }
}


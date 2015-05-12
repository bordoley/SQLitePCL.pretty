using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace SQLitePCL.pretty.Orm.Sql
{
    internal static partial class SqlCompiler
    {   
        internal static string CompileFromClause(params Type[] types) =>
            "FROM " + string.Join(", ", types.Select(x => "\"" + TableMapping.Get(x).TableName + "\""));
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
        public override string ToString() => from;
    }

    /// <summary>
    /// The FROM clause of a SQL query.
    /// </summary>
    public sealed class FromClause<T> : FromClause
    {
        internal FromClause(string clause) : base(clause)
        {
        }

        public JoinClause<T,T2> Join<T2>(Expression<Func<T,T2,bool>> predExpr)
        {
            var join = new List<string>();
            join.Add(SqlCompiler.CompileJoinClause(false, typeof(T2),predExpr));
            return new JoinClause<T,T2>(this, join);
        }

        public JoinClause<T,T2> LeftJoin<T2>(Expression<Func<T,T2,bool>> predExpr)
        {
            var join = new List<string>();
            join.Add(SqlCompiler.CompileJoinClause(true, typeof(T2),predExpr));
            return new JoinClause<T,T2>(this, join);
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        public SelectClause<T> Select() =>
            new JoinClause<T>(this, new List<string>()).Select();

        /// <summary>
        /// Select all columns from the table returning only distinct rows.
        /// </summary>
        public SelectClause<T> SelectDistinct() =>
            new JoinClause<T>(this, new List<string>()).SelectDistinct();
    }

    /// <summary>
    /// The FROM clause of a SQL query.
    /// </summary>
    public sealed class FromClause<T1,T2> : FromClause
    {
        internal FromClause(string clause) : base(clause)
        {
        }

        public JoinClause<T1,T2,T3> Join<T3>(Expression<Func<T1,T2,T3,bool>> predExpr)
        {
            var join = new List<string>();
            join.Add(SqlCompiler.CompileJoinClause(false, typeof(T3),predExpr));
            return new JoinClause<T1,T2,T3>(this, join);
        }

        public JoinClause<T1,T2,T3> LeftJoin<T3>(Expression<Func<T1,T2,T3,bool>> predExpr)
        {
            var join = new List<string>();
            join.Add(SqlCompiler.CompileJoinClause(true, typeof(T3),predExpr));
            return new JoinClause<T1,T2,T3>(this, join);
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        public SelectClause<T1,T2> Select() =>
            new JoinClause<T1,T2>(this, new List<string>()).Select();

        /// <summary>
        /// Select all columns from the table returning only distinct rows.
        /// </summary>
        public SelectClause<T1,T2> SelectDistinct() =>
            new JoinClause<T1,T2>(this, new List<string>()).SelectDistinct();
    }

    /// <summary>
    /// The FROM clause of a SQL query.
    /// </summary>
    public sealed class FromClause<T1,T2,T3> : FromClause
    {
        internal FromClause(string clause) : base(clause)
        {
        }

        public JoinClause<T1,T2,T3,T4> Join<T4>(Expression<Func<T1,T2,T3,T4,bool>> predExpr)
        {
            var join = new List<string>();
            join.Add(SqlCompiler.CompileJoinClause(false, typeof(T4),predExpr));
            return new JoinClause<T1,T2,T3,T4>(this, join);
        }

        public JoinClause<T1,T2,T3,T4> LeftJoin<T4>(Expression<Func<T1,T2,T3,T4,bool>> predExpr)
        {
            var join = new List<string>();
            join.Add(SqlCompiler.CompileJoinClause(true, typeof(T4),predExpr));
            return new JoinClause<T1,T2,T3,T4>(this, join);
        }

        /// <summary>
        /// Select all columns from the table.
        /// </summary>
        public SelectClause<T1,T2,T3> Select() =>
            new JoinClause<T1,T2,T3>(this, new List<string>()).Select();

        /// <summary>
        /// Select all columns from the table returning only distinct rows.
        /// </summary>
        public SelectClause<T1,T2,T3> SelectDistinct() =>
            new JoinClause<T1,T2,T3>(this, new List<string>()).SelectDistinct();
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
        public SelectClause<T1,T2,T3,T4> Select() =>
            new JoinClause<T1,T2,T3,T4>(this, new List<string>()).Select();

        /// <summary>
        /// Select all columns from the table returning only distinct rows.
        /// </summary>
        public SelectClause<T1,T2,T3,T4> SelectDistinct() =>
            new JoinClause<T1,T2,T3,T4>(this, new List<string>()).SelectDistinct();
    }
}


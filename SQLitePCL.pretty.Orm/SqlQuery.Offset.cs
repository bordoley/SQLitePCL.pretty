using System;
namespace SQLitePCL.pretty.Orm
{
    public static partial class SqlQuery
    {
        /// <summary>
        /// The OFFSET clause of a SQL query.
        /// </summary>
        public sealed class OffsetClause<T> : ISqlQuery
        {
            private readonly LimitClause<T> limit;
            private readonly int offset;

            internal OffsetClause(LimitClause<T> limit, int offset)
            {
                this.limit = limit;
                this.offset = offset;
            }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents the current <see cref="OffsetClause&lt;T&gt;"/>.
            /// </summary>
            /// <returns>A <see cref="System.String"/> that represents the current <see cref="OffsetClause&lt;T&gt;"/>.</returns>

            public override string ToString()
            {
                return 
                    limit.ToString() +
                    "\r\nOFFSET " + offset;
            }
        }
    }
}


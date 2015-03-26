using System;

namespace SQLitePCL.pretty.Orm
{
    /// <summary>
    /// Methods that are intended to be used <see cref="SQLitePCL.pretty.Orm.Sql.WhereClause&lt;T&gt;"/> expressions. 
    /// </summary>
    public static class SqlMethods
    {
        /// <summary>
        /// SQLite IS expression.
        /// </summary>
        /// <returns>This method always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <param name="This">This.</param>
        /// <param name="other">Other.</param>
        /// <typeparam name="T">The type.</typeparam>
        public static bool Is<T>(this T This, T other = null)
            where T: class
        {
            throw new NotSupportedException("Function should only be used in SQL expressions.");
        }

        /// <summary>
        /// SQLite IS expression.
        /// </summary>
        /// <returns>This method always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <param name="This">This.</param>
        /// <param name="other">Other.</param>
        /// <typeparam name="T">The type.</typeparam>
        public static bool Is<T>(this Nullable<T> This, Nullable<T> other = null)
            where T: struct
        {
            throw new NotSupportedException("Function should only be used in SQL expressions.");
        }

        /// <summary>
        /// SQLite IS NOT expression.
        /// </summary>
        /// <returns>This method always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <param name="This">This.</param>
        /// <param name="other">Other.</param>
        /// <typeparam name="T">The type.</typeparam>
        public static bool IsNot<T>(this T This, T other = null)
            where T: class
        {
            throw new NotSupportedException("Function should only be used in SQL expressions.");
        }

        /// <summary>
        /// SQLite IS NOT expression.
        /// </summary>
        /// <returns>This method always throws a <see cref="System.NotSupportedException"/>.</returns>
        /// <param name="This">This.</param>
        /// <param name="other">Other.</param>
        /// <typeparam name="T">The type.</typeparam>
        public static bool IsNot<T>(this Nullable<T> This, Nullable<T> other = null)
            where T: struct
        {
            throw new NotSupportedException("Function should only be used in SQL expressions.");
        }
    }
}

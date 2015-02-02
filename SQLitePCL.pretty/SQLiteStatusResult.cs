using System;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// The current and highwater value of a perfromance metric returned by 
    /// either <see cref="SQLite3.Status"/> or  <see cref="IDatabaseConnection.Status"/>.
    /// </summary>
    public struct SQLiteStatusResult : IEquatable<SQLiteStatusResult>, IComparable<SQLiteStatusResult>, IComparable
    {
        /// <summary>
        /// Indicates whether the two SQLiteStatusResult instances are equal to each other.
        /// </summary>
        /// <param name="x">A SQLiteStatusResult instance.</param>
        /// <param name="y">A SQLiteStatusResult instance.</param>
        /// <returns><see langword="true"/> if the two instances are equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator ==(SQLiteStatusResult x, SQLiteStatusResult y)
        {
            return x.Equals(y);
        }

        /// <summary>
        /// Indicates whether the two SQLiteStatusResult instances are not equal each other.
        /// </summary>
        /// <param name="x">A SQLiteStatusResult instance.</param>
        /// <param name="y">A SQLiteStatusResult instance.</param>
        /// <returns><see langword="true"/> if the two instances are not equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator !=(SQLiteStatusResult x, SQLiteStatusResult y)
        {
            return !(x == y);
        }

        /// <summary>
        /// Indicates if the the first SQLiteStatusResult is greater than or equal to the second.
        /// </summary>
        /// <param name="x">A SQLiteStatusResult instance.</param>
        /// <param name="y">A SQLiteStatusResult instance.</param>
        /// <returns><see langword="true"/>if the the first SQLiteStatusResult is greater than or equal to the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(SQLiteStatusResult x, SQLiteStatusResult y)
        {
            return x.CompareTo(y) >= 0;
        }

        /// <summary>
        /// Indicates if the the first SQLiteStatusResult is greater than the second.
        /// </summary>
        /// <param name="x">A SQLiteStatusResult instance.</param>
        /// <param name="y">A SQLiteStatusResult instance.</param>
        /// <returns><see langword="true"/>if the the first SQLiteStatusResult is greater than the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator >(SQLiteStatusResult x, SQLiteStatusResult y)
        {
            return x.CompareTo(y) > 0;
        }

        /// <summary>
        /// Indicates if the the first SQLiteStatusResult is less than or equal to the second.
        /// </summary>
        /// <param name="x">A SQLiteStatusResult instance.</param>
        /// <param name="y">A SQLiteStatusResult instance.</param>
        /// <returns><see langword="true"/>if the the first SQLiteStatusResult is less than or equal to the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(SQLiteStatusResult x, SQLiteStatusResult y)
        {
            return x.CompareTo(y) <= 0;
        }

        /// <summary>
        /// Indicates if the the first SQLiteStatusResult is less than the second.
        /// </summary>
        /// <param name="x">A SQLiteStatusResult instance.</param>
        /// <param name="y">A SQLiteStatusResult instance.</param>
        /// <returns><see langword="true"/>if the the first SQLiteStatusResult is less than the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator <(SQLiteStatusResult x, SQLiteStatusResult y)
        {
            return x.CompareTo(y) < 0;
        }


        private readonly int current;
        private readonly int highwater;

        internal SQLiteStatusResult(int current, int highwater)
        {
            this.current = current;
            this.highwater = highwater;
        }

        /// <summary>
        /// The current value of the performance metric.
        /// </summary>
        public int Current { get { return this.current; } }

        /// <summary>
        /// The highwater value of the performance metric.
        /// </summary>
        public int Highwater { get { return this.highwater; }}

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = this.current;
            hash = hash * 31 + this.highwater;
            return hash;
        }

        /// <inheritdoc/>
        public bool Equals(SQLiteStatusResult other)
        {
            return this.current == other.current && this.highwater == other.highwater;
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return other is SQLiteStatusResult && this == (SQLiteStatusResult)other;
        }

        /// <inheritdoc/>
        public int CompareTo(SQLiteStatusResult other)
        {
            var result = this.current.CompareTo(other.current);
            if (result != 0) { return result; }

            return this.highwater.CompareTo(other.highwater);
        }

        /// <inheritdoc/>
        public int CompareTo(object obj)
        {
            if (obj is SQLiteStatusResult)
            {
                return this.CompareTo((SQLiteStatusResult)obj);
            }
            throw new ArgumentException("Can only compare to other SQLiteStatusResult");
        }
    }
}


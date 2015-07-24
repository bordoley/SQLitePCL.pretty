using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLitePCL.pretty.Orm
{
    /// <summary>
    /// Details about a Table index.
    /// </summary>
    internal sealed class IndexInfo : IEquatable<IndexInfo>
    {
        /// <summary>
        /// Indicates whether the two IndexInfo instances are equal to each other.
        /// </summary>
        /// <param name="x">A IndexInfo instance.</param>
        /// <param name="y">A IndexInfo instance.</param>
        /// <returns><see langword="true"/> if the two instances are equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator ==(IndexInfo x, IndexInfo y) =>
            object.ReferenceEquals(x, null) ? object.ReferenceEquals(y, null) : x.Equals(y);

        /// <summary>
        /// Indicates whether the two IndexInfo instances are not equal each other.
        /// </summary>
        /// <param name="x">A IndexInfo instance.</param>
        /// <param name="y">A IndexInfo instance.</param>
        /// <returns><see langword="true"/> if the two instances are not equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator !=(IndexInfo x, IndexInfo y) =>
            !(x == y);

        /// <summary>
        /// Whether the index is unique or not.
        /// </summary>
        public bool Unique { get; }

        /// <summary>
        /// The columns that compose the index in order.
        /// </summary>
        public IEnumerable<string> Columns { get; }

        internal IndexInfo(bool unique, IReadOnlyList<string> columns)
        {
            this.Unique = unique;
            this.Columns = columns;
        }

        /// <summary>
        /// Determines whether the specified <see cref="SQLitePCL.pretty.Orm.IndexInfo"/> is equal to the current <see cref="SQLitePCL.pretty.Orm.IndexInfo"/>.
        /// </summary>
        /// <param name="other">The <see cref="SQLitePCL.pretty.Orm.IndexInfo"/> to compare with the current <see cref="SQLitePCL.pretty.Orm.IndexInfo"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="SQLitePCL.pretty.Orm.IndexInfo"/> is equal to the current
        /// <see cref="SQLitePCL.pretty.Orm.IndexInfo"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(IndexInfo other) =>
            (this.Unique == other.Unique) &&
                // FIXME: Ideally we use immutable lists that implement correct equality semantics
                (this.Columns.SequenceEqual(other.Columns));

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to the current <see cref="SQLitePCL.pretty.Orm.IndexInfo"/>.
        /// </summary>
        /// <param name="other">The <see cref="System.Object"/> to compare with the current <see cref="SQLitePCL.pretty.Orm.IndexInfo"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to the current
        /// <see cref="SQLitePCL.pretty.Orm.IndexInfo"/>; otherwise, <c>false</c>.</returns>
        public override bool Equals(object other) =>
            other is IndexInfo && this == (IndexInfo)other;

        /// <summary>
        /// Serves as a hash function for a <see cref="SQLitePCL.pretty.Orm.IndexInfo"/> object.
        /// </summary>
        /// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a
        /// hash table.</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + this.Unique.GetHashCode();

            // FIXME: This sucks. Prefer to use actual immutable collections.
            foreach (var col in this.Columns)
            {
                hash =  hash * 31 + col.GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.IndexInfo"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.IndexInfo"/>.</returns>
        public override string ToString() =>
            string.Format("[IndexInfo: Unique={0}, Columns={1}", this.Unique, string.Join(", ", this.Columns));
    }
}


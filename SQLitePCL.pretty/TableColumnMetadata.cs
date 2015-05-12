/*
   Copyright 2014 David Bordoley

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Metadata about a specific column of a database table
    /// </summary>
    public sealed class TableColumnMetadata : IEquatable<TableColumnMetadata>, IComparable<TableColumnMetadata>, IComparable
    {
        /// <summary>
        /// Indicates whether the two TableColumnMetadata instances are equal to each other.
        /// </summary>
        /// <param name="x">A TableColumnMetadata instance.</param>
        /// <param name="y">A TableColumnMetadata instance.</param>
        /// <returns><see langword="true"/> if the two instances are equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator ==(TableColumnMetadata x, TableColumnMetadata y)
        {
            if (object.ReferenceEquals(x, null))
            {
                 return object.ReferenceEquals(y, null);
            }

            return x.Equals(y);
        }

        /// <summary>
        /// Indicates whether the two TableColumnMetadata instances are not equal each other.
        /// </summary>
        /// <param name="x">A TableColumnMetadata instance.</param>
        /// <param name="y">A TableColumnMetadata instance.</param>
        /// <returns><see langword="true"/> if the two instances are equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator !=(TableColumnMetadata x, TableColumnMetadata y) =>
            !(x == y);

        /// <summary>
        /// Indicates if the the first TableColumnMetadata is greater than or equal to the second.
        /// </summary>
        /// <param name="x">A TableColumnMetadata instance.</param>
        /// <param name="y">A TableColumnMetadata instance.</param>
        /// <returns><see langword="true"/> if the the first TableColumnMetadata is greater than or equal to the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(TableColumnMetadata x, TableColumnMetadata y) =>
            x.CompareTo(y) >= 0;

        /// <summary>
        /// Indicates if the the first TableColumnMetadata is greater than the second.
        /// </summary>
        /// <param name="x">A TableColumnMetadata instance.</param>
        /// <param name="y">A TableColumnMetadata instance.</param>
        /// <returns><see langword="true"/> if the the first TableColumnMetadata is greater than the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator >(TableColumnMetadata x, TableColumnMetadata y) =>
            x.CompareTo(y) > 0;

        /// <summary>
        /// Indicates if the the first TableColumnMetadata is less than or equal to the second.
        /// </summary>
        /// <param name="x">A TableColumnMetadata instance.</param>
        /// <param name="y">A TableColumnMetadata instance.</param>
        /// <returns><see langword="true"/> if the the first TableColumnMetadata is less than or equal to the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(TableColumnMetadata x, TableColumnMetadata y) =>
            x.CompareTo(y) <= 0;

        /// <summary>
        /// Indicates if the the first TableColumnMetadata is less than the second.
        /// </summary>
        /// <param name="x">A TableColumnMetadata instance.</param>
        /// <param name="y">A TableColumnMetadata instance.</param>
        /// <returns><see langword="true"/> if the the first TableColumnMetadata is less than the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator <(TableColumnMetadata x, TableColumnMetadata y) =>
            x.CompareTo(y) < 0;


        internal TableColumnMetadata(string declaredType, string collationSequence, bool hasNotNullConstraint, bool isPrimaryKeyPart, bool isAutoIncrement)
        {
            this.DeclaredType = declaredType;
            this.CollationSequence = collationSequence;
            this.HasNotNullConstraint = hasNotNullConstraint;
            this.IsPrimaryKeyPart = isPrimaryKeyPart;
            this.IsAutoIncrement = isAutoIncrement;
        }

        /// <summary>
        /// Returns the declared type of a column or null if no type is declared.
        /// </summary>
        public string DeclaredType { get; }

        /// <summary>
        /// Name of the default collation sequence.
        /// </summary>
        public string CollationSequence { get; }
        /// <summary>
        /// True if column has a NOT NULL constraint.
        /// </summary>
        public bool HasNotNullConstraint { get; }

        /// <summary>
        /// True if column is part of the PRIMARY KEY.
        /// </summary>
        public bool IsPrimaryKeyPart { get; }

        /// <summary>
        /// True if column is AUTOINCREMENT.
        /// </summary>
        public bool IsAutoIncrement { get; }

        /// <inheritdoc/>
        public bool Equals(TableColumnMetadata other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.DeclaredType == other.DeclaredType &&
                   this.CollationSequence == other.CollationSequence &&
                   this.HasNotNullConstraint == other.HasNotNullConstraint &&
                   this.IsPrimaryKeyPart == other.IsPrimaryKeyPart &&
                   this.IsAutoIncrement == other.IsAutoIncrement;
        }

        /// <inheritdoc/>
        public override bool Equals(object other) =>
            other is TableColumnMetadata && this == (TableColumnMetadata)other;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + this.DeclaredType.GetHashCode();
            hash = hash * 31 + this.CollationSequence.GetHashCode();
            hash = hash * 31 + this.HasNotNullConstraint.GetHashCode();
            hash = hash * 31 + this.IsPrimaryKeyPart.GetHashCode();
            hash = hash * 32 + this.IsAutoIncrement.GetHashCode();
            return hash;
        }

        /// <inheritdoc/>
        public int CompareTo(TableColumnMetadata other)
        {
            if (!Object.ReferenceEquals(other, null))
            {
                var result = String.CompareOrdinal(this.DeclaredType, other.DeclaredType);
                if (result != 0) { return result; }

                result = String.CompareOrdinal(this.CollationSequence, other.CollationSequence);
                if (result != 0) { return result; }

                result = this.HasNotNullConstraint.CompareTo(other.HasNotNullConstraint);
                if (result != 0) { return result; }

                result = this.IsPrimaryKeyPart.CompareTo(other.IsPrimaryKeyPart);
                if (result != 0) { return result; }

                return this.IsAutoIncrement.CompareTo(other.IsAutoIncrement);
            }
            else
            {
                return 1;
            }
        }

        /// <inheritdoc/>
        public int CompareTo(object obj)
        {
            if (System.Object.ReferenceEquals(obj, null))
            {
                return 1;
            }

            return this.CompareTo((TableColumnMetadata)obj);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.TableColumnMetadata"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.TableColumnMetadata"/>.</returns>
        public override string ToString() =>
            string.Format("[TableColumnMetadata: DeclaredType={0}, CollationSequence={1}, HasNotNullConstraint={2}, IsPrimaryKeyPart={3}, IsAutoIncrement={4}]", DeclaredType, CollationSequence, HasNotNullConstraint, IsPrimaryKeyPart, IsAutoIncrement);
    }
}

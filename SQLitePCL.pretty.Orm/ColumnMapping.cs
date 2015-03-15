using System;
using System.Reflection;

namespace SQLitePCL.pretty.Orm
{
    /// <summary>
    /// Provides the mapping between a columns SQL and CLR representation.
    /// </summary>
    public sealed class ColumnMapping: IEquatable<ColumnMapping>
    {
        /// <summary>
        /// Indicates whether the two ColumnMapping instances are equal to each other.
        /// </summary>
        /// <param name="x">A ColumnMapping instance.</param>
        /// <param name="y">A ColumnMapping instance.</param>
        /// <returns><see langword="true"/> if the two instances are equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator ==(ColumnMapping x, ColumnMapping y)
        {
            return x.Equals(y);
        }

        /// <summary>
        /// Indicates whether the two ColumnMapping instances are not equal each other.
        /// </summary>
        /// <param name="x">A ColumnMapping instance.</param>
        /// <param name="y">A ColumnMapping instance.</param>
        /// <returns><see langword="true"/> if the two instances are not equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator !=(ColumnMapping x, ColumnMapping y)
        {
            return !(x == y);
        }

        private readonly Type clrType;
        private readonly PropertyInfo property;
        private readonly TableColumnMetadata metadata;

        internal ColumnMapping(Type clrType, PropertyInfo property, TableColumnMetadata metadata)
        {
            this.clrType = clrType;
            this.property = property;
            this.metadata = metadata;
        }

        /// <summary>
        /// The CLR <see cref="Type"/> of the column.
        /// </summary>
        public Type ClrType { get { return clrType; } }

        /// <summary>
        /// The <see cref="PropertyInfo"/> of the column.
        /// </summary>
        public PropertyInfo Property { get { return property; } }

        /// <summary>
        /// The <see cref="TableColumnMetadata"/> of the column. 
        /// </summary>
        public TableColumnMetadata Metadata { get { return metadata; } }

        /// <inheritdoc/>
        public bool Equals(ColumnMapping other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.ClrType == other.ClrType &&
                this.Property == other.Property &&
                this.Metadata == other.Metadata;
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return other is ColumnMapping && this == (ColumnMapping)other;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + this.ClrType.GetHashCode();
            hash = hash * 31 + this.Property.GetHashCode();
            hash = hash * 31 + this.Metadata.GetHashCode();
            return hash;
        }
    }

}


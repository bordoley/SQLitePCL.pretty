using System;
using System.Reflection;

namespace SQLitePCL.pretty.Orm
{
    /// <summary>
    /// Provides the mapping between a column's SQL and CLR representation.
    /// </summary>
    internal sealed class ColumnMapping: IEquatable<ColumnMapping>
    {
        /// <summary>
        /// Indicates whether the two ColumnMapping instances are equal to each other.
        /// </summary>
        /// <param name="x">A ColumnMapping instance.</param>
        /// <param name="y">A ColumnMapping instance.</param>
        /// <returns><see langword="true"/> if the two instances are equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator ==(ColumnMapping x, ColumnMapping y)
        {
            if (object.ReferenceEquals(x, null))
            {
                 return object.ReferenceEquals(y, null);
            }

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
        private readonly object defaultValue;
        private readonly PropertyInfo property;
        private readonly TableColumnMetadata metadata;
        private readonly ForeignKeyConstraint foreignKeyConstraint;

        internal ColumnMapping(Type clrType, object defaultValue, PropertyInfo property, TableColumnMetadata metadata, ForeignKeyConstraint foreignKeyConstraint)
        {
            this.clrType = clrType;
            this.defaultValue = defaultValue;
            this.property = property;
            this.metadata = metadata;
            this.foreignKeyConstraint = foreignKeyConstraint;
        }

        /// <summary>
        /// The CLR <see cref="Type"/> of the column.
        /// </summary>
        public Type ClrType { get { return clrType; } }


        /// <summary>
        /// The default value of the ClrType as specified by the user.
        /// </summary>
        public object DefaultValue { get { return defaultValue; } }

        /// <summary>
        /// The <see cref="PropertyInfo"/> of the column.
        /// </summary>
        public PropertyInfo Property { get { return property; } }

        /// <summary>
        /// The <see cref="TableColumnMetadata"/> of the column. 
        /// </summary>
        public TableColumnMetadata Metadata { get { return metadata; } }

        /// <summary>
        /// Gets the foreign key constraint on the column or null.
        /// </summary>
        public ForeignKeyConstraint ForeignKeyConstraint { get { return foreignKeyConstraint; } }

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
                (this.DefaultValue == null ? (other.DefaultValue == null) : this.DefaultValue.Equals(other.defaultValue)) &&
                this.Property == other.Property &&
                this.Metadata == other.Metadata &&
                (this.ForeignKeyConstraint == null) ? (other.ForeignKeyConstraint == null) : this.ForeignKeyConstraint.Equals(other.ForeignKeyConstraint);
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
            hash = hash * 31 + (this.DefaultValue ?? 0).GetHashCode();
            hash = hash * 31 + this.Property.GetHashCode();
            hash = hash * 31 + this.Metadata.GetHashCode();
            hash = hash * 31 + (this.ForeignKeyConstraint ?? ((object) 0)).GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.ColumnMapping"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="SQLitePCL.pretty.Orm.ColumnMapping"/>.</returns>
        public override string ToString()
        {
            return string.Format("[ColumnMapping: ClrType={0}, Property={1}, Metadata={2}]", ClrType, Property, Metadata);
        }
    }

}


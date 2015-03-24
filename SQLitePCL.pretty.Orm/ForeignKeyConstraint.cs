using System;

namespace SQLitePCL.pretty.Orm
{
    /// <summary>
    /// Foreign key constraint table, column name tuple.
    /// </summary>
    internal sealed class ForeignKeyConstraint : IEquatable<ForeignKeyConstraint>
    {
        /// <summary>
        /// Indicates whether the two ForeignKeyConstraint instances are equal to each other.
        /// </summary>
        /// <param name="x">A ForeignKeyConstraint instance.</param>
        /// <param name="y">A ForeignKeyConstraint instance.</param>
        /// <returns><see langword="true"/> if the two instances are equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator ==(ForeignKeyConstraint x, ForeignKeyConstraint y)
        {
            if (object.ReferenceEquals(x, null))
            {
                 return object.ReferenceEquals(y, null);
            }

            return x.Equals(y);
        }

        /// <summary>
        /// Indicates whether the two ForeignKeyConstraint instances are not equal each other.
        /// </summary>
        /// <param name="x">A ForeignKeyConstraint instance.</param>
        /// <param name="y">A ForeignKeyConstraint instance.</param>
        /// <returns><see langword="true"/> if the two instances are not equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator !=(ForeignKeyConstraint x, ForeignKeyConstraint y)
        {
            return !(x == y);
        }

        private readonly string tableName;
        private readonly string columnName;

        internal ForeignKeyConstraint(string tableName, string columnName)
        {
            this.tableName = tableName;
            this.columnName = columnName;
        }

        /// <summary>
        /// Gets the foreign key constraing table name.
        /// </summary>
        public string TableName { get { return tableName; } }

        /// <summary>
        /// Gets the foreign key constraing column name.
        /// </summary>
        public string ColumnName { get { return columnName; } }

        /// <inheritdoc/>
        public bool Equals(ForeignKeyConstraint other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.TableName == other.TableName &&
                    this.ColumnName == other.ColumnName;
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return other is ForeignKeyConstraint && this == (ForeignKeyConstraint)other;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + this.TableName.GetHashCode();
            hash = hash * 31 + this.ColumnName.GetHashCode();
            return hash;
        }
    }
}


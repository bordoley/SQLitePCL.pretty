using System;

namespace SQLitePCL.pretty.Orm
{
    /*
    internal sealed class Ordering : IEquatable<Ordering>
    {
        public static bool operator ==(Ordering x, Ordering y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Ordering x, Ordering y)
        {
            return !(x == y);
        }

        private readonly string _columnName;
        private readonly bool _ascending;

        internal Ordering(string columnName, bool ascending)
        {
            _columnName = columnName;
            _ascending = ascending;
        }

        public string ColumnName { get { return _columnName; } }
        public bool Ascending { get { return _ascending; }  }

        /// <inheritdoc/>
        public bool Equals(Ordering other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.ColumnName == other.ColumnName &&
                this.Ascending == other.Ascending;
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return other is Ordering && this == (Ordering)other;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + this.Ascending.GetHashCode();
            hash = hash * 31 + this.ColumnName.GetHashCode();
            return hash;
        }
    } */
}


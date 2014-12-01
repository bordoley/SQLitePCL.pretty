using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLitePCL.pretty
{
    /// <summary>
    /// Represents information about a single column in <see cref="IStatement"/> result set.
    /// </summary>
    public sealed class ColumnInfo : IEquatable<ColumnInfo>, IComparable<ColumnInfo>, IComparable
    {
        /// <summary>
        /// Indicates whether the two ColumnInfo instances are equal to each other.
        /// </summary>
        /// <param name="x">A ColumnInfo instance.</param>
        /// <param name="y">A ColumnInfo instance.</param>
        /// <returns><see langword="true"/> if the two instances are equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator ==(ColumnInfo x, ColumnInfo y)
        {
            return x.Equals(y);
        }

        /// <summary>
        /// Indicates whether the two ColumnInfo instances are not equal each other.
        /// </summary>
        /// <param name="x">A ColumnInfo instance.</param>
        /// <param name="y">A ColumnInfo instance.</param>
        /// <returns><see langword="true"/> if the two instances are not equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator !=(ColumnInfo x, ColumnInfo y)
        {
            return !(x == y);
        }

        /// <summary>
        /// Indicates if the the first ColumnInfo is greater than or equal to the second.
        /// </summary>
        /// <param name="x">A ColumnInfo instance.</param>
        /// <param name="y">A ColumnInfo instance.</param>
        /// <returns><see langword="true"/> if the the first ColumnInfo is greater than or equal to the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(ColumnInfo x, ColumnInfo y)
        {
            switch (x.CompareTo(y))
            {
                case -1:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Indicates if the the first ColumnInfo is greater than the second.
        /// </summary>
        /// <param name="x">A ColumnInfo instance.</param>
        /// <param name="y">A ColumnInfo instance.</param>
        /// <returns><see langword="true"/> if the the first ColumnInfo is greater than the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator >(ColumnInfo x, ColumnInfo y)
        {
            switch (x.CompareTo(y))
            {
                case 1:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Indicates if the the first ColumnInfo is less than or equal to the second.
        /// </summary>
        /// <param name="x">A ColumnInfo instance.</param>
        /// <param name="y">A ColumnInfo instance.</param>
        /// <returns><see langword="true"/> if the the first ColumnInfo is less than or equal to the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(ColumnInfo x, ColumnInfo y)
        {
            switch (x.CompareTo(y))
            {
                case 1:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Indicates if the the first SQLiteVersion is less than the second.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/> if the the first SQLiteVersion is less than the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator <(ColumnInfo x, ColumnInfo y)
        {
            switch (x.CompareTo(y))
            {
                case -1:
                    return true;
                default:
                    return false;
            }
        }

        internal static ColumnInfo Create(StatementImpl stmt, int index)
        {
            return new ColumnInfo(
                raw.sqlite3_column_name(stmt.sqlite3_stmt, index),
                raw.sqlite3_column_database_name(stmt.sqlite3_stmt, index),
                raw.sqlite3_column_origin_name(stmt.sqlite3_stmt, index),
                raw.sqlite3_column_table_name(stmt.sqlite3_stmt, index),
                raw.sqlite3_column_decltype(stmt.sqlite3_stmt, index));
        }

        private readonly string name;
        private readonly string databaseName;
        private readonly string tableName;
        private readonly string originName;
        private readonly string declaredType;

        internal ColumnInfo(string name, string databaseName, string originName, string tableName, string declaredType)
        {
            this.name = name;
            this.databaseName = databaseName;
            this.originName = originName;
            this.tableName = tableName;
            this.declaredType = declaredType;
        }

        /// <summary>
        /// The column name.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/column_name.html"/>
        public string Name { get { return name; } }

        /// <summary>
        /// The database that is the origin of this particular result column.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/column_database_name.html"/>
        public string DatabaseName { get { return databaseName; } }

        /// <summary>
        /// The column that is the origin of this particular result column.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/column_database_name.html"/>
        public string OriginName { get { return originName; }}

        /// <summary>
        ///  The table that is the origin of this particular result column.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/column_database_name.html"/>
        public string TableName { get { return tableName; }}

        /// <summary>
        /// Returns the declared type of a column in a result set or null if no type is declared.
        /// </summary>
        /// <seealso href="https://sqlite.org/c3ref/column_decltype.html"/>
        public string DeclaredType { get { return declaredType; } }

        /// <inheritdoc/>
        public bool Equals(ColumnInfo other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Name == other.Name &&
                this.DatabaseName == other.DatabaseName &&
                this.OriginName == other.OriginName &&
                this.TableName == other.TableName &&
                this.DeclaredType == other.DeclaredType;
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return other is ColumnInfo && this == (ColumnInfo)other;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + this.Name.GetHashCode();
            hash = hash * 31 + this.DatabaseName.GetHashCode();
            hash = hash * 31 + this.OriginName.GetHashCode();
            hash = hash * 31 + this.TableName.GetHashCode();
            hash = hash * 32 + this.DeclaredType.GetHashCode();
            return hash;
        }

        /// <inheritdoc/>
        public int CompareTo(ColumnInfo other)
        {
            if (!Object.ReferenceEquals(other, null))
            {
                var result = this.Name.CompareTo(other.Name);
                if (result != 0) { return result; }
                
                result = this.DatabaseName.CompareTo(other.DatabaseName);
                if (result != 0) { return result; }

                result = this.TableName.CompareTo(other.TableName);
                if (result != 0) { return result; }

                result = this.OriginName.CompareTo(other.OriginName);
                if (result != 0) { return result; }

                return this.DeclaredType.CompareTo(other.DeclaredType);
            }
            else 
            {
                return 1;
            }
        }

        /// <inheritdoc/>
        int IComparable.CompareTo(object obj)
        {
            if (System.Object.ReferenceEquals(obj, null))
            {
                return 1;
            }

            return this.CompareTo((ColumnInfo)obj);
        }
    }
}

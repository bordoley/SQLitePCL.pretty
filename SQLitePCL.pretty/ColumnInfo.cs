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
    // FIXME: Implement IComparable
    public struct ColumnInfo : IEquatable<ColumnInfo> //IComparable<ColumnInfo>, IComparable
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

        internal static ColumnInfo Create(StatementImpl stmt, int index)
        {
            return new ColumnInfo(
                raw.sqlite3_column_name(stmt.sqlite3_stmt, index),
                raw.sqlite3_column_database_name(stmt.sqlite3_stmt, index),
                raw.sqlite3_column_origin_name(stmt.sqlite3_stmt, index),
                raw.sqlite3_column_table_name(stmt.sqlite3_stmt, index));
        }

        private readonly string name;
        private readonly string databaseName;
        private readonly string originName;
        private readonly string tableName;

        internal ColumnInfo(string name, string databaseName, string originName, string tableName)
        {
            this.name = name;
            this.databaseName = databaseName;
            this.originName = originName;
            this.tableName = tableName;
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

        /// <inheritdoc/>
        public bool Equals(ColumnInfo other)
        {
            return this.Name == other.Name &&
                this.DatabaseName == other.DatabaseName &&
                this.OriginName == other.OriginName &&
                this.TableName == other.TableName;
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
            return hash;
        }
    }
}

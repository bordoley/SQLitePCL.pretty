/*
   Copyright 2014 David Bordoley
   Copyright 2014 Zumero, LLC

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
    /// The SQLite numeric version number.
    /// </summary>
    /// <seealso href="https://sqlite.org/c3ref/c_source_id.html"/>
    public struct SQLiteVersion : IEquatable<SQLiteVersion>, IComparable<SQLiteVersion>, IComparable
    {
        /// <summary>
        /// Indicates whether the two SQLiteVersion instances are equal to each other.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/> if the two instances are equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator ==(SQLiteVersion x, SQLiteVersion y) =>
            x.Equals(y);

        /// <summary>
        /// Indicates whether the two SQLiteVersion instances are not equal each other.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/> if the two instances are not equal to each other; otherwise,  <see langword="false"/>.</returns>
        public static bool operator !=(SQLiteVersion x, SQLiteVersion y) =>
            !(x == y);

        /// <summary>
        /// Indicates if the the first SQLiteVersion is greater than or equal to the second.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/>if the the first SQLiteVersion is greater than or equal to the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator >=(SQLiteVersion x, SQLiteVersion y) =>
            x.version >= y.version;

        /// <summary>
        /// Indicates if the the first SQLiteVersion is greater than the second.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/>if the the first SQLiteVersion is greater than the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator >(SQLiteVersion x, SQLiteVersion y) =>
            x.version > y.version;

        /// <summary>
        /// Indicates if the the first SQLiteVersion is less than or equal to the second.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/>if the the first SQLiteVersion is less than or equal to the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator <=(SQLiteVersion x, SQLiteVersion y) =>
            x.version <= y.version;

        /// <summary>
        /// Indicates if the the first SQLiteVersion is less than the second.
        /// </summary>
        /// <param name="x">A SQLiteVersion instance.</param>
        /// <param name="y">A SQLiteVersion instance.</param>
        /// <returns><see langword="true"/>if the the first SQLiteVersion is less than the second; otherwise, <see langword="false"/>.</returns>
        public static bool operator <(SQLiteVersion x, SQLiteVersion y) =>
            x.version < y.version;

        internal static SQLiteVersion Of(int version)
        {
            // FIXME: If made public in the future, add contracts on the version number.
            return new SQLiteVersion(version);
        }

        private readonly int version;

        private SQLiteVersion(int version)
        {
            this.version = version;
        }

        /// <summary>
        /// Gets the major version number.
        /// </summary>
        public int Major
        {
            get
            {
                return version / 1000000;
            }
        }

        /// <summary>
        /// Gets the minor version number.
        /// </summary>
        public int Minor
        {
            get
            {
                return (version / 1000) % 1000;
            }
        }

        /// <summary>
        /// Gets the release version number.
        /// </summary>
        public int Release
        {
            get
            {
                return version % 1000;
            }
        }

        /// <summary>
        /// Converts the version number as an integer with the value (Major*1000000 + Minor*1000 + Release).
        /// </summary>
        /// <returns>The version number as an integer</returns>
        public int ToInt() =>  version;

        /// <inheritdoc/>
        public override string ToString() =>
            $"{this.Major}.{this.Minor}.{this.Release}";

        /// <inheritdoc/>
        public override int GetHashCode() => version;

        /// <inheritdoc/>
        public bool Equals(SQLiteVersion other) =>
            this.version == other.version;

        /// <inheritdoc/>
        public override bool Equals(object other) =>
            other is SQLiteVersion && this == (SQLiteVersion)other;

        /// <inheritdoc/>
        public int CompareTo(SQLiteVersion other) =>
            this.version.CompareTo(other.version);

        /// <inheritdoc/>
        public int CompareTo(object obj)
        {
            if (obj is SQLiteVersion)
            {
                return this.CompareTo((SQLiteVersion)obj);
            }
            throw new ArgumentException("Can only compare to other SQLiteVersion");
        }
    }
}

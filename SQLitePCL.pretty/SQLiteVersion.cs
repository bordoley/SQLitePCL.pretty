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
    public struct SQLiteVersion : IEquatable<SQLiteVersion>, IComparable<SQLiteVersion>, IComparable
    {
        public static bool operator ==(SQLiteVersion x, SQLiteVersion y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(SQLiteVersion x, SQLiteVersion y)
        {
            return !(x == y);
        }

        internal static SQLiteVersion Of(int version)
        {
            int release = version % 1000;
            int minor = (version / 1000) % 1000;
            int major = version / 1000000;
            return new SQLiteVersion(major, minor, release);
        }

        private readonly int major;
        private readonly int minor;
        private readonly int release;

        private SQLiteVersion(int major, int minor, int release)
        {
            this.major = major;
            this.minor = minor;
            this.release = release;
        }

        public int Major
        {
            get
            {
                return major;
            }
        }

        public int Minor
        {
            get
            {
                return minor;
            }
        }

        public int Release
        {
            get
            {
                return release;
            }
        }

        public int ToInt()
        {
            return (this.Major * 1000000 + this.Minor * 1000 + this.Release);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", major, minor, release);
        }

        public override int GetHashCode()
        {
            return this.ToInt();
        }

        public bool Equals(SQLiteVersion other)
        {
            return this.Major == other.Major &&
                this.Minor == other.Minor &&
                this.Release == other.Release;
        }

        public override bool Equals(object other)
        {
            return other is SQLiteVersion && this == (SQLiteVersion)other;
        }

        public int CompareTo(SQLiteVersion other)
        {
            return this.ToInt().CompareTo(other.ToInt());
        }

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

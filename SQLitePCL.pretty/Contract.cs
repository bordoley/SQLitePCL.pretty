using System;

namespace SQLitePCL.pretty
{
    internal static class Contract
    {
        public static void Requires(bool test)
        {
            if (!test)
                throw new ArgumentException();
        }
    }
}


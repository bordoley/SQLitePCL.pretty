using System;

namespace SQLitePCL.pretty.tests
{
    public abstract class TestBase
    {
#if WIN
        private static readonly Lazy<bool> _initialized = new Lazy<bool>(() =>
        {
            SQLitePCL.Batteries.Init();
            return true;
        });
        public TestBase ()
        {
            if (!_initialized.Value)
            {
                
            }
        }
#endif
    }
}

using System;

namespace SQLitePCL.pretty.tests
{
    public abstract class TestBase
    {
#if WIN
        private static readonly Lazy<bool> _initialized = new Lazy<bool>(() =>
        {
            SQLitePCL.Batteries_V2.Init();
            return true;
        });

        protected TestBase ()
        {
            if (!_initialized.Value)
            {
                
            }
        }
#endif
    }
}

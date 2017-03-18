namespace SQLitePCL.pretty.tests
{
    public abstract class TestBase
    {
        static TestBase()
        {
            SQLitePCL.Batteries.Init();
        }
    }
}

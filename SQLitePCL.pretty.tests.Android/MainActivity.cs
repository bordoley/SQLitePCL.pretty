using SQLitePCL;
using System;
using System.Reflection;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Xunit.Sdk;
using Xunit.Runners.UI;

namespace SQLitePCL.pretty.tests.Android
{
    [Activity(Label = "xUnit Android Runner", MainLauncher = true)]
    public class MainActivity : RunnerActivity
    {
        static MainActivity()
        {
            SQLitePCL.Batteries.Init();
        }

        protected override void OnCreate(Bundle bundle)
        {
            AddTestAssembly(typeof(SQLitePCL.pretty.tests.SQLiteDatabaseConnectionTests).Assembly);
            // or in any assembly that you load (since JIT is available)

#if false
			// you can use the default or set your own custom writer (e.g. save to web site and tweet it ;-)
			Writer = new TcpTextWriter ("10.0.1.2", 16384);
			// start running the test suites as soon as the application is loaded
			AutoStart = true;
			// crash the application (to ensure it's ended) and return to springboard
			TerminateAfterExecution = true;
#endif
            // you cannot add more assemblies once calling base
            base.OnCreate(bundle);
        }
    }
}


SQLitePCL.pretty
================
*A pretty face on top of [SQLitePCL.raw](http://github.com/ericsink/SQLitePCL.raw).* 

# What is this?

This library wraps the C like SQLiteAPI provided by SQLitePCL.raw with a friendly C# object oriented API. 

# Why is it called SQLitePCL.pretty?

SQLitePCL.raw includes a set of extension methods in a package called SQLitePCL.ugly used to make writing unit tests easier, but with no intent of providing an interface "The C# Way". This API is the logical compliment. It's "pretty" and meant to provide an API that C# developers will find familiar and can easily consume.

# How do I add SQLitePCL.pretty to my project?

Use the NuGet packages:
* [SQLitePCL.pretty](http://www.nuget.org/packages/SQLitePCL.pretty/)
* [SQlitePCL.pretty.Async](http://www.nuget.org/packages/SQLitePCL.pretty.Async/)

# API Overview

* SQLite3 - This is the main static class you use to obtain database connections. It also provides APIs for obtaining the SQLite version, compile options used, and information about SQLite memory use.

* IDatabaseConnection - The main interface for interacting with a SQLite database. You use this class to prepare and execute SQL statements. In addition, numerous extension methods are provided to make writing queries easier. This class also supports several advance SQLite features:
  * Registering collation, aggregate and scalar functions written in managed code.
  * Rollback and update event tracking using C# events.
  * Trace and profiling events.
  * The ability to register a commit hook.
  * Support for streaming data to and from SQLite using the .NET Stream interface.
   
* SQLiteDatabaseConnection - A concrete implementation of IDatabaseConnection exposed in the API in order to enable database backups. 

* IStatement - This interface is used to bind parameters and to enumerate the result set of a SQL query. Its a lower level interface that you rarely need to use in practice but is available if needed.

* ISQLiteValue - This interface is used to wrap SQLite dynamically typed values which are used in result sets as well as in aggregate and scalar functions. 

* IResultSetValue - A subclass of ISQLiteValue that includes additional result set specific details about a value, including the database, table and column names of the value.

* IDatabaseBackup - Interface to SQLite's backup API.

* SQLiteException - An exception wrapper around SQLite's error codes.

* SQLiteVersion - A struct that wraps the SQLite numeric version.

# Let me see an example
```
using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("I'm a byte stream")))
using (var db = SQLite3.Open(":memory:"))
{
    db.ExecuteAll(
        @"CREATE TABLE foo (w int, x float, y string, z blob);
          INSERT INTO foo (w,x,y,z) VALUES (0, 0, '', null);");

    db.Execute("INSERT INTO foo (w, x, y, z) VALUES (?, ?, ?, ?)", 1, 1.1, "hello", stream);

    var dst = db.Query("SELECT rowid, z FROM foo where rowid = ?", db.LastInsertedRowId)
                .Select(row => db.OpenBlob(row[1], row[0].ToInt64(), true))
                .First();

    using (dst) { stream.CopyTo(dst); }

    foreach (var row in db.Query("SELECT rowid, * FROM foo"))
    {
        Console.Write(
                    row[0].ToInt64() + ": " +
                    row[1].ToInt() + ", " +
                    row[2].ToInt64() + ", " +
                    row[3].ToString() + ", ");

        if (row[4].SQLiteType == SQLiteType.Null)
        {
            Console.Write("null\n");
            continue;
        }

        using (var blob = db.OpenBlob(row[4], row[0].ToInt64()))
        {
            var str = new StreamReader(blob).ReadToEnd();
            Console.Write(str + "\n");
        }
    }
}
```


Additionally, you can take a look at the [unit tests](http://github.com/bordoley/SQLitePCL.pretty/tree/master/SQLitePCL.pretty.tests) for more examples.

# Thats great and all, but I'm a writing a mobile app and can't block the UI thread

In that case, be sure to include SQLitePCL.pretty.Async in your project, and checkout the following example

```
using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("I'm a byte stream")))
using (var db = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
{
    await db.ExecuteAllAsync(
        @"CREATE TABLE foo (w int, x float, y string, z blob);
          INSERT INTO foo (w,x,y,z) VALUES (0, 0, '', null);");

    await db.ExecuteAsync("INSERT INTO foo (w, x, y, z) VALUES (?, ?, ?, ?)", 1, 1.1, "hello", stream);

    var rowId = await db.Query("SELECT rowid, z FROM foo where y = 'hello'", row => row[0].ToInt64()).FirstAsync();

    var dst = await db.OpenBlobAsync("main", "foo", "z", rowId, true);

    using (dst) { await stream.CopyToAsync(dst); }

    await db.Query("SELECT rowid, * FROM foo", row =>
        row[0].ToInt64() + ": " +
        row[1].ToInt() + ", " +
        row[2].ToInt64() + ", " +
        row[3].ToString() + ", " +
        row[4].ToString()).Do(str => { Console.WriteLine(str); });
}
```

SQLitePCL.pretty
================
*A pretty face on top of [SQLitePCL.raw](https://github.com/ericsink/SQLitePCL.raw).* 

# What is this?

This library wraps the C like SQLiteAPI provided by SQLitePCL.raw with a friendly C# object oriented API. 

# Why is it called SQLitePCL.pretty?

SQLitePCL.raw includes a set of extension methods in a package called SQLitePCL.ugly used to make writing unit tests easier, but with no intent of providing an interface "The C# Way". This API is the logical compliment. It's "pretty" and meant to provide an API that C# developers will find familiar and can easily consume.

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
using (var db = SQLite3.Open(":memory:"))
{
    db.ExecuteAll(
        @"CREATE TABLE foo (w int, x float, y string, z blob);
          INSERT INTO foo (w,x,y,z) VALUES (0, 0, '', null);");

    var stream = new MemoryStream(Encoding.UTF8.GetBytes("I'm a byte stream"));

    db.Execute("INSERT INTO foo (w, x, y, z) VALUES (?, ?, ?, ?)", 1, 1.1, "hello", stream);

    foreach (var row in db.Query("SELECT rowid, z FROM foo where rowid = ?", db.LastInsertedRowId))
    {
        using (var dst = db.OpenBlob(row[1], row[0].ToInt64(), true))
        {
            stream.CopyTo(dst);
        }
    }

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
        }
        else
        {
            using (var blob = db.OpenBlob(row[4], row[0].ToInt64()))
            {
                var str = new StreamReader(blob).ReadToEnd();
                Console.Write(str + "\n");
            }
        }
    }

    stream.Dispose();
}
```


Additionally, you can take a look at the [unit tests](http://github.com/bordoley/SQLitePCL.pretty/tree/master/SQLitePCL.pretty.tests) for more examples.

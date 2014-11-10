SQLitePCL.pretty
================
*A pretty face on top of [SQLitePCL.raw](https://github.com/ericsink/SQLitePCL.raw).* 

# What is this?

This library wraps the C like SQLiteAPI provided by SQLitePCL.raw with a C# friendly object oriented API. 

# Why is it called SQLitePCL.pretty?

SQLitePCL.raw includes a set of extension methods in a package called SQLitePCL.ugly used to make writing unit tests easier, but with no intent of providing an interface "The C# Way". This API is the logical compliment. It's "pretty" and meant to provide an API targetted that C# developers can easily consume.

# API Overview

* SQLite3 - This is the main static class you use to obtain database connections. It also provides APIs for obtaining the SQLite version, compile options used, and information about SQLite memory use.

* IDatabaseConnection - The main interface for interacting with a SQLite database. You use this class to prepare and execute SQL statements. In addition, numerous extension methods are provided to make writing queries easier. This class also supports several advance SQLite features:
  * Registering collation, aggregate and scalar functions written in managed code.
  * Rollback and update event tracking using C# events.
  * Trace and profiling events.
  * The ability to register a commit hook.
  * Support for streaming data to and from SQLite using the .NET Stream interface.

* IStatement -

* ISQLiteValue -

* IResultSetValue -

* IDatabaseBackup -

* SQLiteException -

* SQLiteVersion -

# Let me see some examples?

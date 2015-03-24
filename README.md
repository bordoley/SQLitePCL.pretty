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
* [SQLitePCL.pretty.Orm](https://www.nuget.org/packages/SQLitePCL.pretty.Orm/)

# How stable is this project?

While the code is well tested, the API is currently in flux. Users should expect binary incompatible changes until version 0.1.0 or higher is released. Once that milestone is achieved, the project will switch fully to semantic versioning, and only major version revisions will include breaking API changes.

# API Overview

[Complete (more or less...) API documentation] (http://bordoley.github.io/SQLitePCL.pretty.Documentation/Help/html/N_SQLitePCL_pretty.htm).


# Let me see an example
```CSharp
using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("I'm a byte stream")))
using (var db = SQLite3.Open(":memory:"))
{
    db.ExecuteAll(
        @"CREATE TABLE foo (w int, x float, y string, z blob);
            INSERT INTO foo (w,x,y,z) VALUES (0, 0, '', null);");

    db.Execute("INSERT INTO foo (w, x, y, z) VALUES (?, ?, ?, ?)", 1, 1.1, "hello", stream);

    var dst = db.Query("SELECT rowid, z FROM foo where rowid = ?", db.LastInsertedRowId)
                .Select(row => db.OpenBlob(row[1].ColumnInfo, row[0].ToInt64(), true))
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

        using (var blob = db.OpenBlob(row[4].ColumnInfo, row[0].ToInt64(), false))
        {
            var str = new StreamReader(blob).ReadToEnd();
            Console.Write(str + "\n");
        }
    }
}
```

Additionally, you can take a look at the [unit tests](http://github.com/bordoley/SQLitePCL.pretty/tree/master/SQLitePCL.pretty.tests) for more examples.

# Thats great and all, but I'm a writing a mobile app and can't block the UI thread

In that case, be sure to include SQLitePCL.pretty.Async in your project, and checkout the following example:

```CSharp
using (var db = SQLite3.Open(":memory:").AsAsyncDatabaseConnection())
using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("I'm a byte stream")))
{
    await db.ExecuteAllAsync(
        @"CREATE TABLE foo (w int, x float, y string, z blob);
          INSERT INTO foo (w,x,y,z) VALUES (0, 0, '', null);");

    await db.ExecuteAsync("INSERT INTO foo (w, x, y, z) VALUES (?, ?, ?, ?)", 1, 1.1, "hello", stream);

    var dst =
        await db.Query("SELECT rowid, z FROM foo where y = 'hello'")
                .Select(row => db.OpenBlobAsync(row[1].ColumnInfo, row[0].ToInt64(), true))
                .FirstAsync()
                .ToTask()
                .Unwrap();

    using (dst)
    {
        await stream.CopyToAsync(dst);
    }

    await db.Query("SELECT rowid, * FROM foo")
            .Select(row =>
                row[0].ToInt64() + ": " +
                row[1].ToInt() + ", " +
                row[2].ToInt64() + ", " +
                row[3].ToString() + ", " +
                row[4].ToString())
            .Do(str => { Console.WriteLine(str); });
}
```

# But I absolutely must have an ORM

SQLitePCL.pretty has a very simple table mapping ORM, available in the SQLitePCL.pretty.Orm package on nuget. It supports inserting both new and existing objects, and finding and deleting objects by primary key, from SQLite database tables. Notably, the ORM is designed to make working with immutable data types much easier by supporting the builder pattern for deserializing database objects. Below is a simplish example:

```CSharp

// Per thread builder to limit the number of instances.
private static readonly ThreadLocal<TestObject.Builder> testObjectBuilder = new 
        ThreadLocal<TestObject.Builder>(() => new TestObject.Builder());

public void Example()
{
    // You must provide a result selector to convert a result set row into an object.
    // By making this an explicit parameter, SQLitePCL.pretty can support immutable object builders.
    var resultSelector = 
        ResultSet.RowToObject(
            () => testObjectBuilder.Value, 
            o => ((TestObject.Builder)o).Build());

    using (var db = SQLite3.OpenInMemory())
    {
        // Creates the table and indexes if they do not yet exist.
        db.InitTable<TestObject>();

        // Insert a new object into the database that has nullable values.
        // InsertOrReplace returns the resulting object in the database along with
        // its new primary key and any default values assigned by the database.
        var inserted = 
            db.InsertOrReplace(
                new TestObject.Builder() { Value = "Hello" }.Build(), 
                resultSelector);

        // Lookup the object in the database.
        TestObject found;
        if (!db.TryFind(inserted.Id.Value, resultSelector, out found))
        {
            throw new Exception("item not found");
        }

        // We can use linq like syntax to write queries against the database.
        var query = 
            SqlQuery.From<TestObject>()
                    .Select()
                    // This creates a named bind parameter ":value"
                    .Where<string>((x, value) => x.Value.Contains(value))
                    .OrderBy(x => x.Value)
                    .Take(100)
                    .Skip(0);

        using (var stmt = db.PrepareStatement(query))
        {
            stmt.BindParameters[":value"].Bind("ell");
            // This will throw if no results are returned
            stmt.Query().First();
        }

        // Delete the object from the database.
        TestObject deleted;
        if (!db.TryDelete(inserted.Id.Value, resultSelector, out deleted))
        {
            throw new Exception("deletion failed");
        }
    }
}


public sealed class TestObject : IEquatable<TestObject>
{
    public class Builder
    {
        private long? id;
        private string value;

        public long? Id { get { return id; } set { this.id = value; } }
        public string Value { get { return value; } set { this.value = value; } }

        public TestObject Build()
        {
            return new TestObject(id, value);
        }
    }

    private long? id;
    private string value; 

    private TestObject(long? id, string value)
    {
        this.id = id;
        this.value = value;
    }

    [PrimaryKey]
    public long? Id { get { return id; } }

    public string Value { get { return value; } }

    public bool Equals(TestObject other)
    {
        return this.Id == other.Id && 
            this.Value == other.Value;
    }

    public override bool Equals(object other)
    {
        return other is TestObject && this.Equals((TestObject)other);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + this.Id.GetHashCode();
        hash = hash * 31 + this.Value.GetHashCode();

        return hash;
    }
}
```

# How does this compare to...
## [SQLitePCL.raw](https://github.com/ericsink/SQLitePCL.raw)

SQLitePCL.raw provides a very thin C# wrapper ontop of the SQLite C API. The API exposed by SQLitePCL.raw is downright hostile from an application developer perspective, but is designed for use as a common portable layer upon which friendlier wrappers can be built. SQLitePCL.pretty is one such wrapper. 

## [SQLitePCL](https://sqlitepcl.codeplex.com/)

SQLitePCL is the original PCL library release by MSOpenTech. Stylistically, this library is most similar to SQLitePCL.pretty. There are some key differences though:

* SQLitePCL.pretty is portable to more platforms due to the dependency on SQLitePCL.raw
* SQLitePCL.pretty support many additional SQLite features such as functions, collation and events.
* SQLitePCL.pretty has a much nicer well thought out API. This is subjective, of course, but for reference compare SQLitePCL.pretty.IStatement to SQLitePCL.ISQLiteStatement
* SQLitePCL.pretty supports blob read and writes using a .NET Stream 

## [SQLite-Net](https://github.com/praeclarum/sqlite-net)

SQLite-Net provides an ORM similar to the one provided by SQLitePCL.pretty to manipulate database objects. SQLite-NET is exceptionally well tested in the wild, and should generally be your default choice when being conservative. On the converse, SQLitePCL.pretty has extensive unit test coverage and is supports many newer features available in more recent SQLite versions. 

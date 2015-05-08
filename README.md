# Brief for .net
Brief for .NET provides a simpler interface to retrieve and map sets of data from SQL server by hiding a lot of the boilerplate code required by ADO.NET.

##Examples:

First, we initialize a **"Manager"**

```csharp
using Brief;
```
```csharp
  var cs = new ConnectionString("[connection string]");
  var man = new Manager(cs);
```

Let's retrieve a dataset from a stored procedure
```csharp
var dataTable = man.With(new StoredProcedure("stored procedure name")).Get<DataTable>();
```

The **“With()”** method accepts:
*“Query”* objects, *“StoredProcedure”* objects and .NET’s *"SqlCommand"* objects to return an instance of *"ManagerActions"*.

```csharp
var actions = man.With(new Query("SELECT ID, FirstName, LastName, Age FROM tbl_Sample WHERE Age > 20"));

var dataSet = actions.Get<DataSet>();
var firstRow = actions.Get<DataRow>();
var rowCollection = actions.Get<DataRowCollection>();

```
 > Note: Query and StoredProcedure objects implement implicit casting to SqlCommand.

Brief uses reflection to map data to public properties; so, let’s assume we have the following model:
```csharp
    public class User
    {
        [MapTo("ID")]
        public int Number { set; get; }
        public string FirstName { set; get; }
        public string LastName { set; get; }
        public int Age { set; get; }

    }
```
Getting a list of "User" objects:

```csharp
 var actions = man.With(new Query("SELECT ID, FirstName, LastName, Age FROM tbl_Sample WHERE Age > 20"));
 
 var user = actions.GetListOf<User>();
```

If the public property name and the column name in the result dataset match the data will be mapped but you can also use the ```[MapTo(“column name”)]``` property to customize the property names of your model.

To map a single record user:
```csharp
var user = man.With(new Query("SELECT * FROM tbl_Sample WHERE ID = 1")).Get<User>();
```

Retrieving data by using the SqlReader object:
```csharp
var action = man.With(new Query("SELECT FirstName, LastName FROM tbl_Sample"));
            action.ReadInto(r =>
            {
                Console.WriteLine("{0} {1}",r["FirstName"],r["LastName"]);
            });
```


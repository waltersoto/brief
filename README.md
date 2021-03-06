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

Let's retrieve a DataTable from a stored procedure
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

To map a single record:
```csharp
var user = man.With(new Query("SELECT * FROM tbl_Sample WHERE ID = 1")).Get<User>();
```

Retrieve data by using the SqlReader object:
```csharp
var action = man.With(new Query("SELECT FirstName, LastName FROM tbl_Sample"));

 action.ReadInto(r =>  {
                Console.WriteLine("{0} {1}",r["FirstName"],r["LastName"]);
        });
```
Retrieve data by using the SqlReader object and map it to a model:
```csharp
  man.With(new Query("SELECT FirstName, LastName FROM tbl_Sample"))
         .ReadInto<User>(u => {
            Console.WriteLine("{0} {1}",u.FirstName,u.LastName);
          });
```

Executing a list of commands as a “transaction”:
```csharp
 man.Transaction(new List<SqlCommand> {
           new Query("query 1"),
           new Query("query 1"),
           new StoredProcedure("name")
           {
             Parameters = { {"@Par","Value"},
                           { "@Par2","Value"} }
           } 
        }, r => {
            Console.WriteLine($"Row(s) affected {r}");
        });
```

>Note: The “Transaction()” method will automatically commit or rollback (if an exception occurred)

##Other methods:

```csharp
 var actions = man.With(new SqlCommand("some command"));

 //ExecuteScalar
  object obj = actions.Scalar(); 

  //Cast the ExecuteScalar object to a differnt type
  int intResult = actions.ScalarTo<int>();

  //Get output parameters (if present)
  var outputList = actions.OutputParameters();

  //Cast the value of output parameters as other type.
  var outputAsString = actions.OutputParametersAs<string>();

  //Get return parameter (if present)
  var returnValue = actions.ReturnParameter();
  
  //Cast the value of the return parameter as other type.
  var returnValueAsInt = actions.ReturnParameterAs<int>();
 
  //ExecuteNonQuery
  int affectedRows = actions.Execute(); 
  
```


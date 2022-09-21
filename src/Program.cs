using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration;
using Dapper;
using Dapper.FluentMap;
using Dapper.FluentMap.Dommel;
using Dommel;
using MassTransit;
using static System.Console;
using Seekatar.Tools;
using System.Text.Json;
using Loyal.Core.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

#pragma warning disable CS8321 // unused fn

//============================== worker fns
void insertIntKeyDapper(IDbConnection conn, ParentWithInt c)
{
    var sql = "INSERT INTO IntKey (S,I) VALUES (@S,@I)";

    var affectedRows = conn.Execute(sql, new {S = $"Dapper at {DateTime.Now}", c.I});

    WriteLine($"Inserted {affectedRows} into IntKey");
}

void insertGuidKeyDapper(IDbConnection conn, ParentWithGuid c)
{
    var sql = "INSERT INTO Guid (Id, S, I) VALUES (@Id, @S,@I)";

    var affectedRows = conn.Execute(sql, new {Id = NewId.NextGuid(), S = $"Dapper at {DateTime.Now}", c.I});

    // WriteLine($"Dapper Inserted {affectedRows} into GuidKey");
}

void bulkInsertGuidKeyDapper(IDbConnection conn, int count)
{
    var sql = new StringBuilder("INSERT INTO Guid (Id, S, I) VALUES");
    var parms = new DynamicParameters();
    for (var i = 0; i < count; i++)
    {
        sql.Append($"(@guid{i}, @s{i}, @i{i}),");
        parms.Add($"@guid{i}", Guid.NewGuid(), DbType.Guid);
        parms.Add($"@s{i}", $"It's {i}");
        parms.Add($"@i{i}", i);
    }

    var affectedRows = conn.Execute(sql.ToString().Trim(','), parms);

    WriteLine($"Dapper Bulk Inserted {affectedRows} into GuidKey");
}

void bulkInsert<T>(IDbConnection conn, string insert, IEnumerable<T> items, Action<DynamicParameters, T, int> append, IDbTransaction? trans = null, int chunkSize = 10)
    where T : class
{
    var affectedRows = 0;
    var i = 0;
    var parts = insert.Split("VALUES (");
    if (parts.Length < 2)
    {
        throw new Exception("Invalid insert statement");
    }
    
    var sql = new StringBuilder($"{parts[0]} VALUES");
    var parms = new DynamicParameters();
    var values = $"({parts[1]},";
    
    foreach (var item in items)
    {
        append(parms, item, i);
        sql.Append(string.Format(values, i));

        if (++i < chunkSize)
            continue;

        conn.Execute(sql.ToString().Trim(','), parms, trans);
        
        sql.Clear();
        sql.Append($"{parts[0]} VALUES");
        parms = new DynamicParameters();
        i = 0;
    }

    if (i > 0)
    {
        conn.Execute(sql.ToString().Trim(','), parms, trans);
    }
}

void insertIntKeyDommel(IDbConnection conn, ParentWithInt c)
{
    var key = conn.Insert(c);

    c.S = $"Dommel at {DateTime.Now}";

    WriteLine($"Inserted key to IntKey is {key}");
}

void insertGuidKeyDommel(IDbConnection conn, ParentWithGuid c)
{
    c.S = $"Dommel at {DateTime.Now}";
    var key = conn.Insert(c);
    c.Id = (key as Guid?) ?? throw new Exception("ow!");

    WriteLine($"Inserted key is to GuidKey is {key}");
}

void insertChildFor(IDbConnection connection, ParentWithGuid parentWithGuid)
{
    var kid = new Child() {ParentWithGuidId = parentWithGuid.Id, ChildName = DateTime.Now.ToString()};
    connection.Insert(kid);
}

//============================== main
//============================== main
//============================== main
//============================== main

// add IConfiguration
var configuration = new ConfigurationBuilder()
    .AddSharedDevSettings()
    .AddJsonFile("appsettings.json", true, true)
    .AddEnvironmentVariables()
    .Build();

// add Serilog
var serviceCollection = new ServiceCollection();
Log.Logger = new LoggerConfiguration()
    .ReadFrom
    .Configuration(configuration)
    .CreateLogger();

serviceCollection.AddLogging(configure => configure.AddSerilog());
var provider = serviceCollection.BuildServiceProvider();
var logger = provider.GetService<ILogger<DbConnectionEx>>();
if (logger is null)
{
    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    logger = loggerFactory.CreateLogger<DbConnectionEx>();
    if (logger is null) throw new Exception("Can't create logger");
}




var ioptions = Options.Create(new DbOptions()
{
    MasterConnectionString = configuration.GetConnectionString("SqlServer")
});

using var dbconnection = new DbConnectionEx(ioptions);
var connection = dbconnection.Connection;


// initials maps for table names mainly
FluentMapper.Initialize(config =>
{
    config.AddMap(new IntMap());
    config.AddMap(new GuidMap());
    config.AddMap(new ChildMap());
    config.ForDommel();
});

// add this to allow the Guid to come back from the insert
DommelMapper.AddSqlBuilder(typeof(SqlConnection), new GuidSqlServerSqlBuilder());


var items = new List<ParentWithGuid>();
for (int i = 0; i < 500; i++)
{
    items.Add(new ParentWithGuid() {I = i, Id = NewId.NextGuid(), S = $"It is {i}"});
}


// bulkInsertGuidKeyDapper(connection, 10);

var sw = Stopwatch.StartNew();
foreach ( var i in items) {
    insertGuidKeyDapper(connection, i);
}
WriteLine($"Inserts took {sw.ElapsedMilliseconds}ms");
  
    
sw.Restart();
bulkInsert(connection, "INSERT INTO Guid (Id, S, I) VALUES (@guid{0}, @s{0}, @i{0})", items, (parms, item, i) =>
{
    parms.Add($"@guid{i}", NewId.NextGuid(), DbType.Guid);
    parms.Add($"@s{i}", item.S);
    parms.Add($"@i{i}", item.I);
}, null, 12);
WriteLine($"Bulk insert no transaction took {sw.ElapsedMilliseconds}ms");

var trans = connection.BeginTransaction();
sw.Restart();
bulkInsert(connection, "INSERT INTO Guid (Id, S, I) VALUES (@guid{0}, @s{0}, @i{0})", items, (parms, item, i) =>
{
    parms.Add($"@guid{i}", NewId.NextGuid(), DbType.Guid);
    parms.Add($"@s{i}", item.S);
    parms.Add($"@i{i}", item.I);
}, trans, 12);
trans.Commit();
WriteLine($"Bulk insert with transaction took {sw.ElapsedMilliseconds}ms");


return;

var parentWithInt = new ParentWithInt() {S = $"test at {DateTime.Now}", I = 1};
var parentWithGuid = new ParentWithGuid() {S = $"test at {DateTime.Now}", I = 1};

var loop = 1;
var childLoop = 10;
var testDommel = true;
if (args.Count() > 0)
    int.TryParse(args[0], out loop);
if (args.Count() > 1 && bool.TryParse(args[1], out var testDapper))
    testDommel = false;

var s = "select I, 'last' as last FROM GuidKey";
var result = await connection.QueryAsync<ParentWithGuid, ParentWithGuid, ParentWithGuid>(s, (p1, p2) =>
{
    p1.S = "this works!";
    return p1;
}, splitOn: "last");

WriteLine("Results #1!");
foreach (var r in result)
{
    WriteLine($"   {r.S}, {r.I}");
}

// this gets The type 'String' is not supported for SQL literals.
var sql = "SELECT '{=clientId}' as ClientId";
var expiredMessages = await connection.QueryAsync<ParentWithGuid>(sql, new {clientId = "123456"});
WriteLine("Results!");
foreach (var r in expiredMessages)
{
    WriteLine($"   {r}");
}

return;

var deleteMe = new List<Guid>();

for (int i = 0; i < loop; i++)
{
    if (testDommel)
    {
        insertIntKeyDommel(connection, parentWithInt);
        insertGuidKeyDommel(connection, parentWithGuid);
        for (int j = 0; j < childLoop; j++)
        {
            insertChildFor(connection, parentWithGuid);
        }

        WriteLine($"Added {childLoop} kids");
        deleteMe.Add(parentWithGuid.Id);
    }
    else
    {
        insertIntKeyDapper(connection, parentWithInt);
        insertGuidKeyDapper(connection, parentWithGuid);
    }
}

WriteLine($"Looped {loop} times");

if (testDommel)
{
    parentWithGuid.I = 123;
    connection.Update(parentWithGuid);
    WriteLine($"Updated {parentWithGuid.Id}");

    var x = connection.FirstOrDefault<ParentWithGuid>(p => p.Id == parentWithGuid.Id);
    WriteLine($"For updated parent, I is {x?.I}");

    // for this to work, the Child class must implement IEquatable
    var parent = connection.FirstOrDefault<ParentWithGuid, Child, ParentWithGuid>(p => p.Id == parentWithGuid.Id);
    WriteLine($"Parent has {parent?.Children.Count} kids");
    WriteLine(JsonSerializer.Serialize(parent, new JsonSerializerOptions() {WriteIndented = true}));

    deleteMe = deleteMe.Skip(1).ToList();
    connection.DeleteMultiple<ParentWithGuid>(o => deleteMe.Contains(o.Id));
    WriteLine($"Deleted all parent but first parent");
}
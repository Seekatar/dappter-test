using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using Dapper.FluentMap;
using Dapper.FluentMap.Dommel;
using Dommel;
using MassTransit;
using static System.Console;
using Seekatar.Tools;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

#pragma warning disable CS8321 // unused fn

//============================== worker fns
void insertIntKeyDapper(IDbConnection conn, ParentWithInt c)
{
    var sql = "INSERT INTO IntKey (S,I) VALUES (@S,@I)";

    var affectedRows = conn.Execute(sql, new { S = $"Dapper at {DateTime.Now}", c.I });

    WriteLine($"Inserted {affectedRows} into IntKey");
}

void insertGuidKeyDapper(IDbConnection conn, ParentWithGuid c)
{
    var sql = "INSERT INTO Guid (Id, S, I) VALUES (@Id, @S,@I)";

    var affectedRows = conn.Execute(sql, new { Id = NewId.NextGuid(), S = $"Dapper at {DateTime.Now}", c.I });

    WriteLine($"Dapper Inserted {affectedRows} into GuidKey");
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
    var kid = new Child() { ParentWithGuidId = parentWithGuid.Id, ChildName = DateTime.Now.ToString() };
    connection.Insert(kid);
}

void testSelect(DbConnection connection)
{
    const string sql = "select /**select**/ from GuidKeys /**where**/";

    var builder = new SqlBuilder();
    var q = "test";
    builder.AddTemplate(sql);
    // works, but has injection issues
    builder.Select($"'{q}' as S");
    // doesn't work builder.Select("'@q' as S", new { q = "test"});
    builder.Select("I");

    builder.Where("I = @I", new { I = 123 });

    var builderTemplate = builder.AddTemplate("Select /**select**/ from GuidKey /**where**/ ");

    foreach( var p in connection.Query<ParentWithGuid>(builderTemplate.RawSql, builderTemplate.Parameters))
    {
        WriteLine($" >> {p.S} {p.I}");
    }

}
//============================== main
//============================== main
//============================== main
//============================== main

WriteLine($"We are in {Directory.GetCurrentDirectory()}");
var configuration = new ConfigurationBuilder()
            .AddSharedDevSettings()
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables()
            .Build();

using var connection = new SqlConnection(configuration.GetConnectionString("SqlServer"));

// initials maps for table names mainly
FluentMapper.Initialize(config =>
    {
        config.AddMap(new IntMap());
        config.AddMap(new GuidMap());
        config.AddMap(new ChildMap());

        config.AddMap(new ClientMap());

        config.ForDommel();
    });

// add this to allow the Guid to come back from the insert
// DommelMapper.AddSqlBuilder(typeof(SqlConnection), new GuidSqlServerSqlBuilder());


var parentWithInt = new ParentWithInt() { S = $"test at {DateTime.Now}", I = 1 };
var parentWithGuid = new ParentWithGuid() { S = $"test at {DateTime.Now}", I = 1 };

var loop = 1;
var childLoop = 10;
var testDommel = true;
if (args.Count() > 0)
    int.TryParse(args[0], out loop);
if (args.Count() > 1 && bool.TryParse(args[1], out var testDapper))
    testDommel = false;

var deleteMe = new List<Guid>();
#if GuidTest
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

testSelect(connection);
#endif

if (testDommel)
{
    var client = new Client() { ClientId = 1234, Name = "test", Description = DateTime.Now.ToString() };
    connection.Insert(client);

    parentWithGuid.I = 123;
    connection.Update(parentWithGuid);
    WriteLine($"Updated {parentWithGuid.Id}");

    var x = connection.FirstOrDefault<ParentWithGuid>(p => p.Id == parentWithGuid.Id);
    WriteLine($"For updated parent, I is {x?.I}");

    // for this to work, the Child class must implement IEquatable
    // var parent = connection.FirstOrDefault<ParentWithGuid, Child, ParentWithGuid>(p => p.Id == parentWithGuid.Id);
    // WriteLine($"Parent has {parent?.Children.Count} kids");
    // WriteLine(JsonSerializer.Serialize(parent, new JsonSerializerOptions() { WriteIndented = true }));

    deleteMe = deleteMe.Skip(1).ToList();
    connection.DeleteMultiple<ParentWithGuid>(o => deleteMe.Contains(o.Id));
    WriteLine($"Deleted all parent but first parent");
}

class Client  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)] // w/o this using latest code get error cannot insert NULL into ClientId
    public int ClientId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

class ClientWithId : Client
{
    public int Id { get; set; }
}
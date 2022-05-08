using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using Dapper;
using Dapper.FluentMap;
using Dapper.FluentMap.Dommel;
using Dapper.FluentMap.Dommel.Mapping;
using Dommel;
using MassTransit;
using static System.Console;

#pragma warning disable CS8321 // unused fn

var connString = "Server=localhost;Database=dapper;User Id=sa;Password=Passw0rd!;";

void insertIntKeyDapper(DbConnection conn, ParentWithInt c)
{
    var sql = "INSERT INTO IntKey (S,I) VALUES (@S,@I)";

    var affectedRows = conn.Execute(sql, new { S = $"Dapper at {DateTime.Now}", c.I });

    WriteLine($"Affected Rows: {affectedRows}");
}

void insertIntKeyDommel(DbConnection conn, ParentWithInt c)
{
    var key = conn.Insert(c);
    c.S = $"Dommel at {DateTime.Now}";
    WriteLine($"Inserted key is: {key}");
}

void insertGuidKeyDommel(DbConnection conn, ParentWithGuid c)
{
    c.S = $"Dommel at {DateTime.Now}";
    var key = conn.Insert(c);
    c.Id = (key as Guid?) ?? throw new Exception("ow!");

    WriteLine($"Inserted key is: {key}. C.Id is {c.Id}");
}

void insertGuidDapper(DbConnection conn, ParentWithGuid c)
{
    var sql = "INSERT INTO Guid (Id, S, I) VALUES (@Id, @S,@I)";

    var affectedRows = conn.Execute(sql, new { Id = NewId.NextGuid(), S = $"Dapper at {DateTime.Now}", c.I });
    WriteLine($"Affected Rows: {affectedRows}");
}

void insertChildFor(SqlConnection connection, ParentWithGuid parentWithGuid)
{
    var kid = new Child() { ParentWithGuidId = parentWithGuid.Id, ChildName = DateTime.Now.ToString() };
    connection.Insert(kid);
}

//============================== main

using var connection = new SqlConnection(connString);

var parentWithInt = new ParentWithInt() { S = $"test at {DateTime.Now}", I = 1 };
var parentWithGuid = new ParentWithGuid() { S = $"test at {DateTime.Now}", I = 1 };

FluentMapper.Initialize(config =>
    {
        config.AddMap(new IntMap());
        config.AddMap(new GuidMap());
        config.AddMap(new ChildMap());
        config.ForDommel();
    });
DommelMapper.AddSqlBuilder(typeof(SqlConnection), new GuidSqlServerSqlBuilder());

var loop = 1;
var childLoop = 10;
if (args.Count() > 0)
    int.TryParse(args[0], out loop);


for (int i = 0; i < loop; i++)
{
    // insertIntKeyDapper(connection, c);
    // insertIntKeyDommel(connection, c);
    insertGuidKeyDommel(connection, parentWithGuid);
    // insertGuidDapper(connection, cGuid);
}
WriteLine($"Looped {loop} times");
parentWithGuid.I = 123;
connection.Update(parentWithGuid);
WriteLine($"Updated {parentWithGuid.Id}");

var x = connection.FirstOrDefault<ParentWithGuid>(p => p.Id == parentWithGuid.Id );
WriteLine($"I is {x?.I}");

for (int i = 0; i < childLoop; i++)
{
    insertChildFor(connection,parentWithGuid);
}
WriteLine($"Added {childLoop} kids");

var combined = connection.FirstOrDefault<ParentWithGuid,Child,ParentWithGuid>( p => p.Id == parentWithGuid.Id );
WriteLine($"Combined has {combined?.Children.Count} kids");


var deleteMe = new Guid[] {parentWithGuid.Id};
connection.DeleteMultiple<ParentWithGuid>(o => deleteMe.Contains(o.Id));
WriteLine($"Deleted {parentWithGuid.Id}");

//============================== classes
public class GuidSqlServerSqlBuilder : SqlServerSqlBuilder
{
    public override string BuildInsert(Type type, string tableName, string[] columnNames, string[] paramNames)
    {
        return $"set nocount on insert into {tableName} ({string.Join(", ", columnNames)}) output inserted.Id values ({string.Join(", ", paramNames)})";
    }
}

class ParentWithInt
{
    public int Id { get; set; }
    public string S { get; set; } = "";
    public int I { get; set; }
}

class ParentWithGuid : IEquatable<ParentWithGuid>
{
    public Guid Id { get; set; }
    public string S { get; set; } = "";
    public int I { get; set; }

    public IList<Child> Children { get; set; } = new List<Child>();

    public bool Equals(ParentWithGuid? other)
    {
        return Id == (other?.Id ?? Guid.Empty);
    }
}

class Child : IEquatable<Child> {
    public Guid Id { get; set; }
    public Guid ParentWithGuidId { get; set; }
    public string ChildName { get; set; } = "";

    public bool Equals(Child? other)
    {
        return Id == (other?.Id ?? Guid.Empty);
    }
}

class ChildMap : DommelEntityMap<Child>
{
    public ChildMap()
    {
        ToTable("Children");
    }
}
class GuidMap : DommelEntityMap<ParentWithGuid>
{
    public GuidMap()
    {
        ToTable("GuidKey");
    }
}
class IntMap : DommelEntityMap<ParentWithInt>
{
    public IntMap()
    {
        ToTable("IntKey");
    }
}
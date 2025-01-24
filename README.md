# Dapper & Dommel Test Program

This is a little .NET 8 console app to test using Dapper and Dommel to save and get objects that use Guid as a primary key.

## Getting Dommel Working

### Guid Primary Keys

There's an outdated [issue](https://github.com/henkmollema/Dommel/issues/80) on GitHub that describes getting Guids out if SQL generates the Key. The code snippet is not quite correct anymore. See
[GuidSqlServerSqlBuilder.cs](src/GuidSqlServerSqlBuilder.cs) for one that works.

The tables use `NEWSEQUENTIALID` as the default for the `Id` column for better index fragmentation.

### 1:M Automapping

This is an [experimental](https://github.com/henkmollema/Dommel#automatic-multi-mapping) feature, but seems to work fine. As the doc mentions, you need to implement `IEquatable` in the child class.

## Build and Run

Create a `shared.appsettings.Development.json` file above your repo to add a connection string. It should look something like this:

```json
{
    "ConnectionStrings":{
        "SqlServer": "Server=localhost;Database=dapper;User Id=sa;Password=Passw0rd!;"
    }
}
```

```powershell
cd /src
dotnet build
dotnet run # use default insert loop count of 1
dotnet run -- 10 # non default insert loop count
dotnet run -- 1 true # run Dapper instead of Dommel test
```

## Testing Dapper Select *

To test using `select *` you can pass in `star` as a parameter and it will make some queries that force errors in classes with or without constructors.

## SQL Files

Run with your favorite SQL tool or extension.

| File                     | Action                       |
| ------------------------ | ---------------------------- |
| `Scripts/bootstrap.sql`  | Create all the tables        |
| `Scripts/dropTables.sql` | Drop all the tables          |
| `Scripts/selectAll.sql`  | Dump the data (or delete it) |

## Creating this Code

```powershell
dotnet new console -o dapper-test
cd dapper-test
dotnet new sln
mkdir src
move *.cs src
move *.sln src
cd src
dotnet sln add dapper-test.csproj

dotnet add package Dapper
dotnet add package Dommel
dotnet add package Dapper.FluentMap.Dommel
dotnet add package System.Data.SqlClient
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Seekatar.Tools
```

## Running SQL Server Locally in Docker

Tip from Andrew Lock's [Generating sortable Guids using NewId](https://andrewlock.net/generating-sortable-guids-using-newid/) blog post.

```powershell
 docker run -d --name sql1 --hostname sql1 `
    -e "ACCEPT_EULA=Y" `
    -e "SA_PASSWORD=Passw0rd!" `
    -p 1433:1433 `
    mcr.microsoft.com/mssql/server:2019-latest
```

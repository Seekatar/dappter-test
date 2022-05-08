# Dapper Test Program

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
```

```powershell
 docker run -d --name sql1 --hostname sql1 `
    -e "ACCEPT_EULA=Y" `
    -e "SA_PASSWORD=Passw0rd!" `
    -p 1433:1433 `
    mcr.microsoft.com/mssql/server:2019-latest
```

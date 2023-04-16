use master;
GO

if NOT EXISTS (SELECT name FROM sys.databases where name = 'dapper')
    create database dapper;

use dapper;

if OBJECT_ID('dbo.IntKey','U') IS NULL
    create table IntKey (
        Id INT PRIMARY KEY IDENTITY,
        S VARCHAR(100) NOT NULL,
        I INT
    )
ELSE
    PRINT 'dbo.IntKey EXISTS'

if OBJECT_ID('dbo.GuidKey','U') IS NULL
    create table GuidKey (
        Id UNIQUEIDENTIFIER PRIMARY KEY default NEWSEQUENTIALID(),
        S VARCHAR(100) NOT NULL,
        I INT
    )
ELSE
    PRINT 'dbo.GuidKey EXISTS'

if OBJECT_ID('dbo.Guid','U') IS NULL
    create table Guid (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        S VARCHAR(100) NOT NULL,
        I INT
    )
ELSE
    PRINT 'dbo.Guid EXISTS'

if OBJECT_ID('dbo.Children','U') IS NULL
    create table Children (
        Id UNIQUEIDENTIFIER PRIMARY KEY default NEWSEQUENTIALID(),
        ParentWithGuidId UNIQUEIDENTIFIER NOT NULL,
        ChildName VARCHAR(100) NOT NULL
    )
ELSE
    PRINT 'dbo.Children EXISTS'

if OBJECT_ID('dbo.Client','U') IS NULL
    create table Client (
        ClientId int PRIMARY KEY NOT NULL,
        Name varchar(100) NOT NULL,
        Description varchar(100) NOT NULL
    )
ELSE
    PRINT 'dbo.Client EXISTS'

if OBJECT_ID('dbo.ClientWithId','U') IS NULL
    create table ClientWithId (
        Id int PRIMARY KEY,
        ClientId int NOT NULL,
        Name varchar(100) NOT NULL,
        Description varchar(100) NOT NULL
    )
ELSE
    PRINT 'dbo.ClientWithId EXISTS'
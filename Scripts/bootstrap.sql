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
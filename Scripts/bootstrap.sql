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

insert into Children (ParentWithGuidId, ChildName) values
    ('D4E3D3A4-3D3D-4D3D-8D3D-3D3D3D3D3D3D', 'Child 1'),
    ('D4E3D3A4-3D3D-4D3D-8D3D-3D3D3D3D3D3a', 'Child 2'),
    ('D4E3D3A4-3D3D-4D3D-8D3D-3D3D3D3D3D3b', 'Child 3'),
    ('D4E3D3A4-3D3D-4D3D-8D3D-3D3D3D3D3D3c', 'Child 4'),
    ('D4E3D3A4-3D3D-4D3D-8D3D-3D3D3D3D3D3e', 'Child 5')
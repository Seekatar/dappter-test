
use dapper;

if OBJECT_ID('dbo.IntKey','U') IS NOT NULL
    drop table IntKey

if OBJECT_ID('dbo.GuidKey','U') IS NOT NULL
    drop table GuidKey

if OBJECT_ID('dbo.Guid','U') IS NOT NULL
    drop table Guid 
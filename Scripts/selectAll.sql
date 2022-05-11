/*
DELETE FROM [dapper].[dbo].[IntKey]
DELETE FROM [dapper].[dbo].[GuidKey]
DELETE FROM [dapper].[dbo].[Guid]
*/
use dapper;

SELECT *
FROM IntKey

SELECT g.Id, S as Parent, I, ChildName
FROM GuidKey g
LEFT JOIN Children c
 ON c.ParentWithGuidId = g.id

SELECT *
FROM Guid

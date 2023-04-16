using Dommel;

// public class GuidSqlServerSqlBuilder : SqlServerSqlBuilder
// {
//     public override string BuildInsert(Type type, string tableName, string[] columnNames, string[] paramNames)
//     {
//         return $"set nocount on insert into {tableName} ({string.Join(", ", columnNames)}) output inserted.Id values ({string.Join(", ", paramNames)})";
//     }
// }

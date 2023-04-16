using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniProfiler.Integrations;
using Polly.Retry;

namespace Loyal.Core.Database;

public record Client
{
    public long ClientId { get; set; }
    public string Name { get; set; } = "";
    public string CUID { get; set; } = "";
};

public sealed class DbConnectionEx : IDisposable
{
    private static readonly ConcurrentDictionary<string, long> ClientIds = new();
    private readonly string _traceFile;
    private static readonly object Lock = new();
    private CustomDbProfiler? _profiler;

    /// <summary>
    /// used for master db connections
    /// </summary>
    /// <param name="options">connection options</param>
    /// <param name="logger">Logger for retry policy</param>
    /// <param name="retryPolicy">Non-default retry policy</param>
    public DbConnectionEx(IOptions<DbOptions> options, ILogger? logger = null, AsyncRetryPolicy? retryPolicy = null)
    {
        _traceFile = options.Value.TraceFile;
        ConnectionString = options.Value.MasterConnectionString;
        Connection = NewConnection(ConnectionString);
        Connect(logger, retryPolicy);
    }

    /// <summary>
    /// used for client db connections
    /// </summary>
    /// <param name="options">connection options</param>
    /// <param name="clientUId"></param>
    /// <param name="logger">Logger for retry policy</param>
    /// <param name="retryPolicy">Non-default retry policy</param>
    public DbConnectionEx(IOptions<DbOptions> options, string clientUId, ILogger? logger = null, AsyncRetryPolicy? retryPolicy = null)
    {
        _traceFile = options.Value.TraceFile;
        if (ClientIds.ContainsKey(clientUId))
        {
            ConnectionString = string.Format(options.Value.ClientConnectionString, ClientIds[clientUId]);
        }
        else
        {
            // look up the value in TransparentlyMaster
            using (var conn = NewConnection(options.Value.MasterConnectionString))
            {
                var clientId = conn.Query<long?>("SELECT ClientId FROM dbo.Clients WHERE CUID = @CUID AND Active = 1",
                        new {CUID = clientUId})
                    .FirstOrDefault();


                if (clientId is null)
                {
                    throw new Exception($"Client with CUID '{clientUId}' not found");
                }

                ClientIds.AddOrUpdate(clientUId, clientId.Value, (key, oldValue) => clientId.Value);
            }

            ConnectionString = string.Format(options.Value.ClientConnectionString, ClientIds[clientUId]);
        }

        Connection = NewConnection(ConnectionString);
        Connect(logger, retryPolicy);
    }

    /// <summary>
    /// SQL Server connection factory that uses System.Data.Common.
    /// </summary>
    private class SqlServerDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        /// <param name="connectionString"></param>
        public SqlServerDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public System.Data.Common.DbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }


    private DbConnection NewConnection(string connectionString)
    {
        if (string.IsNullOrEmpty(_traceFile))
            return new SqlConnection(connectionString);
        else
        {
            _profiler = new CustomDbProfiler();
            var factory = new SqlServerDbConnectionFactory(connectionString);
            return ProfiledDbConnectionFactory.New(factory, _profiler);
        }
    }

    public static async Task<IList<Client>> AllClients(IOptions<DbOptions> options)
    {
        using var conn = new DbConnectionEx(options);

        var clients = (await conn.Connection.QueryAsync<Client>("SELECT ClientId, Name, CUID FROM dbo.Clients WHERE Active = 1").ConfigureAwait(false)).ToList();

        foreach (var client in clients)
        {
            ClientIds.AddOrUpdate(client.CUID, client.ClientId, (key, oldValue) => client.ClientId);
        }

        return clients;
    }

    public DbConnection Connection { get; }

    private string ConnectionString { get; }

    public void Dispose()
    {
        if (Connection.State != ConnectionState.Open) return;

        if (!string.IsNullOrEmpty(_traceFile) && Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            if (File.GetLastWriteTime(_traceFile).Day != DateTime.Now.Day)
            {
                try { File.Delete(_traceFile); } catch { }
            }
            lock (Lock)
            {
                var sb = new StringBuilder($">>>> {DateTime.Now}{Environment.NewLine}");
                sb.Append(_profiler.GetCommands());
                try
                {
                    File.AppendAllText(_traceFile, sb.ToString());
                }
                catch
                {
                }
            }
        }

        Connection.Close();
    }

    private void Connect(ILogger? logger, AsyncRetryPolicy? retryPolicy )
    {
        if (Connection.State != ConnectionState.Open)
        {
            Connection.Open(); //  .OpenWithRetryAsync(logger, retryPolicy).Wait();
        }
    }
}
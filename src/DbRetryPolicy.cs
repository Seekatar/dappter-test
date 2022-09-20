using Polly;
using System;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;

// largely from https://github.com/azurevoodoo/AzureSQLTransientHandling
namespace Loyal.Core.Database;

public static class DbRetryPolicyFactory
{
    private const int SqlRetryCount = 4;

    
    // questionable transient errors
    // -2 timeout? Cadru
    private const int SqlErrorInstanceDoesNotSupportEncryption = 20;
    private const int SqlErrorServiceRequestProcessFail = 40540; // only Voodoo uses this
    private const int SqlErrorServiceExperiencingAProblem = 40545; // only Voodoo uses this
    private const int SqlErrorOperationInProgress = 40627; // only Voodoo uses this
    
    // errors
    private const int SqlErrorConnectedButLoginFailed = 64;
    private const int SqlErrorUnableToEstablishConnection = 233;
    
    // Not found
    private const int SqlErrorTransportLevelErrorReceivingResult = 10053;
    
    private const int SqlErrorTransportLevelErrorWhenSendingRequestToServer = 10054;
    private const int SqlErrorNetworkRelatedErrorDuringConnect = 10060;
    
    // resource limits
    private const int SqlErrorDatabaseLimitReached = 10928;
    private const int SqlErrorResourceLimitReached = 10929;
    
    // transient per MS
    private const int SqlErrorServiceErrorEncountered = 40197;
    private const int SqlErrorServiceBusy = 40501;
    private const int SqlErrorDatabaseUnavailable = 40613;
    // 4060 login
    // 4221 login
    // 49918
    // 49919
    // 49920
    // resource/elastic per MS
    // 10928
    // 10929
    // xaction releast
    // 40549 long running xaction
    // 40550 too many locks Cadru, vanny, not MS
    
    // per cadru, gist & devMobile
    // 20 encryption not supported
    // 64 connection login
    // 233 connection
    // 1205 deadlock
    // 10053 network related
    // 10054 network
    // 10060 network
    // 11001 network
    // 41301
    // 41302
    // 41305
    // 41325
    // 41839
    
    

    private static bool ConnectRetryableError(SqlException exception)
    {
        return exception.Errors.OfType<SqlError>().Any(ConnectRetryableError);
    }

    private static bool SqlRetryableError(SqlException exception)
    {
        return exception.Errors.OfType<SqlError>().Any(SqlRetryableError);
    }

    private static bool ConnectRetryableError(SqlError error)
    {
        switch (error.Number)
        {
            case SqlErrorOperationInProgress:
            case SqlErrorDatabaseUnavailable:
            case SqlErrorServiceExperiencingAProblem:
            case SqlErrorServiceRequestProcessFail:
            case SqlErrorServiceBusy:
            case SqlErrorServiceErrorEncountered:
            case SqlErrorResourceLimitReached:
            case SqlErrorDatabaseLimitReached:
            case SqlErrorNetworkRelatedErrorDuringConnect:
            case SqlErrorTransportLevelErrorWhenSendingRequestToServer:
            case SqlErrorTransportLevelErrorReceivingResult:
            case SqlErrorUnableToEstablishConnection:
            case SqlErrorConnectedButLoginFailed:
            case SqlErrorInstanceDoesNotSupportEncryption:
                return true;

            default:
                return false;
        }
    }
   
    private static bool SqlRetryableError(SqlError error)
    {
        switch (error.Number)
        {
            case SqlErrorOperationInProgress:
            case SqlErrorDatabaseUnavailable:
            case SqlErrorServiceExperiencingAProblem:
            case SqlErrorServiceRequestProcessFail:
            case SqlErrorServiceBusy:
            case SqlErrorServiceErrorEncountered:
            case SqlErrorResourceLimitReached:
            case SqlErrorDatabaseLimitReached:
            case SqlErrorNetworkRelatedErrorDuringConnect:
            case SqlErrorTransportLevelErrorWhenSendingRequestToServer:
            case SqlErrorTransportLevelErrorReceivingResult:
            case SqlErrorUnableToEstablishConnection:
            case SqlErrorConnectedButLoginFailed:
            case SqlErrorInstanceDoesNotSupportEncryption:
                return true;

            default:
                return false;
        }
    }

    public static AsyncRetryPolicy CreateConnectRetryPolicy(int retryCount = 4, int initialTimeoutSec = 5, int maxTimeoutSec = 60)
    {
        return Policy
            // .Handle<TimeoutException>() // others don't use this, only this one
            .Handle<SqlException>(ConnectRetryableError)
            .WaitAndRetryAsync(
                retryCount,
                // get timespan for delay
                (attempt) => TimeSpan.FromSeconds(Math.Min(maxTimeoutSec, Math.Pow(initialTimeoutSec, attempt))),
                // on retry
                (exception, timeSpan, retries, context) =>
                {
                    if (context["logger"] is not ILogger logger)
                        return;
                    logger.LogDebug(exception, "SQL Retry of {retries}/{retryCount} failed", retries, retryCount);
                    
                    if (retryCount != retries)
                        return;

                    // only log if the final retry fails
                    logger.LogError(exception, "SQL Retry of {retries}/{retryCount} failed", retries, retryCount);
                });
    } 
    
    public static AsyncRetryPolicy CreateSqlRetryPolicy(int retryCount = 4, int initialTimeoutSec = 5, int maxTimeoutSec = 60)
    {
        return Policy
            .Handle<TimeoutException>()
            .Or<SqlException>(SqlRetryableError)
            .WaitAndRetryAsync(retryCount, (attempt) =>
                TimeSpan.FromSeconds(Math.Min(maxTimeoutSec, Math.Pow(initialTimeoutSec, attempt)))
            );        
    }

    public static AsyncRetryPolicy DefaultConnectRetryPolicy { get; set; } = CreateConnectRetryPolicy();
    public static AsyncRetryPolicy DefaultSqlRetryPolicy { get; set; } = CreateSqlRetryPolicy();
    
    public static async Task<DbConnection> OpenWithRetryAsync(this DbConnection conn, ILogger? logger = null, AsyncRetryPolicy? retryPolicy = null)
    {
        retryPolicy ??= DefaultConnectRetryPolicy;
        var context = new Dictionary<string, object>();
        if (logger is not null)
        {
            context["logger"] = logger;
        } 
        await retryPolicy.ExecuteAsync((_) => conn.OpenAsync(), context).ConfigureAwait(false);;
        return conn;
    }
    
    public static async Task<T> RunWithRetryAsync<T>(this DbConnection conn, Func<Task<T>> runSql, ILogger? logger = null, AsyncRetryPolicy? retryPolicy = null) 
    {
        retryPolicy ??= DefaultSqlRetryPolicy;
        var context = new Dictionary<string, object>();
        if (logger is not null)
        {
            context["logger"] = logger;
        } 

        return await retryPolicy.ExecuteAsync((_) => runSql(), context).ConfigureAwait(false);
    }
}
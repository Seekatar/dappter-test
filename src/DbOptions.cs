using System.ComponentModel.DataAnnotations;
using Microsoft.Identity.Client;

namespace Loyal.Core.Database;

public class DbOptions
{

    /// <summary>
    /// Connection string for the master database
    /// </summary>
    [Required]
    public string MasterConnectionString { get; set; } = "";

    /// <summary>
    /// Connection string for the client database, should have {0} placeholder for the client id
    /// </summary>
    [Required]
    public string ClientConnectionString { get; set; } = "";

    /// <summary>
    /// Path to SQL trace file. Used only if ASPNETCORE_ENVIRONMENT is set to Development
    /// </summary>
    public string TraceFile { get; set; } = "";
    
    /// <summary>
    /// For always encrypted
    /// </summary>
    public string ColMasterKeyClientId { get; set; } = "";
    
    /// <summary>
    /// For always encrypted
    /// </summary>
    public string ColMasterKeyClientSecret { get; set; } = "";
    
    /// <summary>
    /// For always encrypted, the Azure TenantId
    /// </summary>
    public string AadTenantId { get; set; } = "";

    /// <summary>
    /// initial time for connect retries for exponential backoff to max 60 seconds. MS recommends 5 seconds
    /// </summary>
    [Range(1, 10)]
    public int InitialConnectTimeoutSec { get; set; } = 5;

    /// <summary>
    /// initial time for SQL/DML retries for exponential backoff to max 60 seconds. MS recommends 5 seconds
    /// </summary>
    [Range(1, 10)]
    public int InitialQueryTimeoutSec { get; set; } = 5;

    /// <summary>
    /// Name of the section in the config files (or prefix for environment variables)
    /// </summary>
    public static string SectionName => "Database";
}
namespace Mathy.ELM.Core.Enums;

/// <summary>
/// Categories for application logging to help organize and filter log entries.
/// </summary>
public enum LogCategory
{
    /// <summary>
    /// General application operations
    /// </summary>
    Application,

    /// <summary>
    /// Email notification operations (sending, queuing, delivery)
    /// </summary>
    EmailNotification,

    /// <summary>
    /// Save operations (create new records)
    /// </summary>
    Save,

    /// <summary>
    /// Update operations (modify existing records)
    /// </summary>
    Update,

    /// <summary>
    /// Cancel operations (cancel requests)
    /// </summary>
    Cancel,

    /// <summary>
    /// Save as draft operations
    /// </summary>
    SaveAsDraft,

    /// <summary>
    /// Active Directory operations (user creation, updates, queries)
    /// </summary>
    ActiveDirectory,

    /// <summary>
    /// Service ticket operations (ServiceDesk Plus integration)
    /// </summary>
    ServiceTicket,

    /// <summary>
    /// Authentication and authorization operations
    /// </summary>
    Authentication,

    /// <summary>
    /// Database operations
    /// </summary>
    Database,

    /// <summary>
    /// EF Core migration operations
    /// </summary>
    EFMigration,

    /// <summary>
    /// Viewpoint/Vista API integration operations
    /// </summary>
    ViewpointIntegration,

    /// <summary>
    /// Reference data synchronization operations
    /// </summary>
    ReferenceDataSync,

    /// <summary>
    /// Background job operations (Hangfire)
    /// </summary>
    BackgroundJob,

    /// <summary>
    /// HR Request operations (all request types)
    /// </summary>
    HRRequest,

    /// <summary>
    /// Azure Service Bus operations
    /// </summary>
    ServiceBus,

    /// <summary>
    /// API request/response logging
    /// </summary>
    ApiRequest,

    /// <summary>
    /// SignalR hub operations
    /// </summary>
    SignalR,

    /// <summary>
    /// Application startup and shutdown
    /// </summary>
    Startup,

    /// <summary>
    /// Security-related events
    /// </summary>
    Security
}

using Mathy.ELM.Core.Enums;

namespace Mathy.ELM.Core.Interfaces;

/// <summary>
/// ECM Application Logger interface for structured logging with category support.
/// Logs are written to separate files based on log type:
/// - ecm_log_app_{date}.log - General application logs (authentication, save, update, cancel, Viewpoint, etc.)
/// - ecm_log_error_{date}.log - Error and Warning logs (all errors and warnings from any category)
/// - ecm_log_efMigrationsHistory_{date}.log - EF Core migrations
/// - ecm_log_email_{date}.log - Email notifications (template processing, queue operations, recipients)
/// - ecm_log_jobs_{date}.log - Background jobs and Service Bus operations
/// All logs are retained for 8 days (FIFO).
/// </summary>
public interface IEcmLogger
{
    #region Success/Information Logging (ecm_log_app)

    /// <summary>
    /// Logs a successful operation with category.
    /// </summary>
    void LogSuccess(LogCategory category, string message, params object[] args);

    /// <summary>
    /// Logs a successful operation with category and additional properties.
    /// </summary>
    void LogSuccess(LogCategory category, string message, Dictionary<string, object> properties);

    /// <summary>
    /// Logs an informational message with category.
    /// </summary>
    void LogInfo(LogCategory category, string message, params object[] args);

    /// <summary>
    /// Logs an informational message with category and additional properties.
    /// </summary>
    void LogInfo(LogCategory category, string message, Dictionary<string, object> properties);

    /// <summary>
    /// Logs a warning message with category.
    /// </summary>
    void LogWarning(LogCategory category, string message, params object[] args);

    /// <summary>
    /// Logs a warning message with category and additional properties.
    /// </summary>
    void LogWarning(LogCategory category, string message, Dictionary<string, object> properties);

    #endregion

    #region Error Logging (ecm_log_error)

    /// <summary>
    /// Logs an error message with category.
    /// </summary>
    void LogError(LogCategory category, string message, params object[] args);

    /// <summary>
    /// Logs an error with exception details.
    /// </summary>
    void LogError(LogCategory category, Exception exception, string message, params object[] args);

    /// <summary>
    /// Logs an error with category and additional properties.
    /// </summary>
    void LogError(LogCategory category, string message, Dictionary<string, object> properties);

    /// <summary>
    /// Logs an error with exception and additional properties.
    /// </summary>
    void LogError(LogCategory category, Exception exception, string message, Dictionary<string, object> properties);

    /// <summary>
    /// Logs a critical/fatal error.
    /// </summary>
    void LogCritical(LogCategory category, Exception exception, string message, params object[] args);

    #endregion

    #region Migration Logging (ecm_log_efMigrationsHistory)

    /// <summary>
    /// Logs a database migration event.
    /// </summary>
    void LogMigration(bool success, string migrationName, string operation, string? errorMessage = null);

    /// <summary>
    /// Logs pending migrations check.
    /// </summary>
    void LogMigrationCheck(int pendingCount, IEnumerable<string> pendingMigrations);

    /// <summary>
    /// Logs migration application start.
    /// </summary>
    void LogMigrationStart(string migrationName);

    /// <summary>
    /// Logs migration application completion.
    /// </summary>
    void LogMigrationComplete(string migrationName, TimeSpan duration);

    /// <summary>
    /// Logs migration failure.
    /// </summary>
    void LogMigrationFailed(string migrationName, Exception exception);

    /// <summary>
    /// Logs database schema update summary.
    /// </summary>
    void LogMigrationSummary(int appliedCount, int failedCount, TimeSpan totalDuration);

    #endregion

    #region Specialized Logging Methods (ecm_log_app)

    /// <summary>
    /// Logs a save operation (create).
    /// </summary>
    void LogSave(bool success, string entityType, object? entityId, string? userName = null, string? errorMessage = null);

    /// <summary>
    /// Logs an update operation.
    /// </summary>
    void LogUpdate(bool success, string entityType, object? entityId, string? userName = null, string? errorMessage = null);

    /// <summary>
    /// Logs a cancel operation.
    /// </summary>
    void LogCancel(bool success, string entityType, object? entityId, string? userName = null, string? reason = null, string? errorMessage = null);

    /// <summary>
    /// Logs a save as draft operation.
    /// </summary>
    void LogSaveAsDraft(bool success, string entityType, object? entityId, string? userName = null, string? errorMessage = null);

    /// <summary>
    /// Logs an Active Directory operation.
    /// </summary>
    void LogActiveDirectory(bool success, string operation, string? userName = null, string? targetUser = null, string? errorMessage = null);

    /// <summary>
    /// Logs a service ticket operation.
    /// </summary>
    void LogServiceTicket(bool success, string operation, string? ticketId = null, string? requestType = null, string? errorMessage = null, string? employeeName = null);

    /// <summary>
    /// Logs an authentication event.
    /// </summary>
    void LogAuthentication(bool success, string operation, string? userName = null, string? ipAddress = null, string? errorMessage = null);

    /// <summary>
    /// Logs a Viewpoint integration operation.
    /// </summary>
    void LogViewpointIntegration(bool success, string operation, string? endpoint = null, int? recordCount = null, string? errorMessage = null);

    /// <summary>
    /// Logs an HR request operation.
    /// </summary>
    void LogHRRequest(bool success, string requestType, string operation, object? requestId = null, string? userName = null, string? errorMessage = null);

    /// <summary>
    /// Logs a reference data sync operation.
    /// </summary>
    void LogReferenceDataSync(bool success, string entityType, int addedCount, int updatedCount, int skippedCount, string? errorMessage = null);

    #endregion

    #region Email Logging (ecm_log_email)

    /// <summary>
    /// Logs an email notification operation.
    /// </summary>
    void LogEmailNotification(bool success, string operation, string recipient, string? subject = null, string? errorMessage = null);

    #endregion

    #region Jobs/Service Bus Logging (ecm_log_jobs)

    /// <summary>
    /// Logs a background job operation.
    /// </summary>
    void LogBackgroundJob(bool success, string jobName, string operation, string? jobId = null, string? errorMessage = null);

    /// <summary>
    /// Logs a Service Bus operation (queue message send/receive).
    /// </summary>
    void LogServiceBus(bool success, string operation, string? queueName = null, string? messageId = null, string? errorMessage = null);

    #endregion
}

using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace Mathy.ELM.Infrastructure.Services;

/// <summary>
/// ECM Application Logger implementation using Serilog.
/// Provides structured logging with category support and separate log files.
/// Log files:
/// - ecm_log_app_.log - General application logs (authentication, save, update, cancel, Viewpoint, etc.)
/// - ecm_log_error_.log - Error and Warning logs (all errors and warnings from any category)
/// - ecm_log_efMigrationsHistory_.log - EF Core migrations
/// - ecm_log_email_.log - Email notifications (template processing, queue operations, recipients)
/// - ecm_log_jobs_.log - Background jobs and Service Bus operations
/// - ecm_log_error_AD_.log - Active Directory error and warning logs
/// - ecm_log_error_EmailNotif_.log - Email notification error and warning logs
/// </summary>
public class EcmLogger : IEcmLogger
{
    private readonly ILogger _appLogger;
    private readonly ILogger _errorLogger;
    private readonly ILogger _migrationLogger;
    private readonly ILogger _emailLogger;
    private readonly ILogger _jobsLogger;
    private readonly ILogger _adErrorLogger;
    private readonly ILogger _emailNotifErrorLogger;

    public EcmLogger(
        ILogger appLogger,
        ILogger errorLogger,
        ILogger migrationLogger,
        ILogger emailLogger,
        ILogger jobsLogger,
        ILogger adErrorLogger,
        ILogger emailNotifErrorLogger)
    {
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        _errorLogger = errorLogger ?? throw new ArgumentNullException(nameof(errorLogger));
        _migrationLogger = migrationLogger ?? throw new ArgumentNullException(nameof(migrationLogger));
        _emailLogger = emailLogger ?? throw new ArgumentNullException(nameof(emailLogger));
        _jobsLogger = jobsLogger ?? throw new ArgumentNullException(nameof(jobsLogger));
        _adErrorLogger = adErrorLogger ?? throw new ArgumentNullException(nameof(adErrorLogger));
        _emailNotifErrorLogger = emailNotifErrorLogger ?? throw new ArgumentNullException(nameof(emailNotifErrorLogger));
    }

    #region Success/Information Logging

    public void LogSuccess(LogCategory category, string message, params object[] args)
    {
        using (LogContext.PushProperty("Category", category.ToString()))
        using (LogContext.PushProperty("Status", "SUCCESS"))
        {
            _appLogger.Information(message, args);
        }
    }

    public void LogSuccess(LogCategory category, string message, Dictionary<string, object> properties)
    {
        using (LogContext.PushProperty("Category", category.ToString()))
        using (LogContext.PushProperty("Status", "SUCCESS"))
        {
            PushProperties(properties);
            _appLogger.Information(message);
        }
    }

    public void LogInfo(LogCategory category, string message, params object[] args)
    {
        using (LogContext.PushProperty("Category", category.ToString()))
        {
            _appLogger.Information(message, args);
        }
    }

    public void LogInfo(LogCategory category, string message, Dictionary<string, object> properties)
    {
        using (LogContext.PushProperty("Category", category.ToString()))
        {
            PushProperties(properties);
            _appLogger.Information(message);
        }
    }

    public void LogWarning(LogCategory category, string message, params object[] args)
    {
        using (LogContext.PushProperty("Category", category.ToString()))
        using (LogContext.PushProperty("Status", "WARNING"))
        {
            GetErrorLogger(category).Warning(message, args);
        }
    }

    public void LogWarning(LogCategory category, string message, Dictionary<string, object> properties)
    {
        using (LogContext.PushProperty("Category", category.ToString()))
        using (LogContext.PushProperty("Status", "WARNING"))
        {
            PushProperties(properties);
            GetErrorLogger(category).Warning(message);
        }
    }

    #endregion

    #region Error Logging

    public void LogError(LogCategory category, string message, params object[] args)
    {
        using (LogContext.PushProperty("Category", category.ToString()))
        using (LogContext.PushProperty("Status", "ERROR"))
        {
            GetErrorLogger(category).Error(message, args);
        }
    }

    public void LogError(LogCategory category, Exception exception, string message, params object[] args)
    {
        using (LogContext.PushProperty("Category", category.ToString()))
        using (LogContext.PushProperty("Status", "ERROR"))
        using (LogContext.PushProperty("ExceptionType", exception.GetType().Name))
        {
            GetErrorLogger(category).Error(exception, message, args);
        }
    }

    public void LogError(LogCategory category, string message, Dictionary<string, object> properties)
    {
        using (LogContext.PushProperty("Category", category.ToString()))
        using (LogContext.PushProperty("Status", "ERROR"))
        {
            PushProperties(properties);
            GetErrorLogger(category).Error(message);
        }
    }

    public void LogError(LogCategory category, Exception exception, string message, Dictionary<string, object> properties)
    {
        using (LogContext.PushProperty("Category", category.ToString()))
        using (LogContext.PushProperty("Status", "ERROR"))
        using (LogContext.PushProperty("ExceptionType", exception.GetType().Name))
        {
            PushProperties(properties);
            GetErrorLogger(category).Error(exception, message);
        }
    }

    public void LogCritical(LogCategory category, Exception exception, string message, params object[] args)
    {
        using (LogContext.PushProperty("Category", category.ToString()))
        using (LogContext.PushProperty("Status", "CRITICAL"))
        using (LogContext.PushProperty("ExceptionType", exception.GetType().Name))
        {
            GetErrorLogger(category).Fatal(exception, message, args);
        }
    }

    #endregion

    #region Migration Logging

    public void LogMigration(bool success, string migrationName, string operation, string? errorMessage = null)
    {
        using (LogContext.PushProperty("Category", LogCategory.EFMigration.ToString()))
        using (LogContext.PushProperty("MigrationName", migrationName))
        using (LogContext.PushProperty("Operation", operation))
        using (LogContext.PushProperty("Status", success ? "SUCCESS" : "FAILED"))
        {
            if (success)
            {
                _migrationLogger.Information("Migration {Operation}: {MigrationName}", operation, migrationName);
            }
            else
            {
                using (LogContext.PushProperty("ErrorMessage", errorMessage ?? "Unknown error"))
                {
                    _migrationLogger.Error("Migration {Operation} FAILED: {MigrationName} - {ErrorMessage}",
                        operation, migrationName, errorMessage);
                }
            }
        }
    }

    public void LogMigrationCheck(int pendingCount, IEnumerable<string> pendingMigrations)
    {
        using (LogContext.PushProperty("Category", LogCategory.EFMigration.ToString()))
        using (LogContext.PushProperty("Operation", "CHECK"))
        using (LogContext.PushProperty("PendingCount", pendingCount))
        {
            if (pendingCount > 0)
            {
                _migrationLogger.Information(
                    "Found {PendingCount} pending migration(s): {PendingMigrations}",
                    pendingCount,
                    string.Join(", ", pendingMigrations));
            }
            else
            {
                _migrationLogger.Information("No pending migrations found. Database is up to date.");
            }
        }
    }

    public void LogMigrationStart(string migrationName)
    {
        using (LogContext.PushProperty("Category", LogCategory.EFMigration.ToString()))
        using (LogContext.PushProperty("MigrationName", migrationName))
        using (LogContext.PushProperty("Operation", "START"))
        {
            _migrationLogger.Information("Starting migration: {MigrationName}", migrationName);
        }
    }

    public void LogMigrationComplete(string migrationName, TimeSpan duration)
    {
        using (LogContext.PushProperty("Category", LogCategory.EFMigration.ToString()))
        using (LogContext.PushProperty("MigrationName", migrationName))
        using (LogContext.PushProperty("Operation", "COMPLETE"))
        using (LogContext.PushProperty("DurationMs", duration.TotalMilliseconds))
        using (LogContext.PushProperty("Status", "SUCCESS"))
        {
            _migrationLogger.Information(
                "Migration completed: {MigrationName} (Duration: {DurationMs:F2}ms)",
                migrationName,
                duration.TotalMilliseconds);
        }
    }

    public void LogMigrationFailed(string migrationName, Exception exception)
    {
        using (LogContext.PushProperty("Category", LogCategory.EFMigration.ToString()))
        using (LogContext.PushProperty("MigrationName", migrationName))
        using (LogContext.PushProperty("Operation", "FAILED"))
        using (LogContext.PushProperty("Status", "ERROR"))
        using (LogContext.PushProperty("ExceptionType", exception.GetType().Name))
        {
            _migrationLogger.Error(exception, "Migration FAILED: {MigrationName}", migrationName);
        }
    }

    public void LogMigrationSummary(int appliedCount, int failedCount, TimeSpan totalDuration)
    {
        using (LogContext.PushProperty("Category", LogCategory.EFMigration.ToString()))
        using (LogContext.PushProperty("Operation", "SUMMARY"))
        using (LogContext.PushProperty("AppliedCount", appliedCount))
        using (LogContext.PushProperty("FailedCount", failedCount))
        using (LogContext.PushProperty("TotalDurationMs", totalDuration.TotalMilliseconds))
        using (LogContext.PushProperty("Status", failedCount == 0 ? "SUCCESS" : "PARTIAL"))
        {
            _migrationLogger.Information(
                "Migration Summary - Applied: {AppliedCount}, Failed: {FailedCount}, Total Duration: {TotalDurationMs:F2}ms",
                appliedCount,
                failedCount,
                totalDuration.TotalMilliseconds);
        }
    }

    #endregion

    #region Specialized Logging Methods

    public void LogEmailNotification(bool success, string operation, string recipient, string? subject = null, string? errorMessage = null)
    {
        using (LogContext.PushProperty("Category", LogCategory.EmailNotification.ToString()))
        using (LogContext.PushProperty("Status", success ? "SUCCESS" : "FAILED"))
        using (LogContext.PushProperty("Operation", operation))
        using (LogContext.PushProperty("Recipient", recipient))
        {
            if (!string.IsNullOrEmpty(subject))
                LogContext.PushProperty("Subject", subject);

            if (success)
            {
                _emailLogger.Information("Email {Operation} - Recipient: {Recipient}, Subject: {Subject}",
                    operation, recipient, subject ?? "N/A");
            }
            else
            {
                using (LogContext.PushProperty("ErrorMessage", errorMessage ?? "Unknown error"))
                {
                    // Log to email logger for completeness of email activity
                    _emailLogger.Warning("Email {Operation} FAILED - Recipient: {Recipient}, Error: {ErrorMessage}",
                        operation, recipient, errorMessage ?? "Unknown error");
                    // Log to dedicated email notification error log file (ecm_log_error_EmailNotif) instead of general error log
                    _emailNotifErrorLogger.Error("Email {Operation} FAILED - Recipient: {Recipient}, Error: {ErrorMessage}",
                        operation, recipient, errorMessage ?? "Unknown error");
                }
            }
        }
    }

    public void LogSave(bool success, string entityType, object? entityId, string? userName = null, string? errorMessage = null)
    {
        var properties = new Dictionary<string, object>
        {
            { "EntityType", entityType },
            { "EntityId", entityId?.ToString() ?? "N/A" }
        };

        if (!string.IsNullOrEmpty(userName))
            properties["UserName"] = userName;

        if (success)
        {
            LogSuccess(LogCategory.Save,
                "Created {EntityType} with ID: {EntityId} by User: {UserName}",
                properties);
        }
        else
        {
            properties["ErrorMessage"] = errorMessage ?? "Unknown error";
            LogError(LogCategory.Save,
                "Failed to create {EntityType} - Error: {ErrorMessage}",
                properties);
        }
    }

    public void LogUpdate(bool success, string entityType, object? entityId, string? userName = null, string? errorMessage = null)
    {
        var properties = new Dictionary<string, object>
        {
            { "EntityType", entityType },
            { "EntityId", entityId?.ToString() ?? "N/A" }
        };

        if (!string.IsNullOrEmpty(userName))
            properties["UserName"] = userName;

        if (success)
        {
            LogSuccess(LogCategory.Update,
                "Updated {EntityType} with ID: {EntityId} by User: {UserName}",
                properties);
        }
        else
        {
            properties["ErrorMessage"] = errorMessage ?? "Unknown error";
            LogError(LogCategory.Update,
                "Failed to update {EntityType} with ID: {EntityId} - Error: {ErrorMessage}",
                properties);
        }
    }

    public void LogCancel(bool success, string entityType, object? entityId, string? userName = null, string? reason = null, string? errorMessage = null)
    {
        var properties = new Dictionary<string, object>
        {
            { "EntityType", entityType },
            { "EntityId", entityId?.ToString() ?? "N/A" }
        };

        if (!string.IsNullOrEmpty(userName))
            properties["UserName"] = userName;

        if (!string.IsNullOrEmpty(reason))
            properties["Reason"] = reason;

        if (success)
        {
            LogSuccess(LogCategory.Cancel,
                "Cancelled {EntityType} with ID: {EntityId} by User: {UserName}, Reason: {Reason}",
                properties);
        }
        else
        {
            properties["ErrorMessage"] = errorMessage ?? "Unknown error";
            LogError(LogCategory.Cancel,
                "Failed to cancel {EntityType} with ID: {EntityId} - Error: {ErrorMessage}",
                properties);
        }
    }

    public void LogSaveAsDraft(bool success, string entityType, object? entityId, string? userName = null, string? errorMessage = null)
    {
        var properties = new Dictionary<string, object>
        {
            { "EntityType", entityType },
            { "EntityId", entityId?.ToString() ?? "N/A" }
        };

        if (!string.IsNullOrEmpty(userName))
            properties["UserName"] = userName;

        if (success)
        {
            LogSuccess(LogCategory.SaveAsDraft,
                "Saved {EntityType} as draft with ID: {EntityId} by User: {UserName}",
                properties);
        }
        else
        {
            properties["ErrorMessage"] = errorMessage ?? "Unknown error";
            LogError(LogCategory.SaveAsDraft,
                "Failed to save {EntityType} as draft - Error: {ErrorMessage}",
                properties);
        }
    }

    public void LogActiveDirectory(bool success, string operation, string? userName = null, string? targetUser = null, string? errorMessage = null)
    {
        var properties = new Dictionary<string, object>
        {
            { "Operation", operation }
        };

        if (!string.IsNullOrEmpty(userName))
            properties["UserName"] = userName;

        if (!string.IsNullOrEmpty(targetUser))
            properties["TargetUser"] = targetUser;

        if (success)
        {
            LogSuccess(LogCategory.ActiveDirectory,
                "AD {Operation} - Target: {TargetUser} by User: {UserName}",
                properties);
        }
        else
        {
            properties["ErrorMessage"] = errorMessage ?? "Unknown error";

            // Log to dedicated AD error log file (ecm_log_error_AD) instead of general error log
            using (LogContext.PushProperty("Category", LogCategory.ActiveDirectory.ToString()))
            using (LogContext.PushProperty("Status", "ERROR"))
            {
                PushProperties(properties);
                _adErrorLogger.Error("AD {Operation} FAILED - Target: {TargetUser}, Error: {ErrorMessage}",
                    operation, targetUser ?? "N/A", errorMessage ?? "Unknown error");
            }
        }
    }

    public void LogServiceTicket(bool success, string operation, string? ticketId = null, string? requestType = null, string? errorMessage = null, string? employeeName = null)
    {
        var properties = new Dictionary<string, object>
        {
            { "Operation", operation },
            { "EmployeeName", string.IsNullOrWhiteSpace(employeeName) ? "N/A" : employeeName }
        };

        if (!string.IsNullOrEmpty(ticketId))
            properties["TicketId"] = ticketId;

        if (!string.IsNullOrEmpty(requestType))
            properties["RequestType"] = requestType;

        if (success)
        {
            LogSuccess(LogCategory.ServiceTicket,
                "ServiceTicket {Operation} - Employee: {EmployeeName}, TicketId: {TicketId}, RequestType: {RequestType}",
                properties);
        }
        else
        {
            properties["ErrorMessage"] = errorMessage ?? "Unknown error";
            LogError(LogCategory.ServiceTicket,
                "ServiceTicket {Operation} FAILED - Employee: {EmployeeName}, Error: {ErrorMessage}",
                properties);
        }
    }

    public void LogAuthentication(bool success, string operation, string? userName = null, string? ipAddress = null, string? errorMessage = null)
    {
        var properties = new Dictionary<string, object>
        {
            { "Operation", operation }
        };

        if (!string.IsNullOrEmpty(userName))
            properties["UserName"] = userName;

        if (!string.IsNullOrEmpty(ipAddress))
            properties["IpAddress"] = ipAddress;

        if (success)
        {
            LogSuccess(LogCategory.Authentication,
                "Authentication {Operation} - User: {UserName}, IP: {IpAddress}",
                properties);
        }
        else
        {
            properties["ErrorMessage"] = errorMessage ?? "Unknown error";
            LogError(LogCategory.Authentication,
                "Authentication {Operation} FAILED - User: {UserName}, IP: {IpAddress}, Error: {ErrorMessage}",
                properties);
        }
    }

    public void LogViewpointIntegration(bool success, string operation, string? endpoint = null, int? recordCount = null, string? errorMessage = null)
    {
        var properties = new Dictionary<string, object>
        {
            { "Operation", operation }
        };

        if (!string.IsNullOrEmpty(endpoint))
            properties["Endpoint"] = endpoint;

        if (recordCount.HasValue)
            properties["RecordCount"] = recordCount.Value;

        if (success)
        {
            LogSuccess(LogCategory.ViewpointIntegration,
                "Viewpoint {Operation} - Endpoint: {Endpoint}, Records: {RecordCount}",
                properties);
        }
        else
        {
            properties["ErrorMessage"] = errorMessage ?? "Unknown error";
            LogError(LogCategory.ViewpointIntegration,
                "Viewpoint {Operation} FAILED - Endpoint: {Endpoint}, Error: {ErrorMessage}",
                properties);
        }
    }

    public void LogBackgroundJob(bool success, string jobName, string operation, string? jobId = null, string? errorMessage = null)
    {
        using (LogContext.PushProperty("Category", LogCategory.BackgroundJob.ToString()))
        using (LogContext.PushProperty("Status", success ? "SUCCESS" : "FAILED"))
        using (LogContext.PushProperty("JobName", jobName))
        using (LogContext.PushProperty("Operation", operation))
        {
            if (!string.IsNullOrEmpty(jobId))
                LogContext.PushProperty("JobId", jobId);

            if (success)
            {
                _jobsLogger.Information("BackgroundJob {Operation} - Job: {JobName}, JobId: {JobId}",
                    operation, jobName, jobId ?? "N/A");
            }
            else
            {
                using (LogContext.PushProperty("ErrorMessage", errorMessage ?? "Unknown error"))
                {
                    // Log to both jobs logger and error logger for failed jobs
                    _jobsLogger.Warning("BackgroundJob {Operation} FAILED - Job: {JobName}, Error: {ErrorMessage}",
                        operation, jobName, errorMessage ?? "Unknown error");
                    _errorLogger.Error("BackgroundJob {Operation} FAILED - Job: {JobName}, Error: {ErrorMessage}",
                        operation, jobName, errorMessage ?? "Unknown error");
                }
            }
        }
    }

    public void LogServiceBus(bool success, string operation, string? queueName = null, string? messageId = null, string? errorMessage = null)
    {
        using (LogContext.PushProperty("Category", LogCategory.ServiceBus.ToString()))
        using (LogContext.PushProperty("Status", success ? "SUCCESS" : "FAILED"))
        using (LogContext.PushProperty("Operation", operation))
        {
            if (!string.IsNullOrEmpty(queueName))
                LogContext.PushProperty("QueueName", queueName);

            if (!string.IsNullOrEmpty(messageId))
                LogContext.PushProperty("MessageId", messageId);

            if (success)
            {
                _jobsLogger.Information("Message {Operation} Azure Service Bus queue '{QueueName}' - MessageId: {MessageId}",
                    operation, queueName ?? "N/A", messageId ?? "N/A");
            }
            else
            {
                using (LogContext.PushProperty("ErrorMessage", errorMessage ?? "Unknown error"))
                {
                    // Log to both jobs logger and error logger for failed operations
                    _jobsLogger.Warning("ServiceBus {Operation} FAILED - Queue: {QueueName}, Error: {ErrorMessage}",
                        operation, queueName ?? "N/A", errorMessage ?? "Unknown error");
                    _errorLogger.Error("ServiceBus {Operation} FAILED - Queue: {QueueName}, Error: {ErrorMessage}",
                        operation, queueName ?? "N/A", errorMessage ?? "Unknown error");
                }
            }
        }
    }

    public void LogHRRequest(bool success, string requestType, string operation, object? requestId = null, string? userName = null, string? errorMessage = null)
    {
        var properties = new Dictionary<string, object>
        {
            { "RequestType", requestType },
            { "Operation", operation }
        };

        if (requestId != null)
            properties["RequestId"] = requestId.ToString()!;

        if (!string.IsNullOrEmpty(userName))
            properties["UserName"] = userName;

        if (success)
        {
            LogSuccess(LogCategory.HRRequest,
                "HRRequest {Operation} - Type: {RequestType}, ID: {RequestId}, User: {UserName}",
                properties);
        }
        else
        {
            properties["ErrorMessage"] = errorMessage ?? "Unknown error";
            LogError(LogCategory.HRRequest,
                "HRRequest {Operation} FAILED - Type: {RequestType}, ID: {RequestId}, Error: {ErrorMessage}",
                properties);
        }
    }

    public void LogReferenceDataSync(bool success, string entityType, int addedCount, int updatedCount, int skippedCount, string? errorMessage = null)
    {
        var properties = new Dictionary<string, object>
        {
            { "EntityType", entityType },
            { "AddedCount", addedCount },
            { "UpdatedCount", updatedCount },
            { "SkippedCount", skippedCount }
        };

        if (success)
        {
            LogSuccess(LogCategory.ReferenceDataSync,
                "ReferenceDataSync {EntityType} - Added: {AddedCount}, Updated: {UpdatedCount}, Skipped: {SkippedCount}",
                properties);
        }
        else
        {
            properties["ErrorMessage"] = errorMessage ?? "Unknown error";
            LogError(LogCategory.ReferenceDataSync,
                "ReferenceDataSync {EntityType} FAILED - Error: {ErrorMessage}",
                properties);
        }
    }

    #endregion

    #region Private Helpers

    private void PushProperties(Dictionary<string, object> properties)
    {
        foreach (var prop in properties)
        {
            LogContext.PushProperty(prop.Key, prop.Value);
        }
    }

    /// <summary>
    /// Returns the appropriate error/warning logger based on the log category.
    /// AD errors go to ecm_log_error_AD, EmailNotification errors go to ecm_log_error_EmailNotif,
    /// all other errors go to the general ecm_log_error.
    /// </summary>
    private ILogger GetErrorLogger(LogCategory category)
    {
        return category switch
        {
            LogCategory.ActiveDirectory => _adErrorLogger,
            LogCategory.EmailNotification => _emailNotifErrorLogger,
            _ => _errorLogger,
        };
    }

    #endregion
}

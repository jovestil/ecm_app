using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Constants;
using Mathy.ELM.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Mathy.ELM.Infrastructure.Data;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Entities;
using Enums = Mathy.ELM.Core.Enums;
using Entities = Mathy.ELM.Core.Entities;
using System.Text.Json;

namespace Mathy.ELM.Infrastructure.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly IEcmLogger _ecmLogger;
    private readonly IHRRequestService _hrRequestService;
    private readonly IReferenceDataService _referenceDataService;
    private readonly IViewpointService _viewpointService;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IAzureServiceBusEmailService _azureServiceBusEmailService;

    public BackgroundJobService(
        ILogger<BackgroundJobService> logger,
        IEcmLogger ecmLogger,
        IHRRequestService hrRequestService,
        IReferenceDataService referenceDataService,
        IViewpointService viewpointService,
        INotificationService notificationService,
        IEmailService emailService,
        IServiceScopeFactory serviceScopeFactory,
        IAzureServiceBusEmailService azureServiceBusEmailService)
    {
        _logger = logger;
        _ecmLogger = ecmLogger;
        _hrRequestService = hrRequestService;
        _referenceDataService = referenceDataService;
        _viewpointService = viewpointService;
        _notificationService = notificationService;
        _emailService = emailService;
        _serviceScopeFactory = serviceScopeFactory;
        _azureServiceBusEmailService = azureServiceBusEmailService;
    }

    public string EnqueueNotificationJob(int hrRequestId)
    {
        _logger.LogInformation("Enqueuing notification job for HR request {HRRequestId}", hrRequestId);

        var jobId = BackgroundJob.Enqueue(() => ProcessHRRequestNotificationAsync(hrRequestId));

        _ecmLogger.LogBackgroundJob(
            success: true,
            jobName: "EnqueueNotificationJob",
            operation: $"Enqueued notification job for HR request {hrRequestId}",
            jobId: jobId,
            errorMessage: null);

        return jobId;
    }

    public string ScheduleFollowUpJob(int hrRequestId, DateTime scheduledDate)
    {
        _logger.LogInformation("Scheduling follow-up job for HR request {HRRequestId} at {ScheduledDate}", 
            hrRequestId, scheduledDate);
        
        return BackgroundJob.Schedule(() => ProcessFollowUpAsync(hrRequestId), scheduledDate);
    }

    public void SetupReferenceDataSyncJob()
    {
        _logger.LogInformation("Setting up recurring reference data sync job");
        
        // Run every day at 2 AM
        RecurringJob.AddOrUpdate(
            "sync-reference-data",
            () => SyncReferenceDataAsync(),
            Cron.Daily(2));
    }

    public async Task ProcessHRRequestNotificationAsync(int hrRequestId)
    {
        try
        {
            _logger.LogInformation("Processing notification for HR request {HRRequestId}", hrRequestId);
            
            // Get the HR request
            var hrRequestResponse = await _hrRequestService.GetHRRequestByIdAsync(hrRequestId);
            if (!hrRequestResponse.Success || hrRequestResponse.Data == null)
            {
                _ecmLogger.LogWarning(LogCategory.BackgroundJob, "HR request {HRRequestId} not found", hrRequestId);

                _ecmLogger.LogBackgroundJob(
                    success: false,
                    jobName: "ProcessHRRequestNotification",
                    operation: $"Process notification for HR request {hrRequestId}",
                    jobId: null,
                    errorMessage: $"HR request {hrRequestId} not found");

                return;
            }

            // TODO: Implement actual notification logic
            // This could include:
            // - Sending email notifications
            // - Creating system notifications
            // - Integrating with external systems
            // - Updating request status

            _logger.LogInformation("Successfully processed notification for HR request {HRRequestId}", hrRequestId);

            _ecmLogger.LogBackgroundJob(
                success: true,
                jobName: "ProcessHRRequestNotification",
                operation: $"Successfully processed notification for HR request {hrRequestId}",
                jobId: null,
                errorMessage: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification for HR request {HRRequestId}", hrRequestId);

            _ecmLogger.LogBackgroundJob(
                success: false,
                jobName: "ProcessHRRequestNotification",
                operation: $"Process notification for HR request {hrRequestId}",
                jobId: null,
                errorMessage: ex.Message);

            throw; // Re-throw to allow Hangfire to handle retry logic
        }
    }

    public async Task ProcessFollowUpAsync(int hrRequestId)
    {
        try
        {
            _logger.LogInformation("Processing follow-up for HR request {HRRequestId}", hrRequestId);
            
            // Get the HR request
            var hrRequestResponse = await _hrRequestService.GetHRRequestByIdAsync(hrRequestId);
            if (!hrRequestResponse.Success || hrRequestResponse.Data == null)
            {
                _ecmLogger.LogWarning(LogCategory.BackgroundJob, "HR request {HRRequestId} not found for follow-up", hrRequestId);
                return;
            }

            // TODO: Implement follow-up logic
            // This could include:
            // - Checking request status
            // - Sending reminder notifications
            // - Escalating overdue requests
            // - Updating approvals or workflows
            
            _logger.LogInformation("Successfully processed follow-up for HR request {HRRequestId}", hrRequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing follow-up for HR request {HRRequestId}", hrRequestId);
            throw; // Re-throw to allow Hangfire to handle retry logic
        }
    }

    public async Task SyncReferenceDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting reference data sync from Viewpoint");
            
            // Create a new scope for the reference data service since this runs in background
            using var scope = _serviceScopeFactory.CreateScope();
            var referenceDataService = scope.ServiceProvider.GetRequiredService<IReferenceDataService>();
            
            // Sync companies from Viewpoint
            _logger.LogInformation("Starting company sync from Viewpoint");
            var companySyncResult = await referenceDataService.SyncCompaniesFromViewpointAsync();

            if (companySyncResult.Success && companySyncResult.Data != null)
            {
                _logger.LogInformation("Company sync completed successfully: {Summary}", companySyncResult.Data.Summary);

                _ecmLogger.LogReferenceDataSync(
                    success: true,
                    entityType: "Company",
                    addedCount: companySyncResult.Data.NewCompaniesAdded,
                    updatedCount: companySyncResult.Data.ExistingCompaniesUpdated,
                    skippedCount: companySyncResult.Data.CompaniesDeactivated,
                    errorMessage: null);
            }
            else
            {
                _ecmLogger.LogWarning(LogCategory.BackgroundJob, "Company sync completed with errors: {Message}. Errors: {Errors}",
                    companySyncResult.Message, string.Join("; ", companySyncResult.Errors ?? new List<string>()));

                // Don't throw exception for partial failures - log and continue
                if (companySyncResult.Data?.Errors?.Count > 0)
                {
                    foreach (var error in companySyncResult.Data.Errors)
                    {
                        _logger.LogError("Company sync error: {Error}", error);
                    }
                }

                _ecmLogger.LogReferenceDataSync(
                    success: false,
                    entityType: "Company",
                    addedCount: companySyncResult.Data?.NewCompaniesAdded ?? 0,
                    updatedCount: companySyncResult.Data?.ExistingCompaniesUpdated ?? 0,
                    skippedCount: companySyncResult.Data?.CompaniesDeactivated ?? 0,
                    errorMessage: companySyncResult.Message);
            }

            // TODO: Add other reference data sync operations:
            // await referenceDataService.SyncPositionsAsync();
            // await referenceDataService.SyncDepartmentsAsync();
            // await referenceDataService.SyncPayrollGroupsAsync();

            _logger.LogInformation("Successfully completed reference data sync");

            _ecmLogger.LogBackgroundJob(
                success: true,
                jobName: "SyncReferenceData",
                operation: "Successfully completed reference data sync",
                jobId: null,
                errorMessage: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reference data sync");

            _ecmLogger.LogBackgroundJob(
                success: false,
                jobName: "SyncReferenceData",
                operation: "Reference data sync failed",
                jobId: null,
                errorMessage: ex.Message);

            throw; // Re-throw to allow Hangfire to handle retry logic
        }
    }

    public async Task<string> ScheduleViewpointStatusUpdateJob(int hrRequestDetailId, DateTime effectiveDate, int? requestTypeId = null, string? submitterEmail = null)
    {
        // Treat EffectiveDate as a server-local calendar date (Central Time in production).
        // Unspecified-Kind DateTimes are treated as UTC by Hangfire, which shifts the job a day
        // earlier in local time for timezones west of UTC.
        var scheduledDate = DateTime.SpecifyKind(effectiveDate.Date, DateTimeKind.Local);

        if (requestTypeId.HasValue)
        {
            _logger.LogInformation("Scheduling Viewpoint status update job for HR request detail {HRRequestDetailId} at {ScheduledDate} (local) for request type ID {RequestTypeId}",
                hrRequestDetailId, scheduledDate, requestTypeId.Value);
        }
        else
        {
            _logger.LogInformation("Scheduling Viewpoint status update job for HR request detail {HRRequestDetailId} at {ScheduledDate} (local)",
                hrRequestDetailId, scheduledDate);
        }

        // Convert requestTypeId to RequestType enum if provided
        Core.Enums.RequestType? requestTypeEnum = null;
        if (requestTypeId.HasValue)
        {
            requestTypeEnum = (Core.Enums.RequestType)requestTypeId.Value;
        }

        // Schedule the Hangfire job
        var jobId = BackgroundJob.Schedule(() => UpdateEmployeeStatusInViewpointAsync(hrRequestDetailId, requestTypeEnum, submitterEmail), scheduledDate);
        
        // Store the job ID in the HR request detail using a separate DbContext scope
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        
        var requestDetail = await context.HRRequestDetails.FindAsync(hrRequestDetailId);
        if (requestDetail != null)
        {
            requestDetail.HangfireJobId = jobId;
            requestDetail.ModifiedDate = DateTime.UtcNow;
            // TODO: Set ModifiedBy to system user
            
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Stored Hangfire job ID {JobId} for HR request detail {HRRequestDetailId}",
                jobId, hrRequestDetailId);

            _ecmLogger.LogBackgroundJob(
                success: true,
                jobName: "ScheduleViewpointStatusUpdateJob",
                operation: $"Scheduled Viewpoint status update for HR request detail {hrRequestDetailId} at {effectiveDate}",
                jobId: jobId,
                errorMessage: null);
        }
        else
        {
            _ecmLogger.LogWarning(LogCategory.BackgroundJob, "HR request detail {HRRequestDetailId} not found when trying to store job ID {JobId}",
                hrRequestDetailId, jobId);

            _ecmLogger.LogBackgroundJob(
                success: false,
                jobName: "ScheduleViewpointStatusUpdateJob",
                operation: $"Failed to store job ID for HR request detail {hrRequestDetailId}",
                jobId: jobId,
                errorMessage: $"HR request detail {hrRequestDetailId} not found");
        }

        return jobId;
    }

    public async Task UpdateEmployeeStatusInViewpointAsync(int hrRequestDetailId, Core.Enums.RequestType? requestType = null, string? submitterEmail = null)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        
        try
        {
            var requestTypeText = requestType?.ToString() ?? "Unknown";
            _logger.LogInformation("Processing Viewpoint status update for HR request detail {HRRequestDetailId} with request type {RequestType}", 
                hrRequestDetailId, requestTypeText);
            
            // Get the HR request detail with related data - conditionally include RequestType if not provided
            IQueryable<Core.Entities.HRRequestDetail> query = context.HRRequestDetails
                .Include(rd => rd.ParentRequest);
                
            if (requestType == null)
            {
                query = query.Include(rd => rd.RequestType);
            }
            
            var requestDetail = await query.FirstOrDefaultAsync(rd => rd.Id == hrRequestDetailId);

            if (requestDetail == null)
            {
                _ecmLogger.LogWarning(LogCategory.BackgroundJob, "HR request detail {HRRequestDetailId} not found", hrRequestDetailId);

                _ecmLogger.LogBackgroundJob(
                    success: false,
                    jobName: "UpdateEmployeeStatusInViewpoint",
                    operation: $"Update employee status for HR request detail {hrRequestDetailId}",
                    jobId: null,
                    errorMessage: $"HR request detail {hrRequestDetailId} not found");

                return;
            }
            
            // Set status to Processing now that the job is actually executing
            requestDetail.RequestStatusId = (int)Enums.RequestStatus.Processing;
            requestDetail.ModifiedDate = DateTime.UtcNow;
            // TODO: Set ModifiedBy to system user
            
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Set HR request detail {HRRequestDetailId} status to Processing as job begins execution", hrRequestDetailId);

            // Use provided requestType or fall back to database value
            Core.Enums.RequestType effectiveRequestType;
            if (requestType.HasValue)
            {
                effectiveRequestType = requestType.Value;
                _logger.LogInformation("Using provided request type {RequestType} for HR request detail {HRRequestDetailId}", 
                    effectiveRequestType, hrRequestDetailId);
            }
            else
            {
                if (requestDetail.RequestType == null)
                {
                    _ecmLogger.LogWarning(LogCategory.BackgroundJob, "Request type not found for HR request detail {HRRequestDetailId} and none was provided", hrRequestDetailId);
                    return;
                }
                effectiveRequestType = (Core.Enums.RequestType)requestDetail.RequestTypeId;
                _logger.LogInformation("Retrieved request type {RequestType} from database for HR request detail {HRRequestDetailId}", 
                    effectiveRequestType, hrRequestDetailId);
            }

            // Status is already set to InProgress when job was scheduled

            // Handle request type-specific processing
            switch (effectiveRequestType)
            {
                case Core.Enums.RequestType.Promotion:
                    // Promotion/Transfer has special processing to update employee fields
                    await UpdateEmployeeForPromotionTransferInViewPointAsync(requestDetail, submitterEmail);
                    return;

                case Core.Enums.RequestType.Termination:
                    // Termination has special processing to update Status, ActiveYN, TermDate, TermReason
                    await UpdateEmployeeForTerminationInViewPointAsync(context, requestDetail, submitterEmail);
                    return;

                case Core.Enums.RequestType.ReturnToWork:
                    // ReturnToWork has special processing to update Status, ActiveYN, udReturntoworkdate
                    await UpdateEmployeeForReturnToWorkInViewPointAsync(context, requestDetail, submitterEmail);
                    return;

                case Core.Enums.RequestType.Layoff:
                    // Layoff uses the generic status update logic below
                    break;

                default:
                    _ecmLogger.LogWarning(LogCategory.BackgroundJob, "Unknown request type {RequestType} for HR request detail {HRRequestDetailId}",
                        effectiveRequestType, hrRequestDetailId);
                    break;
            }

            // Get the employee from Viewpoint
            var employee = await _viewpointService.GetEmployeeByNumberAsync(requestDetail.EmployeeId.ToString());

            if (employee == null)
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Employee {EmployeeId} not found in Viewpoint for HR request detail {HRRequestDetailId}",
                    requestDetail.EmployeeId, hrRequestDetailId);
                return;
            }

            // Get initial status for logging purposes (will be replaced by actual transformed status)
            var initialTargetStatus = ViewpointEmployeeStatus.GetStatusForRequestType(effectiveRequestType);

            _logger.LogInformation("Starting employee {EmployeeId} status update for request type {RequestType} (initial target: {InitialStatus})",
                requestDetail.EmployeeId, effectiveRequestType, initialTargetStatus);

            requestDetail.ViewpointProcessed = true;
            requestDetail.ViewpointProcessedDate = DateTime.UtcNow;

            // Send SignalR notification for status update to Processing
            var employeeName = GetEmployeeDisplayName(employee);
            await _notificationService.SendHRRequestStatusUpdateAsync(
                "system",
                hrRequestDetailId,
                Enums.RequestStatus.Processing.ToString(),
                employeeName,
                "Employee status update is now in progress"
            );
            // Status was set to Processing at the beginning of job execution
            await context.SaveChangesAsync();


            // Update the employee status in Viewpoint
            var updateResult = await _viewpointService.UpdateEmployeeStatusInViewpointAsync(new List<Core.DTOs.ViewpointEmployeeDto> { employee }, initialTargetStatus, effectiveRequestType);

            if (updateResult.Success)
            {
                _logger.LogInformation("Successfully queued employee {EmployeeId} status update to '{ActualStatus}' in Viewpoint for HR request detail {HRRequestDetailId}. ActionId: {ActionId}",
                    requestDetail.EmployeeId, updateResult.ActualStatusUsed, hrRequestDetailId, updateResult.ActionId);

                // Verify the action was successfully processed in Viewpoint
                // Poll for up to 10 minutes (10 attempts with 1 minute intervals)
                ViewpointActionDetailResponseDto? verificationResult = null;
                int maxRetries = 10;
                int retryCount = 0;
                int delayMilliseconds = 60000; // 1 minute

                while (retryCount < maxRetries)
                {
                    verificationResult = await _viewpointService.VerifyViewpointActionAsync(updateResult.ActionId);

                    if (verificationResult == null)
                    {
                        _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Verification attempt {Attempt}/{MaxRetries} failed - no response from Viewpoint for employee {EmployeeName} ({EmployeeId})",
                            retryCount + 1, maxRetries, GetEmployeeDisplayName(employee), requestDetail.EmployeeId);

                        var noResponseError = "Failed to get verification response from Viewpoint";
                        requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                        requestDetail.ViewpointErrorMessage = noResponseError;
                        requestDetail.ModifiedDate = DateTime.UtcNow;
                        await context.SaveChangesAsync();

                        // Send failure notification
                        employeeName = GetEmployeeDisplayName(employee);
                        await _notificationService.SendHRRequestCompletionNotificationAsync(
                            submitterEmail ?? "system",
                            hrRequestDetailId,
                            employeeName,
                            false,
                            $"{effectiveRequestType} request failed: {noResponseError}"
                        );

                        // Send Failed Request email notification
                        await SendFailedRequestEmailAsync(context, requestDetail, noResponseError, submitterEmail);
                        return;
                    }

                    _logger.LogInformation("Verification attempt {Attempt}/{MaxRetries} - Status: {Status} for employee {EmployeeId}",
                        retryCount + 1, maxRetries, verificationResult.Status ?? "null", requestDetail.EmployeeId);

                    // Check if action completed successfully
                    if (string.Equals(verificationResult.Status, "Successful", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("✅ Action completed successfully for employee {EmployeeId}", requestDetail.EmployeeId);
                        break;
                    }

                    // Check if action failed
                    if (string.Equals(verificationResult.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                    {
                        var actionFailedContext = verificationResult.ContextJson != null
                            ? JsonSerializer.Serialize(verificationResult.ContextJson)
                            : "null";
                        _ecmLogger.LogError(LogCategory.ViewpointIntegration,
                            "Action failed in Viewpoint for employee {EmployeeName} ({EmployeeId}). ActionId={ActionId}, Status={Status}, Context={Context}",
                            GetEmployeeDisplayName(employee), requestDetail.EmployeeId, verificationResult.Id, verificationResult.Status, actionFailedContext);

                        var actionFailedError = "Viewpoint action failed";
                        requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                        requestDetail.ViewpointErrorMessage = actionFailedError;
                        requestDetail.ModifiedDate = DateTime.UtcNow;
                        await context.SaveChangesAsync();

                        // Send failure notification
                        employeeName = GetEmployeeDisplayName(employee);
                        await _notificationService.SendHRRequestCompletionNotificationAsync(
                            submitterEmail ?? "system",
                            hrRequestDetailId,
                            employeeName,
                            false,
                            $"{effectiveRequestType} request failed: {actionFailedError}"
                        );

                        // Send Failed Request email notification
                        await SendFailedRequestEmailAsync(context, requestDetail, actionFailedError, submitterEmail);
                        return;
                    }

                    // If status is still "Queued" or other intermediate status, wait and retry
                    retryCount++;

                    if (retryCount < maxRetries)
                    {
                        _logger.LogInformation("Action still processing (Status: {Status}). Waiting {Delay} seconds before retry {Retry}/{MaxRetries}...",
                            verificationResult.Status, delayMilliseconds / 1000, retryCount + 1, maxRetries);

                        await Task.Delay(delayMilliseconds);
                    }
                }

                // Check if we exhausted all retries without success
                if (retryCount >= maxRetries && !string.Equals(verificationResult?.Status, "Successful", StringComparison.OrdinalIgnoreCase))
                {
                    _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Verification timed out after {MaxRetries} attempts for employee {EmployeeName} ({EmployeeId}). Final status: {Status}",
                        maxRetries, GetEmployeeDisplayName(employee), requestDetail.EmployeeId, verificationResult?.Status ?? "unknown");

                    var timeoutError = $"Verification timed out after {maxRetries} attempts. Final status: {verificationResult?.Status ?? "unknown"}";
                    requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    requestDetail.ViewpointErrorMessage = timeoutError;
                    requestDetail.ModifiedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    // Send failure notification
                    employeeName = GetEmployeeDisplayName(employee);
                    await _notificationService.SendHRRequestCompletionNotificationAsync(
                        submitterEmail ?? "system",
                        hrRequestDetailId,
                        employeeName,
                        false,
                        $"{effectiveRequestType} request failed: {timeoutError}"
                    );

                    // Send Failed Request email notification
                    await SendFailedRequestEmailAsync(context, requestDetail, timeoutError, submitterEmail);
                    return;
                }

                _logger.LogInformation("✅ Verification successful for employee {EmployeeId}", requestDetail.EmployeeId);

                // Update the local Employee table to keep it in sync with Viewpoint
                await UpdateLocalEmployeeAfterViewpointSuccessAsync(
                    context,
                    requestDetail.EmployeeId,
                    requestDetail.EmployeeCompanyCode,
                    effectiveRequestType,
                    updateResult.ActualStatusUsed,
                    requestDetail.EffectiveDate);

                // Set status to Completed
                requestDetail.RequestStatusId = (int)Enums.RequestStatus.Completed;
                requestDetail.ViewpointErrorMessage = null;
                requestDetail.ModifiedDate = DateTime.UtcNow;
                await context.SaveChangesAsync();

                // Send success notification
                employeeName = GetEmployeeDisplayName(employee);
                await _notificationService.SendHRRequestCompletionNotificationAsync(
                    submitterEmail ?? "system",
                    hrRequestDetailId,
                    employeeName,
                    true,
                    $"{effectiveRequestType} request completed successfully. Employee status updated in Viewpoint."
                );

                _logger.LogInformation("✅ Set HR request detail {HRRequestDetailId} status to Completed", hrRequestDetailId);

                _ecmLogger.LogBackgroundJob(
                    success: true,
                    jobName: "UpdateEmployeeStatusInViewpoint",
                    operation: $"Successfully updated employee {requestDetail.EmployeeId} status in Viewpoint for HR request detail {hrRequestDetailId}",
                    jobId: null,
                    errorMessage: null);
            }
            else
            {
                _logger.LogError("Failed to update employee {EmployeeId} status in Viewpoint for HR request detail {HRRequestDetailId}. Error: {ErrorMessage}",
                    requestDetail.EmployeeId, hrRequestDetailId, updateResult.ErrorMessage);

                // Update the request detail status to Failed
                var failedErrorMessage = updateResult.ErrorMessage ?? "Failed to update employee status in Viewpoint";
                requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                requestDetail.ViewpointErrorMessage = failedErrorMessage;

                // Send SignalR notification for status update to Failed
                employeeName = GetEmployeeDisplayName(employee);
                await _notificationService.SendHRRequestCompletionNotificationAsync(
                    submitterEmail ?? "system",
                    hrRequestDetailId,
                    employeeName,
                    false,
                    failedErrorMessage
                );
                await context.SaveChangesAsync();

                // Send Failed Request email notification
                await SendFailedRequestEmailAsync(context, requestDetail, failedErrorMessage, submitterEmail);

                _logger.LogInformation("Set HR request detail {HRRequestDetailId} status to Failed", hrRequestDetailId);

                _ecmLogger.LogBackgroundJob(
                    success: false,
                    jobName: "UpdateEmployeeStatusInViewpoint",
                    operation: $"Failed to update employee {requestDetail.EmployeeId} status in Viewpoint for HR request detail {hrRequestDetailId}",
                    jobId: null,
                    errorMessage: failedErrorMessage);

                throw new InvalidOperationException($"Failed to update employee {requestDetail.EmployeeId} status in Viewpoint: {updateResult.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Viewpoint status update for HR request detail {HRRequestDetailId}", hrRequestDetailId);

            _ecmLogger.LogBackgroundJob(
                success: false,
                jobName: "UpdateEmployeeStatusInViewpoint",
                operation: $"Error processing Viewpoint status update for HR request detail {hrRequestDetailId}",
                jobId: null,
                errorMessage: ex.Message);
            
            // Update the request detail status to Failed on exception
            try
            {
                var requestDetail = await context.HRRequestDetails.FindAsync(hrRequestDetailId);
                if (requestDetail != null)
                {
                    var exceptionErrorMessage = $"Exception during processing: {ex.Message}";
                    requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    requestDetail.ViewpointErrorMessage = exceptionErrorMessage;

                    // Send SignalR notification for status update to Failed due to exception
                    await _notificationService.SendHRRequestCompletionNotificationAsync(
                        "system",
                        hrRequestDetailId,
                        $"Employee {requestDetail.EmployeeId}",
                        false,
                        exceptionErrorMessage
                    );
                    await context.SaveChangesAsync();

                    // Send Failed Request email notification
                    await SendFailedRequestEmailAsync(context, requestDetail, exceptionErrorMessage, submitterEmail);

                    _logger.LogInformation("Set HR request detail {HRRequestDetailId} status to Failed due to exception", hrRequestDetailId);
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to update status to Failed for HR request detail {HRRequestDetailId}", hrRequestDetailId);
            }
            
            throw; // Re-throw to allow Hangfire to handle retry logic
        }
    }

    public async Task VerifyViewpointStatusUpdateAsync(int hrRequestDetailId, string expectedStatus, int attemptNumber = 1, string? submitterEmail = null)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        
        try
        {
            _logger.LogInformation("Verifying Viewpoint status update for HR request detail {HRRequestDetailId}, expected status: {ExpectedStatus}, attempt: {AttemptNumber}/3", 
                hrRequestDetailId, expectedStatus, attemptNumber);

            // Get the HR request detail with related data
            var requestDetail = await context.HRRequestDetails
                .Include(rd => rd.ParentRequest)
                .Include(rd => rd.RequestType)
                .FirstOrDefaultAsync(rd => rd.Id == hrRequestDetailId);

            if (requestDetail == null)
            {
                _ecmLogger.LogWarning(LogCategory.BackgroundJob, "HR request detail {HRRequestDetailId} not found for verification", hrRequestDetailId);
                return;
            }

            // Skip verification for Promotion requests
            // Promotion updates multiple fields (7 fields), not just status, so status verification doesn't apply
            // Promotion requests are marked as Completed immediately after Viewpoint API succeeds
            if (requestDetail.RequestTypeId == (int)Core.Enums.RequestType.Promotion)
            {
                _logger.LogInformation("Skipping verification for Promotion request {HRRequestDetailId} - Promotion updates multiple fields, not just status",
                    hrRequestDetailId);
                return;
            }

            // Get the current employee data from Viewpoint
            var employee = await _viewpointService.GetEmployeeByNumberAsync(requestDetail.EmployeeId.ToString());
            
             if (employee == null)
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Employee {EmployeeId} not found in Viewpoint during verification for HR request detail {HRRequestDetailId}",
                    requestDetail.EmployeeId, hrRequestDetailId);
                
                // Set status to Failed since employee not found during verification
                var empNotFoundError = "Employee not found during verification";
                requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                requestDetail.ViewpointErrorMessage = empNotFoundError;
                await context.SaveChangesAsync();

                _logger.LogInformation("Set HR request detail {HRRequestDetailId} status to Failed - employee not found during verification", hrRequestDetailId);

                // Send completion notification for employee not found
                await _notificationService.SendHRRequestCompletionNotificationAsync(
                    "system",
                    hrRequestDetailId,
                    $"Employee {requestDetail.EmployeeId}",
                    false,
                    empNotFoundError
                );

                // Send Failed Request email notification
                await SendFailedRequestEmailAsync(context, requestDetail, empNotFoundError, submitterEmail);

                // Schedule email notification job for employee not found
                var emailJobId = BackgroundJob.Enqueue(() => SendCompletionEmailNotificationAsync(hrRequestDetailId, false, empNotFoundError, submitterEmail));
                _logger.LogInformation("Scheduled email notification job {EmailJobId} for employee not found in HR request detail {HRRequestDetailId}", 
                    emailJobId, hrRequestDetailId);
                
                return;
            }

            // Check if the status matches what we expected
            if (string.Equals(employee.Status, expectedStatus, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Verification successful: Employee {EmployeeId} has expected status '{ExpectedStatus}' in Viewpoint for HR request detail {HRRequestDetailId}", 
                    requestDetail.EmployeeId, expectedStatus, hrRequestDetailId);
                
                // Set status to Completed now that verification is successful
                requestDetail.RequestStatusId = (int)Enums.RequestStatus.Completed;
                requestDetail.ViewpointErrorMessage = null; // Clear any previous error message
                
                // Update the employee table status to match Viewpoint
                var employeeRecord = await context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeNumber == requestDetail.EmployeeId && !e.IsDeleted);
                
                if (employeeRecord != null)
                {
                    employeeRecord.EmploymentStatus = expectedStatus;
                    employeeRecord.ModifiedDate = DateTime.UtcNow;
                    employeeRecord.ModifiedBy = 1; // System user
                    
                    _logger.LogInformation("Updated employee {EmployeeId} status to '{ExpectedStatus}' in local database", 
                        requestDetail.EmployeeId, expectedStatus);
                }
                else
                {
                    _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Employee {EmployeeId} not found in local database, skipping status update",
                        requestDetail.EmployeeId);
                }
                
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Set HR request detail {HRRequestDetailId} status to Completed after successful verification", hrRequestDetailId);

                // Send completion notification for successful verification
                var employeeName = GetEmployeeDisplayName(employee);
                await _notificationService.SendHRRequestCompletionNotificationAsync(
                    "system",
                    hrRequestDetailId,
                    employeeName,
                    true,
                    $"Status update completed successfully. Employee status is now '{expectedStatus}'."
                );

                // Schedule email notification job for successful completion
                var emailJobId = BackgroundJob.Enqueue(() => SendCompletionEmailNotificationAsync(hrRequestDetailId, true, $"Status update completed successfully. Employee status is now '{expectedStatus}'.", submitterEmail));
                _logger.LogInformation("Scheduled email notification job {EmailJobId} for successful completion of HR request detail {HRRequestDetailId}", 
                    emailJobId, hrRequestDetailId);
            }
            else
            {
                var errorMessage = $"Status verification failed. Expected: '{expectedStatus}', Actual: '{employee.Status}'";
                _logger.LogError("Verification failed: Employee {EmployeeId} has status '{ActualStatus}' but expected '{ExpectedStatus}' for HR request detail {HRRequestDetailId}, attempt {AttemptNumber}/2", 
                    requestDetail.EmployeeId, employee.Status, expectedStatus, hrRequestDetailId, attemptNumber);
                
                // Check if we should retry (max 2 attempts)
                if (attemptNumber < 2)
                {
                    var nextAttempt = attemptNumber + 1;
                    _logger.LogInformation("Scheduling retry verification attempt {NextAttempt}/2 for HR request detail {HRRequestDetailId} in 2 minutes", 
                        nextAttempt, hrRequestDetailId);
                    
                    // Schedule another verification attempt in 2 minutes
                    var retryJobId = BackgroundJob.Schedule(() => VerifyViewpointStatusUpdateAsync(hrRequestDetailId, expectedStatus, nextAttempt, submitterEmail), TimeSpan.FromMinutes(2));
                    _logger.LogInformation("Scheduled retry verification job {RetryJobId} for HR request detail {HRRequestDetailId}", 
                        retryJobId, hrRequestDetailId);
                }
                else
                {
                    // All retries exhausted, set status to Failed
                    _logger.LogError("All verification attempts exhausted (2/2) for HR request detail {HRRequestDetailId}, setting status to Failed", hrRequestDetailId);

                    var verifyFailedError = errorMessage + $" (Failed after {attemptNumber} attempts)";
                    requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    requestDetail.ViewpointErrorMessage = verifyFailedError;
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Set HR request detail {HRRequestDetailId} status to Failed after {AttemptNumber} verification attempts", hrRequestDetailId, attemptNumber);

                    // Send completion notification for failed verification after all retries
                    var employeeNameFailed = GetEmployeeDisplayName(employee);
                    await _notificationService.SendHRRequestCompletionNotificationAsync(
                        "system",
                        hrRequestDetailId,
                        employeeNameFailed,
                        false,
                        verifyFailedError
                    );

                    // Send Failed Request email notification
                    await SendFailedRequestEmailAsync(context, requestDetail, verifyFailedError, submitterEmail);

                    // Schedule email notification job for failed verification
                    var emailJobId = BackgroundJob.Enqueue(() => SendCompletionEmailNotificationAsync(hrRequestDetailId, false, verifyFailedError, submitterEmail));
                    _logger.LogInformation("Scheduled email notification job {EmailJobId} for failed completion of HR request detail {HRRequestDetailId}", 
                        emailJobId, hrRequestDetailId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Viewpoint status verification for HR request detail {HRRequestDetailId}", hrRequestDetailId);
            
            // Set status to Failed and update error message in the database
            try
            {
                var requestDetail = await context.HRRequestDetails
                    .Include(rd => rd.ParentRequest)
                    .FirstOrDefaultAsync(rd => rd.Id == hrRequestDetailId);
                if (requestDetail != null)
                {
                    var verifyExError = $"Verification error: {ex.Message}";
                    requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    requestDetail.ViewpointErrorMessage = verifyExError;
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Set HR request detail {HRRequestDetailId} status to Failed due to verification exception", hrRequestDetailId);

                    // Send completion notification for exception
                    await _notificationService.SendHRRequestCompletionNotificationAsync(
                        "system",
                        hrRequestDetailId,
                        $"Employee {requestDetail.EmployeeId}",
                        false,
                        verifyExError
                    );

                    // Send Failed Request email notification
                    await SendFailedRequestEmailAsync(context, requestDetail, verifyExError, submitterEmail);
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to update status to Failed for HR request detail {HRRequestDetailId}", hrRequestDetailId);
            }

            throw; // Re-throw to allow Hangfire to handle retry logic
        }
    }

    public async Task SendCompletionEmailNotificationAsync(int hrRequestDetailId, bool isSuccess, string statusMessage, string? submitterEmail = null)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        
        try
        {
            _logger.LogInformation("Processing email notification for HR request detail {HRRequestDetailId}, success: {IsSuccess}", 
                hrRequestDetailId, isSuccess);

            // Get the HR request detail with related data
            var requestDetail = await context.HRRequestDetails
                .Include(rd => rd.ParentRequest)
                .FirstOrDefaultAsync(rd => rd.Id == hrRequestDetailId);

            if (requestDetail == null)
            {
                _ecmLogger.LogWarning(LogCategory.EmailNotification, "HR request detail {HRRequestDetailId} not found for email notification", hrRequestDetailId);

                _ecmLogger.LogEmailNotification(
                    success: false,
                    operation: "SendCompletionEmail",
                    recipient: null,
                    subject: null,
                    errorMessage: $"HR request detail {hrRequestDetailId} not found");

                return;
            }

            // Get employee data for the notification
            var employee = await _viewpointService.GetEmployeeByNumberAsync(requestDetail.EmployeeId.ToString());
            var employeeName = employee != null ? GetEmployeeDisplayName(employee) : $"Employee {requestDetail.EmployeeId}";

            // Create email notification entry in NotificationQueue
            var emailSubject = isSuccess
                ? $"HR Request #{requestDetail.ParentRequestId} - Employee Status Update Completed"
                : $"HR Request #{requestDetail.ParentRequestId} - Employee Status Update Failed";

            var emailBody = isSuccess
                ? $"Dear User,\n\n" +
                  $"Your HR request for {employeeName} has been completed successfully.\n\n" +
                  $"Details:\n" +
                  $"- Employee: {employeeName} (ID: {requestDetail.EmployeeId})\n" +
                  $"- Request ID: #{requestDetail.ParentRequestId}\n" +
                  $"- Completion Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm}\n" +
                  $"- Status: {statusMessage}\n\n" +
                  $"Thank you for using the HR Change Management System.\n\n" +
                  $"Best regards,\nHR System"
                : $"Dear User,\n\n" +
                  $"Your HR request for {employeeName} has failed.\n\n" +
                  $"Details:\n" +
                  $"- Employee: {employeeName} (ID: {requestDetail.EmployeeId})\n" +
                  $"- Request ID: #{requestDetail.ParentRequestId}\n" +
                  $"- Failure Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm}\n" +
                  $"- Error: {statusMessage}\n\n" +
                  $"Please contact HR or IT support for assistance.\n\n" +
                  $"Best regards,\nHR System";

            // Use provided submitter email or fallback to placeholder
            var recipientEmail = !string.IsNullOrEmpty(submitterEmail)
                ? submitterEmail
                : $"user-{requestDetail.ParentRequest.SubmittedBy}@placeholder.com";

            // Send email directly using email service
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var emailResult = await emailService.SendEmailAsync(recipientEmail, emailSubject, emailBody);

            if (emailResult.Success)
            {
                _logger.LogInformation("Email notification sent successfully for HR request detail {HRRequestDetailId} to {RecipientEmail}",
                    hrRequestDetailId, recipientEmail);

                _ecmLogger.LogEmailNotification(
                    success: true,
                    operation: "SendCompletionEmail",
                    recipient: recipientEmail,
                    subject: emailSubject,
                    errorMessage: null);
            }
            else
            {
                _logger.LogError("Failed to send email notification for HR request detail {HRRequestDetailId} to {RecipientEmail}. Error: {Error}",
                    hrRequestDetailId, recipientEmail, emailResult.Message);

                _ecmLogger.LogEmailNotification(
                    success: false,
                    operation: "SendCompletionEmail",
                    recipient: recipientEmail,
                    subject: emailSubject,
                    errorMessage: emailResult.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating email notification for HR request detail {HRRequestDetailId}", hrRequestDetailId);

            _ecmLogger.LogEmailNotification(
                success: false,
                operation: "SendCompletionEmail",
                recipient: null,
                subject: null,
                errorMessage: ex.Message);

            throw; // Re-throw to allow Hangfire to handle retry logic
        }
    }

    /// <summary>
    /// Get display name for employee from Viewpoint data
    /// </summary>
    private string GetEmployeeDisplayName(Core.DTOs.ViewpointEmployeeDto employee)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(employee.FirstName))
            parts.Add(employee.FirstName);

        if (!string.IsNullOrWhiteSpace(employee.LastName))
            parts.Add(employee.LastName);

        if (parts.Count > 0)
            return string.Join(" ", parts);

        // Fallback to employee number if name not available
        return $"Employee {employee.HRRef ?? employee.PREmp}";
    }

    /// <summary>
    /// Maps NewHireRequestDetail entity to CreateNewHireRequestDto for email templating
    /// </summary>
    private Core.DTOs.CreateNewHireRequestDto MapNewHireDetailsToDto(Core.Entities.NewHireRequestDetail newHireDetails, Core.Entities.HRRequestDetail requestDetail)
    {
        var dto = new Core.DTOs.CreateNewHireRequestDto
        {
            PersonalInfo = new Core.DTOs.NewHirePersonalInfoDto
            {
                EmployeeId = newHireDetails.EmployeeId,
                FirstName = newHireDetails.FirstName,
                LastName = newHireDetails.LastName,
                PreferredFirstName = newHireDetails.PreferredFirstName,
                FirstDayEmployment = newHireDetails.FirstDayEmployment,
                Rehire = newHireDetails.Rehire,
                ReferredBy = newHireDetails.ReferredBy
            },
            PositionInfo = new Core.DTOs.NewHirePositionInfoDto
            {
                CompanyCode = newHireDetails.CompanyCode,
                LocationCode = newHireDetails.LocationCode,
                EmploymentStatus = newHireDetails.EmploymentStatus,
                IsUnion = newHireDetails.IsUnion,
                UnionCraftId = newHireDetails.UnionCraftId,
                IsApprentice = newHireDetails.IsApprentice,
                IsUnionWage = newHireDetails.IsUnionWage,
                SalaryCode = newHireDetails.SalaryCode,
                PositionCode = newHireDetails.PositionCode,
                PayrollDeptCode = newHireDetails.PayrollDeptCode,
                SupervisorId = newHireDetails.SupervisorId,
                AppPercentage = newHireDetails.AppPercentage
            }
        };

        // Map credit card info if available
        if (newHireDetails.CreditCardDetail != null)
        {
            dto.CreditCardInfo = new Core.DTOs.NewHireCreditCardInfoDto
            {
                KwikTripCard = newHireDetails.CreditCardDetail.KwikTripCard,
                CompanyExpenseCard = newHireDetails.CreditCardDetail.CompanyExpenseCard,
                CreditExpenseType = newHireDetails.CreditCardDetail.CreditExpenseType,
                WeeklyLimit = newHireDetails.CreditCardDetail.WeeklyLimit,
                FuelCardlockAccess = newHireDetails.CreditCardDetail.FuelCardlockAccess,
                FuelCardlockAddress = newHireDetails.CreditCardDetail.FuelCardlockAddress
            };
        }

        return dto;
    }

    public async Task<string> ScheduleViewpointVerifyNewHireEmployee(int hrRequestDetailId, DateTime firstDayEmployment, string? submitterEmail = null)
    {
        // Treat FirstDayEmployment as a server-local calendar date (Central Time in production).
        // Unspecified-Kind DateTimes are treated as UTC by Hangfire, which shifts the job a day
        // earlier in local time for timezones west of UTC.
        var scheduledDate = DateTime.SpecifyKind(firstDayEmployment.Date, DateTimeKind.Local);

        _logger.LogInformation("Scheduling Viewpoint new hire verification job for HR request detail {HRRequestDetailId} at {ScheduledDate} (local) (FirstDayEmployment: {FirstDayEmployment})",
            hrRequestDetailId, scheduledDate, firstDayEmployment);

        // Schedule the Hangfire job
        var jobId = BackgroundJob.Schedule(() => VerifyNewHireEmployeeInViewpointAsync(hrRequestDetailId, 1, submitterEmail), scheduledDate);

        // Note: HangfireJobId will be stored by the calling service (NewHireRequestDetailsService)
        // to avoid race conditions and duplicate database updates
        _logger.LogInformation("Scheduled new hire verification job {JobId} for HR request detail {HRRequestDetailId}",
            jobId, hrRequestDetailId);

        return jobId;
    }

    public async Task VerifyNewHireEmployeeInViewpointAsync(int hrRequestDetailId, int attemptNumber = 1, string? submitterEmail = null)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            _logger.LogInformation("Processing Viewpoint new hire verification for HR request detail {HRRequestDetailId}, attempt {AttemptNumber}",
                hrRequestDetailId, attemptNumber);

            // Get the HR request detail with new hire details
            var requestDetail = await context.HRRequestDetails
                .Include(rd => rd.NewHireDetails)
                .FirstOrDefaultAsync(rd => rd.Id == hrRequestDetailId && !rd.IsDeleted);

            if (requestDetail == null)
            {
                _logger.LogError("HR request detail {HRRequestDetailId} not found", hrRequestDetailId);
                return;
            }

            var newHireDetail = requestDetail.NewHireDetails;
            if (newHireDetail == null)
            {
                _logger.LogError("New hire detail not found for HR request detail {HRRequestDetailId}", hrRequestDetailId);
                return;
            }

            // Set status to Processing when processing starts
            requestDetail.RequestStatusId = (int)Enums.RequestStatus.Processing;
            requestDetail.ModifiedDate = DateTime.UtcNow;
            requestDetail.ModifiedBy = 1; // System user
            await context.SaveChangesAsync();

            // Send SignalR notification for status update to Processing
            await notificationService.SendHRRequestStatusUpdateAsync(
                submitterEmail ?? "system",
                hrRequestDetailId,
                "Processing",
                $"{newHireDetail.FirstName} {newHireDetail.LastName}",
                $"Verifying new hire employee in Viewpoint (attempt {attemptNumber})"
            );

            // Call Viewpoint API to search for the new hire employee
            var viewpointService = scope.ServiceProvider.GetRequiredService<IViewpointService>();
            var searchResult = await viewpointService.SearchEmployeeInNewHireWithAPIAsync(
                newHireDetail.CompanyCode ?? 0,
                (newHireDetail.PayrollDeptCode ?? 0).ToString(),
                newHireDetail.LastName ?? string.Empty,
                (newHireDetail.FirstDayEmployment ?? DateTime.Now).ToString("yyyy-MM-ddTHH:mm:ss")
            );

            if (searchResult != null && searchResult.Any())
            {
                var employeeName = $"{newHireDetail.FirstName} {newHireDetail.LastName}";

                if (searchResult.Count == 1)
                {
                    // Single employee found - verification successful
                    var foundEmployee = searchResult[0];
                    var hrRef = foundEmployee.HRRef;

                    _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                        "New hire employee verification successful for HR request detail {HRRequestDetailId}. Found single matching employee with HRRef: {HRRef}",
                        hrRequestDetailId, hrRef);

                    // Update NewHireRequestDetails with HRRef from Viewpoint
                    if (hrRef.HasValue)
                    {
                        // Parse hire date once outside the predicate — EF Core cannot translate
                        // DateTime.ToString(format) to SQL, so compare by .Date instead.
                        DateTime? viewpointHireDate = null;
                        if (!string.IsNullOrWhiteSpace(foundEmployee.HireDate) &&
                            DateTime.TryParse(foundEmployee.HireDate, out var parsedHireDate))
                        {
                            viewpointHireDate = parsedHireDate.Date;
                        }

                        // If Viewpoint returned no HireDate, either retry (first attempt) or fail
                        // after retries are exhausted. Matches the existing retry-then-fail pattern
                        // used for "employee not found".
                        if (!viewpointHireDate.HasValue)
                        {
                            if (attemptNumber < 2)
                            {
                                var retryMessage = "Retrying new hire verification because HireDate returned by Viewpoint is null or empty. Next attempt will run in 2 hours.";
                                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
                                    "HireDate is null/empty from Viewpoint for HR request detail {HRRequestDetailId}. Scheduling retry attempt {NextAttempt} in 2 hours.",
                                    hrRequestDetailId, attemptNumber + 1);

                                await SendHireDateRetryEmailAsync(context, requestDetail, retryMessage, submitterEmail);

                                var retryJobId = BackgroundJob.Schedule(
                                    () => VerifyNewHireEmployeeInViewpointAsync(hrRequestDetailId, attemptNumber + 1, submitterEmail),
                                    TimeSpan.FromHours(2)
                                );

                                _ecmLogger.LogBackgroundJob(true, "VerifyNewHireEmployeeInViewpointAsync",
                                    $"Scheduled retry due to null HireDate for HR request detail {hrRequestDetailId}", retryJobId);
                                return;
                            }
                            else
                            {
                                var hireDateFailure = "New hire verification failed - HireDate is null or empty in Viewpoint after 2 attempts";
                                _ecmLogger.LogError(LogCategory.ViewpointIntegration,
                                    "HireDate still null/empty after retry for HR request detail {HRRequestDetailId}. Marking request as Failed.",
                                    hrRequestDetailId);

                                requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                                requestDetail.ViewpointErrorMessage = hireDateFailure;
                                requestDetail.ModifiedDate = DateTime.UtcNow;
                                await context.SaveChangesAsync();

                                await notificationService.SendHRRequestCompletionNotificationAsync(
                                    submitterEmail ?? "system",
                                    hrRequestDetailId,
                                    employeeName,
                                    false,
                                    hireDateFailure
                                );

                                await SendFailedRequestEmailAsync(context, requestDetail, hireDateFailure, submitterEmail);
                                EnqueueNotificationJob(hrRequestDetailId);
                                return;
                            }
                        }

                        var hireDateValue = viewpointHireDate.Value;
                        var newHireToUpdate = await context.NewHireRequestDetails
                            .FirstOrDefaultAsync(nh =>
                                nh.RequestDetailId == hrRequestDetailId &&
                                nh.CompanyCode == foundEmployee.HRCo &&
                                nh.PayrollDeptCode.ToString() == foundEmployee.PRDept &&
                                nh.LastName == foundEmployee.LastName &&
                                nh.FirstDayEmployment.HasValue &&
                                nh.FirstDayEmployment.Value.Date == hireDateValue);

                        if (newHireToUpdate != null)
                        {
                            newHireToUpdate.EmployeeId = hrRef.Value;

                            // Update NetworkId from Viewpoint's udNetworkUserID custom field
                            var vpNetworkId = foundEmployee.CustomFields?.NetworkUserID;
                            if (!string.IsNullOrEmpty(vpNetworkId))
                            {
                                newHireToUpdate.NetworkId = vpNetworkId;
                                _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                                    "Updated NewHireRequestDetails NetworkId to '{NetworkId}' from Viewpoint for HR request detail {HRRequestDetailId}",
                                    vpNetworkId, hrRequestDetailId);
                            }

                            _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                                "Updated NewHireRequestDetails EmployeeId to {HRRef} for HR request detail {HRRequestDetailId}",
                                hrRef.Value, hrRequestDetailId);
                        }
                        else
                        {
                            _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
                                "Could not find matching NewHireRequestDetails record to update with HRRef {HRRef} for HR request detail {HRRequestDetailId}",
                                hrRef.Value, hrRequestDetailId);
                        }
                    }

                    // Set status to Completed
                    requestDetail.RequestStatusId = (int)Enums.RequestStatus.Completed;
                    requestDetail.ModifiedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    // Send SignalR notification for status update to Completed
                    await notificationService.SendHRRequestCompletionNotificationAsync(
                        submitterEmail ?? "system",
                        hrRequestDetailId,
                        employeeName,
                        true,
                        $"New hire employee successfully verified in Viewpoint with HRRef: {hrRef}"
                    );

                    // Schedule email notification job for successful completion
                    EnqueueNotificationJob(hrRequestDetailId);
                }
                else
                {
                    // Multiple employees found - set to Failed
                    _ecmLogger.LogError(LogCategory.ViewpointIntegration,
                        "Multiple employees found in Viewpoint for HR request detail {HRRequestDetailId}. Found {EmployeeCount} matching employees - Employee is duplicate in Viewpoint HRRM",
                        hrRequestDetailId, searchResult.Count);

                    // Set status to Failed
                    var duplicateError = $"Employee is duplicate in Viewpoint HRRM. Found {searchResult.Count} matching employees.";
                    requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    requestDetail.ViewpointErrorMessage = "Employee is duplicate in Viewpoint HRRM";
                    requestDetail.ModifiedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    // Send SignalR notification for status update to Failed
                    await notificationService.SendHRRequestCompletionNotificationAsync(
                        submitterEmail ?? "system",
                        hrRequestDetailId,
                        employeeName,
                        false,
                        duplicateError
                    );

                    // Send Failed Request email notification
                    await SendFailedRequestEmailAsync(context, requestDetail, duplicateError, submitterEmail);

                    // Schedule email notification job for failure
                    var emailJobId = BackgroundJob.Enqueue(() => SendCompletionEmailNotificationAsync(hrRequestDetailId, false, duplicateError, submitterEmail));
                    _ecmLogger.LogBackgroundJob(true, "SendCompletionEmailNotificationAsync",
                        $"Scheduled for duplicate employee failure in HR request detail {hrRequestDetailId}", emailJobId);
                }
            }
            else if (searchResult != null && searchResult.Count == 0)
            {
                // Employee not found - check if we should retry
                if (attemptNumber < 2)
                {
                    _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
                        "New hire employee not found in Viewpoint for HR request detail {HRRequestDetailId}, scheduling retry attempt {NextAttempt}",
                        hrRequestDetailId, attemptNumber + 1);

                    // Schedule retry in 2 hours
                    var retryJobId = BackgroundJob.Schedule(
                        () => VerifyNewHireEmployeeInViewpointAsync(hrRequestDetailId, attemptNumber + 1, submitterEmail),
                        TimeSpan.FromHours(2)
                    );

                    _ecmLogger.LogBackgroundJob(true, "VerifyNewHireEmployeeInViewpointAsync",
                        $"Scheduled retry verification for HR request detail {hrRequestDetailId}", retryJobId);
                }
                else
                {
                    // All retries exhausted, set status to Failed
                    _ecmLogger.LogError(LogCategory.ViewpointIntegration,
                        "All retry attempts exhausted for new hire verification. HR request detail {HRRequestDetailId} marked as Failed",
                        hrRequestDetailId);

                    var notFoundError = "New hire employee verification failed - employee not found in Viewpoint after 2 attempts";
                    requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    requestDetail.ViewpointErrorMessage = notFoundError;
                    requestDetail.ModifiedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    // Send failure notification
                    await notificationService.SendHRRequestCompletionNotificationAsync(
                        submitterEmail ?? "system",
                        hrRequestDetailId,
                        $"{newHireDetail.FirstName} {newHireDetail.LastName}",
                        false,
                        notFoundError
                    );

                    // Send Failed Request email notification
                    await SendFailedRequestEmailAsync(context, requestDetail, notFoundError, submitterEmail);

                    // Schedule email notification job for failure
                    EnqueueNotificationJob(hrRequestDetailId);
                }
            }
            else
            {
                // Handle searchResult == null case (API call failed)
                _ecmLogger.LogError(LogCategory.ViewpointIntegration,
                    "Viewpoint API call failed for HR request detail {HRRequestDetailId}, attempt {AttemptNumber}",
                    hrRequestDetailId, attemptNumber);

                if (attemptNumber < 2)
                {
                    _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
                        "Viewpoint API call failed for HR request detail {HRRequestDetailId}, scheduling retry attempt {NextAttempt}",
                        hrRequestDetailId, attemptNumber + 1);

                    // Schedule retry in 2 hours
                    var retryJobId = BackgroundJob.Schedule(
                        () => VerifyNewHireEmployeeInViewpointAsync(hrRequestDetailId, attemptNumber + 1, submitterEmail),
                        TimeSpan.FromHours(2)
                    );

                    _ecmLogger.LogBackgroundJob(true, "VerifyNewHireEmployeeInViewpointAsync",
                        $"Scheduled retry verification for API failure in HR request detail {hrRequestDetailId}", retryJobId);
                }
                else
                {
                    // All retries exhausted due to API failure, set status to Failed
                    _ecmLogger.LogError(LogCategory.ViewpointIntegration,
                        "All retry attempts exhausted due to API failures. HR request detail {HRRequestDetailId} marked as Failed",
                        hrRequestDetailId);

                    var apiFailedError = "Viewpoint API call failed after 2 attempts";
                    requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    requestDetail.ViewpointErrorMessage = apiFailedError;
                    requestDetail.ModifiedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    // Send failure notification
                    await notificationService.SendHRRequestCompletionNotificationAsync(
                        submitterEmail ?? "system",
                        hrRequestDetailId,
                        $"{newHireDetail.FirstName} {newHireDetail.LastName}",
                        false,
                        $"New hire employee verification failed - {apiFailedError}"
                    );

                    // Send Failed Request email notification
                    await SendFailedRequestEmailAsync(context, requestDetail, apiFailedError, submitterEmail);

                    // Schedule email notification job for failure
                    EnqueueNotificationJob(hrRequestDetailId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying new hire employee in Viewpoint for HR request detail {HRRequestDetailId}, attempt {AttemptNumber}",
                hrRequestDetailId, attemptNumber);

            // Set status to Failed on exception
            using var errorScope = _serviceScopeFactory.CreateScope();
            var errorContext = errorScope.ServiceProvider.GetRequiredService<MathyELMContext>();
            var requestDetail = await errorContext.HRRequestDetails.FindAsync(hrRequestDetailId);
            if (requestDetail != null)
            {
                var verifyNewHireError = $"Exception during new hire verification: {ex.Message}";
                requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                requestDetail.ViewpointErrorMessage = verifyNewHireError;
                requestDetail.ModifiedDate = DateTime.UtcNow;
                await errorContext.SaveChangesAsync();

                // Send Failed Request email notification
                await SendFailedRequestEmailAsync(errorContext, requestDetail, verifyNewHireError, submitterEmail);
            }
        }
    }

    /// <summary>
    /// Sets up the recurring job for processing New Hire email notifications based on submission frequency
    /// </summary>
    public void SetupNewHireEmailNotificationsJob()
    {
        _logger.LogInformation("Setting up recurring New Hire email notification job based on submission frequency");

        // Run every day at 12:00 AM (midnight) to check for emails to send/schedule
        RecurringJob.AddOrUpdate(
            "newhire-email-notifications",
            () => ProcessNewHireEmailNotificationsAsync(),
            Cron.Daily(0));

        _logger.LogInformation("New Hire email notification job scheduled to run daily at 12:00 AM (midnight)");
    }

    /// <summary>
    /// Processes New Hire email notifications based on EmailTemplate SubmissionFreq
    /// Schedules emails for delivery based on: TriggerDate = FirstDayEmployment + SubmissionFreq
    /// </summary>
    public async Task ProcessNewHireEmailNotificationsAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IAzureServiceBusEmailService>();

        try
        {
            var today = DateTime.Today.Date;
            _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Starting email notification processing for {Date:yyyy-MM-dd}", today);

            // Get all active NEWHIRE email templates with scheduled trigger type
            var emailTemplates = await context.EmailTemplates
                .Where(t => t.IsActive && !t.IsDeleted && t.TriggerType == "Scheduled" && t.RequestType == "NEWHIRE")
                .ToListAsync();

            if (!emailTemplates.Any())
            {
                _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] No active scheduled email templates found");
                return;
            }

            _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Processing {TemplateCount} email templates", emailTemplates.Count);

            int totalScheduled = 0;
            int totalFailed = 0;

            // Process each template
            foreach (var template in emailTemplates)
            {
                try
                {
                    // Get all submitted new hire requests
                    var newHireRequests = await context.HRRequestDetails
                        .Include(rd => rd.ParentRequest)
                        .Include(rd => rd.NewHireDetails)
                        .Where(rd =>
                            rd.RequestTypeId == 5 && // New Hire
                            (rd.RequestStatusId == 1 || rd.RequestStatusId == 2) && // Submitted and Equal to Pending and Processing.
                            !rd.IsDeleted &&
                            rd.NewHireDetails != null &&
                            rd.NewHireDetails.FirstDayEmployment.HasValue)
                        .ToListAsync();

                    _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Template '{TemplateName}' (Id={TemplateId}, TriggerType={TriggerType}): Processing {RequestCount} requests",
                        template.TemplateName, template.Id, template.TriggerType, newHireRequests.Count);

                    // Process each request for this template
                    foreach (var requestDetail in newHireRequests)
                    {
                        try
                        {
                            var firstDayEmployment = requestDetail.NewHireDetails.FirstDayEmployment.Value.Date;
                            var triggerDate = firstDayEmployment.AddDays(template.SubmissionFreq ?? 0);
                            var daysUntilTrigger = (triggerDate - today).Days;

                            _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Request {RequestId}: Status={Status}, FirstDay={FirstDay:yyyy-MM-dd}, SubmissionFreq={Freq}, TriggerDate={TriggerDate:yyyy-MM-dd}, DaysUntil={Days}",
                                requestDetail.ParentRequestId, requestDetail.RequestStatusId, firstDayEmployment, template.SubmissionFreq ?? 0, triggerDate, daysUntilTrigger);

                            // Only schedule via Hangfire for all trigger dates
                            if (daysUntilTrigger >= 0)
                            {
                                var jobId = $"newhire-email-{requestDetail.ParentRequestId}-{template.Id}-{template.SubmissionFreq}";

                                _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Processing job: {JobId} for {TriggerDate:yyyy-MM-dd} at 00:00", jobId, triggerDate);

                                // Check if trigger date is in the past or now
                                // Hangfire.Schedule() doesn't execute jobs with past dates
                                // So we must use Enqueue() for past dates to ensure execution
                                string hangfireJobId;

                                if (triggerDate <= DateTime.UtcNow)
                                {
                                    // Date is in the past or now - enqueue immediately for immediate execution
                                    _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] TriggerDate {TriggerDate} is in the past (now is {CurrentTime}), enqueueing immediately for execution",
                                        triggerDate, DateTime.UtcNow);

                                    hangfireJobId = BackgroundJob.Enqueue(
                                        () => SendScheduledNewHireEmailAsync(requestDetail.ParentRequestId, template.Id));
                                }
                                else
                                {
                                    // Date is in the future - schedule normally
                                    _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] TriggerDate {TriggerDate} is in the future (now is {CurrentTime}), scheduling for later execution",
                                        triggerDate, DateTime.UtcNow);

                                    hangfireJobId = BackgroundJob.Schedule(
                                        () => SendScheduledNewHireEmailAsync(requestDetail.ParentRequestId, template.Id),
                                        triggerDate);
                                }

                                _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Job queued with jobId: {JobId}, HangfireJobId: {HangfireJobId}", jobId, hangfireJobId);

                                totalScheduled++;
                            }
                        }
                        catch (Exception ex)
                        {
                            totalFailed++;
                            _logger.LogError(ex, "[NEW HIRE EMAIL NOTIFICATIONS] Error processing request {RequestId} for template '{TemplateName}'",
                                requestDetail.ParentRequestId, template.TemplateName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[NEW HIRE EMAIL NOTIFICATIONS] Error processing template '{TemplateName}'", template.TemplateName);
                }
            }

            _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Processing completed. Scheduled: {Scheduled}, Failed: {Failed}",
                totalScheduled, totalFailed);

            _ecmLogger.LogBackgroundJob(
                success: totalFailed == 0,
                jobName: "ProcessNewHireEmailNotifications",
                operation: $"Processed new hire email notifications - Scheduled: {totalScheduled}, Failed: {totalFailed}",
                jobId: null,
                errorMessage: totalFailed > 0 ? $"{totalFailed} email(s) failed to schedule" : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NEW HIRE EMAIL NOTIFICATIONS] CRITICAL ERROR: Failed to process email notifications");

            _ecmLogger.LogBackgroundJob(
                success: false,
                jobName: "ProcessNewHireEmailNotifications",
                operation: "Process new hire email notifications",
                jobId: null,
                errorMessage: ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Synchronous wrapper method for Hangfire job execution
    /// Hangfire requires a synchronous method signature - cannot use async Task directly from lambda
    /// </summary>
    public void ExecuteScheduledNewHireEmailSync(int parentRequestId, int templateId)
    {
        _logger.LogInformation("[HANGFIRE JOB] >>> SYNC WRAPPER STARTED - ParentRequestId={RequestId}, TemplateId={TemplateId}",
            parentRequestId, templateId);

        try
        {
            _logger.LogInformation("[HANGFIRE JOB] >>> About to call async method...");

            // Synchronous wrapper - blocks until async method completes
            var task = ExecuteScheduledNewHireEmailAsync(parentRequestId, templateId);
            task.GetAwaiter().GetResult();

            _logger.LogInformation("[HANGFIRE JOB] >>> SYNC WRAPPER COMPLETED successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HANGFIRE JOB] >>> SYNC WRAPPER FAILED - Exception: {ExceptionType}: {ExceptionMessage}",
                ex.GetType().Name, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Public wrapper method for Hangfire job execution (Async version)
    /// </summary>
    public async Task ExecuteScheduledNewHireEmailAsync(int parentRequestId, int templateId)
    {
        _logger.LogInformation("[HANGFIRE] ExecuteScheduledNewHireEmailAsync called with ParentRequestId={RequestId}, TemplateId={TemplateId}",
            parentRequestId, templateId);

        await SendScheduledNewHireEmailAsync(parentRequestId, templateId);
    }

    /// <summary>
    /// Sends a scheduled New Hire email notification
    /// </summary>
    public async Task SendScheduledNewHireEmailAsync(int parentRequestId, int templateId)
    {
        _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] >>> STARTING SendScheduledNewHireEmailAsync: ParentRequestId={RequestId}, TemplateId={TemplateId}",
            parentRequestId, templateId);

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IAzureServiceBusEmailService>();
        var emailRecipientsService = scope.ServiceProvider.GetRequiredService<IEmailRecipientsService>();

        try
        {
            _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] >>> Scope created, fetching template and request data...");
            _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Executing scheduled email: ParentRequestId={RequestId}, TemplateId={TemplateId}",
                parentRequestId, templateId);

            // Get the template
            var template = await context.EmailTemplates
                .Where(t => t.Id == templateId && t.IsActive && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (template == null)
            {
                var errorMsg = $"Template not found: {templateId}";
                _logger.LogError("[NEW HIRE EMAIL NOTIFICATIONS] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Get the request
            var requestDetail = await context.HRRequestDetails
                .Include(rd => rd.ParentRequest)
                .Include(rd => rd.NewHireDetails)
                    .ThenInclude(nh => nh!.CreditCardDetail)
                .FirstOrDefaultAsync(rd => rd.ParentRequestId == parentRequestId && rd.RequestTypeId == 5);

            if (requestDetail == null || requestDetail.NewHireDetails == null)
            {
                var errorMsg = $"Request or NewHireDetails not found for ParentRequestId: {parentRequestId}";
                _logger.LogError("[NEW HIRE EMAIL NOTIFICATIONS] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Check if this is a "Past Start Date" template - validate request status
            if (template.TemplateName == "Past Start Date")
            {
                // For Past Start Date emails, only send if request is NOT Completed (RequestStatusId != 3)
                if (requestDetail.RequestStatusId == 3 || requestDetail.RequestStatusId == 5 || requestDetail.RequestStatusId == 6)
                {
                    _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Skipping 'Past Start Date' email - request already completed: {RequestId}",
                        parentRequestId);
                    // Request is completed, job can complete successfully (no email needed)
                    return;
                }
            }

            // Check if notification already exists in NotificationQueue to prevent duplicate sends
            var existingNotification = await context.NotificationQueue
                .FirstOrDefaultAsync(nq => nq.RequestId == parentRequestId && nq.TemplateId == templateId);

            if (existingNotification != null)
            {
                _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Notification already queued for RequestId={RequestId}, TemplateId={TemplateId}. Status={Status}. Skipping duplicate send.",
                    parentRequestId, templateId, existingNotification.Status);
                // Notification already queued, job can complete successfully without sending again
                return;
            }

            // Create DTO and send
            var requestDto = MapNewHireDetailsToDto(requestDetail.NewHireDetails, requestDetail);
            _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Sending email with DTO: FirstName={FirstName}, LastName={LastName}, EmployeeId={EmployeeId}",
                requestDto?.PersonalInfo?.FirstName, requestDto?.PersonalInfo?.LastName, requestDto?.PersonalInfo?.EmployeeId);

            // Resolve recipients from EmailTemplate.Recipients field using EmailRecipientsService
            // Supports both CompanyDL fields and special recipient keys like 'Manager', 'Submitter', 'HRDL'
            var submitterEmail = requestDetail.ParentRequest?.SubmitterEmail ?? "noreply@example.com";
            var managerEmail = "noreply@example.com"; // Will be resolved via reflection if Manager key exists

            // Try to get manager email from supervisor if available
            if (requestDetail.NewHireDetails?.SupervisorId.HasValue == true)
            {
                var supervisor = await context.Employees
                    .Where(e => e.EmployeeNumber == requestDetail.NewHireDetails.SupervisorId.Value && !e.IsDeleted)
                    .FirstOrDefaultAsync();
                managerEmail = !string.IsNullOrEmpty(supervisor?.WorkEmail) ? supervisor.WorkEmail : "noreply@example.com";
            }

            var recipients = await emailRecipientsService.GetRecipientsFromTemplateAsync(
                template.TemplateName,
                requestDetail.NewHireDetails?.CompanyCode,
                requestDetail.NewHireDetails?.PayrollDeptCode ?? 0,  // Department code for filtering DL
                managerEmail: managerEmail,
                submitterEmail: submitterEmail
            );

            // If no recipients resolved from template, log warning but don't fail
            if (!recipients.Any())
            {
                _ecmLogger.LogWarning(LogCategory.EmailNotification, "[NEW HIRE EMAIL NOTIFICATIONS] No recipients resolved for template '{TemplateName}'. Check EmailTemplate.Recipients configuration.",
                    template.TemplateName);
                recipients = new List<string> { submitterEmail }; // Fallback to submitter
            }

            var toEmails = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));
            _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Resolved {RecipientCount} recipient(s) for template '{TemplateName}': {Recipients}",
                recipients.Count, template.TemplateName, toEmails);

            var emailResult = await emailService.SendEmailFromTemplateNameAsync(template.TemplateName, requestDto, toEmails, null, parentRequestId);

            if (emailResult.Success)
            {
                _logger.LogInformation("[NEW HIRE EMAIL NOTIFICATIONS] Scheduled email sent successfully: {RequestId}, Template: '{TemplateName}'",
                    parentRequestId, template.TemplateName);

                _ecmLogger.LogEmailNotification(
                    success: true,
                    operation: "SendScheduledNewHireEmail",
                    recipient: toEmails,
                    subject: template.TemplateName,
                    errorMessage: null);
            }
            else
            {
                _logger.LogError("[NEW HIRE EMAIL NOTIFICATIONS] Failed to send scheduled email: {Error}. RequestId={RequestId}, Template={TemplateName}",
                    emailResult.Message, parentRequestId, template.TemplateName);

                _ecmLogger.LogEmailNotification(
                    success: false,
                    operation: "SendScheduledNewHireEmail",
                    recipient: toEmails,
                    subject: template.TemplateName,
                    errorMessage: emailResult.Message);

                // CRITICAL: Throw exception so Hangfire retries the job
                throw new InvalidOperationException($"Email send failed for request {parentRequestId}: {emailResult.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NEW HIRE EMAIL NOTIFICATIONS] Error executing scheduled email: {RequestId}", parentRequestId);

            _ecmLogger.LogEmailNotification(
                success: false,
                operation: "SendScheduledNewHireEmail",
                recipient: null,
                subject: null,
                errorMessage: ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Immediately triggers scheduled emails for a new hire request if their trigger dates have already passed.
    /// This method is called when a new hire request is created/updated to check if any emails should be sent immediately
    /// instead of waiting for the daily ProcessNewHireEmailNotificationsAsync job to run.
    ///
    /// Works with ALL trigger types (positive and negative SubmissionFreq):
    /// - "Past Start Date" (SubmissionFreq = 7, 14, etc.) - sends 7+ days after start
    /// - "Pre-Start Date" (SubmissionFreq = -3, -7, etc.) - sends 3-7 days before start
    /// - Any other template type where triggerDate has already passed
    /// </summary>
    /// <param name="parentRequestId">The parent HR request ID</param>
    public async Task TriggerOverdueScheduledEmailsAsync(int parentRequestId)
    {
        _logger.LogInformation("[IMMEDIATE TRIGGER] Starting check for overdue scheduled emails: ParentRequestId={RequestId}", parentRequestId);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();

            // Get the new hire request and details
            var requestDetail = await context.HRRequestDetails
                .Include(rd => rd.NewHireDetails)
                .FirstOrDefaultAsync(rd => rd.ParentRequestId == parentRequestId && rd.RequestTypeId == 5);

            if (requestDetail?.NewHireDetails?.FirstDayEmployment == null)
            {
                _logger.LogInformation("[IMMEDIATE TRIGGER] New hire request or FirstDayEmployment not found for ParentRequestId={RequestId}", parentRequestId);
                return;
            }

            var today = DateTime.UtcNow.Date;
            var firstDayEmployment = requestDetail.NewHireDetails.FirstDayEmployment.Value.Date;

            _logger.LogInformation("[IMMEDIATE TRIGGER] Processing new hire request: ParentRequestId={RequestId}, FirstDayEmployment={FirstDay}, Today={Today}",
                parentRequestId, firstDayEmployment, today);

            // Get ALL email templates for New Hire (all trigger types: Past Start Date, Pre-Start Date, etc.)
            // Only process Scheduled templates that are overdue.
            // Immediate templates (Confirmation, Task Emails) are already sent by the controller's notification step.
            // Event-driven templates (Change Date, Failed Request) are triggered by their specific events.
            var templates = await context.EmailTemplates
                .Where(t => t.RequestType == "NEWHIRE" && t.TriggerType == "Scheduled" && t.IsActive && !t.IsDeleted)
                .ToListAsync();

            if (!templates.Any())
            {
                _logger.LogInformation("[IMMEDIATE TRIGGER] No active New Hire email templates found");
                return;
            }

            foreach (var template in templates)
            {
                try
                {
                    // Calculate trigger date (works with positive and negative SubmissionFreq)
                    var triggerDate = firstDayEmployment.AddDays(template.SubmissionFreq ?? 0);

                    _logger.LogInformation("[IMMEDIATE TRIGGER] Checking template '{TemplateName}' (Id={TemplateId}): TriggerDate={TriggerDate}, Today={Today}, SubmissionFreq={Freq}",
                        template.TemplateName, template.Id, triggerDate, today, template.SubmissionFreq ?? 0);

                    // If trigger date is today or in the past, immediately enqueue
                    if (triggerDate <= today)
                    {
                        // Check for duplicate in NotificationQueue first to prevent duplicate sends
                        var existing = await context.NotificationQueue
                            .FirstOrDefaultAsync(nq => nq.RequestId == parentRequestId && nq.TemplateId == template.Id);

                        if (existing != null)
                        {
                            _logger.LogInformation("[IMMEDIATE TRIGGER] Notification already exists for RequestId={RequestId}, TemplateId={TemplateId}, Status={Status}. Skipping duplicate.",
                                parentRequestId, template.Id, existing.Status);
                            continue;
                        }

                        // Enqueue immediately
                        var jobId = BackgroundJob.Enqueue(
                            () => SendScheduledNewHireEmailAsync(parentRequestId, template.Id));

                        _logger.LogInformation("[IMMEDIATE TRIGGER] ✅ ENQUEUED IMMEDIATELY: ParentRequestId={RequestId}, Template='{TemplateName}' (Id={TemplateId}), TriggerDate={TriggerDate} (SubmissionFreq={Freq}), JobId={JobId}",
                            parentRequestId, template.TemplateName, template.Id, triggerDate, template.SubmissionFreq ?? 0, jobId);
                    }
                    else
                    {
                        var daysUntil = (triggerDate - today).Days;
                        _logger.LogInformation("[IMMEDIATE TRIGGER] Template '{TemplateName}' not yet due: TriggerDate={TriggerDate} ({DaysUntil} days from now)",
                            template.TemplateName, triggerDate, daysUntil);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[IMMEDIATE TRIGGER] Error processing template '{TemplateName}' (Id={TemplateId}) for ParentRequestId={RequestId}",
                        template.TemplateName, template.Id, parentRequestId);
                    // Continue processing other templates even if one fails
                }
            }

            _logger.LogInformation("[IMMEDIATE TRIGGER] Completed checking overdue scheduled emails for ParentRequestId={RequestId}", parentRequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IMMEDIATE TRIGGER] Error in TriggerOverdueScheduledEmailsAsync for ParentRequestId={RequestId}", parentRequestId);
            // Don't throw - this is a best-effort operation and shouldn't block the request save
        }
    }

    /// <summary>
    /// Sets up a recurring daily job to send draft reminder emails to submitters
    /// </summary>
    public void SetupDraftReminderEmailJob()
    {
        _logger.LogInformation("Setting up recurring draft reminder email job");

        // Run every day at 12:00 AM (Midnight)
        RecurringJob.AddOrUpdate(
            "process-draft-reminder-emails",
            () => ProcessDraftReminderEmailsAsync(),
            Cron.Daily(0));

        _logger.LogInformation("Draft reminder email job registered successfully - will run daily at 12:00 AM (Midnight)");
    }

    /// <summary>
    /// Processes and sends draft reminder emails for all new hire requests with Draft status
    /// This is a recurring job that runs daily and finds all draft requests, then sends reminder emails to submitters
    /// </summary>
    public async Task ProcessDraftReminderEmailsAsync()
    {
        try
        {
            _logger.LogInformation("[DRAFT REMINDER] Starting draft reminder email processing");

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IAzureServiceBusEmailService>();

                // Query all new hire requests with Draft status (RequestStatusId = 6)
                var draftRequests = await dbContext.HRRequestDetails
                    .Where(d => d.RequestStatusId == (int)Enums.RequestStatus.Draft
                             && !d.IsDeleted
                             && d.NewHireDetails != null)  // Only New Hire requests
                    .Include(d => d.ParentRequest)
                    .Include(d => d.NewHireDetails)
                    .ToListAsync();

                _logger.LogInformation("[DRAFT REMINDER] Found {Count} draft new hire requests", draftRequests.Count);

                if (draftRequests.Count == 0)
                {
                    _logger.LogInformation("[DRAFT REMINDER] No draft requests found - nothing to process");
                    return;
                }

                // Get the Draft Reminder email template
                var draftReminderTemplate = await dbContext.EmailTemplates
                    .Where(t => t.TemplateName == "Draft Reminder" && t.IsActive && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (draftReminderTemplate == null)
                {
                    _ecmLogger.LogWarning(LogCategory.EmailNotification, "[DRAFT REMINDER] 'Draft Reminder' email template not found in database. Please create the template first.");
                    return;
                }

                _logger.LogInformation("[DRAFT REMINDER] Found 'Draft Reminder' template (Id={TemplateId})", draftReminderTemplate.Id);

                int emailsSent = 0;
                int emailsSkipped = 0;

                // Process each draft request
                foreach (var requestDetail in draftRequests)
                {
                    try
                    {
                        var parentRequest = requestDetail.ParentRequest;
                        if (parentRequest == null || string.IsNullOrEmpty(parentRequest.SubmitterEmail))
                        {
                            _ecmLogger.LogWarning(LogCategory.EmailNotification, "[DRAFT REMINDER] Draft request {RequestDetailId} (ParentRequestId={ParentId}) has no submitter email - skipping",
                                requestDetail.Id, requestDetail.ParentRequestId);
                            emailsSkipped++;
                            continue;
                        }

                        // Check if email already sent today to prevent duplicates
                        var alreadySent = await dbContext.NotificationQueue
                            .Where(nq => nq.RequestId == requestDetail.ParentRequestId
                                      && nq.TemplateId == draftReminderTemplate.Id
                                      && nq.CreatedDate.Date == DateTime.UtcNow.Date
                                      && !nq.IsDeleted)
                            .AnyAsync();

                        if (alreadySent)
                        {
                            _logger.LogInformation("[DRAFT REMINDER] Draft reminder already sent today for RequestDetailId={RequestDetailId} (ParentRequestId={ParentId})",
                                requestDetail.Id, requestDetail.ParentRequestId);
                            emailsSkipped++;
                            continue;
                        }

                        // Send draft reminder email to submitter
                        _logger.LogInformation("[DRAFT REMINDER] Sending draft reminder to {SubmitterEmail} for draft request {RequestDetailId}",
                            parentRequest.SubmitterEmail, requestDetail.Id);

                        // Build a basic CreateNewHireRequestDto for template rendering
                        // Note: We're sending minimal data since this is just a reminder
                        var dummyRequest = new CreateNewHireRequestDto
                        {
                            PersonalInfo = new NewHirePersonalInfoDto
                            {
                                FirstName = requestDetail.NewHireDetails?.FirstName ?? "New Hire",
                                LastName = requestDetail.NewHireDetails?.LastName,
                                FirstDayEmployment = requestDetail.NewHireDetails?.FirstDayEmployment
                            }
                        };

                        var result = await emailService.SendEmailFromTemplateNameAsync(
                            "Draft Reminder",
                            dummyRequest,
                            parentRequest.SubmitterEmail,  // Send to submitter
                            null,
                            requestDetail.ParentRequestId);

                        if (result.Success)
                        {
                            emailsSent++;
                            _logger.LogInformation("[DRAFT REMINDER] ✅ Draft reminder sent successfully to {SubmitterEmail} for RequestDetailId={RequestDetailId}",
                                parentRequest.SubmitterEmail, requestDetail.Id);
                        }
                        else
                        {
                            emailsSkipped++;
                            _ecmLogger.LogWarning(LogCategory.EmailNotification, "[DRAFT REMINDER] Failed to send draft reminder to {SubmitterEmail}: {ErrorMessage}",
                                parentRequest.SubmitterEmail, result.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        emailsSkipped++;
                        _logger.LogError(ex, "[DRAFT REMINDER] Error sending draft reminder for RequestDetailId={RequestDetailId}",
                            requestDetail.Id);
                        // Continue processing other draft requests even if one fails
                    }
                }

                _logger.LogInformation("[DRAFT REMINDER] Processing complete - Sent={EmailsSent}, Skipped={EmailsSkipped}",
                    emailsSent, emailsSkipped);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DRAFT REMINDER] Error in ProcessDraftReminderEmailsAsync");
            throw;  // Allow Hangfire to handle retry logic
        }
    }

    /// <summary>
    /// Sends an immediate draft reminder email to the submitter when they save a request as Draft
    /// </summary>
    public async Task SendImmediateDraftReminderAsync(int parentRequestId)
    {
        try
        {
            _logger.LogInformation("[DRAFT REMINDER - IMMEDIATE] Starting immediate draft reminder email processing for ParentRequestId={RequestId}", parentRequestId);

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IAzureServiceBusEmailService>();

                // Get the parent HRRequest to access submitter email
                var parentRequest = await dbContext.HRRequests
                    .Where(hr => hr.Id == parentRequestId && !hr.IsDeleted)
                    .FirstOrDefaultAsync();

                if (parentRequest == null || string.IsNullOrEmpty(parentRequest.SubmitterEmail))
                {
                    _ecmLogger.LogWarning(LogCategory.EmailNotification, "[DRAFT REMINDER - IMMEDIATE] Parent request {RequestId} not found or has no submitter email - skipping",
                        parentRequestId);
                    return;
                }

                _logger.LogInformation("[DRAFT REMINDER - IMMEDIATE] Found parent request with submitter email: {SubmitterEmail}",
                    parentRequest.SubmitterEmail);

                // Get the Draft Reminder email template
                var draftReminderTemplate = await dbContext.EmailTemplates
                    .Where(t => t.TemplateName == "Draft Reminder" && t.IsActive && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (draftReminderTemplate == null)
                {
                    _ecmLogger.LogWarning(LogCategory.EmailNotification, "[DRAFT REMINDER - IMMEDIATE] 'Draft Reminder' email template not found in database");
                    return;
                }

                _logger.LogInformation("[DRAFT REMINDER - IMMEDIATE] Found 'Draft Reminder' template (Id={TemplateId})",
                    draftReminderTemplate.Id);

                // Check if email already sent today to prevent duplicates
                var alreadySent = await dbContext.NotificationQueue
                    .Where(nq => nq.RequestId == parentRequestId
                              && nq.TemplateId == draftReminderTemplate.Id
                              && nq.CreatedDate.Date == DateTime.UtcNow.Date
                              && !nq.IsDeleted)
                    .AnyAsync();

                if (alreadySent)
                {
                    _logger.LogInformation("[DRAFT REMINDER - IMMEDIATE] Draft reminder already sent today for ParentRequestId={RequestId} - skipping",
                        parentRequestId);
                    return;
                }

                // Get the HRRequestDetail to access new hire details
                var requestDetail = await dbContext.HRRequestDetails
                    .Where(d => d.ParentRequestId == parentRequestId
                             && !d.IsDeleted
                             && d.NewHireDetails != null)
                    .Include(d => d.NewHireDetails)
                    .FirstOrDefaultAsync();

                if (requestDetail?.NewHireDetails == null)
                {
                    _ecmLogger.LogWarning(LogCategory.EmailNotification, "[DRAFT REMINDER - IMMEDIATE] New hire request details not found for ParentRequestId={RequestId}",
                        parentRequestId);
                    return;
                }

                // Build a basic CreateNewHireRequestDto for template rendering
                var dummyRequest = new CreateNewHireRequestDto
                {
                    PersonalInfo = new NewHirePersonalInfoDto
                    {
                        FirstName = requestDetail.NewHireDetails.FirstName ?? "New Hire",
                        LastName = requestDetail.NewHireDetails.LastName,
                        FirstDayEmployment = requestDetail.NewHireDetails.FirstDayEmployment
                    }
                };

                // Send the draft reminder email
                _logger.LogInformation("[DRAFT REMINDER - IMMEDIATE] Sending draft reminder to {SubmitterEmail} for ParentRequestId={RequestId}",
                    parentRequest.SubmitterEmail, parentRequestId);

                var result = await emailService.SendEmailFromTemplateNameAsync(
                    "Draft Reminder",
                    dummyRequest,
                    parentRequest.SubmitterEmail,  // Send to submitter
                    null,
                    parentRequestId);

                if (result.Success)
                {
                    _logger.LogInformation("[DRAFT REMINDER - IMMEDIATE] ✅ Draft reminder sent successfully to {SubmitterEmail} for ParentRequestId={RequestId}",
                        parentRequest.SubmitterEmail, parentRequestId);

                    _ecmLogger.LogEmailNotification(
                        success: true,
                        operation: "SendImmediateDraftReminder",
                        recipient: parentRequest.SubmitterEmail,
                        subject: "Draft Reminder",
                        errorMessage: null);
                }
                else
                {
                    _ecmLogger.LogWarning(LogCategory.EmailNotification, "[DRAFT REMINDER - IMMEDIATE] Failed to send draft reminder to {SubmitterEmail}: {ErrorMessage}",
                        parentRequest.SubmitterEmail, result.Message);

                    _ecmLogger.LogEmailNotification(
                        success: false,
                        operation: "SendImmediateDraftReminder",
                        recipient: parentRequest.SubmitterEmail,
                        subject: "Draft Reminder",
                        errorMessage: result.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DRAFT REMINDER - IMMEDIATE] Error in SendImmediateDraftReminderAsync for ParentRequestId={RequestId}",
                parentRequestId);

            _ecmLogger.LogEmailNotification(
                success: false,
                operation: "SendImmediateDraftReminder",
                recipient: null,
                subject: "Draft Reminder",
                errorMessage: ex.Message);

            // Don't throw - allow the draft save/update to continue even if email fails
        }
    }

    /// <summary>
    /// Sets up the Welcome Email scheduled notification job
    /// Runs daily at midnight to process Welcome Email notifications for new hires
    /// </summary>
    public void SetupWelcomeEmailScheduledJob()
    {
        _logger.LogInformation("Setting up recurring Welcome Email notification job");

        // Run every day at 12:00 AM (midnight) to check for Welcome Emails to send
        RecurringJob.AddOrUpdate(
            "welcome-email-notifications",
            () => ProcessWelcomeEmailNotificationsAsync(),
            Cron.Daily(0));

        _logger.LogInformation("Welcome Email notification job scheduled to run daily at 12:00 AM (midnight)");
    }

    /// <summary>
    /// Processes Welcome Email notifications for new hires with First Day of Employment = today
    /// Searches for employee in Viewpoint API and sends Welcome Email if found
    /// Notifies HRDL & submitter if employee not found or API error occurs
    /// </summary>
    public async Task ProcessWelcomeEmailNotificationsAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();

        try
        {
            var today = DateTime.Today.Date;
            _logger.LogInformation("[WELCOME EMAIL NOTIFICATIONS] Starting Welcome Email processing for {Date:yyyy-MM-dd}", today);

            // Get the Welcome Email template
            var welcomeEmailTemplate = await context.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateName == "Welcome Email" && t.IsActive && !t.IsDeleted);

            if (welcomeEmailTemplate == null)
            {
                _ecmLogger.LogWarning(LogCategory.EmailNotification, "[WELCOME EMAIL NOTIFICATIONS] Welcome Email template not found or is not active");
                return;
            }

            // Get all New Hire requests with First Day of Employment = today
            var newHireRequests = await context.HRRequestDetails
                .Include(rd => rd.ParentRequest)
                .Include(rd => rd.NewHireDetails)
                .Where(rd =>
                    rd.RequestTypeId == 5 && // New Hire
                    rd.RequestStatusId == 3 && // Completed (verified successfully)
                    !rd.IsDeleted &&
                    rd.NewHireDetails != null &&
                    rd.NewHireDetails.FirstDayEmployment.HasValue &&
                    rd.NewHireDetails.FirstDayEmployment.Value.Date == today)
                .ToListAsync();

            _logger.LogInformation("[WELCOME EMAIL NOTIFICATIONS] Found {RequestCount} new hire requests with First Day of Employment = today",
                newHireRequests.Count);

            foreach (var requestDetail in newHireRequests)
            {
                try
                {
                    // Check for existing notification to prevent duplicates
                    var existingNotification = await context.NotificationQueue
                        .FirstOrDefaultAsync(nq =>
                            nq.RequestId == requestDetail.ParentRequestId &&
                            nq.TemplateId == welcomeEmailTemplate.Id);

                    if (existingNotification != null)
                    {
                        _logger.LogInformation("[WELCOME EMAIL NOTIFICATIONS] Skipping duplicate - Welcome Email already processed for RequestId={RequestId}",
                            requestDetail.ParentRequestId);
                        continue;
                    }

                    // Process the welcome email
                    await VerifyAndSendWelcomeEmailAsync(requestDetail.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[WELCOME EMAIL NOTIFICATIONS] Error processing Welcome Email for HRRequestDetailId={DetailId}",
                        requestDetail.Id);
                }
            }

            _logger.LogInformation("[WELCOME EMAIL NOTIFICATIONS] Welcome Email processing completed for {Date:yyyy-MM-dd}", today);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WELCOME EMAIL NOTIFICATIONS] Fatal error in ProcessWelcomeEmailNotificationsAsync");
            throw;
        }
    }

    /// <summary>
    /// Verifies new hire employee exists in Viewpoint API and sends Welcome Email if found
    /// Handles three scenarios: Employee found, Employee not found, API error
    /// </summary>
    private async Task VerifyAndSendWelcomeEmailAsync(int newHireDetailId, int attemptNumber = 1)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        var viewpointService = scope.ServiceProvider.GetRequiredService<IViewpointService>();

        try
        {
            // Load new hire request details
            var newHireDetail = await context.NewHireRequestDetails
                .Include(nd => nd.HRRequestDetail)
                .ThenInclude(rd => rd.ParentRequest)
                .FirstOrDefaultAsync(nd => nd.RequestDetailId == newHireDetailId);

            if (newHireDetail == null)
            {
                _logger.LogError("[WELCOME EMAIL VERIFICATION] NewHireRequestDetail not found with Id={DetailId}", newHireDetailId);
                return;
            }

            var parentRequest = newHireDetail.HRRequestDetail.ParentRequest;

            _logger.LogInformation("[WELCOME EMAIL VERIFICATION] Starting verification for {LastName}, {FirstName} (DetailId={DetailId}, RequestId={RequestId}, Attempt={AttemptNumber}/2)",
                newHireDetail.LastName, newHireDetail.FirstName, newHireDetailId, parentRequest.Id, attemptNumber);

            // Verify First Day of Employment is today
            if (newHireDetail.FirstDayEmployment?.Date != DateTime.Today)
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "[WELCOME EMAIL VERIFICATION] First Day of Employment mismatch. Expected today ({Today:yyyy-MM-dd}), got {FirstDay:yyyy-MM-dd}",
                    DateTime.Today, newHireDetail.FirstDayEmployment?.Date);
                return;
            }

            // Search for employee in Viewpoint API
            var searchResult = await viewpointService.SearchEmployeeInNewHireWithAPIAsync(
                newHireDetail.CompanyCode ?? 0,
                (newHireDetail.PayrollDeptCode ?? 0).ToString(),
                newHireDetail.LastName ?? string.Empty,
                (newHireDetail.FirstDayEmployment ?? DateTime.Now).ToString("yyyy-MM-ddTHH:mm:ss")
            );

            _logger.LogInformation("[WELCOME EMAIL VERIFICATION] Viewpoint search result: {ResultCount} employees found",
                searchResult?.Count ?? 0);

            // Handle different search results
            if (searchResult != null && searchResult.Count == 1)
            {
                // Employee found - extract WorkEmail and send Welcome Email
                var foundEmployee = searchResult.First();
                var workEmail = foundEmployee.CustomFields?.WorkEmail;

                if (string.IsNullOrWhiteSpace(workEmail))
                {
                    _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "[WELCOME EMAIL VERIFICATION] Employee found but WorkEmail is empty for {LastName}, {FirstName}",
                        newHireDetail.LastName, newHireDetail.FirstName);

                    // DO NOT UPDATE STATUS - Just send notification to HR
                    _logger.LogInformation("[WELCOME EMAIL] Employee found but has no WorkEmail - Notifying HR (No Status Update)");

                    await SendWelcomeEmailNotFoundNotificationAsync(newHireDetailId);
                }
                else
                {
                    _logger.LogInformation("[WELCOME EMAIL VERIFICATION] Employee found with WorkEmail={WorkEmail}", workEmail);

                    // [NEW] Update employee in Viewpoint before sending Welcome Email
                    await UpdateEmployeeInViewpointAndSendWelcomeEmailAsync(
                        newHireDetailId,
                        workEmail,
                        newHireDetail,
                        parentRequest,
                        attemptNumber);
                }
            }
            else if (searchResult != null && searchResult.Count == 0)
            {
                // Employee not found in Viewpoint
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "[WELCOME EMAIL VERIFICATION] Employee not found in Viewpoint for {LastName}, {FirstName} (Attempt {AttemptNumber}/2)",
                    newHireDetail.LastName, newHireDetail.FirstName, attemptNumber);

                // Check if we should retry
                if (attemptNumber < 2)
                {
                    var nextAttempt = attemptNumber + 1;
                    _logger.LogInformation("[WELCOME EMAIL] Scheduling retry verification attempt {NextAttempt}/2 for DetailId={DetailId} in 2 minutes",
                        nextAttempt, newHireDetailId);

                    // SCHEDULE RETRY: No status update here, just schedule
                    var retryJobId = Hangfire.BackgroundJob.Schedule(
                        () => VerifyAndSendWelcomeEmailAsync(newHireDetailId, nextAttempt),
                        TimeSpan.FromMinutes(2));

                    _logger.LogInformation("[WELCOME EMAIL] Scheduled retry job {JobId}", retryJobId);
                }
                else
                {
                    // ALL RETRIES EXHAUSTED: Update status to Failed
                    _logger.LogError("[WELCOME EMAIL] All verification attempts exhausted (2/2) - Employee not found");

                    var requestDetail = newHireDetail.HRRequestDetail;
                    var welcomeNotFoundError = "Employee not found in Viewpoint after 2 verification attempts";
                    requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed; // 4
                    requestDetail.ViewpointErrorMessage = welcomeNotFoundError;
                    await context.SaveChangesAsync();

                    _logger.LogInformation("[WELCOME EMAIL] ❌ Set status to Failed after {AttemptNumber} verification attempts", attemptNumber);

                    await SendFailedRequestEmailAsync(context, requestDetail, welcomeNotFoundError);
                    await SendWelcomeEmailNotFoundNotificationAsync(newHireDetailId);
                }
            }
            else
            {
                // API error or unexpected result
                _logger.LogError("[WELCOME EMAIL VERIFICATION] Viewpoint API returned unexpected result: {ResultCount} employees (Attempt {AttemptNumber}/2)",
                    searchResult?.Count ?? -1, attemptNumber);

                // Check if we should retry
                if (attemptNumber < 2)
                {
                    var nextAttempt = attemptNumber + 1;
                    _logger.LogInformation("[WELCOME EMAIL] Scheduling retry verification attempt {NextAttempt}/2 for DetailId={DetailId} in 2 minutes due to API error",
                        nextAttempt, newHireDetailId);

                    // SCHEDULE RETRY: No status update here, just schedule
                    var retryJobId = Hangfire.BackgroundJob.Schedule(
                        () => VerifyAndSendWelcomeEmailAsync(newHireDetailId, nextAttempt),
                        TimeSpan.FromMinutes(2));

                    _logger.LogInformation("[WELCOME EMAIL] Scheduled retry job {JobId}", retryJobId);
                }
                else
                {
                    // ALL RETRIES EXHAUSTED: Update status to Failed
                    _logger.LogError("[WELCOME EMAIL] All verification attempts exhausted (2/2) - Viewpoint API error");

                    var requestDetail = newHireDetail.HRRequestDetail;
                    var welcomeApiError = $"Viewpoint API verification failed after 2 attempts: {searchResult?.Count ?? -1} employees returned";
                    requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed; // 4
                    requestDetail.ViewpointErrorMessage = welcomeApiError;
                    await context.SaveChangesAsync();

                    _logger.LogInformation("[WELCOME EMAIL] ❌ Set status to Failed after {AttemptNumber} verification attempts", attemptNumber);

                    await SendFailedRequestEmailAsync(context, requestDetail, welcomeApiError);
                    await SendWelcomeEmailVerificationFailedNotificationAsync(newHireDetailId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WELCOME EMAIL VERIFICATION] Error verifying employee in Viewpoint for DetailId={DetailId} (Attempt {AttemptNumber}/2)", newHireDetailId, attemptNumber);

            // Check if we should retry
            if (attemptNumber < 2)
            {
                var nextAttempt = attemptNumber + 1;
                _logger.LogInformation("[WELCOME EMAIL] Scheduling retry verification attempt {NextAttempt}/2 for DetailId={DetailId} in 2 minutes due to exception",
                    nextAttempt, newHireDetailId);

                // SCHEDULE RETRY: No status update here, just schedule
                var retryJobId = Hangfire.BackgroundJob.Schedule(
                    () => VerifyAndSendWelcomeEmailAsync(newHireDetailId, nextAttempt),
                    TimeSpan.FromMinutes(2));

                _logger.LogInformation("[WELCOME EMAIL] Scheduled retry job {JobId}", retryJobId);
            }
            else
            {
                // ALL RETRIES EXHAUSTED: Update status to Failed
                _logger.LogError("[WELCOME EMAIL] All verification attempts exhausted (2/2) - Exception occurred");

                try
                {
                    var requestDetail = await context.HRRequestDetails.FindAsync(newHireDetailId);
                    if (requestDetail != null)
                    {
                        var welcomeExError = $"Viewpoint verification exception after 2 attempts: {ex.Message}";
                        requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed; // 4
                        requestDetail.ViewpointErrorMessage = welcomeExError;
                        await context.SaveChangesAsync();

                        _logger.LogInformation("[WELCOME EMAIL] ❌ Set status to Failed after {AttemptNumber} verification attempts", attemptNumber);

                        await SendFailedRequestEmailAsync(context, requestDetail, welcomeExError);
                    }
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "[WELCOME EMAIL] Error updating status to Failed");
                }

                await SendWelcomeEmailVerificationFailedNotificationAsync(newHireDetailId);
            }
        }
    }

    /// <summary>
    /// Sends Welcome Email to new hire employee via Azure Service Bus
    /// </summary>
    private async Task SendWelcomeEmailAsync(int newHireDetailId, string employeeEmail)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IAzureServiceBusEmailService>();
        var emailRecipientsService = scope.ServiceProvider.GetRequiredService<IEmailRecipientsService>();

        try
        {
            // Load new hire request details
            var newHireDetail = await context.NewHireRequestDetails
                .Include(nd => nd.HRRequestDetail)
                .ThenInclude(rd => rd.ParentRequest)
                .FirstOrDefaultAsync(nd => nd.RequestDetailId == newHireDetailId);

            if (newHireDetail == null)
            {
                _logger.LogError("[WELCOME EMAIL SEND] NewHireRequestDetail not found with Id={DetailId}", newHireDetailId);
                _ecmLogger.LogEmailNotification(false, "WelcomeEmail", "(unknown)", "N/A", $"NewHireRequestDetail not found with Id={newHireDetailId}");
                return;
            }

            var parentRequest = newHireDetail.HRRequestDetail.ParentRequest;

            // Verify Welcome Email template exists
            var welcomeEmailTemplate = await context.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateName == "Welcome Email" && t.IsActive && !t.IsDeleted);

            if (welcomeEmailTemplate == null)
            {
                _logger.LogError("[WELCOME EMAIL SEND] Welcome Email template not found");
                _ecmLogger.LogEmailNotification(false, "WelcomeEmail", employeeEmail, "N/A", "Welcome Email template not found or inactive");
                return;
            }

            _logger.LogInformation("[WELCOME EMAIL SEND] Sending Welcome Email for {LastName}, {FirstName}",
                newHireDetail.LastName, newHireDetail.FirstName);

            // Create DTO for email sending
            var createNewHireDto = new CreateNewHireRequestDto
            {
                PersonalInfo = new NewHirePersonalInfoDto
                {
                    FirstName = newHireDetail.FirstName,
                    LastName = newHireDetail.LastName,
                    FirstDayEmployment = newHireDetail.FirstDayEmployment,
                    Suffix = newHireDetail.Suffix,
                    PreferredFirstName = newHireDetail.PreferredFirstName,
                    ReferredBy = newHireDetail.ReferredBy,
                    Rehire = newHireDetail.Rehire
                },
                PositionInfo = new NewHirePositionInfoDto
                {
                    CompanyCode = newHireDetail.CompanyCode,
                    PositionCode = newHireDetail.PositionCode,
                    PayrollDeptCode = newHireDetail.PayrollDeptCode,
                    LocationCode = newHireDetail.LocationCode,
                    EmploymentStatus = newHireDetail.EmploymentStatus,
                    IsUnion = newHireDetail.IsUnion,
                    IsApprentice = newHireDetail.IsApprentice,
                    SalaryCode = newHireDetail.SalaryCode
                }
            };

            // Resolve recipients from EmailTemplate.Recipients field using EmailRecipientsService
            // Supports both CompanyDL fields and special recipient keys like 'EMPLOYEE'
            var recipients = await emailRecipientsService.GetRecipientsFromTemplateAsync(
                "Welcome Email",
                newHireDetail.CompanyCode,
                newHireDetail.PayrollDeptCode ?? 0,  // Department code for filtering DL
                managerEmail: null,
                submitterEmail: null,
                employeeEmail: employeeEmail
            );

            // If no recipients resolved from template, use employeeEmail as fallback
            if (!recipients.Any())
            {
                _ecmLogger.LogWarning(LogCategory.EmailNotification, "[WELCOME EMAIL SEND] No recipients resolved from template, using employee email as fallback");
                recipients = new List<string> { employeeEmail };
            }

            var toEmails = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

            _logger.LogInformation("[WELCOME EMAIL SEND] Resolved {RecipientCount} recipient(s): {Recipients}",
                recipients.Count, toEmails);

            // Send email via Azure Service Bus
            var result = await emailService.SendEmailFromTemplateNameAsync(
                "Welcome Email",
                createNewHireDto,
                toEmails,
                null,
                parentRequest.Id
            );

            if (result.Success)
            {
                _logger.LogInformation("[WELCOME EMAIL SEND] ✅ Welcome Email sent successfully to {Recipients}", toEmails);

                _ecmLogger.LogEmailNotification(
                    success: true,
                    operation: "WelcomeEmail",
                    recipient: toEmails,
                    subject: $"Welcome Email - {newHireDetail.FirstName} {newHireDetail.LastName}",
                    errorMessage: null);

                // UPDATE STATUS: Email sent successfully
                var requestDetail = newHireDetail.HRRequestDetail;
                requestDetail.RequestStatusId = (int)Enums.RequestStatus.Completed; // 3
                requestDetail.ViewpointErrorMessage = null;
                await context.SaveChangesAsync();

                _logger.LogInformation("[WELCOME EMAIL] ✅ Set status to Completed - Email sent successfully");
            }
            else
            {
                _ecmLogger.LogWarning(LogCategory.EmailNotification, "[WELCOME EMAIL SEND] Failed to send Welcome Email: {ErrorMessage}", result.Message);

                _ecmLogger.LogEmailNotification(
                    success: false,
                    operation: "WelcomeEmail",
                    recipient: toEmails,
                    subject: $"Welcome Email - {newHireDetail.FirstName} {newHireDetail.LastName}",
                    errorMessage: result.Message);

                // UPDATE STATUS: Email send failed
                var requestDetail = newHireDetail.HRRequestDetail;
                var welcomeEmailError = $"Failed to send Welcome Email: {result.Message}";
                requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed; // 4
                requestDetail.ViewpointErrorMessage = welcomeEmailError;
                await context.SaveChangesAsync();

                await SendFailedRequestEmailAsync(context, requestDetail, welcomeEmailError);
                _logger.LogInformation("[WELCOME EMAIL] ❌ Set status to Failed - Email send failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WELCOME EMAIL SEND] Error sending Welcome Email for DetailId={DetailId}", newHireDetailId);
            _ecmLogger.LogEmailNotification(false, "WelcomeEmail", employeeEmail, "N/A", $"Exception: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates employee in Viewpoint, then sends Welcome Email
    /// Implements 2-attempt retry pattern for Viewpoint update
    /// Status updated to Completed (3) on success, Failed (4) on all retries exhausted
    /// </summary>
    private async Task UpdateEmployeeInViewpointAndSendWelcomeEmailAsync(
        int newHireDetailId,
        string employeeEmail,
        NewHireRequestDetail newHireDetail,
        HRRequest parentRequest,
        int attemptNumber = 1)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        var viewpointService = scope.ServiceProvider.GetRequiredService<IViewpointService>();

        try
        {
            _logger.LogInformation("[WELCOME EMAIL VIEWPOINT UPDATE] Starting Viewpoint update for {LastName}, {FirstName} (Attempt {AttemptNumber}/2)",
                newHireDetail.LastName, newHireDetail.FirstName, attemptNumber);

            // Build the UpdateEmployeeNewHireRequestDto
            var updateRequest = new UpdateEmployeeNewHireRequestDto
            {
                HRCo = newHireDetail.CompanyCode ?? 0,
                HRRef = 0, // Will be populated from request detail if needed
                PRDept = newHireDetail.PayrollDeptCode?.ToString() ?? string.Empty,
                LastName = newHireDetail.LastName ?? string.Empty,
                HireDate = newHireDetail.FirstDayEmployment?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
                CustomFields = new ViewpointCustomFieldsUpdateDto()
            };

            // Call UpdateEmployeeForNewHireInViewPointAsync
            var updateResult = await viewpointService.UpdateEmployeeForNewHireInViewPointAsync(updateRequest);

            if (updateResult.Success)
            {
                _logger.LogInformation("[WELCOME EMAIL VIEWPOINT UPDATE] ✅ Employee update queued successfully in Viewpoint. ActionId: {ActionId}",
                    updateResult.ActionId);

                // Viewpoint update succeeded, wait 2 minutes then fetch fresh employee data
                _logger.LogInformation("[WELCOME EMAIL VIEWPOINT UPDATE] Waiting 2 minutes for Viewpoint to propagate changes...");
                await Task.Delay(TimeSpan.FromMinutes(2));

                // Fetch fresh employee data from Viewpoint
                var freshSearchResult = await viewpointService.SearchEmployeeInNewHireWithAPIAsync(
                    newHireDetail.CompanyCode ?? 0,
                    (newHireDetail.PayrollDeptCode ?? 0).ToString(),
                    newHireDetail.LastName ?? string.Empty,
                    (newHireDetail.FirstDayEmployment ?? DateTime.Now).ToString("yyyy-MM-ddTHH:mm:ss")
                );

                string emailToUse = employeeEmail; // Default to original email

                if (freshSearchResult != null && freshSearchResult.Count == 1)
                {
                    var freshEmployee = freshSearchResult.First();
                    var freshWorkEmail = freshEmployee.CustomFields?.WorkEmail;

                    if (!string.IsNullOrWhiteSpace(freshWorkEmail))
                    {
                        emailToUse = freshWorkEmail;
                        _logger.LogInformation("[WELCOME EMAIL VIEWPOINT UPDATE] ✅ Fresh udWorkEmail fetched from Viewpoint: {FreshEmail}", freshWorkEmail);
                    }
                    else
                    {
                        _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "[WELCOME EMAIL VIEWPOINT UPDATE] Fresh employee found but no udWorkEmail. Using Submitter email as fallback: {SubmitterEmail}", parentRequest.SubmitterEmail);
                        emailToUse = parentRequest.SubmitterEmail ?? employeeEmail;
                    }
                }
                else
                {
                    _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "[WELCOME EMAIL VIEWPOINT UPDATE] Failed to fetch fresh employee data from Viewpoint. Using Submitter email as fallback: {SubmitterEmail}", parentRequest.SubmitterEmail);
                    emailToUse = parentRequest.SubmitterEmail ?? employeeEmail;
                }

                // Send Welcome Email with selected email address
                await SendWelcomeEmailAsync(newHireDetailId, emailToUse);
            }
            else
            {
                _logger.LogError("[WELCOME EMAIL VIEWPOINT UPDATE] ❌ Failed to update employee in Viewpoint. Error: {ErrorMessage}",
                    updateResult.ErrorMessage);

                // Check if we should retry
                if (attemptNumber < 2)
                {
                    var nextAttempt = attemptNumber + 1;
                    _logger.LogInformation("[WELCOME EMAIL VIEWPOINT UPDATE] Scheduling retry attempt {NextAttempt}/2 for DetailId={DetailId} in 2 minutes",
                        nextAttempt, newHireDetailId);

                    // Schedule retry in 2 minutes
                    var retryJobId = Hangfire.BackgroundJob.Schedule(
                        () => UpdateEmployeeInViewpointAndSendWelcomeEmailAsync(newHireDetailId, employeeEmail, newHireDetail, parentRequest, nextAttempt),
                        TimeSpan.FromMinutes(2));

                    _logger.LogInformation("[WELCOME EMAIL VIEWPOINT UPDATE] Scheduled retry job {JobId}", retryJobId);
                }
                else
                {
                    // All retries exhausted
                    _logger.LogError("[WELCOME EMAIL VIEWPOINT UPDATE] All Viewpoint update attempts exhausted (2/2) - Setting status to Failed");

                    var requestDetail = await context.HRRequestDetails.FindAsync(newHireDetailId);
                    if (requestDetail != null)
                    {
                        var vpUpdateError = $"Failed to update employee in Viewpoint after 2 attempts: {updateResult.ErrorMessage}";
                        requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed; // 4
                        requestDetail.ViewpointErrorMessage = vpUpdateError;
                        await context.SaveChangesAsync();

                        await SendFailedRequestEmailAsync(context, requestDetail, vpUpdateError);
                        _logger.LogInformation("[WELCOME EMAIL VIEWPOINT UPDATE] ❌ Set status to Failed after {AttemptNumber} update attempts", attemptNumber);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WELCOME EMAIL VIEWPOINT UPDATE] Error updating employee in Viewpoint for DetailId={DetailId} (Attempt {AttemptNumber}/2)",
                newHireDetailId, attemptNumber);

            // Check if we should retry
            if (attemptNumber < 2)
            {
                var nextAttempt = attemptNumber + 1;
                _logger.LogInformation("[WELCOME EMAIL VIEWPOINT UPDATE] Scheduling retry attempt {NextAttempt}/2 for DetailId={DetailId} in 2 minutes due to exception",
                    nextAttempt, newHireDetailId);

                // Schedule retry in 2 minutes
                var retryJobId = Hangfire.BackgroundJob.Schedule(
                    () => UpdateEmployeeInViewpointAndSendWelcomeEmailAsync(newHireDetailId, employeeEmail, newHireDetail, parentRequest, nextAttempt),
                    TimeSpan.FromMinutes(2));

                _logger.LogInformation("[WELCOME EMAIL VIEWPOINT UPDATE] Scheduled retry job {JobId}", retryJobId);
            }
            else
            {
                // All retries exhausted
                _logger.LogError("[WELCOME EMAIL VIEWPOINT UPDATE] All Viewpoint update attempts exhausted (2/2) due to exception");

                try
                {
                    var requestDetail = await context.HRRequestDetails.FindAsync(newHireDetailId);
                    if (requestDetail != null)
                    {
                        var vpUpdateExError = $"Viewpoint update exception after 2 attempts: {ex.Message}";
                        requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed; // 4
                        requestDetail.ViewpointErrorMessage = vpUpdateExError;
                        await context.SaveChangesAsync();

                        await SendFailedRequestEmailAsync(context, requestDetail, vpUpdateExError);
                        _logger.LogInformation("[WELCOME EMAIL VIEWPOINT UPDATE] ❌ Set status to Failed after {AttemptNumber} update attempts", attemptNumber);
                    }
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "[WELCOME EMAIL VIEWPOINT UPDATE] Error updating status after exception");
                }
            }
        }
    }

    /// <summary>
    /// Sends notification to HRDL and submitter when employee is not found in Viewpoint
    /// </summary>
    private async Task SendWelcomeEmailNotFoundNotificationAsync(int newHireDetailId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();

        try
        {
            // Load new hire request details
            var newHireDetail = await context.NewHireRequestDetails
                .Include(nd => nd.HRRequestDetail)
                .ThenInclude(rd => rd.ParentRequest)
                .FirstOrDefaultAsync(nd => nd.RequestDetailId == newHireDetailId);

            if (newHireDetail == null)
            {
                _logger.LogError("[WELCOME EMAIL NOT FOUND NOTIFICATION] NewHireRequestDetail not found with Id={DetailId}", newHireDetailId);
                _ecmLogger.LogEmailNotification(false, "WelcomeEmailNotFound", "(unknown)", "N/A", $"NewHireRequestDetail not found with Id={newHireDetailId}");
                return;
            }

            var parentRequest = newHireDetail.HRRequestDetail.ParentRequest;
            var companyCode = newHireDetail.CompanyCode ?? 0;

            // Get HRDL from CompanyDL table
            var companyDL = await context.CompanyDLs
                .FirstOrDefaultAsync(dl => dl.CompanyCode == companyCode && !dl.IsDeleted);

            if (companyDL == null || string.IsNullOrWhiteSpace(companyDL.HRDL))
            {
                _logger.LogError("[WELCOME EMAIL NOT FOUND NOTIFICATION] CompanyDL or HRDL not found for CompanyCode={CompanyCode}", companyCode);
                _ecmLogger.LogEmailNotification(false, "WelcomeEmailNotFound", "(no HRDL)", "N/A", $"CompanyDL or HRDL not found for CompanyCode={companyCode}");
                return;
            }

            var hrdlEmail = companyDL.HRDL;

            _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "[WELCOME EMAIL NOT FOUND NOTIFICATION] Employee not found in Viewpoint. Notifying HRDL ({HRDLEmail}) and submitter ({SubmitterEmail})",
                hrdlEmail, parentRequest.SubmitterEmail);

            // Create notification email body
            var notificationBody = $@"
                <p>The Welcome Email could not be sent for the following new hire:</p>
                <ul>
                    <li><strong>Employee Name:</strong> {newHireDetail.FirstName} {newHireDetail.LastName}</li>
                    <li><strong>First Day of Employment:</strong> {newHireDetail.FirstDayEmployment:yyyy-MM-dd}</li>
                    <li><strong>Request ID:</strong> {parentRequest.Id}</li>
                    <li><strong>Reason:</strong> Employee could not be verified in Viewpoint API</li>
                </ul>
                <p>Please verify the employee data and resubmit the request if needed.</p>
            ";

            // Log to NotificationQueue
            var notificationQueue = new NotificationQueue
            {
                RequestId = parentRequest.Id,
                TemplateId = null,
                ToEmail = hrdlEmail,
                CcEmail = parentRequest.SubmitterEmail,
                Subject = $"Welcome Email Not Sent - Employee Not Found: {newHireDetail.LastName}, {newHireDetail.FirstName}",
                Body = notificationBody,
                Status = "EmployeeNotFound",
                CreatedBy = 0 // System user
            };

            context.NotificationQueue.Add(notificationQueue);
            await context.SaveChangesAsync();

            _logger.LogInformation("[WELCOME EMAIL NOT FOUND NOTIFICATION] ✅ Notification logged for HRDL ({HRDLEmail}) and submitter", hrdlEmail);

            _ecmLogger.LogEmailNotification(
                success: true,
                operation: "WelcomeEmailNotFound",
                recipient: hrdlEmail,
                subject: notificationQueue.Subject,
                errorMessage: "Employee not found in Viewpoint - notification sent to HRDL");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WELCOME EMAIL NOT FOUND NOTIFICATION] Error sending notification for DetailId={DetailId}", newHireDetailId);
            _ecmLogger.LogEmailNotification(false, "WelcomeEmailNotFound", "(unknown)", "N/A", $"Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends notification to HRDL and submitter when Viewpoint API verification fails
    /// </summary>
    private async Task SendWelcomeEmailVerificationFailedNotificationAsync(int newHireDetailId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();

        try
        {
            // Load new hire request details
            var newHireDetail = await context.NewHireRequestDetails
                .Include(nd => nd.HRRequestDetail)
                .ThenInclude(rd => rd.ParentRequest)
                .FirstOrDefaultAsync(nd => nd.RequestDetailId == newHireDetailId);

            if (newHireDetail == null)
            {
                _logger.LogError("[WELCOME EMAIL VERIFICATION FAILED] NewHireRequestDetail not found with Id={DetailId}", newHireDetailId);
                _ecmLogger.LogEmailNotification(false, "WelcomeEmailVerificationFailed", "(unknown)", "N/A", $"NewHireRequestDetail not found with Id={newHireDetailId}");
                return;
            }

            var parentRequest = newHireDetail.HRRequestDetail.ParentRequest;
            var companyCode = newHireDetail.CompanyCode ?? 0;

            // Get HRDL from CompanyDL table
            var companyDL = await context.CompanyDLs
                .FirstOrDefaultAsync(dl => dl.CompanyCode == companyCode && !dl.IsDeleted);

            if (companyDL == null || string.IsNullOrWhiteSpace(companyDL.HRDL))
            {
                _logger.LogError("[WELCOME EMAIL VERIFICATION FAILED] CompanyDL or HRDL not found for CompanyCode={CompanyCode}", companyCode);
                _ecmLogger.LogEmailNotification(false, "WelcomeEmailVerificationFailed", "(no HRDL)", "N/A", $"CompanyDL or HRDL not found for CompanyCode={companyCode}");
                return;
            }

            var hrdlEmail = companyDL.HRDL;

            _logger.LogError("[WELCOME EMAIL VERIFICATION FAILED] API error occurred. Notifying HRDL ({HRDLEmail}) and submitter ({SubmitterEmail})",
                hrdlEmail, parentRequest.SubmitterEmail);

            // Create notification email body
            var notificationBody = $@"
                <p>The Welcome Email verification process failed for the following new hire:</p>
                <ul>
                    <li><strong>Employee Name:</strong> {newHireDetail.FirstName} {newHireDetail.LastName}</li>
                    <li><strong>First Day of Employment:</strong> {newHireDetail.FirstDayEmployment:yyyy-MM-dd}</li>
                    <li><strong>Request ID:</strong> {parentRequest.Id}</li>
                    <li><strong>Reason:</strong> Viewpoint API verification failed due to communication errors</li>
                </ul>
                <p>Please retry the Welcome Email process manually or contact system support.</p>
            ";

            // Log to NotificationQueue
            var notificationQueue = new NotificationQueue
            {
                RequestId = parentRequest.Id,
                TemplateId = null,
                ToEmail = hrdlEmail,
                CcEmail = parentRequest.SubmitterEmail,
                Subject = $"Welcome Email Verification Failed: {newHireDetail.LastName}, {newHireDetail.FirstName}",
                Body = notificationBody,
                Status = "VerificationFailed",
                CreatedBy = 0 // System user
            };

            context.NotificationQueue.Add(notificationQueue);
            await context.SaveChangesAsync();

            _logger.LogInformation("[WELCOME EMAIL VERIFICATION FAILED] ✅ Failure notification logged for HRDL ({HRDLEmail}) and submitter", hrdlEmail);

            _ecmLogger.LogEmailNotification(
                success: true,
                operation: "WelcomeEmailVerificationFailed",
                recipient: hrdlEmail,
                subject: notificationQueue.Subject,
                errorMessage: "Viewpoint API verification failed - notification sent to HRDL");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WELCOME EMAIL VERIFICATION FAILED] Error sending failure notification for DetailId={DetailId}", newHireDetailId);
            _ecmLogger.LogEmailNotification(false, "WelcomeEmailVerificationFailed", "(unknown)", "N/A", $"Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Schedules a new hire pre-employment processing job to execute on the FirstDayEmployment date
    /// </summary>
    public async Task<string> ScheduleNewHirePreEmploymentProcessingJob(int hrRequestDetailId, DateTime firstDayEmployment, string? submitterEmail = null)
    {
        // Interpret FirstDayEmployment as a server-local calendar date (Central Time in production).
        // Incoming DateTime may have Kind = Unspecified; tagging it Local prevents Hangfire and
        // comparisons from treating it as UTC, which shifted the job a day earlier in local time.
        var scheduledDate = DateTime.SpecifyKind(firstDayEmployment.Date, DateTimeKind.Local);

        _logger.LogInformation("[NEW HIRE PRE-EMPLOYMENT] Scheduling pre-employment processing for HR request detail {HRRequestDetailId} at {ScheduledDate} (local) (FirstDayEmployment: {FirstDayEmployment})",
            hrRequestDetailId, scheduledDate, firstDayEmployment);

        // Compare calendar dates in local time: only enqueue immediately if FirstDayEmployment is today or past.
        string jobId;
        if (scheduledDate.Date <= DateTime.Now.Date)
        {
            _logger.LogInformation("[NEW HIRE PRE-EMPLOYMENT] Scheduled date {ScheduledDate} is today or in the past (local) - enqueueing immediately for HR request detail {HRRequestDetailId}",
                scheduledDate, hrRequestDetailId);
            jobId = BackgroundJob.Enqueue(() => ProcessNewHirePreEmploymentAsync(hrRequestDetailId, submitterEmail));
        }
        else
        {
            jobId = BackgroundJob.Schedule(() => ProcessNewHirePreEmploymentAsync(hrRequestDetailId, submitterEmail), scheduledDate);
        }

        // Store the job ID in the HR request detail using a separate DbContext scope
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();

        var requestDetail = await context.HRRequestDetails.FindAsync(hrRequestDetailId);
        if (requestDetail != null)
        {
            requestDetail.HangfireJobId = jobId;
            requestDetail.ModifiedDate = DateTime.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogInformation("[NEW HIRE PRE-EMPLOYMENT] Stored Hangfire job ID {JobId} for HR request detail {HRRequestDetailId}",
                jobId, hrRequestDetailId);
        }
        else
        {
            _ecmLogger.LogWarning(LogCategory.BackgroundJob, "[NEW HIRE PRE-EMPLOYMENT] HR request detail {HRRequestDetailId} not found when trying to store job ID {JobId}",
                hrRequestDetailId, jobId);
        }

        return jobId;
    }

    /// <summary>
    /// Processes new hire pre-employment preparation on the FirstDayEmployment date
    /// Calls UpdateEmployeeForNewHireInViewPointAsync to update employee in Viewpoint
    /// Updates RequestStatusId to Completed (3) if successful, Failed (4) if unsuccessful
    /// </summary>
    public async Task ProcessNewHirePreEmploymentAsync(int hrRequestDetailId, string? submitterEmail = null)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        var viewpointService = scope.ServiceProvider.GetRequiredService<IViewpointService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            _logger.LogInformation("[NEW HIRE PRE-EMPLOYMENT] Starting pre-employment processing for HR request detail {HRRequestDetailId}", hrRequestDetailId);

            // Get the HR request detail with related data
            var requestDetail = await context.HRRequestDetails
                .Include(rd => rd.ParentRequest)
                .Include(rd => rd.NewHireDetails)
                .FirstOrDefaultAsync(rd => rd.Id == hrRequestDetailId);

            if (requestDetail == null)
            {
                _ecmLogger.LogWarning(LogCategory.BackgroundJob, "[NEW HIRE PRE-EMPLOYMENT] HR request detail {HRRequestDetailId} not found", hrRequestDetailId);
                return;
            }

            var newHireDetail = requestDetail.NewHireDetails;
            if (newHireDetail == null)
            {
                _ecmLogger.LogWarning(LogCategory.BackgroundJob, "[NEW HIRE PRE-EMPLOYMENT] NewHireRequestDetail not found for HRRequestDetail {HRRequestDetailId}", hrRequestDetailId);

                // Set status to Failed with all fields
                var notFoundError = "NewHireRequestDetail data not found";
                requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed; // 4
                requestDetail.ViewpointErrorMessage = notFoundError;
                requestDetail.ViewpointProcessed = false;
                requestDetail.ViewpointProcessedDate = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, requestDetail, notFoundError, submitterEmail);

                _logger.LogInformation("[NEW HIRE PRE-EMPLOYMENT] ❌ Set HR request detail {HRRequestDetailId} status to Failed - NewHireRequestDetail not found", hrRequestDetailId);

                // Send failure notification
                await notificationService.SendHRRequestCompletionNotificationAsync(
                    submitterEmail ?? "system",
                    hrRequestDetailId,
                    "Unknown Employee",
                    false,
                    "New hire request details not found in database"
                );

                return;
            }

            // Build the UpdateEmployeeNewHireRequestDto from NewHireRequestDetail
            var updateRequest = new UpdateEmployeeNewHireRequestDto
            {
                HRCo = newHireDetail.CompanyCode ?? 0,
                HRRef = requestDetail.EmployeeId,  // Use the Viewpoint Employee ID as HRRef
                PRDept = newHireDetail.PayrollDeptCode?.ToString() ?? string.Empty,
                LastName = newHireDetail.LastName ?? string.Empty,
                HireDate = newHireDetail.FirstDayEmployment?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
                // Custom fields would be populated here if needed
                CustomFields = new ViewpointCustomFieldsUpdateDto()
            };

            _ecmLogger.LogInfo(LogCategory.ViewpointIntegration,
                "NewHire Pre-Employment: Calling UpdateEmployeeForNewHireInViewPointAsync for employee {LastName}, {FirstName} (EmployeeId: {EmployeeId})",
                newHireDetail.LastName, newHireDetail.FirstName, requestDetail.EmployeeId);

            // Call UpdateEmployeeForNewHireInViewPointAsync
            var updateResult = await viewpointService.UpdateEmployeeForNewHireInViewPointAsync(updateRequest);

            if (updateResult.Success)
            {
                _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                    "NewHire Pre-Employment: Employee update queued successfully in Viewpoint. ActionId: {ActionId}", updateResult.ActionId);

                // Verify the action was successfully processed in Viewpoint
                // Poll for up to 10 minutes (10 attempts with 1 minute intervals)
                ViewpointActionDetailResponseDto? verificationResult = null;
                int maxRetries = 10;
                int retryCount = 0;
                int delayMilliseconds = 60000; // 1 minute

                while (retryCount < maxRetries)
                {
                    verificationResult = await viewpointService.VerifyViewpointActionAsync(updateResult.ActionId);

                    if (verificationResult == null)
                    {
                        _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
                            "NewHire Pre-Employment: Verification attempt {Attempt}/{MaxRetries} failed - no response from Viewpoint for employee {LastName}, {FirstName}",
                            retryCount + 1, maxRetries, newHireDetail.LastName, newHireDetail.FirstName);

                        var noVerifyResponseError = "Failed to get verification response from Viewpoint";
                        requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                        requestDetail.ViewpointErrorMessage = noVerifyResponseError;
                        requestDetail.ViewpointProcessed = false;
                        requestDetail.ViewpointProcessedDate = DateTime.UtcNow;
                        requestDetail.ModifiedDate = DateTime.UtcNow;
                        await context.SaveChangesAsync();
                        await SendFailedRequestEmailAsync(context, requestDetail, noVerifyResponseError, submitterEmail);

                        // Send failure notification
                        var failedEmployeeName = $"{newHireDetail.FirstName} {newHireDetail.LastName}";
                        await notificationService.SendHRRequestCompletionNotificationAsync(
                            submitterEmail ?? "system",
                            hrRequestDetailId,
                            failedEmployeeName,
                            false,
                            "New hire pre-employment failed: No verification response from Viewpoint"
                        );
                        return;
                    }

                    _ecmLogger.LogInfo(LogCategory.ViewpointIntegration,
                        "NewHire Pre-Employment: Verification attempt {Attempt}/{MaxRetries} - Status: {Status} for employee {LastName}, {FirstName}",
                        retryCount + 1, maxRetries, verificationResult.Status ?? "null", newHireDetail.LastName, newHireDetail.FirstName);

                    // Check if action completed successfully
                    if (string.Equals(verificationResult.Status, "Successful", StringComparison.OrdinalIgnoreCase))
                    {
                        _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                            "NewHire Pre-Employment: Action completed successfully for employee {LastName}, {FirstName}", newHireDetail.LastName, newHireDetail.FirstName);
                        break;
                    }

                    // Check if action failed
                    if (string.Equals(verificationResult.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                    {
                        _ecmLogger.LogError(LogCategory.ViewpointIntegration,
                            "NewHire Pre-Employment: Action failed in Viewpoint for employee {LastName}, {FirstName}", newHireDetail.LastName, newHireDetail.FirstName);

                        var vpActionFailedError = "Viewpoint action failed";
                        requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                        requestDetail.ViewpointErrorMessage = vpActionFailedError;
                        requestDetail.ViewpointProcessed = false;
                        requestDetail.ViewpointProcessedDate = DateTime.UtcNow;
                        requestDetail.ModifiedDate = DateTime.UtcNow;
                        await context.SaveChangesAsync();
                        await SendFailedRequestEmailAsync(context, requestDetail, vpActionFailedError, submitterEmail);

                        // Send failure notification
                        var failedEmployeeName = $"{newHireDetail.FirstName} {newHireDetail.LastName}";
                        await notificationService.SendHRRequestCompletionNotificationAsync(
                            submitterEmail ?? "system",
                            hrRequestDetailId,
                            failedEmployeeName,
                            false,
                            "New hire pre-employment failed: Viewpoint action failed"
                        );
                        return;
                    }

                    // If status is still "Queued" or other intermediate status, wait and retry
                    retryCount++;

                    if (retryCount < maxRetries)
                    {
                        _ecmLogger.LogInfo(LogCategory.ViewpointIntegration,
                            "NewHire Pre-Employment: Action still processing (Status: {Status}). Waiting {Delay} seconds before retry {Retry}/{MaxRetries}...",
                            verificationResult.Status, delayMilliseconds / 1000, retryCount + 1, maxRetries);

                        await Task.Delay(delayMilliseconds);
                    }
                }

                // Check if we exhausted all retries without success
                if (retryCount >= maxRetries && !string.Equals(verificationResult?.Status, "Successful", StringComparison.OrdinalIgnoreCase))
                {
                    _ecmLogger.LogError(LogCategory.ViewpointIntegration,
                        "NewHire Pre-Employment: Verification timed out after {MaxRetries} attempts for employee {LastName}, {FirstName}. Final status: {Status}",
                        maxRetries, newHireDetail.LastName, newHireDetail.FirstName, verificationResult?.Status ?? "unknown");

                    var verifyTimeoutError = $"Verification timed out after {maxRetries} attempts. Final status: {verificationResult?.Status ?? "unknown"}";
                    requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    requestDetail.ViewpointErrorMessage = verifyTimeoutError;
                    requestDetail.ViewpointProcessed = false;
                    requestDetail.ViewpointProcessedDate = DateTime.UtcNow;
                    requestDetail.ModifiedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    await SendFailedRequestEmailAsync(context, requestDetail, verifyTimeoutError, submitterEmail);

                    // Send failure notification
                    var failedEmployeeName = $"{newHireDetail.FirstName} {newHireDetail.LastName}";
                    await notificationService.SendHRRequestCompletionNotificationAsync(
                        submitterEmail ?? "system",
                        hrRequestDetailId,
                        failedEmployeeName,
                        false,
                        $"New hire pre-employment failed: Verification timed out after {maxRetries} attempts"
                    );
                    return;
                }

                _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                    "NewHire Pre-Employment: Verification successful for employee {LastName}, {FirstName}", newHireDetail.LastName, newHireDetail.FirstName);

                // Set status to Completed
                requestDetail.RequestStatusId = (int)Enums.RequestStatus.Completed;
                requestDetail.ViewpointErrorMessage = null;
                requestDetail.ViewpointProcessed = true;
                requestDetail.ViewpointProcessedDate = DateTime.UtcNow;

                await context.SaveChangesAsync();

                // Fetch fresh employee data from Viewpoint to get updated udWorkEmail
                var freshSearchResult = await viewpointService.SearchEmployeeInNewHireWithAPIAsync(
                    newHireDetail.CompanyCode ?? 0,
                    (newHireDetail.PayrollDeptCode ?? 0).ToString(),
                    newHireDetail.LastName ?? string.Empty,
                    (newHireDetail.FirstDayEmployment ?? DateTime.Now).ToString("yyyy-MM-ddTHH:mm:ss")
                );

                string emailToUse = submitterEmail ?? "system"; // Default to submitter email

                if (freshSearchResult != null && freshSearchResult.Count == 1)
                {
                    var freshEmployee = freshSearchResult.First();
                    var freshWorkEmail = freshEmployee.CustomFields?.WorkEmail;

                    if (!string.IsNullOrWhiteSpace(freshWorkEmail))
                    {
                        emailToUse = freshWorkEmail;
                        _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                            "NewHire Pre-Employment: Fresh udWorkEmail fetched from Viewpoint: {FreshEmail}", freshWorkEmail);
                    }
                    else
                    {
                        _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
                            "NewHire Pre-Employment: Fresh employee found but no udWorkEmail. Using Submitter email as fallback: {SubmitterEmail}", submitterEmail);
                        emailToUse = submitterEmail ?? "system";
                    }
                }
                else
                {
                    _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
                        "NewHire Pre-Employment: Failed to fetch fresh employee data from Viewpoint. Using Submitter email as fallback: {SubmitterEmail}", submitterEmail);
                    emailToUse = submitterEmail ?? "system";
                }

                // Send Welcome Email with the selected email address
                _ecmLogger.LogInfo(LogCategory.EmailNotification,
                    "NewHire Pre-Employment: Sending Welcome Email to {EmailAddress}", emailToUse);
                await SendWelcomeEmailAsync(hrRequestDetailId, emailToUse);

                // Send success notification
                var employeeName = $"{newHireDetail.FirstName} {newHireDetail.LastName}";
                await notificationService.SendHRRequestCompletionNotificationAsync(
                    submitterEmail ?? "system",
                    hrRequestDetailId,
                    employeeName,
                    true,
                    "New hire employee pre-employment preparation completed successfully. Update queued in Viewpoint."
                );

                _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                    "NewHire Pre-Employment: Set HR request detail {HRRequestDetailId} status to Completed", hrRequestDetailId);
            }
            else
            {
                _ecmLogger.LogError(LogCategory.ViewpointIntegration,
                    "NewHire Pre-Employment: Failed to update employee in Viewpoint. Error: {ErrorMessage}", updateResult.ErrorMessage);

                // Set status to Failed with all fields
                var queueFailedError = updateResult.ErrorMessage ?? "Failed to queue employee update in Viewpoint";
                requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed; // 4
                requestDetail.ViewpointErrorMessage = queueFailedError;
                requestDetail.ViewpointProcessed = false;
                requestDetail.ViewpointProcessedDate = DateTime.UtcNow;

                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, requestDetail, queueFailedError, submitterEmail);

                // Send failure notification
                var employeeName = $"{newHireDetail.FirstName} {newHireDetail.LastName}";
                await notificationService.SendHRRequestCompletionNotificationAsync(
                    submitterEmail ?? "system",
                    hrRequestDetailId,
                    employeeName,
                    false,
                    $"New hire employee pre-employment preparation failed. Error: {updateResult.ErrorMessage}"
                );

                _logger.LogInformation("[NEW HIRE PRE-EMPLOYMENT] ❌ Set HR request detail {HRRequestDetailId} status to Failed", hrRequestDetailId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NEW HIRE PRE-EMPLOYMENT] Error processing new hire pre-employment for HR request detail {HRRequestDetailId}", hrRequestDetailId);

            // Set status to Failed with all fields and send notification
            try
            {
                var requestDetail = await context.HRRequestDetails.FindAsync(hrRequestDetailId);
                if (requestDetail != null)
                {
                    var exceptionError = ex.Message;
                    requestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed; // 4
                    requestDetail.ViewpointErrorMessage = exceptionError;
                    requestDetail.ViewpointProcessed = false;
                    requestDetail.ViewpointProcessedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    await SendFailedRequestEmailAsync(context, requestDetail, exceptionError, submitterEmail);

                    _logger.LogInformation("[NEW HIRE PRE-EMPLOYMENT] ❌ Set HR request detail {HRRequestDetailId} status to Failed due to exception", hrRequestDetailId);

                    // Send failure notification
                    try
                    {
                        var newHireDetail = await context.NewHireRequestDetails
                            .FirstOrDefaultAsync(n => n.RequestDetailId == hrRequestDetailId);

                        var employeeName = newHireDetail != null
                            ? $"{newHireDetail.FirstName} {newHireDetail.LastName}"
                            : "Unknown Employee";

                        await notificationService.SendHRRequestCompletionNotificationAsync(
                            submitterEmail ?? "system",
                            hrRequestDetailId,
                            employeeName,
                            false,
                            $"New hire pre-employment processing failed with exception: {ex.Message}"
                        );

                        _logger.LogInformation("[NEW HIRE PRE-EMPLOYMENT] ✅ Sent failure notification for HR request detail {HRRequestDetailId}", hrRequestDetailId);
                    }
                    catch (Exception notificationEx)
                    {
                        _logger.LogError(notificationEx, "[NEW HIRE PRE-EMPLOYMENT] Error sending failure notification for HR request detail {HRRequestDetailId}", hrRequestDetailId);
                    }
                }
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "[NEW HIRE PRE-EMPLOYMENT] Error updating request detail status after exception");
            }
        }
    }

    /// <summary>
    /// Updates employee information in Viewpoint for a promotion/transfer request
    /// Updates 7 Viewpoint fields: PRCo, PRGroup, PRDept, PositionCode, udSupervisor, udPhysicalLocation, Status
    /// Updates RequestStatusId to Completed (3) if successful, Failed (4) if unsuccessful
    /// </summary>
    private async Task UpdateEmployeeForPromotionTransferInViewPointAsync(Core.Entities.HRRequestDetail requestDetail, string? submitterEmail)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();

        // Reload requestDetail in this context so changes can be tracked and saved
        var trackedRequestDetail = await context.HRRequestDetails.FindAsync(requestDetail.Id);
        if (trackedRequestDetail == null)
        {
            _ecmLogger.LogWarning(LogCategory.BackgroundJob, "HR request detail {HRRequestDetailId} not found when reloading",
                requestDetail.Id);
            return;
        }

        try
        {
            _logger.LogInformation("Processing Promotion/Transfer Viewpoint update for HR request detail {HRRequestDetailId}",
                trackedRequestDetail.Id);

            // Load promotion request detail with all related data
            // Note: HRRequestDetail doesn't have Employee navigation property, use EmployeeId instead
            var promotionDetail = await context.PromotionRequestDetails
                .Include(p => p.HRRequestDetail)
                .FirstOrDefaultAsync(p => p.RequestDetailId == trackedRequestDetail.Id);

            if (promotionDetail == null)
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Promotion request detail not found for HR request detail {HRRequestDetailId}",
                    trackedRequestDetail.Id);
                var promoNotFoundError = "Promotion request detail not found";
                trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                trackedRequestDetail.ViewpointErrorMessage = promoNotFoundError;
                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, trackedRequestDetail, promoNotFoundError, submitterEmail);
                return;
            }

            // Get employee ID from HRRequestDetail
            var employeeId = promotionDetail.HRRequestDetail.EmployeeId;
            if (employeeId == null)
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Employee ID not found for promotion request detail {PromotionDetailId}",
                    promotionDetail.Id);
                var empIdNotFoundError = "Employee ID not found";
                trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                trackedRequestDetail.ViewpointErrorMessage = empIdNotFoundError;
                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, trackedRequestDetail, empIdNotFoundError, submitterEmail);
                return;
            }

            // Search for employee in Viewpoint by HRRef (EmployeeNumber)
            _logger.LogInformation("Searching for employee in Viewpoint with HRRef (EmployeeNumber): {EmployeeNumber}",
                employeeId);

            var viewpointEmployee = await _viewpointService.GetEmployeeByNumberAsync(employeeId.ToString());

            if (viewpointEmployee == null)
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Employee {EmployeeNumber} not found in Viewpoint", employeeId);
                var empVpNotFoundError = $"Employee {employeeId} not found in Viewpoint";
                trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                trackedRequestDetail.ViewpointErrorMessage = empVpNotFoundError;
                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, trackedRequestDetail, empVpNotFoundError, submitterEmail);
                return;
            }

            var promoEmployeeFullName = $"{viewpointEmployee.FirstName} {viewpointEmployee.LastName}".Trim();

            _logger.LogInformation("Employee {EmployeeName} ({EmployeeNumber}) found in Viewpoint, updating fields for promotion/transfer",
                promoEmployeeFullName, employeeId);

            // Update Viewpoint employee fields with promotion data
            viewpointEmployee.PRCo = promotionDetail.NewPayrollCompanyCode;
            viewpointEmployee.PRGroup = promotionDetail.NewPayrollGroupCode;
            viewpointEmployee.PRDept = promotionDetail.NewPayrollDeptCode.ToString();
            viewpointEmployee.PositionCode = promotionDetail.NewPositionCode;
            viewpointEmployee.Status = promotionDetail.NewStatus;
            viewpointEmployee.EarnCode = promotionDetail.NewSalaryCode;

            // Update custom fields
            if (viewpointEmployee.CustomFields == null)
            {
                viewpointEmployee.CustomFields = new ViewpointCustomFields();
            }

            // Set SupervisorId as JsonElement
            if (promotionDetail.NewSupervisorId.HasValue)
            {
                viewpointEmployee.CustomFields.SupervisorId = JsonSerializer.SerializeToElement(promotionDetail.NewSupervisorId.Value);
            }

            // Set PhysicalLocation as JsonElement
            viewpointEmployee.CustomFields.PhysicalLocation = JsonSerializer.SerializeToElement(promotionDetail.NewPhysicalLocationCode);

            _logger.LogInformation("Updating employee {EmployeeNumber} in Viewpoint with new promotion data: " +
                "PRCo={PRCo}, PRGroup={PRGroup}, PRDept={PRDept}, PositionCode={PositionCode}, " +
                "SupervisorId={SupervisorId}, PhysicalLocation={PhysicalLocation}, Status={Status}, EarnCode={EarnCode}",
                employeeId,
                viewpointEmployee.PRCo,
                viewpointEmployee.PRGroup,
                viewpointEmployee.PRDept,
                viewpointEmployee.PositionCode,
                viewpointEmployee.CustomFields.SupervisorId,
                viewpointEmployee.CustomFields.PhysicalLocation,
                viewpointEmployee.Status,
                viewpointEmployee.EarnCode);

            // Call Viewpoint API to update employee
            _logger.LogInformation("Calling Viewpoint API to update employee for promotion/transfer");
            var updateResult = await _viewpointService.UpdateEmployeeForPromotionTransferInViewPointAsync(viewpointEmployee);

            if (!updateResult.Success || string.IsNullOrEmpty(updateResult.ActionId))
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Failed to queue promotion update in Viewpoint for employee {EmployeeName} ({EmployeeNumber}). Error: {Error}",
                    promoEmployeeFullName, employeeId, updateResult.ErrorMessage);

                var promoUpdateError = updateResult.ErrorMessage ?? "Failed to update employee in Viewpoint";
                trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                trackedRequestDetail.ViewpointErrorMessage = promoUpdateError;
                trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, trackedRequestDetail, promoUpdateError, submitterEmail);
                return;
            }

            // Store ActionId in ViewpointErrorMessage for tracking (will be cleared if verification succeeds)
            trackedRequestDetail.ViewpointErrorMessage = $"ActionId: {updateResult.ActionId}";
            trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
            await context.SaveChangesAsync();

            _logger.LogInformation("Promotion update queued successfully with action ID: {ActionId}. Verifying action status...",
                updateResult.ActionId);

            // Send notification that verification has started
            try
            {
                var employeeName = $"{viewpointEmployee.FirstName} {viewpointEmployee.LastName}".Trim();
                var verificationStartMessage = $"Verifying Promotion/Transfer for {employeeName} (Employee #{employeeId}) in Viewpoint...";

                await _notificationService.SendHRRequestStatusUpdateAsync(
                    userId: submitterEmail ?? "system",
                    hrRequestDetailId: trackedRequestDetail.Id,
                    status: "Verifying",
                    employeeName: employeeName,
                    message: verificationStartMessage
                );

                _logger.LogInformation("Sent verification start notification for promotion request {HRRequestDetailId}",
                    trackedRequestDetail.Id);
            }
            catch (Exception notifEx)
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Failed to send verification start notification for promotion request {HRRequestDetailId}. Error: {Error}",
                    trackedRequestDetail.Id, notifEx.Message);
            }

            // Verify the action was successfully processed in Viewpoint
            // Poll for up to 10 minutes (10 attempts with 1 minute intervals)
            ViewpointActionDetailResponseDto? verificationResult = null;
            int maxRetries = 10;
            int retryCount = 0;
            int delayMilliseconds = 60000; // 1 minute

            while (retryCount < maxRetries)
            {
                verificationResult = await _viewpointService.VerifyViewpointActionAsync(updateResult.ActionId);

                if (verificationResult == null)
                {
                    _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Verification attempt {Attempt}/{MaxRetries} failed - no response from Viewpoint for employee {EmployeeName} ({EmployeeNumber})",
                        retryCount + 1, maxRetries, promoEmployeeFullName, employeeId);

                    var promoVerifyError = "Failed to get verification response from Viewpoint";
                    trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    trackedRequestDetail.ViewpointErrorMessage = promoVerifyError;
                    trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    await SendFailedRequestEmailAsync(context, trackedRequestDetail, promoVerifyError, submitterEmail);
                    return;
                }

                _logger.LogInformation("Verification attempt {Attempt}/{MaxRetries} - Status: {Status} for employee {EmployeeNumber}",
                    retryCount + 1, maxRetries, verificationResult.Status ?? "null", employeeId);

                // Check if action completed successfully
                if (string.Equals(verificationResult.Status, "Successful", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Action completed successfully for employee {EmployeeNumber}", employeeId);
                    break;
                }

                // Check if action failed
                if (string.Equals(verificationResult.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    var promoFailedContext = verificationResult.ContextJson != null
                        ? JsonSerializer.Serialize(verificationResult.ContextJson)
                        : "null";
                    _ecmLogger.LogError(LogCategory.ViewpointIntegration,
                        "Promotion action failed in Viewpoint for employee {EmployeeName} ({EmployeeNumber}). ActionId={ActionId}, Status={Status}, Context={Context}",
                        promoEmployeeFullName, employeeId, verificationResult.Id, verificationResult.Status, promoFailedContext);

                    var promoActionFailedError = "Viewpoint action failed";
                    trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    trackedRequestDetail.ViewpointErrorMessage = promoActionFailedError;
                    trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    await SendFailedRequestEmailAsync(context, trackedRequestDetail, promoActionFailedError, submitterEmail);
                    return;
                }

                // If status is still "Queued" or other intermediate status, wait and retry
                retryCount++;

                if (retryCount < maxRetries)
                {
                    _logger.LogInformation("Action still processing (Status: {Status}). Waiting {Delay} seconds before retry {Retry}/{MaxRetries}...",
                        verificationResult.Status, delayMilliseconds / 1000, retryCount + 1, maxRetries);

                    await Task.Delay(delayMilliseconds);
                }
            }

            // Check if we exhausted all retries without success
            if (retryCount >= maxRetries && !string.Equals(verificationResult?.Status, "Successful", StringComparison.OrdinalIgnoreCase))
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Verification timed out after {MaxRetries} attempts for employee {EmployeeName} ({EmployeeNumber}). Final status: {Status}",
                    maxRetries, promoEmployeeFullName, employeeId, verificationResult?.Status ?? "unknown");

                var promoTimeoutError = $"Verification timed out after {maxRetries} attempts. Final status: {verificationResult?.Status ?? "unknown"}";
                trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                trackedRequestDetail.ViewpointErrorMessage = promoTimeoutError;
                trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, trackedRequestDetail, promoTimeoutError, submitterEmail);
                return;
            }

            _logger.LogInformation("Verification successful. Updating Employee table with verified data");

            // Update Employee table with verified data from Viewpoint
            if (verificationResult.Data != null && verificationResult.Data.Key?.HRCo != null && verificationResult.Data.Key?.HRRef != null)
            {
                var employee = await context.Employees
                    .FirstOrDefaultAsync(e =>
                        e.CompanyCode == verificationResult.Data.Key.HRCo.Value &&
                        e.EmployeeNumber == verificationResult.Data.Key.HRRef.Value);

                if (employee != null)
                {
                    // Update employee fields with verified data
                    employee.PayrollCompanyCode = verificationResult.Data.PRCo;
                    employee.PayrollGroupCode = verificationResult.Data.PRGroup;

                    if (int.TryParse(verificationResult.Data.PRDept, out int prDept))
                    {
                        employee.PayrollDeptCode = prDept;
                    }

                    employee.PositionCode = verificationResult.Data.PositionCode;
                    employee.EmploymentStatus = verificationResult.Data.Status;
                    employee.SupervisorId = verificationResult.Data.CustomFields?.udSupervisor;
                    employee.PhysicalLocationCode = verificationResult.Data.CustomFields?.udPhysicalLocation;
                    employee.SalaryCode = promotionDetail.NewSalaryCode;
                    employee.ViewpointSyncDate = DateTime.UtcNow;
                    employee.ModifiedDate = DateTime.UtcNow;

                    _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                        "Updated Employee {EmployeeNumber} for Promotion/Transfer: SalaryCode={SalaryCode}, ViewpointSyncDate={SyncDate}",
                        employeeId, promotionDetail.NewSalaryCode, employee.ViewpointSyncDate);
                }
                else
                {
                    _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
                        "Employee {EmployeeNumber} not found in Employee table for Promotion/Transfer update",
                        employeeId);
                }
            }

            _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                "Successfully completed promotion/transfer Viewpoint update for employee {EmployeeName} ({EmployeeNumber})",
                promoEmployeeFullName, employeeId);

            trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Completed;
            trackedRequestDetail.ViewpointProcessed = true;
            trackedRequestDetail.ViewpointProcessedDate = DateTime.UtcNow;
            // Keep ActionId in ViewpointErrorMessage for tracking (do not clear)
            trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Send real-time notification to frontend
            try
            {
                var employeeName = $"{viewpointEmployee.FirstName} {viewpointEmployee.LastName}".Trim();
                var message = $"Promotion/Transfer for {employeeName} (Employee #{employeeId}) has been successfully processed in Viewpoint";

                await _notificationService.SendHRRequestCompletionNotificationAsync(
                    userId: submitterEmail ?? "system",
                    hrRequestDetailId: trackedRequestDetail.Id,
                    employeeName: employeeName,
                    isSuccess: true,
                    message: message
                );

                _logger.LogInformation("Sent completion notification for promotion request {HRRequestDetailId}",
                    trackedRequestDetail.Id);
            }
            catch (Exception notifEx)
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Failed to send notification for promotion request {HRRequestDetailId}, but request completed successfully. Error: {Error}",
                    trackedRequestDetail.Id, notifEx.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee in Viewpoint for promotion request detail {HRRequestDetailId}",
                trackedRequestDetail.Id);

            var promoExceptionError = $"Exception: {ex.Message}";
            trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
            trackedRequestDetail.ViewpointErrorMessage = promoExceptionError;
            trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
            await context.SaveChangesAsync();
            await SendFailedRequestEmailAsync(context, trackedRequestDetail, promoExceptionError, submitterEmail);
        }
    }

    /// <summary>
    /// Updates the local Employee table after a successful Viewpoint status update.
    /// This keeps the local database in sync with Viewpoint for Layoff, Termination, and ReturnToWork requests.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="employeeNumber">The employee number to update</param>
    /// <param name="companyCode">The company code for the employee</param>
    /// <param name="requestType">The type of HR request (Layoff, Termination, ReturnToWork)</param>
    /// <param name="newStatus">The new employment status from Viewpoint</param>
    /// <param name="effectiveDate">The effective date of the change</param>
    /// <param name="terminationReasonCode">The termination reason code (only for Termination requests)</param>
    private async Task UpdateLocalEmployeeAfterViewpointSuccessAsync(
        MathyELMContext context,
        int employeeNumber,
        int? companyCode,
        Core.Enums.RequestType requestType,
        string? newStatus,
        DateTime? effectiveDate,
        string? terminationReasonCode = null)
    {
        try
        {
            _ecmLogger.LogInfo(LogCategory.ViewpointIntegration,
                "Updating local Employee table for employee {EmployeeNumber} after successful {RequestType} Viewpoint update. New status: {NewStatus}",
                employeeNumber, requestType, newStatus ?? "N/A");

            // Find the employee in the local database
            Core.Entities.Employee? employee;

            if (companyCode.HasValue)
            {
                // Try to find by employee number and company code first
                employee = await context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber &&
                                              e.CompanyCode == companyCode.Value &&
                                              !e.IsDeleted);
            }
            else
            {
                // Fall back to just employee number
                employee = await context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber && !e.IsDeleted);
            }

            if (employee == null)
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
                    "Employee {EmployeeNumber} not found in local database for {RequestType} update. Skipping local sync.",
                    employeeNumber, requestType);
                return;
            }

            var employeeFullName = $"{employee.FirstName} {employee.LastName}".Trim();

            // Update fields based on request type
            switch (requestType)
            {
                case Core.Enums.RequestType.Layoff:
                    employee.EmploymentStatus = newStatus;
                    employee.ViewpointSyncDate = DateTime.UtcNow;
                    _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                        "Updated Employee {EmployeeName} ({EmployeeNumber}) for Layoff: EmploymentStatus={Status}, ViewpointSyncDate={SyncDate}",
                        employeeFullName, employeeNumber, newStatus, employee.ViewpointSyncDate);
                    break;

                case Core.Enums.RequestType.Termination:
                    employee.EmploymentStatus = newStatus;
                    if (effectiveDate.HasValue)
                    {
                        employee.TerminationDate = effectiveDate.Value;
                    }
                    if (!string.IsNullOrEmpty(terminationReasonCode))
                    {
                        employee.TerminationReasonCode = terminationReasonCode;
                    }
                    employee.ViewpointSyncDate = DateTime.UtcNow;
                    _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                        "Updated Employee {EmployeeName} ({EmployeeNumber}) for Termination: EmploymentStatus={Status}, TerminationDate={TermDate}, TerminationReasonCode={ReasonCode}, ViewpointSyncDate={SyncDate}",
                        employeeFullName, employeeNumber, newStatus, employee.TerminationDate, employee.TerminationReasonCode, employee.ViewpointSyncDate);
                    break;

                case Core.Enums.RequestType.ReturnToWork:
                    employee.EmploymentStatus = newStatus;
                    if (effectiveDate.HasValue)
                    {
                        employee.ReturnToWorkDate = effectiveDate.Value;
                    }
                    employee.ViewpointSyncDate = DateTime.UtcNow;
                    _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                        "Updated Employee {EmployeeName} ({EmployeeNumber}) for ReturnToWork: EmploymentStatus={Status}, ReturnToWorkDate={RtwDate}, ViewpointSyncDate={SyncDate}",
                        employeeFullName, employeeNumber, newStatus, employee.ReturnToWorkDate, employee.ViewpointSyncDate);
                    break;

                default:
                    _logger.LogDebug("Request type {RequestType} does not require local Employee table update", requestType);
                    return;
            }

            await context.SaveChangesAsync();
            _ecmLogger.LogSuccess(LogCategory.ViewpointIntegration,
                "Successfully updated local Employee table for employee {EmployeeName} ({EmployeeNumber}) after {RequestType} Viewpoint update",
                employeeFullName, employeeNumber, requestType);
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the overall request - Viewpoint update was already successful
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex,
                "Failed to update local Employee table for employee {EmployeeNumber} after {RequestType} Viewpoint update. Viewpoint update was successful, but local sync failed.",
                employeeNumber, requestType);
        }
    }

    /// <summary>
    /// Updates employee in Viewpoint for termination request
    /// Updates: Status, ActiveYN, TermDate, TermReason
    /// Also updates local Employee table after successful verification
    /// </summary>
    private async Task UpdateEmployeeForTerminationInViewPointAsync(MathyELMContext context, Core.Entities.HRRequestDetail requestDetail, string? submitterEmail)
    {
        var trackedRequestDetail = await context.HRRequestDetails.FindAsync(requestDetail.Id);
        if (trackedRequestDetail == null)
        {
            _ecmLogger.LogWarning(LogCategory.BackgroundJob, "HR request detail {HRRequestDetailId} not found in tracked context", requestDetail.Id);
            return;
        }

        try
        {
            _logger.LogInformation("Starting termination employee update for HR request detail {HRRequestDetailId}, Employee {EmployeeId}",
                trackedRequestDetail.Id, trackedRequestDetail.EmployeeId);

            // Get termination details to retrieve ReasonCode
            var terminationDetails = await context.TerminationRequestDetails
                .FirstOrDefaultAsync(td => td.RequestDetailId == trackedRequestDetail.Id);

            string? termReason = terminationDetails?.ReasonCode;
            DateTime? termDate = trackedRequestDetail.EffectiveDate;

            _logger.LogInformation("Termination details - TermDate: {TermDate}, TermReason: {TermReason}",
                termDate, termReason ?? "N/A");

            // Get employee from Viewpoint
            var employee = await _viewpointService.GetEmployeeByNumberAsync(trackedRequestDetail.EmployeeId.ToString());
            if (employee == null)
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
                    "Employee {EmployeeId} not found in Viewpoint for termination request {HRRequestDetailId}",
                    trackedRequestDetail.EmployeeId, trackedRequestDetail.Id);

                var termEmpNotFoundError = "Employee not found in Viewpoint";
                trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                trackedRequestDetail.ViewpointErrorMessage = termEmpNotFoundError;
                trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, trackedRequestDetail, termEmpNotFoundError, submitterEmail);
                return;
            }

            var employeeName = GetEmployeeDisplayName(employee);

            // Send SignalR notification for status update to Processing
            await _notificationService.SendHRRequestStatusUpdateAsync(
                "system",
                trackedRequestDetail.Id,
                Enums.RequestStatus.Processing.ToString(),
                employeeName,
                "Termination update is now in progress"
            );

            trackedRequestDetail.ViewpointProcessed = true;
            trackedRequestDetail.ViewpointProcessedDate = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Call ViewpointService to update employee for termination
            var updateResult = await _viewpointService.UpdateEmployeeForTerminationInViewPointAsync(employee, termDate, termReason);

            if (!updateResult.Success)
            {
                _logger.LogError("Failed to queue termination update in Viewpoint for employee {EmployeeId}. Error: {Error}",
                    trackedRequestDetail.EmployeeId, updateResult.ErrorMessage);

                var termUpdateError = updateResult.ErrorMessage ?? "Failed to update employee in Viewpoint";
                trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                trackedRequestDetail.ViewpointErrorMessage = termUpdateError;
                trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, trackedRequestDetail, termUpdateError, submitterEmail);

                await _notificationService.SendHRRequestCompletionNotificationAsync(
                    submitterEmail ?? "system",
                    trackedRequestDetail.Id,
                    employeeName,
                    false,
                    updateResult.ErrorMessage ?? "Failed to update employee in Viewpoint"
                );
                return;
            }

            _logger.LogInformation("Termination update queued successfully. ActionId: {ActionId}", updateResult.ActionId);

            // Verify the action was successfully processed in Viewpoint
            // Poll for up to 10 minutes (10 attempts with 1 minute intervals)
            ViewpointActionDetailResponseDto? verificationResult = null;
            int maxRetries = 10;
            int retryCount = 0;
            int delayMilliseconds = 60000; // 1 minute

            while (retryCount < maxRetries)
            {
                verificationResult = await _viewpointService.VerifyViewpointActionAsync(updateResult.ActionId);

                if (verificationResult == null)
                {
                    _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Verification attempt {Attempt}/{MaxRetries} failed - no response from Viewpoint for employee {EmployeeId}",
                        retryCount + 1, maxRetries, trackedRequestDetail.EmployeeId);

                    var termVerifyError = "Failed to get verification response from Viewpoint";
                    trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    trackedRequestDetail.ViewpointErrorMessage = termVerifyError;
                    trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    await SendFailedRequestEmailAsync(context, trackedRequestDetail, termVerifyError, submitterEmail);

                    await _notificationService.SendHRRequestCompletionNotificationAsync(
                        submitterEmail ?? "system",
                        trackedRequestDetail.Id,
                        employeeName,
                        false,
                        "Termination request failed: No verification response from Viewpoint"
                    );
                    return;
                }

                _logger.LogInformation("Verification attempt {Attempt}/{MaxRetries} - Status: {Status} for employee {EmployeeId}",
                    retryCount + 1, maxRetries, verificationResult.Status ?? "null", trackedRequestDetail.EmployeeId);

                // Check if action completed successfully
                if (string.Equals(verificationResult.Status, "Successful", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Termination action completed successfully for employee {EmployeeId}", trackedRequestDetail.EmployeeId);
                    break;
                }

                // Check if action failed
                if (string.Equals(verificationResult.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    var termFailedContext = verificationResult.ContextJson != null
                        ? JsonSerializer.Serialize(verificationResult.ContextJson)
                        : "null";
                    _ecmLogger.LogError(LogCategory.ViewpointIntegration,
                        "Termination action failed in Viewpoint for employee {EmployeeId}. ActionId={ActionId}, Status={Status}, Context={Context}",
                        trackedRequestDetail.EmployeeId, verificationResult.Id, verificationResult.Status, termFailedContext);

                    var termActionFailedError = "Viewpoint action failed";
                    trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    trackedRequestDetail.ViewpointErrorMessage = termActionFailedError;
                    trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    await SendFailedRequestEmailAsync(context, trackedRequestDetail, termActionFailedError, submitterEmail);

                    await _notificationService.SendHRRequestCompletionNotificationAsync(
                        submitterEmail ?? "system",
                        trackedRequestDetail.Id,
                        employeeName,
                        false,
                        "Termination request failed: Viewpoint action failed"
                    );
                    return;
                }

                // If status is still "Queued" or other intermediate status, wait and retry
                retryCount++;

                if (retryCount < maxRetries)
                {
                    _logger.LogInformation("Action still processing (Status: {Status}). Waiting {Delay} seconds before retry {Retry}/{MaxRetries}...",
                        verificationResult.Status, delayMilliseconds / 1000, retryCount + 1, maxRetries);

                    await Task.Delay(delayMilliseconds);
                }
            }

            // Check if we exhausted all retries without success
            if (retryCount >= maxRetries && !string.Equals(verificationResult?.Status, "Successful", StringComparison.OrdinalIgnoreCase))
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Verification timed out after {MaxRetries} attempts for employee {EmployeeId}. Final status: {Status}",
                    maxRetries, trackedRequestDetail.EmployeeId, verificationResult?.Status ?? "unknown");

                var termTimeoutError = $"Verification timed out after {maxRetries} attempts. Final status: {verificationResult?.Status ?? "unknown"}";
                trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                trackedRequestDetail.ViewpointErrorMessage = termTimeoutError;
                trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, trackedRequestDetail, termTimeoutError, submitterEmail);

                await _notificationService.SendHRRequestCompletionNotificationAsync(
                    submitterEmail ?? "system",
                    trackedRequestDetail.Id,
                    employeeName,
                    false,
                    $"Termination request failed: Verification timed out after {maxRetries} attempts"
                );
                return;
            }

            _logger.LogInformation("Verification successful for termination employee {EmployeeId}", trackedRequestDetail.EmployeeId);

            // Update the local Employee table to keep it in sync with Viewpoint
            await UpdateLocalEmployeeAfterViewpointSuccessAsync(
                context,
                trackedRequestDetail.EmployeeId,
                trackedRequestDetail.EmployeeCompanyCode,
                Core.Enums.RequestType.Termination,
                updateResult.ActualStatusUsed,
                trackedRequestDetail.EffectiveDate,
                termReason);

            // Set status to Completed
            trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Completed;
            trackedRequestDetail.ViewpointErrorMessage = null;
            trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Send success notification
            await _notificationService.SendHRRequestCompletionNotificationAsync(
                submitterEmail ?? "system",
                trackedRequestDetail.Id,
                employeeName,
                true,
                "Termination request completed successfully. Employee status, termination date, and reason updated in Viewpoint."
            );

            _logger.LogInformation("✅ Set termination HR request detail {HRRequestDetailId} status to Completed", trackedRequestDetail.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee in Viewpoint for termination request detail {HRRequestDetailId}",
                trackedRequestDetail.Id);

            var termExceptionError = $"Exception: {ex.Message}";
            trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
            trackedRequestDetail.ViewpointErrorMessage = termExceptionError;
            trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
            await context.SaveChangesAsync();
            await SendFailedRequestEmailAsync(context, trackedRequestDetail, termExceptionError, submitterEmail);
        }
    }

    /// <summary>
    /// Updates employee in Viewpoint for return to work request
    /// Updates: Status, ActiveYN, udReturntoworkdate (custom field)
    /// Also updates local Employee table after successful verification
    /// </summary>
    private async Task UpdateEmployeeForReturnToWorkInViewPointAsync(MathyELMContext context, Core.Entities.HRRequestDetail requestDetail, string? submitterEmail)
    {
        var trackedRequestDetail = await context.HRRequestDetails.FindAsync(requestDetail.Id);
        if (trackedRequestDetail == null)
        {
            _ecmLogger.LogWarning(LogCategory.BackgroundJob, "HR request detail {HRRequestDetailId} not found in tracked context", requestDetail.Id);
            return;
        }

        try
        {
            _logger.LogInformation("Starting return to work employee update for HR request detail {HRRequestDetailId}, Employee {EmployeeId}",
                trackedRequestDetail.Id, trackedRequestDetail.EmployeeId);

            DateTime? returnToWorkDate = trackedRequestDetail.EffectiveDate;

            _logger.LogInformation("Return to work details - ReturnToWorkDate: {ReturnToWorkDate}", returnToWorkDate);

            // Get employee from Viewpoint
            var employee = await _viewpointService.GetEmployeeByNumberAsync(trackedRequestDetail.EmployeeId.ToString());
            if (employee == null)
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
                    "Employee {EmployeeId} not found in Viewpoint for return to work request {HRRequestDetailId}",
                    trackedRequestDetail.EmployeeId, trackedRequestDetail.Id);

                var rtwEmpNotFoundError = "Employee not found in Viewpoint";
                trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                trackedRequestDetail.ViewpointErrorMessage = rtwEmpNotFoundError;
                trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, trackedRequestDetail, rtwEmpNotFoundError, submitterEmail);
                return;
            }

            var employeeName = GetEmployeeDisplayName(employee);

            // Send SignalR notification for status update to Processing
            await _notificationService.SendHRRequestStatusUpdateAsync(
                "system",
                trackedRequestDetail.Id,
                Enums.RequestStatus.Processing.ToString(),
                employeeName,
                "Return to work update is now in progress"
            );

            trackedRequestDetail.ViewpointProcessed = true;
            trackedRequestDetail.ViewpointProcessedDate = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Call ViewpointService to update employee for return to work
            var updateResult = await _viewpointService.UpdateEmployeeForReturnToWorkInViewPointAsync(employee, returnToWorkDate);

            if (!updateResult.Success)
            {
                _logger.LogError("Failed to queue return to work update in Viewpoint for employee {EmployeeId}. Error: {Error}",
                    trackedRequestDetail.EmployeeId, updateResult.ErrorMessage);

                var rtwUpdateError = updateResult.ErrorMessage ?? "Failed to update employee in Viewpoint";
                trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                trackedRequestDetail.ViewpointErrorMessage = rtwUpdateError;
                trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, trackedRequestDetail, rtwUpdateError, submitterEmail);

                await _notificationService.SendHRRequestCompletionNotificationAsync(
                    submitterEmail ?? "system",
                    trackedRequestDetail.Id,
                    employeeName,
                    false,
                    updateResult.ErrorMessage ?? "Failed to update employee in Viewpoint"
                );
                return;
            }

            _logger.LogInformation("Return to work update queued successfully. ActionId: {ActionId}", updateResult.ActionId);

            // Verify the action was successfully processed in Viewpoint
            // Poll for up to 10 minutes (10 attempts with 1 minute intervals)
            ViewpointActionDetailResponseDto? verificationResult = null;
            int maxRetries = 10;
            int retryCount = 0;
            int delayMilliseconds = 60000; // 1 minute

            while (retryCount < maxRetries)
            {
                verificationResult = await _viewpointService.VerifyViewpointActionAsync(updateResult.ActionId);

                if (verificationResult == null)
                {
                    _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Verification attempt {Attempt}/{MaxRetries} failed - no response from Viewpoint for employee {EmployeeId}",
                        retryCount + 1, maxRetries, trackedRequestDetail.EmployeeId);

                    var rtwVerifyError = "Failed to get verification response from Viewpoint";
                    trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    trackedRequestDetail.ViewpointErrorMessage = rtwVerifyError;
                    trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    await SendFailedRequestEmailAsync(context, trackedRequestDetail, rtwVerifyError, submitterEmail);

                    await _notificationService.SendHRRequestCompletionNotificationAsync(
                        submitterEmail ?? "system",
                        trackedRequestDetail.Id,
                        employeeName,
                        false,
                        "Return to work request failed: No verification response from Viewpoint"
                    );
                    return;
                }

                _logger.LogInformation("Verification attempt {Attempt}/{MaxRetries} - Status: {Status} for employee {EmployeeId}",
                    retryCount + 1, maxRetries, verificationResult.Status ?? "null", trackedRequestDetail.EmployeeId);

                // Check if action completed successfully
                if (string.Equals(verificationResult.Status, "Successful", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Return to work action completed successfully for employee {EmployeeId}", trackedRequestDetail.EmployeeId);
                    break;
                }

                // Check if action failed
                if (string.Equals(verificationResult.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    var rtwFailedContext = verificationResult.ContextJson != null
                        ? JsonSerializer.Serialize(verificationResult.ContextJson)
                        : "null";
                    _ecmLogger.LogError(LogCategory.ViewpointIntegration,
                        "Return to work action failed in Viewpoint for employee {EmployeeId}. ActionId={ActionId}, Status={Status}, Context={Context}",
                        trackedRequestDetail.EmployeeId, verificationResult.Id, verificationResult.Status, rtwFailedContext);

                    var rtwActionFailedError = "Viewpoint action failed";
                    trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                    trackedRequestDetail.ViewpointErrorMessage = rtwActionFailedError;
                    trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    await SendFailedRequestEmailAsync(context, trackedRequestDetail, rtwActionFailedError, submitterEmail);

                    await _notificationService.SendHRRequestCompletionNotificationAsync(
                        submitterEmail ?? "system",
                        trackedRequestDetail.Id,
                        employeeName,
                        false,
                        "Return to work request failed: Viewpoint action failed"
                    );
                    return;
                }

                // If status is still "Queued" or other intermediate status, wait and retry
                retryCount++;

                if (retryCount < maxRetries)
                {
                    _logger.LogInformation("Action still processing (Status: {Status}). Waiting {Delay} seconds before retry {Retry}/{MaxRetries}...",
                        verificationResult.Status, delayMilliseconds / 1000, retryCount + 1, maxRetries);

                    await Task.Delay(delayMilliseconds);
                }
            }

            // Check if we exhausted all retries without success
            if (retryCount >= maxRetries && !string.Equals(verificationResult?.Status, "Successful", StringComparison.OrdinalIgnoreCase))
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration, "Verification timed out after {MaxRetries} attempts for employee {EmployeeId}. Final status: {Status}",
                    maxRetries, trackedRequestDetail.EmployeeId, verificationResult?.Status ?? "unknown");

                var rtwTimeoutError = $"Verification timed out after {maxRetries} attempts. Final status: {verificationResult?.Status ?? "unknown"}";
                trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
                trackedRequestDetail.ViewpointErrorMessage = rtwTimeoutError;
                trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await SendFailedRequestEmailAsync(context, trackedRequestDetail, rtwTimeoutError, submitterEmail);

                await _notificationService.SendHRRequestCompletionNotificationAsync(
                    submitterEmail ?? "system",
                    trackedRequestDetail.Id,
                    employeeName,
                    false,
                    $"Return to work request failed: Verification timed out after {maxRetries} attempts"
                );
                return;
            }

            _logger.LogInformation("Verification successful for return to work employee {EmployeeId}", trackedRequestDetail.EmployeeId);

            // Update the local Employee table to keep it in sync with Viewpoint
            await UpdateLocalEmployeeAfterViewpointSuccessAsync(
                context,
                trackedRequestDetail.EmployeeId,
                trackedRequestDetail.EmployeeCompanyCode,
                Core.Enums.RequestType.ReturnToWork,
                updateResult.ActualStatusUsed,
                trackedRequestDetail.EffectiveDate);

            // Set status to Completed
            trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Completed;
            trackedRequestDetail.ViewpointErrorMessage = null;
            trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Send success notification
            await _notificationService.SendHRRequestCompletionNotificationAsync(
                submitterEmail ?? "system",
                trackedRequestDetail.Id,
                employeeName,
                true,
                "Return to work request completed successfully. Employee status and return to work date updated in Viewpoint."
            );

            _logger.LogInformation("✅ Set return to work HR request detail {HRRequestDetailId} status to Completed", trackedRequestDetail.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee in Viewpoint for return to work request detail {HRRequestDetailId}",
                trackedRequestDetail.Id);

            var rtwExceptionError = $"Exception: {ex.Message}";
            trackedRequestDetail.RequestStatusId = (int)Enums.RequestStatus.Failed;
            trackedRequestDetail.ViewpointErrorMessage = rtwExceptionError;
            trackedRequestDetail.ModifiedDate = DateTime.UtcNow;
            await context.SaveChangesAsync();
            await SendFailedRequestEmailAsync(context, trackedRequestDetail, rtwExceptionError, submitterEmail);
        }
    }

    #region Return to Work Scheduled Email Notifications

    /// <summary>
    /// Sets up a recurring daily job to process Return to Work email notifications
    /// </summary>
    public void SetupReturnToWorkEmailNotificationsJob()
    {
        _logger.LogInformation("Setting up recurring Return to Work email notification job");

        // Run every day at 12:00 AM (Midnight)
        RecurringJob.AddOrUpdate(
            "process-returntowork-email-notifications",
            () => ProcessReturnToWorkEmailNotificationsAsync(),
            Cron.Daily(0));

        _logger.LogInformation("Return to Work email notification job scheduled to run daily at 12:00 AM (midnight)");
    }

    /// <summary>
    /// Processes Return to Work email notifications based on EmailTemplate SubmissionFreq
    /// Schedules emails for delivery based on: TriggerDate = EffectiveDate + SubmissionFreq
    /// </summary>
    public async Task ProcessReturnToWorkEmailNotificationsAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IAzureServiceBusEmailService>();

        try
        {
            var today = DateTime.Today.Date;
            _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] Starting email notification processing for {Date:yyyy-MM-dd}", today);

            // Get all active RETURNTOWORK email templates with scheduled trigger type
            var emailTemplates = await context.EmailTemplates
                .Where(t => t.IsActive && !t.IsDeleted && t.TriggerType == "Scheduled" && t.RequestType == "RETURNTOWORK")
                .ToListAsync();

            if (!emailTemplates.Any())
            {
                _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] No active scheduled RETURNTOWORK email templates found");
                return;
            }

            _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] Processing {TemplateCount} email templates", emailTemplates.Count);

            int totalScheduled = 0;
            int totalFailed = 0;

            // Process each template
            foreach (var template in emailTemplates)
            {
                try
                {
                    // Get all submitted Return to Work requests with effective date
                    var returnToWorkRequests = await context.HRRequestDetails
                        .Include(rd => rd.ParentRequest)
                        .Include(rd => rd.ReturnToWorkDetails)
                        .Where(rd =>
                            rd.RequestTypeId == 4 && // Return to Work
                            (rd.RequestStatusId == 1 || rd.RequestStatusId == 2) && // Submitted/Pending or Processing
                            !rd.IsDeleted &&
                            rd.ParentRequest != null &&
                            rd.EffectiveDate.HasValue)
                        .ToListAsync();

                    _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] Template '{TemplateName}' (Id={TemplateId}, TriggerType={TriggerType}): Processing {RequestCount} requests",
                        template.TemplateName, template.Id, template.TriggerType, returnToWorkRequests.Count);

                    // Process each request for this template
                    foreach (var requestDetail in returnToWorkRequests)
                    {
                        try
                        {
                            var effectiveDate = requestDetail.EffectiveDate!.Value.Date;
                            var triggerDate = effectiveDate.AddDays(template.SubmissionFreq ?? 0);
                            var daysUntilTrigger = (triggerDate - today).Days;

                            _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] RequestDetailId {RequestDetailId} (ParentRequestId={ParentRequestId}): Status={Status}, EffectiveDate={EffectiveDate:yyyy-MM-dd}, SubmissionFreq={Freq}, TriggerDate={TriggerDate:yyyy-MM-dd}, DaysUntil={Days}",
                                requestDetail.Id, requestDetail.ParentRequestId, requestDetail.RequestStatusId, effectiveDate, template.SubmissionFreq ?? 0, triggerDate, daysUntilTrigger);

                            // Only schedule via Hangfire for all trigger dates
                            if (daysUntilTrigger >= 0)
                            {
                                // Use requestDetail.Id (HRRequestDetailId) for unique job per employee
                                var jobId = $"returntowork-email-{requestDetail.Id}-{template.Id}-{template.SubmissionFreq}";

                                _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] Processing job: {JobId} for {TriggerDate:yyyy-MM-dd} at 00:00", jobId, triggerDate);

                                string hangfireJobId;

                                if (triggerDate <= DateTime.UtcNow)
                                {
                                    // Date is in the past or now - enqueue immediately for immediate execution
                                    _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] TriggerDate {TriggerDate} is in the past (now is {CurrentTime}), enqueueing immediately for execution",
                                        triggerDate, DateTime.UtcNow);

                                    hangfireJobId = BackgroundJob.Enqueue(
                                        () => SendScheduledReturnToWorkEmailAsync(requestDetail.Id, template.Id));
                                }
                                else
                                {
                                    // Date is in the future - schedule normally
                                    _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] TriggerDate {TriggerDate} is in the future (now is {CurrentTime}), scheduling for later execution",
                                        triggerDate, DateTime.UtcNow);

                                    hangfireJobId = BackgroundJob.Schedule(
                                        () => SendScheduledReturnToWorkEmailAsync(requestDetail.Id, template.Id),
                                        triggerDate);
                                }

                                _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] Job queued with jobId: {JobId}, HangfireJobId: {HangfireJobId}", jobId, hangfireJobId);

                                totalScheduled++;
                            }
                        }
                        catch (Exception ex)
                        {
                            totalFailed++;
                            _logger.LogError(ex, "[RETURN TO WORK EMAIL NOTIFICATIONS] Error processing request {RequestId} for template '{TemplateName}'",
                                requestDetail.ParentRequestId, template.TemplateName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[RETURN TO WORK EMAIL NOTIFICATIONS] Error processing template '{TemplateName}'", template.TemplateName);
                }
            }

            _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] Processing completed. Scheduled: {Scheduled}, Failed: {Failed}",
                totalScheduled, totalFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RETURN TO WORK EMAIL NOTIFICATIONS] CRITICAL ERROR: Failed to process email notifications");
            throw;
        }
    }

    /// <summary>
    /// Sends a scheduled Return to Work email notification
    /// </summary>
    public async Task SendScheduledReturnToWorkEmailAsync(int hrRequestDetailId, int templateId)
    {
        _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] >>> STARTING SendScheduledReturnToWorkEmailAsync: HRRequestDetailId={DetailId}, TemplateId={TemplateId}",
            hrRequestDetailId, templateId);

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IAzureServiceBusEmailService>();
        var emailRecipientsService = scope.ServiceProvider.GetRequiredService<IEmailRecipientsService>();

        try
        {
            _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] >>> Scope created, fetching template and request data...");

            // Get the template
            var template = await context.EmailTemplates
                .Where(t => t.Id == templateId && t.IsActive && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (template == null)
            {
                var errorMsg = $"Template not found: {templateId}";
                _logger.LogError("[RETURN TO WORK EMAIL NOTIFICATIONS] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Get the specific request detail by ID (each employee has their own HRRequestDetailId)
            var requestDetail = await context.HRRequestDetails
                .Include(rd => rd.ParentRequest)
                .Include(rd => rd.ReturnToWorkDetails)
                .FirstOrDefaultAsync(rd => rd.Id == hrRequestDetailId && rd.RequestTypeId == 4);

            if (requestDetail == null)
            {
                var errorMsg = $"Return to Work request not found for HRRequestDetailId: {hrRequestDetailId}";
                _logger.LogError("[RETURN TO WORK EMAIL NOTIFICATIONS] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Note: Duplicate check is handled by Hangfire job ID uniqueness (job ID includes hrRequestDetailId)
            // This ensures each employee gets their own scheduled email without duplicates

            // Get employee data
            var employee = await context.Employees
                .Where(e => e.EmployeeNumber == requestDetail.EmployeeId && !e.IsDeleted)
                .FirstOrDefaultAsync();

            // Get manager email from supervisor
            string? managerEmail = null;
            if (employee?.SupervisorId != null)
            {
                var supervisor = await context.Employees
                    .Where(e => e.EmployeeNumber == employee.SupervisorId && !e.IsDeleted)
                    .FirstOrDefaultAsync();
                managerEmail = supervisor?.WorkEmail;
            }

            // Build email data DTO
            var emailData = new ReturnToWorkEmailDataDto
            {
                EmployeeId = requestDetail.EmployeeId,
                CompanyCode = requestDetail.EmployeeCompanyCode,
                DeptCode = requestDetail.EmployeeDepartmentCode,
                EffectiveDate = requestDetail.EffectiveDate,
                Notes = requestDetail.ParentRequest?.Notes,
                Submitter = null, // Will be looked up by EmailFieldMapperService using SubmitterEmail
                ManagerEmail = managerEmail
            };

            // Get recipients from template
            var recipients = await emailRecipientsService.GetRecipientsFromTemplateAsync(
                template.TemplateName,
                emailData.CompanyCode,
                emailData.DeptCode ?? 0,
                managerEmail,
                requestDetail.ParentRequest?.SubmitterEmail,
                null,
                "RETURNTOWORK"
            );

            if (recipients == null || !recipients.Any())
            {
                _ecmLogger.LogWarning(LogCategory.EmailNotification, "[RETURN TO WORK EMAIL NOTIFICATIONS] No recipients found for template '{TemplateName}' for HRRequestDetailId={DetailId}",
                    template.TemplateName, hrRequestDetailId);
                return;
            }

            var toEmail = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

            _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] Sending email to: {ToEmail}", toEmail);

            // Send the email (use ParentRequestId for email service tracking)
            var result = await emailService.SendEmailFromTemplateNameForReturnToWorkAsync(
                template.TemplateName,
                emailData,
                toEmail,
                null,
                requestDetail.ParentRequestId
            );

            if (result.Success)
            {
                _logger.LogInformation("[RETURN TO WORK EMAIL NOTIFICATIONS] ✅ Email sent successfully for HRRequestDetailId={DetailId}, EmployeeId={EmployeeId} using template '{TemplateName}'",
                    hrRequestDetailId, requestDetail.EmployeeId, template.TemplateName);
            }
            else
            {
                _logger.LogError("[RETURN TO WORK EMAIL NOTIFICATIONS] ❌ Failed to send email for HRRequestDetailId={DetailId}: {Message}",
                    hrRequestDetailId, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RETURN TO WORK EMAIL NOTIFICATIONS] Error sending scheduled email for HRRequestDetailId={DetailId}, template {TemplateId}",
                hrRequestDetailId, templateId);
            throw;
        }
    }

    /// <summary>
    /// Immediately triggers scheduled emails for a Return to Work request if their trigger dates have already passed.
    /// This method is called when a Return to Work request is created/updated to check if any emails should be sent
    /// immediately instead of waiting for the daily ProcessReturnToWorkEmailNotificationsAsync job.
    /// </summary>
    public async Task TriggerOverdueScheduledEmailsForReturnToWorkAsync(int hrRequestDetailId)
    {
        _logger.LogInformation("[RTW IMMEDIATE TRIGGER] Starting check for overdue scheduled emails: HRRequestDetailId={DetailId}", hrRequestDetailId);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();

            // Get the specific Return to Work request detail by ID (each employee has their own HRRequestDetailId)
            var requestDetail = await context.HRRequestDetails
                .Include(rd => rd.ParentRequest)
                .Include(rd => rd.ReturnToWorkDetails)
                .FirstOrDefaultAsync(rd => rd.Id == hrRequestDetailId && rd.RequestTypeId == 4);

            if (requestDetail?.EffectiveDate == null)
            {
                _logger.LogInformation("[RTW IMMEDIATE TRIGGER] Return to Work request or EffectiveDate not found for HRRequestDetailId={DetailId}", hrRequestDetailId);
                return;
            }

            var today = DateTime.UtcNow.Date;
            var effectiveDate = requestDetail.EffectiveDate.Value.Date;

            _logger.LogInformation("[RTW IMMEDIATE TRIGGER] Processing Return to Work request: HRRequestDetailId={DetailId}, EmployeeId={EmployeeId}, EffectiveDate={EffectiveDate}, Today={Today}",
                hrRequestDetailId, requestDetail.EmployeeId, effectiveDate, today);

            // Get ALL email templates for Return to Work with Scheduled trigger type
            var templates = await context.EmailTemplates
                .Where(t => t.RequestType == "RETURNTOWORK" && t.TriggerType == "Scheduled" && t.IsActive && !t.IsDeleted)
                .ToListAsync();

            if (!templates.Any())
            {
                _logger.LogInformation("[RTW IMMEDIATE TRIGGER] No active Return to Work scheduled email templates found");
                return;
            }

            foreach (var template in templates)
            {
                try
                {
                    // Calculate trigger date (works with positive and negative SubmissionFreq)
                    var triggerDate = effectiveDate.AddDays(template.SubmissionFreq ?? 0);

                    _logger.LogInformation("[RTW IMMEDIATE TRIGGER] Checking template '{TemplateName}' (Id={TemplateId}): TriggerDate={TriggerDate}, Today={Today}, SubmissionFreq={Freq}",
                        template.TemplateName, template.Id, triggerDate, today, template.SubmissionFreq ?? 0);

                    // If trigger date is today or in the past, immediately enqueue
                    if (triggerDate <= today)
                    {
                        // Note: Duplicate check is handled by Hangfire job ID uniqueness
                        // The job ID includes hrRequestDetailId which ensures each employee gets their own email

                        // Enqueue immediately using HRRequestDetailId (for specific employee)
                        var jobId = BackgroundJob.Enqueue(
                            () => SendScheduledReturnToWorkEmailAsync(hrRequestDetailId, template.Id));

                        _logger.LogInformation("[RTW IMMEDIATE TRIGGER] ✅ ENQUEUED IMMEDIATELY: HRRequestDetailId={DetailId}, EmployeeId={EmployeeId}, Template='{TemplateName}' (Id={TemplateId}), TriggerDate={TriggerDate} (SubmissionFreq={Freq}), JobId={JobId}",
                            hrRequestDetailId, requestDetail.EmployeeId, template.TemplateName, template.Id, triggerDate, template.SubmissionFreq ?? 0, jobId);
                    }
                    else
                    {
                        var daysUntil = (triggerDate - today).Days;
                        _logger.LogInformation("[RTW IMMEDIATE TRIGGER] Template '{TemplateName}' not yet due: TriggerDate={TriggerDate} ({DaysUntil} days from now)",
                            template.TemplateName, triggerDate, daysUntil);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[RTW IMMEDIATE TRIGGER] Error processing template '{TemplateName}' (Id={TemplateId}) for HRRequestDetailId={DetailId}",
                        template.TemplateName, template.Id, hrRequestDetailId);
                    // Continue processing other templates even if one fails
                }
            }

            _logger.LogInformation("[RTW IMMEDIATE TRIGGER] Completed checking overdue scheduled emails for HRRequestDetailId={DetailId}", hrRequestDetailId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RTW IMMEDIATE TRIGGER] Error in TriggerOverdueScheduledEmailsForReturnToWorkAsync for HRRequestDetailId={DetailId}", hrRequestDetailId);
            // Don't throw - this is a best-effort operation and shouldn't block the request save
        }
    }

    #endregion

    #region Layoff Scheduled Email Notifications

    /// <summary>
    /// Sets up a recurring daily job to process Layoff scheduled email notifications
    /// </summary>
    public void SetupLayoffEmailNotificationsJob()
    {
        _logger.LogInformation("Setting up recurring Layoff email notification job");

        // Run every day at 12:00 AM (Midnight)
        RecurringJob.AddOrUpdate(
            "process-layoff-email-notifications",
            () => ProcessLayoffEmailNotificationsAsync(),
            Cron.Daily(0));

        _logger.LogInformation("Layoff email notification job registered successfully - will run daily at 12:00 AM (Midnight)");
    }

    /// <summary>
    /// Processes Layoff email notifications based on EmailTemplate SubmissionFreq
    /// Schedules all emails via Hangfire based on: TriggerDate = LastDayWorked + SubmissionFreq
    /// </summary>
    public async Task ProcessLayoffEmailNotificationsAsync()
    {
        _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] Starting scheduled email notification processing");

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();

            var today = DateTime.UtcNow.Date;

            // Get all Layoff email templates with Scheduled trigger type
            var templates = await context.EmailTemplates
                .Where(t => t.RequestType == "LAYOFF" && t.TriggerType == "Scheduled" && t.IsActive && !t.IsDeleted)
                .ToListAsync();

            if (!templates.Any())
            {
                _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] No active Layoff scheduled email templates found");
                return;
            }

            _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] Found {TemplateCount} Layoff scheduled email templates to process", templates.Count);

            int totalScheduled = 0;
            int totalFailed = 0;

            foreach (var template in templates)
            {
                try
                {
                    _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] Processing template '{TemplateName}' (Id={TemplateId}, TriggerType={TriggerType}, SubmissionFreq={Freq})",
                        template.TemplateName, template.Id, template.TriggerType, template.SubmissionFreq ?? 0);

                    // Get all submitted Layoff requests with LayoffDetails
                    // Note: Layoff uses LayoffDetails.LastDayWorked instead of HRRequestDetail.EffectiveDate
                    var layoffRequests = await context.HRRequestDetails
                        .Include(rd => rd.ParentRequest)
                        .Include(rd => rd.LayoffDetails)
                        .Where(rd =>
                            rd.RequestTypeId == 2 && // Layoff (RequestType.Layoff = 2)
                            (rd.RequestStatusId == 1 || rd.RequestStatusId == 2) && // Submitted/Pending or Processing
                            !rd.IsDeleted &&
                            rd.ParentRequest != null &&
                            rd.LayoffDetails != null)
                        .ToListAsync();

                    _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] Template '{TemplateName}' (Id={TemplateId}, TriggerType={TriggerType}): Processing {RequestCount} requests",
                        template.TemplateName, template.Id, template.TriggerType, layoffRequests.Count);

                    // Process each request for this template
                    foreach (var requestDetail in layoffRequests)
                    {
                        try
                        {
                            // Note: Layoff uses LayoffDetails.LastDayWorked as the base date for email scheduling
                            var lastDayWorked = requestDetail.LayoffDetails!.LastDayWorked.Date;
                            var triggerDate = lastDayWorked.AddDays(template.SubmissionFreq ?? 0);
                            var daysUntilTrigger = (triggerDate - today).Days;

                            _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] RequestDetailId {RequestDetailId} (ParentRequestId={ParentRequestId}): Status={Status}, LastDayWorked={LastDayWorked:yyyy-MM-dd}, SubmissionFreq={Freq}, TriggerDate={TriggerDate:yyyy-MM-dd}, DaysUntil={Days}",
                                requestDetail.Id, requestDetail.ParentRequestId, requestDetail.RequestStatusId, lastDayWorked, template.SubmissionFreq ?? 0, triggerDate, daysUntilTrigger);

                            // Only schedule via Hangfire for all trigger dates
                            if (daysUntilTrigger >= 0)
                            {
                                // Use requestDetail.Id (HRRequestDetailId) for unique job per employee
                                var jobId = $"layoff-email-{requestDetail.Id}-{template.Id}-{template.SubmissionFreq}";

                                _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] Processing job: {JobId} for {TriggerDate:yyyy-MM-dd} at 00:00", jobId, triggerDate);

                                string hangfireJobId;

                                if (triggerDate <= DateTime.UtcNow)
                                {
                                    // Date is in the past or now - enqueue immediately for immediate execution
                                    _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] TriggerDate {TriggerDate} is in the past (now is {CurrentTime}), enqueueing immediately for execution",
                                        triggerDate, DateTime.UtcNow);

                                    hangfireJobId = BackgroundJob.Enqueue(
                                        () => SendScheduledLayoffEmailAsync(requestDetail.Id, template.Id));
                                }
                                else
                                {
                                    // Date is in the future - schedule normally
                                    _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] TriggerDate {TriggerDate} is in the future (now is {CurrentTime}), scheduling for later execution",
                                        triggerDate, DateTime.UtcNow);

                                    hangfireJobId = BackgroundJob.Schedule(
                                        () => SendScheduledLayoffEmailAsync(requestDetail.Id, template.Id),
                                        triggerDate);
                                }

                                _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] Job queued with jobId: {JobId}, HangfireJobId: {HangfireJobId}", jobId, hangfireJobId);

                                totalScheduled++;
                            }
                        }
                        catch (Exception ex)
                        {
                            totalFailed++;
                            _logger.LogError(ex, "[LAYOFF EMAIL NOTIFICATIONS] Error processing request {RequestId} for template '{TemplateName}'",
                                requestDetail.ParentRequestId, template.TemplateName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[LAYOFF EMAIL NOTIFICATIONS] Error processing template '{TemplateName}'", template.TemplateName);
                }
            }

            _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] Processing completed. Scheduled: {Scheduled}, Failed: {Failed}",
                totalScheduled, totalFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LAYOFF EMAIL NOTIFICATIONS] CRITICAL ERROR: Failed to process email notifications");
            throw;
        }
    }

    /// <summary>
    /// Sends a scheduled Layoff email notification
    /// </summary>
    public async Task SendScheduledLayoffEmailAsync(int hrRequestDetailId, int templateId)
    {
        _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] >>> STARTING SendScheduledLayoffEmailAsync: HRRequestDetailId={DetailId}, TemplateId={TemplateId}",
            hrRequestDetailId, templateId);

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IAzureServiceBusEmailService>();
        var emailRecipientsService = scope.ServiceProvider.GetRequiredService<IEmailRecipientsService>();

        try
        {
            _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] >>> Scope created, fetching template and request data...");

            // Get the template
            var template = await context.EmailTemplates
                .Where(t => t.Id == templateId && t.IsActive && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (template == null)
            {
                var errorMsg = $"Template not found: {templateId}";
                _logger.LogError("[LAYOFF EMAIL NOTIFICATIONS] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Get the specific request detail by ID (each employee has their own HRRequestDetailId)
            var requestDetail = await context.HRRequestDetails
                .Include(rd => rd.ParentRequest)
                .Include(rd => rd.LayoffDetails)
                .FirstOrDefaultAsync(rd => rd.Id == hrRequestDetailId && rd.RequestTypeId == 2); // Layoff (RequestType.Layoff = 2)

            if (requestDetail == null)
            {
                var errorMsg = $"Layoff request not found for HRRequestDetailId: {hrRequestDetailId}";
                _logger.LogError("[LAYOFF EMAIL NOTIFICATIONS] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Note: Duplicate check is handled by Hangfire job ID uniqueness (job ID includes hrRequestDetailId)
            // This ensures each employee gets their own scheduled email without duplicates

            // Get employee data
            var employee = await context.Employees
                .Where(e => e.EmployeeNumber == requestDetail.EmployeeId && !e.IsDeleted)
                .FirstOrDefaultAsync();

            // Get manager email from supervisor
            string? managerEmail = null;
            if (employee?.SupervisorId != null)
            {
                var supervisor = await context.Employees
                    .Where(e => e.EmployeeNumber == employee.SupervisorId && !e.IsDeleted)
                    .FirstOrDefaultAsync();
                managerEmail = supervisor?.WorkEmail;
            }

            // Build email data DTO
            // Note: Layoff uses LayoffDetails.LastDayWorked as the effective date
            var emailData = new LayoffEmailDataDto
            {
                EmployeeId = requestDetail.EmployeeId,
                CompanyCode = requestDetail.EmployeeCompanyCode,
                DeptCode = requestDetail.EmployeeDepartmentCode,
                EffectiveDate = requestDetail.LayoffDetails?.LastDayWorked,
                Notes = requestDetail.ParentRequest?.Notes,
                Submitter = null, // Will be looked up by EmailFieldMapperService using SubmitterEmail
                ManagerEmail = managerEmail
            };

            // Get recipients from template
            var recipients = await emailRecipientsService.GetRecipientsFromTemplateAsync(
                template.TemplateName,
                emailData.CompanyCode,
                emailData.DeptCode ?? 0,
                managerEmail,
                requestDetail.ParentRequest?.SubmitterEmail,
                null,
                "LAYOFF"
            );

            if (recipients == null || !recipients.Any())
            {
                _ecmLogger.LogWarning(LogCategory.EmailNotification, "[LAYOFF EMAIL NOTIFICATIONS] No recipients found for template '{TemplateName}' for HRRequestDetailId={DetailId}",
                    template.TemplateName, hrRequestDetailId);
                return;
            }

            var toEmail = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

            _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] Sending email to: {ToEmail}", toEmail);

            // Send the email (use ParentRequestId for email service tracking)
            var result = await emailService.SendEmailFromTemplateNameForLayoffAsync(
                template.TemplateName,
                emailData,
                toEmail,
                null,
                requestDetail.ParentRequestId
            );

            if (result.Success)
            {
                _logger.LogInformation("[LAYOFF EMAIL NOTIFICATIONS] ✅ Email sent successfully for HRRequestDetailId={DetailId}, EmployeeId={EmployeeId} using template '{TemplateName}'",
                    hrRequestDetailId, requestDetail.EmployeeId, template.TemplateName);
            }
            else
            {
                _logger.LogError("[LAYOFF EMAIL NOTIFICATIONS] ❌ Failed to send email for HRRequestDetailId={DetailId}: {Message}",
                    hrRequestDetailId, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LAYOFF EMAIL NOTIFICATIONS] Error sending scheduled email for HRRequestDetailId={DetailId}, template {TemplateId}",
                hrRequestDetailId, templateId);
            throw;
        }
    }

    /// <summary>
    /// Immediately triggers scheduled emails for a Layoff request if their trigger dates have already passed.
    /// This method is called when a Layoff request is created/updated to check if any emails should be sent
    /// immediately instead of waiting for the daily ProcessLayoffEmailNotificationsAsync job.
    /// </summary>
    public async Task TriggerOverdueScheduledEmailsForLayoffAsync(int hrRequestDetailId)
    {
        _logger.LogInformation("[LAYOFF IMMEDIATE TRIGGER] Starting check for overdue scheduled emails: HRRequestDetailId={DetailId}", hrRequestDetailId);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();

            // Get the specific Layoff request detail by ID (each employee has their own HRRequestDetailId)
            var requestDetail = await context.HRRequestDetails
                .Include(rd => rd.ParentRequest)
                .Include(rd => rd.LayoffDetails)
                .FirstOrDefaultAsync(rd => rd.Id == hrRequestDetailId && rd.RequestTypeId == 2); // Layoff (RequestType.Layoff = 2)

            // Note: Layoff uses LayoffDetails.LastDayWorked as the base date for email scheduling
            if (requestDetail?.LayoffDetails?.LastDayWorked == null)
            {
                _logger.LogInformation("[LAYOFF IMMEDIATE TRIGGER] Layoff request or LastDayWorked not found for HRRequestDetailId={DetailId}", hrRequestDetailId);
                return;
            }

            var today = DateTime.UtcNow.Date;
            var lastDayWorked = requestDetail.LayoffDetails.LastDayWorked.Date;

            _logger.LogInformation("[LAYOFF IMMEDIATE TRIGGER] Processing Layoff request: HRRequestDetailId={DetailId}, EmployeeId={EmployeeId}, LastDayWorked={LastDayWorked}, Today={Today}",
                hrRequestDetailId, requestDetail.EmployeeId, lastDayWorked, today);

            // Get ALL email templates for Layoff with Scheduled trigger type
            var templates = await context.EmailTemplates
                .Where(t => t.RequestType == "LAYOFF" && t.TriggerType == "Scheduled" && t.IsActive && !t.IsDeleted)
                .ToListAsync();

            if (!templates.Any())
            {
                _logger.LogInformation("[LAYOFF IMMEDIATE TRIGGER] No active Layoff scheduled email templates found");
                return;
            }

            foreach (var template in templates)
            {
                try
                {
                    // Calculate trigger date (works with positive and negative SubmissionFreq)
                    var triggerDate = lastDayWorked.AddDays(template.SubmissionFreq ?? 0);

                    _logger.LogInformation("[LAYOFF IMMEDIATE TRIGGER] Checking template '{TemplateName}' (Id={TemplateId}): TriggerDate={TriggerDate}, Today={Today}, SubmissionFreq={Freq}",
                        template.TemplateName, template.Id, triggerDate, today, template.SubmissionFreq ?? 0);

                    // If trigger date is today or in the past, immediately enqueue
                    if (triggerDate <= today)
                    {
                        // Note: Duplicate check is handled by Hangfire job ID uniqueness
                        // The job ID includes hrRequestDetailId which ensures each employee gets their own email

                        // Enqueue immediately using HRRequestDetailId (for specific employee)
                        var jobId = BackgroundJob.Enqueue(
                            () => SendScheduledLayoffEmailAsync(hrRequestDetailId, template.Id));

                        _logger.LogInformation("[LAYOFF IMMEDIATE TRIGGER] ✅ ENQUEUED IMMEDIATELY: HRRequestDetailId={DetailId}, EmployeeId={EmployeeId}, Template='{TemplateName}' (Id={TemplateId}), TriggerDate={TriggerDate} (SubmissionFreq={Freq}), JobId={JobId}",
                            hrRequestDetailId, requestDetail.EmployeeId, template.TemplateName, template.Id, triggerDate, template.SubmissionFreq ?? 0, jobId);
                    }
                    else
                    {
                        var daysUntil = (triggerDate - today).Days;
                        _logger.LogInformation("[LAYOFF IMMEDIATE TRIGGER] Template '{TemplateName}' not yet due: TriggerDate={TriggerDate} ({DaysUntil} days from now)",
                            template.TemplateName, triggerDate, daysUntil);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[LAYOFF IMMEDIATE TRIGGER] Error processing template '{TemplateName}' (Id={TemplateId}) for HRRequestDetailId={DetailId}",
                        template.TemplateName, template.Id, hrRequestDetailId);
                    // Continue processing other templates even if one fails
                }
            }

            _logger.LogInformation("[LAYOFF IMMEDIATE TRIGGER] Completed checking overdue scheduled emails for HRRequestDetailId={DetailId}", hrRequestDetailId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LAYOFF IMMEDIATE TRIGGER] Error in TriggerOverdueScheduledEmailsForLayoffAsync for HRRequestDetailId={DetailId}", hrRequestDetailId);
            // Don't throw - this is a best-effort operation and shouldn't block the request save
        }
    }

    #endregion

    #region Termination Scheduled Email Notifications

    /// <summary>
    /// Sets up a recurring daily job to process Termination scheduled email notifications
    /// </summary>
    public void SetupTerminationEmailNotificationsJob()
    {
        _logger.LogInformation("Setting up recurring Termination email notification job");

        // Run every day at 12:00 AM (Midnight)
        RecurringJob.AddOrUpdate(
            "process-termination-email-notifications",
            () => ProcessTerminationEmailNotificationsAsync(),
            Cron.Daily(0));

        _logger.LogInformation("Termination email notification job registered successfully - will run daily at 12:00 AM (Midnight)");
    }

    /// <summary>
    /// Processes Termination email notifications based on EmailTemplate SubmissionFreq
    /// Schedules all emails via Hangfire based on: TriggerDate = EffectiveDate + SubmissionFreq
    /// </summary>
    public async Task ProcessTerminationEmailNotificationsAsync()
    {
        _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] Starting scheduled email notification processing");

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();

            var today = DateTime.UtcNow.Date;
            var totalScheduled = 0;
            var totalFailed = 0;

            // Get all active TERMINATION templates with Scheduled trigger type
            var templates = await context.EmailTemplates
                .Where(t => t.RequestType == "TERMINATION" && t.TriggerType == "Scheduled" && t.IsActive && !t.IsDeleted)
                .ToListAsync();

            if (!templates.Any())
            {
                _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] No active scheduled TERMINATION templates found");
                return;
            }

            _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] Found {Count} active scheduled TERMINATION templates", templates.Count);

            foreach (var template in templates)
            {
                try
                {
                    // Get all active termination requests
                    var terminationRequests = await context.HRRequestDetails
                        .Include(rd => rd.ParentRequest)
                        .Include(rd => rd.TerminationDetails)
                        .Where(rd => rd.RequestTypeId == 3 && !rd.IsDeleted) // Termination (RequestType.Termination = 3)
                        .ToListAsync();

                    _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] Template '{TemplateName}': Found {Count} active termination requests",
                        template.TemplateName, terminationRequests.Count);

                    foreach (var requestDetail in terminationRequests)
                    {
                        try
                        {
                            if (requestDetail.EffectiveDate == null)
                            {
                                _ecmLogger.LogWarning(LogCategory.EmailNotification, "[TERMINATION EMAIL NOTIFICATIONS] Skipping request {DetailId} - EffectiveDate is null", requestDetail.Id);
                                continue;
                            }

                            var effectiveDate = requestDetail.EffectiveDate.Value.Date;
                            var triggerDate = effectiveDate.AddDays(template.SubmissionFreq ?? 0);
                            var daysUntilTrigger = (triggerDate - today).Days;

                            _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] RequestDetailId {RequestDetailId} (ParentRequestId={ParentRequestId}): Status={Status}, EffectiveDate={EffectiveDate:yyyy-MM-dd}, SubmissionFreq={Freq}, TriggerDate={TriggerDate:yyyy-MM-dd}, DaysUntil={Days}",
                                requestDetail.Id, requestDetail.ParentRequestId, requestDetail.RequestStatusId, effectiveDate, template.SubmissionFreq ?? 0, triggerDate, daysUntilTrigger);

                            // Only schedule via Hangfire for all trigger dates
                            if (daysUntilTrigger >= 0)
                            {
                                // Use requestDetail.Id (HRRequestDetailId) for unique job per employee
                                var jobId = $"termination-email-{requestDetail.Id}-{template.Id}-{template.SubmissionFreq}";

                                _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] Processing job: {JobId} for {TriggerDate:yyyy-MM-dd} at 00:00", jobId, triggerDate);

                                string hangfireJobId;

                                if (triggerDate <= DateTime.UtcNow)
                                {
                                    // Date is in the past or now - enqueue immediately for immediate execution
                                    _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] TriggerDate {TriggerDate} is in the past (now is {CurrentTime}), enqueueing immediately for execution",
                                        triggerDate, DateTime.UtcNow);

                                    hangfireJobId = BackgroundJob.Enqueue(
                                        () => SendScheduledTerminationEmailAsync(requestDetail.Id, template.Id));
                                }
                                else
                                {
                                    // Date is in the future - schedule normally
                                    _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] TriggerDate {TriggerDate} is in the future (now is {CurrentTime}), scheduling for later execution",
                                        triggerDate, DateTime.UtcNow);

                                    hangfireJobId = BackgroundJob.Schedule(
                                        () => SendScheduledTerminationEmailAsync(requestDetail.Id, template.Id),
                                        triggerDate);
                                }

                                _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] Job queued with jobId: {JobId}, HangfireJobId: {HangfireJobId}", jobId, hangfireJobId);

                                totalScheduled++;
                            }
                        }
                        catch (Exception ex)
                        {
                            totalFailed++;
                            _logger.LogError(ex, "[TERMINATION EMAIL NOTIFICATIONS] Error processing request {RequestId} for template '{TemplateName}'",
                                requestDetail.ParentRequestId, template.TemplateName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TERMINATION EMAIL NOTIFICATIONS] Error processing template '{TemplateName}'", template.TemplateName);
                }
            }

            _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] Processing completed. Scheduled: {Scheduled}, Failed: {Failed}",
                totalScheduled, totalFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TERMINATION EMAIL NOTIFICATIONS] CRITICAL ERROR: Failed to process email notifications");
            throw;
        }
    }

    /// <summary>
    /// Sends a scheduled Termination email notification
    /// </summary>
    public async Task SendScheduledTerminationEmailAsync(int hrRequestDetailId, int templateId)
    {
        _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] >>> STARTING SendScheduledTerminationEmailAsync: HRRequestDetailId={DetailId}, TemplateId={TemplateId}",
            hrRequestDetailId, templateId);

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IAzureServiceBusEmailService>();
        var emailRecipientsService = scope.ServiceProvider.GetRequiredService<IEmailRecipientsService>();

        try
        {
            _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] >>> Scope created, fetching template and request data...");

            // Get the template
            var template = await context.EmailTemplates
                .Where(t => t.Id == templateId && t.IsActive && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (template == null)
            {
                var errorMsg = $"Template not found: {templateId}";
                _logger.LogError("[TERMINATION EMAIL NOTIFICATIONS] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Get the specific request detail by ID (each employee has their own HRRequestDetailId)
            var requestDetail = await context.HRRequestDetails
                .Include(rd => rd.ParentRequest)
                .Include(rd => rd.TerminationDetails)
                .FirstOrDefaultAsync(rd => rd.Id == hrRequestDetailId && rd.RequestTypeId == 3); // Termination (RequestType.Termination = 3)

            if (requestDetail == null)
            {
                var errorMsg = $"Termination request not found for HRRequestDetailId: {hrRequestDetailId}";
                _logger.LogError("[TERMINATION EMAIL NOTIFICATIONS] {Error}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Get employee data
            var employee = await context.Employees
                .Where(e => e.EmployeeNumber == requestDetail.EmployeeId && !e.IsDeleted)
                .FirstOrDefaultAsync();

            // Get manager email from supervisor
            string? managerEmail = null;
            if (employee?.SupervisorId != null)
            {
                var supervisor = await context.Employees
                    .Where(e => e.EmployeeNumber == employee.SupervisorId && !e.IsDeleted)
                    .FirstOrDefaultAsync();
                managerEmail = supervisor?.WorkEmail;
            }

            // Build email data DTO
            var emailData = new TerminationEmailDataDto
            {
                EmployeeId = requestDetail.EmployeeId,
                CompanyCode = requestDetail.EmployeeCompanyCode,
                DeptCode = requestDetail.EmployeeDepartmentCode,
                EffectiveDate = requestDetail.EffectiveDate,
                Notes = requestDetail.ParentRequest?.Notes,
                Submitter = null, // Will be looked up by EmailFieldMapperService using SubmitterEmail
                ManagerEmail = managerEmail
            };

            // Get recipients from template
            var recipients = await emailRecipientsService.GetRecipientsFromTemplateAsync(
                template.TemplateName,
                emailData.CompanyCode,
                emailData.DeptCode ?? 0,
                managerEmail,
                requestDetail.ParentRequest?.SubmitterEmail,
                null,
                "TERMINATION"
            );

            if (recipients == null || !recipients.Any())
            {
                _ecmLogger.LogWarning(LogCategory.EmailNotification, "[TERMINATION EMAIL NOTIFICATIONS] No recipients found for template '{TemplateName}' for HRRequestDetailId={DetailId}",
                    template.TemplateName, hrRequestDetailId);
                return;
            }

            var toEmail = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

            _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] Sending email to: {ToEmail}", toEmail);

            // Send the email (use ParentRequestId for email service tracking)
            var result = await emailService.SendEmailFromTemplateNameForTerminationAsync(
                template.TemplateName,
                emailData,
                toEmail,
                null,
                requestDetail.ParentRequestId
            );

            if (result.Success)
            {
                _logger.LogInformation("[TERMINATION EMAIL NOTIFICATIONS] ✅ Email sent successfully for HRRequestDetailId={DetailId}, EmployeeId={EmployeeId} using template '{TemplateName}'",
                    hrRequestDetailId, requestDetail.EmployeeId, template.TemplateName);
            }
            else
            {
                _logger.LogError("[TERMINATION EMAIL NOTIFICATIONS] ❌ Failed to send email for HRRequestDetailId={DetailId}: {Message}",
                    hrRequestDetailId, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TERMINATION EMAIL NOTIFICATIONS] Error sending scheduled email for HRRequestDetailId={DetailId}, template {TemplateId}",
                hrRequestDetailId, templateId);
            throw;
        }
    }

    /// <summary>
    /// Immediately triggers scheduled emails for a Termination request if their trigger dates have already passed.
    /// This method is called when a Termination request is created/updated to check if any emails should be sent
    /// immediately instead of waiting for the daily ProcessTerminationEmailNotificationsAsync job.
    /// </summary>
    public async Task TriggerOverdueScheduledEmailsForTerminationAsync(int hrRequestDetailId)
    {
        _logger.LogInformation("[TERMINATION IMMEDIATE TRIGGER] Starting check for overdue scheduled emails: HRRequestDetailId={DetailId}", hrRequestDetailId);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MathyELMContext>();

            // Get the specific Termination request detail by ID (each employee has their own HRRequestDetailId)
            var requestDetail = await context.HRRequestDetails
                .Include(rd => rd.ParentRequest)
                .Include(rd => rd.TerminationDetails)
                .FirstOrDefaultAsync(rd => rd.Id == hrRequestDetailId && rd.RequestTypeId == 3); // Termination (RequestType.Termination = 3)

            if (requestDetail?.EffectiveDate == null)
            {
                _logger.LogInformation("[TERMINATION IMMEDIATE TRIGGER] Termination request or EffectiveDate not found for HRRequestDetailId={DetailId}", hrRequestDetailId);
                return;
            }

            var today = DateTime.UtcNow.Date;
            var effectiveDate = requestDetail.EffectiveDate.Value.Date;

            _logger.LogInformation("[TERMINATION IMMEDIATE TRIGGER] Processing Termination request: HRRequestDetailId={DetailId}, EmployeeId={EmployeeId}, EffectiveDate={EffectiveDate}, Today={Today}",
                hrRequestDetailId, requestDetail.EmployeeId, effectiveDate, today);

            // Get ALL email templates for Termination with Scheduled trigger type
            var templates = await context.EmailTemplates
                .Where(t => t.RequestType == "TERMINATION" && t.TriggerType == "Scheduled" && t.IsActive && !t.IsDeleted)
                .ToListAsync();

            if (!templates.Any())
            {
                _logger.LogInformation("[TERMINATION IMMEDIATE TRIGGER] No active Termination scheduled email templates found");
                return;
            }

            foreach (var template in templates)
            {
                try
                {
                    // Calculate trigger date (works with positive and negative SubmissionFreq)
                    var triggerDate = effectiveDate.AddDays(template.SubmissionFreq ?? 0);

                    _logger.LogInformation("[TERMINATION IMMEDIATE TRIGGER] Checking template '{TemplateName}' (Id={TemplateId}): TriggerDate={TriggerDate}, Today={Today}, SubmissionFreq={Freq}",
                        template.TemplateName, template.Id, triggerDate, today, template.SubmissionFreq ?? 0);

                    // If trigger date is today or in the past, immediately enqueue
                    if (triggerDate <= today)
                    {
                        // Note: Duplicate check is handled by Hangfire job ID uniqueness
                        // The job ID includes hrRequestDetailId which ensures each employee gets their own email

                        // Enqueue immediately using HRRequestDetailId (for specific employee)
                        var jobId = BackgroundJob.Enqueue(
                            () => SendScheduledTerminationEmailAsync(hrRequestDetailId, template.Id));

                        _logger.LogInformation("[TERMINATION IMMEDIATE TRIGGER] ✅ ENQUEUED IMMEDIATELY: HRRequestDetailId={DetailId}, EmployeeId={EmployeeId}, Template='{TemplateName}' (Id={TemplateId}), TriggerDate={TriggerDate} (SubmissionFreq={Freq}), JobId={JobId}",
                            hrRequestDetailId, requestDetail.EmployeeId, template.TemplateName, template.Id, triggerDate, template.SubmissionFreq ?? 0, jobId);
                    }
                    else
                    {
                        var daysUntil = (triggerDate - today).Days;
                        _logger.LogInformation("[TERMINATION IMMEDIATE TRIGGER] Template '{TemplateName}' not yet due: TriggerDate={TriggerDate} ({DaysUntil} days from now)",
                            template.TemplateName, triggerDate, daysUntil);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TERMINATION IMMEDIATE TRIGGER] Error processing template '{TemplateName}' (Id={TemplateId}) for HRRequestDetailId={DetailId}",
                        template.TemplateName, template.Id, hrRequestDetailId);
                    // Continue processing other templates even if one fails
                }
            }

            _logger.LogInformation("[TERMINATION IMMEDIATE TRIGGER] Completed checking overdue scheduled emails for HRRequestDetailId={DetailId}", hrRequestDetailId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TERMINATION IMMEDIATE TRIGGER] Error in TriggerOverdueScheduledEmailsForTerminationAsync for HRRequestDetailId={DetailId}", hrRequestDetailId);
            // Don't throw - this is a best-effort operation and shouldn't block the request save
        }
    }

    #endregion

    #region Failed Request Email Notification

    /// <summary>
    /// Send a retry-notification email when new hire verification is being retried
    /// because Viewpoint returned a null or empty HireDate. Resolves recipients from the
    /// "Failed Request" template so HR and the submitter are informed of the delay.
    /// </summary>
    private async Task SendHireDateRetryEmailAsync(
        MathyELMContext context,
        HRRequestDetail requestDetail,
        string retryMessage,
        string? submitterEmail = null)
    {
        try
        {
            var parentRequest = await context.HRRequests
                .Where(r => r.Id == requestDetail.ParentRequestId && !r.IsDeleted)
                .FirstOrDefaultAsync();

            string? managerEmail = null;
            var employee = await context.Employees
                .Where(e => e.EmployeeNumber == requestDetail.EmployeeId && !e.IsDeleted)
                .FirstOrDefaultAsync();

            if (employee?.SupervisorId.HasValue == true)
            {
                var supervisor = await context.Employees
                    .Where(e => e.EmployeeNumber == employee.SupervisorId.Value && !e.IsDeleted)
                    .FirstOrDefaultAsync();
                managerEmail = supervisor?.WorkEmail;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var emailRecipientsService = scope.ServiceProvider.GetRequiredService<IEmailRecipientsService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var recipients = await emailRecipientsService.GetRecipientsFromTemplateAsync(
                "Failed Request",
                requestDetail.EmployeeCompanyCode,
                requestDetail.EmployeeDepartmentCode ?? 0,
                managerEmail,
                submitterEmail ?? parentRequest?.SubmitterEmail,
                null,
                "NEWHIRE"
            );

            if (recipients == null || !recipients.Any())
            {
                _ecmLogger.LogEmailNotification(false, "HireDateRetryEmail", "", null, "No recipients resolved for HireDate retry notification");
                return;
            }

            var toEmail = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));
            var subject = $"New Hire Request Verification Retry - Request #{requestDetail.Id}";
            var body =
                "<p>The new hire verification for this request is being retried.</p>" +
                $"<p><strong>Reason:</strong> {System.Net.WebUtility.HtmlEncode(retryMessage)}</p>" +
                $"<p><strong>Request Detail ID:</strong> {requestDetail.Id}</p>" +
                "<p>The system will automatically retry the verification in 2 hours. No action is required unless HireDate is still missing in Viewpoint after the retry — the request will then be marked as Failed.</p>";

            var emailResult = await emailService.SendEmailAsync(toEmail, subject, body);
            _ecmLogger.LogEmailNotification(emailResult?.Success == true, "HireDateRetryEmail", toEmail, null,
                emailResult?.Success == true ? null : emailResult?.Message);
        }
        catch (Exception ex)
        {
            _ecmLogger.LogEmailNotification(false, "HireDateRetryEmail", "", null, $"Exception: {ex.Message}");
            _logger.LogError(ex, "Failed to send HireDate retry notification email for HR request detail {HRRequestDetailId}", requestDetail.Id);
        }
    }

    /// <summary>
    /// Send "Failed Request" email notification when a request status is set to Failed.
    /// Resolves recipients from the "Failed Request" template and sends the error message.
    /// </summary>
    private async Task SendFailedRequestEmailAsync(
        MathyELMContext context,
        HRRequestDetail requestDetail,
        string errorMessage,
        string? submitterEmail = null)
    {
        try
        {
            var requestTypeString = requestDetail.RequestTypeId switch
            {
                1 => "PROMOTION",
                2 => "LAYOFF",
                3 => "TERMINATION",
                4 => "RETURNTOWORK",
                5 => "NEWHIRE",
                _ => null
            };

            if (requestTypeString == null)
            {
                _ecmLogger.LogEmailNotification(false, "FailedRequestEmail", "", null, $"Unknown RequestTypeId {requestDetail.RequestTypeId}");
                return;
            }

            // Get parent request for submitter info
            var parentRequest = await context.HRRequests
                .Where(r => r.Id == requestDetail.ParentRequestId && !r.IsDeleted)
                .FirstOrDefaultAsync();

            // Get supervisor/manager email
            string? managerEmail = null;
            var employee = await context.Employees
                .Where(e => e.EmployeeNumber == requestDetail.EmployeeId && !e.IsDeleted)
                .FirstOrDefaultAsync();

            if (employee?.SupervisorId.HasValue == true)
            {
                var supervisor = await context.Employees
                    .Where(e => e.EmployeeNumber == employee.SupervisorId.Value && !e.IsDeleted)
                    .FirstOrDefaultAsync();
                managerEmail = supervisor?.WorkEmail;
            }

            // Resolve recipients from the "Failed Request" template
            using var scope = _serviceScopeFactory.CreateScope();
            var emailRecipientsService = scope.ServiceProvider.GetRequiredService<IEmailRecipientsService>();

            var recipients = await emailRecipientsService.GetRecipientsFromTemplateAsync(
                "Failed Request",
                requestDetail.EmployeeCompanyCode,
                requestDetail.EmployeeDepartmentCode ?? 0,
                managerEmail,
                submitterEmail ?? parentRequest?.SubmitterEmail,
                null,
                requestTypeString
            );

            if (recipients == null || !recipients.Any())
            {
                _ecmLogger.LogEmailNotification(false, "FailedRequestEmail", "", null, $"No recipients resolved for Failed Request ({requestTypeString})");
                return;
            }

            var toEmail = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

            // Send email using the appropriate type-specific method
            switch (requestDetail.RequestTypeId)
            {
                case 1: // Promotion
                    var promotionDetail = await context.PromotionRequestDetails
                        .Where(p => p.RequestDetailId == requestDetail.Id && !p.IsDeleted)
                        .FirstOrDefaultAsync();
                    var promotionData = new CreatePromotionRequestDto
                    {
                        EmployeeId = requestDetail.EmployeeId,
                        EffectiveDate = requestDetail.EffectiveDate ?? DateTime.MinValue,
                        Notes = parentRequest?.Notes,
                        ErrorMessage = errorMessage,
                        NewPayrollCompanyCode = promotionDetail?.NewPayrollCompanyCode ?? 0,
                        NewPayrollDeptCode = promotionDetail?.NewPayrollDeptCode ?? 0
                    };
                    await _azureServiceBusEmailService.SendEmailFromTemplateNameForPromotionAsync(
                        "Failed Request", promotionData, toEmail, null, requestDetail.ParentRequestId);
                    break;

                case 2: // Layoff
                    var layoffData = new LayoffEmailDataDto
                    {
                        EmployeeId = requestDetail.EmployeeId,
                        CompanyCode = requestDetail.EmployeeCompanyCode,
                        DeptCode = requestDetail.EmployeeDepartmentCode,
                        EffectiveDate = requestDetail.EffectiveDate,
                        Notes = parentRequest?.Notes,
                        Submitter = parentRequest?.SubmitterName,
                        ManagerEmail = managerEmail,
                        ErrorMessage = errorMessage
                    };
                    await _azureServiceBusEmailService.SendEmailFromTemplateNameForLayoffAsync(
                        "Failed Request", layoffData, toEmail, null, requestDetail.ParentRequestId);
                    break;

                case 3: // Termination
                    var terminationData = new TerminationEmailDataDto
                    {
                        EmployeeId = requestDetail.EmployeeId,
                        CompanyCode = requestDetail.EmployeeCompanyCode,
                        DeptCode = requestDetail.EmployeeDepartmentCode,
                        EffectiveDate = requestDetail.EffectiveDate,
                        Notes = parentRequest?.Notes,
                        Submitter = parentRequest?.SubmitterName,
                        ManagerEmail = managerEmail,
                        ErrorMessage = errorMessage
                    };
                    await _azureServiceBusEmailService.SendEmailFromTemplateNameForTerminationAsync(
                        "Failed Request", terminationData, toEmail, null, requestDetail.ParentRequestId);
                    break;

                case 4: // ReturnToWork
                    var rtwData = new ReturnToWorkEmailDataDto
                    {
                        EmployeeId = requestDetail.EmployeeId,
                        CompanyCode = requestDetail.EmployeeCompanyCode,
                        DeptCode = requestDetail.EmployeeDepartmentCode,
                        EffectiveDate = requestDetail.EffectiveDate,
                        Notes = parentRequest?.Notes,
                        Submitter = parentRequest?.SubmitterName,
                        ManagerEmail = managerEmail,
                        ErrorMessage = errorMessage
                    };
                    await _azureServiceBusEmailService.SendEmailFromTemplateNameForReturnToWorkAsync(
                        "Failed Request", rtwData, toEmail, null, requestDetail.ParentRequestId);
                    break;

                case 5: // NewHire
                    var newHireDetail = await context.NewHireRequestDetails
                        .Where(n => n.RequestDetailId == requestDetail.Id && !n.IsDeleted)
                        .FirstOrDefaultAsync();

                    var newHireData = new CreateNewHireRequestDto
                    {
                        Notes = parentRequest?.Notes,
                        ErrorMessage = errorMessage,
                        PersonalInfo = new NewHirePersonalInfoDto
                        {
                            EmployeeId = requestDetail.EmployeeId,
                            FirstName = newHireDetail?.FirstName,
                            LastName = newHireDetail?.LastName,
                            FirstDayEmployment = requestDetail.EffectiveDate
                        },
                        PositionInfo = new NewHirePositionInfoDto
                        {
                            CompanyCode = requestDetail.EmployeeCompanyCode,
                            PayrollDeptCode = requestDetail.EmployeeDepartmentCode,
                            SupervisorId = newHireDetail?.SupervisorId,
                            PositionCode = newHireDetail?.PositionCode
                        }
                    };
                    await _azureServiceBusEmailService.SendEmailFromTemplateNameAsync(
                        "Failed Request", newHireData, toEmail, null, requestDetail.ParentRequestId);
                    break;
            }

            _ecmLogger.LogEmailNotification(true, "FailedRequestEmail", toEmail, $"Failed Request email sent for {requestTypeString} detail {requestDetail.Id}");
        }
        catch (Exception ex)
        {
            _ecmLogger.LogEmailNotification(false, "FailedRequestEmail", "", null, $"Error sending Failed Request email for detail {requestDetail.Id}: {ex.Message}");
        }
    }

    #endregion

}
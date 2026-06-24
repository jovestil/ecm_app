namespace Mathy.ELM.Core.Services;

public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a job to process an HR request notification
    /// </summary>
    /// <param name="hrRequestId">The ID of the HR request</param>
    /// <returns>The job ID</returns>
    string EnqueueNotificationJob(int hrRequestId);
    
    /// <summary>
    /// Schedules a follow-up job for a specific date/time
    /// </summary>
    /// <param name="hrRequestId">The ID of the HR request</param>
    /// <param name="scheduledDate">When to execute the job</param>
    /// <returns>The job ID</returns>
    string ScheduleFollowUpJob(int hrRequestId, DateTime scheduledDate);
    
    /// <summary>
    /// Sets up a recurring job to sync reference data from Viewpoint
    /// </summary>
    void SetupReferenceDataSyncJob();
    
    /// <summary>
    /// Processes a single HR request notification (background job method)
    /// </summary>
    /// <param name="hrRequestId">The ID of the HR request</param>
    Task ProcessHRRequestNotificationAsync(int hrRequestId);
    
    /// <summary>
    /// Processes follow-up actions for an HR request (background job method)
    /// </summary>
    /// <param name="hrRequestId">The ID of the HR request</param>
    Task ProcessFollowUpAsync(int hrRequestId);
    
    /// <summary>
    /// Syncs reference data from Viewpoint (recurring job method)
    /// </summary>
    Task SyncReferenceDataAsync();
    
    /// <summary>
    /// Schedules a Viewpoint status update job for a specific effective date
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID</param>
    /// <param name="effectiveDate">When to execute the status update</param>
    /// <param name="requestTypeId">The request type ID (optional)</param>
    /// <returns>The job ID</returns>
    Task<string> ScheduleViewpointStatusUpdateJob(int hrRequestDetailId, DateTime effectiveDate, int? requestTypeId = null, string? submitterEmail = null);
    
    /// <summary>
    /// Updates employee status in Viewpoint based on HR request (background job method)
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID</param>
    /// <param name="requestType">The type of HR request (optional, will be retrieved from database if not provided)</param>
    Task UpdateEmployeeStatusInViewpointAsync(int hrRequestDetailId, Enums.RequestType? requestType = null, string? submitterEmail = null);
    
    /// <summary>
    /// Verifies that the employee status was properly updated in Viewpoint (background job method)
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID</param>
    /// <param name="expectedStatus">The expected status that should be set</param>
    /// <param name="attemptNumber">The current attempt number (1-2)</param>
    Task VerifyViewpointStatusUpdateAsync(int hrRequestDetailId, string expectedStatus, int attemptNumber = 1, string? submitterEmail = null);

    /// <summary>
    /// Schedules a Viewpoint new hire employee verification job for a specific FirstDayEmployment date
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID</param>
    /// <param name="firstDayEmployment">When to execute the verification</param>
    /// <param name="submitterEmail">Email of the user who submitted the request</param>
    /// <returns>The job ID</returns>
    Task<string> ScheduleViewpointVerifyNewHireEmployee(int hrRequestDetailId, DateTime firstDayEmployment, string? submitterEmail = null);

    /// <summary>
    /// Verifies that the new hire employee was created in Viewpoint (background job method)
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID</param>
    /// <param name="attemptNumber">The current attempt number (1-2)</param>
    /// <param name="submitterEmail">Email of the user who submitted the request</param>
    Task VerifyNewHireEmployeeInViewpointAsync(int hrRequestDetailId, int attemptNumber = 1, string? submitterEmail = null);

    /// <summary>
    /// Sets up recurring job to process New Hire email notifications based on submission frequency (runs daily)
    /// </summary>
    void SetupNewHireEmailNotificationsJob();

    /// <summary>
    /// Processes New Hire email notifications based on EmailTemplate SubmissionFreq (recurring job method)
    /// Schedules all emails via Hangfire based on: TriggerDate = FirstDayEmployment + SubmissionFreq
    /// </summary>
    Task ProcessNewHireEmailNotificationsAsync();

    /// <summary>
    /// Sends a scheduled New Hire email notification (background job method)
    /// </summary>
    /// <param name="parentRequestId">The parent HR request ID</param>
    /// <param name="templateId">The email template ID</param>
    Task SendScheduledNewHireEmailAsync(int parentRequestId, int templateId);

    /// <summary>
    /// Public wrapper method for Hangfire job execution (Synchronous version)
    /// </summary>
    /// <param name="parentRequestId">The parent HR request ID</param>
    /// <param name="templateId">The email template ID</param>
    void ExecuteScheduledNewHireEmailSync(int parentRequestId, int templateId);

    /// <summary>
    /// Public wrapper method for Hangfire job execution (Async version)
    /// Hangfire will call this through its JobActivator with proper dependency injection
    /// </summary>
    /// <param name="parentRequestId">The parent HR request ID</param>
    /// <param name="templateId">The email template ID</param>
    Task ExecuteScheduledNewHireEmailAsync(int parentRequestId, int templateId);

    /// <summary>
    /// Immediately triggers scheduled emails for a new hire request if their trigger dates have already passed.
    /// This method is called when a new hire request is created/updated to check if any emails should be sent
    /// immediately instead of waiting for the daily ProcessNewHireEmailNotificationsAsync job.
    ///
    /// Works with ALL trigger types (positive and negative SubmissionFreq):
    /// - "Past Start Date" (SubmissionFreq = 7, 14, etc.)
    /// - "Pre-Start Date" (SubmissionFreq = -3, -7, etc.)
    /// - Any other template type where triggerDate has already passed
    /// </summary>
    /// <param name="parentRequestId">The parent HR request ID</param>
    Task TriggerOverdueScheduledEmailsAsync(int parentRequestId);

    /// <summary>
    /// Sets up recurring job to send draft reminder emails to submitters daily
    /// </summary>
    void SetupDraftReminderEmailJob();

    /// <summary>
    /// Processes and sends draft reminder emails for all new hire requests with Draft status (recurring job method)
    /// Queries for all draft new hire requests and sends "Draft Reminder" email to the submitter
    /// </summary>
    Task ProcessDraftReminderEmailsAsync();

    /// <summary>
    /// Sends an immediate draft reminder email to the submitter when they save a request as Draft
    /// Called when request status is set to Draft (RequestStatusId = 6)
    /// Uses NotificationQueue deduplication to prevent duplicate emails on same day
    /// </summary>
    /// <param name="parentRequestId">The parent HR request ID</param>
    Task SendImmediateDraftReminderAsync(int parentRequestId);

    /// <summary>
    /// Sets up recurring job to send Welcome Email notifications to new hires on their First Day of Employment
    /// </summary>
    void SetupWelcomeEmailScheduledJob();

    /// <summary>
    /// Processes Welcome Email notifications for new hires with First Day of Employment = today
    /// Searches for employee in Viewpoint API and sends Welcome Email if found
    /// Notifies HRDL & submitter if employee not found or API error occurs
    /// </summary>
    Task ProcessWelcomeEmailNotificationsAsync();

    /// <summary>
    /// Schedules a new hire pre-employment processing job to execute on the FirstDayEmployment date
    /// The job will call UpdateEmployeeForNewHireInViewPointAsync to prepare the employee in Viewpoint
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID</param>
    /// <param name="firstDayEmployment">The first day of employment date</param>
    /// <param name="submitterEmail">Email of the user who submitted the request</param>
    /// <returns>The job ID</returns>
    Task<string> ScheduleNewHirePreEmploymentProcessingJob(int hrRequestDetailId, DateTime firstDayEmployment, string? submitterEmail = null);

    /// <summary>
    /// Processes new hire pre-employment preparation on the FirstDayEmployment date
    /// Calls UpdateEmployeeForNewHireInViewPointAsync to update employee in Viewpoint
    /// Updates RequestStatusId to Completed (3) if successful, Failed (4) if unsuccessful
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID</param>
    /// <param name="submitterEmail">Email of the user who submitted the request</param>
    Task ProcessNewHirePreEmploymentAsync(int hrRequestDetailId, string? submitterEmail = null);

    /// <summary>
    /// Sets up recurring job to process Return to Work email notifications based on submission frequency (runs daily)
    /// </summary>
    void SetupReturnToWorkEmailNotificationsJob();

    /// <summary>
    /// Processes Return to Work email notifications based on EmailTemplate SubmissionFreq (recurring job method)
    /// Schedules all emails via Hangfire based on: TriggerDate = EffectiveDate + SubmissionFreq
    /// </summary>
    Task ProcessReturnToWorkEmailNotificationsAsync();

    /// <summary>
    /// Sends a scheduled Return to Work email notification (background job method)
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID (for specific employee)</param>
    /// <param name="templateId">The email template ID</param>
    Task SendScheduledReturnToWorkEmailAsync(int hrRequestDetailId, int templateId);

    /// <summary>
    /// Immediately triggers scheduled emails for a Return to Work request if their trigger dates have already passed.
    /// This method is called when a Return to Work request is created/updated to check if any emails should be sent
    /// immediately instead of waiting for the daily ProcessReturnToWorkEmailNotificationsAsync job.
    ///
    /// Works with ALL trigger types (positive and negative SubmissionFreq):
    /// - "Post Effective Date" (SubmissionFreq = 7, 14, etc.)
    /// - "Pre Effective Date" (SubmissionFreq = -3, -7, etc.)
    /// - Any other template type where triggerDate has already passed
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID (for specific employee)</param>
    Task TriggerOverdueScheduledEmailsForReturnToWorkAsync(int hrRequestDetailId);

    /// <summary>
    /// Sets up recurring job to process Layoff email notifications based on submission frequency (runs daily)
    /// </summary>
    void SetupLayoffEmailNotificationsJob();

    /// <summary>
    /// Processes Layoff email notifications based on EmailTemplate SubmissionFreq (recurring job method)
    /// Schedules all emails via Hangfire based on: TriggerDate = LastDayWorked + SubmissionFreq
    /// </summary>
    Task ProcessLayoffEmailNotificationsAsync();

    /// <summary>
    /// Sends a scheduled Layoff email notification (background job method)
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID (for specific employee)</param>
    /// <param name="templateId">The email template ID</param>
    Task SendScheduledLayoffEmailAsync(int hrRequestDetailId, int templateId);

    /// <summary>
    /// Immediately triggers scheduled emails for a Layoff request if their trigger dates have already passed.
    /// This method is called when a Layoff request is created/updated to check if any emails should be sent
    /// immediately instead of waiting for the daily ProcessLayoffEmailNotificationsAsync job.
    ///
    /// Works with ALL trigger types (positive and negative SubmissionFreq):
    /// - "Post Last Day Worked" (SubmissionFreq = 7, 14, etc.)
    /// - "Pre Last Day Worked" (SubmissionFreq = -3, -7, etc.)
    /// - Any other template type where triggerDate has already passed
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID (for specific employee)</param>
    Task TriggerOverdueScheduledEmailsForLayoffAsync(int hrRequestDetailId);

    /// <summary>
    /// Sets up recurring job to process Termination email notifications based on submission frequency (runs daily)
    /// </summary>
    void SetupTerminationEmailNotificationsJob();

    /// <summary>
    /// Processes Termination email notifications based on EmailTemplate SubmissionFreq (recurring job method)
    /// Schedules all emails via Hangfire based on: TriggerDate = EffectiveDate + SubmissionFreq
    /// </summary>
    Task ProcessTerminationEmailNotificationsAsync();

    /// <summary>
    /// Sends a scheduled Termination email notification (background job method)
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID (for specific employee)</param>
    /// <param name="templateId">The email template ID</param>
    Task SendScheduledTerminationEmailAsync(int hrRequestDetailId, int templateId);

    /// <summary>
    /// Immediately triggers scheduled emails for a Termination request if their trigger dates have already passed.
    /// This method is called when a Termination request is created/updated to check if any emails should be sent
    /// immediately instead of waiting for the daily ProcessTerminationEmailNotificationsAsync job.
    ///
    /// Works with ALL trigger types (positive and negative SubmissionFreq):
    /// - "Post Effective Date" (SubmissionFreq = 7, 14, etc.)
    /// - "Pre Effective Date" (SubmissionFreq = -3, -7, etc.)
    /// - Any other template type where triggerDate has already passed
    /// </summary>
    /// <param name="hrRequestDetailId">The HR request detail ID (for specific employee)</param>
    Task TriggerOverdueScheduledEmailsForTerminationAsync(int hrRequestDetailId);
}
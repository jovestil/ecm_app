using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;

namespace Mathy.ELM.Infrastructure.Services;

/// <summary>
/// Producer-only Azure Service Bus implementation for email notification queue
/// Sends messages to Azure Service Bus; Power Automate consumes and processes them
/// </summary>
public class AzureServiceBusEmailService : IAzureServiceBusEmailService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureServiceBusEmailService> _logger;
    private readonly IEcmLogger _ecmLogger;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ServiceBusAdministrationClient _adminClient;
    private readonly MathyELMContext _context;
    private readonly IEmailFieldMapperService _fieldMapperService;
    private readonly IEmailTemplateBuilderService _templateBuilderService;

    private readonly string _queueName;

    public AzureServiceBusEmailService(
        IConfiguration configuration,
        ILogger<AzureServiceBusEmailService> logger,
        IEcmLogger ecmLogger,
        MathyELMContext context,
        IEmailFieldMapperService fieldMapperService,
        IEmailTemplateBuilderService templateBuilderService)
    {
        _configuration = configuration;
        _logger = logger;
        _ecmLogger = ecmLogger;
        _context = context;
        _fieldMapperService = fieldMapperService;
        _templateBuilderService = templateBuilderService;

        // Get configuration values
        var connectionString = _configuration["AzureServiceBus:ConnectionString"];
        _queueName = _configuration["AzureServiceBus:QueueName"] ?? "email-notifications";

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Azure Service Bus connection string is not configured");
        }

        // Initialize Service Bus client and sender (producer only)
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(_queueName);
        _adminClient = new ServiceBusAdministrationClient(connectionString);

        _logger.LogInformation("[AZURE SERVICE BUS] Email service initialized (producer-only) for queue: {QueueName}", _queueName);
    }

    /// <summary>
    /// Ensure the queue exists in Azure Service Bus, creating it if necessary
    /// </summary>
    public async Task<bool> EnsureQueueExistsAsync()
    {
        try
        {
            bool queueExists = await _adminClient.QueueExistsAsync(_queueName);

            if (!queueExists)
            {
                await _adminClient.CreateQueueAsync(_queueName);
                _logger.LogInformation("[AZURE SERVICE BUS] Queue '{QueueName}' created successfully", _queueName);
                _ecmLogger.LogServiceBus(true, "created", _queueName);
            }
            else
            {
                _logger.LogInformation("[AZURE SERVICE BUS] Queue '{QueueName}' already exists", _queueName);
                _ecmLogger.LogServiceBus(true, "already exists", _queueName);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE SERVICE BUS] Failed to ensure queue '{QueueName}' exists", _queueName);
            _ecmLogger.LogServiceBus(false, "ensure queue exists", _queueName, errorMessage: ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Parse Body field from EmailTemplate to extract ContentCodes (comma-delimited)
    /// Example: "START-DATE,EMPLOYEE-NAME,COMPANY-NAME" → ["START-DATE", "EMPLOYEE-NAME", "COMPANY-NAME"]
    /// </summary>
    private List<string> ParseBodyFieldCodes(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            _logger.LogWarning("[AZURE SERVICE BUS] Body field is empty or null");
            return new List<string>();
        }

        var codes = body
            .Split(',')
            .Select(code => code.Trim())
            .Where(code => !string.IsNullOrEmpty(code))
            .ToList();

        _logger.LogInformation("[AZURE SERVICE BUS] Parsed {Count} field codes from Body: {Codes}",
            codes.Count, string.Join(", ", codes));

        return codes;
    }

    /// <summary>
    /// Extracts @@CODE placeholder patterns from HTML body content
    /// Used for ContentStyling="Text" templates where Body contains HTML with @@PLACEHOLDER patterns
    /// Example: "@@EMPLOYEE-FIRSTNAME" and "@@START-DATE" from HTML like "<p>Welcome @@EMPLOYEE-FIRSTNAME! Start: @@START-DATE</p>"
    /// </summary>
    private List<string> ExtractBodyFieldCodesForText(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            _logger.LogWarning("[AZURE SERVICE BUS] Body field is empty or null");
            return new List<string>();
        }

        // Find all @@CODE patterns (e.g., @@EMPLOYEE-FIRSTNAME, @@START-DATE)
        var placeholderPattern = @"@@([A-Z\-]+)";
        var matches = System.Text.RegularExpressions.Regex.Matches(body, placeholderPattern);

        var codes = new List<string>();
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var code = match.Groups[1].Value;
            if (!string.IsNullOrEmpty(code) && !codes.Contains(code))
            {
                codes.Add(code);
            }
        }

        _logger.LogInformation("[AZURE SERVICE BUS] Extracted {Count} field codes from Text-style Body: {Codes}",
            codes.Count, string.Join(", ", codes));

        return codes;
    }

    /// <summary>
    /// Query EmailContentMappers to get ContentField names for Body part
    /// Example: ContentCode 'START-DATE' → ContentField 'startdate'
    /// Falls back to 'APPLICATION' ContentSource for shared fields (e.g., SUBMITTER) if not found in specific source
    /// </summary>
    /// <param name="contentCodes">List of content codes to map</param>
    /// <param name="contentSource">Optional content source filter (e.g., 'NEWHIRE', 'PROMOTION'). If null, returns first match.</param>
    private async Task<Dictionary<string, string>> GetBodyContentFieldMappingsAsync(List<string> contentCodes, string? contentSource = null)
    {
        if (!contentCodes.Any())
        {
            _logger.LogWarning("[AZURE SERVICE BUS] No content codes provided for Body mapping");
            return new Dictionary<string, string>();
        }

        try
        {
            var mappings = new Dictionary<string, string>();

            foreach (var code in contentCodes)
            {
                Core.Entities.EmailContentMapper? mapper = null;

                // First try to find mapping with specific ContentSource
                if (!string.IsNullOrEmpty(contentSource))
                {
                    mapper = await _context.EmailContentMappers
                        .Where(m => m.ContentCode == code &&
                               m.ContentPartType == "Body" &&
                               m.ContentSource == contentSource &&
                               !m.IsDeleted)
                        .FirstOrDefaultAsync();

                    // Fallback to 'APPLICATION' ContentSource for shared fields (e.g., SUBMITTER)
                    if (mapper == null)
                    {
                        mapper = await _context.EmailContentMappers
                            .Where(m => m.ContentCode == code &&
                                   m.ContentPartType == "Body" &&
                                   m.ContentSource == "APPLICATION" &&
                                   !m.IsDeleted)
                            .FirstOrDefaultAsync();

                        if (mapper != null)
                        {
                            _logger.LogInformation("[AZURE SERVICE BUS] ContentCode '{Code}' not found for '{Source}', using APPLICATION fallback",
                                code, contentSource);
                        }
                    }
                }
                else
                {
                    // No specific source, get first match
                    mapper = await _context.EmailContentMappers
                        .Where(m => m.ContentCode == code &&
                               m.ContentPartType == "Body" &&
                               !m.IsDeleted)
                        .FirstOrDefaultAsync();
                }

                if (mapper != null && !string.IsNullOrEmpty(mapper.ContentField))
                {
                    mappings[code] = mapper.ContentField;
                    _logger.LogInformation("[AZURE SERVICE BUS] Mapped ContentCode '{Code}' to ContentField '{Field}' (Source: {Source})",
                        code, mapper.ContentField, mapper.ContentSource);
                }
                else
                {
                    _logger.LogWarning("[AZURE SERVICE BUS] No ContentField mapping found for ContentCode '{Code}' (Body, Source: {Source})",
                        code, contentSource ?? "any");
                }
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Body ContentField mappings completed: {MappingCount}/{TotalCount} mapped",
                mappings.Count, contentCodes.Count);

            return mappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE SERVICE BUS] Failed to query Body ContentField mappings");
            throw;
        }
    }

    /// <summary>
    /// Get ContentLabel mappings for Body content codes
    /// Maps ContentCode (e.g., "START-DATE") to ContentLabel (e.g., "Start Date")
    /// </summary>
    private async Task<Dictionary<string, string>> GetBodyContentLabelMappingsAsync(List<string> contentCodes, string? contentSource = null)
    {
        if (!contentCodes.Any())
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var mappings = new Dictionary<string, string>();

            foreach (var code in contentCodes)
            {
                Core.Entities.EmailContentMapper? mapper = null;

                // First try to find mapping with specific ContentSource
                if (!string.IsNullOrEmpty(contentSource))
                {
                    mapper = await _context.EmailContentMappers
                        .Where(m => m.ContentCode == code &&
                               m.ContentPartType == "Body" &&
                               m.ContentSource == contentSource &&
                               !m.IsDeleted)
                        .FirstOrDefaultAsync();

                    // Fallback to 'APPLICATION' ContentSource for shared fields
                    if (mapper == null)
                    {
                        mapper = await _context.EmailContentMappers
                            .Where(m => m.ContentCode == code &&
                                   m.ContentPartType == "Body" &&
                                   m.ContentSource == "APPLICATION" &&
                                   !m.IsDeleted)
                            .FirstOrDefaultAsync();
                    }
                }
                else
                {
                    mapper = await _context.EmailContentMappers
                        .Where(m => m.ContentCode == code &&
                               m.ContentPartType == "Body" &&
                               !m.IsDeleted)
                        .FirstOrDefaultAsync();
                }

                if (mapper != null && !string.IsNullOrEmpty(mapper.ContentLabel))
                {
                    mappings[code] = mapper.ContentLabel;
                }
                else
                {
                    // Fallback: use ContentCode as label if ContentLabel is not set
                    mappings[code] = code;
                }
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Body ContentLabel mappings completed: {MappingCount}/{TotalCount}",
                mappings.Count, contentCodes.Count);

            return mappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE SERVICE BUS] Failed to query Body ContentLabel mappings");
            throw;
        }
    }

    /// <summary>
    /// Extract field codes from Subject string (tokens starting with @)
    /// Example: "3-Days until New Hire Starts : @START-DATE @EMPLOYEE-NAME" → ["START-DATE", "EMPLOYEE-NAME"]
    /// </summary>
    private List<string> ExtractSubjectFieldCodes(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            _logger.LogWarning("[AZURE SERVICE BUS] Subject is empty or null");
            return new List<string>();
        }

        // Find all @@FIELD-CODE patterns
        var codePattern = System.Text.RegularExpressions.Regex.Matches(subject, @"@@([A-Z\-]+)");
        var codes = codePattern
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        _logger.LogInformation("[AZURE SERVICE BUS] Extracted {Count} field codes from Subject: {Codes}",
            codes.Count, string.Join(", ", codes));

        return codes;
    }

    /// <summary>
    /// Query EmailContentMappers to get ContentField names for Subject part
    /// Example: ContentCode 'START-DATE' with ContentPartType 'Subject' → ContentField 'startdate'
    /// Falls back to 'APPLICATION' ContentSource for shared fields if not found in specific source
    /// </summary>
    /// <param name="contentCodes">List of content codes to map</param>
    /// <param name="contentSource">Optional content source filter (e.g., 'NEWHIRE', 'PROMOTION'). If null, returns first match.</param>
    private async Task<Dictionary<string, string>> GetSubjectContentFieldMappingsAsync(List<string> contentCodes, string? contentSource = null)
    {
        if (!contentCodes.Any())
        {
            _logger.LogWarning("[AZURE SERVICE BUS] No content codes provided for Subject mapping");
            return new Dictionary<string, string>();
        }

        try
        {
            var mappings = new Dictionary<string, string>();

            foreach (var code in contentCodes)
            {
                Core.Entities.EmailContentMapper? mapper = null;

                // First try to find mapping with specific ContentSource
                if (!string.IsNullOrEmpty(contentSource))
                {
                    mapper = await _context.EmailContentMappers
                        .Where(m => m.ContentCode == code &&
                               m.ContentPartType == "Subject" &&
                               m.ContentSource == contentSource &&
                               !m.IsDeleted)
                        .FirstOrDefaultAsync();

                    // Fallback to 'APPLICATION' ContentSource for shared fields
                    if (mapper == null)
                    {
                        mapper = await _context.EmailContentMappers
                            .Where(m => m.ContentCode == code &&
                                   m.ContentPartType == "Subject" &&
                                   m.ContentSource == "APPLICATION" &&
                                   !m.IsDeleted)
                            .FirstOrDefaultAsync();

                        if (mapper != null)
                        {
                            _logger.LogInformation("[AZURE SERVICE BUS] Subject ContentCode '{Code}' not found for '{Source}', using APPLICATION fallback",
                                code, contentSource);
                        }
                    }
                }
                else
                {
                    // No specific source, get first match
                    mapper = await _context.EmailContentMappers
                        .Where(m => m.ContentCode == code &&
                               m.ContentPartType == "Subject" &&
                               !m.IsDeleted)
                        .FirstOrDefaultAsync();
                }

                if (mapper != null && !string.IsNullOrEmpty(mapper.ContentField))
                {
                    mappings[code] = mapper.ContentField;
                    _logger.LogInformation("[AZURE SERVICE BUS] Mapped Subject ContentCode '{Code}' to ContentField '{Field}' (Source: {Source})",
                        code, mapper.ContentField, mapper.ContentSource);
                }
                else
                {
                    _logger.LogWarning("[AZURE SERVICE BUS] No ContentField mapping found for Subject ContentCode '{Code}' (Source: {Source})",
                        code, contentSource ?? "any");
                }
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Subject ContentField mappings completed: {MappingCount}/{TotalCount} mapped",
                mappings.Count, contentCodes.Count);

            return mappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE SERVICE BUS] Failed to query Subject ContentField mappings");
            throw;
        }
    }

    /// <summary>
    /// Replace @CODE placeholders in subject with actual values from mapped field data
    /// Example: "3-Days until... @START-DATE @EMPLOYEE-NAME" with fieldData ['startdate'→'2025-01-15', 'employee_name'→'John Doe']
    ///          → "3-Days until... 2025-01-15 John Doe"
    /// </summary>
    private async Task<string> ReplaceSubjectPlaceholdersAsync(string subject,
        List<string> subjectCodes,
        Dictionary<string, string> subjectFieldMappings,
        CreateNewHireRequestDto requestData)
    {
        if (string.IsNullOrWhiteSpace(subject) || !subjectCodes.Any())
        {
            return subject;
        }

        var replacedSubject = subject;

        foreach (var code in subjectCodes)
        {
            if (!subjectFieldMappings.ContainsKey(code))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Skipping subject placeholder @@{Code} - no ContentField mapping found", code);
                continue;
            }

            var contentField = subjectFieldMappings[code];

            // Map single field to get its value
            var fieldData = await _fieldMapperService.MapNewHireFieldsToDataAsync(
                requestData,
                new List<string> { contentField });

            if (fieldData.ContainsKey(contentField))
            {
                var fieldValue = fieldData[contentField]?.ToString() ?? string.Empty;
                var placeholder = $"@@{code}";
                replacedSubject = replacedSubject.Replace(placeholder, fieldValue);

                _logger.LogInformation("[AZURE SERVICE BUS] Replaced subject placeholder '{Placeholder}' with value '{Value}'",
                    placeholder, fieldValue);
            }
            else
            {
                _logger.LogWarning("[AZURE SERVICE BUS] ContentField '{Field}' not found in mapped data for subject code '{Code}'",
                    contentField, code);
            }
        }

        // Replace [currentdate] placeholder with today's date in MM/dd/yyyy format
        var currentDatePattern = @"\[currentdate\]";
        var currentDateMatches = System.Text.RegularExpressions.Regex.Matches(
            replacedSubject, currentDatePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (currentDateMatches.Count > 0)
        {
            var formattedDate = DateTime.Now.ToString("MM/dd/yyyy");
            replacedSubject = System.Text.RegularExpressions.Regex.Replace(
                replacedSubject, currentDatePattern, formattedDate,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            _logger.LogInformation("[AZURE SERVICE BUS] Replaced [currentdate] placeholder with {Date}",
                formattedDate);
        }

        _logger.LogInformation("[AZURE SERVICE BUS] Subject placeholder replacement complete");
        return replacedSubject;
    }

    /// <summary>
    /// Replaces @@CODE placeholders in HTML body with actual field values
    /// Used when ContentStyling is "Text" (pre-formatted HTML with placeholder patterns)
    /// Finds patterns like @@EMPLOYEE-FIRSTNAME, @@START-DATE, etc. and replaces them with mapped values
    /// </summary>
    /// <param name="body">HTML body content with @@CODE placeholders</param>
    /// <param name="fieldData">Dictionary of ContentField → Value mappings</param>
    /// <param name="bodyContentFieldMappings">Dictionary of ContentCode → ContentField mappings</param>
    /// <returns>HTML body with placeholders replaced with actual values</returns>
    private string ReplaceBodyPlaceholders(
        string body,
        Dictionary<string, string> fieldData,
        Dictionary<string, string> bodyContentFieldMappings)
    {
        if (string.IsNullOrWhiteSpace(body) || fieldData.Count == 0 || bodyContentFieldMappings.Count == 0)
        {
            return body;
        }

        var replacedBody = body;

        // Find all @@CODE patterns in the body (e.g., @@EMPLOYEE-FIRSTNAME, @@START-DATE)
        var placeholderPattern = @"@@([A-Z\-]+)";
        var matches = System.Text.RegularExpressions.Regex.Matches(replacedBody, placeholderPattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var contentCode = match.Groups[1].Value;
            var placeholder = $"@@{contentCode}";

            // Check if we have a ContentField mapping for this ContentCode
            if (!bodyContentFieldMappings.TryGetValue(contentCode, out var contentField))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] No ContentField mapping found for body placeholder '{Placeholder}'", placeholder);
                continue;
            }

            // Look up the value in fieldData
            if (fieldData.TryGetValue(contentField, out var fieldValue))
            {
                var valueToReplace = fieldValue ?? "N/A";
                replacedBody = replacedBody.Replace(placeholder, valueToReplace);

                _logger.LogInformation("[AZURE SERVICE BUS] Replaced body placeholder '{Placeholder}' with value",
                    placeholder);
            }
            else
            {
                _logger.LogWarning("[AZURE SERVICE BUS] ContentField '{Field}' not found in mapped data for body placeholder '{Placeholder}'",
                    contentField, placeholder);
                replacedBody = replacedBody.Replace(placeholder, "N/A");
            }
        }

        _logger.LogInformation("[AZURE SERVICE BUS] Body placeholder replacement complete");
        return replacedBody;
    }

    /// <summary>
    /// Send a single email notification to the Azure Service Bus queue
    /// Logs the notification to NotificationQueue for audit and tracking purposes
    /// </summary>
    public async Task<ApiResponse<bool>> SendEmailNotificationAsync(EmailNotificationDto emailNotification)
    {
        NotificationQueue? notificationLog = null;

        try
        {
            _logger.LogInformation("[AZURE SERVICE BUS] Queueing email notification to {ToEmail} - Type: {NotificationType}",
                emailNotification.ToEmail, emailNotification.NotificationType);

            // Step 1: Log to NotificationQueue (Status = "Pending") - only if RequestId is valid
            if (emailNotification.RequestId.HasValue && emailNotification.RequestId > 0)
            {
                notificationLog = new NotificationQueue
                {
                    RequestId = emailNotification.RequestId.Value,
                    TemplateId = emailNotification.TemplateId,
                    ToEmail = emailNotification.ToEmail,
                    CcEmail = emailNotification.CcEmail,
                    Subject = emailNotification.Subject,
                    Body = emailNotification.Body,
                    Status = "Pending",
                    AttemptCount = 0,
                    CreatedDate = DateTime.UtcNow
                };

                _context.NotificationQueue.Add(notificationLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[AZURE SERVICE BUS] Logged email to NotificationQueue - RequestId: {RequestId}, NotificationId: {NotificationId}",
                    emailNotification.RequestId, notificationLog.Id);
            }
            else
            {
                _logger.LogInformation("[AZURE SERVICE BUS] Email notification has no RequestId, skipping NotificationQueue logging - ToEmail: {ToEmail}",
                    emailNotification.ToEmail);
            }

            // Serialize email notification to JSON
            var messageBody = JsonSerializer.Serialize(emailNotification);
            var message = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json",
                Subject = emailNotification.NotificationType,
                MessageId = Guid.NewGuid().ToString()
            };

            // Add custom properties for filtering and routing in Power Automate
            message.ApplicationProperties.Add("NotificationType", emailNotification.NotificationType);
            message.ApplicationProperties.Add("Priority", emailNotification.Priority);
            message.ApplicationProperties.Add("ToEmail", emailNotification.ToEmail);

            if (emailNotification.RequestId.HasValue)
            {
                message.ApplicationProperties.Add("RequestId", emailNotification.RequestId.Value);
            }
            if (!string.IsNullOrEmpty(emailNotification.Module))
            {
                message.ApplicationProperties.Add("Module", emailNotification.Module);
            }
            if (!string.IsNullOrEmpty(emailNotification.Trigger))
            {
                message.ApplicationProperties.Add("Trigger", emailNotification.Trigger);
            }

            // Step 2: Send message to Azure Service Bus queue
            await _sender.SendMessageAsync(message);
            _ecmLogger.LogServiceBus(true, "sent to", _queueName, message.MessageId);

            // Step 3: Update NotificationQueue status to "Queued" (only if we logged it)
            if (notificationLog != null)
            {
                notificationLog.Status = "Queued";
                notificationLog.LastAttempt = DateTime.UtcNow;
                notificationLog.AttemptCount = 1;
                _context.NotificationQueue.Update(notificationLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[AZURE SERVICE BUS] Successfully queued email notification - MessageId: {MessageId}, NotificationId: {NotificationId}",
                    message.MessageId, notificationLog.Id);
                _ecmLogger.LogEmailNotification(
                    true,
                    "Queue",
                    emailNotification.ToEmail,
                    emailNotification.Subject,
                    null);
            }
            else
            {
                _logger.LogInformation("[AZURE SERVICE BUS] Successfully queued email notification to Service Bus (no NotificationQueue log) - MessageId: {MessageId}",
                    message.MessageId);
                _ecmLogger.LogEmailNotification(
                    true,
                    "Queue",
                    emailNotification.ToEmail,
                    emailNotification.Subject,
                    null);
            }

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Email notification queued successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE SERVICE BUS] Failed to queue email notification to {ToEmail}", emailNotification.ToEmail);
            _ecmLogger.LogServiceBus(false, "send to", _queueName, errorMessage: $"Failed to queue email to {emailNotification.ToEmail}: {ex.Message}");
            _ecmLogger.LogEmailNotification(
                false,
                "Queue",
                emailNotification.ToEmail,
                emailNotification.Subject,
                ex.Message);

            // Step 4: Log failure to NotificationQueue if logging was created
            if (notificationLog != null)
            {
                try
                {
                    notificationLog.Status = "Failed";
                    notificationLog.ErrorMessage = ex.Message;
                    notificationLog.LastAttempt = DateTime.UtcNow;
                    notificationLog.AttemptCount = notificationLog.AttemptCount + 1;
                    _context.NotificationQueue.Update(notificationLog);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("[AZURE SERVICE BUS] Logged failure to NotificationQueue - NotificationId: {NotificationId}", notificationLog.Id);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "[AZURE SERVICE BUS] Failed to log error to NotificationQueue");
                    _ecmLogger.LogServiceBus(false, "log to NotificationQueue", _queueName, errorMessage: logEx.Message);
                }
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = "Failed to queue email notification",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Send multiple email notifications to the Azure Service Bus queue
    /// </summary>
    public async Task<ApiResponse<int>> SendBulkEmailNotificationsAsync(List<EmailNotificationDto> emailNotifications)
    {
        int successCount = 0;
        var errors = new List<string>();

        try
        {
            _logger.LogInformation("[AZURE SERVICE BUS] Queueing {Count} email notifications in bulk", emailNotifications.Count);
            _ecmLogger.LogServiceBus(true, $"starting bulk queue ({emailNotifications.Count} emails)", _queueName);

            // Create a batch of messages
            using ServiceBusMessageBatch messageBatch = await _sender.CreateMessageBatchAsync();

            foreach (var emailNotification in emailNotifications)
            {
                try
                {
                    var messageBody = JsonSerializer.Serialize(emailNotification);
                    var message = new ServiceBusMessage(messageBody)
                    {
                        ContentType = "application/json",
                        Subject = emailNotification.NotificationType,
                        MessageId = Guid.NewGuid().ToString()
                    };

                    // Add custom properties for filtering and routing in Power Automate
                    message.ApplicationProperties.Add("NotificationType", emailNotification.NotificationType);
                    message.ApplicationProperties.Add("Priority", emailNotification.Priority);
                    message.ApplicationProperties.Add("ToEmail", emailNotification.ToEmail);

                    if (emailNotification.RequestId.HasValue)
                    {
                        message.ApplicationProperties.Add("RequestId", emailNotification.RequestId.Value);
                    }
                    if (!string.IsNullOrEmpty(emailNotification.Module))
                    {
                        message.ApplicationProperties.Add("Module", emailNotification.Module);
                    }
                    if (!string.IsNullOrEmpty(emailNotification.Trigger))
                    {
                        message.ApplicationProperties.Add("Trigger", emailNotification.Trigger);
                    }

                    // Try to add message to batch
                    if (!messageBatch.TryAddMessage(message))
                    {
                        // Batch is full, send current batch and create new one
                        if (messageBatch.Count > 0)
                        {
                            await _sender.SendMessagesAsync(messageBatch);
                            successCount += messageBatch.Count;
                            _logger.LogInformation("[AZURE SERVICE BUS] Sent batch of {Count} messages", messageBatch.Count);
                        }

                        // Start new batch with current message
                        using ServiceBusMessageBatch newBatch = await _sender.CreateMessageBatchAsync();
                        if (!newBatch.TryAddMessage(message))
                        {
                            errors.Add($"Message too large for batch: {emailNotification.ToEmail}");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AZURE SERVICE BUS] Failed to add message to batch for {ToEmail}", emailNotification.ToEmail);
                    errors.Add($"Failed to queue message for {emailNotification.ToEmail}: {ex.Message}");
                }
            }

            // Send any remaining messages in the batch
            if (messageBatch.Count > 0)
            {
                await _sender.SendMessagesAsync(messageBatch);
                successCount += messageBatch.Count;
                _logger.LogInformation("[AZURE SERVICE BUS] Sent final batch of {Count} messages", messageBatch.Count);
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Successfully queued {SuccessCount}/{TotalCount} email notifications",
                successCount, emailNotifications.Count);
            _ecmLogger.LogServiceBus(true, $"bulk queue completed ({successCount}/{emailNotifications.Count} emails)", _queueName);

            return new ApiResponse<int>
            {
                Success = successCount > 0,
                Data = successCount,
                Message = $"Queued {successCount}/{emailNotifications.Count} email notifications",
                Errors = errors.Any() ? errors : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE SERVICE BUS] Failed to queue bulk email notifications");
            _ecmLogger.LogServiceBus(false, $"bulk queue ({successCount}/{emailNotifications.Count} succeeded before failure)", _queueName, errorMessage: ex.Message);

            return new ApiResponse<int>
            {
                Success = false,
                Data = successCount,
                Message = "Failed to queue bulk email notifications",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Send an email using an EmailTemplate by template name
    /// Retrieves Body field codes from EmailContentMappers, maps fields to request data,
    /// generates HTML email body with replaced subject placeholders, and sends via Azure Service Bus
    ///
    /// Process:
    /// 1. Load template (Body with ContentStyling='TableRow')
    /// 2. Parse Body field to get ContentCodes (comma-delimited)
    /// 3. Query EmailContentMappers to get ContentField names for Body
    /// 4. Extract @-prefixed codes from Subject
    /// 5. Query EmailContentMappers to get ContentField names for Subject
    /// 6. Map all ContentFields to request data
    /// 7. Build HTML email body
    /// 8. Replace Subject placeholders with mapped values
    /// 9. Send via Azure Service Bus
    /// </summary>
    public async Task<ApiResponse<bool>> SendEmailFromTemplateNameAsync(
        string templateName,
        CreateNewHireRequestDto requestData,
        string toEmail,
        string? ccEmail = null,
        int? requestId = null)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(templateName))
            {
                _logger.LogError("[AZURE SERVICE BUS] Template name is required");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Template name is required",
                    Errors = new List<string> { "Template name cannot be empty" }
                };
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogError("[AZURE SERVICE BUS] To email address is required");
                _ecmLogger.LogEmailNotification(false, templateName, "(empty)", "N/A", "No recipient - ToEmail is empty or null");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "To email address is required",
                    Errors = new List<string> { "To email cannot be empty" }
                };
            }

            if (requestData == null)
            {
                _logger.LogError("[AZURE SERVICE BUS] Request data is required");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Request data is required",
                    Errors = new List<string> { "Request data cannot be null" }
                };
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Starting email template processing: {TemplateName}", templateName);

            // Step 1: Load the email template from database (with ContentStyling='TableRow' for Body)
            var template = await _context.EmailTemplates
                .Where(t => t.TemplateName == templateName && t.RequestType == "NEWHIRE" && t.IsActive && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (template == null)
            {
                _logger.LogError("[AZURE SERVICE BUS] Email template '{TemplateName}' not found for NEWHIRE", templateName);
                _ecmLogger.LogEmailNotification(false, templateName, toEmail, "N/A", $"NEWHIRE email template '{templateName}' not found or inactive");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Email template '{templateName}' not found",
                    Errors = new List<string> { "Template not found" }
                };
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Template loaded - ContentStyling: {ContentStyling}, Body: {Body}",
                template.ContentStyling, template.Body);

            // Step 2: Parse Body field to get list of ContentCodes based on ContentStyling
            // For Text style: extract $PLACEHOLDER patterns from HTML
            // For TableRow style: parse comma-delimited codes
            List<string> bodyContentCodes;

            if (!string.IsNullOrEmpty(template.ContentStyling) && template.ContentStyling.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                // ContentStyling is "Text" - extract $CODE patterns from HTML body
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'Text' - extracting $CODE placeholders from Body");
                bodyContentCodes = ExtractBodyFieldCodesForText(template.Body);
            }
            else
            {
                // ContentStyling is "TableRow" (default) - parse comma-delimited codes
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'TableRow' or empty - parsing comma-delimited codes from Body");
                bodyContentCodes = ParseBodyFieldCodes(template.Body);
            }

            if (!bodyContentCodes.Any())
            {
                _logger.LogError("[AZURE SERVICE BUS] No Body field codes found in template '{TemplateName}'", templateName);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Template Body is empty or invalid",
                    Errors = new List<string> { "No field codes found in template body" }
                };
            }

            // Step 3: Query EmailContentMappers to get ContentField names for Body
            var bodyContentFieldMappings = await GetBodyContentFieldMappingsAsync(bodyContentCodes, "NEWHIRE");

            if (!bodyContentFieldMappings.Any())
            {
                _logger.LogWarning("[AZURE SERVICE BUS] No Body ContentField mappings found for template '{TemplateName}'", templateName);
            }

            // Step 3b: Query EmailContentMappers to get ContentLabel mappings for Body
            var bodyContentLabelMappings = await GetBodyContentLabelMappingsAsync(bodyContentCodes, "NEWHIRE");

            // Get list of ContentField names to pass to mapper service
            var bodyContentFields = bodyContentFieldMappings.Values.ToList();

            // Step 4: Extract @-prefixed codes from Subject
            var subjectContentCodes = ExtractSubjectFieldCodes(template.Subject);
            _logger.LogInformation("[AZURE SERVICE BUS] Subject content codes extracted: {Count}", subjectContentCodes.Count);

            // Step 5: Query EmailContentMappers to get ContentField names for Subject
            var subjectContentFieldMappings = new Dictionary<string, string>();
            if (subjectContentCodes.Any())
            {
                subjectContentFieldMappings = await GetSubjectContentFieldMappingsAsync(subjectContentCodes, "NEWHIRE");
                if (!subjectContentFieldMappings.Any())
                {
                    _logger.LogWarning("[AZURE SERVICE BUS] No Subject ContentField mappings found for template '{TemplateName}'", templateName);
                }
            }

            // Combine all ContentFields to map at once
            var allContentFields = bodyContentFields
                .Union(subjectContentFieldMappings.Values)
                .Distinct()
                .ToList();

            // Step 6: Map all ContentFields to actual data from the request
            _logger.LogInformation("[AZURE SERVICE BUS] Mapping {Count} content fields to request data", allContentFields.Count);
            var fieldData = await _fieldMapperService.MapNewHireFieldsToDataAsync(requestData, allContentFields);

            _logger.LogInformation("[AZURE SERVICE BUS] Mapped {MappedCount}/{TotalCount} fields to request data",
                fieldData.Count, allContentFields.Count);

            // Inject ECM Link if referenced in template content fields
            var ecmLinkField = allContentFields.FirstOrDefault(f =>
                f.Equals("ecmlink", StringComparison.OrdinalIgnoreCase) ||
                f.Equals("ecm", StringComparison.OrdinalIgnoreCase) ||
                f.Equals("ecm link", StringComparison.OrdinalIgnoreCase));
            if (ecmLinkField != null)
            {
                var frontendBaseUrl = _configuration["EcmApp:FrontendBaseUrl"];
                if (!string.IsNullOrEmpty(frontendBaseUrl))
                {
                    var ecmUrl = frontendBaseUrl.TrimEnd('/');
                    fieldData[ecmLinkField] = $"<a href='{ecmUrl}' style='color: #0066cc;'>View in ECM</a>";
                    _logger.LogInformation("[AZURE SERVICE BUS] Injected ECM Link: {EcmUrl}", ecmUrl);
                }
                else
                {
                    _logger.LogWarning("[AZURE SERVICE BUS] EcmApp:FrontendBaseUrl not configured, cannot generate ECM Link");
                }
            }

            // Step 7: Build HTML email body from template and field data
            // ContentStyling determines how the body is processed:
            // - "TableRow" (default): Generate formatted table with field labels and values via BuildEmailBodyFromTemplate()
            // - "Text": Use pre-formatted HTML body with $PLACEHOLDER replacement
            string emailBody;

            if (!string.IsNullOrEmpty(template.ContentStyling) && template.ContentStyling.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                // ContentStyling is "Text" - use pre-formatted HTML with placeholder replacement
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'Text' - using HTML body with placeholder replacement");
                emailBody = ReplaceBodyPlaceholders(template.Body, fieldData, bodyContentFieldMappings);
            }
            else
            {
                // ContentStyling is "TableRow" (default) - use the template builder to generate formatted table
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is '{Style}' - using template builder for formatted table",
                    string.IsNullOrEmpty(template.ContentStyling) ? "TableRow (default)" : template.ContentStyling);

                emailBody = _templateBuilderService.BuildEmailBodyFromTemplate(
                    template,
                    fieldData,
                    bodyContentFieldMappings,
                    bodyContentLabelMappings);
            }

            if (string.IsNullOrWhiteSpace(emailBody))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Generated email body is empty for template '{TemplateName}'", templateName);
            }

            // Step 8: Replace Subject placeholders (@CODE) with actual mapped values
            var emailSubject = await ReplaceSubjectPlaceholdersAsync(
                template.Subject,
                subjectContentCodes,
                subjectContentFieldMappings,
                requestData);

            if (string.IsNullOrWhiteSpace(emailSubject))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Generated email subject is empty for template '{TemplateName}'", templateName);
                emailSubject = template.Subject; // Fallback to original if replacement failed
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Email built successfully - Subject: {Subject}", emailSubject);

            // Step 9: Create EmailNotificationDto and send via Azure Service Bus
            var emailNotification = new EmailNotificationDto
            {
                RequestId = requestId,
                ToEmail = toEmail,
                CcEmail = ccEmail,
                Subject = emailSubject,
                Body = emailBody,
                NotificationType = template.EmailType,
                Priority = template.TriggerType == "Immediate" ? 1 : 2,
                Module = template.RequestType,
                Trigger = template.TriggerType,
                TemplateId = template.Id
            };

            // Send the email notification
            var result = await SendEmailNotificationAsync(emailNotification);

            if (result.Success)
            {
                _logger.LogInformation("[AZURE SERVICE BUS] Email sent successfully using template {TemplateName}", templateName);
                _ecmLogger.LogEmailNotification(
                    true,
                    "TemplateProcessing",
                    toEmail,
                    emailSubject,
                    null);
            }
            else
            {
                _logger.LogError("[AZURE SERVICE BUS] Failed to send email using template {TemplateName}: {Message}",
                    templateName, result.Message);
                _ecmLogger.LogEmailNotification(
                    false,
                    "TemplateProcessing",
                    toEmail,
                    emailSubject,
                    result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE SERVICE BUS] Error sending email from template {TemplateName}", templateName);
            _ecmLogger.LogError(LogCategory.ServiceBus, ex, $"Error sending email from template '{templateName}' to {toEmail}");
            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Error sending email from template '{templateName}'",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Replace @CODE placeholders in subject with actual values from mapped promotion field data
    /// </summary>
    private async Task<string> ReplaceSubjectPlaceholdersForPromotionAsync(string subject,
        List<string> subjectCodes,
        Dictionary<string, string> subjectFieldMappings,
        CreatePromotionRequestDto requestData)
    {
        if (string.IsNullOrWhiteSpace(subject) || !subjectCodes.Any())
        {
            return subject;
        }

        var replacedSubject = subject;

        foreach (var code in subjectCodes)
        {
            if (!subjectFieldMappings.ContainsKey(code))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Skipping subject placeholder @@{Code} - no ContentField mapping found", code);
                continue;
            }

            var contentField = subjectFieldMappings[code];

            // Map single field to get its value
            var fieldData = await _fieldMapperService.MapPromotionFieldsToDataAsync(
                requestData,
                new List<string> { contentField });

            if (fieldData.ContainsKey(contentField))
            {
                var fieldValue = fieldData[contentField]?.ToString() ?? string.Empty;
                var placeholder = $"@@{code}";
                replacedSubject = replacedSubject.Replace(placeholder, fieldValue);

                _logger.LogInformation("[AZURE SERVICE BUS] Replaced subject placeholder '{Placeholder}' with value '{Value}'",
                    placeholder, fieldValue);
            }
            else
            {
                _logger.LogWarning("[AZURE SERVICE BUS] ContentField '{Field}' not found in mapped data for subject code '{Code}'",
                    contentField, code);
            }
        }

        // Replace [currentdate] placeholder with today's date in MM/dd/yyyy format
        var currentDatePattern = @"\[currentdate\]";
        var currentDateMatches = System.Text.RegularExpressions.Regex.Matches(
            replacedSubject, currentDatePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (currentDateMatches.Count > 0)
        {
            var formattedDate = DateTime.Now.ToString("MM/dd/yyyy");
            replacedSubject = System.Text.RegularExpressions.Regex.Replace(
                replacedSubject, currentDatePattern, formattedDate,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            _logger.LogInformation("[AZURE SERVICE BUS] Replaced [currentdate] placeholder with {Date}",
                formattedDate);
        }

        _logger.LogInformation("[AZURE SERVICE BUS] Subject placeholder replacement complete");
        return replacedSubject;
    }

    /// <summary>
    /// Send an email using an EmailTemplate by template name for Promotion requests
    /// Retrieves Body field codes from EmailContentMappers, maps fields to request data,
    /// generates HTML email body with replaced subject placeholders, and sends via Azure Service Bus
    ///
    /// Process:
    /// 1. Load template (Body with ContentStyling='TableRow')
    /// 2. Parse Body field to get ContentCodes (comma-delimited)
    /// 3. Query EmailContentMappers to get ContentField names for Body
    /// 4. Extract @-prefixed codes from Subject
    /// 5. Query EmailContentMappers to get ContentField names for Subject
    /// 6. Map all ContentFields to promotion request data
    /// 7. Build HTML email body
    /// 8. Replace Subject placeholders with mapped values
    /// 9. Send via Azure Service Bus
    /// </summary>
    public async Task<ApiResponse<bool>> SendEmailFromTemplateNameForPromotionAsync(
        string templateName,
        CreatePromotionRequestDto requestData,
        string toEmail,
        string? ccEmail = null,
        int? requestId = null)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(templateName))
            {
                _logger.LogError("[AZURE SERVICE BUS] Template name is required");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Template name is required",
                    Errors = new List<string> { "Template name cannot be empty" }
                };
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogError("[AZURE SERVICE BUS] To email address is required");
                _ecmLogger.LogEmailNotification(false, templateName, "(empty)", "N/A", "No recipient - ToEmail is empty or null");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "To email address is required",
                    Errors = new List<string> { "To email cannot be empty" }
                };
            }

            if (requestData == null)
            {
                _logger.LogError("[AZURE SERVICE BUS] Request data is required");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Request data is required",
                    Errors = new List<string> { "Request data cannot be null" }
                };
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Starting promotion email template processing: {TemplateName}", templateName);

            // Step 1: Load the email template from database (with RequestType='PROMOTION' filter)
            var template = await _context.EmailTemplates
                .Where(t => t.TemplateName == templateName && t.RequestType == "PROMOTION" && t.IsActive && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (template == null)
            {
                _logger.LogError("[AZURE SERVICE BUS] Email template '{TemplateName}' not found", templateName);
                _ecmLogger.LogEmailNotification(false, templateName, toEmail, "N/A", $"Promotion email template '{templateName}' not found or inactive");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Email template '{templateName}' not found",
                    Errors = new List<string> { "Template not found" }
                };
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Template loaded - ContentStyling: {ContentStyling}, Body: {Body}",
                template.ContentStyling, template.Body);

            // Step 2: Parse Body field to get list of ContentCodes based on ContentStyling
            // For Text style: extract $PLACEHOLDER patterns from HTML
            // For TableRow style: parse comma-delimited codes
            List<string> bodyContentCodes;

            if (!string.IsNullOrEmpty(template.ContentStyling) && template.ContentStyling.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                // ContentStyling is "Text" - extract $CODE patterns from HTML body
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'Text' - extracting $CODE placeholders from Body");
                bodyContentCodes = ExtractBodyFieldCodesForText(template.Body);
            }
            else
            {
                // ContentStyling is "TableRow" (default) - parse comma-delimited codes
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'TableRow' or empty - parsing comma-delimited codes from Body");
                bodyContentCodes = ParseBodyFieldCodes(template.Body);
            }

            if (!bodyContentCodes.Any())
            {
                _logger.LogError("[AZURE SERVICE BUS] No Body field codes found in template '{TemplateName}'", templateName);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Template Body is empty or invalid",
                    Errors = new List<string> { "No field codes found in template body" }
                };
            }

            // Step 3: Query EmailContentMappers to get ContentField names for Body (filter by PROMOTION source)
            var bodyContentFieldMappings = await GetBodyContentFieldMappingsAsync(bodyContentCodes, "PROMOTION");

            if (!bodyContentFieldMappings.Any())
            {
                _logger.LogWarning("[AZURE SERVICE BUS] No Body ContentField mappings found for template '{TemplateName}'", templateName);
            }

            // Step 3b: Query EmailContentMappers to get ContentLabel mappings for Body (filter by PROMOTION source)
            var bodyContentLabelMappings = await GetBodyContentLabelMappingsAsync(bodyContentCodes, "PROMOTION");

            // Get list of ContentField names to pass to mapper service
            var bodyContentFields = bodyContentFieldMappings.Values.ToList();

            // Step 4: Extract @-prefixed codes from Subject
            var subjectContentCodes = ExtractSubjectFieldCodes(template.Subject);
            _logger.LogInformation("[AZURE SERVICE BUS] Subject content codes extracted: {Count}", subjectContentCodes.Count);

            // Step 5: Query EmailContentMappers to get ContentField names for Subject (filter by PROMOTION source)
            var subjectContentFieldMappings = new Dictionary<string, string>();
            if (subjectContentCodes.Any())
            {
                subjectContentFieldMappings = await GetSubjectContentFieldMappingsAsync(subjectContentCodes, "PROMOTION");
                if (!subjectContentFieldMappings.Any())
                {
                    _logger.LogWarning("[AZURE SERVICE BUS] No Subject ContentField mappings found for template '{TemplateName}'", templateName);
                }
            }

            // Combine all ContentFields to map at once
            var allContentFields = bodyContentFields
                .Union(subjectContentFieldMappings.Values)
                .Distinct()
                .ToList();

            // Step 6: Map all ContentFields to actual data from the promotion request
            _logger.LogInformation("[AZURE SERVICE BUS] Mapping {Count} content fields to promotion request data", allContentFields.Count);
            var fieldData = await _fieldMapperService.MapPromotionFieldsToDataAsync(requestData, allContentFields);

            _logger.LogInformation("[AZURE SERVICE BUS] Mapped {MappedCount}/{TotalCount} fields to promotion request data",
                fieldData.Count, allContentFields.Count);

            // Step 7: Build HTML email body from template and field data
            // ContentStyling determines how the body is processed:
            // - "TableRow" (default): Generate formatted table with field labels and values via BuildEmailBodyFromTemplate()
            // - "Text": Use pre-formatted HTML body with $PLACEHOLDER replacement
            string emailBody;

            if (!string.IsNullOrEmpty(template.ContentStyling) && template.ContentStyling.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                // ContentStyling is "Text" - use pre-formatted HTML with placeholder replacement
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'Text' - using HTML body with placeholder replacement");
                emailBody = ReplaceBodyPlaceholders(template.Body, fieldData, bodyContentFieldMappings);
            }
            else
            {
                // ContentStyling is "TableRow" (default) - use the template builder to generate formatted table
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is '{Style}' - using template builder for formatted table",
                    string.IsNullOrEmpty(template.ContentStyling) ? "TableRow (default)" : template.ContentStyling);

                emailBody = _templateBuilderService.BuildEmailBodyFromTemplate(
                    template,
                    fieldData,
                    bodyContentFieldMappings,
                    bodyContentLabelMappings);
            }

            if (string.IsNullOrWhiteSpace(emailBody))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Generated email body is empty for template '{TemplateName}'", templateName);
            }

            // Step 8: Replace Subject placeholders (@CODE) with actual mapped values
            var emailSubject = await ReplaceSubjectPlaceholdersForPromotionAsync(
                template.Subject,
                subjectContentCodes,
                subjectContentFieldMappings,
                requestData);

            if (string.IsNullOrWhiteSpace(emailSubject))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Generated email subject is empty for template '{TemplateName}'", templateName);
                emailSubject = template.Subject; // Fallback to original if replacement failed
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Promotion email built successfully - Subject: {Subject}", emailSubject);

            // Step 9: Create EmailNotificationDto and send via Azure Service Bus
            var emailNotification = new EmailNotificationDto
            {
                RequestId = requestId,
                ToEmail = toEmail,
                CcEmail = ccEmail,
                Subject = emailSubject,
                Body = emailBody,
                NotificationType = template.EmailType,
                Priority = template.TriggerType == "Immediate" ? 1 : 2,
                Module = template.RequestType,
                Trigger = template.TriggerType,
                TemplateId = template.Id
            };

            // Send the email notification
            var result = await SendEmailNotificationAsync(emailNotification);

            if (result.Success)
            {
                _logger.LogInformation("[AZURE SERVICE BUS] Promotion email sent successfully using template {TemplateName}", templateName);
                _ecmLogger.LogEmailNotification(
                    true,
                    "PromotionTemplateProcessing",
                    toEmail,
                    emailSubject,
                    null);
            }
            else
            {
                _logger.LogError("[AZURE SERVICE BUS] Failed to send promotion email using template {TemplateName}: {Message}",
                    templateName, result.Message);
                _ecmLogger.LogEmailNotification(
                    false,
                    "PromotionTemplateProcessing",
                    toEmail,
                    emailSubject,
                    result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE SERVICE BUS] Error sending promotion email from template {TemplateName}", templateName);
            _ecmLogger.LogError(LogCategory.ServiceBus, ex, $"Error sending promotion email from template '{templateName}' to {toEmail}");
            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Error sending promotion email from template '{templateName}'",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Send an email using an EmailTemplate by template name for Termination requests
    /// Process flow:
    /// 1. Load EmailTemplate by TemplateName (with RequestType='TERMINATION' filter)
    /// 2. Parse Body field to get list of ContentCodes
    /// 3. Query EmailContentMappers to get ContentField names for Body
    /// 4. Extract @-prefixed codes from Subject
    /// 5. Query EmailContentMappers to get ContentField names for Subject
    /// 6. Map all ContentFields to termination request data
    /// 7. Build HTML email body
    /// 8. Replace Subject placeholders with mapped values
    /// 9. Send via Azure Service Bus
    /// </summary>
    public async Task<ApiResponse<bool>> SendEmailFromTemplateNameForTerminationAsync(
        string templateName,
        TerminationEmailDataDto requestData,
        string toEmail,
        string? ccEmail = null,
        int? requestId = null)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(templateName))
            {
                _logger.LogError("[AZURE SERVICE BUS] Template name is required");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Template name is required",
                    Errors = new List<string> { "Template name cannot be empty" }
                };
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogError("[AZURE SERVICE BUS] To email address is required");
                _ecmLogger.LogEmailNotification(false, templateName, "(empty)", "N/A", "No recipient - ToEmail is empty or null");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "To email address is required",
                    Errors = new List<string> { "To email cannot be empty" }
                };
            }

            if (requestData == null)
            {
                _logger.LogError("[AZURE SERVICE BUS] Request data is required");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Request data is required",
                    Errors = new List<string> { "Request data cannot be null" }
                };
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Starting termination email template processing: {TemplateName}", templateName);

            // Step 1: Load the email template from database (with RequestType='TERMINATION' filter)
            var template = await _context.EmailTemplates
                .Where(t => t.TemplateName == templateName && t.RequestType == "TERMINATION" && t.IsActive && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (template == null)
            {
                _logger.LogError("[AZURE SERVICE BUS] Email template '{TemplateName}' not found", templateName);
                _ecmLogger.LogEmailNotification(false, templateName, toEmail, "N/A", $"Termination email template '{templateName}' not found or inactive");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Email template '{templateName}' not found",
                    Errors = new List<string> { "Template not found" }
                };
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Template loaded - ContentStyling: {ContentStyling}, Body: {Body}",
                template.ContentStyling, template.Body);

            // Step 2: Parse Body field to get list of ContentCodes based on ContentStyling
            List<string> bodyContentCodes;

            if (!string.IsNullOrEmpty(template.ContentStyling) && template.ContentStyling.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'Text' - extracting $CODE placeholders from Body");
                bodyContentCodes = ExtractBodyFieldCodesForText(template.Body);
            }
            else
            {
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'TableRow' or empty - parsing comma-delimited codes from Body");
                bodyContentCodes = ParseBodyFieldCodes(template.Body);
            }

            if (!bodyContentCodes.Any())
            {
                _logger.LogError("[AZURE SERVICE BUS] No Body field codes found in template '{TemplateName}'", templateName);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Template Body is empty or invalid",
                    Errors = new List<string> { "No field codes found in template body" }
                };
            }

            // Step 3: Query EmailContentMappers to get ContentField names for Body (filter by TERMINATION source)
            var bodyContentFieldMappings = await GetBodyContentFieldMappingsAsync(bodyContentCodes, "TERMINATION");

            if (!bodyContentFieldMappings.Any())
            {
                _logger.LogWarning("[AZURE SERVICE BUS] No Body ContentField mappings found for template '{TemplateName}'", templateName);
            }

            // Step 3b: Query EmailContentMappers to get ContentLabel mappings for Body (filter by TERMINATION source)
            var bodyContentLabelMappings = await GetBodyContentLabelMappingsAsync(bodyContentCodes, "TERMINATION");

            // Get list of ContentField names to pass to mapper service
            var bodyContentFields = bodyContentFieldMappings.Values.ToList();

            // Step 4: Extract @-prefixed codes from Subject
            var subjectContentCodes = ExtractSubjectFieldCodes(template.Subject);
            _logger.LogInformation("[AZURE SERVICE BUS] Subject content codes extracted: {Count}", subjectContentCodes.Count);

            // Step 5: Query EmailContentMappers to get ContentField names for Subject (filter by TERMINATION source)
            var subjectContentFieldMappings = new Dictionary<string, string>();
            if (subjectContentCodes.Any())
            {
                subjectContentFieldMappings = await GetSubjectContentFieldMappingsAsync(subjectContentCodes, "TERMINATION");
                if (!subjectContentFieldMappings.Any())
                {
                    _logger.LogWarning("[AZURE SERVICE BUS] No Subject ContentField mappings found for template '{TemplateName}'", templateName);
                }
            }

            // Combine all ContentFields to map at once
            var allContentFields = bodyContentFields
                .Union(subjectContentFieldMappings.Values)
                .Distinct()
                .ToList();

            // Step 6: Map all ContentFields to actual data from the termination request
            _logger.LogInformation("[AZURE SERVICE BUS] Mapping {Count} content fields to termination request data", allContentFields.Count);
            var fieldData = await _fieldMapperService.MapTerminationFieldsToDataAsync(requestData, allContentFields);

            _logger.LogInformation("[AZURE SERVICE BUS] Mapped {MappedCount}/{TotalCount} fields to termination request data",
                fieldData.Count, allContentFields.Count);

            // Step 7: Build HTML email body from template and field data
            string emailBody;

            if (!string.IsNullOrEmpty(template.ContentStyling) && template.ContentStyling.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'Text' - using HTML body with placeholder replacement");
                emailBody = ReplaceBodyPlaceholders(template.Body, fieldData, bodyContentFieldMappings);
            }
            else
            {
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is '{Style}' - using template builder for formatted table",
                    string.IsNullOrEmpty(template.ContentStyling) ? "TableRow (default)" : template.ContentStyling);

                emailBody = _templateBuilderService.BuildEmailBodyFromTemplate(
                    template,
                    fieldData,
                    bodyContentFieldMappings,
                    bodyContentLabelMappings);
            }

            if (string.IsNullOrWhiteSpace(emailBody))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Generated email body is empty for template '{TemplateName}'", templateName);
            }

            // Step 8: Replace Subject placeholders (@CODE) with actual mapped values
            var emailSubject = ReplaceSubjectPlaceholdersGeneric(
                template.Subject,
                subjectContentCodes,
                subjectContentFieldMappings,
                fieldData);

            if (string.IsNullOrWhiteSpace(emailSubject))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Generated email subject is empty for template '{TemplateName}'", templateName);
                emailSubject = template.Subject; // Fallback to original if replacement failed
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Termination email built successfully - Subject: {Subject}", emailSubject);

            // Step 9: Create EmailNotificationDto and send via Azure Service Bus
            var emailNotification = new EmailNotificationDto
            {
                RequestId = requestId,
                ToEmail = toEmail,
                CcEmail = ccEmail,
                Subject = emailSubject,
                Body = emailBody,
                NotificationType = template.EmailType,
                Priority = template.TriggerType == "Immediate" ? 1 : 2,
                Module = template.RequestType,
                Trigger = template.TriggerType,
                TemplateId = template.Id
            };

            // Send the email notification
            var result = await SendEmailNotificationAsync(emailNotification);

            if (result.Success)
            {
                _logger.LogInformation("[AZURE SERVICE BUS] Termination email sent successfully using template {TemplateName}", templateName);
                _ecmLogger.LogEmailNotification(
                    true,
                    "TerminationTemplateProcessing",
                    toEmail,
                    emailSubject,
                    null);
            }
            else
            {
                _logger.LogError("[AZURE SERVICE BUS] Failed to send termination email using template {TemplateName}: {Message}",
                    templateName, result.Message);
                _ecmLogger.LogEmailNotification(
                    false,
                    "TerminationTemplateProcessing",
                    toEmail,
                    emailSubject,
                    result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE SERVICE BUS] Error sending termination email from template {TemplateName}", templateName);
            _ecmLogger.LogError(LogCategory.ServiceBus, ex, $"Error sending termination email from template '{templateName}' to {toEmail}");
            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Error sending termination email from template '{templateName}'",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Send an email using an EmailTemplate by template name for Layoff requests
    /// Steps:
    /// 1. Load EmailTemplate by TemplateName and RequestType='LAYOFF'
    /// 2. Parse Body field to get ContentCode list
    /// 3. Query EmailContentMappers to get ContentField names (filter by ContentSource='LAYOFF')
    /// 4. Map ContentFields to actual data using IEmailFieldMapperService.MapLayoffFieldsToDataAsync
    /// 5. Build HTML email body from template and field data
    /// 6. Create EmailNotificationDto with the generated content
    /// 7. Send via Azure Service Bus
    /// </summary>
    public async Task<ApiResponse<bool>> SendEmailFromTemplateNameForLayoffAsync(
        string templateName,
        LayoffEmailDataDto requestData,
        string toEmail,
        string? ccEmail = null,
        int? requestId = null)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(templateName))
            {
                _logger.LogError("[AZURE SERVICE BUS] Template name is required");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Template name is required",
                    Errors = new List<string> { "Template name cannot be empty" }
                };
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogError("[AZURE SERVICE BUS] To email address is required");
                _ecmLogger.LogEmailNotification(false, templateName, "(empty)", "N/A", "No recipient - ToEmail is empty or null");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "To email address is required",
                    Errors = new List<string> { "To email cannot be empty" }
                };
            }

            if (requestData == null)
            {
                _logger.LogError("[AZURE SERVICE BUS] Request data is required");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Request data is required",
                    Errors = new List<string> { "Request data cannot be null" }
                };
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Starting layoff email template processing: {TemplateName}", templateName);

            // Step 1: Load the email template from database (with RequestType='LAYOFF' filter)
            var template = await _context.EmailTemplates
                .Where(t => t.TemplateName == templateName && t.RequestType == "LAYOFF" && t.IsActive && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (template == null)
            {
                _logger.LogError("[AZURE SERVICE BUS] Email template '{TemplateName}' not found", templateName);
                _ecmLogger.LogEmailNotification(false, templateName, toEmail, "N/A", $"Layoff email template '{templateName}' not found or inactive");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Email template '{templateName}' not found",
                    Errors = new List<string> { "Template not found" }
                };
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Template loaded - ContentStyling: {ContentStyling}, Body: {Body}",
                template.ContentStyling, template.Body);

            // Step 2: Parse Body field to get list of ContentCodes based on ContentStyling
            List<string> bodyContentCodes;

            if (!string.IsNullOrEmpty(template.ContentStyling) && template.ContentStyling.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'Text' - extracting $CODE placeholders from Body");
                bodyContentCodes = ExtractBodyFieldCodesForText(template.Body);
            }
            else
            {
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'TableRow' or empty - parsing comma-delimited codes from Body");
                bodyContentCodes = ParseBodyFieldCodes(template.Body);
            }

            if (!bodyContentCodes.Any())
            {
                _logger.LogError("[AZURE SERVICE BUS] No Body field codes found in template '{TemplateName}'", templateName);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Template Body is empty or invalid",
                    Errors = new List<string> { "No field codes found in template body" }
                };
            }

            // Step 3: Query EmailContentMappers to get ContentField names for Body (filter by LAYOFF source)
            var bodyContentFieldMappings = await GetBodyContentFieldMappingsAsync(bodyContentCodes, "LAYOFF");

            if (!bodyContentFieldMappings.Any())
            {
                _logger.LogWarning("[AZURE SERVICE BUS] No Body ContentField mappings found for template '{TemplateName}'", templateName);
            }

            // Step 3b: Query EmailContentMappers to get ContentLabel mappings for Body (filter by LAYOFF source)
            var bodyContentLabelMappings = await GetBodyContentLabelMappingsAsync(bodyContentCodes, "LAYOFF");

            // Get list of ContentField names to pass to mapper service
            var bodyContentFields = bodyContentFieldMappings.Values.ToList();

            // Step 4: Extract @-prefixed codes from Subject
            var subjectContentCodes = ExtractSubjectFieldCodes(template.Subject);
            _logger.LogInformation("[AZURE SERVICE BUS] Subject content codes extracted: {Count}", subjectContentCodes.Count);

            // Step 5: Query EmailContentMappers to get ContentField names for Subject (filter by LAYOFF source)
            var subjectContentFieldMappings = new Dictionary<string, string>();
            if (subjectContentCodes.Any())
            {
                subjectContentFieldMappings = await GetSubjectContentFieldMappingsAsync(subjectContentCodes, "LAYOFF");
                if (!subjectContentFieldMappings.Any())
                {
                    _logger.LogWarning("[AZURE SERVICE BUS] No Subject ContentField mappings found for template '{TemplateName}'", templateName);
                }
            }

            // Combine all ContentFields to map at once
            var allContentFields = bodyContentFields
                .Union(subjectContentFieldMappings.Values)
                .Distinct()
                .ToList();

            // Step 6: Map all ContentFields to actual data from the layoff request
            _logger.LogInformation("[AZURE SERVICE BUS] Mapping {Count} content fields to layoff request data", allContentFields.Count);
            var fieldData = await _fieldMapperService.MapLayoffFieldsToDataAsync(requestData, allContentFields);

            _logger.LogInformation("[AZURE SERVICE BUS] Mapped {MappedCount}/{TotalCount} fields to layoff request data",
                fieldData.Count, allContentFields.Count);

            // Step 7: Build HTML email body from template and field data
            string emailBody;

            if (!string.IsNullOrEmpty(template.ContentStyling) && template.ContentStyling.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'Text' - using HTML body with placeholder replacement");
                emailBody = ReplaceBodyPlaceholders(template.Body, fieldData, bodyContentFieldMappings);
            }
            else
            {
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is '{Style}' - using template builder for formatted table",
                    string.IsNullOrEmpty(template.ContentStyling) ? "TableRow (default)" : template.ContentStyling);

                emailBody = _templateBuilderService.BuildEmailBodyFromTemplate(
                    template,
                    fieldData,
                    bodyContentFieldMappings,
                    bodyContentLabelMappings);
            }

            if (string.IsNullOrWhiteSpace(emailBody))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Generated email body is empty for template '{TemplateName}'", templateName);
            }

            // Step 8: Replace Subject placeholders (@CODE) with actual mapped values
            var emailSubject = ReplaceSubjectPlaceholdersGeneric(
                template.Subject,
                subjectContentCodes,
                subjectContentFieldMappings,
                fieldData);

            if (string.IsNullOrWhiteSpace(emailSubject))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Generated email subject is empty for template '{TemplateName}'", templateName);
                emailSubject = template.Subject; // Fallback to original if replacement failed
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Layoff email built successfully - Subject: {Subject}", emailSubject);

            // Step 9: Create EmailNotificationDto and send via Azure Service Bus
            var emailNotification = new EmailNotificationDto
            {
                RequestId = requestId,
                ToEmail = toEmail,
                CcEmail = ccEmail,
                Subject = emailSubject,
                Body = emailBody,
                NotificationType = template.EmailType,
                Priority = template.TriggerType == "Immediate" ? 1 : 2,
                Module = template.RequestType,
                Trigger = template.TriggerType,
                TemplateId = template.Id
            };

            // Send the email notification
            var result = await SendEmailNotificationAsync(emailNotification);

            if (result.Success)
            {
                _logger.LogInformation("[AZURE SERVICE BUS] Layoff email sent successfully using template {TemplateName}", templateName);
                _ecmLogger.LogEmailNotification(
                    true,
                    "LayoffTemplateProcessing",
                    toEmail,
                    emailSubject,
                    null);
            }
            else
            {
                _logger.LogError("[AZURE SERVICE BUS] Failed to send layoff email using template {TemplateName}: {Message}",
                    templateName, result.Message);
                _ecmLogger.LogEmailNotification(
                    false,
                    "LayoffTemplateProcessing",
                    toEmail,
                    emailSubject,
                    result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE SERVICE BUS] Error sending layoff email from template {TemplateName}", templateName);
            _ecmLogger.LogError(LogCategory.ServiceBus, ex, $"Error sending layoff email from template '{templateName}' to {toEmail}");
            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Error sending layoff email from template '{templateName}'",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Send an email using an EmailTemplate by template name for ReturnToWork requests
    /// Steps:
    /// 1. Load EmailTemplate by TemplateName and RequestType='RETURNTOWORK'
    /// 2. Parse Body field to get ContentCode list
    /// 3. Query EmailContentMappers to get ContentField names (filter by ContentSource='RETURNTOWORK')
    /// 4. Map ContentFields to actual data using IEmailFieldMapperService.MapReturnToWorkFieldsToDataAsync
    /// 5. Build HTML email body from template and field data
    /// 6. Create EmailNotificationDto with the generated content
    /// 7. Send via Azure Service Bus
    /// </summary>
    public async Task<ApiResponse<bool>> SendEmailFromTemplateNameForReturnToWorkAsync(
        string templateName,
        ReturnToWorkEmailDataDto requestData,
        string toEmail,
        string? ccEmail = null,
        int? requestId = null)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(templateName))
            {
                _logger.LogError("[AZURE SERVICE BUS] Template name is required");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Template name is required",
                    Errors = new List<string> { "Template name cannot be empty" }
                };
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogError("[AZURE SERVICE BUS] To email address is required");
                _ecmLogger.LogEmailNotification(false, templateName, "(empty)", "N/A", "No recipient - ToEmail is empty or null");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "To email address is required",
                    Errors = new List<string> { "To email cannot be empty" }
                };
            }

            if (requestData == null)
            {
                _logger.LogError("[AZURE SERVICE BUS] Request data is required");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Request data is required",
                    Errors = new List<string> { "Request data cannot be null" }
                };
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Starting return to work email template processing: {TemplateName}", templateName);

            // Step 1: Load the email template from database (with RequestType='RETURNTOWORK' filter)
            var template = await _context.EmailTemplates
                .Where(t => t.TemplateName == templateName && t.RequestType == "RETURNTOWORK" && t.IsActive && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (template == null)
            {
                _logger.LogError("[AZURE SERVICE BUS] Email template '{TemplateName}' not found", templateName);
                _ecmLogger.LogEmailNotification(false, templateName, toEmail, "N/A", $"Return to work email template '{templateName}' not found or inactive");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Email template '{templateName}' not found",
                    Errors = new List<string> { "Template not found" }
                };
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Template loaded - ContentStyling: {ContentStyling}, Body: {Body}",
                template.ContentStyling, template.Body);

            // Step 2: Parse Body field to get list of ContentCodes based on ContentStyling
            List<string> bodyContentCodes;

            if (!string.IsNullOrEmpty(template.ContentStyling) && template.ContentStyling.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'Text' - extracting $CODE placeholders from Body");
                bodyContentCodes = ExtractBodyFieldCodesForText(template.Body);
            }
            else
            {
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'TableRow' or empty - parsing comma-delimited codes from Body");
                bodyContentCodes = ParseBodyFieldCodes(template.Body);
            }

            if (!bodyContentCodes.Any())
            {
                _logger.LogError("[AZURE SERVICE BUS] No Body field codes found in template '{TemplateName}'", templateName);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Template Body is empty or invalid",
                    Errors = new List<string> { "No field codes found in template body" }
                };
            }

            // Step 3: Query EmailContentMappers to get ContentField names for Body (filter by RETURNTOWORK source)
            var bodyContentFieldMappings = await GetBodyContentFieldMappingsAsync(bodyContentCodes, "RETURNTOWORK");

            if (!bodyContentFieldMappings.Any())
            {
                _logger.LogWarning("[AZURE SERVICE BUS] No Body ContentField mappings found for template '{TemplateName}'", templateName);
            }

            // Step 3b: Query EmailContentMappers to get ContentLabel mappings for Body (filter by RETURNTOWORK source)
            var bodyContentLabelMappings = await GetBodyContentLabelMappingsAsync(bodyContentCodes, "RETURNTOWORK");

            // Get list of ContentField names to pass to mapper service
            var bodyContentFields = bodyContentFieldMappings.Values.ToList();

            // Step 4: Extract @-prefixed codes from Subject
            var subjectContentCodes = ExtractSubjectFieldCodes(template.Subject);
            _logger.LogInformation("[AZURE SERVICE BUS] Subject content codes extracted: {Count}", subjectContentCodes.Count);

            // Step 5: Query EmailContentMappers to get ContentField names for Subject (filter by RETURNTOWORK source)
            var subjectContentFieldMappings = new Dictionary<string, string>();
            if (subjectContentCodes.Any())
            {
                subjectContentFieldMappings = await GetSubjectContentFieldMappingsAsync(subjectContentCodes, "RETURNTOWORK");
                if (!subjectContentFieldMappings.Any())
                {
                    _logger.LogWarning("[AZURE SERVICE BUS] No Subject ContentField mappings found for template '{TemplateName}'", templateName);
                }
            }

            // Combine all ContentFields to map at once
            var allContentFields = bodyContentFields
                .Union(subjectContentFieldMappings.Values)
                .Distinct()
                .ToList();

            // Step 6: Map all ContentFields to actual data from the return to work request
            _logger.LogInformation("[AZURE SERVICE BUS] Mapping {Count} content fields to return to work request data", allContentFields.Count);
            var fieldData = await _fieldMapperService.MapReturnToWorkFieldsToDataAsync(requestData, allContentFields);

            _logger.LogInformation("[AZURE SERVICE BUS] Mapped {MappedCount}/{TotalCount} fields to return to work request data",
                fieldData.Count, allContentFields.Count);

            // Step 7: Build HTML email body from template and field data
            string emailBody;

            if (!string.IsNullOrEmpty(template.ContentStyling) && template.ContentStyling.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is 'Text' - using HTML body with placeholder replacement");
                emailBody = ReplaceBodyPlaceholders(template.Body, fieldData, bodyContentFieldMappings);
            }
            else
            {
                _logger.LogInformation("[AZURE SERVICE BUS] ContentStyling is '{Style}' - using template builder for formatted table",
                    string.IsNullOrEmpty(template.ContentStyling) ? "TableRow (default)" : template.ContentStyling);

                emailBody = _templateBuilderService.BuildEmailBodyFromTemplate(
                    template,
                    fieldData,
                    bodyContentFieldMappings,
                    bodyContentLabelMappings);
            }

            if (string.IsNullOrWhiteSpace(emailBody))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Generated email body is empty for template '{TemplateName}'", templateName);
            }

            // Step 8: Replace Subject placeholders (@CODE) with actual mapped values
            var emailSubject = ReplaceSubjectPlaceholdersGeneric(
                template.Subject,
                subjectContentCodes,
                subjectContentFieldMappings,
                fieldData);

            if (string.IsNullOrWhiteSpace(emailSubject))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Generated email subject is empty for template '{TemplateName}'", templateName);
                emailSubject = template.Subject; // Fallback to original if replacement failed
            }

            _logger.LogInformation("[AZURE SERVICE BUS] Return to work email built successfully - Subject: {Subject}", emailSubject);

            // Step 9: Create EmailNotificationDto and send via Azure Service Bus
            var emailNotification = new EmailNotificationDto
            {
                RequestId = requestId,
                ToEmail = toEmail,
                CcEmail = ccEmail,
                Subject = emailSubject,
                Body = emailBody,
                NotificationType = template.EmailType,
                Priority = template.TriggerType == "Immediate" ? 1 : 2,
                Module = template.RequestType,
                Trigger = template.TriggerType,
                TemplateId = template.Id
            };

            // Send the email notification
            var result = await SendEmailNotificationAsync(emailNotification);

            if (result.Success)
            {
                _logger.LogInformation("[AZURE SERVICE BUS] Return to work email sent successfully using template {TemplateName}", templateName);
                _ecmLogger.LogEmailNotification(
                    true,
                    "ReturnToWorkTemplateProcessing",
                    toEmail,
                    emailSubject,
                    null);
            }
            else
            {
                _logger.LogError("[AZURE SERVICE BUS] Failed to send return to work email using template {TemplateName}: {Message}",
                    templateName, result.Message);
                _ecmLogger.LogEmailNotification(
                    false,
                    "ReturnToWorkTemplateProcessing",
                    toEmail,
                    emailSubject,
                    result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE SERVICE BUS] Error sending return to work email from template {TemplateName}", templateName);
            _ecmLogger.LogError(LogCategory.ServiceBus, ex, $"Error sending return to work email from template '{templateName}' to {toEmail}");
            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Error sending return to work email from template '{templateName}'",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Generic method to replace @CODE placeholders in subject with field data
    /// </summary>
    private string ReplaceSubjectPlaceholdersGeneric(
        string subject,
        List<string> subjectCodes,
        Dictionary<string, string> subjectFieldMappings,
        Dictionary<string, string> fieldData)
    {
        if (string.IsNullOrWhiteSpace(subject) || !subjectCodes.Any())
        {
            return subject;
        }

        var replacedSubject = subject;

        foreach (var code in subjectCodes)
        {
            if (!subjectFieldMappings.TryGetValue(code, out var fieldName))
            {
                _logger.LogWarning("[AZURE SERVICE BUS] Skipping subject placeholder @@{Code} - no ContentField mapping found", code);
                continue;
            }

            // Normalize field name to match dictionary key
            var normalizedFieldName = fieldName.Trim().Replace(" ", "").ToLowerInvariant();

            if (fieldData.TryGetValue(normalizedFieldName, out var fieldValue))
            {
                var placeholder = $"@@{code}";
                replacedSubject = replacedSubject.Replace(placeholder, fieldValue);
                _logger.LogDebug("[AZURE SERVICE BUS] Replaced subject placeholder {Placeholder} with {Value}", placeholder, fieldValue);
            }
            else
            {
                _logger.LogWarning("[AZURE SERVICE BUS] No field data found for subject code {Code} (field: {FieldName})", code, fieldName);
            }
        }

        return replacedSubject;
    }

    public void Dispose()
    {
        _sender?.DisposeAsync().AsTask().Wait();
        _client?.DisposeAsync().AsTask().Wait();
        _logger.LogInformation("[AZURE SERVICE BUS] Service disposed");
    }
}

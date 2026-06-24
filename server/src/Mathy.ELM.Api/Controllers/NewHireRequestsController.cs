using Microsoft.AspNetCore.Mvc;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Mathy.ELM.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class NewHireRequestsController : ControllerBase
{
    private readonly IHRRequestService _hrRequestService;
    private readonly INewHireRequestDetailsService _newHireDetailsService;
    private readonly IServiceDeskIntegrationService _serviceDeskService;
    private readonly IAzureServiceBusEmailService _emailService;
    private readonly IEmailRecipientsService _emailRecipientsService;
    private readonly MathyELMContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<NewHireRequestsController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEcmLogger _ecmLogger;

    public NewHireRequestsController(
        IHRRequestService hrRequestService,
        INewHireRequestDetailsService newHireDetailsService,
        IServiceDeskIntegrationService serviceDeskService,
        IAzureServiceBusEmailService emailService,
        IEmailRecipientsService emailRecipientsService,
        MathyELMContext context,
        IUserContextService userContextService,
        IBackgroundJobService backgroundJobService,
        ILogger<NewHireRequestsController> logger,
        IConfiguration configuration,
        IEcmLogger ecmLogger)
    {
        _hrRequestService = hrRequestService;
        _newHireDetailsService = newHireDetailsService;
        _serviceDeskService = serviceDeskService;
        _emailService = emailService;
        _emailRecipientsService = emailRecipientsService;
        _context = context;
        _userContextService = userContextService;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
        _configuration = configuration;
        _ecmLogger = ecmLogger;
    }

    /// <summary>
    /// Trims trailing/leading spaces from all string fields in the new hire request
    /// </summary>
    private void TrimNewHireRequestFields(CreateNewHireRequestDto request)
    {
        if (request == null) return;

        // Notes
        request.Notes = request.Notes?.Trim();

        // Personal info
        if (request.PersonalInfo != null)
        {
            request.PersonalInfo.FirstName = request.PersonalInfo.FirstName?.Trim();
            request.PersonalInfo.LastName = request.PersonalInfo.LastName?.Trim();
            request.PersonalInfo.MiddleInitial = request.PersonalInfo.MiddleInitial?.Trim();
            request.PersonalInfo.Suffix = request.PersonalInfo.Suffix?.Trim();
            request.PersonalInfo.PreferredFirstName = request.PersonalInfo.PreferredFirstName?.Trim();
            request.PersonalInfo.UserId = request.PersonalInfo.UserId?.Trim();
            request.PersonalInfo.ReferredBy = request.PersonalInfo.ReferredBy?.Trim();
        }

        // Position info
        if (request.PositionInfo != null)
        {
            request.PositionInfo.EmploymentStatus = request.PositionInfo.EmploymentStatus?.Trim();
            request.PositionInfo.PositionCode = request.PositionInfo.PositionCode?.Trim();
            request.PositionInfo.AppPercentage = request.PositionInfo.AppPercentage?.Trim();
        }

        // Credit card info
        if (request.CreditCardInfo != null)
        {
            request.CreditCardInfo.CreditExpenseType = request.CreditCardInfo.CreditExpenseType?.Trim();
            request.CreditCardInfo.FuelCardlockAddress = request.CreditCardInfo.FuelCardlockAddress?.Trim();
        }

        // Vehicle info
        if (request.VehicleInfo != null)
        {
            request.VehicleInfo.DriverClassification = request.VehicleInfo.DriverClassification?.Trim();
            request.VehicleInfo.DrugAndAlcoholProfile = request.VehicleInfo.DrugAndAlcoholProfile?.Trim();
        }

        // IT info
        if (request.ITInfo != null)
        {
            request.ITInfo.AlternateDeliveryLocation = request.ITInfo.AlternateDeliveryLocation?.Trim();
            request.ITInfo.EmailAddress = request.ITInfo.EmailAddress?.Trim();
        }

        // Phone info
        if (request.PhoneInfo != null)
        {
            request.PhoneInfo.WorkPhoneNumber = request.PhoneInfo.WorkPhoneNumber?.Trim();
            request.PhoneInfo.WorkExtension = request.PhoneInfo.WorkExtension?.Trim();
        }

        // Applications
        foreach (var app in request.Applications)
        {
            app.ApplicationName = app.ApplicationName?.Trim();
            app.AccessNotes = app.AccessNotes?.Trim();
        }

        // Folders
        foreach (var folder in request.Folders)
        {
            folder.FolderType = folder.FolderType?.Trim();
            folder.FolderName = folder.FolderName?.Trim();
        }
    }

    /// <summary>
    /// Validates new hire request for draft operations (only FirstName and LastName required)
    /// </summary>
    private ApiResponse<List<HRRequestDetailDto>>? ValidateForDraft(CreateNewHireRequestDto request)
    {
        if (request == null)
        {
            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "New hire request details are required"
            };
        }

        if (request.PersonalInfo == null)
        {
            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Personal information is required"
            };
        }

        if (string.IsNullOrEmpty(request.PersonalInfo.FirstName) || string.IsNullOrEmpty(request.PersonalInfo.LastName))
        {
            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "First name and last name are required"
            };
        }

        // All validation passed
        return null;
    }

    /// <summary>
    /// Validates new hire request for submit operations (all required fields must be provided)
    /// </summary>
    private ApiResponse<List<HRRequestDetailDto>>? ValidateForSubmit(CreateNewHireRequestDto request)
    {
        // First run draft validation (FirstName, LastName)
        var draftValidation = ValidateForDraft(request);
        if (draftValidation != null)
        {
            return draftValidation;
        }

        // Additional validation for submit operations
        if (request.PersonalInfo.FirstDayEmployment == default)
        {
            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "First day of employment is required"
            };
        }

        if (request.PositionInfo == null)
        {
            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Position information is required"
            };
        }

        // Validate required position fields
        if (request.PositionInfo.CompanyCode <= 0)
        {
            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Company code is required"
            };
        }

        if (request.PositionInfo.LocationCode <= 0)
        {
            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Location code is required"
            };
        }

        if (string.IsNullOrEmpty(request.PositionInfo.EmploymentStatus))
        {
            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Employment status is required"
            };
        }

        if (string.IsNullOrEmpty(request.PositionInfo.PositionCode))
        {
            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Position code is required"
            };
        }

        if (!request.PositionInfo.PayrollDeptCode.HasValue || request.PositionInfo.PayrollDeptCode <= 0)
        {
            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Payroll department code is required"
            };
        }

        // SupervisorId is optional - null is allowed when no supervisor is found
        // (user selects "NOT FOUND, Will contact HR" option)

        // All validation passed
        return null;
    }

    /// <summary>
    /// Get company distribution lists from CompanyDL table
    /// </summary>
    private async Task<CompanyDL?> GetCompanyDistributionListsAsync(int? companyCode)
    {
        if (!companyCode.HasValue)
        {
            Console.WriteLine($"[COMPANY DL] No company code provided");
            return null;
        }

        try
        {
            var companyDL = await _context.CompanyDLs
                .Where(c => c.CompanyCode == companyCode.Value && !c.IsDeleted)
                .FirstOrDefaultAsync();

            if (companyDL == null)
            {
                Console.WriteLine($"[COMPANY DL] No distribution lists found for company code {companyCode}");
            }
            else
            {
                Console.WriteLine($"[COMPANY DL] Retrieved distribution lists for company code {companyCode}");
            }

            return companyDL;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[COMPANY DL] ERROR: Failed to fetch distribution lists for company {companyCode}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get notification recipients for New Hire submission
    /// </summary>
    private async Task<(string manager, string submitter, List<string> siteDLs, CompanyDL? companyDL)> GetNotificationRecipientsAsync(CreateNewHireRequestDto request)
    {
        // Get submitter email from logged-in user (Azure AD token)
        string submitterEmail;
        try
        {
            submitterEmail = _userContextService.GetUserEmail();
            _logger.LogInformation("Retrieved submitter email from user context: {Email}", submitterEmail);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError("Failed to get user email from token: {Message}", ex.Message);
            submitterEmail = "unknown@example.com"; // Fallback
        }

        // Get manager email from supervisor's employee record
        string managerEmail = "noreply@example.com"; // Default fallback
        if (request.PositionInfo.SupervisorId.HasValue)
        {
            var supervisor = await _context.Employees
                .Where(e => e.EmployeeNumber == request.PositionInfo.SupervisorId.Value && !e.IsDeleted)
                .FirstOrDefaultAsync();

            if (supervisor != null && !string.IsNullOrEmpty(supervisor.WorkEmail))
            {
                managerEmail = supervisor.WorkEmail;
                _logger.LogInformation("Retrieved manager email for supervisor {SupervisorId}: {Email}",
                    request.PositionInfo.SupervisorId.Value, managerEmail);
            }
            else
            {
                _logger.LogWarning("Supervisor with ID {SupervisorId} not found or has no work email. Using fallback email.",
                    request.PositionInfo.SupervisorId.Value);
            }
        }
        else
        {
            _logger.LogWarning("No supervisor ID provided in request. Manager email will use fallback.");
        }

        // Fetch company distribution lists from database
        var companyDL = await GetCompanyDistributionListsAsync(request.PositionInfo.CompanyCode);

        // Site DLs now come from CompanyDL.SiteDL and HRDL
        var siteDLs = new List<string>();
        if (companyDL != null)
        {
            if (!string.IsNullOrEmpty(companyDL.SiteDL))
                siteDLs.AddRange(companyDL.SiteDL.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()));
            if (!string.IsNullOrEmpty(companyDL.HRDL))
                siteDLs.AddRange(companyDL.HRDL.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()));
        }

        return (managerEmail, submitterEmail, siteDLs, companyDL);
    }

    /// <summary>
    /// Send all email notifications triggered by New Hire submission
    /// Recipients are resolved from EmailTemplate.Recipients field using EmailRecipientsService
    /// </summary>
    private async Task SendNewHireSubmissionNotificationsAsync(CreateNewHireRequestDto request, int requestId)
    {
        Console.WriteLine($"[NEW HIRE NOTIFICATIONS] Starting email notifications for request ID: {requestId}");

        try
        {
            var submitterEmail = _userContextService.GetUserEmail();
            var managerEmail = await GetManagerEmailAsync(request.PositionInfo.SupervisorId);
            var companyCode = request.PositionInfo.CompanyCode;
            var deptCode = request.PositionInfo.PayrollDeptCode ?? 0;

            // 1. Confirmation Email - Recipients from EmailTemplate
            await SendTemplateEmailAsync(
                "Confirmation",
                request,
                requestId,
                companyCode,
                deptCode,
                managerEmail: managerEmail,
                submitterEmail: submitterEmail
            );

            // 2. Physical Security Task - If door access requested
            if (request.BuildingAccess?.Any() == true)
            {
                await SendTemplateEmailAsync(
                    "Task Email - Door Access",
                    request,
                    requestId,
                    companyCode,
                    deptCode,
                    managerEmail: managerEmail,
                    submitterEmail: submitterEmail
                );
            }

            // 3. Credit Card Task - If any credit card issued
            bool creditCardRequested = request.CreditCardInfo?.KwikTripCard == true ||
                                       request.CreditCardInfo?.CompanyExpenseCard == true;

            if (creditCardRequested)
            {
                await SendTemplateEmailAsync(
                    "Task Email - Credit Card",
                    request,
                    requestId,
                    companyCode,
                    deptCode,
                    managerEmail: managerEmail,
                    submitterEmail: submitterEmail
                );
            }

            // 4. Fuel Fob Task - If fuel cardlock access requested
            if (request.CreditCardInfo?.FuelCardlockAccess == true)
            {
                await SendTemplateEmailAsync(
                    "Task Email - Fuel Fob",
                    request,
                    requestId,
                    companyCode,
                    deptCode,
                    managerEmail: managerEmail,
                    submitterEmail: submitterEmail
                );
            }

            // 5. Fleet Task - If vehicle requested
            if (request.VehicleInfo?.NeedCompanyCar == true)
            {
                await SendTemplateEmailAsync(
                    "Task Email - Fleet",
                    request,
                    requestId,
                    companyCode,
                    deptCode,
                    managerEmail: managerEmail,
                    submitterEmail: submitterEmail
                );
            }

            // 6. Compliance Task - Only sent if IsApprovedToOperate is true

            if (request.VehicleInfo?.IsApprovedToOperate == true)
            {
                await SendTemplateEmailAsync(
                    "Task Email - Compliance",
                    request,
                    requestId,
                    companyCode,
                    deptCode,
                    managerEmail: managerEmail,
                    submitterEmail: submitterEmail
                );
            }
            else
            {
                Console.WriteLine($"[NEW HIRE NOTIFICATIONS] Task Email - Compliance skipped: IsApprovedToOperate is not true (Value: {request.VehicleInfo?.IsApprovedToOperate?.ToString() ?? "null"})");
            }

            // 7. Safety Task - If template exists
            // NOTE: Safety template may not be in EmailTemplates table yet
            await SendTemplateEmailAsync(
                "Task Email - Safety",
                request,
                requestId,
                companyCode,
                deptCode,
                managerEmail: managerEmail,
                submitterEmail: submitterEmail
            );

            // 8. Reminder Email - IT HR - If start date is within 3 days
            if (request.PersonalInfo?.FirstDayEmployment.HasValue == true)
            {
                var firstDayEmployment = request.PersonalInfo.FirstDayEmployment.Value.Date;
                var today = DateTime.Today.Date;
                var daysUntilStart = (firstDayEmployment - today).Days;

                // Send if start date is within 3 days (0-2 days from now)
                if (daysUntilStart >= 0 && daysUntilStart < 3)
                {
                    await SendTemplateEmailAsync(
                        "Task Email - IT HR",
                        request,
                        requestId,
                        companyCode,
                        deptCode,
                        managerEmail: managerEmail,
                        submitterEmail: submitterEmail
                    );
                    Console.WriteLine($"[NEW HIRE NOTIFICATIONS] Reminder Email - IT HR queued successfully (Start date in {daysUntilStart} days)");
                }
            }

            Console.WriteLine($"[NEW HIRE NOTIFICATIONS] All notifications processed for request ID: {requestId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NEW HIRE NOTIFICATIONS] CRITICAL ERROR: Failed to send notifications: {ex.Message}");
            _logger.LogError(ex, "Error sending new hire notifications for request {RequestId}", requestId);
            // Don't throw - we don't want to block request creation if email fails
        }
    }

    /// <summary>
    /// Helper method to send an email using template with resolved recipients
    /// Resolves recipients from EmailTemplate.Recipients field with support for special recipients (Manager, Submitter, Employee)
    /// </summary>
    private async Task SendTemplateEmailAsync(
        string templateName,
        CreateNewHireRequestDto request,
        int requestId,
        int? companyCode,
        int deptCode,
        string? managerEmail = null,
        string? submitterEmail = null,
        string? employeeEmail = null)
    {
        try
        {
            // Resolve recipients from EmailTemplate.Recipients field
            // Supports both CompanyDL fields (ITDL, HRDL, etc.) and special recipients (Manager, Submitter, Employee)
            // Filter by RequestType='NEWHIRE' to get new hire-specific templates
            var recipients = await _emailRecipientsService.GetRecipientsFromTemplateAsync(
                templateName,
                companyCode,
                deptCode,
                managerEmail,
                submitterEmail,
                employeeEmail,
                requestType: "NEWHIRE");

            if (!recipients.Any())
            {
                Console.WriteLine($"[NEW HIRE NOTIFICATIONS] WARNING: No recipients resolved for template '{templateName}'");
                return;
            }

            var toEmails = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

            await _emailService.SendEmailFromTemplateNameAsync(
                templateName,
                request,
                toEmails,
                null,
                requestId
            );

            Console.WriteLine($"[NEW HIRE NOTIFICATIONS] Email '{templateName}' queued successfully to {recipients.Count} recipient(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NEW HIRE NOTIFICATIONS] ERROR: Failed to queue email '{templateName}': {ex.Message}");
            _logger.LogWarning(ex, "Failed to send template email '{TemplateName}' for request {RequestId}", templateName, requestId);
        }
    }

    /// <summary>
    /// Helper method to get manager email from supervisor ID
    /// </summary>
    private async Task<string> GetManagerEmailAsync(int? supervisorId)
    {
        if (!supervisorId.HasValue)
        {
            return ""; // Fallback
        }

        try
        {
            var supervisor = await _context.Employees
                .Where(e => e.EmployeeNumber == supervisorId.Value && !e.IsDeleted)
                .FirstOrDefaultAsync();

            return !string.IsNullOrEmpty(supervisor?.WorkEmail) ? supervisor.WorkEmail : "noreply@example.com";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve manager email for supervisor {SupervisorId}", supervisorId);
            return "";
        }
    }

    /// <summary>
    /// Create new hire request: Create AD User, then Create HR Request and Save New Hire Details
    /// </summary>
    /// <param name="request">New hire request with HR request details</param>
    /// <returns>Success indicator with created request details</returns>
    [HttpPost("CreateNewHireRequest")]
    public async Task<ActionResult<ApiResponse<List<HRRequestDetailDto>>>> CreateNewHireRequest([FromBody] CreateNewHireRequestDto request)
    {
        // Validate for submit operation (all required fields)
        var validation = ValidateForSubmit(request);
        if (validation != null)
        {
            return BadRequest(validation);
        }

        string? adUsername = null;
        string? adEmail = null;
        string? adPassword = null;
        bool adUserCreated = false;

        try
        {
            // Trim all string fields before AD creation and DB save
            TrimNewHireRequestFields(request);

            // Step 0: Create AD User (PREREQUISITE - must succeed before HR request)
            // Check if AD integration is enabled in appsettings
            var isActiveDirectoryEnabled = _configuration.GetSection("ActiveDirectory:IsActive").Value?.ToLower() == "yes";

            if (isActiveDirectoryEnabled)
            {
                _ecmLogger.LogActiveDirectory(true, "CreateUser", null, null, $"Creating AD user for {request.PersonalInfo.FirstName} {request.PersonalInfo.LastName}");

                (bool adSuccess, string? username, string? email, string? password) = await _newHireDetailsService.CreateUserInADOU(
                    request.PositionInfo.CompanyCode ?? 0,
                    request.PositionInfo.PayrollDeptCode ?? 0,
                    request.PersonalInfo.PreferredFirstName,
                    request.PersonalInfo.FirstName,
                    request.PersonalInfo.LastName,
                    request.PersonalInfo.MiddleInitial,
                    request.PositionInfo.PositionCode, // Using position code as title
                    null, // Department - can be enhanced later
                    request.ITInfo?.EmailAddress // Pre-generated email from frontend
                );

                adUsername = username;
                adEmail = email;
                adPassword = password;

                if (!adSuccess)
                {
                    _ecmLogger.LogActiveDirectory(false, "CreateUser", null, adUsername, $"Step 0 FAILED: AD user creation failed for username '{adUsername}'");

                    // Attempt to rollback AD user if it was partially created
                    if (!string.IsNullOrEmpty(adUsername))
                    {
                        _ecmLogger.LogActiveDirectory(true, "RollbackUser", null, adUsername, $"Attempting AD user rollback for '{adUsername}'");
                        var rollbackSuccess = await _newHireDetailsService.DeleteUserFromAD(adUsername, request.PositionInfo.CompanyCode ?? 0);
                        if (rollbackSuccess)
                        {
                            _ecmLogger.LogActiveDirectory(true, "RollbackUser", null, adUsername, $"Successfully rolled back AD user '{adUsername}'");
                        }
                        else
                        {
                            _ecmLogger.LogActiveDirectory(false, "RollbackUser", null, adUsername, $"WARNING: Failed to rollback AD user '{adUsername}' - manual cleanup may be required");
                        }
                    }

                    return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                    {
                        Success = false,
                        Message = $"Failed to create Active Directory user. Username: {adUsername ?? "N/A"}, Email: {adEmail ?? "N/A"}. Please check server logs for details.",
                        Errors = new List<string> { "AD user creation failed" }
                    });
                }

                adUserCreated = true;
                _ecmLogger.LogActiveDirectory(true, "CreateUser", null, adUsername, $"AD user created - Username: '{adUsername}', Email: '{adEmail}'");
            }
            else
            {
                _ecmLogger.LogActiveDirectory(false, "CreateUser", null, null, "ActiveDirectory:IsActive is not set to 'Yes' in appsettings - skipped");
            }

            // Phase 1: Create base HR Request (RequestTypeId = 5)
            var multiEmployeeRequest = new CreateMultiEmployeeHRRequestDto
            {
                RequestTypeId = 5, // NewHire
                EmployeeIds = new List<int> { request.PersonalInfo.EmployeeId ?? 0 },
                RequestTitle = $"New Hire Request - {request.PersonalInfo.FirstName} {request.PersonalInfo.LastName}",
                RequestDescription = $"New hire request for {request.PersonalInfo.FirstName} {request.PersonalInfo.LastName} starting {request.PersonalInfo.FirstDayEmployment:yyyy-MM-dd}. AD Username: {adUsername}, Email: {adEmail}",
                EffectiveDate = request.PersonalInfo.FirstDayEmployment,
                Notes = request.Notes,
                ProcessingNotes = request.Notes,
                RequestedBy = _userContextService.GetUserEmployeeNumber(),
                CompanyId = request.PositionInfo.CompanyCode,
                PayrollGroupId = request.PositionInfo.PayrollDeptCode
            };

            // Step 1: Create HR Request
            Console.WriteLine($"[NEW HIRE] Step 1: Creating HR request");
            var hrRequestResult = await _hrRequestService.CreateMultiEmployeeHRRequestAsync(multiEmployeeRequest);

            if (!hrRequestResult.Success || hrRequestResult.Data == null || !hrRequestResult.Data.Any())
            {
                Console.WriteLine($"[NEW HIRE] Step 1 FAILED: HR request creation failed");

                // Rollback: Delete AD user
                if (adUserCreated && !string.IsNullOrEmpty(adUsername))
                {
                    Console.WriteLine($"[NEW HIRE] Rolling back AD user '{adUsername}'");
                    var adRollbackSuccess = await _newHireDetailsService.DeleteUserFromAD(adUsername, request.PositionInfo.CompanyCode ?? 0);
                    if (!adRollbackSuccess)
                    {
                        Console.WriteLine($"[NEW HIRE] WARNING: Failed to rollback AD user '{adUsername}' - manual cleanup required");
                    }
                }

                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = "Failed to create HR request. AD user has been rolled back.",
                    Errors = hrRequestResult.Errors
                });
            }

            Console.WriteLine($"[NEW HIRE] Step 1 SUCCESS: HR request created");

            // Step 2: Create New Hire Request Details
            Console.WriteLine($"[NEW HIRE] Step 2: Creating New Hire request details");
            var hrRequestDetailId = hrRequestResult.Data.First().Id;
            var newHireResult = await _newHireDetailsService.CreateNewHireRequestDetailsAsync(
                hrRequestDetailId,
                request,
                adUsername,
                adEmail,
                adPassword
            );

            if (!newHireResult.Success)
            {
                Console.WriteLine($"[NEW HIRE] Step 2 FAILED: New Hire details creation failed");

                // Rollback: Delete the HR Request that was just created
                try
                {
                    foreach (var hrDetail in hrRequestResult.Data)
                    {
                        if (hrDetail.ParentRequestId > 0)
                        {
                            await _hrRequestService.DeleteHRRequestAsync(hrDetail.ParentRequestId);
                        }
                    }
                    Console.WriteLine($"[NEW HIRE] Successfully rolled back HR request");
                }
                catch (Exception rollbackEx)
                {
                    Console.WriteLine($"[NEW HIRE] ERROR: Failed to rollback HR request: {rollbackEx.Message}");
                }

                // Rollback: Delete AD user
                if (adUserCreated && !string.IsNullOrEmpty(adUsername))
                {
                    Console.WriteLine($"[NEW HIRE] Rolling back AD user '{adUsername}'");
                    var adRollbackSuccess = await _newHireDetailsService.DeleteUserFromAD(adUsername, request.PositionInfo.CompanyCode ?? 0);
                    if (!adRollbackSuccess)
                    {
                        Console.WriteLine($"[NEW HIRE] WARNING: Failed to rollback AD user '{adUsername}' - manual cleanup required");
                        return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                        {
                            Success = false,
                            Message = $"Failed to create New Hire details. HR request has been rolled back, but AD user '{adUsername}' could not be deleted - manual cleanup required. Original error: {newHireResult.Message}",
                            Errors = newHireResult.Errors
                        });
                    }
                }

                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = $"Failed to create New Hire details. HR request and AD user have been rolled back. Error: {newHireResult.Message}",
                    Errors = newHireResult.Errors
                });
            }

            Console.WriteLine($"[NEW HIRE] Step 2 SUCCESS: New Hire details created");

            // Step 2.5: Create ServiceDesk record (non-blocking)
            Console.WriteLine($"[NEW HIRE] Step 2.5: Creating ServiceDesk record");
            var parentRequestId = hrRequestResult.Data.First().ParentRequestId;
            try
            {
                var newHireRequestDetailId = newHireResult.Data?.Id ?? 0;
                if (newHireRequestDetailId > 0)
                {
                    var serviceDeskSuccess = await CreateServiceDeskRecordAsync(
                        newHireRequestDetailId,
                        parentRequestId,
                        request,
                        adUsername,
                        //"testADUserName",
                        adEmail,
                        adPassword);

                    if (serviceDeskSuccess)
                        Console.WriteLine($"[NEW HIRE] Step 2.5 SUCCESS: ServiceDesk record created");
                    else
                        Console.WriteLine($"[NEW HIRE] Step 2.5 FAILED: ServiceDesk record creation failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NEW HIRE] Step 2.5 WARNING: ServiceDesk record creation failed: {ex.Message}");
                _logger.LogWarning(ex, "[NEW HIRE] Failed to create ServiceDesk record");
                // Non-blocking - don't fail the entire request if ServiceDesk fails
            }

            // Step 3: Send email notifications (non-blocking)
            Console.WriteLine($"[NEW HIRE] Step 3: Sending email notifications");
            await SendNewHireSubmissionNotificationsAsync(request, parentRequestId);
            Console.WriteLine($"[NEW HIRE] Step 3 COMPLETE: Email notifications processed");

            // Step 4: Send/Schedule email notifications based on SubmissionFreq trigger dates
            Console.WriteLine($"[NEW HIRE] Step 4: Processing email notifications (Send immediately or schedule)");
            try
            {
                var today = DateTime.Today.Date;
                var firstDayEmployment = request.PersonalInfo.FirstDayEmployment?.Date ?? DateTime.Today;

                // Get all active scheduled email templates
                var emailTemplates = await _context.EmailTemplates
                    .Where(t => t.IsActive && !t.IsDeleted && t.TriggerType == "Scheduled")
                    .ToListAsync();

                int emailsScheduled = 0;

                foreach (var template in emailTemplates)
                {
                    try
                    {
                        // Skip Welcome Email - it's handled separately with proper recipient in ProcessNewHirePreEmploymentAsync
                        if (template.TemplateName == "Welcome Email")
                        {
                            Console.WriteLine($"[NEW HIRE] Skipping 'Welcome Email' template - handled separately with proper recipient in ProcessNewHirePreEmploymentAsync");
                            continue;
                        }

                        // Calculate trigger date: FirstDayEmployment + SubmissionFreq days
                        var triggerDate = firstDayEmployment.AddDays(template.SubmissionFreq ?? 0);
                        var daysUntilTrigger = (triggerDate - today).Days;

                        Console.WriteLine($"[NEW HIRE] Template '{template.TemplateName}': TriggerDate={triggerDate:yyyy-MM-dd}, DaysUntil={daysUntilTrigger}");

                        // Only schedule via Hangfire for all trigger dates
                        if (daysUntilTrigger >= 0)
                        {
                            var jobId = $"newhire-email-{parentRequestId}-{template.Id}-{template.SubmissionFreq}";

                            // Check if trigger date is today or in the past
                            // Hangfire.Schedule() doesn't execute jobs with past/today dates
                            // So we must use Enqueue() for past/today dates to ensure execution
                            if (triggerDate <= today)
                            {
                                // Date is today or in the past - enqueue immediately for immediate execution
                                Console.WriteLine($"[NEW HIRE] Step 4: TriggerDate {triggerDate:yyyy-MM-dd} is today/past (now is {today:yyyy-MM-dd}), enqueueing immediately");

                                var hangfireJobId = Hangfire.BackgroundJob.Enqueue(
                                    () => _backgroundJobService.SendScheduledNewHireEmailAsync(parentRequestId, template.Id));

                                emailsScheduled++;
                                Console.WriteLine($"[NEW HIRE] Step 4: '{template.TemplateName}' enqueued immediately (JobId={hangfireJobId})");
                            }
                            else
                            {
                                // Date is in the future - schedule normally
                                Console.WriteLine($"[NEW HIRE] Step 4: TriggerDate {triggerDate:yyyy-MM-dd} is future (now is {today:yyyy-MM-dd}), scheduling for later");

                                var hangfireJobId = Hangfire.BackgroundJob.Schedule(
                                    () => _backgroundJobService.SendScheduledNewHireEmailAsync(parentRequestId, template.Id),
                                    triggerDate.Add(TimeSpan.Zero)); // Midnight

                                emailsScheduled++;
                                Console.WriteLine($"[NEW HIRE] Step 4: '{template.TemplateName}' scheduled successfully (JobId={hangfireJobId})");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[NEW HIRE] Step 4 ERROR: Failed to process template '{template.TemplateName}': {ex.Message}");
                    }
                }

                Console.WriteLine($"[NEW HIRE] Step 4 SUCCESS: Emails scheduled={emailsScheduled}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NEW HIRE] Step 4 WARNING: Failed to process email notifications: {ex.Message}");
                // Don't fail the entire request if email processing fails
                _logger.LogWarning(ex, "[NEW HIRE] Failed to process email notifications");
            }

            // Success response
            var response = new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = true,
                Data = hrRequestResult.Data,
                Message = $"Successfully completed new hire request for {request.PersonalInfo.FirstName} {request.PersonalInfo.LastName}. AD Username: {adUsername}, Email: {adEmail}, Temporary Password: {adPassword}"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NEW HIRE] EXCEPTION: {ex.Message}");
            Console.WriteLine($"[NEW HIRE] Stack trace: {ex.StackTrace}");

            // Rollback AD user if it was created
            if (adUserCreated && !string.IsNullOrEmpty(adUsername))
            {
                Console.WriteLine($"[NEW HIRE] Exception rollback: Attempting to delete AD user '{adUsername}'");
                try
                {
                    var adRollbackSuccess = await _newHireDetailsService.DeleteUserFromAD(adUsername, request.PositionInfo.CompanyCode ?? 0);
                    if (adRollbackSuccess)
                    {
                        Console.WriteLine($"[NEW HIRE] Successfully rolled back AD user '{adUsername}'");
                    }
                    else
                    {
                        Console.WriteLine($"[NEW HIRE] WARNING: Failed to rollback AD user '{adUsername}' - manual cleanup required");
                    }
                }
                catch (Exception rollbackEx)
                {
                    Console.WriteLine($"[NEW HIRE] ERROR during AD rollback: {rollbackEx.Message}");
                }
            }

            return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = $"An error occurred during new hire request processing: {ex.Message}",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Save new hire request as draft: Create HR Request with Draft status and Save New Hire Details
    /// </summary>
    /// <param name="request">New hire request with HR request details</param>
    /// <returns>Success indicator with created request details</returns>
    [HttpPost("SaveNewHireRequestAsDraft")]
    public async Task<ActionResult<ApiResponse<List<HRRequestDetailDto>>>> SaveNewHireRequestAsDraft([FromBody] CreateNewHireRequestDto request)
    {
        var logId = $"CONTROLLER_DRAFT_SAVE_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString()[..8]}";
        var startTime = DateTime.UtcNow;

        Console.WriteLine($"[{logId}] ====== CONTROLLER: SAVE DRAFT STARTED ======");
        Console.WriteLine($"[{logId}] Request timestamp: {startTime:yyyy-MM-dd HH:mm:ss.fff} UTC");
        Console.WriteLine($"[{logId}] Request size: {(request != null ? JsonSerializer.Serialize(request).Length : 0)} characters");

        if (request == null)
        {
            Console.WriteLine($"[{logId}] ERROR: Request is null");
            return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "New hire request details are required"
            });
        }

        Console.WriteLine($"[{logId}] Request payload: {JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true })}");

        Console.WriteLine($"[{logId}] Starting validation checks for draft save");

        // Validate for draft operation (only FirstName and LastName required)
        var validation = ValidateForDraft(request);
        if (validation != null)
        {
            Console.WriteLine($"[{logId}] ERROR: Draft validation failed - {validation.Message}");
            return BadRequest(validation);
        }

        // Trim all string fields before saving
        TrimNewHireRequestFields(request);

        Console.WriteLine($"[{logId}] Draft validation checks passed. FirstName: '{request.PersonalInfo.FirstName}', LastName: '{request.PersonalInfo.LastName}', FirstDayEmployment: {request.PersonalInfo.FirstDayEmployment:yyyy-MM-dd}");

        try
        {
            Console.WriteLine($"[{logId}] Calling service layer: SaveNewHireRequestAsDraftAsync");
            var result = await _newHireDetailsService.SaveNewHireRequestAsDraftAsync(request);
            Console.WriteLine($"[{logId}] Service call completed. Success: {result.Success}");

            if (!result.Success)
            {
                Console.WriteLine($"[{logId}] ERROR: Service returned Success=false");
                Console.WriteLine($"[{logId}] Service error message: {result.Message}");
                Console.WriteLine($"[{logId}] Service errors: {string.Join(", ", result.Errors ?? new List<string>())}");

                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors
                });
            }

            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;
            Console.WriteLine($"[{logId}] SUCCESS: Draft save completed in {duration:F2}ms");
            Console.WriteLine($"[{logId}] Returning result with {result.Data?.Count ?? 0} items");

            return Ok(result);
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;
            Console.WriteLine($"[{logId}] EXCEPTION after {duration:F2}ms: {ex.Message}");
            Console.WriteLine($"[{logId}] Exception type: {ex.GetType().Name}");
            Console.WriteLine($"[{logId}] Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[{logId}] Inner exception: {ex.InnerException.Message}");
            }

            return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = $"An error occurred while saving new hire request as draft: {ex.Message}",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update existing new hire request as draft: Update HR Request Details and keep Draft status
    /// </summary>
    /// <param name="parentId">Parent HR request ID</param>
    /// <param name="request">Updated new hire request data</param>
    /// <returns>Success indicator with updated request details</returns>
    [HttpPut("UpdateNewHireRequestAsDraft/{parentId}")]
    public async Task<ActionResult<ApiResponse<List<HRRequestDetailDto>>>> UpdateNewHireRequestAsDraft(int parentId, [FromBody] CreateNewHireRequestDto request)
    {
        if (parentId <= 0)
        {
            return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Invalid parent request ID"
            });
        }

        // Validate for draft operation (only FirstName and LastName required)
        var validation = ValidateForDraft(request);
        if (validation != null)
        {
            return BadRequest(validation);
        }

        // Trim all string fields before saving
        TrimNewHireRequestFields(request);

        try
        {
            var result = await _newHireDetailsService.UpdateNewHireRequestAsDraftAsync(parentId, request);

            if (!result.Success)
            {
                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = $"An error occurred while updating new hire request as draft: {ex.Message}",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update existing new hire request and submit: Update HR Request Details and change status to Pending
    /// </summary>
    /// <param name="parentId">Parent HR request ID</param>
    /// <param name="request">Updated new hire request data</param>
    /// <returns>Success indicator with updated request details</returns>
    [HttpPut("UpdateNewHireRequest/{parentId}")]
    public async Task<ActionResult<ApiResponse<List<HRRequestDetailDto>>>> UpdateNewHireRequest(int parentId, [FromBody] CreateNewHireRequestDto request)
    {
        if (parentId <= 0)
        {
            return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Invalid parent request ID"
            });
        }

        // Validate for submit operation (all required fields)
        var validation = ValidateForSubmit(request);
        if (validation != null)
        {
            return BadRequest(validation);
        }

        // Trim all string fields before saving
        TrimNewHireRequestFields(request);

        try
        {
            var result = await _newHireDetailsService.UpdateNewHireRequestAsync(parentId, request);

            if (!result.Success)
            {
                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = result.Message,
                    Errors = result.Errors
                });
            }

            // Send email notifications after successful update and submit
            Console.WriteLine($"[NEW HIRE UPDATE] Sending email notifications for parent ID: {parentId}");
            await SendNewHireSubmissionNotificationsAsync(request, parentId);
            Console.WriteLine($"[NEW HIRE UPDATE] Email notifications processed");

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = $"An error occurred while updating new hire request: {ex.Message}",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update Work Phone Number and Work Extension for a new hire request (ECM_ADMIN only)
    /// </summary>
    [HttpPut("UpdatePhoneInfo/{parentId}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdatePhoneInfo(int parentId, [FromBody] UpdatePhoneInfoDto request)
    {
        if (parentId <= 0)
        {
            return BadRequest(new ApiResponse<bool> { Success = false, Message = "Invalid parent request ID" });
        }

        try
        {
            var newHireDetail = await _context.NewHireRequestDetails
                .Include(n => n.ITPhoneRequirement)
                .FirstOrDefaultAsync(n => n.HRRequestDetail.ParentRequestId == parentId && !n.IsDeleted);

            if (newHireDetail == null)
            {
                return NotFound(new ApiResponse<bool> { Success = false, Message = "New hire request not found" });
            }

            if (newHireDetail.ITPhoneRequirement == null)
            {
                return NotFound(new ApiResponse<bool> { Success = false, Message = "Phone requirement record not found" });
            }

            newHireDetail.ITPhoneRequirement.WorkPhoneNumber = request.WorkPhoneNumber;
            newHireDetail.ITPhoneRequirement.WorkExtension = request.WorkExtension;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Phone info updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<bool> { Success = false, Message = $"Error updating phone info: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get comprehensive new hire request details by parent HR request ID for viewing
    /// </summary>
    /// <param name="parentId">Parent HR request ID</param>
    /// <returns>Complete new hire details with all related information and reference data</returns>
    [HttpGet("GetNewHireDetailsByParentId/{parentId}")]
    public async Task<ActionResult<ApiResponse<NewHireRequestViewDto>>> GetNewHireDetailsByParentId(int parentId)
    {
        if (parentId <= 0)
        {
            return BadRequest(new ApiResponse<NewHireRequestViewDto>
            {
                Success = false,
                Message = "Invalid parent request ID"
            });
        }

        try
        {
            // Use the new comprehensive service method
            var result = await _newHireDetailsService.GetNewHireRequestViewByParentIdAsync(parentId);

            if (!result.Success)
            {
                return result.Data == null
                    ? NotFound(result)
                    : StatusCode(500, result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<NewHireRequestViewDto>
            {
                Success = false,
                Message = $"An error occurred while retrieving new hire details: {ex.Message}",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Create Active Directory user account
    /// </summary>
    /// <param name="request">AD user creation request</param>
    /// <returns>Success indicator</returns>
    [HttpPost("create-ad-user")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> CreateADUser([FromBody] CreateADUserRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }

        try
        {
            // Trim trailing spaces from name fields
            request.FirstName = request.FirstName?.Trim();
            request.LastName = request.LastName?.Trim();
            request.MiddleInitial = request.MiddleInitial?.Trim();
            request.PreferredFirstName = request.PreferredFirstName?.Trim();

            var (success, username, email, password) = await _newHireDetailsService.CreateUserInADOU(
                request.CompanyCode,
                request.PayrollDeptCode,
                request.PreferredFirstName,
                request.FirstName,
                request.LastName,
                request.MiddleInitial,
                request.Title,
                request.Department);

            var response = new ApiResponse<bool>
            {
                Success = success,
                Data = success,
                Message = success
                    ? $"Successfully created AD user '{username}' with Email: {email}"
                    : $"Failed to create AD user{(username != null ? $" '{username}'" : "")}{(email != null ? $" with Email: {email}" : "")}. Check server logs for details."
            };

            return success ? Ok(response) : StatusCode(500, response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"An error occurred while creating AD user: {ex.Message}",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Creates a ServiceDesk ticket for the new hire request
    /// This is non-blocking - failures don't fail the overall request
    /// </summary>
    /// <param name="newHireRequestDetailId">ID of the NewHireRequestDetail</param>
    /// <param name="parentRequestId">ID of the parent HR request</param>
    /// <param name="request">The original create new hire request DTO</param>
    /// <param name="adUsername">The generated AD username (e.g., kim001) from CreateUserInADOU</param>
    /// <param name="adEmail">The generated AD email from CreateUserInADOU</param>
    /// <param name="adPassword">The temporary AD password from CreateUserInADOU</param>
    private async Task<bool> CreateServiceDeskRecordAsync(
        int newHireRequestDetailId,
        int parentRequestId,
        CreateNewHireRequestDto request,
        string? adUsername = null,
        string? adEmail = null,
        string? adPassword = null)
    {
        NewHireRequestDetail? newHireDetail = null;
        try
        {
            // Validate that we have AD credentials
            if (string.IsNullOrEmpty(adUsername))
            {
                _ecmLogger.LogServiceTicket(false, "CreateTicket", null, "NewHire", "No AD username provided - ServiceDesk record skipped");
                return false;
            }

            // Get the NewHireRequestDetail entity from the database for additional info
            newHireDetail = await _context.NewHireRequestDetails
                .FirstOrDefaultAsync(n => n.Id == newHireRequestDetailId && !n.IsDeleted);

            if (newHireDetail == null)
            {
                _ecmLogger.LogServiceTicket(false, "CreateTicket", null, "NewHire", $"NewHireRequestDetail {newHireRequestDetailId} not found");
                return false;
            }

            var employeeFullName = $"{newHireDetail.FirstName} {newHireDetail.LastName}".Trim();

            // Get current user context (the person submitting the request)
            var currentUserId = _userContextService.GetUserId();
            var requestorName = _userContextService.GetUserDisplayName();
            var requestorUserName = _userContextService.GetUserName();

            // Parse given_name and family_name from display name (for backward compatibility)
            var nameParts = requestorName.Split(' ');
            var requestorFirstName = nameParts.FirstOrDefault() ?? "Unknown";
            var requestorLastName = string.Join(" ", nameParts.Skip(1)) ?? "User";

            // Validate minimum required fields for ServiceDesk
            if (string.IsNullOrEmpty(newHireDetail.FirstName) ||
                string.IsNullOrEmpty(newHireDetail.LastName) ||
                !newHireDetail.FirstDayEmployment.HasValue)
            {
                _ecmLogger.LogServiceTicket(false, "CreateTicket", null, "NewHire", "Missing required fields (FirstName, LastName, or FirstDayEmployment) for ServiceDesk record", employeeName: employeeFullName);
                return false;
            }

            // Check if ServiceDesk integration is enabled
            var isServiceDeskEnabled = _configuration.GetSection("ServiceDeskPlus:IsActive").Value?.ToLower() == "yes";
            if (!isServiceDeskEnabled)
            {
                _ecmLogger.LogServiceTicket(false, "CreateTicket", null, "NewHire", "ServiceDesk integration is disabled in appsettings", employeeName: employeeFullName);
                return false;
            }

            _ecmLogger.LogServiceTicket(true, "CreateTicket", null, "NewHire", $"Creating ticket for {employeeFullName} with username '{adUsername}'", employeeName: employeeFullName);

            // Build the CreateServiceDeskRecordDto with all available data
            var serviceDeskDto = new CreateServiceDeskRecordDto
            {
                // IDENTIFIERS
                NewHireRequestDetailId = newHireRequestDetailId,
                ParentHRRequestId = parentRequestId,

                // PERSONAL INFORMATION
                FirstName = newHireDetail.FirstName,
                LastName = newHireDetail.LastName,
                PreferredFirstName = newHireDetail.PreferredFirstName ?? newHireDetail.FirstName,
                Rehire = newHireDetail.Rehire ?? false,
                EmailAddress = adEmail,  // Use AD email from CreateUserInADOU
                NetworkUserName = adUsername,  // Use AD username from CreateUserInADOU (e.g., kim001)
                FirstDayOfEmployment = newHireDetail.FirstDayEmployment ?? DateTime.MinValue,
                FirstDayOfEmploymentMilliseconds = ConvertToMilliseconds(newHireDetail.FirstDayEmployment),

                // ORGANIZATION INFORMATION
                CompanyCode = newHireDetail.CompanyCode,
                PayrollDeptCode = newHireDetail.PayrollDeptCode,
                LocationCode = newHireDetail.LocationCode,
                PositionCode = newHireDetail.PositionCode,
                SupervisorId = newHireDetail.SupervisorId,
                EmploymentStatus = newHireDetail.EmploymentStatus,

                // REQUESTOR INFORMATION (who submitted the new hire request)
                RequestorName = requestorName,
                RequestorUserName = requestorUserName,
                RequestorId = int.TryParse(currentUserId, out var userId) ? userId : 0,
                RequestorFirstName = requestorFirstName,
                RequestorLastName = requestorLastName,

                // NETWORK & EMAIL REQUIREMENTS
                RequireNetworkUser = !string.IsNullOrEmpty(adUsername) ? "True" : "False",
                RequireEmailAddress = !string.IsNullOrEmpty(adEmail) ? "True" : "False",

                // PHONE REQUIREMENTS
                DeskPhoneRequired = request.PhoneInfo?.DeskPhone == true ? "Yes" : "No",
                ReuseExistingPhone = request.PhoneInfo?.ReusingExistingPhone == true ? "Yes" : "No",
                CompanyCellPhoneRequired = request.PhoneInfo?.CompanyCellphone == true ? "Yes" : "No",
                CompanyCellPlan = null, // TODO: Add CompanyCellPlan from ITPhoneRequirement table if available
                BYODCellPhone = request.PhoneInfo?.BYODCellphone == true ? "Yes" : "No",

                // IT REQUIREMENTS
                MSOfficeLicenseE5 = request.ITInfo?.MSOfficeLicenseE5,
                MSOfficeLicenseF3 = request.ITInfo?.MSOfficeLicenseF3,
                MicrosoftLicenses = request.ITInfo?.MSOfficeLicenseE5 == true ? "E5 License" : request.ITInfo?.MSOfficeLicenseF3 == true ? "F3 License" : "N/A",
                AlternateEmailDeliveryLocation = request.ITInfo?.AlternateDeliveryLocation,
                AdditionalNotes = request.Notes,

                // COLLECTIONS
                BuildingAccess = request.BuildingAccess,
                ComputerRequirements = await EnrichComputerRequirementsWithDescriptions(request.ComputerRequirements),
                TabletProfiles = request.TabletProfiles,
                Applications = await EnrichApplicationsWithNames(request.Applications),
                SharepointAndFolderAccess = request.Folders,

                // REQUIREMENTS FLAGS
                Requirements = new ServiceDeskRequirementsDto
                {
                    HasPhoneRequirements = request.PhoneInfo != null,
                    HasComputerRequirements = request.ComputerRequirements?.Any() == true,
                    HasTabletProfiles = request.TabletProfiles?.Any() == true,
                    HasBuildingAccess = request.BuildingAccess?.Any() == true,
                    HasITApplications = request.Applications?.Any() == true,
                    HasSoftwareAccessReq = request.Folders?.Any() == true
                }
            };

            _ecmLogger.LogServiceTicket(true, "CreateTicket", null, "NewHire", $"Prepared ServiceDesk DTO - Requirements: Phone={serviceDeskDto.Requirements.HasPhoneRequirements}, Computer={serviceDeskDto.Requirements.HasComputerRequirements}, Tablet={serviceDeskDto.Requirements.HasTabletProfiles}, Building={serviceDeskDto.Requirements.HasBuildingAccess}, Apps={serviceDeskDto.Requirements.HasITApplications}, Software={serviceDeskDto.Requirements.HasSoftwareAccessReq}", employeeName: employeeFullName);

            // Call ServiceDesk integration service
            var result = await _serviceDeskService.CreateServiceDeskRecord(serviceDeskDto);

            if (result.Success && !string.IsNullOrEmpty(result.ServiceDeskTicketId))
            {
                _ecmLogger.LogServiceTicket(true, "CreateTicket", result.ServiceDeskTicketId, "NewHire", employeeName: employeeFullName);

                // Store ServiceDesk sync data in database
                try
                {
                    var syncData = new ServiceDeskSyncData
                    {
                        NewHireRequestId = newHireRequestDetailId,
                        ServiceDeskID = result.ServiceDeskTicketId,
                        HasPhoneRequirements = serviceDeskDto.Requirements.HasPhoneRequirements,
                        HasComputerRequirements = serviceDeskDto.Requirements.HasComputerRequirements,
                        HasTabletProfiles = serviceDeskDto.Requirements.HasTabletProfiles,
                        HasBuildingAccess = serviceDeskDto.Requirements.HasBuildingAccess,
                        HasITApplications = serviceDeskDto.Requirements.HasITApplications,
                        HasSoftwareAccessReq = serviceDeskDto.Requirements.HasSoftwareAccessReq,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = int.TryParse(currentUserId, out var id) ? id : 0
                    };

                    _context.ServiceDeskSyncDatas.Add(syncData);
                    await _context.SaveChangesAsync();

                    _ecmLogger.LogServiceTicket(true, "StoreSyncData", result.ServiceDeskTicketId, "NewHire", employeeName: employeeFullName);
                }
                catch (Exception syncEx)
                {
                    _ecmLogger.LogServiceTicket(false, "StoreSyncData", result.ServiceDeskTicketId, "NewHire", $"Failed to store sync data: {syncEx.Message}", employeeName: employeeFullName);
                }

                return true;
            }
            else
            {
                _ecmLogger.LogServiceTicket(false, "CreateTicket", null, "NewHire", $"ServiceDesk ticket creation failed: {result.Message}", employeeName: employeeFullName);
                return false;
            }
        }
        catch (Exception ex)
        {
            var employeeFullName = newHireDetail != null ? $"{newHireDetail.FirstName} {newHireDetail.LastName}".Trim() : null;
            _ecmLogger.LogServiceTicket(false, "CreateTicket", null, "NewHire", $"Exception: {ex.Message}", employeeName: employeeFullName);
            return false;
        }
    }

    /// <summary>
    /// Helper method to convert DateTime to milliseconds (Unix epoch)
    /// </summary>
    private long ConvertToMilliseconds(DateTime? dateTime)
    {
        if (!dateTime.HasValue) return 0;
        // Use date-only at midnight UTC to avoid timezone offset shifting the date back a day
        var dateOnly = DateTime.SpecifyKind(dateTime.Value.Date, DateTimeKind.Utc);
        return (long)(dateOnly - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }

    /// <summary>
    /// Enriches applications list with application names from the database
    /// </summary>
    private async Task<List<NewHireComputerRequirementDto>> EnrichComputerRequirementsWithDescriptions(List<NewHireComputerRequirementDto> computerRequirements)
    {
        if (computerRequirements == null || !computerRequirements.Any())
        {
            return computerRequirements ?? new List<NewHireComputerRequirementDto>();
        }

        try
        {
            var requirementIds = computerRequirements.Select(c => c.ComputerRequirementsId).ToList();

            var descriptions = await _context.ComputerRequirements
                .Where(c => requirementIds.Contains(c.Id) && !c.IsDeleted)
                .Select(c => new { c.Id, c.Description })
                .ToDictionaryAsync(c => c.Id, c => c.Description);

            foreach (var req in computerRequirements)
            {
                if (descriptions.TryGetValue(req.ComputerRequirementsId, out var description))
                {
                    req.ComputerRequirementsDescription = description;
                }
            }

            return computerRequirements;
        }
        catch (Exception ex)
        {
            _ecmLogger.LogServiceTicket(false, "EnrichData", null, "NewHire", $"Failed to enrich computer requirements: {ex.Message}");
            return computerRequirements;
        }
    }

    private async Task<List<NewHireApplicationRequestDto>> EnrichApplicationsWithNames(List<NewHireApplicationRequestDto> applications)
    {
        if (applications == null || !applications.Any())
        {
            return applications ?? new List<NewHireApplicationRequestDto>();
        }

        try
        {
            // Get all application IDs from the request
            var applicationIds = applications.Select(a => a.ApplicationId).ToList();

            // Query the database to get application names
            var applicationNames = await _context.Applications
                .Where(a => applicationIds.Contains(a.Id) && !a.IsDeleted)
                .Select(a => new { a.Id, a.Name })
                .ToDictionaryAsync(a => a.Id, a => a.Name);

            // Enrich the applications list with names
            foreach (var app in applications)
            {
                if (applicationNames.TryGetValue(app.ApplicationId, out var name))
                {
                    app.ApplicationName = name;
                }
            }

            return applications;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[APPLICATION ENRICHMENT] Error enriching applications with names: {ex.Message}");
            _logger.LogError(ex, "[APPLICATION ENRICHMENT] Failed to enrich applications with names");
            // Return the original applications list without names if there's an error
            return applications;
        }
    }
}
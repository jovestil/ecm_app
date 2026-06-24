using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Data;

namespace Mathy.ELM.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TerminationRequestsController : ControllerBase
{
    private readonly IHRRequestService _hrRequestService;
    private readonly ITerminationRequestDetailsService _terminationDetailsService;
    private readonly IEmployeeService _employeeService;
    private readonly IAzureServiceBusEmailService _emailService;
    private readonly IEmailRecipientsService _emailRecipientsService;
    private readonly IUserContextService _userContextService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IServiceDeskIntegrationService _serviceDeskService;
    private readonly IEcmLogger _ecmLogger;
    private readonly IConfiguration _configuration;
    private readonly MathyELMContext _context;
    private readonly ILogger<TerminationRequestsController> _logger;

    public TerminationRequestsController(
        IHRRequestService hrRequestService,
        ITerminationRequestDetailsService terminationDetailsService,
        IEmployeeService employeeService,
        IAzureServiceBusEmailService emailService,
        IEmailRecipientsService emailRecipientsService,
        IUserContextService userContextService,
        IBackgroundJobService backgroundJobService,
        IServiceDeskIntegrationService serviceDeskService,
        IEcmLogger ecmLogger,
        IConfiguration configuration,
        MathyELMContext context,
        ILogger<TerminationRequestsController> logger)
    {
        _hrRequestService = hrRequestService;
        _terminationDetailsService = terminationDetailsService;
        _employeeService = employeeService;
        _emailService = emailService;
        _emailRecipientsService = emailRecipientsService;
        _userContextService = userContextService;
        _backgroundJobService = backgroundJobService;
        _serviceDeskService = serviceDeskService;
        _ecmLogger = ecmLogger;
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create termination request: Create HR Request and Save Termination Details
    /// </summary>
    /// <param name="request">Termination request with HR request details</param>
    /// <returns>Success indicator with created request details</returns>
    [HttpPost("CreateTerminationRequest")]
    public async Task<ActionResult<ApiResponse<List<HRRequestDetailDto>>>> CreateTerminationRequest([FromBody] CreateSingleEmployeeHRRequestDto request)
    {
        if (request == null)
        {
            return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "HR request details are required"
            });
        }

        if (request.EmployeeId <= 0)
        {
            return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Employee ID is required"
            });
        }

        try
        {
            // Trim all string fields before saving
            request.Notes = request.Notes?.Trim();
            request.ProcessingNotes = request.ProcessingNotes?.Trim();
            request.RequestTitle = request.RequestTitle?.Trim();
            request.RequestDescription = request.RequestDescription?.Trim();
            if (request.TerminationDetails != null)
            {
                request.TerminationDetails.ReasonCode = request.TerminationDetails.ReasonCode?.Trim();
                request.TerminationDetails.ForwardEmail = request.TerminationDetails.ForwardEmail?.Trim();
                request.TerminationDetails.ForwardDeskPhone = request.TerminationDetails.ForwardDeskPhone?.Trim();
                request.TerminationDetails.ForwardCellPhone = request.TerminationDetails.ForwardCellPhone?.Trim();
                request.TerminationDetails.AutoReply = request.TerminationDetails.AutoReply?.Trim();
                request.TerminationDetails.GiveOneDriveAccessTo = request.TerminationDetails.GiveOneDriveAccessTo?.Trim();
                request.TerminationDetails.KwikCard4DigitNo = request.TerminationDetails.KwikCard4DigitNo?.Trim();
            }

            // Convert single employee to multi-employee format for consistency
            var multiEmployeeRequest = new CreateMultiEmployeeHRRequestDto
            {
                RequestTypeId = request.RequestTypeId,
                RequestTitle = request.RequestTitle,
                RequestDescription = request.RequestDescription,
                EffectiveDate = request.EffectiveDate,
                RequestedBy = request.RequestedBy,
                CompanyId = request.CompanyId,
                PayrollGroupId = request.PayrollGroupId,
                Notes = request.Notes,
                ProcessingNotes = request.ProcessingNotes,
                EmployeeIds = new List<int> { request.EmployeeId }
            };

            // Step 1: Create HR Request
            var hrRequestResult = await _hrRequestService.CreateMultiEmployeeHRRequestAsync(multiEmployeeRequest);
            
            if (!hrRequestResult.Success || hrRequestResult.Data == null || !hrRequestResult.Data.Any())
            {
                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = "Failed to create HR request.",
                    Errors = hrRequestResult.Errors
                });
            }

            // Step 2: Create Termination Request Details
            var hrRequestDetailIds = hrRequestResult.Data.Select(d => d.Id).ToList();
            var terminationResult = await _terminationDetailsService.CreateTerminationRequestDetailsAsync(hrRequestDetailIds, request.TerminationDetails);
            
            if (!terminationResult.Success)
            {
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
                }
                catch (Exception rollbackEx)
                {
                    // Log rollback failure but don't override the original error
                    return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                    {
                        Success = false,
                        Message = $"Failed to create Termination details AND failed to rollback HR request. Manual cleanup required. Original error: {terminationResult.Message}. Rollback error: {rollbackEx.Message}",
                        Errors = terminationResult.Errors
                    });
                }

                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = $"Failed to create Termination details. HR request has been rolled back. Error: {terminationResult.Message}",
                    Errors = terminationResult.Errors
                });
            }

            // Step 2.5: Create ServiceDesk record (non-blocking)
            try
            {
                var hrDetail = hrRequestResult.Data.First();
                var terminationDetailId = terminationResult.Data?.FirstOrDefault() ?? 0;

                await CreateTerminationServiceDeskRecordAsync(
                    terminationDetailId,
                    hrDetail.ParentRequestId,
                    request.EmployeeId,
                    request.EffectiveDate ?? DateTime.UtcNow,
                    request.TerminationDetails);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TERMINATION] Step 2.5 WARNING: ServiceDesk record creation failed");
                var employeeForLog = await _context.Employees
                    .Where(e => e.EmployeeNumber == request.EmployeeId && !e.IsDeleted)
                    .Select(e => new { e.FirstName, e.LastName })
                    .FirstOrDefaultAsync();
                var employeeFullName = employeeForLog != null ? $"{employeeForLog.FirstName} {employeeForLog.LastName}".Trim() : null;
                _ecmLogger.LogServiceTicket(false, "CreateTicket", null, "Termination", $"Exception: {ex.Message}", employeeName: employeeFullName);
                // Non-blocking - don't fail the entire request if ServiceDesk fails
            }

            // Step 3: Send confirmation email
            try
            {
                var hrDetail = hrRequestResult.Data.First();

                // Get employee data for email
                var employeeData = await _employeeService.GetEmployeesByHRRequestAsync(
                    "termination-request",
                    page: 1,
                    pageSize: 1,
                    isEditMode: false,
                    employeeIds: new int[] { request.EmployeeId }
                );

                var employee = employeeData.Data?.FirstOrDefault();

                // Get submitter email from user context
                var submitterEmail = _userContextService.GetUserEmail();

                // Get manager email from employee's supervisor
                var managerEmail = await GetManagerEmailAsync(employee?.SupervisorId);

                // Build email data DTO
                // EmployeeName, CompanyName, DivisionName, EmploymentStatus are null -
                // EmailFieldMapperService will look them up from database using EmployeeId
                var emailData = new TerminationEmailDataDto
                {
                    EmployeeId = request.EmployeeId,
                    CompanyCode = request.CompanyId ?? hrDetail.EmployeeCompanyCode,
                    DeptCode = hrDetail.EmployeeDepartmentCode,
                    EffectiveDate = request.EffectiveDate,
                    Notes = request.Notes,
                    Submitter = hrDetail.SubmittedByName,
                    ManagerEmail = managerEmail,
                    WithKwikTripCard = request.TerminationDetails?.WithKwikTripCard ?? false,
                    KwikCard4DigitNo = request.TerminationDetails?.KwikCard4DigitNo
                };

                // Get recipients from template with submitter and manager emails for special recipient resolution
                var recipients = await _emailRecipientsService.GetRecipientsFromTemplateAsync(
                    "Confirmation",
                    emailData.CompanyCode,
                    emailData.DeptCode ?? 0,
                    managerEmail,
                    submitterEmail,
                    null,
                    "TERMINATION"
                );

                if (recipients != null && recipients.Any())
                {
                    var toEmail = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

                    // Send confirmation email
                    var emailResult = await _emailService.SendEmailFromTemplateNameForTerminationAsync(
                        "Confirmation",
                        emailData,
                        toEmail,
                        null,
                        hrDetail.ParentRequestId
                    );

                    if (emailResult.Success)
                    {
                        _logger.LogInformation("Termination confirmation email sent successfully for employee {EmployeeId}", request.EmployeeId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send termination confirmation email for employee {EmployeeId}: {Message}",
                            request.EmployeeId, emailResult.Message);
                    }
                }
                else
                {
                    _logger.LogWarning("No recipients found for termination confirmation email for employee {EmployeeId}", request.EmployeeId);
                }
            }
            catch (Exception emailEx)
            {
                // Log but don't fail the request if email fails
                _logger.LogError(emailEx, "Error sending termination confirmation email for employee {EmployeeId}", request.EmployeeId);
            }

            // Step 4: Send all immediate task email notifications
            // Termination task emails are sent directly without conditionals (unlike Promotion)
            try
            {
                var hrDetail = hrRequestResult.Data.First();

                // Get submitter email from user context
                var submitterEmail = _userContextService.GetUserEmail();

                // Get manager email from employee's supervisor
                var employeeData = await _employeeService.GetEmployeesByHRRequestAsync(
                    "termination-request",
                    page: 1,
                    pageSize: 1,
                    isEditMode: false,
                    employeeIds: new int[] { request.EmployeeId }
                );
                var employee = employeeData.Data?.FirstOrDefault();
                var managerEmail = await GetManagerEmailAsync(employee?.SupervisorId);

                // Build email data DTO for task emails
                var taskEmailData = new TerminationEmailDataDto
                {
                    EmployeeId = request.EmployeeId,
                    CompanyCode = request.CompanyId ?? hrDetail.EmployeeCompanyCode,
                    DeptCode = hrDetail.EmployeeDepartmentCode,
                    EffectiveDate = request.EffectiveDate,
                    Notes = request.Notes,
                    Submitter = hrDetail.SubmittedByName,
                    ManagerEmail = managerEmail,
                    WithKwikTripCard = request.TerminationDetails?.WithKwikTripCard ?? false,
                    KwikCard4DigitNo = request.TerminationDetails?.KwikCard4DigitNo
                };

                await SendTerminationTaskEmailsAsync(
                    taskEmailData,
                    hrDetail.ParentRequestId,
                    managerEmail,
                    submitterEmail);
            }
            catch (Exception taskEmailEx)
            {
                // Log but don't fail the request if task emails fail
                _logger.LogError(taskEmailEx, "Error sending termination task emails for employee {EmployeeId}", request.EmployeeId);
            }

            // Step 5: Trigger overdue scheduled emails for termination request
            // Check if any scheduled emails should be sent immediately based on EffectiveDate and SubmissionFreq
            try
            {
                var hrDetail = hrRequestResult.Data.First();
                await _backgroundJobService.TriggerOverdueScheduledEmailsForTerminationAsync(hrDetail.Id);
                _logger.LogInformation("Triggered overdue scheduled emails check for Termination HRRequestDetailId={DetailId}, EmployeeId={EmployeeId}",
                    hrDetail.Id, hrDetail.EmployeeId);
            }
            catch (Exception triggerEx)
            {
                // Log but don't fail the request if trigger fails
                _logger.LogError(triggerEx, "Error triggering overdue scheduled emails for termination employee {EmployeeId}", request.EmployeeId);
            }

            // Success response
            var response = new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = true,
                Data = hrRequestResult.Data,
                Message = $"Successfully completed termination request for employee ID {request.EmployeeId}"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = $"An error occurred during termination request processing: {ex.Message}",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get termination request details by parent HR request ID
    /// </summary>
    /// <param name="parentId">Parent HR request ID</param>
    /// <returns>Combined HR request and termination details</returns>
    [HttpGet("GetTerminationDetailsByParentId/{parentId}")]
    public async Task<ActionResult<ApiResponse<object>>> GetTerminationDetailsByParentId(int parentId)
    {
        if (parentId <= 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid parent request ID"
            });
        }

        try
        {
            // Get parent HR request data for notes
            var parentHRRequest = await _hrRequestService.GetHRRequestByIdAsync(parentId);
            
            if (!parentHRRequest.Success || parentHRRequest.Data == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Parent HR request not found"
                });
            }

            // Get HR request details first
            var hrRequestDetails = await _hrRequestService.GetHRRequestDetailsAsync(parentId);
            
            if (!hrRequestDetails.Success || hrRequestDetails.Data == null || !hrRequestDetails.Data.Any())
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "HR request details not found"
                });
            }

            // For termination requests, there should typically be only one detail
            var hrRequestDetail = hrRequestDetails.Data.First();

            // Get termination-specific details
            var terminationDetails = await _terminationDetailsService.GetByHRRequestDetailIdAsync(hrRequestDetail.Id);
            
            if (!terminationDetails.Success || terminationDetails.Data == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Termination request details not found"
                });
            }

            // Get properly formatted employee data using GetEmployeesByHRRequest
            var employeeData = await _employeeService.GetEmployeesByHRRequestAsync(
                "termination-request", 
                page: 1, 
                pageSize: 1, 
                isEditMode: true,
                employeeIds: new int[] { hrRequestDetail.EmployeeId }
            );

            EmployeeDto? formattedEmployeeDetail = null;
            if (employeeData.Success && employeeData.Data != null && employeeData.Data.Any())
            {
                formattedEmployeeDetail = employeeData.Data.First();
            }

            // Combine the data
            var combinedData = new
            {
                HRRequest = parentHRRequest.Data,
                HRRequestDetail = hrRequestDetail,
                TerminationDetail = terminationDetails.Data,
                EmployeeDetail = formattedEmployeeDetail
            };

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = combinedData,
                Message = "Termination request details retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"An error occurred while retrieving termination details: {ex.Message}",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Helper method to get manager email from supervisor ID
    /// </summary>
    private async Task<string> GetManagerEmailAsync(int? supervisorId)
    {
        if (!supervisorId.HasValue)
        {
            return "";
        }

        try
        {
            var supervisor = await _context.Employees
                .Where(e => e.EmployeeNumber == supervisorId.Value && !e.IsDeleted)
                .FirstOrDefaultAsync();

            if (supervisor != null && !string.IsNullOrEmpty(supervisor.WorkEmail))
            {
                return supervisor.WorkEmail;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving supervisor email for ID {SupervisorId}", supervisorId);
        }

        return "";
    }

    /// <summary>
    /// Send all immediate task email notifications triggered by Termination submission
    /// Queries EmailTemplates for RequestType = 'TERMINATION', TriggerType = 'Immediate', TemplateName containing 'Task'
    /// No conditionals needed - all task emails are sent directly after submission
    /// </summary>
    private async Task SendTerminationTaskEmailsAsync(
        TerminationEmailDataDto emailData,
        int requestId,
        string? managerEmail,
        string? submitterEmail)
    {
        _logger.LogInformation("[TERMINATION TASK EMAILS] Starting task email notifications for request ID: {RequestId}", requestId);

        try
        {
            // Get all Termination task email templates with TriggerType = 'Immediate'
            var taskTemplates = await _context.EmailTemplates
                .Where(t => t.RequestType == "TERMINATION" &&
                           t.TriggerType == "Immediate" &&
                           t.TemplateName.Contains("Task") &&
                           t.IsActive &&
                           !t.IsDeleted)
                .ToListAsync();

            if (!taskTemplates.Any())
            {
                _logger.LogInformation("[TERMINATION TASK EMAILS] No immediate task email templates found for TERMINATION");
                return;
            }

            _logger.LogInformation("[TERMINATION TASK EMAILS] Found {Count} task email templates to send", taskTemplates.Count);

            foreach (var template in taskTemplates)
            {
                try
                {
                    // Resolve recipients from EmailTemplate.Recipients field
                    var recipients = await _emailRecipientsService.GetRecipientsFromTemplateAsync(
                        template.TemplateName,
                        emailData.CompanyCode,
                        emailData.DeptCode ?? 0,
                        managerEmail,
                        submitterEmail,
                        null,
                        requestType: "TERMINATION");

                    if (!recipients.Any())
                    {
                        _logger.LogWarning("[TERMINATION TASK EMAILS] No recipients resolved for template '{TemplateName}'", template.TemplateName);
                        continue;
                    }

                    var toEmails = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

                    // Send email using termination-specific email method
                    var result = await _emailService.SendEmailFromTemplateNameForTerminationAsync(
                        template.TemplateName,
                        emailData,
                        toEmails,
                        null,
                        requestId);

                    if (result.Success)
                    {
                        _logger.LogInformation("[TERMINATION TASK EMAILS] Email '{TemplateName}' sent successfully to {RecipientCount} recipient(s)",
                            template.TemplateName, recipients.Count);
                    }
                    else
                    {
                        _logger.LogWarning("[TERMINATION TASK EMAILS] Failed to send email '{TemplateName}': {Message}",
                            template.TemplateName, result.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[TERMINATION TASK EMAILS] Error sending template email '{TemplateName}' for request {RequestId}",
                        template.TemplateName, requestId);
                    // Continue with other templates even if one fails
                }
            }

            _logger.LogInformation("[TERMINATION TASK EMAILS] All task notifications processed for request ID: {RequestId}", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TERMINATION TASK EMAILS] Critical error sending task emails for request {RequestId}", requestId);
            // Don't throw - we don't want to block request creation if email fails
        }
    }

    /// <summary>
    /// Creates a ServiceDesk ticket for a termination request (Off-Boarding template).
    /// Non-blocking: failures are logged via IEcmLogger but do not fail the overall request.
    /// </summary>
    private async Task<bool> CreateTerminationServiceDeskRecordAsync(
        int terminationRequestDetailId,
        int parentHRRequestId,
        int employeeId,
        DateTime offBoardDate,
        CreateTerminationRequestDto? terminationDetails)
    {
        Employee? employee = null;
        try
        {
            var isServiceDeskEnabled = _configuration.GetSection("ServiceDeskPlus:IsActive").Value?.ToLower() == "yes";
            if (!isServiceDeskEnabled)
            {
                _ecmLogger.LogServiceTicket(false, "CreateTicket", null, "Termination", "ServiceDesk integration is disabled in appsettings");
                return false;
            }

            employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeId && !e.IsDeleted);

            if (employee == null)
            {
                _ecmLogger.LogServiceTicket(false, "CreateTicket", null, "Termination", $"Employee {employeeId} not found for ServiceDesk record");
                return false;
            }

            var employeeFullName = $"{employee.FirstName} {employee.LastName}".Trim();

            if (string.IsNullOrEmpty(employee.FirstName) || string.IsNullOrEmpty(employee.LastName))
            {
                _ecmLogger.LogServiceTicket(false, "CreateTicket", null, "Termination", "Missing required fields (FirstName, LastName) for ServiceDesk record", employeeName: employeeFullName);
                return false;
            }

            var requestorName = _userContextService.GetUserDisplayName();

            _ecmLogger.LogServiceTicket(true, "CreateTicket", null, "Termination", $"Creating ticket for {employeeFullName}", employeeName: employeeFullName);

            var serviceDeskDto = new CreateTerminationServiceDeskRecordDto
            {
                TerminationRequestDetailId = terminationRequestDetailId,
                ParentHRRequestId = parentHRRequestId,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                OffBoardDate = offBoardDate,
                OffBoardDateMilliseconds = new DateTimeOffset(offBoardDate.Date).ToUnixTimeMilliseconds(),
                RequestorName = requestorName,
                ForwardEmail = terminationDetails?.ForwardEmail,
                EmailAutoReply = terminationDetails?.AutoReply,
                ForwardDeskPhone = terminationDetails?.ForwardDeskPhone,
                ForwardCellPhone = terminationDetails?.ForwardCellPhone,
                OneDriveAccessTo = terminationDetails?.GiveOneDriveAccessTo,
                ReclaimEquipment = null // Field not yet surfaced in ELM UI — legacy null-default emits "No IT Equipment to reclaim"
            };

            var result = await _serviceDeskService.CreateTerminationServiceDeskRecord(serviceDeskDto);

            if (result.Success && !string.IsNullOrEmpty(result.ServiceDeskTicketId))
            {
                _ecmLogger.LogServiceTicket(true, "CreateTicket", result.ServiceDeskTicketId, "Termination", employeeName: employeeFullName);
                return true;
            }

            _ecmLogger.LogServiceTicket(false, "CreateTicket", null, "Termination", $"ServiceDesk ticket creation failed: {result.Message}", employeeName: employeeFullName);
            return false;
        }
        catch (Exception ex)
        {
            var employeeFullName = employee != null ? $"{employee.FirstName} {employee.LastName}".Trim() : null;
            _ecmLogger.LogServiceTicket(false, "CreateTicket", null, "Termination", $"Exception: {ex.Message}", employeeName: employeeFullName);
            return false;
        }
    }
}
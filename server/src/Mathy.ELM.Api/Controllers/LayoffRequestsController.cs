using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Data;

namespace Mathy.ELM.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LayoffRequestsController : ControllerBase
{
    private readonly IHRRequestService _hrRequestService;
    private readonly ILayoffRequestDetailsService _layoffDetailsService;
    private readonly IAzureServiceBusEmailService _emailService;
    private readonly IEmailRecipientsService _emailRecipientsService;
    private readonly IUserContextService _userContextService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly MathyELMContext _context;
    private readonly ILogger<LayoffRequestsController> _logger;

    public LayoffRequestsController(
        IHRRequestService hrRequestService,
        ILayoffRequestDetailsService layoffDetailsService,
        IAzureServiceBusEmailService emailService,
        IEmailRecipientsService emailRecipientsService,
        IUserContextService userContextService,
        IBackgroundJobService backgroundJobService,
        MathyELMContext context,
        ILogger<LayoffRequestsController> logger)
    {
        _hrRequestService = hrRequestService;
        _layoffDetailsService = layoffDetailsService;
        _emailService = emailService;
        _emailRecipientsService = emailRecipientsService;
        _userContextService = userContextService;
        _backgroundJobService = backgroundJobService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create layoff request: Create HR Request and Save Layoff Details
    /// </summary>
    /// <param name="request">Layoff request with HR request details</param>
    /// <returns>Success indicator with created request details</returns>
    [HttpPost("CreateLayoffRequest")]
    public async Task<ActionResult<ApiResponse<List<HRRequestDetailDto>>>> CreateLayoffRequest([FromBody] CreateMultiEmployeeHRRequestDto request)
    {
        if (request == null)
        {
            return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "HR request details are required"
            });
        }

        if (request.EmployeeIds == null || !request.EmployeeIds.Any())
        {
            return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Employee list is required and cannot be empty"
            });
        }

        try
        {
            // Trim all string fields before saving
            request.Notes = request.Notes?.Trim();
            request.ProcessingNotes = request.ProcessingNotes?.Trim();
            request.RequestTitle = request.RequestTitle?.Trim();
            request.RequestDescription = request.RequestDescription?.Trim();

            // Step 1: Create HR Request
            var hrRequestResult = await _hrRequestService.CreateMultiEmployeeHRRequestAsync(request);
            
            if (!hrRequestResult.Success || hrRequestResult.Data == null || !hrRequestResult.Data.Any())
            {
                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = "Failed to create HR request.",
                    Errors = hrRequestResult.Errors
                });
            }

            // Step 2: Create Layoff Request Details
            var hrRequestDetailIds = hrRequestResult.Data.Select(d => d.Id).ToList();
            var layoffResult = await _layoffDetailsService.CreateLayoffRequestDetailsAsync(hrRequestDetailIds);
            
            if (!layoffResult.Success)
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
                        Message = $"Failed to create Layoff details AND failed to rollback HR request. Manual cleanup required. Original error: {layoffResult.Message}. Rollback error: {rollbackEx.Message}",
                        Errors = layoffResult.Errors
                    });
                }

                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = $"Failed to create Layoff details. HR request has been rolled back. Error: {layoffResult.Message}",
                    Errors = layoffResult.Errors
                });
            }

            // Step 3: Send confirmation emails for each employee
            try
            {
                var submitterEmail = _userContextService.GetUserEmail();

                foreach (var hrDetail in hrRequestResult.Data)
                {
                    try
                    {
                        // Get manager email from employee's supervisor
                        var employee = await _context.Employees
                            .Where(e => e.EmployeeNumber == hrDetail.EmployeeId && !e.IsDeleted)
                            .FirstOrDefaultAsync();

                        var managerEmail = await GetManagerEmailAsync(employee?.SupervisorId);

                        // Build email data DTO
                        // EmployeeName, CompanyName, DivisionName, EmploymentStatus are null -
                        // EmailFieldMapperService will look them up from database using EmployeeId
                        var emailData = new LayoffEmailDataDto
                        {
                            EmployeeId = hrDetail.EmployeeId,
                            CompanyCode = hrDetail.EmployeeCompanyCode,
                            DeptCode = hrDetail.EmployeeDepartmentCode,
                            EffectiveDate = request.EffectiveDate,
                            Notes = request.Notes,
                            Submitter = hrDetail.SubmittedByName,
                            ManagerEmail = managerEmail
                        };

                        // Get recipients from template with submitter and manager emails for special recipient resolution
                        var recipients = await _emailRecipientsService.GetRecipientsFromTemplateAsync(
                            "Confirmation",
                            emailData.CompanyCode,
                            emailData.DeptCode ?? 0,
                            managerEmail,
                            submitterEmail,
                            null,
                            "LAYOFF"
                        );

                        if (recipients != null && recipients.Any())
                        {
                            var toEmail = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

                            // Send confirmation email
                            var emailResult = await _emailService.SendEmailFromTemplateNameForLayoffAsync(
                                "Confirmation",
                                emailData,
                                toEmail,
                                null,
                                hrDetail.ParentRequestId
                            );

                            if (emailResult.Success)
                            {
                                _logger.LogInformation("Layoff confirmation email sent successfully for employee {EmployeeId}", hrDetail.EmployeeId);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to send layoff confirmation email for employee {EmployeeId}: {Message}",
                                    hrDetail.EmployeeId, emailResult.Message);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No recipients found for layoff confirmation email for employee {EmployeeId}", hrDetail.EmployeeId);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Log but don't fail the request if email fails for one employee
                        _logger.LogError(emailEx, "Error sending layoff confirmation email for employee {EmployeeId}", hrDetail.EmployeeId);
                    }
                }
            }
            catch (Exception emailEx)
            {
                // Log but don't fail the request if email fails
                _logger.LogError(emailEx, "Error sending layoff confirmation emails");
            }

            // Step 4: Trigger immediate scheduled emails if their trigger dates have already passed
            // Uses HRRequestDetailId to support multi-employee requests (each employee gets their own email)
            try
            {
                foreach (var hrDetail in hrRequestResult.Data)
                {
                    await _backgroundJobService.TriggerOverdueScheduledEmailsForLayoffAsync(hrDetail.Id);
                    _logger.LogInformation("Triggered overdue scheduled emails check for Layoff HRRequestDetailId={DetailId}, EmployeeId={EmployeeId}",
                        hrDetail.Id, hrDetail.EmployeeId);
                }
            }
            catch (Exception triggerEx)
            {
                // Log but don't fail the request if trigger fails
                _logger.LogError(triggerEx, "Error triggering overdue scheduled emails for Layoff requests");
            }

            // Success response
            var response = new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = true,
                Data = hrRequestResult.Data,
                Message = $"Successfully completed layoff request for {request.EmployeeIds.Count} employee(s)"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = $"An error occurred during layoff request processing: {ex.Message}",
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
}
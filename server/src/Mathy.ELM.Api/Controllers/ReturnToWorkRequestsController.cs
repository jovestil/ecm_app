using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Data;

namespace Mathy.ELM.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ReturnToWorkRequestsController : ControllerBase
{
    private readonly IViewpointService _viewpointService;
    private readonly IEmployeeService _employeeService;
    private readonly IHRRequestService _hrRequestService;
    private readonly IReturnToWorkRequestDetailsService _returnToWorkDetailsService;
    private readonly IAzureServiceBusEmailService _emailService;
    private readonly IEmailRecipientsService _emailRecipientsService;
    private readonly IUserContextService _userContextService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly MathyELMContext _context;
    private readonly ILogger<ReturnToWorkRequestsController> _logger;

    public ReturnToWorkRequestsController(
        IViewpointService viewpointService,
        IEmployeeService employeeService,
        IHRRequestService hrRequestService,
        IReturnToWorkRequestDetailsService returnToWorkDetailsService,
        IAzureServiceBusEmailService emailService,
        IEmailRecipientsService emailRecipientsService,
        IUserContextService userContextService,
        IBackgroundJobService backgroundJobService,
        MathyELMContext context,
        ILogger<ReturnToWorkRequestsController> logger)
    {
        _viewpointService = viewpointService;
        _employeeService = employeeService;
        _hrRequestService = hrRequestService;
        _returnToWorkDetailsService = returnToWorkDetailsService;
        _emailService = emailService;
        _emailRecipientsService = emailRecipientsService;
        _userContextService = userContextService;
        _backgroundJobService = backgroundJobService;
        _context = context;
        _logger = logger;
    }
    
    [HttpPost("UpdateEmployeeFromViewpointForReturnToWork")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateEmployeeFromViewpointForReturnToWork([FromBody] List<ViewpointEmployeeDto> employees)
    {
        if (employees == null || !employees.Any())
        {
            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Employee list is required and cannot be empty"
            });
        }

        // Validate each employee
        foreach (var employee in employees)
        {
            if (!employee.HRCo.HasValue || !employee.PREmp.HasValue)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Employee {employee.PREmp ?? 0} must have valid HRCo and PREmp values"
                });
            }
        }

        try
        {
            var result = await _viewpointService.UpdateEmployeeFromViewpointForReturnToWorkAsync(employees);
            
            if (result)
            {
                // Update employment status in local database for each employee
                var localUpdateErrors = new List<string>();
                
                foreach (var employee in employees)
                {
                    if (employee.PREmp.HasValue)
                    {
                        var localUpdateResult = await _employeeService.UpdateEmploymentStatusAsync(employee.PREmp.Value, "U-ACTIVE");
                        if (!localUpdateResult.Success)
                        {
                            localUpdateErrors.Add($"Employee {employee.PREmp.Value}: {localUpdateResult.Message}");
                        }
                    }
                }

                // Determine response based on local update results
                if (localUpdateErrors.Any())
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Success = false,
                        Data = true,
                        Message = $"Viewpoint update succeeded for {employees.Count} employee(s), but some local database updates failed.",
                        Errors = localUpdateErrors
                    });
                }
                
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = $"All {employees.Count} employee(s) status successfully updated to U-ACTIVE in both Viewpoint and local database"
                });
            }
            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Some or all employees failed to update in Viewpoint. Check logs for details."
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"An error occurred while updating employee status: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Create return to work request: Update Viewpoint, Create HR Request, and Save ReturnToWork Details
    /// </summary>
    /// <param name="request">Return to work request with employees and HR request details</param>
    /// <returns>Success indicator with created request details</returns>
    [HttpPost("CreateReturnToWorkRequest")]
    public async Task<ActionResult<ApiResponse<List<HRRequestDetailDto>>>> CreateReturnToWorkRequest([FromBody] CompleteReturnToWorkRequestDto request)
    {
        if (request?.Employees == null || !request.Employees.Any())
        {
            return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "Employee list is required and cannot be empty"
            });
        }

        if (request.HRRequest == null)
        {
            return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "HR request details are required"
            });
        }

        // Validate each employee
        foreach (var employee in request.Employees)
        {
            if (!employee.HRCo.HasValue || !employee.PREmp.HasValue)
            {
                return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = $"Employee {employee.PREmp ?? 0} must have valid HRCo and PREmp values"
                });
            }
        }

        try
        {
            // Trim all string fields before saving
            if (request.HRRequest != null)
            {
                request.HRRequest.Notes = request.HRRequest.Notes?.Trim();
                request.HRRequest.ProcessingNotes = request.HRRequest.ProcessingNotes?.Trim();
                request.HRRequest.RequestTitle = request.HRRequest.RequestTitle?.Trim();
                request.HRRequest.RequestDescription = request.HRRequest.RequestDescription?.Trim();
            }

            // Step 1: Create HR Request
            var hrRequestResult = await _hrRequestService.CreateMultiEmployeeHRRequestAsync(request.HRRequest);
            
            if (!hrRequestResult.Success || hrRequestResult.Data == null || !hrRequestResult.Data.Any())
            {
                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = "Failed to create HR request.",
                    Errors = hrRequestResult.Errors
                });
            }

            // Step 2: Create ReturnToWork Request Details
            var hrRequestDetailIds = hrRequestResult.Data.Select(d => d.Id).ToList();
            var returnToWorkResult = await _returnToWorkDetailsService.CreateReturnToWorkRequestDetailsAsync(hrRequestDetailIds);
            
            if (!returnToWorkResult.Success)
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
                        Message = $"Failed to create ReturnToWork details AND failed to rollback HR request. Manual cleanup required. Original error: {returnToWorkResult.Message}. Rollback error: {rollbackEx.Message}",
                        Errors = returnToWorkResult.Errors
                    });
                }

                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = $"Failed to create ReturnToWork details. HR request has been rolled back. Error: {returnToWorkResult.Message}",
                    Errors = returnToWorkResult.Errors
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
                        var emailData = new ReturnToWorkEmailDataDto
                        {
                            EmployeeId = hrDetail.EmployeeId,
                            CompanyCode = hrDetail.EmployeeCompanyCode,
                            DeptCode = hrDetail.EmployeeDepartmentCode,
                            EffectiveDate = request.HRRequest.EffectiveDate,
                            Notes = request.HRRequest.Notes,
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
                            "RETURNTOWORK"
                        );

                        if (recipients != null && recipients.Any())
                        {
                            var toEmail = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

                            // Send confirmation email
                            var emailResult = await _emailService.SendEmailFromTemplateNameForReturnToWorkAsync(
                                "Confirmation",
                                emailData,
                                toEmail,
                                null,
                                hrDetail.ParentRequestId
                            );

                            if (emailResult.Success)
                            {
                                _logger.LogInformation("Return to work confirmation email sent successfully for employee {EmployeeId}", hrDetail.EmployeeId);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to send return to work confirmation email for employee {EmployeeId}: {Message}",
                                    hrDetail.EmployeeId, emailResult.Message);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No recipients found for return to work confirmation email for employee {EmployeeId}", hrDetail.EmployeeId);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Log but don't fail the request if email fails for one employee
                        _logger.LogError(emailEx, "Error sending return to work confirmation email for employee {EmployeeId}", hrDetail.EmployeeId);
                    }
                }
            }
            catch (Exception emailEx)
            {
                // Log but don't fail the request if email fails
                _logger.LogError(emailEx, "Error sending return to work confirmation emails");
            }

            // Step 4: Trigger immediate scheduled emails if their trigger dates have already passed
            // Uses HRRequestDetailId to support multi-employee requests (each employee gets their own email)
            try
            {
                foreach (var hrDetail in hrRequestResult.Data)
                {
                    await _backgroundJobService.TriggerOverdueScheduledEmailsForReturnToWorkAsync(hrDetail.Id);
                    _logger.LogInformation("Triggered overdue scheduled emails check for Return to Work HRRequestDetailId={DetailId}, EmployeeId={EmployeeId}",
                        hrDetail.Id, hrDetail.EmployeeId);
                }
            }
            catch (Exception triggerEx)
            {
                // Log but don't fail the request if trigger fails
                _logger.LogError(triggerEx, "Error triggering overdue scheduled emails for Return to Work requests");
            }

            // Success response
            var response = new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = true,
                Data = hrRequestResult.Data,
                Message = $"Successfully completed return to work request for {request.Employees.Count} employee(s)"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = $"An error occurred during return to work request processing: {ex.Message}",
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
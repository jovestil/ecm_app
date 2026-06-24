using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Hangfire;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Data;
using Mathy.ELM.Infrastructure.Extensions;
using RequestStatusEnum = Mathy.ELM.Core.Enums.RequestStatus;

namespace Mathy.ELM.Infrastructure.Services;

public class HRRequestService : IHRRequestService
{
    private readonly MathyELMContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HRRequestService> _logger;
    private readonly IEcmLogger _ecmLogger;
    private readonly IRoleFilterService _roleFilterService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAzureServiceBusEmailService _emailService;
    private readonly IEmailRecipientsService _emailRecipientsService;
    private readonly IEmailFieldMapperService _fieldMapperService;

    public HRRequestService(
        MathyELMContext context,
        IUserContextService userContextService,
        IServiceProvider serviceProvider,
        ILogger<HRRequestService> logger,
        IEcmLogger ecmLogger,
        IRoleFilterService roleFilterService,
        IHttpContextAccessor httpContextAccessor,
        IAzureServiceBusEmailService emailService,
        IEmailRecipientsService emailRecipientsService,
        IEmailFieldMapperService fieldMapperService)
    {
        _context = context;
        _userContextService = userContextService;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _ecmLogger = ecmLogger;
        _roleFilterService = roleFilterService;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _emailRecipientsService = emailRecipientsService;
        _fieldMapperService = fieldMapperService;
    }

    public async Task<PagedResponse<List<HRRequestDto>>> GetHRRequestsAsync(
        int page = 1, 
        int pageSize = 25, 
        int? requestTypeId = null, 
        int? statusId = null, 
        int? submittedBy = null)
    {
        var query = _context.HRRequests
            .Include(r => r.Details)
                .ThenInclude(d => d.RequestType)
            .Include(r => r.Details)
                .ThenInclude(d => d.RequestStatus)
            .Where(r => !r.IsDeleted);

        // Apply filters
        if (requestTypeId.HasValue)
        {
            query = query.Where(r => r.Details.Any(d => d.RequestTypeId == requestTypeId.Value));
        }

        if (statusId.HasValue)
        {
            query = query.Where(r => r.Details.Any(d => d.RequestStatusId == statusId.Value));
        }

        if (submittedBy.HasValue)
        {
            query = query.Where(r => r.SubmittedBy == submittedBy.Value);
        }

        var totalCount = await query.CountAsync();
        
        var requests = await query
            .OrderByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var requestDtos = await MapToDtoWithEmployeeNames(requests);

        return new PagedResponse<List<HRRequestDto>>
        {
            Success = true,
            Data = requestDtos,
            Message = $"Retrieved {requestDtos.Count} HR requests",
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ApiResponse<HRRequestDto>> GetHRRequestByIdAsync(int id)
    {
        var request = await _context.HRRequests
            .Include(r => r.Details)
                .ThenInclude(d => d.RequestType)
            .Include(r => r.Details)
                .ThenInclude(d => d.RequestStatus)
            .Include(r => r.Details)
                .ThenInclude(d => d.PromotionDetails)
            .Include(r => r.Details)
                .ThenInclude(d => d.LayoffDetails)
            .Include(r => r.Details)
                .ThenInclude(d => d.TerminationDetails)
            .Include(r => r.Details)
                .ThenInclude(d => d.ReturnToWorkDetails)
            .Include(r => r.Details)
                .ThenInclude(d => d.NewHireDetails)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (request == null)
        {
            return new ApiResponse<HRRequestDto>
            {
                Success = false,
                Message = "HR request not found"
            };
        }

        var requestDtos = await MapToDtoWithEmployeeNames(new List<HRRequest> { request });

        return new ApiResponse<HRRequestDto>
        {
            Success = true,
            Data = requestDtos.First(),
            Message = "HR request retrieved successfully"
        };
    }

    public async Task<ApiResponse<List<HRRequestDetailDto>>> CreateHRRequestAsync(CreateHRRequestDto createDto)
    {
        try
        {
            var currentUserEmail = _userContextService.GetUserEmail();
            var currentUserId = _userContextService.GetUserEmployeeNumber();
            var currentUserName = _userContextService.GetUserDisplayName();

            // Get employee department codes from Employee entity
            var employeeIds = createDto.Details.Select(d => d.EmployeeId).Distinct().ToList();
            var employees = await _context.Employees
                .Where(e => !e.IsDeleted && employeeIds.Contains(e.EmployeeNumber))
                .ToListAsync();

            var employeeDepartmentLookup = employees.ToDictionary(
                e => e.EmployeeNumber,
                e => e.PayrollDeptCode
            );

            var hrRequest = new HRRequest
            {
                SubmittedBy = currentUserId,
                SubmitterName = currentUserName,
                SubmittedDate = DateTime.UtcNow,
                SubmitterEmail = currentUserEmail,
                Notes = createDto.Notes,
                CreatedBy = currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            foreach (var detailDto in createDto.Details)
            {
                var detail = new HRRequestDetail
                {
                    RequestTypeId = detailDto.RequestTypeId,
                    RequestStatusId = 1, // Default to 'Pending'
                    EmployeeId = detailDto.EmployeeId,
                    EmployeeNetworkId = detailDto.EmployeeNetworkId,
                    EmployeePositionCode = detailDto.EmployeePositionCode,
                    EmployeeCompanyCode = detailDto.EmployeeCompanyCode,
                    EmployeeDepartmentCode = employeeDepartmentLookup.GetValueOrDefault(detailDto.EmployeeId, detailDto.EmployeeDepartmentCode),
                    EffectiveDate = detailDto.EffectiveDate,
                    ProcessingNotes = detailDto.ProcessingNotes,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow
                };

                hrRequest.Details.Add(detail);
            }

            _context.HRRequests.Add(hrRequest);
            await _context.SaveChangesAsync();

            _ecmLogger.LogSave(true, "HRRequest", hrRequest.Id, currentUserEmail);

            // Return the created details
            var createdDetails = await GetHRRequestDetailsAsync(hrRequest.Id);
            return createdDetails;
        }
        catch (Exception ex)
        {
            _ecmLogger.LogSave(false, "HRRequest", null, null, ex.Message);
            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = $"Failed to create HR request: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<List<HRRequestDetailDto>>> CreateMultiEmployeeHRRequestAsync(CreateMultiEmployeeHRRequestDto createDto)
    {
        try
        {
            var currentUserId = _userContextService.GetUserEmployeeNumber();
            var currentUserEmail = _userContextService.GetUserEmail();
            var currentUserName = _userContextService.GetUserDisplayName();

            var hrRequest = new HRRequest
            {
                SubmittedBy = currentUserId,
                SubmitterName = currentUserName,
                SubmittedDate = DateTime.UtcNow,
                SubmitterEmail = currentUserEmail,
                Notes = createDto.Notes,
                CreatedBy = currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            // Handle employee information based on request type
            List<object> employeeData = new List<object>();

            // For new hire requests (RequestTypeId = 5), we don't look up existing employees
            if (createDto.RequestTypeId != 5)
            {
                // Fetch employee information for existing employees (layoffs, terminations, etc.)
                employeeData = await _context.Employees
                    .ApplyRoleBasedFilter(_roleFilterService, _httpContextAccessor, _userContextService, _context.PayrollDepartmentShortNames, _logger)
                    .Where(e => createDto.EmployeeIds.Contains(e.EmployeeNumber))
                    .Select(e => new { e.EmployeeNumber, e.CompanyCode, e.NetworkId, e.PositionCode, e.PayrollDeptCode })
                    .Cast<object>()
                    .ToListAsync();
            }

            foreach (var employeeId in createDto.EmployeeIds)
            {
                object employee = null;
                int? companyCode = null;
                string networkId = null;
                string positionCode = null;
                int? payrollDeptCode = null;

                if (createDto.RequestTypeId == 5) // New Hire
                {
                    // For new hires, use the company information from the request DTO
                    companyCode = createDto.CompanyId;
                    // NetworkId and other fields will be null for new hires until they're created in Viewpoint
                    networkId = null;
                    positionCode = null;
                    payrollDeptCode = createDto.PayrollGroupId;
                }
                else
                {
                    // For existing employees, use lookup data
                    var empData = employeeData.Cast<dynamic>().FirstOrDefault(e => e.EmployeeNumber == employeeId);
                    if (empData != null)
                    {
                        companyCode = empData.CompanyCode;
                        networkId = empData.NetworkId;
                        positionCode = empData.PositionCode;
                        payrollDeptCode = empData.PayrollDeptCode;
                    }
                }

                var detail = new HRRequestDetail
                {
                    RequestTypeId = createDto.RequestTypeId,
                    RequestStatusId = (int)Core.Enums.RequestStatus.Pending, // Default to 'Pending'
                    EmployeeId = employeeId,
                    EmployeeCompanyCode = companyCode,
                    EmployeeNetworkId = networkId,
                    EmployeePositionCode = positionCode,
                    EmployeeDepartmentCode = payrollDeptCode,
                    EffectiveDate = createDto.EffectiveDate,
                    ProcessingNotes = createDto.ProcessingNotes,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow
                };

                hrRequest.Details.Add(detail);
            }

            _context.HRRequests.Add(hrRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully created HR request with ID {RequestId} for RequestTypeId: {RequestTypeId}, EmployeeIds: [{EmployeeIds}]",
                hrRequest.Id,
                createDto.RequestTypeId,
                string.Join(", ", createDto.EmployeeIds));

            _ecmLogger.LogSave(true, "HRRequest", hrRequest.Id, currentUserEmail);

            // Create type-specific details for Layoff requests (RequestTypeId = 2)
            if (createDto.RequestTypeId == 2) // Layoff
            {
                var layoffDetailsService = _serviceProvider.GetRequiredService<ILayoffRequestDetailsService>();
                var hrRequestDetailIds = hrRequest.Details.Select(d => d.Id).ToList();
                var layoffResult = await layoffDetailsService.CreateLayoffRequestDetailsAsync(hrRequestDetailIds);
                
                if (!layoffResult.Success)
                {
                    _logger.LogError("Failed to create layoff request details: {Message}", layoffResult.Message);
                    // Note: We don't fail the entire request here, but log the error
                    // The HRRequestDetail records are still created successfully
                }
                else
                {
                    _logger.LogInformation("Successfully created {Count} layoff request details", layoffResult.Data?.Count ?? 0);
                }
            }

            // Schedule background jobs for Viewpoint status updates after saving (skip for new-hire requests)
            if (createDto.RequestTypeId != 5) // 5 = NewHire - new-hire requests don't need Viewpoint status updates
            {
                var backgroundJobService = _serviceProvider.GetRequiredService<IBackgroundJobService>();
                var submitterEmail = _userContextService.GetUserEmail();
                foreach (var detail in hrRequest.Details)
                {
                    if (createDto.EffectiveDate.HasValue)
                    {
                        // Update status to InProgress immediately when job is scheduled
                        //detail.RequestStatusId = (int)Core.Enums.RequestStatus.InProgress;

                        var jobId = backgroundJobService.ScheduleViewpointStatusUpdateJob(detail.Id, createDto.EffectiveDate.Value, createDto.RequestTypeId, submitterEmail);

                        _logger.LogInformation("Set HR request detail {DetailId} status to InProgress and scheduled Viewpoint update job {JobId} for {EffectiveDate}",
                            detail.Id, jobId, createDto.EffectiveDate.Value);
                    }
                    else
                    {
                        _logger.LogWarning("No effective date provided for HR request detail {DetailId}, skipping Viewpoint job scheduling", detail.Id);
                    }
                }
            }
            else
            {
                // New hire request - background job scheduling will be handled by NewHireRequestDetailsService
                // after the NewHireRequestDetail records are created to ensure FirstDayEmployment data is available
                _logger.LogInformation("Skipping background job scheduling for new hire request (RequestTypeId: {RequestTypeId}) - will be handled after NewHireRequestDetails creation", createDto.RequestTypeId);
            }
            
            // Save the status updates
            await _context.SaveChangesAsync();

            // Return the created details directly without re-querying
            // This avoids role-based filter issues for new hire drafts where company/department may be null
            var requestType = await _context.RequestTypes.FirstOrDefaultAsync(rt => rt.Id == createDto.RequestTypeId);
            var requestStatus = await _context.RequestStatuses.FirstOrDefaultAsync(rs => rs.Id == (int)Core.Enums.RequestStatus.Pending);

            var createdDetailDtos = hrRequest.Details.Select(detail => new HRRequestDetailDto
            {
                Id = detail.Id,
                ParentRequestId = hrRequest.Id,
                RequestTypeId = detail.RequestTypeId,
                RequestTypeName = requestType?.RequestTypeName,
                RequestStatusId = detail.RequestStatusId,
                RequestStatusName = requestStatus?.RequestStatusName,
                RequestDisplayStatusName = requestStatus?.RequestDisplayStatusName,
                EmployeeId = detail.EmployeeId,
                EmployeeNetworkId = detail.EmployeeNetworkId,
                EmployeePositionCode = detail.EmployeePositionCode,
                EmployeeCompanyCode = detail.EmployeeCompanyCode,
                EmployeeDepartmentCode = detail.EmployeeDepartmentCode,
                EffectiveDate = detail.EffectiveDate,
                ProcessingNotes = detail.ProcessingNotes,
                SubmittedBy = hrRequest.SubmittedBy,
                SubmittedByName = hrRequest.SubmitterName,
                SubmittedDate = hrRequest.SubmittedDate,
                ViewpointProcessed = detail.ViewpointProcessed,
                ViewpointProcessedDate = detail.ViewpointProcessedDate,
                ViewpointErrorMessage = detail.ViewpointErrorMessage,
                HangfireJobId = detail.HangfireJobId
            }).ToList();

            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = true,
                Data = createdDetailDtos,
                Message = $"Successfully created {createdDetailDtos.Count} HR request detail(s)"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create multi-employee HR request. RequestTypeId: {RequestTypeId}, EmployeeIds: [{EmployeeIds}], CompanyId: {CompanyId}",
                createDto.RequestTypeId,
                string.Join(", ", createDto.EmployeeIds),
                createDto.CompanyId);

            _ecmLogger.LogSave(false, "HRRequest", null, null, ex.Message);

            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = $"Failed to create multi-employee HR request: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<HRRequestDto>> UpdateHRRequestAsync(UpdateHRRequestDto updateDto)
    {
        try
        {
            var request = await _context.HRRequests
                .FirstOrDefaultAsync(r => r.Id == updateDto.Id && !r.IsDeleted);

            if (request == null)
            {
                return new ApiResponse<HRRequestDto>
                {
                    Success = false,
                    Message = "HR request not found"
                };
            }

            var currentUserId = _userContextService.GetUserEmployeeNumber();

            request.Notes = updateDto.Notes;
            request.ModifiedBy = currentUserId;
            request.ModifiedDate = DateTime.UtcNow;

            // Backfill SubmitterName if not already set (for older records)
            if (string.IsNullOrEmpty(request.SubmitterName))
            {
                request.SubmitterName = _userContextService.GetUserDisplayName();
            }

            await _context.SaveChangesAsync();

            _ecmLogger.LogUpdate(true, "HRRequest", request.Id, _userContextService.GetUserEmail());

            var updatedRequest = await GetHRRequestByIdAsync(request.Id);
            return updatedRequest;
        }
        catch (Exception ex)
        {
            _ecmLogger.LogUpdate(false, "HRRequest", updateDto.Id, null, ex.Message);
            return new ApiResponse<HRRequestDto>
            {
                Success = false,
                Message = $"Failed to update HR request: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<HRRequestDetailDto>> UpdateHRRequestDetailAsync(int detailId, UpdateHRRequestDetailDto updateDto)
    {
        try
        {
            var detail = await _context.HRRequestDetails
                .Include(d => d.RequestType)
                .Include(d => d.RequestStatus)
                .FirstOrDefaultAsync(d => d.Id == detailId && !d.IsDeleted);

            if (detail == null)
            {
                return new ApiResponse<HRRequestDetailDto>
                {
                    Success = false,
                    Message = "HR request detail not found"
                };
            }

            var currentUserId = _userContextService.GetUserEmployeeNumber();

            // Validate cancellation rules: Only allow cancellation if request is still pending
            if (updateDto.RequestStatusId == (int)Core.Enums.RequestStatus.Cancelled)
            {
                if (detail.RequestStatusId == (int)Core.Enums.RequestStatus.Processing)
                {
                    return new ApiResponse<HRRequestDetailDto>
                    {
                        Success = false,
                        Message = "Cannot cancel HR request detail that is currently in progress. Only pending requests can be cancelled."
                    };
                }
                
                if (detail.RequestStatusId == (int)Core.Enums.RequestStatus.Completed)
                {
                    return new ApiResponse<HRRequestDetailDto>
                    {
                        Success = false,
                        Message = "Cannot cancel HR request detail that has already been completed."
                    };
                }
                
                if (detail.RequestStatusId == (int)Core.Enums.RequestStatus.Failed)
                {
                    return new ApiResponse<HRRequestDetailDto>
                    {
                        Success = false,
                        Message = "Cannot cancel HR request detail that has already failed."
                    };
                }
                
                if (detail.RequestStatusId == (int)Core.Enums.RequestStatus.Cancelled)
                {
                    return new ApiResponse<HRRequestDetailDto>
                    {
                        Success = false,
                        Message = "HR request detail is already cancelled."
                    };
                }
                
                // Handle Hangfire job cancellation for successful cancellations of pending requests
                if (detail.RequestStatusId == (int)Core.Enums.RequestStatus.Pending)
                {
                    // Cancel existing Hangfire job if it exists
                    if (!string.IsNullOrEmpty(detail.HangfireJobId))
                    {
                        BackgroundJob.Delete(detail.HangfireJobId);
                        _logger.LogInformation("Cancelled Hangfire job {JobId} for HR request detail {DetailId}", 
                            detail.HangfireJobId, detailId);
                        
                        // Clear the job ID since it's been cancelled
                        detail.HangfireJobId = null;
                    }
                }
            }
            
            detail.RequestStatusId = updateDto.RequestStatusId;
            detail.EffectiveDate = updateDto.EffectiveDate;
            detail.ProcessingNotes = updateDto.ProcessingNotes;
            detail.ViewpointProcessed = updateDto.ViewpointProcessed;
            detail.ViewpointErrorMessage = updateDto.ViewpointErrorMessage;
            detail.ModifiedBy = currentUserId;
            detail.ModifiedDate = DateTime.UtcNow;

            if (updateDto.ViewpointProcessed)
            {
                detail.ViewpointProcessedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Log cancel vs update based on status
            if (updateDto.RequestStatusId == (int)Core.Enums.RequestStatus.Cancelled)
            {
                _ecmLogger.LogCancel(true, "HRRequestDetail", detailId, _userContextService.GetUserEmail());
            }
            else
            {
                _ecmLogger.LogUpdate(true, "HRRequestDetail", detailId, _userContextService.GetUserEmail());
            }

            return new ApiResponse<HRRequestDetailDto>
            {
                Success = true,
                Data = MapDetailToDto(detail),
                Message = "HR request detail updated successfully"
            };
        }
        catch (Exception ex)
        {
            _ecmLogger.LogUpdate(false, "HRRequestDetail", detailId, null, ex.Message);
            return new ApiResponse<HRRequestDetailDto>
            {
                Success = false,
                Message = $"Failed to update HR request detail: {ex.Message}"
            };
        }
    }

    public async Task<ApiResponse<bool>> UpdateEffectiveDateByParentIdAsync(int parentId, string effectiveDate)
    {
        try
        {
            if (!DateTime.TryParse(effectiveDate, out var parsedDate))
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Invalid date format"
                };
            }

            var details = await _context.HRRequestDetails
                .Where(d => d.ParentRequestId == parentId && !d.IsDeleted)
                .ToListAsync();

            if (!details.Any())
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "No HR request details found for the specified parent request"
                };
            }

            var currentUserId = _userContextService.GetUserEmployeeNumber();
            var submitterEmail = _userContextService.GetUserEmail();

            // Capture previous effective dates before overwriting
            var previousDates = details.ToDictionary(d => d.Id, d => d.EffectiveDate);

            foreach (var detail in details)
            {
                // Step 1: Cancel the old Hangfire job if one exists
                if (!string.IsNullOrEmpty(detail.HangfireJobId))
                {
                    BackgroundJob.Delete(detail.HangfireJobId);
                    _logger.LogInformation("Cancelled old Hangfire job {JobId} for HR request detail {DetailId} due to effective date change",
                        detail.HangfireJobId, detail.Id);
                    detail.HangfireJobId = null;
                }

                detail.EffectiveDate = parsedDate;
                detail.ModifiedBy = currentUserId;
                detail.ModifiedDate = DateTime.UtcNow;

                // Also update FirstDayEmployment on NewHireRequestDetails if this is a new hire request
                if (detail.RequestTypeId == 5)
                {
                    var newHireDetail = await _context.NewHireRequestDetails
                        .Where(n => n.RequestDetailId == detail.Id && !n.IsDeleted)
                        .FirstOrDefaultAsync();
                    if (newHireDetail != null)
                    {
                        newHireDetail.FirstDayEmployment = parsedDate;
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Step 2: Create new Hangfire jobs with the updated effective date
            var backgroundJobService = _serviceProvider.GetRequiredService<IBackgroundJobService>();
            foreach (var detail in details)
            {
                if (detail.RequestTypeId == 5)
                {
                    // New hire requests use ProcessNewHirePreEmploymentAsync instead of Viewpoint status update
                    var newJobId = await backgroundJobService.ScheduleNewHirePreEmploymentProcessingJob(
                        detail.Id, parsedDate, submitterEmail);
                    _logger.LogInformation("Created new pre-employment Hangfire job {JobId} for HR request detail {DetailId} with new FirstDayEmployment {EffectiveDate}",
                        newJobId, detail.Id, parsedDate);
                }
                else
                {
                    var newJobId = await backgroundJobService.ScheduleViewpointStatusUpdateJob(
                        detail.Id, parsedDate, detail.RequestTypeId, submitterEmail);
                    _logger.LogInformation("Created new Hangfire job {JobId} for HR request detail {DetailId} with new effective date {EffectiveDate}",
                        newJobId, detail.Id, parsedDate);
                }
            }

            _ecmLogger.LogUpdate(true, "HRRequestDetail_EffectiveDate", parentId, submitterEmail);

            // Step 3: Send "Change Date" email notification for each detail
            try
            {
                var parentRequest = await _context.HRRequests
                    .Where(r => r.Id == parentId && !r.IsDeleted)
                    .FirstOrDefaultAsync();

                foreach (var detail in details)
                {
                    var previousDate = previousDates.GetValueOrDefault(detail.Id);
                    await SendChangeDateEmailAsync(detail, parentRequest, previousDate, parsedDate, submitterEmail);
                }
            }
            catch (Exception emailEx)
            {
                _ecmLogger.LogEmailNotification(false, "ChangeDateEmail", submitterEmail ?? "", parentId.ToString(), emailEx.Message);
            }

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Effective date updated successfully"
            };
        }
        catch (Exception ex)
        {
            _ecmLogger.LogUpdate(false, "HRRequestDetail_EffectiveDate", parentId, null, ex.Message);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Failed to update effective date: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Send "Change Date" email notification when effective date is updated
    /// </summary>
    private async Task SendChangeDateEmailAsync(HRRequestDetail detail, HRRequest? parentRequest, DateTime? previousEffectiveDate, DateTime newEffectiveDate, string? submitterEmail)
    {
        try
        {
            // Map RequestTypeId to template RequestType string
            var requestTypeString = detail.RequestTypeId switch
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
                _ecmLogger.LogEmailNotification(false, "ChangeDateEmail", "", null, $"Unknown RequestTypeId {detail.RequestTypeId}");
                return;
            }

            // Get supervisor/manager email
            string? managerEmail = null;
            var employee = await _context.Employees
                .Where(e => e.EmployeeNumber == detail.EmployeeId && !e.IsDeleted)
                .FirstOrDefaultAsync();

            if (employee?.SupervisorId.HasValue == true)
            {
                var supervisor = await _context.Employees
                    .Where(e => e.EmployeeNumber == employee.SupervisorId.Value && !e.IsDeleted)
                    .FirstOrDefaultAsync();
                managerEmail = supervisor?.WorkEmail;
            }

            // Resolve recipients from the "Change Date" template
            var recipients = await _emailRecipientsService.GetRecipientsFromTemplateAsync(
                "Change Date",
                detail.EmployeeCompanyCode,
                detail.EmployeeDepartmentCode ?? 0,
                managerEmail,
                submitterEmail ?? parentRequest?.SubmitterEmail,
                null,
                requestTypeString
            );

            if (recipients == null || !recipients.Any())
            {
                _ecmLogger.LogEmailNotification(false, "ChangeDateEmail", "", null, $"No recipients resolved for Change Date ({requestTypeString})");
                return;
            }

            var toEmail = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

            // Send email using the appropriate type-specific method
            switch (detail.RequestTypeId)
            {
                case 1: // Promotion
                    var promoDetail = await _context.PromotionRequestDetails
                        .Where(p => p.RequestDetailId == detail.Id && !p.IsDeleted)
                        .FirstOrDefaultAsync();

                    var promotionData = new CreatePromotionRequestDto
                    {
                        EmployeeId = detail.EmployeeId,
                        EffectiveDate = newEffectiveDate,
                        PreviousEffectiveDate = previousEffectiveDate,
                        Notes = parentRequest?.Notes ?? detail.ProcessingNotes,
                        CurrentPayrollCompanyCode = promoDetail?.CurrentPayrollCompanyCode,
                        CurrentPayrollDeptCode = promoDetail?.CurrentPayrollDeptCode,
                        CurrentPositionCode = promoDetail?.CurrentPositionCode,
                        CurrentSupervisorId = promoDetail?.CurrentSupervisorId,
                        CurrentStatus = promoDetail?.CurrentStatus,
                        NewPayrollCompanyCode = promoDetail?.NewPayrollCompanyCode ?? 0,
                        NewPayrollDeptCode = promoDetail?.NewPayrollDeptCode ?? 0,
                        NewPositionCode = promoDetail?.NewPositionCode ?? string.Empty,
                        NewSupervisorId = promoDetail?.NewSupervisorId,
                        NewStatus = promoDetail?.NewStatus ?? string.Empty
                    };
                    await _emailService.SendEmailFromTemplateNameForPromotionAsync(
                        "Change Date", promotionData, toEmail, null, detail.ParentRequestId);
                    break;

                case 2: // Layoff
                    var layoffData = new LayoffEmailDataDto
                    {
                        EmployeeId = detail.EmployeeId,
                        CompanyCode = detail.EmployeeCompanyCode,
                        DeptCode = detail.EmployeeDepartmentCode,
                        EffectiveDate = newEffectiveDate,
                        PreviousEffectiveDate = previousEffectiveDate,
                        Notes = parentRequest?.Notes ?? detail.ProcessingNotes,
                        Submitter = parentRequest?.SubmitterName,
                        ManagerEmail = managerEmail
                    };
                    await _emailService.SendEmailFromTemplateNameForLayoffAsync(
                        "Change Date", layoffData, toEmail, null, detail.ParentRequestId);
                    break;

                case 3: // Termination
                    var terminationData = new TerminationEmailDataDto
                    {
                        EmployeeId = detail.EmployeeId,
                        CompanyCode = detail.EmployeeCompanyCode,
                        DeptCode = detail.EmployeeDepartmentCode,
                        EffectiveDate = newEffectiveDate,
                        PreviousEffectiveDate = previousEffectiveDate,
                        Notes = parentRequest?.Notes ?? detail.ProcessingNotes,
                        Submitter = parentRequest?.SubmitterName,
                        ManagerEmail = managerEmail
                    };
                    await _emailService.SendEmailFromTemplateNameForTerminationAsync(
                        "Change Date", terminationData, toEmail, null, detail.ParentRequestId);
                    break;

                case 4: // ReturnToWork
                    var rtwData = new ReturnToWorkEmailDataDto
                    {
                        EmployeeId = detail.EmployeeId,
                        CompanyCode = detail.EmployeeCompanyCode,
                        DeptCode = detail.EmployeeDepartmentCode,
                        EffectiveDate = newEffectiveDate,
                        PreviousEffectiveDate = previousEffectiveDate,
                        Notes = parentRequest?.Notes ?? detail.ProcessingNotes,
                        Submitter = parentRequest?.SubmitterName,
                        ManagerEmail = managerEmail
                    };
                    await _emailService.SendEmailFromTemplateNameForReturnToWorkAsync(
                        "Change Date", rtwData, toEmail, null, detail.ParentRequestId);
                    break;

                case 5: // NewHire
                    var newHireDetail = await _context.NewHireRequestDetails
                        .Where(n => n.RequestDetailId == detail.Id && !n.IsDeleted)
                        .FirstOrDefaultAsync();

                    var newHireData = new CreateNewHireRequestDto
                    {
                        Notes = parentRequest?.Notes ?? newHireDetail?.Notes ?? detail.ProcessingNotes,
                        PersonalInfo = new NewHirePersonalInfoDto
                        {
                            EmployeeId = detail.EmployeeId,
                            FirstName = newHireDetail?.FirstName,
                            LastName = newHireDetail?.LastName,
                            FirstDayEmployment = newEffectiveDate,
                            PreviousFirstDayEmployment = previousEffectiveDate
                        },
                        PositionInfo = new NewHirePositionInfoDto
                        {
                            CompanyCode = detail.EmployeeCompanyCode,
                            PayrollDeptCode = detail.EmployeeDepartmentCode,
                            SupervisorId = newHireDetail?.SupervisorId,
                            PositionCode = newHireDetail?.PositionCode
                        }
                    };
                    await _emailService.SendEmailFromTemplateNameAsync(
                        "Change Date", newHireData, toEmail, null, detail.ParentRequestId);
                    break;
            }

            _ecmLogger.LogEmailNotification(true, "ChangeDateEmail", toEmail, $"Change Date email sent for {requestTypeString} detail {detail.Id}");
        }
        catch (Exception ex)
        {
            _ecmLogger.LogEmailNotification(false, "ChangeDateEmail", "", null, $"Error sending Change Date email for detail {detail.Id}: {ex.Message}");
        }
    }

    public async Task<ApiResponse<HRRequestDetailDto>> CancelHRRequestDetailAsync(int detailId)
    {
        try
        {
            var detail = await _context.HRRequestDetails
                .Include(d => d.RequestType)
                .Include(d => d.RequestStatus)
                .FirstOrDefaultAsync(d => d.Id == detailId && !d.IsDeleted);

            if (detail == null)
            {
                return new ApiResponse<HRRequestDetailDto>
                {
                    Success = false,
                    Message = "HR request detail not found"
                };
            }

            var currentUserId = _userContextService.GetUserEmployeeNumber();

            // Validate cancellation eligibility rules
            // Check if effective date is today or within past 7 days
            if (detail.EffectiveDate.HasValue)
            {
                var daysDifference = (DateTime.UtcNow.Date - detail.EffectiveDate.Value.Date).TotalDays;
                if (daysDifference < -1 && daysDifference > 7)
                {
                    return new ApiResponse<HRRequestDetailDto>
                    {
                        Success = false,
                        Message = "Request cannot be cancelled. Effective date must be today or within the past 7 days."
                    };
                }
            }

            // Validate request status - only allow cancellation of specific statuses
            var eligibleStatuses = new[]
            {
                (int)RequestStatusEnum.Pending,
                (int)RequestStatusEnum.Processing,
                (int)RequestStatusEnum.Failed,
                (int)RequestStatusEnum.Draft
            };

            if (!eligibleStatuses.Contains(detail.RequestStatusId))
            {
                return new ApiResponse<HRRequestDetailDto>
                {
                    Success = false,
                    Message = "Cannot cancel request with the current status. Only Pending, Processing, Failed, or Draft requests can be cancelled."
                };
            }

            // Cancel Hangfire job if it exists
            if (!string.IsNullOrEmpty(detail.HangfireJobId))
            {
                try
                {
                    BackgroundJob.Delete(detail.HangfireJobId);
                    _logger.LogInformation("Cancelled Hangfire job {JobId} for HR request detail {DetailId}",
                        detail.HangfireJobId, detailId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cancel Hangfire job {JobId} for HR request detail {DetailId}",
                        detail.HangfireJobId, detailId);
                }

                detail.HangfireJobId = null;
            }

            // Update status to Cancelled
            detail.RequestStatusId = (int)RequestStatusEnum.Cancelled;
            detail.ModifiedBy = currentUserId;
            detail.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("HR request detail {DetailId} cancelled successfully by user {UserId}", detailId, currentUserId);
            _ecmLogger.LogCancel(true, "HRRequestDetail", detailId, _userContextService.GetUserEmail());

            // Send cancellation email notification
            try
            {
                await SendCancellationEmailAsync(detail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send cancellation email for HR request detail {DetailId}", detailId);
                // Don't fail the cancel operation if email fails
            }

            return new ApiResponse<HRRequestDetailDto>
            {
                Success = true,
                Data = MapDetailToDto(detail),
                Message = "HR request cancelled successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling HR request detail {DetailId}", detailId);
            _ecmLogger.LogCancel(false, "HRRequestDetail", detailId, null, null, ex.Message);
            return new ApiResponse<HRRequestDetailDto>
            {
                Success = false,
                Message = $"Failed to cancel HR request detail: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Send cancellation email notification
    /// </summary>
    private async Task SendCancellationEmailAsync(HRRequestDetail detail)
    {
        try
        {
            const string templateName = "Cancellation Email";

            // Get the template to verify it exists and get the TemplateId
            var template = await _context.EmailTemplates
                .Where(t => t.TemplateName == templateName && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (template == null)
            {
                _logger.LogWarning("Email template '{TemplateName}' not found for cancellation notification", templateName);
                return;
            }

            // Get the parent request for submitter email
            var parentRequest = detail.ParentRequest ?? await _context.HRRequests
                .Where(r => r.Id == detail.ParentRequestId && !r.IsDeleted)
                .FirstOrDefaultAsync();

            // Try to get employee name from Employees table
            var employeeName = "N/A";
            if (detail.EmployeeId > 0)
            {
                var employee = await _context.Employees
                    .Where(e => e.Id == detail.EmployeeId && !e.IsDeleted)
                    .FirstOrDefaultAsync();
                if (employee != null)
                {
                    employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
                }
            }

            // Extract subject and body codes separately
            var subjectCodes = ExtractPlaceholders(template.Subject ?? "");
            var bodyCodes = ExtractPlaceholders(template.Body ?? "");

            // Get ContentField mappings from database for Subject and Body separately
            var subjectContentFieldMappings = await GetSubjectContentFieldMappingsAsync(
                _context,
                subjectCodes);

            var bodyContentFieldMappings = await GetBodyContentFieldMappingsAsync(
                _context,
                bodyCodes);

            // Combine all ContentFields from subject and body mappings
            var allContentFields = subjectContentFieldMappings.Values
                .Union(bodyContentFieldMappings.Values)
                .Distinct()
                .ToList();

            _logger.LogInformation("Building fieldData for {Count} content fields: {Fields}",
                allContentFields.Count, string.Join(", ", allContentFields));

            // Load NewHireRequestDetail which contains the specific new hire data
            var newHireDetail = await _context.NewHireRequestDetails
                .Where(n => n.RequestDetailId == detail.Id && !n.IsDeleted)
                .FirstOrDefaultAsync();

            if (newHireDetail == null)
            {
                _logger.LogWarning("NewHireRequestDetail not found for HRRequestDetail {DetailId}", detail.Id);
                // Continue anyway - will use what we can from HRRequestDetail
            }

            // Build field data for replacement using the NewHireRequestDetail fields
            //var fieldData = await _fieldMapperService.MapNewHireFieldsToDataAsync(newHireDetail, allContentFields);
            var fieldData = await BuildFieldDataFromNewHireDetailAsync(newHireDetail, detail, allContentFields);

            // For Text-styled templates, directly replace placeholders in subject and body
            var processedSubject = ReplaceTextPlaceholders(template.Subject ?? "Cancellation Email", fieldData, subjectContentFieldMappings);
            var processedBody = ReplaceTextPlaceholders(template.Body ?? "", fieldData, bodyContentFieldMappings);

            // Get supervisor/hiring manager email from NewHireRequestDetail
            string? supervisorEmail = null;
            if (newHireDetail?.SupervisorId.HasValue ?? false)
            {
                var supervisor = await _context.Employees
                    .Where(e => e.EmployeeNumber == newHireDetail.SupervisorId.Value && !e.IsDeleted)
                    .FirstOrDefaultAsync();
                supervisorEmail = supervisor?.WorkEmail ?? supervisor?.PersonalEmail;
                _logger.LogInformation("Resolved supervisor email for NewHire: {SupervisorEmail}", supervisorEmail ?? "N/A");
            }

            // Get recipients for the email
            // MapSpecialRecipient supports:
            // - "HIRING-MANAGER" → managerEmail (supervisor)
            // - "SUBMITTER" → submitterEmail
            var recipients = await _emailRecipientsService.GetRecipientsFromTemplateAsync(
                templateName,
                detail.EmployeeCompanyCode,
                detail.EmployeeDepartmentCode ?? 0,  // Department code for filtering DL (default to 0 if null)
                supervisorEmail,  // Maps to HIRING-MANAGER
                parentRequest?.SubmitterEmail,  // Maps to SUBMITTER
                null);

            if (recipients == null || recipients.Count == 0)
            {
                _logger.LogWarning("No recipients resolved for template '{TemplateName}'", templateName);
                return;
            }

            var toEmails = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

            // Create email notification DTO with processed content
            var emailNotification = new EmailNotificationDto
            {
                ToEmail = toEmails,
                Subject = processedSubject,
                Body = processedBody,
                RequestId = detail.ParentRequestId,
                TemplateId = template.Id,
                NotificationType = "Cancellation",
                Priority = 2, // Normal priority
                Module = "HRRequest",
                Trigger = "OnCancellation"
            };

            // Send the email notification
            await _emailService.SendEmailNotificationAsync(emailNotification);

            _logger.LogInformation("Cancellation email notification queued successfully for HR request detail {DetailId}", detail.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending cancellation email for HR request detail {DetailId}", detail.Id);
            // Don't propagate - let the caller handle the exception
            throw;
        }
    }

    /// <summary>
    /// Get ContentField mappings from EmailContentMappers table for Subject ContentPartType
    /// </summary>
    private async Task<Dictionary<string, string>> GetSubjectContentFieldMappingsAsync(
        MathyELMContext context,
        List<string> contentCodes)
    {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var code in contentCodes)
        {
            var mapper = await context.EmailContentMappers
                .Where(m => m.ContentCode == code &&
                       m.ContentPartType == "Subject" &&
                       !m.IsDeleted)
                .FirstOrDefaultAsync();

            if (mapper != null && !string.IsNullOrEmpty(mapper.ContentField))
            {
                mappings[code] = mapper.ContentField;
                _logger.LogInformation("Mapped Subject ContentCode '{Code}' to ContentField '{Field}'",
                    code, mapper.ContentField);
            }
            else
            {
                _logger.LogWarning("No ContentField mapping found for Subject ContentCode '{Code}'", code);
            }
        }

        return mappings;
    }

    /// <summary>
    /// Get ContentField mappings from EmailContentMappers table for Body ContentPartType
    /// </summary>
    private async Task<Dictionary<string, string>> GetBodyContentFieldMappingsAsync(
        MathyELMContext context,
        List<string> contentCodes)
    {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var code in contentCodes)
        {
            var mapper = await context.EmailContentMappers
                .Where(m => m.ContentCode == code &&
                       m.ContentPartType == "Body" &&
                       !m.IsDeleted)
                .FirstOrDefaultAsync();

            if (mapper != null && !string.IsNullOrEmpty(mapper.ContentField))
            {
                mappings[code] = mapper.ContentField;
                _logger.LogInformation("Mapped Body ContentCode '{Code}' to ContentField '{Field}'",
                    code, mapper.ContentField);
            }
            else
            {
                _logger.LogWarning("No ContentField mapping found for Body ContentCode '{Code}'", code);
            }
        }

        return mappings;
    }

    /// <summary>
    /// Extract placeholder codes from a string (codes starting with @@)
    /// </summary>
    private List<string> ExtractPlaceholders(string text)
    {
        var placeholders = new List<string>();
        var pattern = @"@@(\w+-?\w*)";
        var matches = System.Text.RegularExpressions.Regex.Matches(text, pattern);
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            placeholders.Add(match.Groups[1].Value);
        }
        return placeholders;
    }

    /// <summary>
    /// Replace subject placeholders with actual values
    /// </summary>
    private string ReplaceSubjectPlaceholders(string subject, Dictionary<string, string> fieldData, Dictionary<string, string> contentFieldMappings)
    {
        var result = subject;
        var pattern = @"@@(\w+-?\w*)";
        var matches = System.Text.RegularExpressions.Regex.Matches(subject, pattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var placeholder = match.Groups[0].Value;
            var contentCode = match.Groups[1].Value;

            if (contentFieldMappings.TryGetValue(contentCode, out var fieldKey) &&
                fieldData.TryGetValue(fieldKey, out var value))
            {
                result = result.Replace(placeholder, value ?? "N/A");
            }
            else
            {
                result = result.Replace(placeholder, "N/A");
            }
        }

        return result;
    }

    /// <summary>
    /// Replace text placeholders with actual values for Text-styled email templates
    /// Processes both subject and body content with @@CODE pattern replacement
    /// Maps ContentCode → ContentField → actual value from fieldData
    /// </summary>
    private string ReplaceTextPlaceholders(
        string text,
        Dictionary<string, string> fieldData,
        Dictionary<string, string> contentFieldMappings)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var replacedText = text;

        // Find all @@CODE patterns in the text (e.g., @@EMPLOYEE-NAME, @@START-DATE)
        var placeholderPattern = @"@@([A-Z\-]+)";
        var matches = System.Text.RegularExpressions.Regex.Matches(replacedText, placeholderPattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var contentCode = match.Groups[1].Value;
            var placeholder = $"@@{contentCode}";

            // Check if we have a ContentField mapping for this ContentCode
            if (!contentFieldMappings.TryGetValue(contentCode, out var contentField))
            {
                _logger.LogWarning("No ContentField mapping found for text placeholder '{Placeholder}'", placeholder);
                replacedText = replacedText.Replace(placeholder, "N/A");
                continue;
            }

            _logger.LogInformation("ContentCode '{Code}' maps to ContentField '{Field}'", contentCode, contentField);

            // Look up the value in fieldData using the mapped ContentField
            if (fieldData.TryGetValue(contentField, out var fieldValue))
            {
                var valueToReplace = fieldValue ?? "N/A";
                replacedText = replacedText.Replace(placeholder, valueToReplace);

                _logger.LogInformation("Replaced text placeholder '{Placeholder}' with value '{Value}'", placeholder, valueToReplace);
            }
            else
            {
                _logger.LogWarning("ContentField '{Field}' not found in fieldData for placeholder '{Placeholder}'",
                    contentField, placeholder);
                replacedText = replacedText.Replace(placeholder, "N/A");
            }
        }

        _logger.LogInformation("Text placeholder replacement complete for text");
        return replacedText;
    }

    /// <summary>
    /// Build a dictionary of field data from NewHireRequestDetail by mapping ContentField names to actual values
    /// Follows the same pattern as EmailFieldMapperService.MapNewHireFieldsToDataAsync
    /// </summary>
    private async Task<Dictionary<string, string>> BuildFieldDataFromNewHireDetailAsync(
        NewHireRequestDetail? newHireDetail,
        HRRequestDetail detail,
        List<string> contentFields)
    {
        var fieldData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("Building field data from NewHireRequestDetail with {Count} fields", contentFields.Count);

        foreach (var field in contentFields)
        {
            var normalizedField = field.Trim().ToLower();
            string? value = null;

            // Map using the same pattern as EmailFieldMapperService.MapNewHireFieldsToDataAsync
            switch (normalizedField)
            {
                case "start date":
                case "startdate":
                case "first day employment":
                case "firstdayemployment":
                    if (newHireDetail?.FirstDayEmployment.HasValue ?? false)
                    {
                        value = newHireDetail.FirstDayEmployment.Value.ToString("yyyy-MM-dd");
                    }
                    else if (detail.EffectiveDate.HasValue)
                    {
                        value = detail.EffectiveDate.Value.ToString("yyyy-MM-dd");
                    }
                    break;

                case "new employee":
                case "newemployee":
                case "employee name":
                case "employeename":
                case "full name":
                case "fullname":
                    if (newHireDetail != null)
                    {
                        var firstName = newHireDetail.FirstName ?? "";
                        var lastName = newHireDetail.LastName ?? "";
                        value = $"{firstName} {lastName}".Trim();
                    }
                    break;

                case "first name":
                case "firstname":
                    value = newHireDetail?.FirstName ?? "N/A";
                    break;

                case "last name":
                case "lastname":
                    value = newHireDetail?.LastName ?? "N/A";
                    break;

                case "preferred first name":
                case "preferredfirstname":
                    value = newHireDetail?.PreferredFirstName ?? "N/A";
                    break;

                case "company":
                case "companyname":
                    if (newHireDetail?.CompanyCode.HasValue ?? false)
                    {
                        var company = await _context.Companies
                            .Where(c => c.CompanyCode == newHireDetail.CompanyCode.Value && !c.IsDeleted)
                            .FirstOrDefaultAsync();
                        value = company?.CompanyName ?? $"Company {newHireDetail.CompanyCode}";
                    }
                    break;

                case "division":
                case "department":
                case "dept":
                case "payroll department":
                case "payrolldepartment":
                    if (newHireDetail?.PayrollDeptCode.HasValue ?? false)
                    {
                        var dept = await _context.PayrollDepartments
                            .Where(d => d.DeptCode == newHireDetail.PayrollDeptCode.Value && !d.IsDeleted)
                            .FirstOrDefaultAsync();
                        value = dept?.DeptName ?? $"Dept {newHireDetail.PayrollDeptCode}";
                    }
                    break;

                case "position":
                case "positioncode":
                case "position code":
                case "job title":
                case "jobtitle":
                    if (!string.IsNullOrEmpty(newHireDetail?.PositionCode))
                    {
                        var position = await _context.Positions
                            .Where(p => p.PositionCode == newHireDetail.PositionCode && !p.IsDeleted)
                            .FirstOrDefaultAsync();
                        value = position?.PositionName ?? newHireDetail.PositionCode;
                    }
                    break;

                case "supervisor":
                case "supervisorname":
                case "supervisor name":
                case "manager":
                case "hiring manager":
                case "hiringmanager":
                    if (newHireDetail?.SupervisorId.HasValue ?? false)
                    {
                        var supervisor = await _context.Employees
                            .Where(e => e.EmployeeNumber == newHireDetail.SupervisorId.Value && !e.IsDeleted)
                            .FirstOrDefaultAsync();
                        value = supervisor != null ? $"{supervisor.FirstName} {supervisor.LastName}" : $"Employee #{newHireDetail.SupervisorId}";
                    }
                    break;

                case "location":
                case "physical location":
                case "physicallocation":
                    if (newHireDetail?.LocationCode.HasValue ?? false)
                    {
                        var location = await _context.PhysicalLocations
                            .Where(l => l.LocationCode == newHireDetail.LocationCode.Value && !l.IsDeleted)
                            .FirstOrDefaultAsync();
                        value = location?.LocationName ?? $"Location {newHireDetail.LocationCode}";
                    }
                    break;

                case "employment status":
                case "employmentstatus":
                    value = newHireDetail?.EmploymentStatus ?? "N/A";
                    break;

                case "network id":
                case "networkid":
                case "username":
                    value = newHireDetail?.NetworkId ?? "N/A";
                    break;

                case "work email":
                case "workemail":
                case "email":
                    value = newHireDetail?.WorkEmail ?? "N/A";
                    break;

                // Default: try to get from HRRequestDetail
                default:
                    if (string.IsNullOrEmpty(value))
                    {
                        try
                        {
                            var property = typeof(HRRequestDetail).GetProperty(field,
                                System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public);

                            if (property != null)
                            {
                                var propertyValue = property.GetValue(detail);
                                if (propertyValue != null)
                                {
                                    if (propertyValue is DateTime dateValue)
                                    {
                                        value = dateValue.ToString("yyyy-MM-dd");
                                    }
                                    else
                                    {
                                        value = propertyValue.ToString();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error getting field '{Field}' from HRRequestDetail", field);
                        }
                    }
                    break;
            }

            // Default to "N/A" if no value found
            if (string.IsNullOrEmpty(value))
            {
                value = "N/A";
            }

            fieldData[field] = value;
            _logger.LogInformation("Mapped ContentField '{Field}' to value '{Value}'", field, value);
        }

        _logger.LogInformation("Built fieldData with {Count} fields", fieldData.Count);
        return fieldData;
    }

    public async Task<ApiResponse<bool>> DeleteHRRequestAsync(int id)
    {
        try
        {
            var request = await _context.HRRequests
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (request == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "HR request not found"
                };
            }

            var currentUserId = _userContextService.GetUserEmployeeNumber();

            // Soft delete the request and all its details
            request.IsDeleted = true;
            request.ModifiedBy = currentUserId;
            request.ModifiedDate = DateTime.UtcNow;

            foreach (var detail in request.Details)
            {
                detail.IsDeleted = true;
                detail.ModifiedBy = currentUserId;
                detail.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _ecmLogger.LogHRRequest(true, "HRRequest", "DELETE", id, _userContextService.GetUserEmail());

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "HR request deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _ecmLogger.LogHRRequest(false, "HRRequest", "DELETE", id, null, ex.Message);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Failed to delete HR request: {ex.Message}"
            };
        }
    }

    public async Task<PagedResponse<List<HRRequestDto>>> GetHRRequestsByStatusAsync(int statusId, int page = 1, int pageSize = 25)
    {
        return await GetHRRequestsAsync(page, pageSize, statusId: statusId);
    }

    public async Task<PagedResponse<List<HRRequestDto>>> GetHRRequestsBySubmitterAsync(int submittedBy, int page = 1, int pageSize = 25)
    {
        return await GetHRRequestsAsync(page, pageSize, submittedBy: submittedBy);
    }

    public async Task<PagedResponse<List<HRRequestDto>>> GetHRRequestsByTypeAsync(int requestTypeId, int page = 1, int pageSize = 25)
    {
        return await GetHRRequestsAsync(page, pageSize, requestTypeId: requestTypeId);
    }

    public async Task<ApiResponse<List<HRRequestDetailDto>>> GetHRRequestDetailsAsync(int requestId)
    {
        var details = await _context.HRRequestDetails
            .Include(d => d.RequestType)
            .Include(d => d.RequestStatus)
            .Include(d => d.ParentRequest)
            .Include(d => d.PromotionDetails)
            .Include(d => d.LayoffDetails)
            .Include(d => d.TerminationDetails)
            .Include(d => d.ReturnToWorkDetails)
            .Include(d => d.NewHireDetails)
            .Where(d => d.ParentRequestId == requestId && !d.IsDeleted)
            .ApplyRoleBasedFilter(_roleFilterService, _httpContextAccessor, _userContextService, _context.PayrollDepartmentShortNames, _logger)
            .ToListAsync();

        // Get all unique employee IDs from the details
        var employeeIds = details.Select(d => d.EmployeeId).Distinct().ToList();
        
        // Get all unique submitter IDs
        var submitterIds = details.Select(d => d.ParentRequest.SubmittedBy).Distinct().ToList();

        // Fetch employees using a simpler approach that EF Core can translate
        // First try to get employees with company codes where available
        var employeesWithCompanyCode = new List<Employee>();
        var employeeIdsWithoutCompanyCode = employeeIds.ToList();

        var detailsWithCompanyCode = details.Where(d => d.EmployeeCompanyCode.HasValue).ToList();
        if (detailsWithCompanyCode.Any())
        {
            var companyCodes = detailsWithCompanyCode.Select(d => d.EmployeeCompanyCode!.Value).Distinct().ToList();
            var employeeNumbers = detailsWithCompanyCode.Select(d => d.EmployeeId).Distinct().ToList();
            
            employeesWithCompanyCode = await _context.Employees
                .Where(e => !e.IsDeleted && companyCodes.Contains(e.CompanyCode) && employeeNumbers.Contains(e.EmployeeNumber))
                .ToListAsync();
                
            // Remove employee IDs that were found with company codes
            employeeIdsWithoutCompanyCode = employeeIds.Except(employeesWithCompanyCode.Select(e => e.EmployeeNumber)).ToList();
        }

        // Get remaining employees by employee number only
        var employeesWithoutCompanyCode = new List<Employee>();
        if (employeeIdsWithoutCompanyCode.Any())
        {
            employeesWithoutCompanyCode = await _context.Employees
                .Where(e => !e.IsDeleted && employeeIdsWithoutCompanyCode.Contains(e.EmployeeNumber))
                .ToListAsync();
        }

        // Combine both results
        var employees = employeesWithCompanyCode.Concat(employeesWithoutCompanyCode).ToList();

        // Create lookup dictionary for employee names using employee number as key
        var employeeLookup = employees.ToDictionary(
            e => e.EmployeeNumber, 
            e => $"{e.FirstName} {e.MiddleName} {e.LastName}".Replace("  ", " ").Trim()
        );

        // Create lookup dictionary for employee department codes using employee number as key
        var employeeDepartmentLookup = employees.ToDictionary(
            e => e.EmployeeNumber, 
            e => e.PayrollDeptCode
        );

        // Create lookup dictionary for submitter names (using dummy data for now)
        var submitterLookup = submitterIds.ToDictionary(
            id => id,
            id => "System User" // TODO: Implement proper user name lookup
        );

        var detailDtos = details.Select(detail => MapDetailToDtoWithNames(detail, employeeLookup, submitterLookup, employeeDepartmentLookup)).ToList();

        return new ApiResponse<List<HRRequestDetailDto>>
        {
            Success = true,
            Data = detailDtos,
            Message = $"Retrieved {detailDtos.Count} HR request details"
        };
    }

    public async Task<PagedResponse<List<HRRequestDetailDto>>> GetAllHRRequestDetailsAsync(
        int page = 1, 
        int pageSize = 25, 
        int? requestTypeId = null, 
        int? statusId = null, 
        int? employeeId = null, 
        int? submittedBy = null,
        string? searchTerm = null,
        string? sortField = null,
        string? sortDirection = null)
    {
        var query = _context.HRRequestDetails
            .Include(d => d.RequestType)
            .Include(d => d.RequestStatus)
            .Include(d => d.ParentRequest)
            .Include(d => d.PromotionDetails)
            .Include(d => d.LayoffDetails)
            .Include(d => d.TerminationDetails)
            .Include(d => d.ReturnToWorkDetails)
            .Include(d => d.NewHireDetails)
                .ThenInclude(n => n!.ITPhoneRequirement)
            .Where(d => !d.IsDeleted)
            .ApplyRoleBasedFilter(_roleFilterService, _httpContextAccessor, _userContextService, _context.PayrollDepartmentShortNames, _logger);

        // Apply filters
        if (requestTypeId.HasValue)
        {
            query = query.Where(d => d.RequestTypeId == requestTypeId.Value);
        }

        if (statusId.HasValue)
        {
            query = query.Where(d => d.RequestStatusId == statusId.Value);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(d => d.EmployeeId == employeeId.Value);
        }

        if (submittedBy.HasValue)
        {
            query = query.Where(d => d.ParentRequest.SubmittedBy == submittedBy.Value);
        }

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchTermLower = searchTerm.ToLower().Trim();
            
            // Get employee IDs that match the search term
            var matchingEmployeeIds = await _context.Employees
                .ApplyRoleBasedFilter(_roleFilterService, _httpContextAccessor, _userContextService, _context.PayrollDepartmentShortNames, _logger)
                .Where(e => e.FirstName.ToLower().Contains(searchTermLower) ||
                     e.LastName.ToLower().Contains(searchTermLower) ||
                     (e.FirstName + " " + e.LastName).ToLower().Contains(searchTermLower))
                .Select(e => e.EmployeeNumber)
                .ToListAsync();

            // Get submitter IDs that match the search term
            var matchingSubmitterIds = await _context.Employees
                .ApplyRoleBasedFilter(_roleFilterService, _httpContextAccessor, _userContextService, _context.PayrollDepartmentShortNames, _logger)
                .Where(e => e.FirstName.ToLower().Contains(searchTermLower) ||
                     e.LastName.ToLower().Contains(searchTermLower) ||
                     (e.FirstName + " " + e.LastName).ToLower().Contains(searchTermLower))
                .Select(e => e.EmployeeNumber)
                .ToListAsync();

            query = query.Where(d =>
                // Search in request type name
                d.RequestType.RequestTypeName.ToLower().Contains(searchTermLower) ||
                // Search in request status name
                d.RequestStatus.RequestStatusName.ToLower().Contains(searchTermLower) ||
                // Search by employee (from Employees table)
                matchingEmployeeIds.Contains(d.EmployeeId) ||
                // Search by new hire employee name (stored in NewHireDetails, not in Employees table)
                (d.NewHireDetails != null && (
                    (d.NewHireDetails.FirstName != null && d.NewHireDetails.FirstName.ToLower().Contains(searchTermLower)) ||
                    (d.NewHireDetails.LastName != null && d.NewHireDetails.LastName.ToLower().Contains(searchTermLower)) ||
                    (d.NewHireDetails.FirstName != null && d.NewHireDetails.LastName != null &&
                        (d.NewHireDetails.FirstName + " " + d.NewHireDetails.LastName).ToLower().Contains(searchTermLower))
                )) ||
                // Search by submitter
                matchingSubmitterIds.Contains(d.ParentRequest.SubmittedBy));
        }

        var totalCount = await query.CountAsync();
        
        // Apply sorting
        IQueryable<HRRequestDetail> orderedQuery = query;
        
        if (!string.IsNullOrEmpty(sortField))
        {
            var isDescending = !string.IsNullOrEmpty(sortDirection) && 
                              sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
            
            orderedQuery = sortField.ToLower() switch
            {
                "requesttypename" => isDescending
                    ? query.OrderByDescending(d => d.RequestType.RequestTypeName)
                    : query.OrderBy(d => d.RequestType.RequestTypeName),
                // Use GroupJoin (LEFT JOIN) and check NewHireDetails for new hires
                "employeename" => isDescending
                    ? query.GroupJoin(_context.Employees, d => d.EmployeeId, e => e.EmployeeNumber, (d, employees) => new { Detail = d, Employee = employees.FirstOrDefault() })
                           .OrderByDescending(joined => joined.Detail.NewHireDetails != null
                               ? (joined.Detail.NewHireDetails.FirstName ?? "")
                               : (joined.Employee != null ? joined.Employee.FirstName : ""))
                           .ThenByDescending(joined => joined.Detail.NewHireDetails != null
                               ? (joined.Detail.NewHireDetails.LastName ?? "")
                               : (joined.Employee != null ? joined.Employee.LastName : ""))
                           .Select(joined => joined.Detail)
                    : query.GroupJoin(_context.Employees, d => d.EmployeeId, e => e.EmployeeNumber, (d, employees) => new { Detail = d, Employee = employees.FirstOrDefault() })
                           .OrderBy(joined => joined.Detail.NewHireDetails != null
                               ? (joined.Detail.NewHireDetails.FirstName ?? "")
                               : (joined.Employee != null ? joined.Employee.FirstName : ""))
                           .ThenBy(joined => joined.Detail.NewHireDetails != null
                               ? (joined.Detail.NewHireDetails.LastName ?? "")
                               : (joined.Employee != null ? joined.Employee.LastName : ""))
                           .Select(joined => joined.Detail),
                "effectivedate" => isDescending
                    ? query.OrderByDescending(d => d.EffectiveDate)
                    : query.OrderBy(d => d.EffectiveDate),
                "requeststatusname" => isDescending
                    ? query.OrderByDescending(d => d.RequestStatus.RequestStatusName)
                    : query.OrderBy(d => d.RequestStatus.RequestStatusName),
                // Use GroupJoin (LEFT JOIN) to include records without matching submitters
                "submittedbyname" => isDescending
                    ? query.GroupJoin(_context.Employees, d => d.ParentRequest.SubmittedBy, e => e.EmployeeNumber, (d, employees) => new { Detail = d, Employee = employees.FirstOrDefault() })
                           .OrderByDescending(joined => joined.Employee != null ? joined.Employee.FirstName : "")
                           .ThenByDescending(joined => joined.Employee != null ? joined.Employee.LastName : "")
                           .Select(joined => joined.Detail)
                    : query.GroupJoin(_context.Employees, d => d.ParentRequest.SubmittedBy, e => e.EmployeeNumber, (d, employees) => new { Detail = d, Employee = employees.FirstOrDefault() })
                           .OrderBy(joined => joined.Employee != null ? joined.Employee.FirstName : "")
                           .ThenBy(joined => joined.Employee != null ? joined.Employee.LastName : "")
                           .Select(joined => joined.Detail),
                "companyname" => isDescending
                    ? query.GroupJoin(_context.Companies,
                                      d => d.EmployeeCompanyCode,
                                      c => (int?)c.CompanyCode,
                                      (d, companies) => new { Detail = d, Company = companies.FirstOrDefault() })
                             .OrderByDescending(x => x.Company != null ? x.Company.CompanyName : "")
                             .Select(x => x.Detail)
                    : query.GroupJoin(_context.Companies,
                                      d => d.EmployeeCompanyCode,
                                      c => (int?)c.CompanyCode,
                                      (d, companies) => new { Detail = d, Company = companies.FirstOrDefault() })
                             .OrderBy(x => x.Company != null ? x.Company.CompanyName : "")
                             .Select(x => x.Detail),
                "departmentname" => isDescending
                    ? query.SelectMany(
                                d => _context.PayrollDepartments
                                    .Where(pd => pd.CompanyCode == (d.EmployeeCompanyCode ?? 0) && pd.DeptCode == (d.EmployeeDepartmentCode ?? 0))
                                    .DefaultIfEmpty(),
                                (d, pd) => new { Detail = d, DeptName = pd != null ? pd.DeptName : "" })
                             .OrderByDescending(x => x.DeptName)
                             .Select(x => x.Detail)
                    : query.SelectMany(
                                d => _context.PayrollDepartments
                                    .Where(pd => pd.CompanyCode == (d.EmployeeCompanyCode ?? 0) && pd.DeptCode == (d.EmployeeDepartmentCode ?? 0))
                                    .DefaultIfEmpty(),
                                (d, pd) => new { Detail = d, DeptName = pd != null ? pd.DeptName : "" })
                             .OrderBy(x => x.DeptName)
                             .Select(x => x.Detail),
                _ => query.OrderByDescending(d => d.CreatedDate) // Default sort
            };
        }
        else
        {
            // Default sort by CreatedDate descending if no sort field specified
            orderedQuery = query.OrderByDescending(d => d.CreatedDate);
        }
        
        var details = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Get all unique employee IDs for name lookup
        var employeeIds = details.Select(d => d.EmployeeId).Distinct().ToList();
        
        // Get all unique submitter IDs
        var submitterIds = details.Select(d => d.ParentRequest.SubmittedBy).Distinct().ToList();
        
        // Fetch employee data in batch for efficiency (no role filtering for name lookup)
        var employees = await _context.Employees
            .Where(e => !e.IsDeleted && employeeIds.Contains(e.EmployeeNumber))
            .ToListAsync();

        // Fetch submitter data in batch (assuming submitters are also employees) (no role filtering for name lookup)
        var submitters = await _context.Employees
            .Where(e => !e.IsDeleted && submitterIds.Contains(e.EmployeeNumber))
            .ToListAsync();

        // Create lookup dictionaries for fast access
        var employeeLookup = employees.ToDictionary(e => e.EmployeeNumber, e => $"{e.FirstName} {e.LastName}".Trim());
        var employeeDepartmentLookup = employees.ToDictionary(e => e.EmployeeNumber, e => e.PayrollDeptCode);
        var submitterLookup = submitters.ToDictionary(e => e.EmployeeNumber, e => $"{e.FirstName} {e.LastName}".Trim());

        // Fall back to SubmitterEmail for submitters not found in Employees table
        foreach (var detail in details)
        {
            var submitterEmployeeNumber = detail.ParentRequest.SubmittedBy;
            if (!submitterLookup.ContainsKey(submitterEmployeeNumber) && !string.IsNullOrEmpty(detail.ParentRequest.SubmitterEmail))
                submitterLookup[submitterEmployeeNumber] = detail.ParentRequest.SubmitterEmail;
        }

        // Fetch company names for display
        var allCompanyCodes = employees.Select(e => e.CompanyCode)
            .Union(details.Where(d => d.EmployeeCompanyCode.HasValue).Select(d => d.EmployeeCompanyCode!.Value))
            .Distinct()
            .ToList();
        var companiesList = await _context.Companies
            .Where(c => allCompanyCodes.Contains(c.CompanyCode))
            .ToListAsync();
        var companyNameByCode = companiesList.ToDictionary(c => c.CompanyCode, c => c.CompanyName);

        // Fetch department names for display
        var allDeptCodes = employees.Where(e => e.PayrollDeptCode.HasValue).Select(e => e.PayrollDeptCode!.Value)
            .Union(details.Where(d => d.EmployeeDepartmentCode.HasValue).Select(d => d.EmployeeDepartmentCode!.Value))
            .Distinct()
            .ToList();
        var payrollDeptsList = await _context.PayrollDepartments
            .Where(d => allDeptCodes.Contains(d.DeptCode))
            .ToListAsync();
        var deptNameByCompanyAndCode = payrollDeptsList
            .GroupBy(d => (d.CompanyCode, d.DeptCode))
            .ToDictionary(g => g.Key, g => g.First().DeptName);

        // Map to DTOs with employee and submitter names
        var detailDtos = details.Select(detail => MapDetailToDtoWithNames(detail, employeeLookup, submitterLookup, employeeDepartmentLookup, companyNameByCode, deptNameByCompanyAndCode)).ToList();

        return new PagedResponse<List<HRRequestDetailDto>>
        {
            Success = true,
            Data = detailDtos,
            Message = $"Retrieved {detailDtos.Count} HR request details",
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ApiResponse<bool>> ProcessHRRequestDetailAsync(int detailId)
    {
        // This would integrate with Viewpoint to process the request
        // For now, just mark as processed
        var updateDto = new UpdateHRRequestDetailDto
        {
            Id = detailId,
            ViewpointProcessed = true,
            RequestStatusId = 3 // Assuming 3 is 'Completed'
        };

        var result = await UpdateHRRequestDetailAsync(detailId, updateDto);
        
        return new ApiResponse<bool>
        {
            Success = result.Success,
            Data = result.Success,
            Message = result.Success ? "HR request detail processed successfully" : result.Message
        };
    }

    public async Task<ApiResponse<bool>> RetryHRRequestDetailAsync(int detailId)
    {
        try
        {
            var detail = await _context.HRRequestDetails
                .Include(d => d.RequestType)
                .Include(d => d.RequestStatus)
                .FirstOrDefaultAsync(d => d.Id == detailId && !d.IsDeleted);

            if (detail == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "HR request detail not found"
                };
            }

            // Only allow retry for failed requests
            if (detail.RequestStatusId != (int)Core.Enums.RequestStatus.Failed)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Only failed HR request details can be retried"
                };
            }

            var currentUserId = _userContextService.GetUserEmployeeNumber();

            // Reset the request detail to pending status and clear error information
            detail.RequestStatusId = (int)Core.Enums.RequestStatus.Pending;
            detail.ViewpointProcessed = false;
            detail.ViewpointProcessedDate = null;
            detail.ViewpointErrorMessage = null;
            detail.HangfireJobId = null; // Clear any previous job ID
            detail.ModifiedBy = currentUserId;
            detail.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send SignalR notification for status update to Pending
            var notificationService = _serviceProvider.GetRequiredService<INotificationService>();
            await notificationService.SendHRRequestStatusUpdateAsync(
                "system", // Use system user instead of specific submitter
                detail.Id,
                "Pending",
                $"Employee {detail.EmployeeId}",
                "HR request has been reset to pending status for retry"
            );

            // Reschedule the background job if there's an effective date
            if (detail.EffectiveDate.HasValue)
            {
                var backgroundJobService = _serviceProvider.GetRequiredService<IBackgroundJobService>();
                var submitterEmail = _userContextService.GetUserEmail();
                var jobId = await backgroundJobService.ScheduleViewpointStatusUpdateJob(detail.Id, detail.EffectiveDate.Value, detail.RequestTypeId, submitterEmail);
                
                _logger.LogInformation("Retried HR request detail {DetailId} and scheduled new Viewpoint update job {JobId} for {EffectiveDate}", 
                    detail.Id, jobId, detail.EffectiveDate.Value);
            }
            else
            {
                _logger.LogWarning("No effective date available for HR request detail {DetailId}, cannot schedule retry job", detail.Id);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Cannot retry HR request detail without an effective date"
                };
            }

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "HR request detail retry initiated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying HR request detail {DetailId}", detailId);
            return new ApiResponse<bool>
            {
                Success = false,
                Message = $"Failed to retry HR request detail: {ex.Message}"
            };
        }
    }

    private async Task<List<HRRequestDto>> MapToDtoWithEmployeeNames(List<HRRequest> requests)
    {
        // Get all unique employee IDs from all request details
        var employeeIds = requests
            .SelectMany(r => r.Details)
            .Select(d => d.EmployeeId)
            .Distinct()
            .ToList();

        // Get all unique submitter IDs
        var submitterIds = requests
            .Select(r => r.SubmittedBy)
            .Distinct()
            .ToList();

        // Fetch employee data in batch for efficiency (no role filtering for name lookup)
        var employees = await _context.Employees
            .Where(e => !e.IsDeleted && employeeIds.Contains(e.EmployeeNumber))
            .ToListAsync();

        // Fetch submitter data in batch (assuming submitters are also employees) (no role filtering for name lookup)
        var submitters = await _context.Employees
            .Where(e => !e.IsDeleted && submitterIds.Contains(e.EmployeeNumber))
            .ToListAsync();

        // Create lookup dictionaries for fast access
        var employeeLookup = employees.ToDictionary(e => e.EmployeeNumber, e => $"{e.FirstName} {e.LastName}".Trim());
        var employeeDepartmentLookup = employees.ToDictionary(e => e.EmployeeNumber, e => e.PayrollDeptCode);
        var submitterLookup = submitters.ToDictionary(e => e.EmployeeNumber, e => $"{e.FirstName} {e.LastName}".Trim());

        // Fall back to SubmitterEmail for submitters not found in Employees table
        foreach (var request in requests)
        {
            if (!submitterLookup.ContainsKey(request.SubmittedBy) && !string.IsNullOrEmpty(request.SubmitterEmail))
                submitterLookup[request.SubmittedBy] = request.SubmitterEmail;
        }

        return requests.Select(request => new HRRequestDto
        {
            Id = request.Id,
            SubmittedBy = request.SubmittedBy,
            SubmittedByName = !string.IsNullOrEmpty(request.SubmitterName)
                ? request.SubmitterName
                : submitterLookup.GetValueOrDefault(request.SubmittedBy, "Unknown User"),
            SubmittedDate = request.SubmittedDate,
            Notes = request.Notes,
            CreatedBy = request.CreatedBy,
            CreatedDate = request.CreatedDate,
            ModifiedBy = request.ModifiedBy,
            ModifiedDate = request.ModifiedDate,
            IsDeleted = request.IsDeleted,
            Details = request.Details?.Select(detail => MapDetailToDtoWithEmployeeName(detail, employeeLookup, employeeDepartmentLookup)).ToList() ?? new List<HRRequestDetailDto>()
        }).ToList();
    }

    private HRRequestDto MapToDto(HRRequest request)
    {
        return new HRRequestDto
        {
            Id = request.Id,
            SubmittedBy = request.SubmittedBy,
            SubmittedByName = request.SubmitterName,
            SubmittedDate = request.SubmittedDate,
            Notes = request.Notes,
            CreatedBy = request.CreatedBy,
            CreatedDate = request.CreatedDate,
            ModifiedBy = request.ModifiedBy,
            ModifiedDate = request.ModifiedDate,
            IsDeleted = request.IsDeleted,
            Details = request.Details?.Select(MapDetailToDto).ToList() ?? new List<HRRequestDetailDto>()
        };
    }

    private HRRequestDetailDto MapDetailToDtoWithEmployeeName(HRRequestDetail detail, Dictionary<int, string> employeeLookup, Dictionary<int, int?> employeeDepartmentLookup)
    {
        return new HRRequestDetailDto
        {
            Id = detail.Id,
            ParentRequestId = detail.ParentRequestId,
            RequestTypeId = detail.RequestTypeId,
            RequestTypeName = detail.RequestType?.RequestTypeName,
            RequestStatusId = detail.RequestStatusId,
            RequestStatusName = detail.RequestStatus?.RequestStatusName,
            RequestDisplayStatusName = detail.RequestStatus?.RequestDisplayStatusName,
            EmployeeId = detail.EmployeeId,
            EmployeeName = employeeLookup.GetValueOrDefault(detail.EmployeeId, "Unknown Employee"),
            EmployeeNetworkId = detail.EmployeeNetworkId,
            EmployeePositionCode = detail.EmployeePositionCode,
            EmployeeCompanyCode = detail.EmployeeCompanyCode,
            EmployeeDepartmentCode = employeeDepartmentLookup.ContainsKey(detail.EmployeeId) ? employeeDepartmentLookup[detail.EmployeeId] : detail.EmployeeDepartmentCode,
            EffectiveDate = detail.EffectiveDate,
            ProcessingNotes = detail.ProcessingNotes,
            ViewpointProcessed = detail.ViewpointProcessed,
            ViewpointProcessedDate = detail.ViewpointProcessedDate,
            ViewpointErrorMessage = detail.ViewpointErrorMessage,
            HangfireJobId = detail.HangfireJobId
        };
    }

    private HRRequestDetailDto MapDetailToDtoWithNames(
        HRRequestDetail detail,
        Dictionary<int, string> employeeLookup,
        Dictionary<int, string> submitterLookup,
        Dictionary<int, int?> employeeDepartmentLookup,
        Dictionary<int, string>? companyNameByCode = null,
        Dictionary<(int, int), string>? deptNameByCompanyAndCode = null)
    {
        var deptCode = employeeDepartmentLookup.TryGetValue(detail.EmployeeId, out var lookupDept)
            ? lookupDept
            : detail.EmployeeDepartmentCode;

        string? companyName = null;
        if (companyNameByCode != null && detail.EmployeeCompanyCode.HasValue)
            companyNameByCode.TryGetValue(detail.EmployeeCompanyCode.Value, out companyName);

        string? deptName = null;
        if (deptNameByCompanyAndCode != null && detail.EmployeeCompanyCode.HasValue && deptCode.HasValue)
            deptNameByCompanyAndCode.TryGetValue((detail.EmployeeCompanyCode.Value, deptCode.Value), out deptName);

        return new HRRequestDetailDto
        {
            Id = detail.Id,
            ParentRequestId = detail.ParentRequestId,
            RequestTypeId = detail.RequestTypeId,
            RequestTypeName = detail.RequestType?.RequestTypeName,
            RequestStatusId = detail.RequestStatusId,
            RequestStatusName = detail.RequestStatus?.RequestStatusName,
            RequestDisplayStatusName = detail.RequestStatus?.RequestDisplayStatusName,
            EmployeeId = detail.EmployeeId,
            EmployeeName = GetEmployeeNameForRequest(detail, employeeLookup),
            EmployeeNetworkId = detail.EmployeeNetworkId,
            EmployeePositionCode = detail.EmployeePositionCode,
            EmployeeCompanyCode = detail.EmployeeCompanyCode,
            EmployeeDepartmentCode = deptCode,
            CompanyName = companyName,
            DepartmentName = deptName,
            EffectiveDate = detail.EffectiveDate,
            ProcessingNotes = detail.ProcessingNotes,
            SubmittedBy = detail.ParentRequest.SubmittedBy,
            SubmittedByName = !string.IsNullOrEmpty(detail.ParentRequest.SubmitterName)
                ? detail.ParentRequest.SubmitterName
                : submitterLookup.GetValueOrDefault(detail.ParentRequest.SubmittedBy, "Unknown User"),
            SubmittedDate = detail.ParentRequest.SubmittedDate,
            ViewpointProcessed = detail.ViewpointProcessed,
            ViewpointProcessedDate = detail.ViewpointProcessedDate,
            ViewpointErrorMessage = detail.ViewpointErrorMessage,
            HasDeskPhone = detail.NewHireDetails?.ITPhoneRequirement?.DeskPhone == true
        };
    }

    private HRRequestDetailDto MapDetailToDto(HRRequestDetail detail)
    {
        return new HRRequestDetailDto
        {
            Id = detail.Id,
            ParentRequestId = detail.ParentRequestId,
            RequestTypeId = detail.RequestTypeId,
            RequestTypeName = detail.RequestType?.RequestTypeName,
            RequestStatusId = detail.RequestStatusId,
            RequestStatusName = detail.RequestStatus?.RequestStatusName,
            RequestDisplayStatusName = detail.RequestStatus?.RequestDisplayStatusName,
            EmployeeId = detail.EmployeeId,
            EmployeeNetworkId = detail.EmployeeNetworkId,
            EmployeePositionCode = detail.EmployeePositionCode,
            EmployeeCompanyCode = detail.EmployeeCompanyCode,
            EmployeeDepartmentCode = detail.EmployeeDepartmentCode,
            EffectiveDate = detail.EffectiveDate,
            ProcessingNotes = detail.ProcessingNotes,
            ViewpointProcessed = detail.ViewpointProcessed,
            ViewpointProcessedDate = detail.ViewpointProcessedDate,
            ViewpointErrorMessage = detail.ViewpointErrorMessage,
            HangfireJobId = detail.HangfireJobId
        };
    }

    private string GetEmployeeNameForRequest(HRRequestDetail detail, Dictionary<int, string> employeeLookup)
    {
        // Check if this is a new hire request (request type name contains "NewHire")
        if (detail.RequestType?.RequestTypeName?.Contains("NewHire", StringComparison.OrdinalIgnoreCase) == true)
        {
            // For new hire requests, get the name from the NewHireRequestDetail
            if (detail.NewHireDetails != null)
            {
                var firstName = detail.NewHireDetails.FirstName?.Trim();
                var lastName = detail.NewHireDetails.LastName?.Trim();

                if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                {
                    return $"{firstName} {lastName}";
                }
                else if (!string.IsNullOrEmpty(firstName))
                {
                    return firstName;
                }
                else if (!string.IsNullOrEmpty(lastName))
                {
                    return lastName;
                }
            }
        }

        // For non-new hire requests, use the employee lookup as before
        return employeeLookup.GetValueOrDefault(detail.EmployeeId, "Unknown Employee");
    }
}
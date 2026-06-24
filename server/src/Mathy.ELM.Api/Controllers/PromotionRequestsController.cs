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
public class PromotionRequestsController : ControllerBase
{
    private readonly IHRRequestService _hrRequestService;
    private readonly IPromotionRequestDetailsService _promotionDetailsService;
    private readonly IServiceDeskIntegrationService _serviceDeskService;
    private readonly IUserContextService _userContextService;
    private readonly IAzureServiceBusEmailService _emailService;
    private readonly IEmailRecipientsService _emailRecipientsService;
    private readonly MathyELMContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PromotionRequestsController> _logger;

    public PromotionRequestsController(
        IHRRequestService hrRequestService,
        IPromotionRequestDetailsService promotionDetailsService,
        IServiceDeskIntegrationService serviceDeskService,
        IUserContextService userContextService,
        IAzureServiceBusEmailService emailService,
        IEmailRecipientsService emailRecipientsService,
        MathyELMContext context,
        IConfiguration configuration,
        ILogger<PromotionRequestsController> logger)
    {
        _hrRequestService = hrRequestService;
        _promotionDetailsService = promotionDetailsService;
        _serviceDeskService = serviceDeskService;
        _userContextService = userContextService;
        _emailService = emailService;
        _emailRecipientsService = emailRecipientsService;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Creates a promotion/transfer request
    /// </summary>
    [HttpPost("CreatePromotionRequest")]
    public async Task<ActionResult<ApiResponse<List<HRRequestDetailDto>>>> CreatePromotionRequest([FromBody] CreatePromotionRequestDto request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = "Promotion request details are required"
                });
            }

            // Trim all string fields before saving
            request.Notes = request.Notes?.Trim();
            request.CurrentPositionCode = request.CurrentPositionCode?.Trim();
            request.CurrentStatus = request.CurrentStatus?.Trim();
            request.CurrentWorkEmail = request.CurrentWorkEmail?.Trim();
            request.NewPositionCode = request.NewPositionCode?.Trim();
            request.NewStatus = request.NewStatus?.Trim();
            request.NewWorkEmail = request.NewWorkEmail?.Trim();
            if (request.CreditCardInfo != null)
            {
                request.CreditCardInfo.CreditExpenseType = request.CreditCardInfo.CreditExpenseType?.Trim();
                request.CreditCardInfo.FuelCardlockAddress = request.CreditCardInfo.FuelCardlockAddress?.Trim();
            }
            if (request.VehicleInfo != null)
            {
                request.VehicleInfo.LicenseClass = request.VehicleInfo.LicenseClass?.Trim();
                request.VehicleInfo.DrugAndAlcoholProfile = request.VehicleInfo.DrugAndAlcoholProfile?.Trim();
            }
            if (request.ITInfo != null)
            {
                request.ITInfo.AlternateDeliveryLocation = request.ITInfo.AlternateDeliveryLocation?.Trim();
            }
            if (request.PhoneInfo != null)
            {
                request.PhoneInfo.WorkPhoneNumber = request.PhoneInfo.WorkPhoneNumber?.Trim();
                request.PhoneInfo.WorkExtension = request.PhoneInfo.WorkExtension?.Trim();
                request.PhoneInfo.WorkCell = request.PhoneInfo.WorkCell?.Trim();
            }
            foreach (var app in request.Applications)
            {
                app.ApplicationName = app.ApplicationName?.Trim();
                app.AccessNotes = app.AccessNotes?.Trim();
            }
            foreach (var folder in request.Folders)
            {
                folder.FolderType = folder.FolderType?.Trim();
                folder.FolderName = folder.FolderName?.Trim();
            }

            _logger.LogInformation($"[PROMOTION] Creating promotion request for employee {request.EmployeeId}");

            // Step 1: Create HR Request (RequestTypeId = 1 for Promotion)
            _logger.LogInformation($"[PROMOTION] Step 1: Creating HR request");

            var multiEmployeeRequest = new CreateMultiEmployeeHRRequestDto
            {
                RequestTypeId = 1, // Promotion
                EmployeeIds = new List<int> { request.EmployeeId },
                RequestTitle = $"Promotion/Transfer Request - Position {request.NewPositionCode}",
                RequestDescription = $"Promotion/Transfer request for employee {request.EmployeeId} to position {request.NewPositionCode}",
                EffectiveDate = request.EffectiveDate,
                Notes = request.Notes,
                ProcessingNotes = request.Notes,
                RequestedBy = 1, // TODO: Get from user context
                CompanyId = request.NewPayrollCompanyCode,
                PayrollGroupId = request.NewPayrollGroupCode
            };

            var hrRequestResult = await _hrRequestService.CreateMultiEmployeeHRRequestAsync(multiEmployeeRequest);

            if (!hrRequestResult.Success || hrRequestResult.Data == null || !hrRequestResult.Data.Any())
            {
                _logger.LogError($"[PROMOTION] Step 1 FAILED: HR request creation failed");
                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = "Failed to create HR request",
                    Errors = hrRequestResult.Errors
                });
            }

            _logger.LogInformation($"[PROMOTION] Step 1 SUCCESS: HR request created");

            // Step 2: Create Promotion Request Details
            _logger.LogInformation($"[PROMOTION] Step 2: Creating Promotion request details");
            var hrRequestDetailId = hrRequestResult.Data.First().Id;

            var promotionResult = await _promotionDetailsService.CreatePromotionRequestDetailsAsync(
                hrRequestDetailId,
                request,
                null // currentNetworkId - not needed for promotion
            );

            if (!promotionResult.Success)
            {
                _logger.LogError($"[PROMOTION] Step 2 FAILED: Promotion details creation failed");

                // Rollback: Delete the HR Request
                try
                {
                    foreach (var hrDetail in hrRequestResult.Data)
                    {
                        if (hrDetail.ParentRequestId > 0)
                        {
                            await _hrRequestService.DeleteHRRequestAsync(hrDetail.ParentRequestId);
                        }
                    }
                    _logger.LogInformation($"[PROMOTION] Successfully rolled back HR request");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError($"[PROMOTION] ERROR: Failed to rollback HR request: {rollbackEx.Message}");
                }

                return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = "Failed to create promotion request details",
                    Errors = promotionResult.Errors
                });
            }

            _logger.LogInformation($"[PROMOTION] Step 2 SUCCESS: Promotion request details created");

            // Note: Hangfire job for Viewpoint status update is already scheduled in Step 1
            // (inside HRRequestService.CreateMultiEmployeeHRRequestAsync)

            // Step 2.6: Create ServiceDesk record (non-blocking)
            _logger.LogInformation($"[PROMOTION] Step 2.6: Creating ServiceDesk record");
            var parentRequestId = hrRequestResult.Data.First().ParentRequestId;
            try
            {
                var promotionRequestDetailId = promotionResult.Data?.Id ?? 0;
                if (promotionRequestDetailId > 0)
                {
                    await CreatePromotionServiceDeskRecordAsync(
                        promotionRequestDetailId,
                        parentRequestId,
                        request);

                    _logger.LogInformation($"[PROMOTION] Step 2.6 SUCCESS: ServiceDesk record created");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PROMOTION] Step 2.6 WARNING: ServiceDesk record creation failed: {Message}", ex.Message);
                // Non-blocking - don't fail the entire request if ServiceDesk fails
            }

            // Step 2.7: Send email notifications (non-blocking)
            _logger.LogInformation($"[PROMOTION] Step 2.7: Sending submission notifications");
            try
            {
                // Use parentRequestId for NotificationQueue foreign key constraint
                if (parentRequestId > 0)
                {
                    await SendPromotionRequestSubmissionNotificationsAsync(request, parentRequestId);
                    _logger.LogInformation($"[PROMOTION] Step 2.7 SUCCESS: Email notifications sent");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PROMOTION] Step 2.7 WARNING: Failed to send notifications: {Message}", ex.Message);
                // Non-blocking - don't fail the request if email fails
            }

            return Ok(new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = true,
                Message = "Promotion request created successfully",
                Data = hrRequestResult.Data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"[PROMOTION] Exception: {ex.Message}", ex);
            return StatusCode(500, new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = "An error occurred while creating the promotion request",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get promotion request details by parent HR request ID
    /// </summary>
    /// <param name="parentId">Parent HR request ID</param>
    /// <returns>Promotion request details including all related data</returns>
    [HttpGet("GetPromotionDetailsByParentId/{parentId}")]
    public async Task<ActionResult<ApiResponse<object>>> GetPromotionDetailsByParentId(int parentId)
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
            // Use the promotion details service to get comprehensive view data
            var result = await _promotionDetailsService.GetPromotionRequestViewByParentIdAsync(parentId);

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
            _logger.LogError($"[PROMOTION] Error retrieving promotion details for parentId {parentId}: {ex.Message}", ex);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"An error occurred while retrieving promotion details: {ex.Message}",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Creates a ServiceDesk ticket for a promotion/transfer request (non-blocking)
    /// </summary>
    private async Task CreatePromotionServiceDeskRecordAsync(
        int promotionRequestDetailId,
        int parentRequestId,
        CreatePromotionRequestDto request)
    {
        try
        {
            // Get the PromotionRequestDetail entity from the database for additional info
            var promotionDetail = await _context.PromotionRequestDetails
                .FirstOrDefaultAsync(p => p.Id == promotionRequestDetailId && !p.IsDeleted);

            if (promotionDetail == null)
            {
                _logger.LogWarning($"[PROMOTION SERVICEDESK] WARNING: PromotionRequestDetail not found");
                return;
            }

            // Check if ServiceDesk integration is enabled
            var isServiceDeskEnabled = _configuration.GetSection("ServiceDeskPlus:IsActive").Value?.ToLower() == "yes";
            if (!isServiceDeskEnabled)
            {
                _logger.LogInformation($"[PROMOTION SERVICEDESK] ServiceDesk integration is disabled in appsettings");
                return;
            }

            // Get current user context (the person submitting the request)
            var currentUserId = _userContextService.GetUserId();
            var requestorName = _userContextService.GetUserDisplayName();
            var requestorUserName = _userContextService.GetUserName();

            // Parse given_name and family_name from display name
            var nameParts = requestorName.Split(' ');
            var requestorFirstName = nameParts.FirstOrDefault() ?? "Unknown";
            var requestorLastName = string.Join(" ", nameParts.Skip(1)) ?? "User";

            // Get employee information
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeNumber == request.EmployeeId && !e.IsDeleted);

            if (employee == null)
            {
                _logger.LogWarning($"[PROMOTION SERVICEDESK] WARNING: Employee not found");
                return;
            }

            // Get company, position, location names for current and new
            var currentCompany = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyCode == promotionDetail.CurrentPayrollCompanyCode);
            var newCompany = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyCode == promotionDetail.NewPayrollCompanyCode);
            var currentDept = await _context.PayrollDepartments.FirstOrDefaultAsync(d => d.DeptCode == promotionDetail.CurrentPayrollDeptCode);
            var newDept = await _context.PayrollDepartments.FirstOrDefaultAsync(d => d.DeptCode == promotionDetail.NewPayrollDeptCode);
            var currentPayrollGroup = await _context.PayrollGroups.FirstOrDefaultAsync(g => g.GroupCode == promotionDetail.CurrentPayrollGroupCode);
            var newPayrollGroup = await _context.PayrollGroups.FirstOrDefaultAsync(g => g.GroupCode == promotionDetail.NewPayrollGroupCode);
            var currentPosition = await _context.Positions.FirstOrDefaultAsync(p => p.PositionCode == promotionDetail.CurrentPositionCode);
            var newPosition = await _context.Positions.FirstOrDefaultAsync(p => p.PositionCode == promotionDetail.NewPositionCode);
            var currentLocation = await _context.PhysicalLocations.FirstOrDefaultAsync(l => l.LocationCode == promotionDetail.CurrentPhysicalLocationCode);
            var newLocation = await _context.PhysicalLocations.FirstOrDefaultAsync(l => l.LocationCode == promotionDetail.NewPhysicalLocationCode);

            // Get supervisor name from Employees table
            Employee? supervisor = null;
            if (request.NewSupervisorId.HasValue)
            {
                supervisor = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeNumber == request.NewSupervisorId.Value && !e.IsDeleted);
            }

            // Get reference data for Computer Requirements descriptions
            var computerRequirementIds = request.ComputerRequirements?.Select(c => c.ComputerRequirementsId).ToList() ?? new List<int>();
            var computerRequirementsLookup = await _context.ComputerRequirements
                .Where(cr => computerRequirementIds.Contains(cr.Id) && !cr.IsDeleted)
                .ToDictionaryAsync(cr => cr.Id, cr => cr.Description);

            // Get reference data for Tablet Profile names
            var tabletProfileIds = request.TabletProfiles?.Select(t => t.TabletProfileId).ToList() ?? new List<int>();
            var tabletProfilesLookup = await _context.TabletProfiles
                .Where(tp => tabletProfileIds.Contains(tp.Id) && !tp.IsDeleted)
                .ToDictionaryAsync(tp => tp.Id, tp => tp.ProfileName);

            // Get reference data for Application names
            var applicationIds = request.Applications?.Select(a => a.ApplicationId).ToList() ?? new List<int>();
            var applicationsLookup = await _context.Applications
                .Where(a => applicationIds.Contains(a.Id) && !a.IsDeleted)
                .ToDictionaryAsync(a => a.Id, a => a.Name);

            _logger.LogInformation($"[PROMOTION SERVICEDESK] Creating ticket for {employee.FirstName} {employee.LastName}");

            // Build the CreatePromotionServiceDeskRecordDto with all available data
            var serviceDeskDto = new CreatePromotionServiceDeskRecordDto
            {
                // IDENTIFIERS
                PromotionRequestDetailId = promotionRequestDetailId,
                ParentHRRequestId = parentRequestId,

                // PERSONAL INFORMATION
                FirstName = employee.FirstName ?? "",
                LastName = employee.LastName ?? "",
                PreferredFirstName = employee.NetworkId,  // Employee table doesn't have PreferredName
                EffectiveDate = request.EffectiveDate,
                EffectiveDateMilliseconds = ConvertToMilliseconds(request.EffectiveDate),

                // CURRENT POSITION INFORMATION
                CurrentCompanyCode = promotionDetail.CurrentPayrollCompanyCode,
                CurrentCompanyName = currentCompany?.CompanyName,
                CurrentPayrollDeptCode = promotionDetail.CurrentPayrollDeptCode,
                CurrentPayrollDeptName = currentDept?.DeptName,
                CurrentPayrollGroupCode = promotionDetail.CurrentPayrollGroupCode,
                CurrentPayrollGroupName = currentPayrollGroup?.GroupName,
                CurrentPositionCode = promotionDetail.CurrentPositionCode,
                CurrentPositionName = currentPosition?.PositionName,
                CurrentLocationCode = promotionDetail.CurrentPhysicalLocationCode,
                CurrentLocationName = currentLocation?.LocationName,
                CurrentEmailAddress = promotionDetail.CurrentWorkEmail ?? employee.WorkEmail,
                CurrentNetworkUserName = employee.NetworkId,

                // NEW POSITION INFORMATION
                NewCompanyCode = promotionDetail.NewPayrollCompanyCode,
                NewCompanyName = newCompany?.CompanyName,
                NewPayrollDeptCode = promotionDetail.NewPayrollDeptCode,
                NewPayrollDeptName = newDept?.DeptName,
                NewPayrollGroupCode = promotionDetail.NewPayrollGroupCode,
                NewPayrollGroupName = newPayrollGroup?.GroupName,
                NewPositionCode = promotionDetail.NewPositionCode,
                NewPositionName = newPosition?.PositionName,
                NewLocationCode = promotionDetail.NewPhysicalLocationCode,
                NewLocationName = newLocation?.LocationName,
                NewEmailAddress = promotionDetail.NewWorkEmail ?? (request.ITInfo?.EmailRequired == true ? employee.WorkEmail : null),

                // SUPERVISOR INFORMATION
                NewSupervisorId = request.NewSupervisorId,
                NewSupervisorName = supervisor != null ? $"{supervisor.FirstName} {supervisor.LastName}" : null,

                // REQUESTOR INFORMATION
                RequestorName = requestorName,
                RequestorUserName = requestorUserName,
                RequestorId = int.TryParse(currentUserId, out var userId) ? userId : 0,
                RequestorFirstName = requestorFirstName,
                RequestorLastName = requestorLastName,

                // IT REQUIREMENTS
                RequiresITSupport = request.ITInfo != null,
                ITSupportNotes = request.ITInfo?.AlternateDeliveryLocation,

                // PHONE REQUIREMENTS (if applicable)
                DeskPhoneRequired = request.PhoneInfo?.DeskPhone == true ? "True" : "False",
                ReuseExistingPhone = request.PhoneInfo?.ReusingExistingPhone == true ? "True" : "False",
                CompanyCellPhoneRequired = request.PhoneInfo?.CompanyCellphone == true ? "True" : "False",
                BYODCellPhone = request.PhoneInfo?.BYODCellphone == true ? "True" : "False",

                // BUILDING ACCESS
                UseExistingKeyFob = request.UseExistingKeyFob,

                // COLLECTIONS (map request data to DTO collections)
                BuildingAccess = request.BuildingAccess?.Select(b => new PTBuildingAccessDto
                {
                    BuildingAccessRequirementId = b.AccessId,
                    AccessDescription = b.AccessDescription
                }).ToList(),

                ComputerRequirements = request.ComputerRequirements?.Select(c => new PTComputerRequirementDto
                {
                    ComputerRequirementsId = c.ComputerRequirementsId,
                    ComputerRequirementsDescription = computerRequirementsLookup.TryGetValue(c.ComputerRequirementsId, out var crDesc) ? crDesc : c.ComputerRequirementsDescription ?? ""
                }).ToList(),

                TabletProfiles = request.TabletProfiles?.Select(t => new PTTabletProfileDto
                {
                    TabletProfileId = t.TabletProfileId,
                    TabletProfileName = tabletProfilesLookup.TryGetValue(t.TabletProfileId, out var tpName) ? tpName : t.TabletProfileName ?? "",
                    RolesRequired = t.RolesRequiredForNewHire
                }).ToList(),

                Applications = request.Applications?.Select(a => new PTApplicationRequestDto
                {
                    ApplicationId = a.ApplicationId,
                    ApplicationName = applicationsLookup.TryGetValue(a.ApplicationId, out var appName) ? appName : a.ApplicationName ?? "",
                    AccessNotes = a.AccessNotes
                }).ToList(),

                SharepointAndFolderAccess = request.Folders?.Select(f => new PTFolderRequestDto
                {
                    FolderType = f.FolderType,
                    FolderName = f.FolderName
                }).ToList(),

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

            _logger.LogInformation($"[PROMOTION SERVICEDESK] Prepared ServiceDesk DTO for {employee.FirstName} {employee.LastName}");

            // Call ServiceDesk integration service
            var result = await _serviceDeskService.CreatePromotionServiceDeskRecord(serviceDeskDto);

            if (result.Success && !string.IsNullOrEmpty(result.ServiceDeskTicketId))
            {
                _logger.LogInformation($"[PROMOTION SERVICEDESK] SUCCESS: ServiceDesk ticket created with ID '{result.ServiceDeskTicketId}'");

                // Store ServiceDesk sync data in database
                try
                {
                    var syncData = new PTServiceDeskSyncData
                    {
                        PTRequestDetailId = promotionRequestDetailId,
                        ServiceDeskID = result.ServiceDeskTicketId,
                        HasPhoneRequirements = serviceDeskDto.Requirements.HasPhoneRequirements,
                        HasComputerRequirements = serviceDeskDto.Requirements.HasComputerRequirements,
                        HasTabletProfiles = serviceDeskDto.Requirements.HasTabletProfiles,
                        HasBuildingAccess = serviceDeskDto.Requirements.HasBuildingAccess,
                        HasITApplications = serviceDeskDto.Requirements.HasITApplications,
                        HasSoftwareAccessReq = serviceDeskDto.Requirements.HasSoftwareAccessReq,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = int.TryParse(currentUserId, out var createdById) ? createdById : 0
                    };

                    _context.PTServiceDeskSyncDatas.Add(syncData);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"[PROMOTION SERVICEDESK] SUCCESS: ServiceDesk sync data stored in database");
                }
                catch (Exception syncEx)
                {
                    _logger.LogWarning(syncEx, "[PROMOTION SERVICEDESK] WARNING: Failed to store ServiceDesk sync data: {Message}", syncEx.Message);
                }
            }
            else
            {
                _logger.LogWarning($"[PROMOTION SERVICEDESK] FAILED: ServiceDesk ticket creation failed - Message: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PROMOTION SERVICEDESK] ERROR: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Helper method to convert DateTime to milliseconds (Unix epoch)
    /// </summary>
    private long ConvertToMilliseconds(DateTime? dateTime)
    {
        if (!dateTime.HasValue) return 0;
        return (long)(dateTime.Value.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }

    /// <summary>
    /// Send all email notifications triggered by Promotion/Transfer submission
    /// Recipients are resolved from EmailTemplate.Recipients field using EmailRecipientsService
    /// Uses EmailTemplates with RequestType = 'PROMOTION'
    /// </summary>
    private async Task SendPromotionRequestSubmissionNotificationsAsync(CreatePromotionRequestDto request, int requestId)
    {
        Console.WriteLine($"[PROMOTION NOTIFICATIONS] Starting email notifications for request ID: {requestId}");

        try
        {
            var submitterEmail = _userContextService.GetUserEmail();
            var managerEmail = await GetManagerEmailAsync(request.NewSupervisorId);
            var companyCode = request.NewPayrollCompanyCode;
            var deptCode = request.NewPayrollDeptCode;

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
                                       request.CreditCardInfo?.CompanyExpenseCard == true ||
                                       !string.IsNullOrEmpty(request.CreditCardInfo?.CreditExpenseType);

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
                Console.WriteLine($"[PROMOTION NOTIFICATIONS] Task Email - Compliance skipped: IsApprovedToOperate is not true (Value: {request.VehicleInfo?.IsApprovedToOperate?.ToString() ?? "null"})");
            }

            // 7. Safety Task - If template exists
            await SendTemplateEmailAsync(
                "Task Email - Safety",
                request,
                requestId,
                companyCode,
                deptCode,
                managerEmail: managerEmail,
                submitterEmail: submitterEmail
            );

            Console.WriteLine($"[PROMOTION NOTIFICATIONS] All notifications processed for request ID: {requestId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROMOTION NOTIFICATIONS] CRITICAL ERROR: Failed to send notifications: {ex.Message}");
            _logger.LogError(ex, "Error sending promotion notifications for request {RequestId}", requestId);
            // Don't throw - we don't want to block request creation if email fails
        }
    }

    /// <summary>
    /// Helper method to send an email using template with resolved recipients
    /// Resolves recipients from EmailTemplate.Recipients field with support for special recipients (Manager, Submitter, Employee)
    /// Uses EmailTemplates with RequestType = 'PROMOTION'
    /// </summary>
    private async Task SendTemplateEmailAsync(
        string templateName,
        CreatePromotionRequestDto request,
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
            // Filter by RequestType='PROMOTION' to get promotion-specific templates
            var recipients = await _emailRecipientsService.GetRecipientsFromTemplateAsync(
                templateName,
                companyCode,
                deptCode,
                managerEmail,
                submitterEmail,
                employeeEmail,
                requestType: "PROMOTION");

            if (!recipients.Any())
            {
                Console.WriteLine($"[PROMOTION NOTIFICATIONS] WARNING: No recipients resolved for template '{templateName}'");
                return;
            }

            var toEmails = string.Join(", ", recipients.Where(e => !string.IsNullOrEmpty(e)));

            // Send email using promotion-specific email method
            await _emailService.SendEmailFromTemplateNameForPromotionAsync(
                templateName,
                request,
                toEmails,
                null,
                requestId);

            Console.WriteLine($"[PROMOTION NOTIFICATIONS] Email '{templateName}' queued successfully to {recipients.Count} recipient(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROMOTION NOTIFICATIONS] ERROR: Failed to queue email '{templateName}': {ex.Message}");
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

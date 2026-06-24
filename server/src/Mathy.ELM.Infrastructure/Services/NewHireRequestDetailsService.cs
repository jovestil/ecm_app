using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Data;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Security.Cryptography;

namespace Mathy.ELM.Infrastructure.Services;

public class NewHireRequestDetailsService : INewHireRequestDetailsService
{
    private readonly MathyELMContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IHRRequestService _hrRequestService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IEcmLogger _ecmLogger;

    public NewHireRequestDetailsService(
        MathyELMContext context,
        IUserContextService userContextService,
        IHRRequestService hrRequestService,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IEcmLogger ecmLogger)
    {
        _context = context;
        _userContextService = userContextService;
        _hrRequestService = hrRequestService;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _ecmLogger = ecmLogger;
    }

    public async Task<ApiResponse<NewHireRequestDetailDto>> CreateNewHireRequestDetailsAsync(
        int hrRequestDetailId,
        CreateNewHireRequestDto newHireData,
        string? networkId = null,
        string? workEmail = null,
        string? adPassword = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var currentUserId = _userContextService.GetUserEmployeeNumber();

            // Phase 1: Create main NewHireRequestDetails record
            var newHireDetail = new NewHireRequestDetail
            {
                RequestDetailId = hrRequestDetailId,
                EmployeeId = newHireData.PersonalInfo.EmployeeId,
                FirstName = newHireData.PersonalInfo.FirstName,
                LastName = newHireData.PersonalInfo.LastName,
                MiddleInitial = newHireData.PersonalInfo.MiddleInitial,
                Suffix = newHireData.PersonalInfo.Suffix,
                PreferredFirstName = newHireData.PersonalInfo.PreferredFirstName,
                FirstDayEmployment = newHireData.PersonalInfo.FirstDayEmployment,
                ReferredBy = newHireData.PersonalInfo.ReferredBy,
                Rehire = newHireData.PersonalInfo.Rehire,
                CompanyCode = newHireData.PositionInfo.CompanyCode,
                LocationCode = newHireData.PositionInfo.LocationCode,
                EmploymentStatus = newHireData.PositionInfo.EmploymentStatus,
                IsUnion = newHireData.PositionInfo.IsUnion,
                UnionCraftId = newHireData.PositionInfo.UnionCraftId,
                IsApprentice = newHireData.PositionInfo.IsApprentice,
                IsUnionWage = newHireData.PositionInfo.IsUnionWage,
                SalaryCode = newHireData.PositionInfo.SalaryCode,
                PositionCode = newHireData.PositionInfo.PositionCode,
                PayrollDeptCode = newHireData.PositionInfo.PayrollDeptCode ?? 0,
                SupervisorId = newHireData.PositionInfo.SupervisorId ?? 0,
                AppPercentage = newHireData.PositionInfo.AppPercentage ?? string.Empty,
                // Use AD-generated networkId if available (may differ from frontend due to collision),
                // otherwise fall back to UserId from frontend DTO
                NetworkId = !string.IsNullOrWhiteSpace(networkId)
                    ? networkId
                    : newHireData.PersonalInfo.UserId,
                // Use AD-generated workEmail if available (may differ from frontend due to collision),
                // otherwise fall back to EmailAddress from frontend DTO
                WorkEmail = !string.IsNullOrWhiteSpace(workEmail)
                    ? workEmail
                    : newHireData.ITInfo?.EmailAddress,
                AdPassword = adPassword,
                Notes = newHireData.Notes,
                UseExistingKeyFob = newHireData.UseExistingKeyFob,
                CreatedBy = currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            _context.NewHireRequestDetails.Add(newHireDetail);
            await _context.SaveChangesAsync(); // Get the ID

            // Phase 2: Create related 1:1 records (only if data exists)
            if (newHireData.CreditCardInfo != null)
                await CreateCreditCardDetailsAsync(newHireDetail.Id, newHireData.CreditCardInfo, currentUserId);

            if (newHireData.VehicleInfo != null)
                await CreateVehicleDetailsAsync(newHireDetail.Id, newHireData.VehicleInfo, currentUserId);

            if (newHireData.ITInfo != null)
                await CreateITDetailsAsync(newHireDetail.Id, newHireData.ITInfo, currentUserId);

            if (newHireData.PhoneInfo != null)
                await CreatePhoneRequirementsAsync(newHireDetail.Id, newHireData.PhoneInfo, currentUserId);

            // Phase 3: Create related 1:many records
            if (newHireData.Applications?.Any() == true)
                await CreateApplicationRequestsAsync(newHireDetail.Id, newHireData.Applications, currentUserId);

            if (newHireData.Folders?.Any() == true)
                await CreateFolderRequestsAsync(newHireDetail.Id, newHireData.Folders, currentUserId);

            if (newHireData.TabletProfiles?.Any() == true)
                await CreateTabletProfilesAsync(newHireDetail.Id, newHireData.TabletProfiles, currentUserId);

            if (newHireData.ComputerRequirements?.Any() == true)
                await CreateComputerRequirementsAsync(newHireDetail.Id, newHireData.ComputerRequirements, currentUserId);

            if (newHireData.BuildingAccess?.Any() == true)
                await CreateBuildingAccessAsync(newHireDetail.Id, newHireData.BuildingAccess, currentUserId);

            // Phase 4: Update parent HRRequestDetail with new hire information
            await UpdateParentHRRequestDetailAsync(hrRequestDetailId, newHireData, currentUserId);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Log successful creation
            var userName = _userContextService.GetUserName();
            _ecmLogger.LogSave(true, "NewHireRequest", newHireDetail.Id.ToString(), userName, null);

            // Schedule background job for new hire verification AFTER transaction is committed
            try
            {
                var backgroundJobService = _serviceProvider.GetRequiredService<IBackgroundJobService>();
                var submitterEmail = _userContextService.GetUserEmail();

                // Get the HR request detail to check status (using a new context since transaction is committed)
                using var jobScope = _serviceProvider.CreateScope();
                var jobContext = jobScope.ServiceProvider.GetRequiredService<MathyELMContext>();

                var hrRequestDetail = await jobContext.HRRequestDetails
                    .FirstOrDefaultAsync(hrd => hrd.Id == hrRequestDetailId && !hrd.IsDeleted);

                if (hrRequestDetail != null &&
                    newHireDetail.FirstDayEmployment.HasValue &&
                    newHireDetail.FirstDayEmployment.Value > DateTime.MinValue &&
                    hrRequestDetail.RequestStatusId == (int)Core.Enums.RequestStatus.Pending)
                {
                    //Schedule Viewpoint Job to Verify if exist in the HRRM Viewpoint Vista
                    var jobId = await backgroundJobService.ScheduleViewpointVerifyNewHireEmployee(
                        hrRequestDetailId,
                        newHireDetail.FirstDayEmployment.Value,
                        submitterEmail
                    );

                    // Store the job ID in the HR request detail using the new context
                    hrRequestDetail.HangfireJobId = jobId;
                    hrRequestDetail.ModifiedDate = DateTime.UtcNow;
                    hrRequestDetail.ModifiedBy = currentUserId;
                    await jobContext.SaveChangesAsync();

                    // Note: Using Console.WriteLine to be consistent with existing logging style in this service
                    Console.WriteLine($"Scheduled new hire verification job {jobId} for HR request detail {hrRequestDetailId} on {newHireDetail.FirstDayEmployment}");

                    // Schedule pre-employment processing job to execute on FirstDayEmployment
                    try
                    {
                        var preEmploymentJobId = await backgroundJobService.ScheduleNewHirePreEmploymentProcessingJob(
                            hrRequestDetailId,
                            newHireDetail.FirstDayEmployment.Value,
                            submitterEmail
                        );

                        Console.WriteLine($"Scheduled new hire pre-employment processing job {preEmploymentJobId} for HR request detail {hrRequestDetailId} (on FirstDayEmployment: {newHireDetail.FirstDayEmployment})");
                    }
                    catch (Exception preEmpEx)
                    {
                        Console.WriteLine($"Warning: Failed to schedule pre-employment processing job for HR request detail {hrRequestDetailId}: {preEmpEx.Message}");
                        // Don't fail the request if pre-employment job scheduling fails - the verification job is the critical one
                    }

                    // IMMEDIATELY TRIGGER any overdue scheduled emails (don't wait for daily midnight job)
                    // This checks if any email templates have trigger dates <= today and enqueues them immediately
                    // Applies to all trigger types: "Past Start Date", "Pre-Start Date", etc.
                    try
                    {
                        await backgroundJobService.TriggerOverdueScheduledEmailsAsync(hrRequestDetail.ParentRequestId);
                        Console.WriteLine($"[IMMEDIATE TRIGGER] Checked for overdue scheduled emails for parent request {hrRequestDetail.ParentRequestId}");
                    }
                    catch (Exception triggerEx)
                    {
                        Console.WriteLine($"[IMMEDIATE TRIGGER] Warning: Failed to trigger overdue emails for parent request {hrRequestDetail.ParentRequestId}: {triggerEx.Message}");
                        // Don't fail the request if immediate email triggering fails - it's a non-critical operation
                    }
                }
                else
                {
                    Console.WriteLine($"Skipped new hire verification job scheduling for HR request detail {hrRequestDetailId} - " +
                                    $"FirstDayEmployment: {newHireDetail.FirstDayEmployment}, " +
                                    $"Status: {hrRequestDetail?.RequestStatusId}");
                }
            }
            catch (Exception jobEx)
            {
                Console.WriteLine($"WARNING: Failed to schedule new hire verification job for HR request detail {hrRequestDetailId}: {jobEx.Message}");
                // Job scheduling failure doesn't affect the main operation since transaction is already committed
            }

            return new ApiResponse<NewHireRequestDetailDto>
            {
                Success = true,
                Data = await MapToDto(newHireDetail),
                Message = "New hire details created successfully"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            // Log failed creation
            var userName = _userContextService.GetUserName();
            _ecmLogger.LogSave(false, "NewHireRequest", hrRequestDetailId.ToString(), userName, ex.Message);

            return new ApiResponse<NewHireRequestDetailDto>
            {
                Success = false,
                Message = $"Failed to create new hire details: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<NewHireRequestDetailDto>> GetByHRRequestDetailIdAsync(int hrRequestDetailId)
    {
        try
        {
            var newHireDetail = await _context.NewHireRequestDetails
                .FirstOrDefaultAsync(x => x.RequestDetailId == hrRequestDetailId && !x.IsDeleted);

            if (newHireDetail == null)
            {
                return new ApiResponse<NewHireRequestDetailDto>
                {
                    Success = false,
                    Message = "New hire request details not found"
                };
            }

            return new ApiResponse<NewHireRequestDetailDto>
            {
                Success = true,
                Data = await MapToDto(newHireDetail)
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<NewHireRequestDetailDto>
            {
                Success = false,
                Message = $"Failed to retrieve new hire details: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private async Task CreateCreditCardDetailsAsync(int newHireRequestId, NewHireCreditCardInfoDto creditCardInfo, int userId)
    {
        var creditCardDetail = new CreditCardDetail
        {
            NewHireRequestId = newHireRequestId,
            KwikTripCard = creditCardInfo.KwikTripCard,
            CompanyExpenseCard = creditCardInfo.CompanyExpenseCard,
            CreditExpenseType = creditCardInfo.CreditExpenseType,
            WeeklyLimit = creditCardInfo.WeeklyLimit,
            FuelCardlockAccess = creditCardInfo.FuelCardlockAccess,
            FuelCardlockAddress = creditCardInfo.FuelCardlockAddress,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        _context.CreditCardDetails.Add(creditCardDetail);
    }

    private async Task CreateVehicleDetailsAsync(int newHireRequestId, NewHireVehicleInfoDto vehicleInfo, int userId)
    {
        var vehicleDetail = new VehicleDetail
        {
            NewHireRequestId = newHireRequestId,
            IsApprovedToOperate = vehicleInfo.IsApprovedToOperate,
            DriverClassification = vehicleInfo.DriverClassification,
            DrugAndAlcoholProfile = vehicleInfo.DrugAndAlcoholProfile,
            NeedCompanyCar = vehicleInfo.NeedCompanyCar,
            IsApplicationPart2Complete = vehicleInfo.IsApplicationPart2Complete,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        _context.VehicleDetails.Add(vehicleDetail);
    }

    private async Task CreateITDetailsAsync(int newHireRequestId, NewHireITInfoDto itInfo, int userId)
    {
        var itDetail = new ITDetail
        {
            NewHireRequestId = newHireRequestId,
            EmailRequired = itInfo.EmailRequired,
            AlternateDeliveryLocation = itInfo.AlternateDeliveryLocation,
            MSOfficeLicenseE5 = itInfo.MSOfficeLicenseE5,
            MSOfficeLicenseF3 = itInfo.MSOfficeLicenseF3,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        _context.ITDetails.Add(itDetail);
    }

    private async Task CreatePhoneRequirementsAsync(int newHireRequestId, NewHirePhoneInfoDto phoneInfo, int userId)
    {
        var phoneRequirement = new ITPhoneRequirement
        {
            NewHireRequestId = newHireRequestId,
            DeskPhone = phoneInfo.DeskPhone,
            CompanyCellphone = phoneInfo.CompanyCellphone,
            BYODCellphone = phoneInfo.BYODCellphone,
            ReusingExistingPhone = phoneInfo.ReusingExistingPhone,
            WorkPhoneNumber = phoneInfo.WorkPhoneNumber,
            WorkExtension = phoneInfo.WorkExtension,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        _context.ITPhoneRequirements.Add(phoneRequirement);
    }

    private async Task CreateApplicationRequestsAsync(int newHireRequestId, List<NewHireApplicationRequestDto> applications, int userId)
    {
        foreach (var app in applications)
        {
            var applicationRequest = new ApplicationRequest
            {
                NewHireRequestId = newHireRequestId,
                ApplicationId = app.ApplicationId,
                AccessNotes = app.AccessNotes,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.ApplicationRequests.Add(applicationRequest);
        }
    }

    private async Task CreateFolderRequestsAsync(int newHireRequestId, List<NewHireFolderRequestDto> folders, int userId)
    {
        foreach (var folder in folders)
        {
            var folderRequest = new FolderRequest
            {
                NewHireRequestId = newHireRequestId,
                FolderType = folder.FolderType,
                FolderName = folder.FolderName,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.FolderRequests.Add(folderRequest);
        }
    }

    private async Task CreateTabletProfilesAsync(int newHireRequestId, List<NewHireTabletProfileDto> tabletProfiles, int userId)
    {
        foreach (var tablet in tabletProfiles)
        {
            var tabletProfile = new ITTabletProfile
            {
                NewHireRequestId = newHireRequestId,
                TabletProfileId = tablet.TabletProfileId,
                TabletProfileName = tablet.TabletProfileName,
                RolesRequiredForNewHire = tablet.RolesRequiredForNewHire,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.ITTabletProfiles.Add(tabletProfile);
        }
    }

    private async Task CreateComputerRequirementsAsync(int newHireRequestId, List<NewHireComputerRequirementDto> computerRequirements, int userId)
    {
        foreach (var computer in computerRequirements)
        {
            // Look up the description from the master ComputerRequirement table
            var masterRequirement = await _context.ComputerRequirements
                .FirstOrDefaultAsync(c => c.Id == computer.ComputerRequirementsId && !c.IsDeleted);

            var computerRequirement = new ITComputerRequirement
            {
                NewHireRequestId = newHireRequestId,
                ComputerRequirementsId = computer.ComputerRequirementsId,
                ComputerRequirementsDescription = masterRequirement?.Description ?? computer.ComputerRequirementsDescription,
                IsChild = computer.IsChild,
                ParentId = computer.ParentId,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.ITComputerRequirements.Add(computerRequirement);
        }
    }

    private async Task CreateBuildingAccessAsync(int newHireRequestId, List<NewHireBuildingAccessDto> buildingAccess, int userId)
    {
        foreach (var access in buildingAccess)
        {
            var buildingAccessRequirement = new NewHireBuildingAccessRequirement
            {
                NewHireRequestId = newHireRequestId,
                AccessId = access.AccessId,
                AccessDescription = access.AccessDescription,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.NewHireBuildingAccessRequirements.Add(buildingAccessRequirement);
        }
    }

    private async Task<NewHireRequestDetailDto> MapToDto(NewHireRequestDetail entity)
    {
        return new NewHireRequestDetailDto
        {
            Id = entity.Id,
            RequestDetailId = entity.RequestDetailId,
            EmployeeId = entity.EmployeeId,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            MiddleInitial = entity.MiddleInitial,
            Suffix = entity.Suffix,
            PreferredFirstName = entity.PreferredFirstName,
            UserId = entity.NetworkId,
            FirstDayEmployment = entity.FirstDayEmployment,
            ReferredBy = entity.ReferredBy,
            Rehire = entity.Rehire,
            CompanyCode = entity.CompanyCode,
            LocationCode = entity.LocationCode,
            EmploymentStatus = entity.EmploymentStatus,
            IsUnion = entity.IsUnion,
            UnionCraftId = entity.UnionCraftId,
            IsApprentice = entity.IsApprentice,
            IsUnionWage = entity.IsUnionWage,
            SalaryCode = entity.SalaryCode,
            PositionCode = entity.PositionCode,
            PayrollDeptCode = entity.PayrollDeptCode,
            SupervisorId = entity.SupervisorId,
            Notes = entity.Notes,
            CreatedBy = entity.CreatedBy,
            CreatedDate = entity.CreatedDate,
            ModifiedBy = entity.ModifiedBy,
            ModifiedDate = entity.ModifiedDate,
            IsDeleted = entity.IsDeleted
        };
    }

    private async Task UpdateParentHRRequestDetailAsync(int hrRequestDetailId, CreateNewHireRequestDto newHireData, int currentUserId)
    {
        var hrRequestDetail = await _context.HRRequestDetails
            .FirstOrDefaultAsync(x => x.Id == hrRequestDetailId && !x.IsDeleted);

        if (hrRequestDetail != null)
        {
            // Generate EmployeeNetworkId from FirstName.LastName
            var firstName = newHireData.PersonalInfo.FirstName?.Trim();
            var lastName = newHireData.PersonalInfo.LastName?.Trim();
            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
            {
                hrRequestDetail.EmployeeNetworkId = $"{firstName}.{lastName}";
            }

            // Set EmployeePositionCode from PositionInfo
            if (!string.IsNullOrEmpty(newHireData.PositionInfo.PositionCode))
            {
                hrRequestDetail.EmployeePositionCode = newHireData.PositionInfo.PositionCode;
            }

            // Update company and department codes so dashboard reflects latest selection
            hrRequestDetail.EmployeeCompanyCode = newHireData.PositionInfo.CompanyCode;
            hrRequestDetail.EmployeeDepartmentCode = newHireData.PositionInfo.PayrollDeptCode;

            // Update modification tracking
            hrRequestDetail.ModifiedBy = currentUserId;
            hrRequestDetail.ModifiedDate = DateTime.UtcNow;
        }
    }

    public async Task<ApiResponse<List<HRRequestDetailDto>>> SaveNewHireRequestAsDraftAsync(CreateNewHireRequestDto request)
    {
        var logId = $"SERVICE_DRAFT_SAVE_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString()[..8]}";
        var startTime = DateTime.UtcNow;

        Console.WriteLine($"[{logId}] ====== SERVICE: SAVE DRAFT STARTED ======");
        Console.WriteLine($"[{logId}] Service timestamp: {startTime:yyyy-MM-dd HH:mm:ss.fff} UTC");

        try
        {
            Console.WriteLine($"[{logId}] Phase 1: Creating base HR Request with Draft status");
            Console.WriteLine($"[{logId}] Employee ID: {request.PersonalInfo.EmployeeId ?? 0}");
            Console.WriteLine($"[{logId}] Company Code: {request.PositionInfo.CompanyCode}");
            Console.WriteLine($"[{logId}] First Day Employment: {request.PersonalInfo.FirstDayEmployment:yyyy-MM-dd}");

            // Phase 1: Create base HR Request with Draft status (RequestTypeId = 5)
            var multiEmployeeRequest = new CreateMultiEmployeeHRRequestDto
            {
                RequestTypeId = 5, // NewHire
                EmployeeIds = new List<int> { request.PersonalInfo.EmployeeId ?? 0 },
                RequestTitle = $"New Hire Request - {request.PersonalInfo.FirstName} {request.PersonalInfo.LastName}",
                RequestDescription = $"New hire request for {request.PersonalInfo.FirstName} {request.PersonalInfo.LastName} starting {request.PersonalInfo.FirstDayEmployment:yyyy-MM-dd}",
                EffectiveDate = request.PersonalInfo.FirstDayEmployment,
                Notes = request.Notes,
                ProcessingNotes = request.Notes,
                RequestedBy = 1, // TODO: Get from user context
                CompanyId = request.PositionInfo.CompanyCode,
                PayrollGroupId = null
            };

            Console.WriteLine($"[{logId}] Created MultiEmployeeRequest DTO:");
            Console.WriteLine($"[{logId}] - RequestTypeId: {multiEmployeeRequest.RequestTypeId}");
            Console.WriteLine($"[{logId}] - RequestTitle: {multiEmployeeRequest.RequestTitle}");
            Console.WriteLine($"[{logId}] - EmployeeIds: [{string.Join(", ", multiEmployeeRequest.EmployeeIds)}]");

            // Step 1: Create HR Request
            Console.WriteLine($"[{logId}] Step 1: Calling CreateMultiEmployeeHRRequestAsync");
            var hrRequestResult = await _hrRequestService.CreateMultiEmployeeHRRequestAsync(multiEmployeeRequest);
            Console.WriteLine($"[{logId}] HR Request creation result - Success: {hrRequestResult.Success}");

            if (!hrRequestResult.Success || hrRequestResult.Data == null || !hrRequestResult.Data.Any())
            {
                Console.WriteLine($"[{logId}] ERROR: HR Request creation failed");
                Console.WriteLine($"[{logId}] - Success: {hrRequestResult.Success}");
                Console.WriteLine($"[{logId}] - Data is null: {hrRequestResult.Data == null}");
                Console.WriteLine($"[{logId}] - Data count: {hrRequestResult.Data?.Count ?? 0}");
                Console.WriteLine($"[{logId}] - Error message: {hrRequestResult.Message}");
                Console.WriteLine($"[{logId}] - Errors: [{string.Join(", ", hrRequestResult.Errors ?? new List<string>())}]");

                return new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = "Failed to create HR request.",
                    Errors = hrRequestResult.Errors
                };
            }

            // Step 2: Update the HR Request status to Draft instead of Pending
            Console.WriteLine($"[{logId}] Step 2: Updating HR Request status to Draft");
            var hrRequestDetailId = hrRequestResult.Data.First().Id;
            Console.WriteLine($"[{logId}] HR Request Detail ID: {hrRequestDetailId}");

            var hrRequestDetail = await _context.HRRequestDetails
                .Include(hrd => hrd.ParentRequest)
                .FirstOrDefaultAsync(x => x.Id == hrRequestDetailId && !x.IsDeleted);

            if (hrRequestDetail != null)
            {
                Console.WriteLine($"[{logId}] Found HR Request Detail, updating status to Draft (6)");
                Console.WriteLine($"[{logId}] Previous status: {hrRequestDetail.RequestStatusId}");
                hrRequestDetail.RequestStatusId = 6; // Draft status
                hrRequestDetail.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                hrRequestDetail.ModifiedDate = DateTime.UtcNow;
            }
            else
            {
                Console.WriteLine($"[{logId}] WARNING: HR Request Detail not found with ID {hrRequestDetailId}");
            }

            // Step 3: Create New Hire Request Details with the same validation as regular creation
            Console.WriteLine($"[{logId}] Step 3: Creating New Hire Request Details");
            var newHireResult = await CreateNewHireRequestDetailsAsync(hrRequestDetailId, request);
            Console.WriteLine($"[{logId}] New Hire creation result - Success: {newHireResult.Success}");

            if (!newHireResult.Success)
            {
                Console.WriteLine($"[{logId}] ERROR: New Hire creation failed, initiating rollback");
                Console.WriteLine($"[{logId}] New Hire error message: {newHireResult.Message}");
                Console.WriteLine($"[{logId}] New Hire errors: [{string.Join(", ", newHireResult.Errors ?? new List<string>())}]");

                // Rollback: Delete the HR Request that was just created
                try
                {
                    Console.WriteLine($"[{logId}] Starting rollback process");
                    foreach (var hrDetail in hrRequestResult.Data)
                    {
                        if (hrDetail.ParentRequestId > 0)
                        {
                            Console.WriteLine($"[{logId}] Deleting HR Request with Parent ID: {hrDetail.ParentRequestId}");
                            await _hrRequestService.DeleteHRRequestAsync(hrDetail.ParentRequestId);
                        }
                    }
                    Console.WriteLine($"[{logId}] Rollback completed successfully");
                }
                catch (Exception rollbackEx)
                {
                    Console.WriteLine($"[{logId}] CRITICAL ERROR: Rollback failed");
                    Console.WriteLine($"[{logId}] Rollback exception: {rollbackEx.Message}");
                    Console.WriteLine($"[{logId}] Rollback stack trace: {rollbackEx.StackTrace}");

                    // Log rollback failure but don't override the original error
                    return new ApiResponse<List<HRRequestDetailDto>>
                    {
                        Success = false,
                        Message = $"Failed to create New Hire details AND failed to rollback HR request. Manual cleanup required. Original error: {newHireResult.Message}. Rollback error: {rollbackEx.Message}",
                        Errors = newHireResult.Errors
                    };
                }

                return new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = $"Failed to create New Hire details. HR request has been rolled back. Error: {newHireResult.Message}",
                    Errors = newHireResult.Errors
                };
            }

            // Save the draft status change
            Console.WriteLine($"[{logId}] Step 4: Saving draft status change to database");
            await _context.SaveChangesAsync();
            Console.WriteLine($"[{logId}] Database changes saved successfully");

            // Log successful save as draft
            var userName = _userContextService.GetUserName();
            _ecmLogger.LogSaveAsDraft(true, "NewHireRequest", hrRequestDetailId.ToString(), userName, null);

            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;
            Console.WriteLine($"[{logId}] SUCCESS: Draft save completed in {duration:F2}ms");

            // Send immediate draft reminder email to submitter
            try
            {
                var parentRequest = hrRequestResult.Data?.FirstOrDefault();
                if (parentRequest?.ParentRequestId > 0)
                {
                    var backgroundJobService = _serviceProvider.GetRequiredService<IBackgroundJobService>();
                    await backgroundJobService.SendImmediateDraftReminderAsync(parentRequest.ParentRequestId);
                    Console.WriteLine($"[{logId}] Immediate draft reminder email triggered for ParentRequestId={parentRequest.ParentRequestId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{logId}] Warning: Failed to send immediate draft reminder: {ex.Message}");
                // Don't fail the save operation if email fails
            }

            // Success response
            var response = new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = true,
                Data = hrRequestResult.Data,
                Message = $"Successfully saved new hire request as draft for {request.PersonalInfo.FirstName} {request.PersonalInfo.LastName}"
            };

            Console.WriteLine($"[{logId}] Returning success response with {response.Data?.Count ?? 0} items");
            return response;
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;
            Console.WriteLine($"[{logId}] EXCEPTION after {duration:F2}ms in service layer");
            Console.WriteLine($"[{logId}] Exception type: {ex.GetType().Name}");
            Console.WriteLine($"[{logId}] Exception message: {ex.Message}");
            Console.WriteLine($"[{logId}] Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[{logId}] Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"[{logId}] Inner exception type: {ex.InnerException.GetType().Name}");
            }

            // Log failed save as draft
            var userName = _userContextService.GetUserName();
            _ecmLogger.LogSaveAsDraft(false, "NewHireRequest", "0", userName, ex.Message);

            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = $"An error occurred while saving new hire request as draft: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<List<HRRequestDetailDto>>> UpdateNewHireRequestAsDraftAsync(int parentRequestId, CreateNewHireRequestDto request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var currentUserId = _userContextService.GetUserEmployeeNumber();

            // Find the existing HR request detail
            var existingHRRequestDetail = await _context.HRRequestDetails
                .FirstOrDefaultAsync(x => x.ParentRequestId == parentRequestId && !x.IsDeleted);

            if (existingHRRequestDetail == null)
            {
                return new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = "HR request not found"
                };
            }

            // Update the HR request detail status to Draft (keep as draft)
            existingHRRequestDetail.RequestStatusId = 6; // Draft status
            // Sync EffectiveDate with FirstDayEmployment to maintain data consistency
            existingHRRequestDetail.EffectiveDate = request.PersonalInfo.FirstDayEmployment;
            existingHRRequestDetail.ModifiedBy = currentUserId;
            existingHRRequestDetail.ModifiedDate = DateTime.UtcNow;

            // Find and update the existing new hire detail
            var existingNewHireDetail = await _context.NewHireRequestDetails
                .FirstOrDefaultAsync(x => x.RequestDetailId == existingHRRequestDetail.Id && !x.IsDeleted);

            if (existingNewHireDetail == null)
            {
                return new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = "New hire request details not found"
                };
            }

            // Update main NewHireRequestDetails record
            existingNewHireDetail.EmployeeId = request.PersonalInfo.EmployeeId;
            existingNewHireDetail.FirstName = request.PersonalInfo.FirstName;
            existingNewHireDetail.LastName = request.PersonalInfo.LastName;
            existingNewHireDetail.MiddleInitial = request.PersonalInfo.MiddleInitial;
            existingNewHireDetail.Suffix = request.PersonalInfo.Suffix;
            existingNewHireDetail.PreferredFirstName = request.PersonalInfo.PreferredFirstName;
            // Map UserId from DTO to NetworkId in entity
            if (!string.IsNullOrWhiteSpace(request.PersonalInfo.UserId))
            {
                existingNewHireDetail.NetworkId = request.PersonalInfo.UserId;
            }
            // Map EmailAddress from ITInfo DTO to WorkEmail in entity
            if (!string.IsNullOrWhiteSpace(request.ITInfo?.EmailAddress))
            {
                existingNewHireDetail.WorkEmail = request.ITInfo.EmailAddress;
            }
            existingNewHireDetail.FirstDayEmployment = request.PersonalInfo.FirstDayEmployment;
            existingNewHireDetail.ReferredBy = request.PersonalInfo.ReferredBy;
            existingNewHireDetail.Rehire = request.PersonalInfo.Rehire;
            existingNewHireDetail.CompanyCode = request.PositionInfo.CompanyCode;
            existingNewHireDetail.LocationCode = request.PositionInfo.LocationCode;
            existingNewHireDetail.EmploymentStatus = request.PositionInfo.EmploymentStatus;
            existingNewHireDetail.IsUnion = request.PositionInfo.IsUnion;
            existingNewHireDetail.UnionCraftId = request.PositionInfo.UnionCraftId;
            existingNewHireDetail.IsApprentice = request.PositionInfo.IsApprentice;
            existingNewHireDetail.IsUnionWage = request.PositionInfo.IsUnionWage;
            existingNewHireDetail.SalaryCode = request.PositionInfo.SalaryCode;
            existingNewHireDetail.PositionCode = request.PositionInfo.PositionCode;
            existingNewHireDetail.PayrollDeptCode = request.PositionInfo.PayrollDeptCode ?? 0;
            existingNewHireDetail.SupervisorId = request.PositionInfo.SupervisorId ?? 0;
            existingNewHireDetail.AppPercentage = request.PositionInfo.AppPercentage ?? string.Empty;
            existingNewHireDetail.Notes = request.Notes;
            existingNewHireDetail.UseExistingKeyFob = request.UseExistingKeyFob;
            existingNewHireDetail.ModifiedBy = currentUserId;
            existingNewHireDetail.ModifiedDate = DateTime.UtcNow;

            // Update related 1:1 records using update-in-place approach
            if (request.CreditCardInfo != null)
                await UpdateOrCreateCreditCardDetailsAsync(existingNewHireDetail.Id, request.CreditCardInfo, currentUserId);

            if (request.VehicleInfo != null)
                await UpdateOrCreateVehicleDetailsAsync(existingNewHireDetail.Id, request.VehicleInfo, currentUserId);

            if (request.ITInfo != null)
                await UpdateOrCreateITDetailsAsync(existingNewHireDetail.Id, request.ITInfo, currentUserId);

            if (request.PhoneInfo != null)
                await UpdateOrCreatePhoneRequirementsAsync(existingNewHireDetail.Id, request.PhoneInfo, currentUserId);

            // Update related 1:many records using sync approach
            if (request.Applications?.Any() == true)
                await UpdateApplicationRequestsAsync(existingNewHireDetail.Id, request.Applications, currentUserId);

            if (request.Folders?.Any() == true)
                await UpdateFolderRequestsAsync(existingNewHireDetail.Id, request.Folders, currentUserId);

            if (request.TabletProfiles != null)
                await UpdateTabletProfilesAsync(existingNewHireDetail.Id, request.TabletProfiles, currentUserId);

            if (request.ComputerRequirements?.Any() == true)
                await UpdateComputerRequirementsAsync(existingNewHireDetail.Id, request.ComputerRequirements, currentUserId);

            await UpdateBuildingAccessAsync(existingNewHireDetail.Id, request.BuildingAccess ?? new(), currentUserId);

            // Update parent HRRequestDetail with new hire information
            await UpdateParentHRRequestDetailAsync(existingHRRequestDetail.Id, request, currentUserId);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Log successful update as draft
            var userName = _userContextService.GetUserName();
            _ecmLogger.LogSaveAsDraft(true, "NewHireRequest", existingNewHireDetail.Id.ToString(), userName, null);

            // Send immediate draft reminder email to submitter on update
            try
            {
                var backgroundJobService = _serviceProvider.GetRequiredService<IBackgroundJobService>();
                await backgroundJobService.SendImmediateDraftReminderAsync(parentRequestId);
                Console.WriteLine($"Immediate draft reminder email triggered for ParentRequestId={parentRequestId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to send immediate draft reminder: {ex.Message}");
                // Don't fail the update operation if email fails
            }

            // Return the updated HR request detail
            var updatedHRRequestDetailDto = new HRRequestDetailDto
            {
                Id = existingHRRequestDetail.Id,
                ParentRequestId = existingHRRequestDetail.ParentRequestId,
                RequestTypeId = existingHRRequestDetail.RequestTypeId,
                RequestStatusId = existingHRRequestDetail.RequestStatusId,
                EmployeeId = existingHRRequestDetail.EmployeeId,
                EmployeeNetworkId = existingHRRequestDetail.EmployeeNetworkId,
                EmployeePositionCode = existingHRRequestDetail.EmployeePositionCode,
                EmployeeCompanyCode = existingHRRequestDetail.EmployeeCompanyCode,
                EmployeeDepartmentCode = existingHRRequestDetail.EmployeeDepartmentCode,
                EffectiveDate = existingHRRequestDetail.EffectiveDate,
                ProcessingNotes = existingHRRequestDetail.ProcessingNotes,
                SubmittedBy = existingHRRequestDetail.ParentRequest?.SubmittedBy ?? 0,
                SubmittedByName = existingHRRequestDetail.ParentRequest?.SubmitterName,
                SubmittedDate = existingHRRequestDetail.ParentRequest?.SubmittedDate,
                ViewpointProcessed = existingHRRequestDetail.ViewpointProcessed,
                ViewpointProcessedDate = existingHRRequestDetail.ViewpointProcessedDate,
                ViewpointErrorMessage = existingHRRequestDetail.ViewpointErrorMessage,
                HangfireJobId = existingHRRequestDetail.HangfireJobId
            };

            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = true,
                Data = new List<HRRequestDetailDto> { updatedHRRequestDetailDto },
                Message = $"Successfully updated new hire request as draft for {request.PersonalInfo.FirstName} {request.PersonalInfo.LastName}"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            // Log failed update as draft
            var userName = _userContextService.GetUserName();
            _ecmLogger.LogSaveAsDraft(false, "NewHireRequest", parentRequestId.ToString(), userName, ex.Message);

            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = $"Failed to update new hire request as draft: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<List<HRRequestDetailDto>>> UpdateNewHireRequestAsync(int parentRequestId, CreateNewHireRequestDto request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var currentUserId = _userContextService.GetUserEmployeeNumber();

            // Find the existing HR request detail
            var existingHRRequestDetail = await _context.HRRequestDetails
                .FirstOrDefaultAsync(x => x.ParentRequestId == parentRequestId && !x.IsDeleted);

            if (existingHRRequestDetail == null)
            {
                return new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = "HR request not found"
                };
            }

            // Update the HR request detail status to Pending (submit the draft)
            existingHRRequestDetail.RequestStatusId = 1; // Pending status
            // Sync EffectiveDate with FirstDayEmployment to maintain data consistency
            existingHRRequestDetail.EffectiveDate = request.PersonalInfo.FirstDayEmployment;
            existingHRRequestDetail.ModifiedBy = currentUserId;
            existingHRRequestDetail.ModifiedDate = DateTime.UtcNow;

            // Find and update the existing new hire detail
            var existingNewHireDetail = await _context.NewHireRequestDetails
                .FirstOrDefaultAsync(x => x.RequestDetailId == existingHRRequestDetail.Id && !x.IsDeleted);

            if (existingNewHireDetail == null)
            {
                return new ApiResponse<List<HRRequestDetailDto>>
                {
                    Success = false,
                    Message = "New hire request details not found"
                };
            }

            // Update main NewHireRequestDetails record
            existingNewHireDetail.EmployeeId = request.PersonalInfo.EmployeeId;
            existingNewHireDetail.FirstName = request.PersonalInfo.FirstName;
            existingNewHireDetail.LastName = request.PersonalInfo.LastName;
            existingNewHireDetail.MiddleInitial = request.PersonalInfo.MiddleInitial;
            existingNewHireDetail.Suffix = request.PersonalInfo.Suffix;
            existingNewHireDetail.PreferredFirstName = request.PersonalInfo.PreferredFirstName;
            // Map UserId from DTO to NetworkId in entity
            if (!string.IsNullOrWhiteSpace(request.PersonalInfo.UserId))
            {
                existingNewHireDetail.NetworkId = request.PersonalInfo.UserId;
            }
            // Map EmailAddress from ITInfo DTO to WorkEmail in entity
            if (!string.IsNullOrWhiteSpace(request.ITInfo?.EmailAddress))
            {
                existingNewHireDetail.WorkEmail = request.ITInfo.EmailAddress;
            }
            existingNewHireDetail.FirstDayEmployment = request.PersonalInfo.FirstDayEmployment;
            existingNewHireDetail.ReferredBy = request.PersonalInfo.ReferredBy;
            existingNewHireDetail.Rehire = request.PersonalInfo.Rehire;
            existingNewHireDetail.CompanyCode = request.PositionInfo.CompanyCode;
            existingNewHireDetail.LocationCode = request.PositionInfo.LocationCode;
            existingNewHireDetail.EmploymentStatus = request.PositionInfo.EmploymentStatus;
            existingNewHireDetail.IsUnion = request.PositionInfo.IsUnion;
            existingNewHireDetail.UnionCraftId = request.PositionInfo.UnionCraftId;
            existingNewHireDetail.IsApprentice = request.PositionInfo.IsApprentice;
            existingNewHireDetail.IsUnionWage = request.PositionInfo.IsUnionWage;
            existingNewHireDetail.SalaryCode = request.PositionInfo.SalaryCode;
            existingNewHireDetail.PositionCode = request.PositionInfo.PositionCode;
            existingNewHireDetail.PayrollDeptCode = request.PositionInfo.PayrollDeptCode ?? 0;
            existingNewHireDetail.SupervisorId = request.PositionInfo.SupervisorId ?? 0;
            existingNewHireDetail.AppPercentage = request.PositionInfo.AppPercentage ?? string.Empty;
            existingNewHireDetail.Notes = request.Notes;
            existingNewHireDetail.UseExistingKeyFob = request.UseExistingKeyFob;
            existingNewHireDetail.ModifiedBy = currentUserId;
            existingNewHireDetail.ModifiedDate = DateTime.UtcNow;

            // Update related 1:1 records using update-in-place approach
            if (request.CreditCardInfo != null)
                await UpdateOrCreateCreditCardDetailsAsync(existingNewHireDetail.Id, request.CreditCardInfo, currentUserId);

            if (request.VehicleInfo != null)
                await UpdateOrCreateVehicleDetailsAsync(existingNewHireDetail.Id, request.VehicleInfo, currentUserId);

            if (request.ITInfo != null)
                await UpdateOrCreateITDetailsAsync(existingNewHireDetail.Id, request.ITInfo, currentUserId);

            if (request.PhoneInfo != null)
                await UpdateOrCreatePhoneRequirementsAsync(existingNewHireDetail.Id, request.PhoneInfo, currentUserId);

            // Update related 1:many records using sync approach
            if (request.Applications?.Any() == true)
                await UpdateApplicationRequestsAsync(existingNewHireDetail.Id, request.Applications, currentUserId);

            if (request.Folders?.Any() == true)
                await UpdateFolderRequestsAsync(existingNewHireDetail.Id, request.Folders, currentUserId);

            if (request.TabletProfiles != null)
                await UpdateTabletProfilesAsync(existingNewHireDetail.Id, request.TabletProfiles, currentUserId);

            if (request.ComputerRequirements?.Any() == true)
                await UpdateComputerRequirementsAsync(existingNewHireDetail.Id, request.ComputerRequirements, currentUserId);

            await UpdateBuildingAccessAsync(existingNewHireDetail.Id, request.BuildingAccess ?? new(), currentUserId);

            // Update parent HRRequestDetail with new hire information
            await UpdateParentHRRequestDetailAsync(existingHRRequestDetail.Id, request, currentUserId);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Log successful update
            var userName = _userContextService.GetUserName();
            _ecmLogger.LogUpdate(true, "NewHireRequest", existingNewHireDetail.Id.ToString(), userName, null);

            // Return the updated HR request detail
            var updatedHRRequestDetailDto = new HRRequestDetailDto
            {
                Id = existingHRRequestDetail.Id,
                ParentRequestId = existingHRRequestDetail.ParentRequestId,
                RequestTypeId = existingHRRequestDetail.RequestTypeId,
                RequestStatusId = existingHRRequestDetail.RequestStatusId,
                EmployeeId = existingHRRequestDetail.EmployeeId,
                EmployeeNetworkId = existingHRRequestDetail.EmployeeNetworkId,
                EmployeePositionCode = existingHRRequestDetail.EmployeePositionCode,
                EmployeeCompanyCode = existingHRRequestDetail.EmployeeCompanyCode,
                EmployeeDepartmentCode = existingHRRequestDetail.EmployeeDepartmentCode,
                EffectiveDate = existingHRRequestDetail.EffectiveDate,
                ProcessingNotes = existingHRRequestDetail.ProcessingNotes,
                SubmittedBy = existingHRRequestDetail.ParentRequest?.SubmittedBy ?? 0,
                SubmittedByName = existingHRRequestDetail.ParentRequest?.SubmitterName,
                SubmittedDate = existingHRRequestDetail.ParentRequest?.SubmittedDate,
                ViewpointProcessed = existingHRRequestDetail.ViewpointProcessed,
                ViewpointProcessedDate = existingHRRequestDetail.ViewpointProcessedDate,
                ViewpointErrorMessage = existingHRRequestDetail.ViewpointErrorMessage,
                HangfireJobId = existingHRRequestDetail.HangfireJobId
            };

            // Schedule background job for new hire verification AFTER transaction is committed
            try
            {
                var backgroundJobService = _serviceProvider.GetRequiredService<IBackgroundJobService>();
                var submitterEmail = _userContextService.GetUserEmail();

                // Get the HR request detail to check status (using a new context since transaction is committed)
                using var jobScope = _serviceProvider.CreateScope();
                var jobContext = jobScope.ServiceProvider.GetRequiredService<MathyELMContext>();

                var hrRequestDetail = await jobContext.HRRequestDetails
                    .FirstOrDefaultAsync(hrd => hrd.Id == existingHRRequestDetail.Id && !hrd.IsDeleted);

                if (hrRequestDetail != null &&
                    request.PersonalInfo.FirstDayEmployment.HasValue &&
                    request.PersonalInfo.FirstDayEmployment.Value > DateTime.MinValue &&
                    hrRequestDetail.RequestStatusId == (int)Core.Enums.RequestStatus.Pending)
                {
                    //Schedule Viewpoint Job to Verify if exist in the HRRM Viewpoint Vista
                    var jobId = await backgroundJobService.ScheduleViewpointVerifyNewHireEmployee(
                        existingHRRequestDetail.Id,
                        request.PersonalInfo.FirstDayEmployment.Value,
                        submitterEmail
                    );

                    // Store the job ID in the HR request detail using the new context
                    hrRequestDetail.HangfireJobId = jobId;
                    hrRequestDetail.ModifiedDate = DateTime.UtcNow;
                    hrRequestDetail.ModifiedBy = currentUserId;
                    await jobContext.SaveChangesAsync();

                    Console.WriteLine($"Scheduled new hire verification job {jobId} for HR request detail {existingHRRequestDetail.Id} on {request.PersonalInfo.FirstDayEmployment}");

                    // IMMEDIATELY TRIGGER any overdue scheduled emails (don't wait for daily midnight job)
                    // This checks if any email templates have trigger dates <= today and enqueues them immediately
                    // Applies to all trigger types: "Past Start Date", "Pre-Start Date", etc.
                    try
                    {
                        await backgroundJobService.TriggerOverdueScheduledEmailsAsync(hrRequestDetail.ParentRequestId);
                        Console.WriteLine($"[IMMEDIATE TRIGGER] Checked for overdue scheduled emails for parent request {hrRequestDetail.ParentRequestId}");
                    }
                    catch (Exception triggerEx)
                    {
                        Console.WriteLine($"[IMMEDIATE TRIGGER] Warning: Failed to trigger overdue emails for parent request {hrRequestDetail.ParentRequestId}: {triggerEx.Message}");
                        // Don't fail the request if immediate email triggering fails - it's a non-critical operation
                    }
                }
                else
                {
                    Console.WriteLine($"Skipped new hire verification job scheduling for HR request detail {existingHRRequestDetail.Id} - " +
                                    $"FirstDayEmployment: {request.PersonalInfo.FirstDayEmployment}, " +
                                    $"Status: {hrRequestDetail?.RequestStatusId}");
                }
            }
            catch (Exception jobEx)
            {
                Console.WriteLine($"WARNING: Failed to schedule new hire verification job for HR request detail {existingHRRequestDetail.Id}: {jobEx.Message}");
                // Job scheduling failure doesn't affect the main operation since transaction is already committed
            }

            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = true,
                Data = new List<HRRequestDetailDto> { updatedHRRequestDetailDto },
                Message = $"Successfully updated and submitted new hire request for {request.PersonalInfo.FirstName} {request.PersonalInfo.LastName}"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            // Log failed update
            var userName = _userContextService.GetUserName();
            _ecmLogger.LogUpdate(false, "NewHireRequest", parentRequestId.ToString(), userName, ex.Message);

            return new ApiResponse<List<HRRequestDetailDto>>
            {
                Success = false,
                Message = $"Failed to update new hire request: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private async Task UpdateOrCreateCreditCardDetailsAsync(int newHireRequestId, NewHireCreditCardInfoDto creditCardInfo, int userId)
    {
        var existingRecord = await _context.CreditCardDetails
            .FirstOrDefaultAsync(c => c.NewHireRequestId == newHireRequestId && !c.IsDeleted);

        if (existingRecord != null)
        {
            // Update existing record
            existingRecord.KwikTripCard = creditCardInfo.KwikTripCard;
            existingRecord.CompanyExpenseCard = creditCardInfo.CompanyExpenseCard;
            existingRecord.CreditExpenseType = creditCardInfo.CreditExpenseType;
            existingRecord.WeeklyLimit = creditCardInfo.WeeklyLimit;
            existingRecord.FuelCardlockAccess = creditCardInfo.FuelCardlockAccess;
            existingRecord.FuelCardlockAddress = creditCardInfo.FuelCardlockAddress;
            existingRecord.ModifiedBy = userId;
            existingRecord.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            // Create new record
            await CreateCreditCardDetailsAsync(newHireRequestId, creditCardInfo, userId);
        }
    }

    private async Task UpdateOrCreateVehicleDetailsAsync(int newHireRequestId, NewHireVehicleInfoDto vehicleInfo, int userId)
    {
        var existingRecord = await _context.VehicleDetails
            .FirstOrDefaultAsync(v => v.NewHireRequestId == newHireRequestId && !v.IsDeleted);

        if (existingRecord != null)
        {
            // Update existing record
            existingRecord.IsApprovedToOperate = vehicleInfo.IsApprovedToOperate;
            existingRecord.DriverClassification = vehicleInfo.DriverClassification;
            existingRecord.DrugAndAlcoholProfile = vehicleInfo.DrugAndAlcoholProfile;
            existingRecord.NeedCompanyCar = vehicleInfo.NeedCompanyCar;
            existingRecord.IsApplicationPart2Complete = vehicleInfo.IsApplicationPart2Complete;
            existingRecord.ModifiedBy = userId;
            existingRecord.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            // Create new record
            await CreateVehicleDetailsAsync(newHireRequestId, vehicleInfo, userId);
        }
    }

    private async Task UpdateOrCreateITDetailsAsync(int newHireRequestId, NewHireITInfoDto itInfo, int userId)
    {
        var existingRecord = await _context.ITDetails
            .FirstOrDefaultAsync(i => i.NewHireRequestId == newHireRequestId && !i.IsDeleted);

        if (existingRecord != null)
        {
            // Update existing record
            existingRecord.EmailRequired = itInfo.EmailRequired;
            existingRecord.AlternateDeliveryLocation = itInfo.AlternateDeliveryLocation;
            existingRecord.MSOfficeLicenseE5 = itInfo.MSOfficeLicenseE5;
            existingRecord.MSOfficeLicenseF3 = itInfo.MSOfficeLicenseF3;
            existingRecord.ModifiedBy = userId;
            existingRecord.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            // Create new record
            await CreateITDetailsAsync(newHireRequestId, itInfo, userId);
        }
    }

    private async Task UpdateOrCreatePhoneRequirementsAsync(int newHireRequestId, NewHirePhoneInfoDto phoneInfo, int userId)
    {
        var existingRecord = await _context.ITPhoneRequirements
            .FirstOrDefaultAsync(p => p.NewHireRequestId == newHireRequestId && !p.IsDeleted);

        if (existingRecord != null)
        {
            // Update existing record
            existingRecord.DeskPhone = phoneInfo.DeskPhone;
            existingRecord.CompanyCellphone = phoneInfo.CompanyCellphone;
            existingRecord.BYODCellphone = phoneInfo.BYODCellphone;
            existingRecord.ReusingExistingPhone = phoneInfo.ReusingExistingPhone;
            existingRecord.WorkPhoneNumber = phoneInfo.WorkPhoneNumber;
            existingRecord.WorkExtension = phoneInfo.WorkExtension;
            existingRecord.ModifiedBy = userId;
            existingRecord.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            // Create new record
            await CreatePhoneRequirementsAsync(newHireRequestId, phoneInfo, userId);
        }
    }

    private async Task UpdateApplicationRequestsAsync(int newHireRequestId, List<NewHireApplicationRequestDto> applications, int userId)
    {
        // Get existing records
        var existingRecords = await _context.ApplicationRequests
            .Where(a => a.NewHireRequestId == newHireRequestId && !a.IsDeleted)
            .ToListAsync();

        // Create lookup dictionaries for efficient comparison
        var existingByAppId = existingRecords.ToDictionary(a => a.ApplicationId, a => a);
        var incomingAppIds = applications.Select(a => a.ApplicationId).ToHashSet();

        // Update or create records
        foreach (var app in applications)
        {
            if (existingByAppId.TryGetValue(app.ApplicationId, out var existingRecord))
            {
                // Update existing record
                existingRecord.AccessNotes = app.AccessNotes;
                existingRecord.ModifiedBy = userId;
                existingRecord.ModifiedDate = DateTime.UtcNow;
            }
            else
            {
                // Create new record
                var applicationRequest = new ApplicationRequest
                {
                    NewHireRequestId = newHireRequestId,
                    ApplicationId = app.ApplicationId,
                    AccessNotes = app.AccessNotes,
                    CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.ApplicationRequests.Add(applicationRequest);
            }
        }

        // Soft delete records that are no longer needed
        foreach (var existingRecord in existingRecords)
        {
            if (!incomingAppIds.Contains(existingRecord.ApplicationId))
            {
                existingRecord.IsDeleted = true;
                existingRecord.ModifiedBy = userId;
                existingRecord.ModifiedDate = DateTime.UtcNow;
            }
        }
    }

    private async Task UpdateFolderRequestsAsync(int newHireRequestId, List<NewHireFolderRequestDto> folders, int userId)
    {
        // Get existing records
        var existingRecords = await _context.FolderRequests
            .Where(f => f.NewHireRequestId == newHireRequestId && !f.IsDeleted)
            .ToListAsync();

        // Create a composite key for comparison (FolderType + FolderName)
        var existingByKey = existingRecords.ToDictionary(f => $"{f.FolderType}|{f.FolderName}", f => f);
        var incomingKeys = folders.Select(f => $"{f.FolderType}|{f.FolderName}").ToHashSet();

        // Update or create records
        foreach (var folder in folders)
        {
            var key = $"{folder.FolderType}|{folder.FolderName}";
            if (existingByKey.TryGetValue(key, out var existingRecord))
            {
                // For folders, just update the modification timestamp (no other fields to update)
                existingRecord.ModifiedBy = userId;
                existingRecord.ModifiedDate = DateTime.UtcNow;
            }
            else
            {
                // Create new record
                var folderRequest = new FolderRequest
                {
                    NewHireRequestId = newHireRequestId,
                    FolderType = folder.FolderType,
                    FolderName = folder.FolderName,
                    CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.FolderRequests.Add(folderRequest);
            }
        }

        // Soft delete records that are no longer needed
        foreach (var existingRecord in existingRecords)
        {
            var key = $"{existingRecord.FolderType}|{existingRecord.FolderName}";
            if (!incomingKeys.Contains(key))
            {
                existingRecord.IsDeleted = true;
                existingRecord.ModifiedBy = userId;
                existingRecord.ModifiedDate = DateTime.UtcNow;
            }
        }
    }

    private async Task UpdateTabletProfilesAsync(int newHireRequestId, List<NewHireTabletProfileDto> tabletProfiles, int userId)
    {
        // Get existing records
        var existingRecords = await _context.ITTabletProfiles
            .Where(t => t.NewHireRequestId == newHireRequestId && !t.IsDeleted)
            .ToListAsync();

        // Create lookup dictionaries for efficient comparison
        var existingByProfileId = existingRecords.ToDictionary(t => t.TabletProfileId, t => t);
        var incomingProfileIds = tabletProfiles.Select(t => t.TabletProfileId).ToHashSet();

        // Update or create records
        foreach (var tablet in tabletProfiles)
        {
            if (existingByProfileId.TryGetValue(tablet.TabletProfileId, out var existingRecord))
            {
                // Update existing record
                existingRecord.TabletProfileName = tablet.TabletProfileName;
                existingRecord.RolesRequiredForNewHire = tablet.RolesRequiredForNewHire;
                existingRecord.ModifiedBy = userId;
                existingRecord.ModifiedDate = DateTime.UtcNow;
            }
            else
            {
                // Create new record
                var tabletProfile = new ITTabletProfile
                {
                    NewHireRequestId = newHireRequestId,
                    TabletProfileId = tablet.TabletProfileId,
                    TabletProfileName = tablet.TabletProfileName,
                    RolesRequiredForNewHire = tablet.RolesRequiredForNewHire,
                    CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.ITTabletProfiles.Add(tabletProfile);
            }
        }

        // Soft delete records that are no longer needed
        foreach (var existingRecord in existingRecords)
        {
            if (!incomingProfileIds.Contains(existingRecord.TabletProfileId))
            {
                existingRecord.IsDeleted = true;
                existingRecord.ModifiedBy = userId;
                existingRecord.ModifiedDate = DateTime.UtcNow;
            }
        }
    }

    private async Task UpdateComputerRequirementsAsync(int newHireRequestId, List<NewHireComputerRequirementDto> computerRequirements, int userId)
    {
        // Get existing records
        var existingRecords = await _context.ITComputerRequirements
            .Where(c => c.NewHireRequestId == newHireRequestId && !c.IsDeleted)
            .ToListAsync();

        // Create lookup dictionaries for efficient comparison
        var existingByReqId = existingRecords.ToDictionary(c => c.ComputerRequirementsId, c => c);
        var incomingReqIds = computerRequirements.Select(c => c.ComputerRequirementsId).ToHashSet();

        // Update or create records
        foreach (var computer in computerRequirements)
        {
            // Look up the description from the master ComputerRequirement table
            var masterRequirement = await _context.ComputerRequirements
                .FirstOrDefaultAsync(c => c.Id == computer.ComputerRequirementsId && !c.IsDeleted);

            var description = masterRequirement?.Description ?? computer.ComputerRequirementsDescription;

            if (existingByReqId.TryGetValue(computer.ComputerRequirementsId, out var existingRecord))
            {
                // Update existing record
                existingRecord.ComputerRequirementsDescription = description;
                existingRecord.IsChild = computer.IsChild;
                existingRecord.ParentId = computer.ParentId;
                existingRecord.ModifiedBy = userId;
                existingRecord.ModifiedDate = DateTime.UtcNow;
            }
            else
            {
                // Create new record
                var computerRequirement = new ITComputerRequirement
                {
                    NewHireRequestId = newHireRequestId,
                    ComputerRequirementsId = computer.ComputerRequirementsId,
                    ComputerRequirementsDescription = description,
                    IsChild = computer.IsChild,
                    ParentId = computer.ParentId,
                    CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.ITComputerRequirements.Add(computerRequirement);
            }
        }

        // Soft delete records that are no longer needed
        foreach (var existingRecord in existingRecords)
        {
            if (!incomingReqIds.Contains(existingRecord.ComputerRequirementsId))
            {
                existingRecord.IsDeleted = true;
                existingRecord.ModifiedBy = userId;
                existingRecord.ModifiedDate = DateTime.UtcNow;
            }
        }
    }

    private async Task UpdateBuildingAccessAsync(int newHireRequestId, List<NewHireBuildingAccessDto> buildingAccess, int userId)
    {
        // Get existing records
        var existingRecords = await _context.NewHireBuildingAccessRequirements
            .Where(b => b.NewHireRequestId == newHireRequestId && !b.IsDeleted)
            .ToListAsync();

        // Create lookup dictionaries for efficient comparison
        var existingByAccessId = existingRecords.ToDictionary(b => b.AccessId, b => b);
        var incomingAccessIds = buildingAccess.Select(b => b.AccessId).ToHashSet();

        // Update or create records
        foreach (var access in buildingAccess)
        {
            if (existingByAccessId.TryGetValue(access.AccessId, out var existingRecord))
            {
                // Update existing record
                existingRecord.AccessDescription = access.AccessDescription;
                existingRecord.ModifiedBy = userId;
                existingRecord.ModifiedDate = DateTime.UtcNow;
            }
            else
            {
                // Create new record
                var buildingAccessRequirement = new NewHireBuildingAccessRequirement
                {
                    NewHireRequestId = newHireRequestId,
                    AccessId = access.AccessId,
                    AccessDescription = access.AccessDescription,
                    CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.NewHireBuildingAccessRequirements.Add(buildingAccessRequirement);
            }
        }

        // Soft delete records that are no longer needed
        foreach (var existingRecord in existingRecords)
        {
            if (!incomingAccessIds.Contains(existingRecord.AccessId))
            {
                existingRecord.IsDeleted = true;
                existingRecord.ModifiedBy = userId;
                existingRecord.ModifiedDate = DateTime.UtcNow;
            }
        }
    }

    public async Task<ApiResponse<NewHireRequestViewDto>> GetNewHireRequestViewByParentIdAsync(int parentRequestId)
    {

        try
        {
            // Get HR request details for this parent request
            var hrRequestDetails = await _context.HRRequestDetails
                .Include(hrd => hrd.ParentRequest)
                .Include(hrd => hrd.RequestStatus)
                .Where(hrd => hrd.ParentRequestId == parentRequestId && !hrd.IsDeleted)
                .FirstOrDefaultAsync();

            if (hrRequestDetails == null)
            {
                return new ApiResponse<NewHireRequestViewDto>
                {
                    Success = false,
                    Message = "HR request details not found"
                };
            }

            // Get new hire details with all related data
            var newHireDetail = await _context.NewHireRequestDetails
                .Include(n => n.HRRequestDetail)
                    .ThenInclude(hrd => hrd.ParentRequest)
                .Include(n => n.HRRequestDetail)
                    .ThenInclude(hrd => hrd.RequestStatus)
                .Where(n => n.RequestDetailId == hrRequestDetails.Id && !n.IsDeleted)
                .FirstOrDefaultAsync();

            if (newHireDetail == null)
            {
                return new ApiResponse<NewHireRequestViewDto>
                {
                    Success = false,
                    Message = "New hire request details not found"
                };
            }

            // Execute database operations completely sequentially to avoid DbContext threading issues
            var creditCard = await _context.CreditCardDetails
                .Where(c => c.NewHireRequestId == newHireDetail.Id && !c.IsDeleted)
                .FirstOrDefaultAsync();
            var vehicle = await _context.VehicleDetails
                .Where(v => v.NewHireRequestId == newHireDetail.Id && !v.IsDeleted)
                .FirstOrDefaultAsync();
            var it = await _context.ITDetails
                .Where(i => i.NewHireRequestId == newHireDetail.Id && !i.IsDeleted)
                .FirstOrDefaultAsync();
            var phone = await _context.ITPhoneRequirements
                .Where(p => p.NewHireRequestId == newHireDetail.Id && !p.IsDeleted)
                .FirstOrDefaultAsync();
            var applications = await _context.ApplicationRequests
                .Include(a => a.Application)
                .Where(a => a.NewHireRequestId == newHireDetail.Id && !a.IsDeleted)
                .ToListAsync();
            var folders = await _context.FolderRequests
                .Where(f => f.NewHireRequestId == newHireDetail.Id && !f.IsDeleted)
                .ToListAsync();
            var tabletProfiles = await _context.ITTabletProfiles
                .Include(t => t.TabletProfile)
                .Where(t => t.NewHireRequestId == newHireDetail.Id && !t.IsDeleted)
                .ToListAsync();
            var computerRequirements = await _context.ITComputerRequirements
                .Include(c => c.ComputerRequirement)
                .Where(c => c.NewHireRequestId == newHireDetail.Id && !c.IsDeleted)
                .ToListAsync();
            var buildingAccess = await _context.NewHireBuildingAccessRequirements
                .Include(b => b.BuildingAccessRequirement)
                .Where(b => b.NewHireRequestId == newHireDetail.Id && !b.IsDeleted)
                .ToListAsync();
            var company = await _context.Companies
                .Where(c => c.CompanyCode == newHireDetail.CompanyCode)
                .FirstOrDefaultAsync();
            var location = await _context.PhysicalLocations
                .Where(l => l.LocationCode == newHireDetail.LocationCode)
                .FirstOrDefaultAsync();
            var position = await _context.Positions
                .Where(p => p.PositionCode == newHireDetail.PositionCode)
                .FirstOrDefaultAsync();
            var payrollDept = await _context.PayrollDepartments
                .Where(pd => pd.DeptCode == newHireDetail.PayrollDeptCode)
                .FirstOrDefaultAsync();
            // Supervisor lookup from Employees table using SupervisorId (employee number)
            var supervisor = newHireDetail.SupervisorId.HasValue
                ? await _context.Employees
                    .Where(e => e.EmployeeNumber == newHireDetail.SupervisorId.Value && !e.IsDeleted)
                    .FirstOrDefaultAsync()
                : null;
            var unionCraft = newHireDetail.UnionCraftId.HasValue
                ? await _context.UnionCrafts.Where(uc => uc.Id == newHireDetail.UnionCraftId.Value).FirstOrDefaultAsync()
                : null;

            // Build the comprehensive view DTO
            var viewDto = new NewHireRequestViewDto
            {
                // HR Request Information
                ParentRequestId = newHireDetail.HRRequestDetail.ParentRequestId,
                RequestTitle = $"New Hire Request - {newHireDetail.FirstName ?? "Unknown"} {newHireDetail.LastName ?? "Unknown"}",
                RequestDescription = $"New hire request for {newHireDetail.FirstName ?? "Unknown"} {newHireDetail.LastName ?? "Unknown"}",
                EffectiveDate = newHireDetail.FirstDayEmployment ?? DateTime.Now, // Handle nullable DateTime
                Notes = newHireDetail.Notes,
                CreatedDate = newHireDetail.HRRequestDetail.ParentRequest.CreatedDate,
                RequestStatusName = newHireDetail.HRRequestDetail.RequestStatus?.RequestStatusName ?? "Unknown",
                SubmittedByName = newHireDetail.HRRequestDetail.ParentRequest.SubmitterName ?? "Unknown User",

                // HR Request Detail Information
                RequestDetailId = newHireDetail.RequestDetailId,
                EmployeeId = newHireDetail.EmployeeId,
                EmployeeNetworkId = newHireDetail.HRRequestDetail.EmployeeNetworkId ?? string.Empty,
                EmployeePositionCode = newHireDetail.HRRequestDetail.EmployeePositionCode ?? string.Empty,

                // Personal Information
                FirstName = newHireDetail.FirstName ?? string.Empty, // Handle nullable string
                LastName = newHireDetail.LastName ?? string.Empty, // Handle nullable string
                MiddleInitial = newHireDetail.MiddleInitial,
                Suffix = newHireDetail.Suffix,
                PreferredFirstName = newHireDetail.PreferredFirstName,
                UserId = newHireDetail.NetworkId,
                FirstDayEmployment = newHireDetail.FirstDayEmployment ?? DateTime.Now, // Handle nullable DateTime
                ReferredBy = newHireDetail.ReferredBy,
                Rehire = newHireDetail.Rehire ?? false, // Handle nullable bool

                // Position Information with Display Names
                CompanyCode = newHireDetail.CompanyCode ?? 0, // Handle nullable int
                CompanyName = company?.CompanyName ?? "Unknown Company",
                LocationCode = newHireDetail.LocationCode ?? 0, // Handle nullable int
                LocationName = location?.LocationName ?? "Unknown Location",
                EmploymentStatus = newHireDetail.EmploymentStatus ?? string.Empty, // Handle nullable string
                IsUnion = newHireDetail.IsUnion,
                UnionCraftId = newHireDetail.UnionCraftId,
                UnionCraftDescription = unionCraft?.Description,
                IsApprentice = newHireDetail.IsApprentice,
                IsUnionWage = newHireDetail.IsUnionWage,
                SalaryCode = newHireDetail.SalaryCode,
                PositionCode = newHireDetail.PositionCode ?? string.Empty, // Handle nullable string
                PositionName = position?.PositionName,
                PayrollDeptCode = newHireDetail.PayrollDeptCode ?? 0, // Handle nullable int
                PayrollDeptName = payrollDept?.DeptName,
                SupervisorId = newHireDetail.SupervisorId ?? 0, // Handle nullable int
                SupervisorName = supervisor != null ? $"{supervisor.FirstName} {supervisor.LastName}" : null,
                AppPercentage = newHireDetail.AppPercentage,

                // Related Information
                CreditCardInfo = creditCard != null ? new CreditCardDetailViewDto
                {
                    KwikTripCard = creditCard.KwikTripCard ?? false,
                    CompanyExpenseCard = creditCard.CompanyExpenseCard ?? false,
                    CreditExpenseType = creditCard.CreditExpenseType,
                    WeeklyLimit = creditCard.WeeklyLimit,
                    FuelCardlockAccess = creditCard.FuelCardlockAccess ?? false,
                    FuelCardlockAddress = creditCard.FuelCardlockAddress
                } : null,

                VehicleInfo = vehicle != null ? new VehicleDetailViewDto
                {
                    IsApprovedToOperate = vehicle.IsApprovedToOperate ?? false,
                    DriverClassification = vehicle.DriverClassification,
                    DrugAndAlcoholProfile = vehicle.DrugAndAlcoholProfile,
                    NeedCompanyCar = vehicle.NeedCompanyCar ?? false,
                    IsApplicationPart2Complete = vehicle.IsApplicationPart2Complete ?? false
                } : null,

                ITInfo = it != null ? new ITDetailViewDto
                {
                    EmailRequired = it.EmailRequired ?? false,
                    AlternateDeliveryLocation = it.AlternateDeliveryLocation,
                    MSOfficeLicenseE5 = it.MSOfficeLicenseE5 ?? false,
                    MSOfficeLicenseF3 = it.MSOfficeLicenseF3 ?? false,
                    EmailAddress = newHireDetail.WorkEmail
                } : null,

                PhoneInfo = phone != null ? new ITPhoneRequirementViewDto
                {
                    DeskPhone = phone.DeskPhone ?? false,
                    CompanyCellphone = phone.CompanyCellphone ?? false,
                    BYODCellphone = phone.BYODCellphone ?? false,
                    ReusingExistingPhone = phone.ReusingExistingPhone ?? false,
                    WorkPhoneNumber = phone.WorkPhoneNumber,
                    WorkExtension = phone.WorkExtension
                } : null,

                Applications = applications.Select(a => new ApplicationRequestViewDto
                {
                    ApplicationId = a.ApplicationId,
                    ApplicationName = a.Application?.Name ?? "Unknown Application",
                    AccessNotes = a.AccessNotes
                }).ToList(),

                Folders = folders.Select(f => new FolderRequestViewDto
                {
                    FolderType = f.FolderType,
                    FolderName = f.FolderName
                }).ToList(),

                TabletProfiles = tabletProfiles.Select(t => new ITTabletProfileViewDto
                {
                    TabletProfileId = t.TabletProfileId,
                    TabletProfileName = t.TabletProfile?.ProfileName ?? "Unknown Profile",
                    RolesRequiredForNewHire = t.RolesRequiredForNewHire
                }).ToList(),

                ComputerRequirements = computerRequirements.Select(c => new ITComputerRequirementViewDto
                {
                    ComputerRequirementsId = c.ComputerRequirementsId,
                    ComputerRequirementsDescription = c.ComputerRequirement?.Description ?? "Unknown Requirement",
                    IsChild = c.IsChild,
                    ParentId = c.ParentId
                }).ToList(),

                BuildingAccess = buildingAccess.Select(b => new NewHireBuildingAccessViewDto
                {
                    AccessId = b.AccessId,
                    AccessDescription = b.BuildingAccessRequirement?.Description ?? "Unknown Access"
                }).ToList(),

                UseExistingKeyFob = newHireDetail.UseExistingKeyFob
            };
            return new ApiResponse<NewHireRequestViewDto>
            {
                Success = true,
                Data = viewDto,
                Message = "New hire request details retrieved successfully"
            };
        }
        catch (Exception ex)
        {

            return new ApiResponse<NewHireRequestViewDto>
            {
                Success = false,
                Message = $"Failed to retrieve new hire request details: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private async Task<string> GenerateUsernameAsync(string? preferredFirstName, string firstName)
    {
        // Use preferred first name if provided, otherwise use first name
        // Remove all spaces to handle names like "Mary Ann" -> "maryann"
        string baseName = !string.IsNullOrEmpty(preferredFirstName)
            ? preferredFirstName.ToLower().Trim().Replace(" ", "")
            : firstName.ToLower().Trim().Replace(" ", "");

        //Format: [Preferred First Name or First Name] + '000' (increment)
        //Example: PreferredFirstName is kim -> does not exists in the Employees table (NetworkId field) --> kim001
        //     else if exists say "kim001"  then next is "kim002"
        //Note: Use preferredFirstName parameter when provided, otherwise use firstName

        // Get all matching NetworkIds and find the max number in C# to avoid string ordering issues
        // Use EF.Functions.Like for case-insensitive pattern matching
        var existingNetworkIds = await _context.Employees
            .Where(e => !e.IsDeleted
                && e.NetworkId != null
                && EF.Functions.Like(e.NetworkId, baseName + "%"))
            .Select(e => e.NetworkId)
            .ToListAsync();

        int maxNumber = 0;

        foreach (var networkId in existingNetworkIds)
        {
            if (networkId == null || networkId.Length <= baseName.Length) continue;

            // Extract the numeric suffix after the base name (case-insensitive)
            string suffix = networkId.Substring(baseName.Length);

            // Only consider entries where suffix is purely numeric
            if (int.TryParse(suffix, out int number))
            {
                if (number > maxNumber)
                {
                    maxNumber = number;
                }
            }
        }

        // Generate next username
        int nextNumber = maxNumber + 1;
        return $"{baseName}{nextNumber:D3}"; // Format with leading zeros (e.g., 001, 012, 123)
    }


    private string GenerateSecurePassword(int length = 12)
    {
        // Get password policy from configuration with defaults
        var requireUppercase = bool.Parse(_configuration["ActiveDirectory:PasswordPolicy:RequireUppercase"] ?? "true");
        var requireLowercase = bool.Parse(_configuration["ActiveDirectory:PasswordPolicy:RequireLowercase"] ?? "true");
        var requireNumbers = bool.Parse(_configuration["ActiveDirectory:PasswordPolicy:RequireNumbers"] ?? "true");
        var requireSpecialCharacters = bool.Parse(_configuration["ActiveDirectory:PasswordPolicy:RequireSpecialCharacters"] ?? "true");
        var allowedSpecialCharacters = _configuration["ActiveDirectory:PasswordPolicy:AllowedSpecialCharacters"] ?? "!@#$%^&*";
        var configuredLength = int.TryParse(_configuration["ActiveDirectory:PasswordPolicy:Length"], out var parsedLength) ? parsedLength : length;

        // Use configured length if valid, otherwise use default
        if (configuredLength >= 8 && configuredLength <= 128)
        {
            length = configuredLength;
        }

        // Define character sets
        const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        const string numberChars = "0123456789";
        var specialChars = allowedSpecialCharacters;

        var allChars = "";
        var requiredChars = new List<char>();

        // Build character set and ensure minimum requirements
        if (requireUppercase)
        {
            allChars += uppercaseChars;
            requiredChars.Add(GetRandomChar(uppercaseChars));
        }

        if (requireLowercase)
        {
            allChars += lowercaseChars;
            requiredChars.Add(GetRandomChar(lowercaseChars));
        }

        if (requireNumbers)
        {
            allChars += numberChars;
            requiredChars.Add(GetRandomChar(numberChars));
        }

        if (requireSpecialCharacters)
        {
            allChars += specialChars;
            requiredChars.Add(GetRandomChar(specialChars));
        }

        // If no requirements specified, use all character types
        if (string.IsNullOrEmpty(allChars))
        {
            allChars = uppercaseChars + lowercaseChars + numberChars + specialChars;
            requiredChars.Add(GetRandomChar(uppercaseChars));
            requiredChars.Add(GetRandomChar(lowercaseChars));
            requiredChars.Add(GetRandomChar(numberChars));
            requiredChars.Add(GetRandomChar(specialChars));
        }

        // Fill remaining length with random characters
        var passwordChars = new List<char>(requiredChars);
        for (int i = requiredChars.Count; i < length; i++)
        {
            passwordChars.Add(GetRandomChar(allChars));
        }

        // Shuffle the password to avoid predictable patterns
        for (int i = passwordChars.Count - 1; i > 0; i--)
        {
            int j = GetRandomInt(0, i + 1);
            (passwordChars[i], passwordChars[j]) = (passwordChars[j], passwordChars[i]);
        }

        return new string(passwordChars.ToArray());
    }

    private char GetRandomChar(string chars)
    {
        if (string.IsNullOrEmpty(chars))
            throw new ArgumentException("Character set cannot be empty", nameof(chars));

        int index = GetRandomInt(0, chars.Length);
        return chars[index];
    }

    private int GetRandomInt(int minValue, int maxValue)
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[4];
        rng.GetBytes(bytes);
        uint value = BitConverter.ToUInt32(bytes, 0);
        return (int)(minValue + (value % (maxValue - minValue)));
    }

    public async Task<(bool success, string? username, string? email, string? password)> CreateUserInADOU(
        int companyCode,
        int payrollDeptCode,
        string? preferredFirstName,
        string firstName,
        string lastName,
        string? middleInitial = null,
        string? title = null,
        string? department = null,
        string? preGeneratedEmail = null)
    {
        try
        {
            // Generate username automatically
            var username = await GenerateUsernameAsync(preferredFirstName, firstName);
            Console.WriteLine($"Generated username: {username}");

            string? generatedEmail = null;

            // Get domain from CompanyTypeLocation table where CompanyCode == companyCode
            var companyTypeLocation = await _context.CompanyTypeLocations
                .FirstOrDefaultAsync(c => c.CompanyCode == companyCode && !c.IsDeleted);

            if (companyTypeLocation == null || string.IsNullOrEmpty(companyTypeLocation.Domain))
            {
                var currentUserName = _userContextService.GetUserName();
                _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, username, $"Could not find domain for CompanyCode {companyCode} in CompanyTypeLocation table");
                return (false, username, null, null);
            }

            var domain = companyTypeLocation.Domain;
            var locationType = companyTypeLocation.LocationType;
            Console.WriteLine($"Retrieved domain '{domain}' for CompanyCode {companyCode}");
            Console.WriteLine($"LocationType: '{locationType}'");

            // Get EmailDomain from PayrollDepartments table for automatic email generation
            var payrollDepartment = await _context.PayrollDepartments
                .FirstOrDefaultAsync(pd => pd.CompanyCode == companyCode && pd.DeptCode == payrollDeptCode && !pd.IsDeleted);

            var emailDomain = payrollDepartment?.EmailDomain;
            if (string.IsNullOrEmpty(emailDomain))
            {
                var currentUserName = _userContextService.GetUserName();
                _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, username, $"EmailDomain not found for CompanyCode {companyCode}, DeptCode {payrollDeptCode} in PayrollDepartments table");
                return (false, username, null, null);
            }
            Console.WriteLine($"Retrieved email domain '{emailDomain}' from PayrollDepartments for CompanyCode {companyCode}, DeptCode {payrollDeptCode}");

            // Determine which first name to use (preferred or actual)
            var displayFirstName = !string.IsNullOrEmpty(preferredFirstName) ? preferredFirstName : firstName;

            // Capitalize first letter of displayFirstName and lastName
            if (!string.IsNullOrEmpty(displayFirstName))
            {
                displayFirstName = char.ToUpper(displayFirstName[0]) + displayFirstName.Substring(1);
            }
            if (!string.IsNullOrEmpty(lastName))
            {
                lastName = char.ToUpper(lastName[0]) + lastName.Substring(1);
            }

            // Use pre-generated email from frontend if available, otherwise generate
            if (!string.IsNullOrEmpty(preGeneratedEmail))
            {
                generatedEmail = preGeneratedEmail;
                Console.WriteLine($"Using pre-generated email address from frontend: {generatedEmail}");
            }
            else
            {
                generatedEmail = $"{username}@{emailDomain}";
                Console.WriteLine($"Generated email address: {generatedEmail}");
            }

            // Get AD configuration for this LocationType (company/domain)
            var (ouPathTemplate, adUsername, adPassword, defaultGroups) = GetADConfigurationByLocationtype(locationType);
            if (string.IsNullOrEmpty(ouPathTemplate))
            {
                var currentUserName = _userContextService.GetUserName();
                _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, username, $"ActiveDirectory:OUPath not configured for LocationType '{locationType}' in appsettings");
                return (false, username, generatedEmail, null);
            }
            if (string.IsNullOrEmpty(adUsername) || string.IsNullOrEmpty(adPassword))
            {
                var currentUserName = _userContextService.GetUserName();
                _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, username, $"Could not retrieve AD credentials for LocationType '{locationType}'");
                return (false, username, generatedEmail, null);
            }

            // Convert domain to DC format (e.g., MATHY.LOCAL → DC=MATHY,DC=LOCAL)
            var domainParts = domain.Split('.');
            var dcPath = string.Join(",", domainParts.Select(part => $"DC={part}"));
            var ouPath = ouPathTemplate.Replace("{domain}", dcPath);
            Console.WriteLine($"Using OU path: {ouPath}");

            return await Task.Run(async () =>
            {
                string? generatedPassword = null;

                // Build expected display name for comparison (includes middle initial if provided)
                // Declared here so it's accessible in both try and catch blocks
                var expectedDisplayName = !string.IsNullOrWhiteSpace(middleInitial)
                    ? $"{displayFirstName} {middleInitial.Trim().ToUpper()[0]} {lastName}"
                    : $"{displayFirstName} {lastName}";

                try
                {
                    Console.WriteLine($"DEBUG: AD Username from config: {adUsername}");
                    Console.WriteLine($"DEBUG: AD Password present: {!string.IsNullOrEmpty(adPassword)}");
                    Console.WriteLine($"DEBUG: Connecting to domain: {domain}");
                    Console.WriteLine($"DEBUG: Using service account for LocationType '{locationType}'");

                    // Try to create user directly in target OU first (most efficient)
                    // If OU binding fails, fall back to domain-level creation
                    PrincipalContext context = null;
                    bool userCreatedInOu = false;

                    try
                    {
                        Console.WriteLine($"Attempting to create user directly in OU: {ouPath}");
                        context = new PrincipalContext(ContextType.Domain, domain, ouPath, adUsername, adPassword);
                        userCreatedInOu = true;
                    }
                    catch (PrincipalServerDownException ex)
                    {
                        Console.WriteLine($"WARNING: OU binding issue detected: {ex.Message}");
                        Console.WriteLine($"Falling back to domain-level creation...");
                        context = new PrincipalContext(ContextType.Domain, domain, null, adUsername, adPassword);
                        userCreatedInOu = false;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Console.WriteLine($"WARNING: Access denied to OU: {ex.Message}");
                        Console.WriteLine($"Falling back to domain-level creation...");
                        context = new PrincipalContext(ContextType.Domain, domain, null, adUsername, adPassword);
                        userCreatedInOu = false;
                    }

                    using (context)
                    {
                    // Check if generated username already exists in AD using recursive lookup
                    var adService = new AdUserService(domain, userCreatedInOu ? ouPath : null, adUsername, adPassword);
                    var adResult = adService.GenerateUniqueUserId(username, expectedDisplayName);

                    if (adResult.SamePersonExists)
                    {
                        // Same person already exists in AD — prevent duplicate
                        var currentUserName = _userContextService.GetUserName();
                        _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, adResult.UserId,
                            $"User '{adResult.UserId}' already exists in AD with matching DisplayName '{adResult.DisplayName}'");
                        return (false, adResult.UserId, generatedEmail, null);
                    }

                    // Update username and email if AD collision caused an increment
                    var originalUsername = username;
                    username = adResult.UserId;
                    if (username != originalUsername)
                    {
                        // Username changed due to AD collision — always regenerate email to match
                        generatedEmail = $"{username}@{emailDomain}";
                        Console.WriteLine($"AD collision: username changed from '{originalUsername}' to '{username}', email updated to '{generatedEmail}'");
                    }

                    // Create the user principal
                    using (var user = new UserPrincipal(context))
                    {
                        // Basic account settings
                        user.SamAccountName = username; //sample kim001
                        user.UserPrincipalName = $"{username}@{domain}"; //sample kim001@mathy.local
                        user.Name = expectedDisplayName; //sample Kim N Novy (includes middle initial if provided)
                        user.DisplayName = expectedDisplayName; //sample Kim N Novy (includes middle initial if provided)
                        user.GivenName = displayFirstName; //sample Kim (uses preferred name if available)
                        user.Surname = lastName;
                        //user.EmployeeId = username; //sample kim001

                        // Set email address (already generated earlier)
                        user.EmailAddress = generatedEmail; //sample kim.oberweiser@corpmts.com

                        // Account settings
                        user.Enabled = true;
                        user.PasswordNeverExpires = false;
                        user.UserCannotChangePassword = false;
                        user.PasswordNotRequired = false;

                        // Generate and set a secure password
                        generatedPassword = GenerateSecurePassword();
                        user.SetPassword(generatedPassword);
                        user.ExpirePasswordNow(); // Force password change at first logon
                        Console.WriteLine($"Generated secure password for user '{username}': {generatedPassword}");

                        // Save the user first
                        user.Save();

                        // If user was created at domain level due to OU binding issues, move to correct OU
                        if (!userCreatedInOu)
                        {
                            Console.WriteLine($"User created at domain level, attempting to move to target OU...");
                            MoveUserToOU(user, ouPath, domain, adUsername, adPassword);
                        }
                        else
                        {
                            Console.WriteLine($"User created directly in target OU: {ouPath}");
                        }

                        // Now set additional properties using DirectoryEntry if provided
                        if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(department))
                        {
                            SetAdditionalUserProperties(user, title, department);
                        }

                        // Add user to configured default AD groups for this LocationType
                        if (defaultGroups != null && defaultGroups.Length > 0)
                        {
                            Console.WriteLine($"Adding user to {defaultGroups.Length} default AD group(s)...");
                            foreach (var groupName in defaultGroups)
                            {
                                var addedToGroup = await AddUserToADGroup(companyCode, username, groupName);
                                if (addedToGroup)
                                {
                                    Console.WriteLine($"✓ Successfully added '{username}' to group '{groupName}'");
                                }
                                else
                                {
                                    Console.WriteLine($"✗ Failed to add '{username}' to group '{groupName}'");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("No default AD groups configured.");
                        }

                        Console.WriteLine($"User '{username}' created successfully");

                        // Log successful AD user creation
                        var currentUserName = _userContextService.GetUserName();
                        _ecmLogger.LogActiveDirectory(true, "CreateUser", currentUserName, username, "Successfully created a User in AD");

                        return (true, username, generatedEmail, generatedPassword);
                    }
                    }
                }
                catch (PrincipalExistsException ex)
                {
                    Console.WriteLine($"User '{username}' already exists in Active Directory.");

                    // Log failed AD user creation
                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, username, $"User already exists: {ex.Message}");

                    return (false, username, generatedEmail, null);
                }
                catch (PasswordException ex)
                {
                    Console.WriteLine($"Password policy error: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");

                    // Log failed AD user creation
                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, username, $"Password policy error: {ex.Message}");

                    return (false, username, generatedEmail, null);
                }
                catch (PrincipalOperationException ex)
                {
                    Console.WriteLine($"AD operation error: {ex.Message}");
                    Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");

                    // Log failed AD user creation
                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, username, $"AD operation error: {ex.Message}");

                    return (false, username, generatedEmail, null);
                }
                catch (System.DirectoryServices.DirectoryServicesCOMException ex)
                {
                    Console.WriteLine($"Directory Services COM error: {ex.Message}");
                    Console.WriteLine($"Extended error: {ex.ExtendedErrorMessage}");
                    Console.WriteLine($"Error code: 0x{ex.ErrorCode:X}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");

                    // Log failed AD user creation
                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, username, $"Directory Services COM error: {ex.Message}");

                    return (false, username, generatedEmail, null);
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    Console.WriteLine($"COM error connecting to AD: {ex.Message}");
                    Console.WriteLine($"Error code: 0x{ex.ErrorCode:X}");
                    Console.WriteLine($"Possible causes: Invalid credentials, network issues, or insufficient permissions");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");

                    // Log failed AD user creation
                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, username, $"COM error: {ex.Message}");

                    return (false, username, generatedEmail, null);
                }
                catch (InvalidOperationException ex) when (
                    ex.Message.Contains("constraint violation", StringComparison.OrdinalIgnoreCase) ||
                    ex.InnerException?.Message?.Contains("constraint violation", StringComparison.OrdinalIgnoreCase) == true ||
                    ex.InnerException?.Message?.Contains("object already exists", StringComparison.OrdinalIgnoreCase) == true ||
                    ex.InnerException?.Message?.Contains("ENTRY_EXISTS", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Determine which attribute likely caused the duplicate
                    var duplicateDetails = new List<string>();

                    duplicateDetails.Add($"sAMAccountName: '{username}'");
                    duplicateDetails.Add($"UserPrincipalName: '{username}@{domain}'");
                    duplicateDetails.Add($"CN (Name): '{expectedDisplayName}' in OU '{ouPath}'");

                    var detailMessage = string.Join(", ", duplicateDetails);
                    var innerMsg = ex.InnerException?.Message ?? "No inner exception details";

                    Console.WriteLine($"AD constraint violation - likely duplicate user detected.");
                    Console.WriteLine($"  Attempted values - {detailMessage}");
                    Console.WriteLine($"  Error: {ex.Message}");
                    Console.WriteLine($"  Inner exception: {innerMsg}");
                    Console.WriteLine($"  Check AD for existing objects with the same sAMAccountName, UPN, or CN in the target OU.");

                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, username,
                        $"Duplicate user detected (constraint violation). A user with the same sAMAccountName ('{username}'), UPN ('{username}@{domain}'), or CN ('{expectedDisplayName}') already exists in AD. Inner exception: {innerMsg}");

                    return (false, username, generatedEmail, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error creating user: {ex.Message}");

                    // Log failed AD user creation
                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, username, $"Unexpected error: {ex.Message}");
                    Console.WriteLine($"Exception type: {ex.GetType().Name}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception type: {ex.InnerException.GetType().Name}");
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    return (false, username, generatedEmail, null);
                }
            });
        }
        catch (Exception ex)
        {
            var currentUserName = _userContextService.GetUserName();
            _ecmLogger.LogActiveDirectory(false, "CreateUser", currentUserName, null, $"Failed to retrieve domain or configuration: {ex.Message}");
            return (false, null, null, null);
        }
    }

    private void SetAdditionalUserProperties(UserPrincipal user, string? title, string? department)
    {
        try
        {
            DirectoryEntry directoryEntry = (DirectoryEntry)user.GetUnderlyingObject();

            if (!string.IsNullOrEmpty(title))
            {
                directoryEntry.Properties["title"].Value = title;
            }

            if (!string.IsNullOrEmpty(department))
            {
                directoryEntry.Properties["department"].Value = department;
            }

            directoryEntry.CommitChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not set additional properties: {ex.Message}");
        }
    }

    public async Task<bool> AddUserToADGroup(int companyCode, string username, string groupName)
    {
        try
        {
            // Get domain from CompanyTypeLocation table where CompanyCode == companyCode
            var companyTypeLocation = await _context.CompanyTypeLocations
                .FirstOrDefaultAsync(c => c.CompanyCode == companyCode && !c.IsDeleted);

            if (companyTypeLocation == null || string.IsNullOrEmpty(companyTypeLocation.Domain))
            {
                Console.WriteLine($"ERROR: Could not find domain for CompanyCode {companyCode} in CompanyTypeLocation table");
                return false;
            }

            var domain = companyTypeLocation.Domain;
            var locationType = companyTypeLocation.LocationType;
            Console.WriteLine($"Retrieved domain '{domain}' for CompanyCode {companyCode}");
            Console.WriteLine($"LocationType: '{locationType}'");

            // Get AD configuration for this LocationType (company/domain)
            var (ouPath, adUsername, adPassword, defaultGroups) = GetADConfigurationByLocationtype(locationType);
            if (string.IsNullOrEmpty(adUsername) || string.IsNullOrEmpty(adPassword))
            {
                Console.WriteLine($"ERROR: Could not retrieve AD credentials for LocationType '{locationType}'");
                return false;
            }

            return await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine($"Using service account: {adUsername}");

                    // Create PrincipalContext for the domain with credentials
                    using (var context = new PrincipalContext(ContextType.Domain, domain, null, adUsername, adPassword))
                    {
                        // Find the user
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user == null)
                            {
                                Console.WriteLine($"ERROR: User '{username}' not found in domain '{domain}'");
                                return false;
                            }

                            Console.WriteLine($"Found user '{username}' in domain '{domain}'");

                            // Find the group
                            using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                            {
                                if (group == null)
                                {
                                    Console.WriteLine($"ERROR: Group '{groupName}' not found in domain '{domain}'");
                                    return false;
                                }

                                Console.WriteLine($"Found group '{groupName}' in domain '{domain}'");

                                // Check if user is already a member of the group
                                if (group.Members.Contains(user))
                                {
                                    Console.WriteLine($"User '{username}' is already a member of group '{groupName}'");
                                    return true; // Consider this a success since the desired state is achieved
                                }

                                // Add user to the group
                                group.Members.Add(user);
                                group.Save();

                                Console.WriteLine($"Successfully added user '{username}' to group '{groupName}' in domain '{domain}'");

                                // Log successful AD group addition
                                var currentUserName = _userContextService.GetUserName();
                                _ecmLogger.LogActiveDirectory(true, $"AddToGroup:{groupName}", currentUserName, username, null);

                                return true;
                            }
                        }
                    }
                }
                catch (PrincipalOperationException ex)
                {
                    Console.WriteLine($"Active Directory operation error: {ex.Message}");

                    // Log failed AD group addition
                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, $"AddToGroup:{groupName}", currentUserName, username, $"AD operation error: {ex.Message}");

                    return false;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Access denied: {ex.Message}. Check if the service account has permissions to modify group membership.");

                    // Log failed AD group addition
                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, $"AddToGroup:{groupName}", currentUserName, username, $"Access denied: {ex.Message}");

                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error adding user to group: {ex.Message}");

                    // Log failed AD group addition
                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, $"AddToGroup:{groupName}", currentUserName, username, $"Unexpected error: {ex.Message}");

                    return false;
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to retrieve domain or configuration: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteUserFromAD(string username, int companyCode)
    {
        try
        {
            Console.WriteLine($"[ROLLBACK] Attempting to delete AD user: '{username}'");

            // Get domain from CompanyTypeLocation table where CompanyCode == companyCode
            var companyTypeLocation = await _context.CompanyTypeLocations
                .FirstOrDefaultAsync(c => c.CompanyCode == companyCode && !c.IsDeleted);

            if (companyTypeLocation == null || string.IsNullOrEmpty(companyTypeLocation.Domain))
            {
                Console.WriteLine($"[ROLLBACK] ERROR: Could not find domain for CompanyCode {companyCode} in CompanyTypeLocation table");
                return false;
            }

            var domain = companyTypeLocation.Domain;
            var locationType = companyTypeLocation.LocationType;
            Console.WriteLine($"[ROLLBACK] Retrieved domain '{domain}' for CompanyCode {companyCode}");
            Console.WriteLine($"[ROLLBACK] LocationType: '{locationType}'");

            // Get AD configuration for this LocationType (company/domain)
            var (ouPath, adUsername, adPassword, defaultGroups) = GetADConfigurationByLocationtype(locationType);
            if (string.IsNullOrEmpty(adUsername) || string.IsNullOrEmpty(adPassword))
            {
                Console.WriteLine($"[ROLLBACK] ERROR: Could not retrieve AD credentials for LocationType '{locationType}'");
                return false;
            }

            return await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine($"[ROLLBACK] Using service account: {adUsername}");

                    // Create PrincipalContext for the domain with credentials
                    using (var context = new PrincipalContext(ContextType.Domain, domain, null, adUsername, adPassword))
                    {
                        // Find the user
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user == null)
                            {
                                Console.WriteLine($"[ROLLBACK] User '{username}' not found in domain '{domain}' - may have already been deleted or never created");
                                return true; // Consider this a success since the user doesn't exist
                            }

                            Console.WriteLine($"[ROLLBACK] Found user '{username}' in domain '{domain}', proceeding with deletion");

                            // Delete the user
                            user.Delete();

                            Console.WriteLine($"[ROLLBACK] Successfully deleted user '{username}' from Active Directory");

                            // Log successful AD user deletion
                            var currentUserName = _userContextService.GetUserName();
                            _ecmLogger.LogActiveDirectory(true, "DeleteUser", currentUserName, username, null);

                            return true;
                        }
                    }
                }
                catch (PrincipalOperationException ex)
                {
                    Console.WriteLine($"[ROLLBACK] Active Directory operation error during user deletion: {ex.Message}");
                    Console.WriteLine($"[ROLLBACK] Stack trace: {ex.StackTrace}");

                    // Log failed AD user deletion
                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, "DeleteUser", currentUserName, username, $"AD operation error: {ex.Message}");

                    return false;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"[ROLLBACK] Access denied during user deletion: {ex.Message}. Check if the service account has permissions to delete users.");

                    // Log failed AD user deletion
                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, "DeleteUser", currentUserName, username, $"Access denied: {ex.Message}");

                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ROLLBACK] Unexpected error deleting user from AD: {ex.Message}");
                    Console.WriteLine($"[ROLLBACK] Stack trace: {ex.StackTrace}");

                    // Log failed AD user deletion
                    var currentUserName = _userContextService.GetUserName();
                    _ecmLogger.LogActiveDirectory(false, "DeleteUser", currentUserName, username, $"Unexpected error: {ex.Message}");

                    return false;
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ROLLBACK] ERROR: Failed to retrieve domain or configuration during user deletion: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get Active Directory configuration (OUPath, ServiceAccount, DefaultUserGroups) for a specific LocationType
    /// </summary>
    private (string? ouPath, string? serviceAccountUsername, string? serviceAccountPassword, string[]? defaultGroups)
        GetADConfigurationByLocationtype(string locationTypeKey)
    {
        try
        {
            // Get the Domains array from configuration
            var domainsSection = _configuration.GetSection("ActiveDirectory:Domains");
            if (domainsSection == null)
            {
                Console.WriteLine($"WARNING: ActiveDirectory:Domains section not found in configuration");
                return (null, null, null, null);
            }

            // Find the domain configuration matching the locationTypeKey
            var domainConfig = domainsSection.GetChildren()
                .FirstOrDefault(d => d["Name"]?.Equals(locationTypeKey, StringComparison.OrdinalIgnoreCase) == true);

            if (domainConfig == null)
            {
                Console.WriteLine($"WARNING: No ActiveDirectory configuration found for LocationType '{locationTypeKey}'");
                return (null, null, null, null);
            }

            // Extract configuration values
            var ouPath = domainConfig["OUPath"];
            var serviceAccountUsername = domainConfig["ServiceAccount:Username"];
            var serviceAccountPassword = domainConfig["ServiceAccount:Password"];

            // Extract DefaultUserGroups array
            var groupsSection = domainConfig.GetSection("DefaultUserGroups");
            var defaultGroups = groupsSection.GetChildren()
                .Select(g => g.Value)
                .Where(g => !string.IsNullOrEmpty(g))
                .ToArray();

            Console.WriteLine($"Retrieved AD configuration for LocationType '{locationTypeKey}':");
            Console.WriteLine($"  OUPath: {ouPath}");
            Console.WriteLine($"  ServiceAccount: {serviceAccountUsername}");
            Console.WriteLine($"  DefaultUserGroups: {string.Join(", ", defaultGroups ?? Array.Empty<string>())}");

            return (ouPath, serviceAccountUsername, serviceAccountPassword, defaultGroups);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to get AD configuration for LocationType '{locationTypeKey}': {ex.Message}");
            return (null, null, null, null);
        }
    }

    private void MoveUserToOU(UserPrincipal user, string targetOuPath, string domain, string username, string password)
    {
        try
        {
            Console.WriteLine($"Attempting to move user to OU: {targetOuPath}");

            // Get the user's distinguished name
            var userDN = user.DistinguishedName;
            Console.WriteLine($"User DN: {userDN}");

            // Create DirectoryEntry for the user WITH credentials and domain (important!)
            var userEntry = new DirectoryEntry($"LDAP://{domain}/{userDN}", username, password);

            // Strip LDAP:// prefix if present, then add it back correctly with domain
            var cleanPath = targetOuPath.Replace("LDAP://", "");
            var targetOu = new DirectoryEntry($"LDAP://{domain}/{cleanPath}", username, password);

            Console.WriteLine($"Target OU DN: {cleanPath}");
            Console.WriteLine($"Full user LDAP path: LDAP://{domain}/{userDN}");
            Console.WriteLine($"Full target OU LDAP path: LDAP://{domain}/{cleanPath}");

            userEntry.MoveTo(targetOu);
            userEntry.CommitChanges();

            Console.WriteLine($"Successfully moved user to OU: {cleanPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not move user to OU {targetOuPath}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            // Don't throw - user was created successfully, just not in the desired OU
        }
    }


}
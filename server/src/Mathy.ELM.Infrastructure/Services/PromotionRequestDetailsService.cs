using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Infrastructure.Data;

namespace Mathy.ELM.Infrastructure.Services;

public class PromotionRequestDetailsService : IPromotionRequestDetailsService
{
    private readonly MathyELMContext _context;
    private readonly IHRRequestService _hrRequestService;
    private readonly ILogger<PromotionRequestDetailsService> _logger;
    private readonly IEcmLogger _ecmLogger;

    public PromotionRequestDetailsService(
        MathyELMContext context,
        IHRRequestService hrRequestService,
        ILogger<PromotionRequestDetailsService> logger,
        IEcmLogger ecmLogger)
    {
        _context = context;
        _hrRequestService = hrRequestService;
        _logger = logger;
        _ecmLogger = ecmLogger;
    }

    /// <summary>
    /// Step 2: Create Promotion Request Details
    /// Creates PromotionRequestDetail entity and all related PT* child records
    /// </summary>
    public async Task<ApiResponse<PromotionRequestDetailDto>> CreatePromotionRequestDetailsAsync(
        int hrRequestDetailId,
        CreatePromotionRequestDto promotionData,
        string? currentNetworkId = null)
    {
        var logId = Guid.NewGuid().ToString();
        try
        {
            _logger.LogInformation($"[{logId}] Starting CreatePromotionRequestDetailsAsync for HRRequestDetailId: {hrRequestDetailId}");

            // Get the HR Request Detail
            var hrRequestDetail = await _context.HRRequestDetails
                .FirstOrDefaultAsync(x => x.Id == hrRequestDetailId);

            if (hrRequestDetail == null)
            {
                _logger.LogError($"[{logId}] HRRequestDetail not found: {hrRequestDetailId}");
                _ecmLogger.LogSave(false, "PromotionRequest", hrRequestDetailId, null, "HR Request Detail not found");
                return new ApiResponse<PromotionRequestDetailDto>
                {
                    Success = false,
                    Message = "HR Request Detail not found"
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate and fix CurrentPositionCode if it's a position name instead of code
                string? currentPositionCode = promotionData.CurrentPositionCode;
                if (!string.IsNullOrEmpty(currentPositionCode) && currentPositionCode.Length > 10)
                {
                    _logger.LogWarning($"[{logId}] CurrentPositionCode appears to be a name ('{currentPositionCode}') instead of a code. Attempting to resolve...");

                    // Try to find the position by name
                    var position = await _context.Positions
                        .FirstOrDefaultAsync(p => p.PositionName == currentPositionCode);

                    if (position != null)
                    {
                        _logger.LogInformation($"[{logId}] Resolved position name '{currentPositionCode}' to code '{position.PositionCode}'");
                        currentPositionCode = position.PositionCode;
                    }
                    else
                    {
                        // Truncate to avoid database error, but log warning
                        _logger.LogWarning($"[{logId}] Could not resolve position name '{currentPositionCode}'. Truncating to 10 characters.");
                        currentPositionCode = currentPositionCode.Substring(0, 10);
                    }
                }

                // Phase 1: Create Main PromotionRequestDetail Record
                _logger.LogInformation($"[{logId}] Phase 1: Creating PromotionRequestDetail");
                _logger.LogInformation($"[{logId}] Phase 1 Data - NewPayrollCompanyCode: {promotionData.NewPayrollCompanyCode}, NewPayrollGroupCode: {promotionData.NewPayrollGroupCode}, NewPositionCode: {promotionData.NewPositionCode}");

                var promotionRequestDetail = new PromotionRequestDetail
                {
                    // Current position info
                    CurrentPayrollCompanyCode = promotionData.CurrentPayrollCompanyCode,
                    CurrentPayrollGroupCode = promotionData.CurrentPayrollGroupCode,
                    CurrentPayrollDeptCode = promotionData.CurrentPayrollDeptCode,
                    CurrentPositionCode = currentPositionCode,
                    CurrentSupervisorId = promotionData.CurrentSupervisorId,
                    CurrentPhysicalLocationCode = promotionData.CurrentPhysicalLocationCode,
                    CurrentStatus = promotionData.CurrentStatus,
                    CurrentSalaryCode = promotionData.CurrentSalaryCode,
                    CurrentWorkEmail = promotionData.CurrentWorkEmail,

                    // New position info
                    NewPayrollCompanyCode = promotionData.NewPayrollCompanyCode,
                    NewPayrollGroupCode = promotionData.NewPayrollGroupCode,
                    NewPayrollDeptCode = promotionData.NewPayrollDeptCode,
                    NewPositionCode = promotionData.NewPositionCode,
                    NewSupervisorId = promotionData.NewSupervisorId,
                    NewPhysicalLocationCode = promotionData.NewPhysicalLocationCode,
                    NewStatus = promotionData.NewStatus,
                    NewSalaryCode = promotionData.NewSalaryCode,
                    NewWorkEmail = promotionData.NewWorkEmail,

                    // Building Access
                    UseExistingKeyFob = promotionData.UseExistingKeyFob,

                    // Audit fields
                    CreatedBy = hrRequestDetail.CreatedBy,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedBy = null,
                    ModifiedDate = null,
                    IsDeleted = false
                };

                _logger.LogInformation($"[{logId}] Phase 1: Setting PromotionDetails navigation property");
                hrRequestDetail.PromotionDetails = promotionRequestDetail;
                _logger.LogInformation($"[{logId}] Phase 1: Adding PromotionRequestDetail to context");
                _context.PromotionRequestDetails.Add(promotionRequestDetail);
                _logger.LogInformation($"[{logId}] Phase 1: PromotionRequestDetail added successfully");

                // Phase 2: Create 1:1 PT Child Records (only if data exists)
                _logger.LogInformation($"[{logId}] Phase 2: Creating PT child records");

                if (promotionData.CreditCardInfo != null)
                {
                    _logger.LogInformation($"[{logId}] Phase 2: Creating PTCreditCardDetail");
                    await CreatePTCreditCardDetailAsync(promotionRequestDetail, promotionData.CreditCardInfo, hrRequestDetail.CreatedBy);
                }

                if (promotionData.VehicleInfo != null)
                {
                    _logger.LogInformation($"[{logId}] Phase 2: Creating PTVehicleDetail");
                    await CreatePTVehicleDetailAsync(promotionRequestDetail, promotionData.VehicleInfo, hrRequestDetail.CreatedBy);
                }

                if (promotionData.ITInfo != null)
                {
                    _logger.LogInformation($"[{logId}] Phase 2: Creating PTITDetail");
                    await CreatePTITDetailAsync(promotionRequestDetail, promotionData.ITInfo, hrRequestDetail.CreatedBy);
                }

                if (promotionData.PhoneInfo != null)
                {
                    _logger.LogInformation($"[{logId}] Phase 2: Creating PTITPhoneRequirement");
                    await CreatePTPhoneRequirementAsync(promotionRequestDetail, promotionData.PhoneInfo, hrRequestDetail.CreatedBy);
                }

                // Phase 3: Create 1:Many PT Child Records
                _logger.LogInformation($"[{logId}] Phase 3: Creating PT child collections");

                if (promotionData.Applications?.Any() == true)
                {
                    _logger.LogInformation($"[{logId}] Phase 3: Creating {promotionData.Applications.Count} PTApplicationRequest records");
                    foreach (var appData in promotionData.Applications)
                    {
                        await CreatePTApplicationRequestAsync(promotionRequestDetail, appData, hrRequestDetail.CreatedBy);
                    }
                }

                if (promotionData.Folders?.Any() == true)
                {
                    _logger.LogInformation($"[{logId}] Phase 3: Creating {promotionData.Folders.Count} PTFolderRequest records");
                    foreach (var folderData in promotionData.Folders)
                    {
                        await CreatePTFolderRequestAsync(promotionRequestDetail, folderData, hrRequestDetail.CreatedBy);
                    }
                }

                if (promotionData.TabletProfiles?.Any() == true)
                {
                    _logger.LogInformation($"[{logId}] Phase 3: Creating {promotionData.TabletProfiles.Count} PTITTabletProfile records");
                    foreach (var tabletData in promotionData.TabletProfiles)
                    {
                        await CreatePTTabletProfileAsync(promotionRequestDetail, tabletData, hrRequestDetail.CreatedBy);
                    }
                }

                if (promotionData.ComputerRequirements?.Any() == true)
                {
                    _logger.LogInformation($"[{logId}] Phase 3: Creating {promotionData.ComputerRequirements.Count} PTITComputerRequirement records");
                    foreach (var computerData in promotionData.ComputerRequirements)
                    {
                        await CreatePTComputerRequirementAsync(promotionRequestDetail, computerData, hrRequestDetail.CreatedBy);
                    }
                }

                if (promotionData.BuildingAccess?.Any() == true)
                {
                    _logger.LogInformation($"[{logId}] Phase 3: Creating {promotionData.BuildingAccess.Count} PTBuildingAccessRequirement records");
                    foreach (var buildingAccessData in promotionData.BuildingAccess)
                    {
                        await CreatePTBuildingAccessAsync(promotionRequestDetail, buildingAccessData, hrRequestDetail.CreatedBy);
                    }
                }

                // Phase 4: Update Parent HRRequestDetail
                _logger.LogInformation($"[{logId}] Phase 4: Updating parent HRRequestDetail");

                hrRequestDetail.EmployeeNetworkId = currentNetworkId ?? $"";
                hrRequestDetail.EmployeePositionCode = promotionData.NewPositionCode;
                hrRequestDetail.EffectiveDate = promotionData.EffectiveDate;

                // Phase 5: Save All Changes
                _logger.LogInformation($"[{logId}] Phase 5: Saving all changes");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"[{logId}] Successfully created PromotionRequestDetail with ID: {promotionRequestDetail.Id}");

                // Log successful save to ECM
                _ecmLogger.LogSave(true, "PromotionRequest", promotionRequestDetail.Id, hrRequestDetail.CreatedBy.ToString());
                _ecmLogger.LogHRRequest(true, "Promotion/Transfer", "CREATE", promotionRequestDetail.Id, hrRequestDetail.CreatedBy.ToString());

                // Map to DTO
                var dto = MapToDto(promotionRequestDetail);
                return new ApiResponse<PromotionRequestDetailDto>
                {
                    Success = true,
                    Message = "Promotion request details created successfully",
                    Data = dto
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"[{logId}] Error in transaction: {ex.Message}");
                _logger.LogError($"[{logId}] Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"[{logId}] Inner Exception: {ex.InnerException.Message}");
                    _logger.LogError($"[{logId}] Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{logId}] CreatePromotionRequestDetailsAsync failed: {ex.Message}");
            if (ex.InnerException != null)
            {
                _logger.LogError($"[{logId}] Outer catch - Inner Exception: {ex.InnerException.Message}");
            }

            var errorMessage = $"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}";
            _ecmLogger.LogSave(false, "PromotionRequest", hrRequestDetailId, null, errorMessage);
            _ecmLogger.LogHRRequest(false, "Promotion/Transfer", "CREATE", hrRequestDetailId, null, errorMessage);

            return new ApiResponse<PromotionRequestDetailDto>
            {
                Success = false,
                Message = errorMessage
            };
        }
    }

    public async Task<ApiResponse<PromotionRequestDetailDto>> GetByHRRequestDetailIdAsync(int hrRequestDetailId)
    {
        try
        {
            var promotionDetail = await _context.PromotionRequestDetails
                .FirstOrDefaultAsync(x => x.Id == hrRequestDetailId);

            if (promotionDetail == null)
            {
                return new ApiResponse<PromotionRequestDetailDto>
                {
                    Success = false,
                    Message = "Promotion request detail not found"
                };
            }

            var dto = MapToDto(promotionDetail);
            return new ApiResponse<PromotionRequestDetailDto>
            {
                Success = true,
                Data = dto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetByHRRequestDetailIdAsync failed: {ex.Message}");
            return new ApiResponse<PromotionRequestDetailDto>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ApiResponse<PromotionRequestViewDto>> GetPromotionRequestViewByParentIdAsync(int parentRequestId)
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
                return new ApiResponse<PromotionRequestViewDto>
                {
                    Success = false,
                    Message = "HR request details not found"
                };
            }

            // Get promotion details with all related data
            var promotionDetail = await _context.PromotionRequestDetails
                .Include(p => p.HRRequestDetail)
                    .ThenInclude(hrd => hrd.ParentRequest)
                .Include(p => p.HRRequestDetail)
                    .ThenInclude(hrd => hrd.RequestStatus)
                .Where(p => p.RequestDetailId == hrRequestDetails.Id && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (promotionDetail == null)
            {
                return new ApiResponse<PromotionRequestViewDto>
                {
                    Success = false,
                    Message = "Promotion request details not found"
                };
            }

            // Execute database operations sequentially to avoid DbContext threading issues
            // Load all PT* child detail records
            var creditCard = await _context.PTCreditCardDetails
                .Where(c => c.PTRequestDetailId == promotionDetail.Id && !c.IsDeleted)
                .FirstOrDefaultAsync();
            var vehicle = await _context.PTVehicleDetails
                .Where(v => v.PTRequestDetailId == promotionDetail.Id && !v.IsDeleted)
                .FirstOrDefaultAsync();
            var it = await _context.PTITDetails
                .Where(i => i.PTRequestDetailId == promotionDetail.Id && !i.IsDeleted)
                .FirstOrDefaultAsync();
            var phone = await _context.PTITPhoneRequirements
                .Where(p => p.PTRequestDetailId == promotionDetail.Id && !p.IsDeleted)
                .FirstOrDefaultAsync();
            var applications = await _context.PTApplicationRequests
                .Include(a => a.Application)
                .Where(a => a.PTRequestDetailId == promotionDetail.Id && !a.IsDeleted)
                .ToListAsync();
            var folders = await _context.PTFolderRequests
                .Where(f => f.PTRequestDetailId == promotionDetail.Id && !f.IsDeleted)
                .ToListAsync();
            var tabletProfiles = await _context.PTITTabletProfiles
                .Include(t => t.TabletProfile)
                .Where(t => t.PTRequestDetailId == promotionDetail.Id && !t.IsDeleted)
                .ToListAsync();
            var computerRequirements = await _context.PTITComputerRequirements
                .Include(c => c.ComputerRequirement)
                .Where(c => c.PTRequestDetailId == promotionDetail.Id && !c.IsDeleted)
                .ToListAsync();
            var buildingAccess = await _context.PTBuildingAccessRequirements
                .Include(b => b.BuildingAccessRequirement)
                .Where(b => b.PTRequestDetailId == promotionDetail.Id && !b.IsDeleted)
                .ToListAsync();

            // Load reference data
            var employee = await _context.Employees
                .Where(e => e.EmployeeNumber == promotionDetail.HRRequestDetail.EmployeeId && !e.IsDeleted)
                .FirstOrDefaultAsync();
            var currentCompany = await _context.Companies
                .Where(c => c.CompanyCode == promotionDetail.CurrentPayrollCompanyCode)
                .FirstOrDefaultAsync();
            var newCompany = await _context.Companies
                .Where(c => c.CompanyCode == promotionDetail.NewPayrollCompanyCode)
                .FirstOrDefaultAsync();
            var currentLocation = await _context.PhysicalLocations
                .Where(l => l.LocationCode == promotionDetail.CurrentPhysicalLocationCode)
                .FirstOrDefaultAsync();
            var newLocation = await _context.PhysicalLocations
                .Where(l => l.LocationCode == promotionDetail.NewPhysicalLocationCode)
                .FirstOrDefaultAsync();
            var currentPosition = await _context.Positions
                .Where(p => p.PositionCode == promotionDetail.CurrentPositionCode)
                .FirstOrDefaultAsync();
            var newPosition = await _context.Positions
                .Where(p => p.PositionCode == promotionDetail.NewPositionCode)
                .FirstOrDefaultAsync();
            var currentPayrollDept = await _context.PayrollDepartments
                .Where(pd => pd.DeptCode == promotionDetail.CurrentPayrollDeptCode)
                .FirstOrDefaultAsync();
            var newPayrollDept = await _context.PayrollDepartments
                .Where(pd => pd.DeptCode == promotionDetail.NewPayrollDeptCode)
                .FirstOrDefaultAsync();
            var currentPayrollGroup = await _context.PayrollGroups
                .Where(pg => pg.CompanyCode == promotionDetail.CurrentPayrollCompanyCode
                    && pg.GroupCode == promotionDetail.CurrentPayrollGroupCode && !pg.IsDeleted)
                .FirstOrDefaultAsync();
            var newPayrollGroup = await _context.PayrollGroups
                .Where(pg => pg.CompanyCode == promotionDetail.NewPayrollCompanyCode
                    && pg.GroupCode == promotionDetail.NewPayrollGroupCode && !pg.IsDeleted)
                .FirstOrDefaultAsync();
            var currentSalaryType = await _context.EmployeeSalaryTypes
                .Where(st => st.CompanyCode == promotionDetail.CurrentPayrollCompanyCode
                    && st.SalaryCode == promotionDetail.CurrentSalaryCode && !st.IsDeleted)
                .FirstOrDefaultAsync();
            var newSalaryType = await _context.EmployeeSalaryTypes
                .Where(st => st.CompanyCode == promotionDetail.NewPayrollCompanyCode
                    && st.SalaryCode == promotionDetail.NewSalaryCode && !st.IsDeleted)
                .FirstOrDefaultAsync();

            // Load supervisor information
            var currentSupervisor = promotionDetail.CurrentSupervisorId.HasValue
                ? await _context.Employees
                    .Where(e => e.EmployeeNumber == promotionDetail.CurrentSupervisorId.Value && !e.IsDeleted)
                    .FirstOrDefaultAsync()
                : null;
            var newSupervisor = promotionDetail.NewSupervisorId.HasValue
                ? await _context.Employees
                    .Where(e => e.EmployeeNumber == promotionDetail.NewSupervisorId.Value && !e.IsDeleted)
                    .FirstOrDefaultAsync()
                : null;

            var view = MapToViewDto(
                promotionDetail,
                employee,
                creditCard,
                vehicle,
                it,
                phone,
                applications,
                folders,
                tabletProfiles,
                computerRequirements,
                buildingAccess,
                currentCompany,
                newCompany,
                currentLocation,
                newLocation,
                currentPosition,
                newPosition,
                currentPayrollDept,
                newPayrollDept,
                currentSupervisor,
                newSupervisor,
                currentPayrollGroup,
                newPayrollGroup,
                currentSalaryType,
                newSalaryType);

            return new ApiResponse<PromotionRequestViewDto>
            {
                Success = true,
                Data = view
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetPromotionRequestViewByParentIdAsync failed: {ex.Message}");
            return new ApiResponse<PromotionRequestViewDto>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ApiResponse<List<HRRequestDetailDto>>> SavePromotionRequestAsDraftAsync(CreatePromotionRequestDto request)
    {
        // TODO: Implement draft save
        throw new NotImplementedException();
    }

    public async Task<ApiResponse<List<HRRequestDetailDto>>> UpdatePromotionRequestAsDraftAsync(int parentRequestId, CreatePromotionRequestDto request)
    {
        // TODO: Implement draft update
        throw new NotImplementedException();
    }

    public async Task<ApiResponse<List<HRRequestDetailDto>>> UpdatePromotionRequestAsync(int parentRequestId, CreatePromotionRequestDto request)
    {
        // TODO: Implement submit update
        throw new NotImplementedException();
    }

    // ==================== Private Helper Methods ====================

    private async Task CreatePTCreditCardDetailAsync(PromotionRequestDetail promotionDetail, PromotionCreditCardInfoDto creditCardInfo, int createdBy)
    {
        var creditCard = new PTCreditCardDetail
        {
            PromotionRequestDetail = promotionDetail,
            KwikTripCard = creditCardInfo.KwikTripCard,
            CompanyExpenseCard = creditCardInfo.CompanyExpenseCard,
            CreditExpenseType = creditCardInfo.CreditExpenseType,
            WeeklyLimit = creditCardInfo.WeeklyLimit,
            FuelCardlockAccess = creditCardInfo.FuelCardlockAccess,
            FuelCardlockAddress = creditCardInfo.FuelCardlockAddress,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.PTCreditCardDetails.Add(creditCard);
        await Task.CompletedTask;
    }

    private async Task CreatePTVehicleDetailAsync(PromotionRequestDetail promotionDetail, PromotionVehicleInfoDto vehicleInfo, int createdBy)
    {
        var vehicle = new PTVehicleDetail
        {
            PromotionRequestDetail = promotionDetail,
            IsApprovedToOperate = vehicleInfo.IsApprovedToOperate,
            LicenseClass = vehicleInfo.LicenseClass,
            DrugAndAlcoholProfile = vehicleInfo.DrugAndAlcoholProfile,
            NeedCompanyCar = vehicleInfo.NeedCompanyCar,
            IsApplicationPart2Complete = vehicleInfo.IsApplicationPart2Complete,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.PTVehicleDetails.Add(vehicle);
        await Task.CompletedTask;
    }

    private async Task CreatePTITDetailAsync(PromotionRequestDetail promotionDetail, PromotionITInfoDto itInfo, int createdBy)
    {
        var itDetail = new PTITDetail
        {
            PromotionRequestDetail = promotionDetail,
            EmailRequired = itInfo.EmailRequired,
            AlternateDeliveryLocation = itInfo.AlternateDeliveryLocation,
            MSOfficeLicenseE5 = itInfo.MSOfficeLicenseE5,
            MSOfficeLicenseF3 = itInfo.MSOfficeLicenseF3,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.PTITDetails.Add(itDetail);
        await Task.CompletedTask;
    }

    private async Task CreatePTPhoneRequirementAsync(PromotionRequestDetail promotionDetail, PromotionPhoneRequirementDto phoneInfo, int createdBy)
    {
        var phone = new PTITPhoneRequirement
        {
            PromotionRequestDetail = promotionDetail,
            DeskPhone = phoneInfo.DeskPhone,
            CompanyCellphone = phoneInfo.CompanyCellphone,
            BYODCellphone = phoneInfo.BYODCellphone,
            WorkPhoneNumber = phoneInfo.WorkPhoneNumber,
            WorkExtension = phoneInfo.WorkExtension,
            WorkCell = phoneInfo.WorkCell,
            ReusingExistingPhone = phoneInfo.ReusingExistingPhone,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.PTITPhoneRequirements.Add(phone);
        await Task.CompletedTask;
    }

    private async Task CreatePTApplicationRequestAsync(PromotionRequestDetail promotionDetail, PromotionApplicationRequestDto appData, int createdBy)
    {
        var appRequest = new PTApplicationRequest
        {
            PromotionRequestDetail = promotionDetail,
            ApplicationId = appData.ApplicationId,
            AccessNotes = appData.AccessNotes,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.PTApplicationRequests.Add(appRequest);
        await Task.CompletedTask;
    }

    private async Task CreatePTFolderRequestAsync(PromotionRequestDetail promotionDetail, PromotionFolderRequestDto folderData, int createdBy)
    {
        var folderRequest = new PTFolderRequest
        {
            PromotionRequestDetail = promotionDetail,
            FolderType = folderData.FolderType,
            FolderName = folderData.FolderName,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.PTFolderRequests.Add(folderRequest);
        await Task.CompletedTask;
    }

    private async Task CreatePTTabletProfileAsync(PromotionRequestDetail promotionDetail, PromotionTabletProfileDto tabletData, int createdBy)
    {
        // Look up the profile name from the master TabletProfile table
        var masterProfile = await _context.TabletProfiles
            .FirstOrDefaultAsync(t => t.Id == tabletData.TabletProfileId && !t.IsDeleted);

        var tabletProfile = new PTITTabletProfile
        {
            PromotionRequestDetail = promotionDetail,
            TabletProfileId = tabletData.TabletProfileId,
            TabletProfileName = masterProfile?.ProfileName ?? tabletData.TabletProfileName,
            RolesRequiredForNewHire = tabletData.RolesRequiredForNewHire,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            ModifiedBy = createdBy,
            ModifiedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.PTITTabletProfiles.Add(tabletProfile);
        await Task.CompletedTask;
    }

    private async Task CreatePTComputerRequirementAsync(PromotionRequestDetail promotionDetail, PromotionComputerRequirementDto computerData, int createdBy)
    {
        // Look up the description from the master ComputerRequirement table
        var masterRequirement = await _context.ComputerRequirements
            .FirstOrDefaultAsync(c => c.Id == computerData.ComputerRequirementsId && !c.IsDeleted);

        var computerRequirement = new PTITComputerRequirement
        {
            PromotionRequestDetail = promotionDetail,
            ComputerRequirementsId = computerData.ComputerRequirementsId,
            ComputerRequirementsDescription = masterRequirement?.Description ?? computerData.ComputerRequirementsDescription,
            IsChild = computerData.IsChild ?? false,
            ParentId = computerData.ParentId,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.PTITComputerRequirements.Add(computerRequirement);
        await Task.CompletedTask;
    }

    private async Task CreatePTBuildingAccessAsync(PromotionRequestDetail promotionDetail, PromotionBuildingAccessDto buildingAccessData, int createdBy)
    {
        var buildingAccess = new PTBuildingAccessRequirement
        {
            PromotionRequestDetail = promotionDetail,
            AccessId = buildingAccessData.AccessId,
            AccessDescription = buildingAccessData.AccessDescription,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.PTBuildingAccessRequirements.Add(buildingAccess);
        await Task.CompletedTask;
    }

    // ==================== Mapping Methods ====================

    private PromotionRequestDetailDto MapToDto(PromotionRequestDetail entity)
    {
        return new PromotionRequestDetailDto
        {
            Id = entity.Id,
            CurrentPayrollCompanyCode = entity.CurrentPayrollCompanyCode,
            CurrentPayrollGroupCode = entity.CurrentPayrollGroupCode,
            CurrentPayrollDeptCode = entity.CurrentPayrollDeptCode,
            CurrentPositionCode = entity.CurrentPositionCode,
            CurrentSupervisorId = entity.CurrentSupervisorId,
            CurrentPhysicalLocationCode = entity.CurrentPhysicalLocationCode,
            CurrentStatus = entity.CurrentStatus,
            CurrentSalaryCode = entity.CurrentSalaryCode,
            CurrentWorkEmail = entity.CurrentWorkEmail,
            NewPayrollCompanyCode = entity.NewPayrollCompanyCode,
            NewPayrollGroupCode = entity.NewPayrollGroupCode,
            NewPayrollDeptCode = entity.NewPayrollDeptCode,
            NewPositionCode = entity.NewPositionCode,
            NewSupervisorId = entity.NewSupervisorId,
            NewPhysicalLocationCode = entity.NewPhysicalLocationCode,
            NewStatus = entity.NewStatus,
            NewSalaryCode = entity.NewSalaryCode,
            NewWorkEmail = entity.NewWorkEmail,
            UseExistingKeyFob = entity.UseExistingKeyFob
        };
    }

    private PromotionRequestViewDto MapToViewDto(
        PromotionRequestDetail promotionDetail,
        Employee? employee,
        PTCreditCardDetail? creditCard,
        PTVehicleDetail? vehicle,
        PTITDetail? it,
        PTITPhoneRequirement? phone,
        List<PTApplicationRequest> applications,
        List<PTFolderRequest> folders,
        List<PTITTabletProfile> tabletProfiles,
        List<PTITComputerRequirement> computerRequirements,
        List<PTBuildingAccessRequirement> buildingAccess,
        Company? currentCompany,
        Company? newCompany,
        PhysicalLocation? currentLocation,
        PhysicalLocation? newLocation,
        Position? currentPosition,
        Position? newPosition,
        PayrollDepartment? currentPayrollDept,
        PayrollDepartment? newPayrollDept,
        Employee? currentSupervisor,
        Employee? newSupervisor,
        PayrollGroup? currentPayrollGroup,
        PayrollGroup? newPayrollGroup,
        EmployeeSalaryType? currentSalaryType,
        EmployeeSalaryType? newSalaryType)
    {
        return new PromotionRequestViewDto
        {
            // HR Request Information
            ParentRequestId = promotionDetail.HRRequestDetail.ParentRequestId,
            RequestTitle = $"Promotion/Transfer Request - {promotionDetail.HRRequestDetail.EmployeeNetworkId ?? "Unknown"}",
            RequestDescription = $"Promotion/Transfer request for employee {promotionDetail.HRRequestDetail.EmployeeNetworkId ?? "Unknown"}",
            EffectiveDate = promotionDetail.HRRequestDetail.EffectiveDate,
            Notes = promotionDetail.HRRequestDetail.ParentRequest.Notes,
            CreatedDate = promotionDetail.HRRequestDetail.ParentRequest.CreatedDate,
            RequestStatusId = promotionDetail.HRRequestDetail.RequestStatusId,
            RequestStatusName = promotionDetail.HRRequestDetail.RequestStatus?.RequestStatusName ?? "Unknown",
            SubmittedByName = promotionDetail.HRRequestDetail.ParentRequest.SubmitterName ?? "Unknown User",

            // HR Request Detail Information
            RequestDetailId = promotionDetail.Id,
            EmployeeId = promotionDetail.HRRequestDetail.EmployeeId,
            EmployeeNetworkId = promotionDetail.HRRequestDetail.EmployeeNetworkId ?? string.Empty,
            EmployeePositionCode = promotionDetail.HRRequestDetail.EmployeePositionCode ?? string.Empty,
            EmployeeName = employee != null
                ? string.Join(" ", new[] { employee.FirstName, employee.MiddleName, employee.LastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s)))
                : promotionDetail.HRRequestDetail.EmployeeNetworkId ?? string.Empty,

            // Current Position Information with Display Names
            CurrentPayrollCompanyCode = promotionDetail.CurrentPayrollCompanyCode,
            CurrentCompanyName = currentCompany?.CompanyName,
            CurrentPayrollGroupCode = promotionDetail.CurrentPayrollGroupCode,
            CurrentPayrollGroupName = currentPayrollGroup?.GroupName,
            CurrentPayrollDeptCode = promotionDetail.CurrentPayrollDeptCode,
            CurrentPayrollDeptName = currentPayrollDept?.DeptName,
            CurrentPositionCode = promotionDetail.CurrentPositionCode,
            CurrentPositionName = currentPosition?.PositionName,
            CurrentSupervisorId = promotionDetail.CurrentSupervisorId,
            CurrentSupervisorName = currentSupervisor != null
                ? $"{currentSupervisor.FirstName} {currentSupervisor.LastName}".Trim()
                : null,
            CurrentPhysicalLocationCode = promotionDetail.CurrentPhysicalLocationCode,
            CurrentPhysicalLocationName = currentLocation?.LocationName,
            CurrentStatus = promotionDetail.CurrentStatus,
            CurrentSalaryCode = promotionDetail.CurrentSalaryCode,
            CurrentSalaryDescription = currentSalaryType?.Description,
            CurrentWorkEmail = promotionDetail.CurrentWorkEmail,

            // New Position Information with Display Names
            NewPayrollCompanyCode = promotionDetail.NewPayrollCompanyCode,
            NewCompanyName = newCompany?.CompanyName,
            NewPayrollGroupCode = promotionDetail.NewPayrollGroupCode,
            NewPayrollGroupName = newPayrollGroup?.GroupName,
            NewPayrollDeptCode = promotionDetail.NewPayrollDeptCode,
            NewPayrollDeptName = newPayrollDept?.DeptName,
            NewPositionCode = promotionDetail.NewPositionCode,
            NewPositionName = newPosition?.PositionName,
            NewSupervisorId = promotionDetail.NewSupervisorId,
            NewSupervisorName = newSupervisor != null
                ? $"{newSupervisor.FirstName} {newSupervisor.LastName}".Trim()
                : null,
            NewPhysicalLocationCode = promotionDetail.NewPhysicalLocationCode,
            NewPhysicalLocationName = newLocation?.LocationName,
            NewStatus = promotionDetail.NewStatus,
            NewSalaryCode = promotionDetail.NewSalaryCode,
            NewSalaryDescription = newSalaryType?.Description,
            NewWorkEmail = promotionDetail.NewWorkEmail,

            // Related Information
            CreditCardInfo = creditCard != null ? new PTCreditCardDetailViewDto
            {
                KwikTripCard = creditCard.KwikTripCard ?? false,
                CompanyExpenseCard = creditCard.CompanyExpenseCard ?? false,
                CreditExpenseType = creditCard.CreditExpenseType,
                WeeklyLimit = creditCard.WeeklyLimit,
                FuelCardlockAccess = creditCard.FuelCardlockAccess ?? false,
                FuelCardlockAddress = creditCard.FuelCardlockAddress
            } : null,

            VehicleInfo = vehicle != null ? new PTVehicleDetailViewDto
            {
                IsApprovedToOperate = vehicle.IsApprovedToOperate ?? false,
                LicenseClass = vehicle.LicenseClass,
                DrugAndAlcoholProfile = vehicle.DrugAndAlcoholProfile,
                NeedCompanyCar = vehicle.NeedCompanyCar ?? false,
                IsApplicationPart2Complete = vehicle.IsApplicationPart2Complete ?? false
            } : null,

            ITInfo = it != null ? new PTITDetailViewDto
            {
                EmailRequired = it.EmailRequired ?? false,
                AlternateDeliveryLocation = it.AlternateDeliveryLocation,
                MSOfficeLicenseE5 = it.MSOfficeLicenseE5 ?? false,
                MSOfficeLicenseF3 = it.MSOfficeLicenseF3 ?? false
            } : null,

            PhoneInfo = phone != null ? new PTITPhoneRequirementViewDto
            {
                DeskPhone = phone.DeskPhone ?? false,
                CompanyCellphone = phone.CompanyCellphone ?? false,
                BYODCellphone = phone.BYODCellphone ?? false,
                WorkPhoneNumber = phone.WorkPhoneNumber,
                WorkExtension = phone.WorkExtension,
                WorkCell = phone.WorkCell,
                ReusingExistingPhone = phone.ReusingExistingPhone ?? false
            } : null,

            Applications = applications.Select(a => new PTApplicationRequestViewDto
            {
                ApplicationId = a.ApplicationId,
                ApplicationName = a.Application?.Name ?? "Unknown Application",
                AccessNotes = a.AccessNotes
            }).ToList(),

            Folders = folders.Select(f => new PTFolderRequestViewDto
            {
                FolderType = f.FolderType ?? string.Empty,
                FolderName = f.FolderName ?? string.Empty
            }).ToList(),

            TabletProfiles = tabletProfiles.Select(t => new PTITTabletProfileViewDto
            {
                TabletProfileId = t.TabletProfileId,
                TabletProfileName = t.TabletProfile?.ProfileName ?? t.TabletProfileName ?? "Unknown Profile",
                RolesRequiredForNewHire = t.RolesRequiredForNewHire
            }).ToList(),

            ComputerRequirements = computerRequirements.Select(c => new PTITComputerRequirementViewDto
            {
                ComputerRequirementsId = c.ComputerRequirementsId,
                ComputerRequirementsDescription = c.ComputerRequirement?.Description ?? c.ComputerRequirementsDescription ?? "Unknown Requirement",
                IsChild = c.IsChild,
                ParentId = c.ParentId
            }).ToList(),

            BuildingAccess = buildingAccess.Select(b => new PTBuildingAccessViewDto
            {
                AccessId = b.AccessId,
                AccessDescription = b.BuildingAccessRequirement?.Description ?? b.AccessDescription ?? "Unknown Access"
            }).ToList(),

            UseExistingKeyFob = promotionDetail.UseExistingKeyFob
        };
    }
}

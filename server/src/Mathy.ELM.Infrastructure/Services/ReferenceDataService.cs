using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Data;
using Mathy.ELM.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using Hangfire;

namespace Mathy.ELM.Infrastructure.Services;

public class ReferenceDataService : IReferenceDataService
{
    private readonly MathyELMContext _context;
    private readonly ILogger<ReferenceDataService> _logger;
    private readonly IViewpointService _viewpointService;
    private readonly IRoleFilterService _roleFilterService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserContextService _userContextService;
    private readonly IEcmLogger _ecmLogger;

    public ReferenceDataService(
        MathyELMContext context,
        ILogger<ReferenceDataService> logger,
        IViewpointService viewpointService,
        IRoleFilterService roleFilterService,
        IHttpContextAccessor httpContextAccessor,
        IUserContextService userContextService,
        IEcmLogger ecmLogger)
    {
        _context = context;
        _logger = logger;
        _viewpointService = viewpointService;
        _roleFilterService = roleFilterService;
        _httpContextAccessor = httpContextAccessor;
        _userContextService = userContextService;
        _ecmLogger = ecmLogger;
    }

    public async Task<ApiResponse<List<RequestTypeDto>>> GetRequestTypesAsync(int? requestTypeId = null, string? requestTypeName = null)
    {
        try
        {
            var query = _context.RequestTypes
                .Where(rt => rt.IsActive && !rt.IsDeleted);

            if (requestTypeId.HasValue)
            {
                query = query.Where(rt => rt.Id == requestTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(requestTypeName))
            {
                query = query.Where(rt => rt.RequestTypeName.ToLower().Contains(requestTypeName.ToLower()));
            }

            var requestTypes = await query
                .Select(rt => new RequestTypeDto
                {
                    Id = rt.Id,
                    RequestTypeName = rt.RequestTypeName,
                    RequestTypeDescription = rt.RequestTypeDescription,
                    IsActive = rt.IsActive,
                    CreatedBy = rt.CreatedBy,
                    CreatedDate = rt.CreatedDate,
                    ModifiedBy = rt.ModifiedBy,
                    ModifiedDate = rt.ModifiedDate
                })
                .OrderBy(rt => rt.RequestTypeName)
                .ToListAsync();

            return new ApiResponse<List<RequestTypeDto>>
            {
                Success = true,
                Data = requestTypes,
                Message = $"Retrieved {requestTypes.Count} request types"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving request types with filters: RequestTypeId={RequestTypeId}, RequestTypeName={RequestTypeName}", 
                requestTypeId, requestTypeName);
            
            return new ApiResponse<List<RequestTypeDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving request types",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<RequestStatusDto>>> GetRequestStatusesAsync(int? requestStatusId = null, string? requestStatusName = null)
    {
        try
        {
            var query = _context.RequestStatuses
                .Where(rs => rs.IsActive && !rs.IsDeleted);

            if (requestStatusId.HasValue)
            {
                query = query.Where(rs => rs.Id == requestStatusId.Value);
            }

            if (!string.IsNullOrWhiteSpace(requestStatusName))
            {
                query = query.Where(rs => rs.RequestStatusName.ToLower().Contains(requestStatusName.ToLower()));
            }

            var requestStatuses = await query
                .Select(rs => new RequestStatusDto
                {
                    Id = rs.Id,
                    RequestStatusName = rs.RequestStatusName,
                    RequestStatusDescription = rs.RequestStatusDescription,
                    IsActive = rs.IsActive,
                    CreatedBy = rs.CreatedBy,
                    CreatedDate = rs.CreatedDate,
                    ModifiedBy = rs.ModifiedBy,
                    ModifiedDate = rs.ModifiedDate
                })
                .OrderBy(rs => rs.RequestStatusName)
                .ToListAsync();

            return new ApiResponse<List<RequestStatusDto>>
            {
                Success = true,
                Data = requestStatuses,
                Message = $"Retrieved {requestStatuses.Count} request statuses"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving request statuses with filters: RequestStatusId={RequestStatusId}, RequestStatusName={RequestStatusName}", 
                requestStatusId, requestStatusName);
            
            return new ApiResponse<List<RequestStatusDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving request statuses",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<TerminationReasonDto>>> GetTerminationReasonsAsync(int? reasonId = null, string? reasonCode = null, int? companyCode = null)
    {
        try
        {
            // ===================================================================================
            // PREVIOUS IMPLEMENTATION - Used TerminationReasons table
            // Commented out because termination reasons are now sourced from EmploymentStatuses table
            // where Notes column contains 'TERM' or 'CHANGE' to indicate termination-related statuses.
            // The EmploymentStatuses table is synced from Viewpoint and contains the actual
            // termination reason codes used in the HR system.
            // ===================================================================================
            // var query = _context.TerminationReasons
            //     .Where(tr => tr.IsActive && !tr.IsDeleted);
            //
            // if (reasonId.HasValue)
            // {
            //     query = query.Where(tr => tr.Id == reasonId.Value);
            // }
            //
            // if (!string.IsNullOrWhiteSpace(reasonCode))
            // {
            //     query = query.Where(tr => tr.ReasonCode.ToLower().Contains(reasonCode.ToLower()));
            // }
            //
            // if (companyCode.HasValue)
            // {
            //     query = query.Where(tr => tr.CompanyCode == companyCode.Value);
            // }
            //
            // var terminationReasons = await query
            //     .Select(tr => new TerminationReasonDto
            //     {
            //         Id = tr.Id,
            //         ReasonCode = tr.ReasonCode,
            //         ReasonDescription = tr.ReasonDescription,
            //         CompanyCode = tr.CompanyCode,
            //         IsActive = tr.IsActive,
            //         CreatedBy = tr.CreatedBy,
            //         CreatedDate = tr.CreatedDate,
            //         ModifiedBy = tr.ModifiedBy,
            //         ModifiedDate = tr.ModifiedDate
            //     })
            //     .OrderBy(tr => tr.ReasonDescription)
            //     .ToListAsync();
            // ===================================================================================

            // Termination reasons are Viewpoint codes with Type="N" (e.g. DECEASED, DISABLED,
            // VOLUNTARY, INVOLUNTARY, TRANSFER). Synced into CodeType.
            var query = _context.EmploymentStatuses
                .Where(es => es.IsActive && !es.IsDeleted && es.CodeType == "N");

            if (reasonId.HasValue)
            {
                query = query.Where(es => es.Id == reasonId.Value);
            }

            if (!string.IsNullOrWhiteSpace(reasonCode))
            {
                query = query.Where(es => es.Status.ToLower().Contains(reasonCode.ToLower()));
            }

            if (companyCode.HasValue)
            {
                query = query.Where(es => es.CompanyCode == companyCode.Value);
            }

            var terminationReasons = await query
                .Select(es => new TerminationReasonDto
                {
                    Notes = es.Notes,
                    Id = es.Id,
                    ReasonCode = es.Status,           // Status column maps to ReasonCode (e.g., 'TERM', 'TRANSFER', 'S-RETIRED')
                    ReasonDescription = es.Description, // Description column maps to ReasonDescription
                    CompanyCode = es.CompanyCode,
                    IsActive = es.IsActive,
                    CreatedBy = es.CreatedBy,
                    CreatedDate = es.CreatedDate,
                    ModifiedBy = es.ModifiedBy,
                    ModifiedDate = es.ModifiedDate
                })
                .OrderBy(tr => tr.ReasonDescription)
                .ToListAsync();

            return new ApiResponse<List<TerminationReasonDto>>
            {
                Success = true,
                Data = terminationReasons,
                Message = $"Retrieved {terminationReasons.Count} termination reasons"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving termination reasons with filters: ReasonId={ReasonId}, ReasonCode={ReasonCode}",
                reasonId, reasonCode);

            return new ApiResponse<List<TerminationReasonDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving termination reasons",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<PayrollDepartmentDto>>> GetPayrollDepartmentsAsync(int? companyCode = null, int? deptCode = null)
    {
        try
        {
            var userId = _userContextService.GetUserId();
            var isAdmin = _roleFilterService.IsSystemAdminOrEcmAdmin();

            // Check if user has selected a specific role via X-Selected-Role header
            var httpContext = _httpContextAccessor.HttpContext;
            var selectedRolesHeader = httpContext?.Request.Headers["X-Selected-Role"].FirstOrDefault();
            var hasSelectedRole = !string.IsNullOrEmpty(selectedRolesHeader);

            _logger.LogInformation("=== PAYROLL DEPARTMENTS API DEBUG ===");
            _logger.LogInformation("User ID: {UserId}", userId);
            _logger.LogInformation("Is Admin (SystemAdmin/HRAdmin/ECM_ADMIN): {IsAdmin}", isAdmin);
            _logger.LogInformation("Selected Role Header: {SelectedRole}", selectedRolesHeader ?? "None");
            _logger.LogInformation("Has Selected Role: {HasSelectedRole}", hasSelectedRole);
            _logger.LogInformation("==============================");

            // Start with basic payroll department query
            var query = _context.PayrollDepartments
                .Where(pd => !pd.IsDeleted && pd.IsActive);

            // If user has selected a specific role, use role-based filtering even for admins
            if (hasSelectedRole)
            {
                // Parse selected roles (can be comma-separated)
                var selectedRoles = selectedRolesHeader!.Split(',').Select(r => r.Trim()).Where(r => !string.IsNullOrEmpty(r)).ToList();

                _logger.LogInformation("Using selected role filtering with roles: {SelectedRoles}", string.Join(", ", selectedRoles));

                if (selectedRoles.Any())
                {
                    // Check if ECM_ADMIN is in the selected roles
                    if (selectedRoles.Contains("ECM_ADMIN"))
                    {
                        // ECM_ADMIN gets all active payroll departments - no filtering needed
                        _logger.LogInformation("ECM_ADMIN role detected in selected roles - returning all active payroll departments");
                        // query remains unfiltered (all active payroll departments)
                    }
                    else
                    {
                        // Filter by matching PayrollDepartmentShortNames for non-ECM_ADMIN roles
                        var payrollDeptFilters = await _context.PayrollDepartmentShortNames
                            .Where(p => !p.IsDeleted && selectedRoles.Contains(p.DeptShortName))
                            .Select(p => new { p.CompanyCode, p.DeptCode })
                            .Distinct()
                            .ToListAsync();

                        _logger.LogInformation("Selected role filtering (non-ECM_ADMIN) - found {Count} company/dept combinations",
                            payrollDeptFilters.Count);

                        // Build a list of "CompanyCode_DeptCode" strings for comparison
                        var companyDeptKeys = payrollDeptFilters.Select(p => $"{p.CompanyCode}_{p.DeptCode}").ToList();

                        // Filter payroll departments using string concatenation (EF Core can translate this)
                        query = query.Where(pd => companyDeptKeys.Contains(pd.CompanyCode.ToString() + "_" + pd.DeptCode.ToString()));
                    }
                }
                else
                {
                    // No valid selected roles, return empty list
                    _logger.LogInformation("No valid selected roles, returning empty payroll department list");
                    query = query.Where(pd => false);
                }
            }
            // If no selected role, check if user is admin
            else if (isAdmin)
            {
                _logger.LogInformation("Admin user with no selected role - returning all active payroll departments");
            }
            // Non-admin users without selected role - use their default roles
            else
            {
                var userRoles = _userContextService.GetUserRoles();

                if (userRoles.Any())
                {
                    // Filter by matching PayrollDepartmentShortNames for user's roles
                    var payrollDeptFilters = await _context.PayrollDepartmentShortNames
                        .Where(p => !p.IsDeleted && userRoles.Contains(p.DeptShortName))
                        .Select(p => new { p.CompanyCode, p.DeptCode })
                        .Distinct()
                        .ToListAsync();

                    _logger.LogInformation("Non-admin user - filtering by {Count} company/dept combinations",
                        payrollDeptFilters.Count);

                    // Build a list of "CompanyCode_DeptCode" strings for comparison
                    var companyDeptKeys = payrollDeptFilters.Select(p => $"{p.CompanyCode}_{p.DeptCode}").ToList();

                    // Filter payroll departments using string concatenation (EF Core can translate this)
                    query = query.Where(pd => companyDeptKeys.Contains(pd.CompanyCode.ToString() + "_" + pd.DeptCode.ToString()));
                }
                else
                {
                    // User has no roles, return empty list
                    _logger.LogInformation("User has no department roles, returning empty payroll department list");
                    query = query.Where(pd => false);
                }
            }

            // Apply additional query parameter filters
            if (companyCode.HasValue)
            {
                query = query.Where(pd => pd.CompanyCode == companyCode.Value);
            }

            if (deptCode.HasValue)
            {
                query = query.Where(pd => pd.DeptCode == deptCode.Value);
            }

            var payrollDepartments = await query
                .Select(pd => new PayrollDepartmentDto
                {
                    Id = pd.Id,
                    CompanyCode = pd.CompanyCode,
                    DeptCode = pd.DeptCode,
                    DeptName = pd.DeptName,
                    EmailDomain = pd.EmailDomain,
                    HRPartner = pd.HRPartner,
                    HRRep = pd.HRRep,
                    SafetyRep = pd.SafetyRep,
                    PayrollRep = pd.PayrollRep,
                    CreatedBy = pd.CreatedBy,
                    CreatedDate = pd.CreatedDate,
                    ModifiedBy = pd.ModifiedBy,
                    ModifiedDate = pd.ModifiedDate
                })
                .OrderBy(pd => pd.CompanyCode).ThenBy(pd => pd.DeptCode)
                .ToListAsync();

            return new ApiResponse<List<PayrollDepartmentDto>>
            {
                Success = true,
                Data = payrollDepartments,
                Message = $"Retrieved {payrollDepartments.Count} payroll departments"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll departments with filters: CompanyCode={CompanyCode}, DeptCode={DeptCode}",
                companyCode, deptCode);

            return new ApiResponse<List<PayrollDepartmentDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll departments",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<PayrollDepartmentShortNameDto>>> GetPayrollDepartmentShortNamesAsync(int? companyCode = null, int? deptCode = null, string? deptShortName = null)
    {
        try
        {
            var query = _context.PayrollDepartmentShortNames
                .Where(pdsh => !pdsh.IsDeleted);

            // Apply role-based filtering
            var (filterCompanyCode, filterDeptCode) = _roleFilterService.GetCompanyAndDeptFromSelectedRole();
            
            if (filterCompanyCode.HasValue && filterCompanyCode.Value > 0)
            {
                query = query.Where(pdsh => pdsh.CompanyCode == filterCompanyCode.Value);
            }

            // Apply query parameter filters
            if (companyCode.HasValue)
            {
                query = query.Where(pdsh => pdsh.CompanyCode == companyCode.Value);
            }

            if (deptCode.HasValue)
            {
                query = query.Where(pdsh => pdsh.DeptCode == deptCode.Value);
            }

            if (!string.IsNullOrEmpty(deptShortName))
            {
                query = query.Where(pdsh => pdsh.DeptShortName.Contains(deptShortName));
            }

            var payrollDepartmentShortNames = await query
                .Select(pdsh => new PayrollDepartmentShortNameDto
                {
                    Id = pdsh.Id,
                    CompanyCode = pdsh.CompanyCode,
                    DeptCode = pdsh.DeptCode,
                    DeptShortName = pdsh.DeptShortName,
                    CreatedBy = pdsh.CreatedBy,
                    CreatedDate = pdsh.CreatedDate,
                    ModifiedBy = pdsh.ModifiedBy,
                    ModifiedDate = pdsh.ModifiedDate
                })
                .OrderBy(pdsh => pdsh.CompanyCode).ThenBy(pdsh => pdsh.DeptCode)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} payroll department short names with filters: CompanyCode={CompanyCode}, DeptCode={DeptCode}, DeptShortName={DeptShortName}", 
                payrollDepartmentShortNames.Count, companyCode, deptCode, deptShortName);

            return new ApiResponse<List<PayrollDepartmentShortNameDto>>
            {
                Success = true,
                Data = payrollDepartmentShortNames,
                Message = $"Retrieved {payrollDepartmentShortNames.Count} payroll department short names"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll department short names with filters: CompanyCode={CompanyCode}, DeptCode={DeptCode}, DeptShortName={DeptShortName}", 
                companyCode, deptCode, deptShortName);
            
            return new ApiResponse<List<PayrollDepartmentShortNameDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll department short names",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<PayrollGroupDto>>> GetPayrollGroupsAsync(int? companyCode = null, int? groupCode = null)
    {
        try
        {
            var query = _context.PayrollGroups
                .Where(pg => !pg.IsDeleted && pg.IsActive);

            // Apply role-based filtering
            var (filterCompanyCode, _) = _roleFilterService.GetCompanyAndDeptFromSelectedRole();

            if (filterCompanyCode.HasValue && filterCompanyCode.Value > 0)
            {
                query = query.Where(pg => pg.CompanyCode == filterCompanyCode.Value);
            }

            // Apply query parameter filters
            if (companyCode.HasValue)
            {
                query = query.Where(pg => pg.CompanyCode == companyCode.Value);
            }

            if (groupCode.HasValue)
            {
                query = query.Where(pg => pg.GroupCode == groupCode.Value);
            }

            var payrollGroups = await query
                .Select(pg => new PayrollGroupDto
                {
                    Id = pg.Id,
                    CompanyCode = pg.CompanyCode,
                    GroupCode = pg.GroupCode,
                    GroupName = pg.GroupName,
                    IsActive = pg.IsActive
                })
                .OrderBy(pg => pg.CompanyCode).ThenBy(pg => pg.GroupCode)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} payroll groups with filters: CompanyCode={CompanyCode}, GroupCode={GroupCode}",
                payrollGroups.Count, companyCode, groupCode);

            return new ApiResponse<List<PayrollGroupDto>>
            {
                Success = true,
                Data = payrollGroups,
                Message = $"Retrieved {payrollGroups.Count} payroll groups"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll groups with filters: CompanyCode={CompanyCode}, GroupCode={GroupCode}",
                companyCode, groupCode);

            return new ApiResponse<List<PayrollGroupDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll groups",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<CompanyDto>>> GetCompaniesAsync(int? companyId = null, int? companyCode = null, string? companyName = null)
    {
        try
        {
            var userId = _userContextService.GetUserId();
            var isAdmin = _roleFilterService.IsSystemAdminOrEcmAdmin();
            
            // Check if user has selected a specific role via X-Selected-Role header
            var httpContext = _httpContextAccessor.HttpContext;
            var selectedRolesHeader = httpContext?.Request.Headers["X-Selected-Role"].FirstOrDefault();
            var hasSelectedRole = !string.IsNullOrEmpty(selectedRolesHeader);
            
            _logger.LogInformation("=== COMPANIES API DEBUG ===");
            _logger.LogInformation("User ID: {UserId}", userId);
            _logger.LogInformation("Is Admin (SystemAdmin/HRAdmin/ECM_ADMIN): {IsAdmin}", isAdmin);
            _logger.LogInformation("Selected Role Header: {SelectedRole}", selectedRolesHeader ?? "None");
            _logger.LogInformation("Has Selected Role: {HasSelectedRole}", hasSelectedRole);
            _logger.LogInformation("==============================");

            // Start with basic company query
            var companyQuery = _context.Companies.Where(c => c.IsActive && !c.IsDeleted);

            // If user has selected a specific role, use role-based filtering even for admins
            if (hasSelectedRole)
            {
                // Parse selected roles (can be comma-separated)
                var selectedRoles = selectedRolesHeader!.Split(',').Select(r => r.Trim()).Where(r => !string.IsNullOrEmpty(r)).ToList();
                
                _logger.LogInformation("Using selected role filtering with roles: {SelectedRoles}", string.Join(", ", selectedRoles));
                
                if (selectedRoles.Any())
                {
                    // Check if ECM_ADMIN is in the selected roles
                    if (selectedRoles.Contains("ECM_ADMIN"))
                    {
                        // ECM_ADMIN gets all active companies - no filtering needed
                        _logger.LogInformation("ECM_ADMIN role detected in selected roles - returning all active companies");
                        // companyQuery remains unfiltered (all active companies)
                    }
                    else
                    {
                        // Filter PayrollDepartmentShortNames by selected roles (for non-ECM_ADMIN roles)
                        var payrollCompanyCodes = await _context.PayrollDepartmentShortNames
                            .Where(p => !p.IsDeleted && selectedRoles.Contains(p.DeptShortName))
                            .Select(p => p.CompanyCode)
                            .Distinct()
                            .ToListAsync();

                        _logger.LogInformation("Selected role filtering (non-ECM_ADMIN) - found {Count} company codes: {CompanyCodes}", 
                            payrollCompanyCodes.Count, string.Join(", ", payrollCompanyCodes));

                        // Filter companies by the payroll department company codes
                        companyQuery = companyQuery.Where(c => payrollCompanyCodes.Contains(c.CompanyCode));
                    }
                }
                else
                {
                    // No valid selected roles, return empty list
                    _logger.LogInformation("No valid selected roles, returning empty company list");
                    companyQuery = companyQuery.Where(c => false);
                }
            }
            // If no selected role, check if user is admin
            else if (isAdmin)
            {
                _logger.LogInformation("Admin user with no selected role - returning all active companies");
            }
            // Non-admin users without selected role - use their default roles
            else
            {
                var userRoles = _userContextService.GetUserRoles();
                
                if (userRoles.Any())
                {
                    // Filter PayrollDepartmentShortNames by user roles (DeptShortName)
                    var payrollCompanyCodes = await _context.PayrollDepartmentShortNames
                        .Where(p => !p.IsDeleted && userRoles.Contains(p.DeptShortName))
                        .Select(p => p.CompanyCode)
                        .Distinct()
                        .ToListAsync();

                    _logger.LogInformation("Non-admin user - filtering by {Count} company codes: {CompanyCodes}", 
                        payrollCompanyCodes.Count, string.Join(", ", payrollCompanyCodes));

                    // Filter companies by the payroll department company codes
                    companyQuery = companyQuery.Where(c => payrollCompanyCodes.Contains(c.CompanyCode));
                }
                else
                {
                    // User has no roles, return empty list
                    _logger.LogInformation("User has no department roles, returning empty company list");
                    companyQuery = companyQuery.Where(c => false);
                }
            }

            if (companyId.HasValue)
            {
                companyQuery = companyQuery.Where(c => c.Id == companyId.Value);
            }

            if (companyCode.HasValue)
            {
                companyQuery = companyQuery.Where(c => c.CompanyCode == companyCode.Value);
            }

            if (!string.IsNullOrWhiteSpace(companyName))
            {
                companyQuery = companyQuery.Where(c => c.CompanyName.ToLower().Contains(companyName.ToLower()));
            }

            var companies = await companyQuery
                .OrderBy(c => c.CompanyCode)
                .Select(c => new CompanyDto
                {
                    Id = c.Id,
                    CompanyCode = c.CompanyCode,
                    CompanyName = c.CompanyName,
                    IsActive = c.IsActive,
                    ViewpointSyncDate = c.ViewpointSyncDate
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} companies with filters: CompanyId={CompanyId}, CompanyCode={CompanyCode}, CompanyName={CompanyName}", 
                companies.Count, companyId, companyCode, companyName);

            return new ApiResponse<List<CompanyDto>>
            {
                Success = true,
                Data = companies,
                Message = $"Retrieved {companies.Count} companies successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving companies with filters: CompanyId={CompanyId}, CompanyCode={CompanyCode}, CompanyName={CompanyName}", 
                companyId, companyCode, companyName);

            return new ApiResponse<List<CompanyDto>>
            {
                Success = false,
                Data = new List<CompanyDto>(),
                Message = "Error retrieving companies",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<PhysicalLocationDto>>> GetPhysicalLocationsAsync(int? locationId = null, int? locationCode = null, string? locationName = null)
    {
        try
        {
            var query = _context.PhysicalLocations
                .Where(pl => pl.IsActive && !pl.IsDeleted);

            if (locationId.HasValue)
            {
                query = query.Where(pl => pl.Id == locationId.Value);
            }

            if (locationCode.HasValue)
            {
                query = query.Where(pl => pl.LocationCode == locationCode.Value);
            }

            if (!string.IsNullOrWhiteSpace(locationName))
            {
                query = query.Where(pl => pl.LocationName.ToLower().Contains(locationName.ToLower()));
            }

            var physicalLocations = await query
                .OrderBy(pl => pl.LocationCode)
                .Select(pl => new PhysicalLocationDto
                {
                    Id = pl.Id,
                    LocationCode = pl.LocationCode,
                    LocationName = pl.LocationName,
                    IsActive = pl.IsActive,
                    ViewpointSyncDate = pl.ViewpointSyncDate
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} physical locations with filters: LocationId={LocationId}, LocationCode={LocationCode}, LocationName={LocationName}", 
                physicalLocations.Count, locationId, locationCode, locationName);

            return new ApiResponse<List<PhysicalLocationDto>>
            {
                Success = true,
                Data = physicalLocations,
                Message = $"Retrieved {physicalLocations.Count} physical locations successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving physical locations with filters: LocationId={LocationId}, LocationCode={LocationCode}, LocationName={LocationName}", 
                locationId, locationCode, locationName);

            return new ApiResponse<List<PhysicalLocationDto>>
            {
                Success = false,
                Data = new List<PhysicalLocationDto>(),
                Message = "Error retrieving physical locations",
                Errors = [ex.Message]
            };
        }
    }


    public async Task<ApiResponse<CompanySyncResultDto>> SyncCompaniesFromViewpointAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var syncResult = new CompanySyncResultDto
        {
            SyncDate = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting company sync from Viewpoint API");

            // Get all companies from Viewpoint
            var viewpointCompanies = await _viewpointService.GetAllCompaniesAsync();
            if (viewpointCompanies == null || !viewpointCompanies.Any())
            {
                _logger.LogWarning("No companies retrieved from Viewpoint API");
                syncResult.Errors.Add("No companies retrieved from Viewpoint API");
                _ecmLogger.LogReferenceDataSync(false, "Companies", 0, 0, 0, "No companies retrieved from Viewpoint API");
                return new ApiResponse<CompanySyncResultDto>
                {
                    Success = false,
                    Data = syncResult,
                    Message = "No companies retrieved from Viewpoint API"
                };
            }

            syncResult.TotalViewpointCompanies = viewpointCompanies.Count;
            _logger.LogInformation("Retrieved {Count} companies from Viewpoint API", viewpointCompanies.Count);

            // Get all existing companies from local database
            var existingCompanies = await _context.Companies
                .Where(c => !c.IsDeleted)
                .ToListAsync();

            var existingCompanyDict = existingCompanies.ToDictionary(c => c.CompanyCode, c => c);
            var processedCompanyCodes = new HashSet<int>();

            // Process each Viewpoint company
            foreach (var viewpointCompany in viewpointCompanies)
            {
                try
                {
                    // Skip if company code is missing
                    if (!viewpointCompany.HQCo.HasValue)
                    {
                        _logger.LogWarning("Skipping company with missing HQCo: {CompanyName}", viewpointCompany.Name);
                        syncResult.Errors.Add($"Skipping company with missing HQCo: {viewpointCompany.Name}");
                        continue;
                    }

                    var companyCode = viewpointCompany.HQCo.Value;
                    processedCompanyCodes.Add(companyCode);

                    var companyName = !string.IsNullOrWhiteSpace(viewpointCompany.Name) 
                        ? viewpointCompany.Name.Trim() 
                        : $"Company {companyCode}";

                    if (existingCompanyDict.TryGetValue(companyCode, out var existingCompany))
                    {
                        // Update existing company
                        var hasChanges = false;

                        if (existingCompany.CompanyName != companyName)
                        {
                            existingCompany.CompanyName = companyName;
                            hasChanges = true;
                        }

                        if (!existingCompany.IsActive)
                        {
                            existingCompany.IsActive = true;
                            hasChanges = true;
                        }

                        if (hasChanges)
                        {
                            existingCompany.ViewpointSyncDate = DateTime.UtcNow;
                            existingCompany.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                            existingCompany.ModifiedDate = DateTime.UtcNow;

                            syncResult.ExistingCompaniesUpdated++;
                            _logger.LogDebug("Updated company {CompanyCode}: {CompanyName}", companyCode, companyName);
                        }
                        else
                        {
                            // Just update sync date even if no changes
                            existingCompany.ViewpointSyncDate = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Add new company
                        var currentUserId = _userContextService.GetUserEmployeeNumber();
                        var newCompany = new Company
                        {
                            CompanyCode = companyCode,
                            CompanyName = companyName,
                            IsActive = true,
                            ViewpointSyncDate = DateTime.UtcNow,
                            CreatedBy = currentUserId,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedBy = currentUserId,
                            ModifiedDate = DateTime.UtcNow
                        };

                        _context.Companies.Add(newCompany);
                        syncResult.NewCompaniesAdded++;
                        _logger.LogDebug("Added new company {CompanyCode}: {CompanyName}", companyCode, companyName);
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error processing company {viewpointCompany.HQCo} ({viewpointCompany.Name}): {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    syncResult.Errors.Add(errorMsg);
                }
            }

            // Deactivate companies that are no longer in Viewpoint
            foreach (var existingCompany in existingCompanies)
            {
                if (!processedCompanyCodes.Contains(existingCompany.CompanyCode) && existingCompany.IsActive)
                {
                    existingCompany.IsActive = false;
                    existingCompany.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                    existingCompany.ModifiedDate = DateTime.UtcNow;

                    syncResult.CompaniesDeactivated++;
                    _logger.LogInformation("Deactivated company {CompanyCode}: {CompanyName} (no longer in Viewpoint)",
                        existingCompany.CompanyCode, existingCompany.CompanyName);
                }
            }

            // Save all changes
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;

            _logger.LogInformation("Company sync completed successfully. {Summary}. Duration: {Duration}ms",
                syncResult.Summary, syncResult.SyncDuration.TotalMilliseconds);

            _ecmLogger.LogReferenceDataSync(syncResult.Success, "Companies",
                syncResult.NewCompaniesAdded, syncResult.ExistingCompaniesUpdated, 0, null);

            return new ApiResponse<CompanySyncResultDto>
            {
                Success = syncResult.Success,
                Data = syncResult,
                Message = syncResult.Success ? syncResult.Summary : "Company sync completed with errors",
                Errors = syncResult.Errors
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;
            syncResult.Errors.Add($"Sync failed: {ex.Message}");

            _logger.LogError(ex, "Error during company sync from Viewpoint API");
            _ecmLogger.LogReferenceDataSync(false, "Companies", 0, 0, 0, ex.Message);

            return new ApiResponse<CompanySyncResultDto>
            {
                Success = false,
                Data = syncResult,
                Message = "An error occurred during company sync",
                Errors = syncResult.Errors
            };
        }
    }

    public async Task<ApiResponse<DepartmentSyncResultDto>> SyncDepartmentsFromViewpointAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var syncResult = new DepartmentSyncResultDto
        {
            SyncDate = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting department sync from Viewpoint API");

            // Get all departments from Viewpoint
            var viewpointDepartments = await _viewpointService.GetAllDepartmentsAsync();

            if (viewpointDepartments == null || !viewpointDepartments.Any())
            {
                syncResult.Errors.Add("No departments retrieved from Viewpoint API");
                _ecmLogger.LogReferenceDataSync(false, "Departments", 0, 0, 0, "No departments retrieved from Viewpoint API");
                return new ApiResponse<DepartmentSyncResultDto>
                {
                    Success = false,
                    Data = syncResult,
                    Message = "No departments found in Viewpoint"
                };
            }

            syncResult.TotalViewpointDepartments = viewpointDepartments.Count;

            // Create a set of active Viewpoint department keys for deactivation check
            var activeViewpointDeptKeys = new HashSet<string>();
            var validViewpointDepartments = new List<(int CompanyCode, int DeptCode, string DeptName, string? EmailDomain, int? HRPartner, int? HRRep, int? SafetyRep, int? PayrollRep)>();

            // First pass: validate and collect valid departments
            foreach (var viewpointDept in viewpointDepartments)
            {
                try
                {
                    // Skip departments with missing required fields
                    if (!viewpointDept.PRCo.HasValue || string.IsNullOrEmpty(viewpointDept.PRDept))
                    {
                        syncResult.Errors.Add($"Skipping department with missing company or dept code: {viewpointDept.Description}");
                        continue;
                    }

                    var companyCode = viewpointDept.PRCo.Value;
                    var deptCode = viewpointDept.PRDept;

                    // Convert department code to integer
                    if (!int.TryParse(deptCode, out var deptCodeInt))
                    {
                        syncResult.Errors.Add($"Invalid department code format: {deptCode} for company {companyCode}");
                        continue;
                    }

                    var deptKey = $"{companyCode}-{deptCodeInt}";
                    activeViewpointDeptKeys.Add(deptKey);

                    // Parse string rep values to int
                    int? hrPartner = null;
                    int? hrRep = null;
                    int? safetyRep = null;
                    int? payrollRep = null;

                    if (!string.IsNullOrWhiteSpace(viewpointDept.CustomFields?.HRPartner) &&
                        int.TryParse(viewpointDept.CustomFields.HRPartner, out var hrPartnerVal))
                    {
                        hrPartner = hrPartnerVal;
                    }
                    if (!string.IsNullOrWhiteSpace(viewpointDept.CustomFields?.HRRep) &&
                        int.TryParse(viewpointDept.CustomFields.HRRep, out var hrRepVal))
                    {
                        hrRep = hrRepVal;
                    }
                    if (!string.IsNullOrWhiteSpace(viewpointDept.CustomFields?.SafetyRep) &&
                        int.TryParse(viewpointDept.CustomFields.SafetyRep, out var safetyRepVal))
                    {
                        safetyRep = safetyRepVal;
                    }
                    if (!string.IsNullOrWhiteSpace(viewpointDept.CustomFields?.PayrollRep) &&
                        int.TryParse(viewpointDept.CustomFields.PayrollRep, out var payrollRepVal))
                    {
                        payrollRep = payrollRepVal;
                    }

                    validViewpointDepartments.Add((
                        companyCode,
                        deptCodeInt,
                        viewpointDept.Description ?? string.Empty,
                        viewpointDept.CustomFields?.Domain,
                        hrPartner,
                        hrRep,
                        safetyRep,
                        payrollRep
                    ));
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error validating department {viewpointDept.Description} (Co: {viewpointDept.PRCo}, Dept: {viewpointDept.PRDept}): {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    syncResult.Errors.Add(errorMsg);
                }
            }

            // Second pass: process valid departments using upsert logic
            foreach (var (companyCode, deptCode, deptName, emailDomain, hrPartner, hrRep, safetyRep, payrollRep) in validViewpointDepartments)
            {
                try
                {
                    // Use upsert logic based on unique index IX_PayrollDepartments_CompanyCode_DeptCode
                    var existingDept = await _context.PayrollDepartments
                        .FirstOrDefaultAsync(d => d.CompanyCode == companyCode && d.DeptCode == deptCode);

                    if (existingDept != null)
                    {
                        // Update existing department
                        var hasChanges = false;

                        if (existingDept.DeptName != deptName)
                        {
                            existingDept.DeptName = deptName;
                            hasChanges = true;
                        }

                        if (existingDept.EmailDomain != emailDomain)
                        {
                            existingDept.EmailDomain = emailDomain;
                            hasChanges = true;
                        }

                        if (existingDept.HRPartner != hrPartner)
                        {
                            existingDept.HRPartner = hrPartner;
                            hasChanges = true;
                        }

                        if (existingDept.HRRep != hrRep)
                        {
                            existingDept.HRRep = hrRep;
                            hasChanges = true;
                        }

                        if (existingDept.SafetyRep != safetyRep)
                        {
                            existingDept.SafetyRep = safetyRep;
                            hasChanges = true;
                        }

                        if (existingDept.PayrollRep != payrollRep)
                        {
                            existingDept.PayrollRep = payrollRep;
                            hasChanges = true;
                        }

                        if (!existingDept.IsActive)
                        {
                            existingDept.IsActive = true;
                            hasChanges = true;
                        }

                        if (existingDept.IsDeleted)
                        {
                            existingDept.IsDeleted = false;
                            hasChanges = true;
                        }

                        if (hasChanges)
                        {
                            existingDept.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                            existingDept.ModifiedDate = DateTime.UtcNow;
                            existingDept.ViewpointSyncDate = DateTime.UtcNow;
                            _context.PayrollDepartments.Update(existingDept);
                            syncResult.ExistingDepartmentsUpdated++;
                        }
                        else
                        {
                            // Just update sync date even if no changes
                            existingDept.ViewpointSyncDate = DateTime.UtcNow;
                            _context.PayrollDepartments.Update(existingDept);
                        }
                    }
                    else
                    {
                        // Insert new department
                        var newDept = new PayrollDepartment
                        {
                            CompanyCode = companyCode,
                            DeptCode = deptCode,
                            DeptName = deptName,
                            EmailDomain = emailDomain,
                            HRPartner = hrPartner,
                            HRRep = hrRep,
                            SafetyRep = safetyRep,
                            PayrollRep = payrollRep,
                            IsActive = true,
                            CreatedBy = _userContextService.GetUserEmployeeNumber(),
                            CreatedDate = DateTime.UtcNow,
                            ViewpointSyncDate = DateTime.UtcNow,
                            IsDeleted = false
                        };

                        await _context.PayrollDepartments.AddAsync(newDept);
                        syncResult.NewDepartmentsAdded++;
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error upserting department {deptName} (Co: {companyCode}, Dept: {deptCode}): {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    syncResult.Errors.Add(errorMsg);
                }
            }

            // Third pass: deactivate departments that are no longer in Viewpoint
            var departmentsToDeactivate = await _context.PayrollDepartments
                .Where(d => d.IsActive && !d.IsDeleted)
                .ToListAsync();

            foreach (var existingDept in departmentsToDeactivate)
            {
                var deptKey = $"{existingDept.CompanyCode}-{existingDept.DeptCode}";
                if (!activeViewpointDeptKeys.Contains(deptKey))
                {
                    existingDept.IsActive = false;
                    existingDept.ModifiedDate = DateTime.UtcNow;
                    _context.PayrollDepartments.Update(existingDept);
                    syncResult.DepartmentsDeactivated++;
                }
            }

            // Save all changes
            await _context.SaveChangesAsync();

            // Fourth pass: Update CompanyDL with representative emails
            _logger.LogInformation("Updating CompanyDL with representative emails from PayrollDepartments");
            await UpdateCompanyDLWithRepresentativeEmailsAsync(validViewpointDepartments);

            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;

            _logger.LogInformation("Department sync completed successfully. {Summary}. Duration: {Duration}ms",
                syncResult.Summary, syncResult.SyncDuration.TotalMilliseconds);

            _ecmLogger.LogReferenceDataSync(syncResult.Success, "Departments",
                syncResult.NewDepartmentsAdded, syncResult.ExistingDepartmentsUpdated, 0, null);

            return new ApiResponse<DepartmentSyncResultDto>
            {
                Success = syncResult.Success,
                Data = syncResult,
                Message = syncResult.Success ? syncResult.Summary : "Department sync completed with errors",
                Errors = syncResult.Errors
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;
            syncResult.Errors.Add($"Sync failed: {ex.Message}");

            _logger.LogError(ex, "Error during department sync from Viewpoint API");
            _ecmLogger.LogReferenceDataSync(false, "Departments", 0, 0, 0, ex.Message);

            return new ApiResponse<DepartmentSyncResultDto>
            {
                Success = false,
                Data = syncResult,
                Message = "An error occurred during department sync",
                Errors = syncResult.Errors
            };
        }
    }

    /// <summary>
    /// Updates CompanyDL table with representative emails from PayrollDepartments
    /// </summary>
    private async Task UpdateCompanyDLWithRepresentativeEmailsAsync(
        List<(int CompanyCode, int DeptCode, string DeptName, string? EmailDomain, int? HRPartner, int? HRRep, int? SafetyRep, int? PayrollRep)> departments)
    {
        try
        {
            foreach (var (companyCode, deptCode, _, _, hrPartner, hrRep, safetyRep, payrollRep) in departments)
            {
                // Skip if no representative values
                if (!hrPartner.HasValue && !hrRep.HasValue && !safetyRep.HasValue && !payrollRep.HasValue)
                {
                    continue;
                }

                // Build HRDL email (combination of HRPartner and HRRep work emails)
                var hrEmails = new List<string>();
                if (hrPartner.HasValue)
                {
                    var hrPartnerEmail = await _context.Employees
                        .Where(e => e.EmployeeNumber == hrPartner.Value && !e.IsDeleted)
                        .Select(e => e.WorkEmail)
                        .FirstOrDefaultAsync();
                    if (!string.IsNullOrWhiteSpace(hrPartnerEmail))
                    {
                        hrEmails.Add(hrPartnerEmail);
                    }
                }
                if (hrRep.HasValue)
                {
                    var hrRepEmail = await _context.Employees
                        .Where(e => e.EmployeeNumber == hrRep.Value && !e.IsDeleted)
                        .Select(e => e.WorkEmail)
                        .FirstOrDefaultAsync();
                    if (!string.IsNullOrWhiteSpace(hrRepEmail))
                    {
                        hrEmails.Add(hrRepEmail);
                    }
                }
                var hrdlValue = hrEmails.Any() ? string.Join(";", hrEmails) : null;

                // Get SafetyRep email
                string? safetyDLValue = null;
                if (safetyRep.HasValue)
                {
                    safetyDLValue = await _context.Employees
                        .Where(e => e.EmployeeNumber == safetyRep.Value && !e.IsDeleted)
                        .Select(e => e.WorkEmail)
                        .FirstOrDefaultAsync();
                }

                // Get PayrollRep email
                string? payrollDLValue = null;
                if (payrollRep.HasValue)
                {
                    payrollDLValue = await _context.Employees
                        .Where(e => e.EmployeeNumber == payrollRep.Value && !e.IsDeleted)
                        .Select(e => e.WorkEmail)
                        .FirstOrDefaultAsync();
                }

                // Skip if no emails found
                if (string.IsNullOrWhiteSpace(hrdlValue) && string.IsNullOrWhiteSpace(safetyDLValue) && string.IsNullOrWhiteSpace(payrollDLValue))
                {
                    continue;
                }

                // Find or create CompanyDL record
                var companyDL = await _context.CompanyDLs
                    .FirstOrDefaultAsync(c => c.CompanyCode == companyCode && c.DeptCode == deptCode && !c.IsDeleted);

                if (companyDL != null)
                {
                    // Update existing record
                    var hasChanges = false;

                    if (!string.IsNullOrWhiteSpace(hrdlValue) && companyDL.HRDL != hrdlValue)
                    {
                        companyDL.HRDL = hrdlValue;
                        hasChanges = true;
                    }

                    if (!string.IsNullOrWhiteSpace(safetyDLValue) && companyDL.SafetyDL != safetyDLValue)
                    {
                        companyDL.SafetyDL = safetyDLValue;
                        hasChanges = true;
                    }

                    if (!string.IsNullOrWhiteSpace(payrollDLValue) && companyDL.PAYROLLDL != payrollDLValue)
                    {
                        companyDL.PAYROLLDL = payrollDLValue;
                        hasChanges = true;
                    }

                    if (hasChanges)
                    {
                        companyDL.ModifiedDate = DateTime.UtcNow;
                        _context.CompanyDLs.Update(companyDL);
                        _logger.LogInformation("Updated CompanyDL for Company {CompanyCode}, Dept {DeptCode}: HRDL={HRDL}, SafetyDL={SafetyDL}, PAYROLLDL={PAYROLLDL}",
                            companyCode, deptCode, hrdlValue, safetyDLValue, payrollDLValue);
                    }
                }
                else
                {
                    // Create new CompanyDL record
                    var newCompanyDL = new CompanyDL
                    {
                        CompanyCode = companyCode,
                        DeptCode = deptCode,
                        HRDL = hrdlValue,
                        SafetyDL = safetyDLValue,
                        PAYROLLDL = payrollDLValue,
                        CreatedDate = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _context.CompanyDLs.AddAsync(newCompanyDL);
                    _logger.LogInformation("Created new CompanyDL for Company {CompanyCode}, Dept {DeptCode}: HRDL={HRDL}, SafetyDL={SafetyDL}, PAYROLLDL={PAYROLLDL}",
                        companyCode, deptCode, hrdlValue, safetyDLValue, payrollDLValue);
                }
            }

            // Save CompanyDL changes
            await _context.SaveChangesAsync();
            _logger.LogInformation("CompanyDL representative emails update completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CompanyDL with representative emails");
            // Don't throw - let the department sync succeed even if CompanyDL update fails
        }
    }

    public async Task<ApiResponse<PositionSyncResultDto>> SyncPositionsFromViewpointAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var syncResult = new PositionSyncResultDto
        {
            SyncDate = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting position sync from Viewpoint API");

            // Get all positions from Viewpoint
            var viewpointPositions = await _viewpointService.GetAllPositionsAsync();

            if (viewpointPositions == null || !viewpointPositions.Any())
            {
                syncResult.Errors.Add("No positions retrieved from Viewpoint API");
                _ecmLogger.LogReferenceDataSync(false, "Positions", 0, 0, 0, "No positions retrieved from Viewpoint API");
                return new ApiResponse<PositionSyncResultDto>
                {
                    Success = false,
                    Data = syncResult,
                    Message = "No positions found in Viewpoint"
                };
            }

            syncResult.TotalViewpointPositions = viewpointPositions.Count;

            // Get existing positions from local database
            var existingPositions = await _context.Positions
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            // Create a set of active Viewpoint position keys for deactivation check
            var activeViewpointPositionKeys = new HashSet<string>();

            // Process each Viewpoint position
            foreach (var viewpointPosition in viewpointPositions)
            {
                try
                {
                    // Skip positions with missing required fields
                    if (!viewpointPosition.HRCo.HasValue || string.IsNullOrEmpty(viewpointPosition.PositionCode))
                    {
                        syncResult.Errors.Add($"Skipping position with missing company or position code: {viewpointPosition.JobTitle}");
                        continue;
                    }

                    var companyCode = viewpointPosition.HRCo.Value;
                    var positionCode = viewpointPosition.PositionCode;
                    var positionKey = $"{companyCode}-{positionCode}";
                    activeViewpointPositionKeys.Add(positionKey);

                    // Find existing position
                    var existingPosition = existingPositions.FirstOrDefault(p => 
                        p.CompanyCode == companyCode && p.PositionCode == positionCode);

                    if (existingPosition != null)
                    {
                        // Update existing position
                        var hasChanges = false;

                        // Use JobTitle as the position name, fallback to Description if JobTitle is empty
                        var positionName = !string.IsNullOrEmpty(viewpointPosition.JobTitle) 
                            ? viewpointPosition.JobTitle 
                            : viewpointPosition.Description ?? string.Empty;

                        if (existingPosition.PositionName != positionName)
                        {
                            existingPosition.PositionName = positionName;
                            hasChanges = true;
                        }

                        if (existingPosition.Type != viewpointPosition.Type)
                        {
                            existingPosition.Type = viewpointPosition.Type;
                            hasChanges = true;
                        }

                        if (!existingPosition.IsActive)
                        {
                            existingPosition.IsActive = true;
                            hasChanges = true;
                        }

                        if (hasChanges)
                        {
                            existingPosition.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                            existingPosition.ModifiedDate = DateTime.UtcNow;
                            existingPosition.ViewpointSyncDate = DateTime.UtcNow;
                            _context.Positions.Update(existingPosition);
                            syncResult.ExistingPositionsUpdated++;
                        }
                        else
                        {
                            // Just update sync date even if no changes
                            existingPosition.ViewpointSyncDate = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Create new position
                        var positionName = !string.IsNullOrEmpty(viewpointPosition.JobTitle)
                            ? viewpointPosition.JobTitle
                            : viewpointPosition.Description ?? string.Empty;

                        var newPosition = new Position
                        {
                            CompanyCode = companyCode,
                            PositionCode = positionCode,
                            PositionName = positionName,
                            Type = viewpointPosition.Type,
                            IsActive = true,
                            CreatedBy = _userContextService.GetUserEmployeeNumber(),
                            CreatedDate = DateTime.UtcNow,
                            ViewpointSyncDate = DateTime.UtcNow,
                            IsDeleted = false
                        };

                        await _context.Positions.AddAsync(newPosition);
                        syncResult.NewPositionsAdded++;
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error processing position {viewpointPosition.JobTitle} (Co: {viewpointPosition.HRCo}, Code: {viewpointPosition.PositionCode}): {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    syncResult.Errors.Add(errorMsg);
                }
            }

            // Deactivate positions that are no longer in Viewpoint
            foreach (var existingPosition in existingPositions.Where(p => p.IsActive))
            {
                var positionKey = $"{existingPosition.CompanyCode}-{existingPosition.PositionCode}";
                if (!activeViewpointPositionKeys.Contains(positionKey))
                {
                    existingPosition.IsActive = false;
                    existingPosition.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                    existingPosition.ModifiedDate = DateTime.UtcNow;
                    _context.Positions.Update(existingPosition);
                    syncResult.PositionsDeactivated++;
                }
            }

            // Save all changes
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;

            _logger.LogInformation("Position sync completed successfully. {Summary}. Duration: {Duration}ms",
                syncResult.Summary, syncResult.SyncDuration.TotalMilliseconds);

            _ecmLogger.LogReferenceDataSync(syncResult.Success, "Positions",
                syncResult.NewPositionsAdded, syncResult.ExistingPositionsUpdated, 0, null);

            return new ApiResponse<PositionSyncResultDto>
            {
                Success = syncResult.Success,
                Data = syncResult,
                Message = syncResult.Success ? syncResult.Summary : "Position sync completed with errors",
                Errors = syncResult.Errors
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;
            syncResult.Errors.Add($"Sync failed: {ex.Message}");

            _logger.LogError(ex, "Error during position sync from Viewpoint API");
            _ecmLogger.LogReferenceDataSync(false, "Positions", 0, 0, 0, ex.Message);

            return new ApiResponse<PositionSyncResultDto>
            {
                Success = false,
                Data = syncResult,
                Message = "An error occurred during position sync",
                Errors = syncResult.Errors
            };
        }
    }

    public async Task<ApiResponse<PayrollGroupSyncResultDto>> SyncPayrollGroupsFromViewpointAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var syncResult = new PayrollGroupSyncResultDto
        {
            SyncDate = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting payroll group sync from Viewpoint API");

            // Get all payroll groups from Viewpoint
            var viewpointPayrollGroups = await _viewpointService.GetAllPayrollGroupsAsync();

            if (viewpointPayrollGroups == null || !viewpointPayrollGroups.Any())
            {
                syncResult.Errors.Add("No payroll groups retrieved from Viewpoint API");
                _ecmLogger.LogReferenceDataSync(false, "PayrollGroups", 0, 0, 0, "No payroll groups retrieved from Viewpoint API");
                return new ApiResponse<PayrollGroupSyncResultDto>
                {
                    Success = false,
                    Data = syncResult,
                    Message = "No payroll groups found in Viewpoint"
                };
            }

            syncResult.TotalViewpointPayrollGroups = viewpointPayrollGroups.Count;

            // Get existing payroll groups from local database
            var existingPayrollGroups = await _context.PayrollGroups
                .Where(pg => !pg.IsDeleted)
                .ToListAsync();

            // Create a set of active Viewpoint payroll group keys for deactivation check
            var activeViewpointPayrollGroupKeys = new HashSet<string>();

            // Process each Viewpoint payroll group
            foreach (var viewpointPayrollGroup in viewpointPayrollGroups)
            {
                try
                {
                    // Skip payroll groups with missing required fields
                    if (!viewpointPayrollGroup.PRCo.HasValue || !viewpointPayrollGroup.PRGroup.HasValue)
                    {
                        syncResult.Errors.Add($"Skipping payroll group with missing company or group code: {viewpointPayrollGroup.Description}");
                        continue;
                    }

                    var companyCode = viewpointPayrollGroup.PRCo.Value;
                    var groupCode = viewpointPayrollGroup.PRGroup.Value;
                    var groupKey = $"{companyCode}-{groupCode}";
                    activeViewpointPayrollGroupKeys.Add(groupKey);

                    // Find existing payroll group
                    var existingPayrollGroup = existingPayrollGroups.FirstOrDefault(pg =>
                        pg.CompanyCode == companyCode && pg.GroupCode == groupCode);

                    if (existingPayrollGroup != null)
                    {
                        // Update existing payroll group
                        var hasChanges = false;

                        var groupName = viewpointPayrollGroup.Description ?? string.Empty;
                        if (existingPayrollGroup.GroupName != groupName)
                        {
                            existingPayrollGroup.GroupName = groupName;
                            hasChanges = true;
                        }

                        if (!existingPayrollGroup.IsActive)
                        {
                            existingPayrollGroup.IsActive = true;
                            hasChanges = true;
                        }

                        if (hasChanges)
                        {
                            existingPayrollGroup.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                            existingPayrollGroup.ModifiedDate = DateTime.UtcNow;
                            existingPayrollGroup.ViewpointSyncDate = DateTime.UtcNow;
                            _context.PayrollGroups.Update(existingPayrollGroup);
                            syncResult.ExistingPayrollGroupsUpdated++;
                        }
                        else
                        {
                            // Just update sync date even if no changes
                            existingPayrollGroup.ViewpointSyncDate = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Create new payroll group
                        var groupName = viewpointPayrollGroup.Description ?? string.Empty;

                        var newPayrollGroup = new PayrollGroup
                        {
                            CompanyCode = companyCode,
                            GroupCode = groupCode,
                            GroupName = groupName,
                            IsActive = true,
                            CreatedBy = _userContextService.GetUserEmployeeNumber(),
                            CreatedDate = DateTime.UtcNow,
                            ViewpointSyncDate = DateTime.UtcNow,
                            IsDeleted = false
                        };

                        await _context.PayrollGroups.AddAsync(newPayrollGroup);
                        syncResult.NewPayrollGroupsAdded++;
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error processing payroll group {viewpointPayrollGroup.Description} (Co: {viewpointPayrollGroup.PRCo}, Group: {viewpointPayrollGroup.PRGroup}): {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    syncResult.Errors.Add(errorMsg);
                }
            }

            // Deactivate payroll groups that are no longer in Viewpoint
            foreach (var existingPayrollGroup in existingPayrollGroups.Where(pg => pg.IsActive))
            {
                var groupKey = $"{existingPayrollGroup.CompanyCode}-{existingPayrollGroup.GroupCode}";
                if (!activeViewpointPayrollGroupKeys.Contains(groupKey))
                {
                    existingPayrollGroup.IsActive = false;
                    existingPayrollGroup.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                    existingPayrollGroup.ModifiedDate = DateTime.UtcNow;
                    _context.PayrollGroups.Update(existingPayrollGroup);
                    syncResult.PayrollGroupsDeactivated++;
                }
            }

            // Save all changes
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;

            _logger.LogInformation("Payroll group sync completed successfully. {Summary}. Duration: {Duration}ms",
                syncResult.Summary, syncResult.SyncDuration.TotalMilliseconds);

            _ecmLogger.LogReferenceDataSync(syncResult.Success, "PayrollGroups",
                syncResult.NewPayrollGroupsAdded, syncResult.ExistingPayrollGroupsUpdated, 0, null);

            return new ApiResponse<PayrollGroupSyncResultDto>
            {
                Success = syncResult.Success,
                Data = syncResult,
                Message = syncResult.Success ? syncResult.Summary : "Payroll group sync completed with errors",
                Errors = syncResult.Errors
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;
            syncResult.Errors.Add($"Sync failed: {ex.Message}");

            _logger.LogError(ex, "Error during payroll group sync from Viewpoint API");
            _ecmLogger.LogReferenceDataSync(false, "PayrollGroups", 0, 0, 0, ex.Message);

            return new ApiResponse<PayrollGroupSyncResultDto>
            {
                Success = false,
                Data = syncResult,
                Message = "An error occurred during payroll group sync",
                Errors = syncResult.Errors
            };
        }
    }

    public async Task<ApiResponse<UnionCraftSyncResultDto>> SyncUnionCraftsFromViewpointAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var syncResult = new UnionCraftSyncResultDto
        {
            SyncDate = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting union craft sync from Viewpoint API");

            // Get all crafts from Viewpoint
            var viewpointCrafts = await _viewpointService.GetAllCraftsAsync();

            if (viewpointCrafts == null || !viewpointCrafts.Any())
            {
                _logger.LogWarning("No union crafts retrieved from Viewpoint API");
                syncResult.Errors.Add("No union crafts retrieved from Viewpoint API");
                _ecmLogger.LogReferenceDataSync(false, "UnionCrafts", 0, 0, 0, "No union crafts retrieved from Viewpoint API");
                return new ApiResponse<UnionCraftSyncResultDto>
                {
                    Success = false,
                    Data = syncResult,
                    Message = "No union crafts retrieved from Viewpoint API"
                };
            }

            syncResult.TotalViewpointUnionCrafts = viewpointCrafts.Count;
            _logger.LogInformation("Retrieved {Count} union crafts from Viewpoint API", viewpointCrafts.Count);

            // Get all existing union crafts from local database (filter out records with empty CraftCode to avoid duplicate key errors)
            var existingUnionCrafts = await _context.UnionCrafts
                .Where(uc => !uc.IsDeleted && !string.IsNullOrWhiteSpace(uc.CraftCode))
                .ToListAsync();

            var existingUnionCraftDict = existingUnionCrafts.ToDictionary(uc => $"{uc.CompanyCode}-{uc.CraftCode}", uc => uc);
            var processedCraftKeys = new HashSet<string>();

            // Process each Viewpoint craft
            foreach (var viewpointCraft in viewpointCrafts)
            {
                try
                {
                    // Skip if company code or craft code is missing
                    if (!viewpointCraft.PRCo.HasValue || string.IsNullOrWhiteSpace(viewpointCraft.Craft))
                    {
                        _logger.LogWarning("Skipping craft with missing PRCo or Craft: {Description}", viewpointCraft.Description);
                        syncResult.Errors.Add($"Skipping craft with missing PRCo or Craft: {viewpointCraft.Description}");
                        continue;
                    }

                    var companyCode = viewpointCraft.PRCo.Value;
                    var craftCode = viewpointCraft.Craft.Trim();
                    var craftKey = $"{companyCode}-{craftCode}";
                    processedCraftKeys.Add(craftKey);

                    var description = !string.IsNullOrWhiteSpace(viewpointCraft.Description)
                        ? viewpointCraft.Description.Trim()
                        : $"Craft {craftCode}";

                    if (existingUnionCraftDict.TryGetValue(craftKey, out var existingUnionCraft))
                    {
                        // Update existing union craft
                        var hasChanges = false;

                        if (existingUnionCraft.Description != description)
                        {
                            existingUnionCraft.Description = description;
                            hasChanges = true;
                        }

                        if (!existingUnionCraft.IsActive)
                        {
                            existingUnionCraft.IsActive = true;
                            hasChanges = true;
                        }

                        if (hasChanges)
                        {
                            existingUnionCraft.ViewpointSyncDate = DateTime.UtcNow;
                            existingUnionCraft.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                            existingUnionCraft.ModifiedDate = DateTime.UtcNow;

                            syncResult.ExistingUnionCraftsUpdated++;
                            _logger.LogDebug("Updated union craft {CraftKey}: {Description}", craftKey, description);
                        }
                        else
                        {
                            // Just update sync date even if no changes
                            existingUnionCraft.ViewpointSyncDate = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Add new union craft
                        var currentUserId = _userContextService.GetUserEmployeeNumber();
                        var newUnionCraft = new UnionCraft
                        {
                            CompanyCode = companyCode,
                            CraftCode = craftCode,
                            Description = description,
                            IsActive = true,
                            ViewpointSyncDate = DateTime.UtcNow,
                            CreatedBy = currentUserId,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedBy = currentUserId,
                            ModifiedDate = DateTime.UtcNow
                        };

                        _context.UnionCrafts.Add(newUnionCraft);
                        syncResult.NewUnionCraftsAdded++;
                        _logger.LogDebug("Added new union craft {CraftKey}: {Description}", craftKey, description);
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error processing union craft {viewpointCraft.Craft} (Co: {viewpointCraft.PRCo}): {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    syncResult.Errors.Add(errorMsg);
                }
            }

            // Deactivate union crafts that are no longer in Viewpoint
            foreach (var existingUnionCraft in existingUnionCrafts)
            {
                var craftKey = $"{existingUnionCraft.CompanyCode}-{existingUnionCraft.CraftCode}";
                if (!processedCraftKeys.Contains(craftKey) && existingUnionCraft.IsActive)
                {
                    existingUnionCraft.IsActive = false;
                    existingUnionCraft.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                    existingUnionCraft.ModifiedDate = DateTime.UtcNow;

                    syncResult.UnionCraftsDeactivated++;
                    _logger.LogInformation("Deactivated union craft {CraftKey}: {Description} (no longer in Viewpoint)",
                        craftKey, existingUnionCraft.Description);
                }
            }

            // Save all changes
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;

            _logger.LogInformation("Union craft sync completed successfully. {Summary}. Duration: {Duration}ms",
                syncResult.Summary, syncResult.SyncDuration.TotalMilliseconds);

            _ecmLogger.LogReferenceDataSync(syncResult.Success, "UnionCrafts",
                syncResult.NewUnionCraftsAdded, syncResult.ExistingUnionCraftsUpdated, 0, null);

            return new ApiResponse<UnionCraftSyncResultDto>
            {
                Success = syncResult.Success,
                Data = syncResult,
                Message = syncResult.Success ? syncResult.Summary : "Union craft sync completed with errors",
                Errors = syncResult.Errors
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;
            syncResult.Errors.Add($"Sync failed: {ex.Message}");

            _logger.LogError(ex, "Error during union craft sync from Viewpoint API");
            _ecmLogger.LogReferenceDataSync(false, "UnionCrafts", 0, 0, 0, ex.Message);

            return new ApiResponse<UnionCraftSyncResultDto>
            {
                Success = false,
                Data = syncResult,
                Message = "An error occurred during union craft sync",
                Errors = syncResult.Errors
            };
        }
    }

    public async Task<ApiResponse<EmploymentStatusSyncResultDto>> SyncEmploymentStatusesFromViewpointAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var syncResult = new EmploymentStatusSyncResultDto
        {
            SyncDate = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting employment status sync from Viewpoint API");

            // Get all employment statuses from Viewpoint
            var viewpointEmploymentStatuses = await _viewpointService.GetAllEmploymentStatusesAsync();

            if (viewpointEmploymentStatuses == null || !viewpointEmploymentStatuses.Any())
            {
                _logger.LogWarning("No employment statuses retrieved from Viewpoint API");
                syncResult.Errors.Add("No employment statuses retrieved from Viewpoint API");
                _ecmLogger.LogReferenceDataSync(false, "EmploymentStatuses", 0, 0, 0, "No employment statuses retrieved from Viewpoint API");
                return new ApiResponse<EmploymentStatusSyncResultDto>
                {
                    Success = false,
                    Data = syncResult,
                    Message = "No employment statuses retrieved from Viewpoint API"
                };
            }

            syncResult.TotalViewpointEmploymentStatuses = viewpointEmploymentStatuses.Count;
            _logger.LogInformation("Retrieved {Count} employment statuses from Viewpoint API", viewpointEmploymentStatuses.Count);

            // Get all existing employment statuses from local database
            var existingEmploymentStatuses = await _context.EmploymentStatuses
                .Where(es => !es.IsDeleted)
                .ToListAsync();

            var existingEmploymentStatusDict = existingEmploymentStatuses.ToDictionary(
                es => $"{es.CompanyCode}-{es.Status}-{es.CodeType ?? string.Empty}",
                es => es);
            var processedStatusKeys = new HashSet<string>();

            // Process each Viewpoint employment status
            foreach (var viewpointStatus in viewpointEmploymentStatuses)
            {
                try
                {
                    // Skip if company code or status code is missing
                    if (!viewpointStatus.HRCo.HasValue || string.IsNullOrWhiteSpace(viewpointStatus.Code))
                    {
                        _logger.LogWarning("Skipping employment status with missing HRCo or Code: Notes={Notes}, HRCo={HRCo}, Code={Code}",
                            viewpointStatus.Notes, viewpointStatus.HRCo, viewpointStatus.Code);
                        syncResult.Errors.Add($"Skipping employment status with missing HRCo or Code: {viewpointStatus.Notes}");
                        continue;
                    }

                    // Map fields according to requirements:
                    // HRCo = CompanyCode
                    // Code = Status
                    // Description = Description
                    // Notes = Notes
                    var companyCode = viewpointStatus.HRCo.Value;
                    var status = viewpointStatus.Code.Trim();

                    var codeType = !string.IsNullOrWhiteSpace(viewpointStatus.Type)
                        ? viewpointStatus.Type.Trim()
                        : null;

                    var statusKey = $"{companyCode}-{status}-{codeType ?? string.Empty}";
                    processedStatusKeys.Add(statusKey);

                    var description = !string.IsNullOrWhiteSpace(viewpointStatus.Description)
                        ? viewpointStatus.Description.Trim()
                        : $"Status {status}";

                    var notes = !string.IsNullOrWhiteSpace(viewpointStatus.Notes)
                        ? viewpointStatus.Notes.Trim()
                        : string.Empty;

                    if (existingEmploymentStatusDict.TryGetValue(statusKey, out var existingStatus))
                    {
                        // Update existing employment status
                        var hasChanges = false;

                        if (existingStatus.Description != description)
                        {
                            existingStatus.Description = description;
                            hasChanges = true;
                        }

                        if (existingStatus.Notes != notes)
                        {
                            existingStatus.Notes = notes;
                            hasChanges = true;
                        }

                        if (existingStatus.CodeType != codeType)
                        {
                            existingStatus.CodeType = codeType;
                            hasChanges = true;
                        }

                        // Determine IsActive based on ActiveYN field
                        // Default to true if ActiveYN is not provided (since we're filtering for active statuses)
                        var isActive = string.IsNullOrWhiteSpace(viewpointStatus.ActiveYN) ||
                                      viewpointStatus.ActiveYN.Equals("Y", StringComparison.OrdinalIgnoreCase);

                        if (existingStatus.IsActive != isActive)
                        {
                            existingStatus.IsActive = isActive;
                            hasChanges = true;
                        }

                        if (hasChanges)
                        {
                            existingStatus.ViewpointSyncDate = DateTime.UtcNow;
                            existingStatus.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                            existingStatus.ModifiedDate = DateTime.UtcNow;

                            syncResult.ExistingEmploymentStatusesUpdated++;
                            _logger.LogDebug("Updated employment status {StatusKey}: {Description}, Notes={Notes}", statusKey, description, notes);
                        }
                        else
                        {
                            // Just update sync date even if no changes
                            existingStatus.ViewpointSyncDate = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Add new employment status
                        // Default to true if ActiveYN is not provided (since we're filtering for active statuses)
                        var isActive = string.IsNullOrWhiteSpace(viewpointStatus.ActiveYN) ||
                                      viewpointStatus.ActiveYN.Equals("Y", StringComparison.OrdinalIgnoreCase);

                        var currentUserId = _userContextService.GetUserEmployeeNumber();
                        var newEmploymentStatus = new EmploymentStatus
                        {
                            CompanyCode = companyCode,
                            Status = status,
                            Description = description,
                            Notes = notes,
                            CodeType = codeType,
                            IsActive = isActive,
                            ViewpointSyncDate = DateTime.UtcNow,
                            CreatedBy = currentUserId,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedBy = currentUserId,
                            ModifiedDate = DateTime.UtcNow
                        };

                        _context.EmploymentStatuses.Add(newEmploymentStatus);
                        syncResult.NewEmploymentStatusesAdded++;
                        _logger.LogDebug("Added new employment status {StatusKey}: {Description}, Notes={Notes}", statusKey, description, notes);
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error processing employment status {viewpointStatus.Code} (Co: {viewpointStatus.HRCo}): {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    syncResult.Errors.Add(errorMsg);
                }
            }

            // Deactivate employment statuses that are no longer in Viewpoint
            foreach (var existingStatus in existingEmploymentStatuses)
            {
                var statusKey = $"{existingStatus.CompanyCode}-{existingStatus.Status}-{existingStatus.CodeType ?? string.Empty}";
                if (!processedStatusKeys.Contains(statusKey) && existingStatus.IsActive)
                {
                    existingStatus.IsActive = false;
                    existingStatus.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                    existingStatus.ModifiedDate = DateTime.UtcNow;

                    syncResult.EmploymentStatusesDeactivated++;
                    _logger.LogInformation("Deactivated employment status {StatusKey}: {Description} (no longer in Viewpoint)",
                        statusKey, existingStatus.Description);
                }
            }

            // Save all changes
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;

            _logger.LogInformation("Employment status sync completed successfully. {Summary}. Duration: {Duration}ms",
                syncResult.Summary, syncResult.SyncDuration.TotalMilliseconds);

            _ecmLogger.LogReferenceDataSync(syncResult.Success, "EmploymentStatuses",
                syncResult.NewEmploymentStatusesAdded, syncResult.ExistingEmploymentStatusesUpdated, 0, null);

            return new ApiResponse<EmploymentStatusSyncResultDto>
            {
                Success = syncResult.Success,
                Data = syncResult,
                Message = syncResult.Success ? syncResult.Summary : "Employment status sync completed with errors",
                Errors = syncResult.Errors
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;
            syncResult.Errors.Add($"Sync failed: {ex.Message}");

            _logger.LogError(ex, "Error during employment status sync from Viewpoint API");
            _ecmLogger.LogReferenceDataSync(false, "EmploymentStatuses", 0, 0, 0, ex.Message);

            return new ApiResponse<EmploymentStatusSyncResultDto>
            {
                Success = false,
                Data = syncResult,
                Message = "An error occurred during employment status sync",
                Errors = syncResult.Errors
            };
        }
    }

    public async Task<ApiResponse<EmployeeSalaryTypeSyncResultDto>> SyncEmployeeSalaryTypesFromViewpointAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var syncResult = new EmployeeSalaryTypeSyncResultDto
        {
            SyncDate = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting employee salary type sync from Viewpoint API");

            // Get all earning codes from Viewpoint
            var viewpointEarningCodes = await _viewpointService.GetAllEarningCodesAsync();

            if (viewpointEarningCodes == null || !viewpointEarningCodes.Any())
            {
                _logger.LogWarning("No earning codes retrieved from Viewpoint API");
                _ecmLogger.LogReferenceDataSync(false, "EmployeeSalaryTypes", 0, 0, 0, "No earning codes retrieved from Viewpoint API");
                syncResult.Errors.Add("No earning codes retrieved from Viewpoint API");
                return new ApiResponse<EmployeeSalaryTypeSyncResultDto>
                {
                    Success = false,
                    Data = syncResult,
                    Message = "No earning codes retrieved from Viewpoint API"
                };
            }

            syncResult.TotalViewpointSalaryTypes = viewpointEarningCodes.Count;
            _logger.LogInformation("Retrieved {Count} earning codes from Viewpoint API", viewpointEarningCodes.Count);

            // Get all existing employee salary types from local database
            var existingEmployeeSalaryTypes = await _context.EmployeeSalaryTypes
                .Where(est => !est.IsDeleted)
                .ToListAsync();

            var existingEmployeeSalaryTypeDict = existingEmployeeSalaryTypes.ToDictionary(est => $"{est.CompanyCode}-{est.SalaryCode}", est => est);
            var processedSalaryTypeKeys = new HashSet<string>();

            // Process each Viewpoint earning code
            foreach (var viewpointEarningCode in viewpointEarningCodes)
            {
                try
                {
                    // Skip if company code or earning code is missing
                    // Note: PRCo = CompanyCode, EarnCode = SalaryCode in the mapping
                    if (!viewpointEarningCode.PRCo.HasValue || !viewpointEarningCode.EarnCode.HasValue)
                    {
                        _logger.LogWarning("Skipping earning code with missing PRCo or EarnCode: {Description}", viewpointEarningCode.Description);
                        syncResult.Errors.Add($"Skipping earning code with missing PRCo or EarnCode: {viewpointEarningCode.Description}");
                        continue;
                    }

                    var companyCode = viewpointEarningCode.PRCo.Value;
                    var salaryCode = viewpointEarningCode.EarnCode.Value;
                    var salaryTypeKey = $"{companyCode}-{salaryCode}";
                    processedSalaryTypeKeys.Add(salaryTypeKey);

                    var description = !string.IsNullOrWhiteSpace(viewpointEarningCode.Description)
                        ? viewpointEarningCode.Description.Trim()
                        : $"Earning Code {salaryCode}";

                    if (existingEmployeeSalaryTypeDict.TryGetValue(salaryTypeKey, out var existingSalaryType))
                    {
                        // Update existing employee salary type
                        var hasChanges = false;

                        if (existingSalaryType.Description != description)
                        {
                            existingSalaryType.Description = description;
                            hasChanges = true;
                        }

                        // Reactivate if it was previously deactivated
                        if (!existingSalaryType.IsActive)
                        {
                            existingSalaryType.IsActive = true;
                            hasChanges = true;
                        }

                        if (hasChanges)
                        {
                            existingSalaryType.ViewpointSyncDate = DateTime.UtcNow;
                            existingSalaryType.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                            existingSalaryType.ModifiedDate = DateTime.UtcNow;

                            syncResult.ExistingSalaryTypesUpdated++;
                            _logger.LogDebug("Updated employee salary type {SalaryTypeKey}: {Description}", salaryTypeKey, description);
                        }
                        else
                        {
                            // Just update sync date even if no changes
                            existingSalaryType.ViewpointSyncDate = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Add new employee salary type
                        var currentUserId = _userContextService.GetUserEmployeeNumber();
                        var newSalaryType = new EmployeeSalaryType
                        {
                            CompanyCode = companyCode,
                            SalaryCode = salaryCode,
                            Description = description,
                            IsActive = true,
                            ViewpointSyncDate = DateTime.UtcNow,
                            CreatedBy = currentUserId,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedBy = currentUserId,
                            ModifiedDate = DateTime.UtcNow
                        };

                        _context.EmployeeSalaryTypes.Add(newSalaryType);
                        syncResult.NewSalaryTypesAdded++;
                        _logger.LogDebug("Added new employee salary type {SalaryTypeKey}: {Description}", salaryTypeKey, description);
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error processing earning code {viewpointEarningCode.EarnCode} (Co: {viewpointEarningCode.PRCo}): {ex.Message}";
                    _logger.LogError(ex, errorMsg);
                    syncResult.Errors.Add(errorMsg);
                }
            }

            // Deactivate employee salary types that are no longer in Viewpoint
            foreach (var existingSalaryType in existingEmployeeSalaryTypes)
            {
                var salaryTypeKey = $"{existingSalaryType.CompanyCode}-{existingSalaryType.SalaryCode}";
                if (!processedSalaryTypeKeys.Contains(salaryTypeKey) && existingSalaryType.IsActive)
                {
                    existingSalaryType.IsActive = false;
                    existingSalaryType.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                    existingSalaryType.ModifiedDate = DateTime.UtcNow;

                    syncResult.SalaryTypesDeactivated++;
                    _logger.LogInformation("Deactivated employee salary type {SalaryTypeKey}: {Description} (no longer in Viewpoint)",
                        salaryTypeKey, existingSalaryType.Description);
                }
            }

            // Save all changes
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;

            _logger.LogInformation("Employee salary type sync completed successfully. {Summary}. Duration: {Duration}ms",
                syncResult.Summary, syncResult.SyncDuration.TotalMilliseconds);

            _ecmLogger.LogReferenceDataSync(syncResult.Success, "EmployeeSalaryTypes",
                syncResult.NewSalaryTypesAdded, syncResult.ExistingSalaryTypesUpdated, 0, null);

            return new ApiResponse<EmployeeSalaryTypeSyncResultDto>
            {
                Success = syncResult.Success,
                Data = syncResult,
                Message = syncResult.Success ? syncResult.Summary : "Employee salary type sync completed with errors",
                Errors = syncResult.Errors
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            syncResult.SyncDuration = stopwatch.Elapsed;
            syncResult.Errors.Add($"Sync failed: {ex.Message}");

            _logger.LogError(ex, "Error during employee salary type sync from Viewpoint API");
            _ecmLogger.LogReferenceDataSync(false, "EmployeeSalaryTypes", 0, 0, 0, ex.Message);

            return new ApiResponse<EmployeeSalaryTypeSyncResultDto>
            {
                Success = false,
                Data = syncResult,
                Message = "An error occurred during employee salary type sync",
                Errors = syncResult.Errors
            };
        }
    }

    public async Task<ApiResponse<List<CompanyTypeLocationDto>>> GetCompanyTypeLocationsAsync(int? id = null, int? companyCode = null, string? locationType = null)
    {
        try
        {
            var query = _context.CompanyTypeLocations
                .Where(ctl => !ctl.IsDeleted);

            if (id.HasValue)
            {
                query = query.Where(ctl => ctl.Id == id.Value);
            }

            if (companyCode.HasValue)
            {
                query = query.Where(ctl => ctl.CompanyCode == companyCode.Value);
            }

            if (!string.IsNullOrWhiteSpace(locationType))
            {
                query = query.Where(ctl => ctl.LocationType.ToLower().Contains(locationType.ToLower()));
            }

            var companyTypeLocations = await query
                .OrderBy(ctl => ctl.CompanyCode)
                .ThenBy(ctl => ctl.LocationType)
                .Select(ctl => new CompanyTypeLocationDto
                {
                    Id = ctl.Id,
                    CompanyCode = ctl.CompanyCode,
                    LocationType = ctl.LocationType,
                    IsUnion = ctl.IsUnion
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} company type locations with filters: Id={Id}, CompanyCode={CompanyCode}, LocationType={LocationType}",
                companyTypeLocations.Count, id, companyCode, locationType);

            return new ApiResponse<List<CompanyTypeLocationDto>>
            {
                Success = true,
                Data = companyTypeLocations,
                Message = $"Retrieved {companyTypeLocations.Count} company type locations successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company type locations with filters: Id={Id}, CompanyCode={CompanyCode}, LocationType={LocationType}",
                id, companyCode, locationType);

            return new ApiResponse<List<CompanyTypeLocationDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving company type locations",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<EmploymentStatusDto>>> GetEmploymentStatusesAsync(int? id = null, int? companyCode = null, string? status = null)
    {
        try
        {

            // Previous query joined Employees table - commented out as it only returned statuses in use
            //var query =
            //(from e in _context.Employees
            // join st in _context.EmploymentStatuses
            //     on new { e.CompanyCode, Status = e.EmploymentStatus }
            //     equals new { st.CompanyCode, Status = st.Status }
            // where !e.IsDeleted
            //       && st.Notes.Contains("ACTIVE")
            //       && (!companyCode.HasValue || e.CompanyCode == companyCode.Value)
            // select new EmploymentStatusDto
            // {
            //     Id = st.Id,
            //     CompanyCode = st.CompanyCode,
            //     Status = st.Status,
            //     Description = st.Description,
            //     IsActive = st.IsActive,
            //     ViewpointSyncDate = st.ViewpointSyncDate
            // }).Distinct()
            // .OrderBy(x => x.Status);

            var query = _context.EmploymentStatuses
                .Where(es => !es.IsDeleted
                    && es.IsActive
                    && es.Notes != null && es.Notes.Contains("ACTIVE")
                    && (!companyCode.HasValue || es.CompanyCode == companyCode.Value))
                .OrderBy(es => es.CompanyCode)
                .ThenBy(es => es.Status)
                .Select(es => new EmploymentStatusDto
                {
                    Id = es.Id,
                    CompanyCode = es.CompanyCode,
                    Status = es.Status,
                    Description = es.Description,
                    IsActive = es.IsActive,
                    ViewpointSyncDate = es.ViewpointSyncDate
                });

            var employmentStatuses = await query.ToListAsync();

            _logger.LogInformation("Retrieved {Count} employment statuses with filters: Id={Id}, CompanyCode={CompanyCode}, Status={Status}", 
                employmentStatuses.Count, id, companyCode, status);

            return new ApiResponse<List<EmploymentStatusDto>>
            {
                Success = true,
                Data = employmentStatuses,
                Message = $"Retrieved {employmentStatuses.Count} employment statuses successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employment statuses with filters: Id={Id}, CompanyCode={CompanyCode}, Status={Status}", 
                id, companyCode, status);

            return new ApiResponse<List<EmploymentStatusDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employment statuses",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<UnionCraftDto>>> GetUnionCraftsAsync(int? companyCode = null)
    {
        try
        {
            var query = _context.UnionCrafts
                .Where(uc => !uc.IsDeleted && uc.IsActive
                             && !uc.Description.Contains("DO NOT USE")
                             && !uc.Description.Contains("Do Not Use")
                             && !uc.Description.Contains("DNU"));

            if (companyCode.HasValue)
            {
                query = query.Where(uc => uc.CompanyCode == companyCode.Value);
            }

            var unionCrafts = await query
                .OrderBy(uc => uc.Description)
                .Select(uc => new UnionCraftDto
                {
                    Id = uc.Id,
                    CompanyCode = uc.CompanyCode,
                    CraftCode = uc.CraftCode,
                    Description = uc.Description,
                    IsActive = uc.IsActive,
                    ViewpointSyncDate = uc.ViewpointSyncDate,
                    CreatedBy = uc.CreatedBy,
                    CreatedDate = uc.CreatedDate,
                    ModifiedBy = uc.ModifiedBy,
                    ModifiedDate = uc.ModifiedDate
                })
                .ToListAsync();

            return new ApiResponse<List<UnionCraftDto>>
            {
                Success = true,
                Data = unionCrafts,
                Message = $"Retrieved {unionCrafts.Count} union crafts successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving union crafts with companyCode filter: {CompanyCode}", companyCode);

            return new ApiResponse<List<UnionCraftDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving union crafts",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<EmployeeSalaryTypeDto>>> GetEmployeeSalaryTypesAsync(int? companyCode = null)
    {
        try
        {
            // Join Employees with EmployeeSalaryTypes to get only salary types that are actually used by employees
            var query = from e in _context.Employees
                        join st in _context.EmployeeSalaryTypes
                            on new { e.SalaryCode, e.CompanyCode } equals new { st.SalaryCode, st.CompanyCode }
                        where !e.IsDeleted
                            && !st.IsDeleted
                            && st.IsActive
                            // Exclude Company 78 with SalaryCode 20
                            && !(e.CompanyCode == 78 && e.SalaryCode == 20)
                        select st;

            // Apply company code filter if provided
            if (companyCode.HasValue)
            {
                query = query.Where(st => st.CompanyCode == companyCode.Value);
            }

            // Get distinct salary types and project to DTO
            var employeeSalaryTypes = await query
                .Distinct()
                .OrderBy(st => st.Description)
                .Select(st => new EmployeeSalaryTypeDto
                {
                    Id = st.Id,
                    CompanyCode = st.CompanyCode,
                    SalaryCode = st.SalaryCode,
                    Description = st.Description,
                    IsActive = st.IsActive,
                    ViewpointSyncDate = st.ViewpointSyncDate
                })
                .ToListAsync();

            return new ApiResponse<List<EmployeeSalaryTypeDto>>
            {
                Success = true,
                Data = employeeSalaryTypes,
                Message = $"Retrieved {employeeSalaryTypes.Count} employee salary types successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee salary types for company code: {CompanyCode}", companyCode);

            return new ApiResponse<List<EmployeeSalaryTypeDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee salary types",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<ApprenticePercentageDto>>> GetApprenticePercentagesAsync(int? id = null, string? appPercentage = null)
    {
        try
        {
            var query = _context.ApprenticePercentages
                .Where(ap => !ap.IsDeleted && ap.IsActive);

            if (id.HasValue)
            {
                query = query.Where(ap => ap.Id == id.Value);
            }

            if (!string.IsNullOrWhiteSpace(appPercentage))
            {
                query = query.Where(ap => ap.AppPercentage.ToLower().Contains(appPercentage.ToLower()));
            }

            var apprenticePercentages = await query
                .OrderBy(ap => ap.AppPercentage)
                .Select(ap => new ApprenticePercentageDto
                {
                    Id = ap.Id,
                    AppPercentage = ap.AppPercentage,
                    AppDescription = ap.AppDescription,
                    IsActive = ap.IsActive,
                    CreatedBy = ap.CreatedBy,
                    CreatedDate = ap.CreatedDate,
                    ModifiedBy = ap.ModifiedBy,
                    ModifiedDate = ap.ModifiedDate
                })
                .ToListAsync();

            return new ApiResponse<List<ApprenticePercentageDto>>
            {
                Success = true,
                Data = apprenticePercentages,
                Message = $"Retrieved {apprenticePercentages.Count} apprentice percentages successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving apprentice percentages with filters: Id={Id}, AppPercentage={AppPercentage}", 
                id, appPercentage);

            return new ApiResponse<List<ApprenticePercentageDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving apprentice percentages",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<PositionDto>>> GetPositionsAsync(int? companyCode = null)
    {
        try
        {
            var query = _context.Positions
                .Where(p => !p.IsDeleted && p.IsActive);

            if (companyCode.HasValue)
            {
                query = query.Where(p => p.CompanyCode == companyCode.Value);
            }

            var positions = await query
                .OrderBy(p => p.PositionName)
                .Select(p => new PositionDto
                {
                    Id = p.Id,
                    CompanyCode = p.CompanyCode,
                    PositionCode = p.PositionCode,
                    PositionName = p.PositionName,
                    Type = p.Type,
                    IsActive = p.IsActive,
                    ViewpointSyncDate = p.ViewpointSyncDate
                })
                .ToListAsync();

            return new ApiResponse<List<PositionDto>>
            {
                Success = true,
                Data = positions,
                Message = $"Retrieved {positions.Count} positions successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving positions with filters: CompanyCode={CompanyCode}", companyCode);

            return new ApiResponse<List<PositionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving positions",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<SupervisorDto>>> GetSupervisorsAsync(int? companyCode = null, int? payrollDeptCode = null)
    {
        try
        {
            //var query = _context.Employees
            //    .Where(e => !e.IsDeleted && 
            //               e.EmploymentStatus != null && 
            //               e.EmploymentStatus.Contains("ACTIVE"));

            //if (companyCode.HasValue)
            //{
            //    query = query.Where(e => e.CompanyCode == companyCode.Value);
            //}

            //if (payrollDeptCode.HasValue)
            //{
            //    query = query.Where(e => e.PayrollDeptCode == payrollDeptCode.Value);
            //}

            //var supervisors = await query
            //    .OrderBy(e => e.FirstName)
            //    .ThenBy(e => e.LastName)
            //    .Select(e => new SupervisorDto
            //    {
            //        Id = e.Id,
            //        EmployeeNumber = e.EmployeeNumber,
            //        FirstName = e.FirstName,
            //        LastName = e.LastName,
            //        FullName = $"{e.FirstName} {e.LastName}",
            //        CompanyCode = e.CompanyCode,
            //        PayrollDeptCode = e.PayrollDeptCode,
            //        EmploymentStatus = e.EmploymentStatus
            //    })
            //    .ToListAsync();

            //return new ApiResponse<List<SupervisorDto>>
            //{
            //    Success = true,
            //    Data = supervisors,
            //    Message = $"Retrieved {supervisors.Count} supervisors successfully"
            //};

            // Get distinct supervisors by joining employees with their supervisors
            // Filters: supervisor must be non-terminated, employee must be in specified company/payroll dept
            var query =
                (from sup in _context.Employees
                 join e in _context.Employees on sup.SupervisorId equals e.EmployeeNumber
                 where sup.SupervisorId != null
                       && e.TerminationDate == null
                       && (!companyCode.HasValue || sup.CompanyCode == companyCode.Value)
                       && (!payrollDeptCode.HasValue || sup.PayrollDeptCode == payrollDeptCode.Value)
                 select new SupervisorDto
                 {
                     Id = e.Id,
                     SupervisorId = sup.SupervisorId,
                     EmployeeNumber = e.EmployeeNumber,
                     FirstName = e.FirstName,
                     LastName = e.LastName,
                     FullName = e.FirstName + " " + e.LastName,
                     CompanyCode = e.CompanyCode,
                     PayrollDeptCode = e.PayrollDeptCode,
                     EmploymentStatus = e.EmploymentStatus
                 }).Distinct()
                 .OrderBy(s => s.FirstName)
                 .ThenBy(s => s.LastName);

            var supervisors = await query.ToListAsync();

            return new ApiResponse<List<SupervisorDto>>
            {
                Success = true,
                Data = supervisors,
                Message = $"Retrieved {supervisors.Count} supervisors successfully"
            };

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supervisors with filters: CompanyCode={CompanyCode}, PayrollDeptCode={PayrollDeptCode}", companyCode, payrollDeptCode);

            return new ApiResponse<List<SupervisorDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving supervisors",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<BuildingAccessRequirementDto>>> GetBuildingAccessRequirementsAsync(int? companyCode = null, string? description = null, string? locationType = null)
    {
        try
        {
            var query = from b in _context.BuildingAccessRequirements
                        join ct in _context.CompanyTypeLocations on b.LocationType equals ct.LocationType
                        join c in _context.Companies on ct.CompanyCode equals c.CompanyCode
                        where b.IsActive && !b.IsDeleted && 
                              !ct.IsDeleted &&
                              c.IsActive && !c.IsDeleted
                        select new { b, ct, c };

            if (companyCode.HasValue)
            {
                query = query.Where(x => x.c.CompanyCode == companyCode.Value);
            }

            if (!string.IsNullOrEmpty(description))
            {
                query = query.Where(x => x.b.Description.Contains(description));
            }

            if (!string.IsNullOrEmpty(locationType))
            {
                query = query.Where(x => x.b.LocationType.Contains(locationType));
            }

            var buildingAccessRequirements = await query
                .Select(x => new BuildingAccessRequirementDto
                {
                    Id = x.b.Id,
                    CompanyCode = x.c.CompanyCode,
                    Description = x.b.Description,
                    LocationType = x.b.LocationType
                })
                .OrderBy(x => x.CompanyCode)
                .ThenBy(x => x.LocationType)
                .ThenBy(x => x.Description)
                .ToListAsync();

            return new ApiResponse<List<BuildingAccessRequirementDto>>
            {
                Success = true,
                Data = buildingAccessRequirements,
                Message = $"Retrieved {buildingAccessRequirements.Count} building access requirements successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving building access requirements with filters: CompanyCode={CompanyCode}, Description={Description}, LocationType={LocationType}", companyCode, description, locationType);

            return new ApiResponse<List<BuildingAccessRequirementDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving building access requirements",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<TabletProfileDto>>> GetTabletProfilesAsync(int? companyCode = null, string? locationType = null, string? profileName = null)
    {
        try
        {
            var query = from t in _context.TabletProfiles
                        join ct in _context.CompanyTypeLocations on t.LocationType equals ct.LocationType
                        join c in _context.Companies on ct.CompanyCode equals c.CompanyCode
                        where t.IsActive && !t.IsDeleted && 
                              !ct.IsDeleted &&
                              c.IsActive && !c.IsDeleted
                        select new { t, ct, c };

            if (companyCode.HasValue)
            {
                query = query.Where(x => x.c.CompanyCode == companyCode.Value);
            }

            if (!string.IsNullOrEmpty(locationType))
            {
                query = query.Where(x => x.t.LocationType.Contains(locationType));
            }

            if (!string.IsNullOrEmpty(profileName))
            {
                query = query.Where(x => x.t.ProfileName.Contains(profileName));
            }

            var tabletProfiles = await query
                .Select(x => new TabletProfileDto
                {
                    Id = x.t.Id,
                    LocationType = x.t.LocationType,
                    ProfileName = x.t.ProfileName,
                    IsActive = x.t.IsActive,
                    CreatedBy = x.t.CreatedBy,
                    CreatedDate = x.t.CreatedDate,
                    ModifiedBy = x.t.ModifiedBy,
                    ModifiedDate = x.t.ModifiedDate
                })
                .OrderBy(x => x.LocationType)
                .ThenBy(x => x.ProfileName)
                .ToListAsync();

            return new ApiResponse<List<TabletProfileDto>>
            {
                Success = true,
                Data = tabletProfiles,
                Message = $"Retrieved {tabletProfiles.Count} tablet profiles successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tablet profiles with filters: CompanyCode={CompanyCode}, LocationType={LocationType}, ProfileName={ProfileName}", companyCode, locationType, profileName);

            return new ApiResponse<List<TabletProfileDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving tablet profiles",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<ApplicationDto>>> GetApplicationsAsync(int? companyCode = null, string? name = null, string? locationType = null)
    {
        try
        {
            var query = from t in _context.Applications
                        join ct in _context.CompanyTypeLocations on t.LocationType equals ct.LocationType
                        join c in _context.Companies on ct.CompanyCode equals c.CompanyCode
                        where t.IsActive && !t.IsDeleted && 
                              !ct.IsDeleted &&
                              c.IsActive && !c.IsDeleted
                        select new { t, ct, c };

            if (companyCode.HasValue)
            {
                query = query.Where(x => x.c.CompanyCode == companyCode.Value);
            }

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(x => x.t.Name.Contains(name));
            }

            if (!string.IsNullOrEmpty(locationType))
            {
                query = query.Where(x => x.t.LocationType.Contains(locationType));
            }

            var applications = await query
                .Select(x => new ApplicationDto
                {
                    Id = x.t.Id,
                    LocationType = x.t.LocationType,
                    Name = x.t.Name,
                    Description = x.t.Description,
                    IsActive = x.t.IsActive,
                    CreatedBy = x.t.CreatedBy,
                    CreatedDate = x.t.CreatedDate,
                    ModifiedBy = x.t.ModifiedBy,
                    ModifiedDate = x.t.ModifiedDate
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            return new ApiResponse<List<ApplicationDto>>
            {
                Success = true,
                Data = applications,
                Message = $"Retrieved {applications.Count} applications successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applications with filters: CompanyCode={CompanyCode}, Name={Name}, LocationType={LocationType}", companyCode, name, locationType);

            return new ApiResponse<List<ApplicationDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving applications",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<EmployeeLicenseClassDto>>> GetEmployeeLicenseClassesAsync(int? id = null, string? licenseClass = null, bool? isUnion = null)
    {
        try
        {
            var query = _context.EmployeeLicenseClasses
                .Where(elc => !elc.IsDeleted);

            if (id.HasValue)
            {
                query = query.Where(elc => elc.Id == id.Value);
            }

            if (!string.IsNullOrEmpty(licenseClass))
            {
                query = query.Where(elc => elc.LicenseClass.Contains(licenseClass));
            }

            if (isUnion.HasValue)
            {
                query = query.Where(elc => elc.IsUnion == isUnion.Value);
            }

            var employeeLicenseClasses = await query
                .OrderBy(elc => elc.LicenseClass)
                .Select(elc => new EmployeeLicenseClassDto
                {
                    Id = elc.Id,
                    LicenseClass = elc.LicenseClass,
                    Description = elc.Description,
                    IsUnion = elc.IsUnion,
                    ViewpointSyncDate = elc.ViewpointSyncDate,
                    CreatedBy = elc.CreatedBy,
                    CreatedDate = elc.CreatedDate,
                    ModifiedBy = elc.ModifiedBy,
                    ModifiedDate = elc.ModifiedDate
                })
                .ToListAsync();

            return new ApiResponse<List<EmployeeLicenseClassDto>>
            {
                Success = true,
                Data = employeeLicenseClasses,
                Message = $"Retrieved {employeeLicenseClasses.Count} employee license classes successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee license classes with filters: Id={Id}, LicenseClass={LicenseClass}, IsUnion={IsUnion}", id, licenseClass, isUnion);

            return new ApiResponse<List<EmployeeLicenseClassDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving employee license classes",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<List<ComputerRequirementDto>>> GetComputerRequirementsAsync(int? id = null, bool? isChild = null, int? parentId = null, string? description = null)
    {
        try
        {
            var query = _context.ComputerRequirements
                .Where(cr => cr.IsActive && !cr.IsDeleted);

            if (id.HasValue)
            {
                query = query.Where(cr => cr.Id == id.Value);
            }

            if (isChild.HasValue)
            {
                query = query.Where(cr => cr.IsChild == isChild.Value);
            }

            if (parentId.HasValue)
            {
                query = query.Where(cr => cr.ParentId == parentId.Value);
            }

            if (!string.IsNullOrEmpty(description))
            {
                query = query.Where(cr => cr.Description.Contains(description));
            }

            var computerRequirements = await query
                .OrderBy(cr => cr.IsChild)
                .ThenBy(cr => cr.Description)
                .Select(cr => new ComputerRequirementDto
                {
                    Id = cr.Id,
                    Description = cr.Description,
                    IsChild = cr.IsChild,
                    ParentId = cr.ParentId,
                    IsActive = cr.IsActive,
                    CreatedBy = cr.CreatedBy,
                    CreatedDate = cr.CreatedDate,
                    ModifiedBy = cr.ModifiedBy,
                    ModifiedDate = cr.ModifiedDate
                })
                .ToListAsync();

            return new ApiResponse<List<ComputerRequirementDto>>
            {
                Success = true,
                Data = computerRequirements,
                Message = $"Retrieved {computerRequirements.Count} computer requirements successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving computer requirements with filters: Id={Id}, IsChild={IsChild}, ParentId={ParentId}, Description={Description}", id, isChild, parentId, description);

            return new ApiResponse<List<ComputerRequirementDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving computer requirements",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<string>> GenerateUsernameAsync(string firstName, string? preferredFirstName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(preferredFirstName))
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "First name is required to generate username"
                };
            }

            // Use preferred first name if provided, otherwise use first name
            // Remove all spaces to handle names like "Mary Ann" -> "maryann"
            string baseName = !string.IsNullOrWhiteSpace(preferredFirstName)
                ? preferredFirstName.ToLower().Trim().Replace(" ", "")
                : firstName.ToLower().Trim().Replace(" ", "");

            // Format: [Preferred First Name or First Name] + '000' (increment)
            // Example: PreferredFirstName is kim -> does not exist in the Employees table (NetworkId field) --> kim001
            //          else if exists say "kim001" then next is "kim002"

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
            string generatedUsername = $"{baseName}{nextNumber:D3}"; // Format with leading zeros (e.g., 001, 012, 123)

            return new ApiResponse<string>
            {
                Success = true,
                Data = generatedUsername,
                Message = "Username generated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating username for firstName={FirstName}, preferredFirstName={PreferredFirstName}", firstName, preferredFirstName);

            return new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while generating username",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<string>> GenerateEmailAddressAsync(string firstName, string lastName, int companyCode, int payrollDeptCode, bool emailRequired, string? preferredFirstName = null, string? userId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(firstName))
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "First name is required to generate email address"
                };
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Last name is required to generate email address"
                };
            }

            // Get the email domain from PayrollDepartments table
            var payrollDepartment = await _context.PayrollDepartments
                .Where(pd => pd.CompanyCode == companyCode && pd.DeptCode == payrollDeptCode && !pd.IsDeleted)
                .FirstOrDefaultAsync();

            if (payrollDepartment == null || string.IsNullOrEmpty(payrollDepartment.EmailDomain))
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Email domain not found for Company {companyCode}, Department {payrollDeptCode}"
                };
            }

            string emailAddress;
            if (emailRequired)
            {
                // Email required: firstname.lastname@domain or preferred.lastname@domain
                // Remove spaces to handle names like "Mary Ann" -> "maryann"
                var namePrefix = !string.IsNullOrWhiteSpace(preferredFirstName)
                    ? preferredFirstName.Trim().ToLower().Replace(" ", "")
                    : firstName.Trim().ToLower().Replace(" ", "");
                emailAddress = $"{namePrefix}.{lastName.Trim().ToLower().Replace(" ", "")}@{payrollDepartment.EmailDomain}";
            }
            else
            {
                // No email required: userId@domain
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "User ID is required to generate default email address"
                    };
                }
                emailAddress = $"{userId.Trim().ToLower()}@{payrollDepartment.EmailDomain}";
            }

            return new ApiResponse<string>
            {
                Success = true,
                Data = emailAddress,
                Message = "Email address generated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating email address for firstName={FirstName}, companyCode={CompanyCode}, payrollDeptCode={PayrollDeptCode}", firstName, companyCode, payrollDeptCode);
            _ecmLogger.LogError(LogCategory.ReferenceDataSync, ex, "Error generating email address for firstName={FirstName}, companyCode={CompanyCode}, payrollDeptCode={PayrollDeptCode}", firstName, companyCode, payrollDeptCode);

            return new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while generating email address",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<ViewpointSyncStatusDto>> GetViewpointSyncStatusAsync()
    {
        try
        {
            var syncStatus = new ViewpointSyncStatusDto
            {
                // Get the latest sync dates from ViewpointSyncDate columns
                LastCompanySync = await _context.Companies
                    .Where(c => c.ViewpointSyncDate.HasValue)
                    .MaxAsync(c => (DateTime?)c.ViewpointSyncDate),

                LastDepartmentSync = await _context.PayrollDepartments
                    .Where(d => d.ViewpointSyncDate.HasValue)
                    .MaxAsync(d => (DateTime?)d.ViewpointSyncDate),

                LastPositionSync = await _context.Positions
                    .Where(p => p.ViewpointSyncDate.HasValue)
                    .MaxAsync(p => (DateTime?)p.ViewpointSyncDate),

                LastPayrollGroupSync = await _context.PayrollGroups
                    .Where(pg => pg.ViewpointSyncDate.HasValue)
                    .MaxAsync(pg => (DateTime?)pg.ViewpointSyncDate),

                LastUnionCraftSync = await _context.UnionCrafts
                    .Where(uc => uc.ViewpointSyncDate.HasValue)
                    .MaxAsync(uc => (DateTime?)uc.ViewpointSyncDate),

                LastEmploymentStatusSync = await _context.EmploymentStatuses
                    .Where(es => es.ViewpointSyncDate.HasValue)
                    .MaxAsync(es => (DateTime?)es.ViewpointSyncDate),

                LastEmployeeSalaryTypeSync = await _context.EmployeeSalaryTypes
                    .Where(est => est.ViewpointSyncDate.HasValue)
                    .MaxAsync(est => (DateTime?)est.ViewpointSyncDate),

                LastEmployeeSync = await _context.Employees
                    .Where(e => e.ViewpointSyncDate.HasValue)
                    .MaxAsync(e => (DateTime?)e.ViewpointSyncDate),

                // Get counts of each data type
                TotalCompanies = await _context.Companies.CountAsync(c => c.IsActive && !c.IsDeleted),
                TotalDepartments = await _context.PayrollDepartments.CountAsync(d => d.IsActive && !d.IsDeleted),
                TotalPositions = await _context.Positions.CountAsync(p => p.IsActive && !p.IsDeleted),
                TotalPayrollGroups = await _context.PayrollGroups.CountAsync(pg => pg.IsActive && !pg.IsDeleted),
                TotalUnionCrafts = await _context.UnionCrafts.CountAsync(uc => uc.IsActive && !uc.IsDeleted),
                TotalEmploymentStatuses = await _context.EmploymentStatuses.CountAsync(es => es.IsActive && !es.IsDeleted),
                TotalEmployeeSalaryTypes = await _context.EmployeeSalaryTypes.CountAsync(est => est.IsActive && !est.IsDeleted),
                TotalEmployees = await _context.Employees.CountAsync(e => !e.IsDeleted)
            };

            return new ApiResponse<ViewpointSyncStatusDto>
            {
                Success = true,
                Data = syncStatus,
                Message = "Viewpoint sync status retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Viewpoint sync status");

            return new ApiResponse<ViewpointSyncStatusDto>
            {
                Success = false,
                Message = "An error occurred while retrieving Viewpoint sync status",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<SyncScheduleConfigDto>> GetSyncScheduleConfigAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving sync schedule configuration");

            // Load configurations from database
            var scheduleConfigs = await _context.SyncScheduleConfigs
                .Where(s => s.IsActive && !s.IsDeleted)
                .ToListAsync();

            var config = new SyncScheduleConfigDto
            {
                Companies = GetScheduleValueFromConfigs(scheduleConfigs, "Companies"),
                Departments = GetScheduleValueFromConfigs(scheduleConfigs, "Departments"),
                Positions = GetScheduleValueFromConfigs(scheduleConfigs, "Positions"),
                PayrollGroups = GetScheduleValueFromConfigs(scheduleConfigs, "PayrollGroups"),
                UnionCrafts = GetScheduleValueFromConfigs(scheduleConfigs, "UnionCrafts"),
                Employees = GetScheduleValueFromConfigs(scheduleConfigs, "Employees"),
                LastUpdated = scheduleConfigs.Any() ? scheduleConfigs.Max(s => s.ModifiedDate ?? s.CreatedDate) : DateTime.UtcNow,
                UpdatedBy = scheduleConfigs.Any() ? scheduleConfigs.OrderByDescending(s => s.ModifiedDate ?? s.CreatedDate).First().ModifiedBy?.ToString() ?? "system" : "system"
            };

            return new ApiResponse<SyncScheduleConfigDto>
            {
                Success = true,
                Data = config,
                Message = "Sync schedule configuration retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sync schedule configuration");

            return new ApiResponse<SyncScheduleConfigDto>
            {
                Success = false,
                Message = "An error occurred while retrieving sync schedule configuration",
                Errors = [ex.Message]
            };
        }
    }

    public async Task<ApiResponse<SyncScheduleResultDto>> UpdateSyncScheduleConfigAsync(SyncScheduleConfigDto config)
    {
        try
        {
            _logger.LogInformation("Updating sync schedule configuration");

            var result = new SyncScheduleResultDto
            {
                Success = true,
                Message = "Sync schedule updated successfully"
            };

            // Save configuration to database
            await SaveSyncScheduleConfigToDatabase(config);

            // Clear existing recurring jobs
            ClearExistingScheduledJobs();

            // Set up new recurring jobs based on configuration
            await SetupRecurringJobs(config, result);
            
            _logger.LogInformation("Sync schedule configuration updated successfully. Scheduled jobs: {JobCount}", result.ScheduledJobs.Count);

            return new ApiResponse<SyncScheduleResultDto>
            {
                Success = true,
                Data = result,
                Message = "Sync schedule configuration updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sync schedule configuration");

            return new ApiResponse<SyncScheduleResultDto>
            {
                Success = false,
                Message = "An error occurred while updating sync schedule configuration",
                Errors = [ex.Message]
            };
        }
    }

    private void ClearExistingScheduledJobs()
    {
        var jobIds = new[]
        {
            "companies-sync",
            "departments-sync",
            "positions-sync",
            "payroll-groups-sync",
            "union-crafts-sync",
            "employees-sync"
        };

        foreach (var jobId in jobIds)
        {
            try
            {
                RecurringJob.RemoveIfExists(jobId);
                _logger.LogInformation("Removed existing recurring job: {JobId}", jobId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove recurring job: {JobId}", jobId);
            }
        }
    }

    private async Task SetupRecurringJobs(SyncScheduleConfigDto config, SyncScheduleResultDto result)
    {
        // Companies sync
        if (config.Companies != "disabled")
        {
            var cronExpression = GetCronExpression(config.Companies, "0 9 * * *");
            RecurringJob.AddOrUpdate("companies-sync",
                () => SyncCompaniesFromViewpointAsync(),
                cronExpression, TimeZoneInfo.Local);
            result.ScheduledJobs.Add($"Companies sync scheduled: {config.Companies}");
        }

        // Departments sync  
        if (config.Departments != "disabled")
        {
            var cronExpression = GetCronExpression(config.Departments, "5 9 * * *");
            RecurringJob.AddOrUpdate("departments-sync",
                () => SyncDepartmentsFromViewpointAsync(),
                cronExpression, TimeZoneInfo.Local);
            result.ScheduledJobs.Add($"Departments sync scheduled: {config.Departments}");
        }

        // Positions sync
        if (config.Positions != "disabled")
        {
            var cronExpression = GetCronExpression(config.Positions, "10 9 * * *");
            RecurringJob.AddOrUpdate("positions-sync",
                () => SyncPositionsFromViewpointAsync(),
                cronExpression, TimeZoneInfo.Local);
            result.ScheduledJobs.Add($"Positions sync scheduled: {config.Positions}");
        }

        // Payroll groups sync
        if (config.PayrollGroups != "disabled")
        {
            var cronExpression = GetCronExpression(config.PayrollGroups, "15 9 * * *");
            RecurringJob.AddOrUpdate("payroll-groups-sync",
                () => SyncPayrollGroupsFromViewpointAsync(),
                cronExpression, TimeZoneInfo.Local);
            result.ScheduledJobs.Add($"Payroll groups sync scheduled: {config.PayrollGroups}");
        }

        // Union crafts sync
        if (config.UnionCrafts != "disabled")
        {
            var cronExpression = GetCronExpression(config.UnionCrafts, "18 9 * * *");
            RecurringJob.AddOrUpdate("union-crafts-sync",
                () => SyncUnionCraftsFromViewpointAsync(),
                cronExpression, TimeZoneInfo.Local);
            result.ScheduledJobs.Add($"Union crafts sync scheduled: {config.UnionCrafts}");
        }

        // Employees sync (this one is more complex, so we'll schedule it but note it needs special handling)
        if (config.Employees != "disabled")
        {
            var cronExpression = GetCronExpression(config.Employees, "20 9 * * *");
            // Note: Employee sync is complex and requires special handling
            // For now, we'll just log that it would be scheduled
            result.ScheduledJobs.Add($"Employees sync scheduled: {config.Employees} (requires special handling)");
            _logger.LogInformation("Employee sync scheduled but requires custom implementation for background execution");
        }

        await Task.CompletedTask;
    }

    private string GetCronExpression(string interval, string defaultCron)
    {
        return interval switch
        {
            "daily" => defaultCron,
            "weekly" => defaultCron.Replace("* * *", "* * 1"), // Monday
            "monthly" => defaultCron.Replace("* * *", "1 * *"), // 1st of month
            _ => defaultCron
        };
    }

    private string GetScheduleValueFromConfigs(List<SyncScheduleConfig> configs, string syncType)
    {
        var config = configs.FirstOrDefault(c => c.SyncType == syncType);
        return config?.Schedule ?? "disabled";
    }

    private async Task SaveSyncScheduleConfigToDatabase(SyncScheduleConfigDto config)
    {
        var currentUserId = _userContextService.GetUserEmployeeNumber();

        var syncTypes = new Dictionary<string, string>
        {
            { "Companies", config.Companies },
            { "Departments", config.Departments },
            { "Positions", config.Positions },
            { "PayrollGroups", config.PayrollGroups },
            { "UnionCrafts", config.UnionCrafts },
            { "Employees", config.Employees }
        };

        foreach (var (syncType, schedule) in syncTypes)
        {
            var existingConfig = await _context.SyncScheduleConfigs
                .FirstOrDefaultAsync(s => s.SyncType == syncType && !s.IsDeleted);

            if (existingConfig != null)
            {
                // Update existing configuration
                existingConfig.Schedule = schedule;
                existingConfig.CronExpression = schedule != "disabled" ? GetCronExpression(schedule, "0 9 * * *") : null;
                existingConfig.ModifiedBy = currentUserId;
                existingConfig.ModifiedDate = DateTime.UtcNow;
                existingConfig.IsActive = schedule != "disabled";
            }
            else
            {
                // Create new configuration
                var newConfig = new SyncScheduleConfig
                {
                    SyncType = syncType,
                    Schedule = schedule,
                    CronExpression = schedule != "disabled" ? GetCronExpression(schedule, "0 9 * * *") : null,
                    IsActive = schedule != "disabled",
                    Description = $"Automatic sync schedule for {syncType}",
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.SyncScheduleConfigs.Add(newConfig);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Sync schedule configuration saved to database for user {UserId}", currentUserId);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Extensions;
using Mathy.ELM.Infrastructure.Data;
using System.Globalization;

namespace Mathy.ELM.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly MathyELMContext _context;
    private readonly IViewpointService _viewpointService;
    private readonly ILogger<EmployeeService> _logger;
    private readonly IRoleFilterService _roleFilterService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserContextService _userContextService;
    private readonly IEcmLogger _ecmLogger;

    public EmployeeService(
        MathyELMContext context,
        IViewpointService viewpointService,
        ILogger<EmployeeService> logger,
        IRoleFilterService roleFilterService,
        IHttpContextAccessor httpContextAccessor,
        IUserContextService userContextService,
        IEcmLogger ecmLogger)
    {
        _context = context;
        _viewpointService = viewpointService;
        _logger = logger;
        _roleFilterService = roleFilterService;
        _httpContextAccessor = httpContextAccessor;
        _userContextService = userContextService;
        _ecmLogger = ecmLogger;
    }

    public async Task<ApiResponse<List<EmployeeDto>>> SearchEmployeesAsync(string searchTerm, string? companyCode = null)
    {
        try
        {
            var query = _context.Employees
                .ApplyRoleBasedFilter(_roleFilterService, _httpContextAccessor, _userContextService, _context.PayrollDepartmentShortNames, _logger);

            if (!string.IsNullOrEmpty(companyCode) && int.TryParse(companyCode, out var companyCodeInt))
            {
                query = query.Where(e => e.CompanyCode == companyCodeInt);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                if (int.TryParse(searchTerm, out var employeeNumber))
                {
                    query = query.Where(e => e.EmployeeNumber == employeeNumber);
                }
                else
                {
                    query = query.Where(e => 
                        e.FirstName.ToLower().Contains(term) ||
                        e.LastName.ToLower().Contains(term) ||
                        (e.MiddleName != null && e.MiddleName.ToLower().Contains(term)) ||
                        (e.WorkEmail != null && e.WorkEmail.ToLower().Contains(term)) ||
                        (e.PersonalEmail != null && e.PersonalEmail.ToLower().Contains(term))
                    );
                }
            }

            var employees = await (
                from e in query.Take(20) // Limit results for autocomplete
                join c in _context.Companies on e.CompanyCode equals c.CompanyCode into companyGroup
                from c in companyGroup.DefaultIfEmpty()
                join s in _context.Employees on e.SupervisorId equals s.EmployeeNumber into supervisorGroup
                from s in supervisorGroup.DefaultIfEmpty()
                select new EmployeeDto
                {
                    EmployeeNumber = e.EmployeeNumber.ToString(),
                    EmployeeName = $"{e.FirstName} {e.MiddleName} {e.LastName}".Replace("  ", " ").Trim(),
                    CompanyCode = e.CompanyCode.ToString(),
                    CompanyName = c != null ? c.CompanyName : "",
                    Email = e.WorkEmail ?? e.PersonalEmail ?? "",
                    WorkEmail = e.WorkEmail ?? "",
                    Position = e.PositionCode ?? "",
                    Department = "", // TODO: Add Department lookup if needed
                    PayrollCompanyCode = e.PayrollCompanyCode,
                    PayrollGroupCode = e.PayrollGroupCode,
                    SupervisorId = e.SupervisorId,
                    Supervisor = s != null ? $"{s.FirstName} {s.LastName}".Trim() : null,
                    IsActive = string.IsNullOrEmpty(e.EmploymentStatus) || e.EmploymentStatus != "Terminated",
                    Status = e.EmploymentStatus,
                    SalaryCode = e.SalaryCode
                })
                .ToListAsync();

            return new ApiResponse<List<EmployeeDto>>
            {
                Success = true,
                Data = employees,
                Message = $"Found {employees.Count} employees"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching employees with term: {SearchTerm}", searchTerm);
            _ecmLogger.LogError(LogCategory.Database, "Error searching employees", ex);
            return new ApiResponse<List<EmployeeDto>>
            {
                Success = false,
                Message = "Error searching employees",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<EmployeeDto>> GetEmployeeByNumberAsync(string employeeNumber)
    {
        try
        {
            if (!int.TryParse(employeeNumber, out var empNum))
            {
                return new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "Invalid employee number format"
                };
            }

            var employee = await (
                from e in _context.Employees
                    .ApplyRoleBasedFilter(_roleFilterService, _httpContextAccessor, _userContextService, _context.PayrollDepartmentShortNames, _logger)
                    .Where(e => e.EmployeeNumber == empNum)
                join c in _context.Companies on e.CompanyCode equals c.CompanyCode into companyGroup
                from c in companyGroup.DefaultIfEmpty()
                join s in _context.Employees on e.SupervisorId equals s.EmployeeNumber into supervisorGroup
                from s in supervisorGroup.DefaultIfEmpty()
                select new EmployeeDto
                {
                    EmployeeNumber = e.EmployeeNumber.ToString(),
                    EmployeeName = $"{e.FirstName} {e.MiddleName} {e.LastName}".Replace("  ", " ").Trim(),
                    CompanyCode = e.CompanyCode.ToString(),
                    CompanyName = c != null ? c.CompanyName : "",
                    Email = e.WorkEmail ?? e.PersonalEmail ?? "",
                    WorkEmail = e.WorkEmail ?? "",
                    Position = e.PositionCode ?? "",
                    Department = "", // TODO: Add Department lookup if needed
                    PayrollCompanyCode = e.PayrollCompanyCode,
                    PayrollGroupCode = e.PayrollGroupCode,
                    SupervisorId = e.SupervisorId,
                    Supervisor = s != null ? $"{s.FirstName} {s.LastName}".Trim() : null,
                    IsActive = string.IsNullOrEmpty(e.EmploymentStatus) || e.EmploymentStatus != "Terminated",
                    Status = e.EmploymentStatus,
                    SalaryCode = e.SalaryCode
                })
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                return new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "Employee not found"
                };
            }

            return new ApiResponse<EmployeeDto>
            {
                Success = true,
                Data = employee
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee by number: {EmployeeNumber}", employeeNumber);
            return new ApiResponse<EmployeeDto>
            {
                Success = false,
                Message = "Error retrieving employee",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<PagedResponse<List<EmployeeDto>>> GetEmployeesByCompanyAsync(string companyCode, int page, int pageSize)
    {
        try
        {
            if (!int.TryParse(companyCode, out var companyCodeInt))
            {
                return new PagedResponse<List<EmployeeDto>>
                {
                    Success = false,
                    Message = "Invalid company code format",
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }

            var query = _context.Employees
                .ApplyRoleBasedFilter(_roleFilterService, _httpContextAccessor, _userContextService, _context.PayrollDepartmentShortNames, _logger)
                .Where(e => e.CompanyCode == companyCodeInt);

            var totalCount = await query.CountAsync();

            var employees = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EmployeeDto
                {
                    EmployeeNumber = e.EmployeeNumber.ToString(),
                    EmployeeName = $"{e.FirstName} {e.MiddleName} {e.LastName}".Replace("  ", " ").Trim(),
                    CompanyCode = e.CompanyCode.ToString(),
                    CompanyName = "", // TODO: Add Company lookup if needed
                    Email = e.WorkEmail ?? e.PersonalEmail ?? "",
                    WorkEmail = e.WorkEmail ?? "",
                    Position = e.PositionCode ?? "",
                    Department = "", // TODO: Add Department lookup if needed
                    PayrollCompanyCode = e.PayrollCompanyCode,
                    PayrollGroupCode = e.PayrollGroupCode,
                    IsActive = string.IsNullOrEmpty(e.EmploymentStatus) || e.EmploymentStatus != "Terminated",
                    Status = e.EmploymentStatus,
                    SalaryCode = e.SalaryCode
                })
                .ToListAsync();

            return new PagedResponse<List<EmployeeDto>>
            {
                Success = true,
                Data = employees,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Message = $"Retrieved {employees.Count} of {totalCount} employees"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees for company: {CompanyCode}", companyCode);
            return new PagedResponse<List<EmployeeDto>>
            {
                Success = false,
                Message = "Error retrieving employees",
                Errors = new List<string> { ex.Message },
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = 0
            };
        }
    }

    public async Task<ApiResponse<EmployeeSyncResultDto>> SyncEmployeesFromViewpointAsync(int page = 1, int pageSize = 100, string? filter = null)
    {
        var syncResult = new EmployeeSyncResultDto
        {
            SyncStartTime = DateTime.UtcNow,
            Page = page,
            PageSize = pageSize
        };

        try
        {
            _logger.LogInformation("Starting employee sync from Viewpoint. Page: {Page}, PageSize: {PageSize}, Filter: {Filter}",
                page, pageSize, filter);

            // Get employees from Viewpoint API
            var viewpointResponse = await _viewpointService.GetAllEmployeesAsync(page, pageSize, filter);

            if (viewpointResponse?.Data == null)
            {
                syncResult.SyncEndTime = DateTime.UtcNow;
                syncResult.Errors.Add("Failed to retrieve employees from Viewpoint API");
                _ecmLogger.LogViewpointIntegration(false, "EmployeeSync", "/api/v1/employees", 0, "Failed to retrieve employees from Viewpoint API");
                return new ApiResponse<EmployeeSyncResultDto>
                {
                    Success = false,
                    Data = syncResult,
                    Message = "Failed to retrieve employees from Viewpoint API"
                };
            }

            syncResult.TotalProcessed = viewpointResponse.Data.Count;
            syncResult.HasMore = viewpointResponse.HasMore;

            // Process each employee from Viewpoint
            foreach (var viewpointEmployee in viewpointResponse.Data)
            {
                try
                {
                    await ProcessEmployeeSync(viewpointEmployee, syncResult);
                }
                catch (Exception ex)
                {
                    syncResult.ErrorCount++;
                    syncResult.Errors.Add($"Error processing employee {viewpointEmployee.HRRef}: {ex.Message}");
                    _logger.LogError(ex, "Error processing employee {HRRef} during sync", viewpointEmployee.HRRef);
                }
            }

            // Save all changes
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update error during employee sync. InnerException: {InnerException}", 
                    dbEx.InnerException?.Message);
                
                syncResult.ErrorCount++;
                syncResult.Errors.Add($"Database update error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                
                // Don't re-throw, continue with partial results
            }

            syncResult.SyncEndTime = DateTime.UtcNow;

            _logger.LogInformation("Employee sync completed. Processed: {TotalProcessed}, Inserted: {InsertedCount}, Updated: {UpdatedCount}, Errors: {ErrorCount}",
                syncResult.TotalProcessed, syncResult.InsertedCount, syncResult.UpdatedCount, syncResult.ErrorCount);

            _ecmLogger.LogViewpointIntegration(true, "EmployeeSync", "/api/v1/employees", syncResult.TotalProcessed, null);

            return new ApiResponse<EmployeeSyncResultDto>
            {
                Success = true,
                Data = syncResult,
                Message = $"Sync completed. Processed: {syncResult.TotalProcessed}, Inserted: {syncResult.InsertedCount}, Updated: {syncResult.UpdatedCount}, Errors: {syncResult.ErrorCount}"
            };
        }
        catch (Exception ex)
        {
            syncResult.SyncEndTime = DateTime.UtcNow;
            syncResult.ErrorCount++;
            syncResult.Errors.Add($"Sync operation failed: {ex.Message}");

            _logger.LogError(ex, "Employee sync operation failed");
            _ecmLogger.LogViewpointIntegration(false, "EmployeeSync", "/api/v1/employees", 0, ex.Message);

            return new ApiResponse<EmployeeSyncResultDto>
            {
                Success = false,
                Data = syncResult,
                Message = $"Sync operation failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Full-sweep sync that fetches every Viewpoint employee in one call, upserts them,
    /// and then soft-deletes any ECM Employee row whose (CompanyCode, EmployeeNumber)
    /// is absent from the Viewpoint payload.
    /// Uses a single upfront <c>ToListAsync</c> to populate an in-memory dictionary
    /// for O(1) lookups, collapsing what would otherwise be N per-row DB round-trips
    /// into two (one read, one save).
    /// Use the paginated <see cref="SyncEmployeesFromViewpointAsync"/> when you need
    /// the RTW filter or page-by-page processing — deactivation does not run there.
    /// </summary>
    public async Task<ApiResponse<EmployeeSyncResultDto>> SyncAllEmployeesFromViewpointAsync()
    {
        var syncResult = new EmployeeSyncResultDto
        {
            SyncStartTime = DateTime.UtcNow,
            Page = 1,
            PageSize = int.MaxValue
        };

        try
        {
            _logger.LogInformation("Starting full-sweep employee sync from Viewpoint");

            // Load every existing employee once (including soft-deleted) so the upsert's
            // restore branch can still find them. Duplicates by natural key are coalesced
            // with .First() to match the historical FirstOrDefaultAsync behaviour.
            var existingByKey = (await _context.Employees.ToListAsync())
                .GroupBy(e => (e.CompanyCode, e.EmployeeNumber))
                .ToDictionary(g => g.Key, g => g.First());

            var viewpointResponse = await _viewpointService.GetAllEmployeesAsync(page: 1, pageSize: int.MaxValue, filter: null);

            if (viewpointResponse?.Data == null)
            {
                syncResult.SyncEndTime = DateTime.UtcNow;
                syncResult.Errors.Add("Failed to retrieve employees from Viewpoint API");
                _ecmLogger.LogViewpointIntegration(false, "EmployeeSyncAll", "/api/v1/employees", 0, "Failed to retrieve employees from Viewpoint API");
                return new ApiResponse<EmployeeSyncResultDto>
                {
                    Success = false,
                    Data = syncResult,
                    Message = "Failed to retrieve employees from Viewpoint API"
                };
            }

            syncResult.TotalProcessed = viewpointResponse.Data.Count;
            syncResult.HasMore = false;

            var processedKeys = new HashSet<(int CompanyCode, int EmployeeNumber)>();

            foreach (var viewpointEmployee in viewpointResponse.Data)
            {
                try
                {
                    if (!viewpointEmployee.HRRef.HasValue || !viewpointEmployee.HRCo.HasValue)
                    {
                        syncResult.ErrorCount++;
                        syncResult.Errors.Add($"Missing required employee data (HRRef or HRCo) for employee");
                        continue;
                    }

                    var key = (viewpointEmployee.HRCo.Value, viewpointEmployee.HRRef.Value);
                    existingByKey.TryGetValue(key, out var existingEmployee);

                    ApplyEmployeeUpsert(viewpointEmployee, existingEmployee, syncResult);
                    processedKeys.Add(key);
                }
                catch (Exception ex)
                {
                    syncResult.ErrorCount++;
                    syncResult.Errors.Add($"Error processing employee {viewpointEmployee.HRRef}: {ex.Message}");
                    _logger.LogError(ex, "Error processing employee {HRRef} during full sync", viewpointEmployee.HRRef);
                }
            }

            // Soft-delete ECM rows whose natural key was not present in the Viewpoint payload.
            // Iterates the in-memory dictionary — no second DB query. Mirrors the deactivation
            // pattern in ReferenceDataService (Companies/Departments).
            foreach (var existingEmployee in existingByKey.Values)
            {
                if (existingEmployee.IsDeleted)
                {
                    continue; // already soft-deleted; don't touch ModifiedDate
                }

                if (!processedKeys.Contains((existingEmployee.CompanyCode, existingEmployee.EmployeeNumber)))
                {
                    existingEmployee.IsDeleted = true;
                    existingEmployee.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                    existingEmployee.ModifiedDate = DateTime.UtcNow;
                    syncResult.DeletedCount++;

                    _logger.LogDebug("Soft-deleted missing employee: {CompanyCode}-{EmployeeNumber} ({Name})",
                        existingEmployee.CompanyCode, existingEmployee.EmployeeNumber,
                        $"{existingEmployee.FirstName} {existingEmployee.LastName}".Trim());
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update error during full employee sync. InnerException: {InnerException}",
                    dbEx.InnerException?.Message);

                syncResult.ErrorCount++;
                syncResult.Errors.Add($"Database update error: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }

            syncResult.SyncEndTime = DateTime.UtcNow;

            _logger.LogInformation("Full employee sync completed. Processed: {TotalProcessed}, Inserted: {InsertedCount}, Updated: {UpdatedCount}, Deleted: {DeletedCount}, Errors: {ErrorCount}",
                syncResult.TotalProcessed, syncResult.InsertedCount, syncResult.UpdatedCount, syncResult.DeletedCount, syncResult.ErrorCount);

            _ecmLogger.LogViewpointIntegration(true, "EmployeeSyncAll", "/api/v1/employees", syncResult.TotalProcessed,
                $"Inserted={syncResult.InsertedCount}, Updated={syncResult.UpdatedCount}, Deleted={syncResult.DeletedCount}, Errors={syncResult.ErrorCount}");

            return new ApiResponse<EmployeeSyncResultDto>
            {
                Success = true,
                Data = syncResult,
                Message = $"Full sync completed. Processed: {syncResult.TotalProcessed}, Inserted: {syncResult.InsertedCount}, Updated: {syncResult.UpdatedCount}, Deleted: {syncResult.DeletedCount}, Errors: {syncResult.ErrorCount}"
            };
        }
        catch (Exception ex)
        {
            syncResult.SyncEndTime = DateTime.UtcNow;
            syncResult.ErrorCount++;
            syncResult.Errors.Add($"Full sync operation failed: {ex.Message}");

            _logger.LogError(ex, "Full employee sync operation failed");
            _ecmLogger.LogViewpointIntegration(false, "EmployeeSyncAll", "/api/v1/employees", 0, ex.Message);

            return new ApiResponse<EmployeeSyncResultDto>
            {
                Success = false,
                Data = syncResult,
                Message = $"Full sync operation failed: {ex.Message}"
            };
        }
    }

    private async Task ProcessEmployeeSync(ViewpointEmployeeDto viewpointEmployee, EmployeeSyncResultDto syncResult)
    {
        if (!viewpointEmployee.HRRef.HasValue || !viewpointEmployee.HRCo.HasValue)
        {
            syncResult.ErrorCount++;
            syncResult.Errors.Add($"Missing required employee data (HRRef or HRCo) for employee");
            return;
        }

        // Find existing employee by company code and employee number (including soft-deleted)
        // Note: Don't apply role-based filter during sync to avoid unique constraint violations
        var existingEmployee = await _context.Employees
            .FirstOrDefaultAsync(e =>
                e.CompanyCode == viewpointEmployee.HRCo.Value &&
                e.EmployeeNumber == viewpointEmployee.HRRef.Value);

        ApplyEmployeeUpsert(viewpointEmployee, existingEmployee, syncResult);
    }

    /// <summary>
    /// Applies the insert / restore / update branch for a single Viewpoint employee
    /// against an already-resolved existing entity (or null for insert). Shared by
    /// the per-row paginated sync and the dictionary-based full sweep so they stay
    /// behaviorally identical.
    /// </summary>
    private void ApplyEmployeeUpsert(ViewpointEmployeeDto viewpointEmployee, Employee? existingEmployee, EmployeeSyncResultDto syncResult)
    {
        if (existingEmployee == null)
        {
            // Create new employee
            var newEmployee = MapViewpointToEmployee(viewpointEmployee);
            newEmployee.CreatedBy = _userContextService.GetUserEmployeeNumber();
            newEmployee.CreatedDate = DateTime.UtcNow;

            _context.Employees.Add(newEmployee);
            syncResult.InsertedCount++;

            _logger.LogDebug("Created new employee: {CompanyCode}-{EmployeeNumber}",
                newEmployee.CompanyCode, newEmployee.EmployeeNumber);
        }
        else if (existingEmployee.IsDeleted)
        {
            // Restore soft-deleted employee
            _logger.LogInformation("Restoring soft-deleted employee: {CompanyCode}-{EmployeeNumber}",
                existingEmployee.CompanyCode, existingEmployee.EmployeeNumber);

            // Update the employee data and restore it
            MapViewpointToEmployee(viewpointEmployee, existingEmployee);
            existingEmployee.IsDeleted = false; // Restore the employee
            existingEmployee.ModifiedBy = _userContextService.GetUserEmployeeNumber();
            existingEmployee.ModifiedDate = DateTime.UtcNow;
            existingEmployee.ViewpointSyncDate = DateTime.UtcNow;

            syncResult.UpdatedCount++;

            _logger.LogDebug("Restored and updated employee: {CompanyCode}-{EmployeeNumber}",
                existingEmployee.CompanyCode, existingEmployee.EmployeeNumber);
        }
        else
        {
            // Update existing employee
            MapViewpointToEmployee(viewpointEmployee, existingEmployee);
            existingEmployee.ModifiedBy = _userContextService.GetUserEmployeeNumber();
            existingEmployee.ModifiedDate = DateTime.UtcNow;
            existingEmployee.ViewpointSyncDate = DateTime.UtcNow;

            syncResult.UpdatedCount++;

            _logger.LogDebug("Updated existing employee: {CompanyCode}-{EmployeeNumber}",
                existingEmployee.CompanyCode, existingEmployee.EmployeeNumber);
        }
    }

    private Employee MapViewpointToEmployee(ViewpointEmployeeDto viewpointDto, Employee? existingEmployee = null)
    {
        var employee = existingEmployee ?? new Employee();

        // Map basic properties
        employee.CompanyCode = viewpointDto.HRCo ?? 0;
        employee.EmployeeNumber = viewpointDto.HRRef ?? 0;
        employee.FirstName = TruncateString(viewpointDto.FirstName ?? "", 30);
        employee.MiddleName = TruncateString(viewpointDto.MiddleName, 15);
        employee.LastName = TruncateString(viewpointDto.LastName ?? "", 30);
        employee.PersonalEmail = TruncateString(viewpointDto.Email, 255);
        employee.WorkEmail = TruncateString(viewpointDto.CustomFields?.WorkEmail, 255);
        employee.NetworkId = TruncateString(viewpointDto.CustomFields?.NetworkUserID, 255);

        // Map payroll information
        employee.PayrollCompanyCode = viewpointDto.PRCo;
        employee.PayrollGroupCode = viewpointDto.PRGroup;
        
        // Parse PRDept - it might be string or int
        if (viewpointDto.PRDept != null && int.TryParse(viewpointDto.PRDept, out var deptCode))
        {
            employee.PayrollDeptCode = deptCode;
        }

        employee.PositionCode = TruncateString(viewpointDto.PositionCode, 10);

        // Map location and department codes from custom fields
        if (viewpointDto.CustomFields?.PhysicalLocation.HasValue == true)
        {
            var physLocationStr = viewpointDto.CustomFields.GetStringValue(viewpointDto.CustomFields.PhysicalLocation);
            if (int.TryParse(physLocationStr, out var physLocation))
            {
                employee.PhysicalLocationCode = physLocation;
            }
        }

        if (viewpointDto.CustomFields?.Function.HasValue == true)
        {
            var functionStr = viewpointDto.CustomFields.GetStringValue(viewpointDto.CustomFields.Function);
            if (int.TryParse(functionStr, out var funcDept))
            {
                employee.FunctionalDeptCode = funcDept;
            }
        }

        // Map supervisor information
        if (viewpointDto.CustomFields?.SupervisorId.HasValue == true)
        {
            var supervisorIdStr = viewpointDto.CustomFields.GetStringValue(viewpointDto.CustomFields.SupervisorId);
            if (int.TryParse(supervisorIdStr, out var supervisorId))
            {
                employee.SupervisorId = supervisorId;
            }
        }

        // Map phone information
        employee.WorkPhoneNumber = TruncateString(viewpointDto.WorkPhone, 50);

        // Map work extension and cell from CustomFields
        if (viewpointDto.CustomFields != null)
        {
            // Map work extension
            if (viewpointDto.CustomFields.WorkExt.HasValue && viewpointDto.CustomFields.WorkExt.Value.ValueKind != System.Text.Json.JsonValueKind.Null)
            {
                var workExtStr = viewpointDto.CustomFields.WorkExt.Value.GetString();
                employee.WorkExtension = TruncateString(workExtStr, 50);
            }

            // Map work cell
            employee.WorkCell = TruncateString(viewpointDto.CustomFields.WorkCell, 50);
        }

        // Map employment status and dates
        employee.EmploymentStatus = TruncateString(viewpointDto.Status, 10);

        // Map salary code (EarnCode from Viewpoint)
        employee.SalaryCode = viewpointDto.EarnCode;

        // Parse and set termination date if present
        if (!string.IsNullOrEmpty(viewpointDto.TermDate))
        {
            if (DateTime.TryParse(viewpointDto.TermDate, out var termDate))
            {
                employee.TerminationDate = termDate;
            }
        }

        // Set termination reason
        employee.TerminationReasonCode = TruncateString(viewpointDto.TermReason, 20);

        // Parse return to work date from custom fields
        if (viewpointDto.CustomFields?.ReturnToWorkDate.HasValue == true)
        {
            var rtwDateStr = viewpointDto.CustomFields.GetStringValue(viewpointDto.CustomFields.ReturnToWorkDate);
            if (DateTime.TryParse(rtwDateStr, out var rtwDate))
            {
                employee.ReturnToWorkDate = rtwDate;
            }
        }

        // Set sync date
        employee.ViewpointSyncDate = DateTime.UtcNow;

        // Ensure required fields are not empty
        if (string.IsNullOrWhiteSpace(employee.FirstName))
        {
            employee.FirstName = "Unknown";
        }
        if (string.IsNullOrWhiteSpace(employee.LastName))
        {
            employee.LastName = "Employee";
        }

        return employee;
    }

    /// <summary>
    /// Truncates a string to the specified maximum length
    /// </summary>
    private static string? TruncateString(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;
            
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    public async Task<PagedResponse<List<EmployeeDto>>> GetEmployeesByHRRequestAsync(string requestType, int page = 1, int pageSize = 25, string? orderBy = null, bool orderByDesc = false, string? search = null, bool isEditMode = false, int[]? employeeIds = null)
    {
        var employmentStatuses = await MapRequestTypeToEmploymentStatusAsync(requestType);
        return await GetEmployeesByStatusAsync(employmentStatuses, page, pageSize, orderBy, orderByDesc, search, isEditMode, employeeIds);
    }

    /// <summary>
    /// Maps request type to employment statuses from the database based on user's roles
    /// Queries EmploymentStatuses table filtered by Notes and user's authorized company codes
    /// </summary>
    private async Task<string[]> MapRequestTypeToEmploymentStatusAsync(string requestType)
    {
        // Map request type to Notes filter value
        List<string> notesFilter = requestType.ToLower() switch
        {
            "layoff-request" => ["ACTIVE", "UNION ACTIVE"],
            "active" => ["ACTIVE","UNION ACTIVE"],
            "termination-request" => ["ACTIVE","UNION ACTIVE"],
            "promotion-request" => ["ACTIVE","UNION ACTIVE"],
            "return-to-work" => ["LAYOFF","UNION LAYOFF"],
            _ => throw new ArgumentException($"Unsupported request type: {requestType}")
        };

        // Get company codes from user's selected roles
        var companyCodes = await GetUserCompanyCodesAsync();

        _logger.LogInformation("MapRequestTypeToEmploymentStatusAsync - RequestType: {RequestType}, NotesFilter: {NotesFilter}, CompanyCodes: [{CompanyCodes}]",
            requestType, notesFilter, companyCodes != null ? string.Join(", ", companyCodes) : "ALL");

        // Query EmploymentStatuses table
        var query = _context.EmploymentStatuses
            .Where(es => !es.IsDeleted && es.IsActive && notesFilter.Contains(es.Notes));

        // Apply company code filter if not admin (companyCodes will be null for admins)
        if (companyCodes != null && companyCodes.Length > 0)
        {
            query = query.Where(es => companyCodes.Contains(es.CompanyCode));
        }

        var statuses = await query
            .Select(es => es.Status)
            .Distinct()
            .ToArrayAsync();

        _logger.LogInformation("MapRequestTypeToEmploymentStatusAsync - Found {Count} statuses: [{Statuses}]",
            statuses.Length, string.Join(", ", statuses));

        return statuses;
    }

    /// <summary>
    /// Gets company codes from user's selected roles
    /// Returns null if user is ECM_ADMIN (no filtering)
    /// Priority: X-Selected-Role header > JWT claims roles
    /// </summary>
    private async Task<int[]?> GetUserCompanyCodesAsync()
    {
        string[] selectedRoles;

        // First try to get roles from X-Selected-Role header (user explicitly selected in UI)
        var httpContext = _httpContextAccessor.HttpContext;
        var selectedRolesHeader = httpContext?.Request.Headers["X-Selected-Role"].FirstOrDefault();

        if (!string.IsNullOrEmpty(selectedRolesHeader))
        {
            // Parse comma-separated roles from header
            selectedRoles = selectedRolesHeader.Split(',')
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r))
                .ToArray();

            _logger.LogDebug("Using roles from X-Selected-Role header: [{Roles}]", string.Join(", ", selectedRoles));
        }
        else
        {
            // Fallback to JWT claims roles
            var userRoles = _userContextService.GetUserRoles();
            if (userRoles == null || !userRoles.Any())
            {
                _logger.LogDebug("No roles found in header or JWT claims - no filtering applied");
                return null;
            }

            selectedRoles = userRoles.ToArray();
            _logger.LogDebug("Using roles from JWT claims: [{Roles}]", string.Join(", ", selectedRoles));
        }

        if (!selectedRoles.Any())
            return null;

        // Check if ECM_ADMIN is in the selected roles - no filtering needed
        if (selectedRoles.Any(r => r.Equals("ECM_ADMIN", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug("ECM_ADMIN role detected - returning all company codes (no filtering)");
            return null;
        }

        // Extract company codes from PayrollDepartmentShortNames table
        var companyCodes = await _context.PayrollDepartmentShortNames
            .Where(p => !p.IsDeleted && selectedRoles.Contains(p.DeptShortName))
            .Select(p => p.CompanyCode)
            .Distinct()
            .ToArrayAsync();

        _logger.LogDebug("Extracted company codes from roles: [{CompanyCodes}]", string.Join(", ", companyCodes));

        return companyCodes;
    }

    private bool IsSearchOnlyEmployeeIds(string search)
    {
        var searchParts = search.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return searchParts.All(part => int.TryParse(part.Trim(), out _));
    }

    [Obsolete("Use GetEmployeesByHRRequestAsync with requestType 'return-to-work' instead")]
    public async Task<PagedResponse<List<EmployeeDto>>> GetEmployeesByLayoffAsync(int page = 1, int pageSize = 25, string? orderBy = null, bool orderByDesc = false, string? search = null, bool isEditMode = false)
    {
        return await GetEmployeesByHRRequestAsync("return-to-work", page, pageSize, orderBy, orderByDesc, search, isEditMode, null);
    }

    public async Task<PagedResponse<List<EmployeeDto>>> GetEmployeesByActiveAsync(int page = 1, int pageSize = 25, string? orderBy = null, bool orderByDesc = false, string? search = null)
    {
        return await GetEmployeesByHRRequestAsync("active", page, pageSize, orderBy, orderByDesc, search, false, null);
    }

    private async Task<PagedResponse<List<EmployeeDto>>> GetEmployeesByStatusAsync(string[] employmentStatuses, int page = 1, int pageSize = 25, string? orderBy = null, bool orderByDesc = false, string? search = null, bool isEditMode = false, int[]? employeeIds = null)
    {
        try
        {
            var baseQuery = _context.Employees
                .Where(e => !e.IsDeleted && (isEditMode || employmentStatuses.Contains(e.EmploymentStatus)))
                .ApplyRoleBasedFilter(_roleFilterService, _httpContextAccessor, _userContextService, _context.PayrollDepartmentShortNames, _logger)
                .Join(_context.Companies,
                    employee => employee.CompanyCode,
                    company => company.CompanyCode,
                    (employee, company) => new { Employee = employee, Company = company })
                .Where(joined => !joined.Company.IsDeleted);

            // Apply employee ID filter if provided (when isEditMode is true)
            if (isEditMode && employeeIds != null && employeeIds.Length > 0)
            {
                baseQuery = baseQuery.Where(joined => employeeIds.Contains(joined.Employee.EmployeeNumber));
            }

            var query = baseQuery.Select(joined => new
                {
                    joined.Employee,
                    joined.Company,
                    Department = _context.PayrollDepartments
                        .Where(pd => !pd.IsDeleted
                            && pd.CompanyCode == joined.Employee.CompanyCode
                            && pd.DeptCode == (joined.Employee.PayrollDeptCode ?? 0))
                        .FirstOrDefault(),
                    Position = _context.Positions
                        .Where(p => p.CompanyCode == joined.Employee.CompanyCode
                            && p.PositionCode == joined.Employee.PositionCode)
                        .FirstOrDefault(),
                    Supervisor = _context.Employees
                        .Where(s => s.EmployeeNumber == joined.Employee.SupervisorId && !s.IsDeleted)
                        .FirstOrDefault(),
                    // Check if employee has existing HR request
                    HasExistingHRRequest = _context.HRRequestDetails
                        .Any(hrd => hrd.EmployeeId == joined.Employee.EmployeeNumber && !hrd.IsDeleted
                         && hrd.RequestStatus.RequestStatusName.ToLower() != "cancelled"
                         && hrd.RequestStatus.RequestStatusName.ToLower() != "completed"
                         && hrd.RequestStatus.RequestStatusName.ToLower() != "failed")
                }
            );

            // Parse search parameter for employee IDs when isEditMode is true
            if (isEditMode && !string.IsNullOrWhiteSpace(search) && employeeIds == null)
            {
                // Try to parse search as comma-separated employee IDs
                var searchParts = search.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var parsedEmployeeIds = new List<int>();
                
                foreach (var part in searchParts)
                {
                    if (int.TryParse(part.Trim(), out var employeeId))
                    {
                        parsedEmployeeIds.Add(employeeId);
                    }
                }
                
                if (parsedEmployeeIds.Count > 0)
                {
                    query = query.Where(joined => parsedEmployeeIds.Contains(joined.Employee.EmployeeNumber));
                }
            }
            // Apply search filter if provided (for normal text search)
            // Skip if we're in edit mode and search contains only numeric values (employee IDs)
            if (!string.IsNullOrWhiteSpace(search) && !(isEditMode && employeeIds == null && IsSearchOnlyEmployeeIds(search)))
            {
                var searchTerm = search.ToLower();
                
                // Split search term by spaces and create OR conditions for each word
                var searchWords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                if (searchWords.Length > 0)
                {
                    // Pre-parse numeric values outside of LINQ expression
                    var numericWords = searchWords
                        .Where(word => int.TryParse(word, out _))
                        .Select(word => int.Parse(word))
                        .ToList();
                    
                    query = query.Where(joined => 
                        searchWords.Any(word =>
                            // Text-based searches (contains)
                            (joined.Employee.FirstName != null && joined.Employee.FirstName.ToLower().Contains(word)) ||
                            (joined.Employee.MiddleName != null && joined.Employee.MiddleName.ToLower().Contains(word)) ||
                            (joined.Employee.LastName != null && joined.Employee.LastName.ToLower().Contains(word)) ||
                            joined.Employee.EmployeeNumber.ToString().Contains(word) || // Partial match for employee number as string
                            joined.Employee.CompanyCode.ToString().Contains(word) || // Partial match for company code string input
                            (joined.Company.CompanyName != null && joined.Company.CompanyName.ToLower().Contains(word)) || // Search by company name
                            (joined.Employee.PayrollDeptCode.HasValue && joined.Employee.PayrollDeptCode.Value.ToString().Contains(word)) || // Partial match for PayrollDeptCode string input
                            (joined.Department != null && joined.Department.DeptName != null && joined.Department.DeptName.ToLower().Contains(word)) || // Search by department name
                            (joined.Employee.PositionCode != null && joined.Employee.PositionCode.ToLower().Contains(word)) ||
                            (joined.Position != null && joined.Position.PositionName != null && joined.Position.PositionName.ToLower().Contains(word))
                        ) ||
                        // Numeric exact matches (processed separately)
                        numericWords.Any(numericValue =>
                            joined.Employee.EmployeeNumber == numericValue ||
                            joined.Employee.CompanyCode == numericValue ||
                            (joined.Employee.PayrollDeptCode.HasValue && joined.Employee.PayrollDeptCode.Value == numericValue)
                        )
                    );
                }
            }

            var totalCount = await query.CountAsync();

            // Apply ordering and pagination based on edit mode
            var finalQuery = query;
            
            if (!isEditMode)
            {
                // Apply ordering based on parameters
                finalQuery = orderBy?.ToLower() switch
                {
                    "employeename" or "name" => orderByDesc 
                        ? query.OrderByDescending(joined => joined.Employee.FirstName).ThenByDescending(joined => joined.Employee.LastName)
                        : query.OrderBy(joined => joined.Employee.FirstName).ThenBy(joined => joined.Employee.LastName),
                    "employeenumber" => orderByDesc 
                        ? query.OrderByDescending(joined => joined.Employee.EmployeeNumber)
                        : query.OrderBy(joined => joined.Employee.EmployeeNumber),
                    "companycode" => orderByDesc 
                        ? query.OrderByDescending(joined => joined.Employee.CompanyCode)
                        : query.OrderBy(joined => joined.Employee.CompanyCode),
                    "position" => orderByDesc 
                        ? query.OrderByDescending(joined => joined.Position != null ? joined.Position.PositionName ?? string.Empty : string.Empty)
                        : query.OrderBy(joined => joined.Position != null ? joined.Position.PositionName ?? string.Empty : string.Empty),
                    "division" => orderByDesc 
                        ? query.OrderByDescending(joined => joined.Department != null ? joined.Department.DeptName ?? string.Empty : string.Empty)
                        : query.OrderBy(joined => joined.Department != null ? joined.Department.DeptName ?? string.Empty : string.Empty),
                    _ => query.OrderBy(joined => joined.Employee.FirstName ?? string.Empty)
                        .ThenBy(joined => joined.Employee.LastName ?? string.Empty)
                        .ThenBy(joined => joined.Employee.CompanyCode)
                        .ThenBy(joined => joined.Employee.PayrollDeptCode ?? 0) // Default ordering
                };

                // Apply pagination
                finalQuery = finalQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);
            }
            else
            {
                // For edit mode (layoff only), apply ordering but not pagination
                finalQuery = orderBy?.ToLower() switch
                {
                    "employeename" or "name" => orderByDesc 
                        ? query.OrderByDescending(joined => joined.Employee.FirstName).ThenByDescending(joined => joined.Employee.LastName)
                        : query.OrderBy(joined => joined.Employee.FirstName).ThenBy(joined => joined.Employee.LastName),
                    "employeenumber" => orderByDesc 
                        ? query.OrderByDescending(joined => joined.Employee.EmployeeNumber)
                        : query.OrderBy(joined => joined.Employee.EmployeeNumber),
                    "companycode" => orderByDesc 
                        ? query.OrderByDescending(joined => joined.Employee.CompanyCode)
                        : query.OrderBy(joined => joined.Employee.CompanyCode),
                    "position" => orderByDesc 
                        ? query.OrderByDescending(joined => joined.Position != null ? joined.Position.PositionName ?? string.Empty : string.Empty)
                        : query.OrderBy(joined => joined.Position != null ? joined.Position.PositionName ?? string.Empty : string.Empty),
                    "division" => orderByDesc 
                        ? query.OrderByDescending(joined => joined.Department != null ? joined.Department.DeptName ?? string.Empty : string.Empty)
                        : query.OrderBy(joined => joined.Department != null ? joined.Department.DeptName ?? string.Empty : string.Empty),
                    _ => query.OrderBy(joined => joined.Employee.FirstName ?? string.Empty)
                        .ThenBy(joined => joined.Employee.LastName ?? string.Empty)
                        .ThenBy(joined => joined.Employee.CompanyCode)
                        .ThenBy(joined => joined.Employee.PayrollDeptCode ?? 0) // Default ordering
                };
            }

            var employees = await finalQuery
                .Select(joined => new EmployeeDto
                {
                    EmployeeNumber = joined.Employee.EmployeeNumber.ToString(),
                    EmployeeName = $"{joined.Employee.FirstName} {joined.Employee.MiddleName} {joined.Employee.LastName}".Replace("  ", " ").Trim(),
                    CompanyCode = joined.Employee.CompanyCode.ToString(),
                    CompanyName = joined.Company.CompanyName ?? "",
                    DivisionCode = joined.Employee.PayrollDeptCode.ToString(),
                    DivisionName = joined.Department != null ? joined.Department.DeptName ?? "" : "",
                    Email = joined.Employee.WorkEmail ?? joined.Employee.PersonalEmail ?? "",
                    WorkEmail = joined.Employee.WorkEmail ?? "",
                    Position = joined.Position != null ? joined.Position.PositionName ?? "" : "",
                    Department = "", // TODO: Add Department lookup if needed
                    PayrollCompanyCode = joined.Employee.PayrollCompanyCode,
                    PayrollDeptCode = joined.Employee.PayrollDeptCode,
                    PayrollGroupCode = joined.Employee.PayrollGroupCode,
                    PhysicalLocationCode = joined.Employee.PhysicalLocationCode,
                    IsActive = employmentStatuses.Contains("U-ACTIVE"),
                    HasExistingHRRequest = joined.HasExistingHRRequest,
                    Status = joined.Employee.EmploymentStatus,
                    SalaryCode = joined.Employee.SalaryCode,
                    SupervisorId = joined.Employee.SupervisorId,
                    Supervisor = joined.Supervisor != null ? $"{joined.Supervisor.FirstName} {joined.Supervisor.LastName}".Trim() : null
                })
                .ToListAsync();

            var statusDescription = employmentStatuses.Contains("U-LAYOFF") && !employmentStatuses.Any(s => s != "U-LAYOFF") ? "laid-off" : "active";
            
            return new PagedResponse<List<EmployeeDto>>
            {
                Success = true,
                Data = employees,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                OrderBy = orderBy,
                OrderByDesc = orderByDesc,
                Message = $"Retrieved {employees.Count} of {totalCount} {statusDescription} employees"
            };
        }
        catch (Exception ex)
        {
            var statusDescription = employmentStatuses.Contains("U-LAYOFF") && !employmentStatuses.Any(s => s != "U-LAYOFF") ? "laid-off" : "active";
            _logger.LogError(ex, "Error retrieving {StatusDescription} employees for page: {Page}, pageSize: {PageSize}, orderBy: {OrderBy}", statusDescription, page, pageSize, orderBy);
            return new PagedResponse<List<EmployeeDto>>
            {
                Success = false,
                Message = $"Error retrieving {statusDescription} employees",
                Errors = new List<string> { ex.Message },
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = 0,
                OrderBy = orderBy,
                OrderByDesc = orderByDesc
            };
        }
    }

    public async Task<ApiResponse<bool>> UpdateEmploymentStatusAsync(int employeeNumber, string employmentStatus)
    {
        try
        {
            var employee = await _context.Employees
                .ApplyRoleBasedFilter(_roleFilterService, _httpContextAccessor, _userContextService, _context.PayrollDepartmentShortNames, _logger)
                .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber);

            if (employee == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Employee with number {employeeNumber} not found"
                };
            }

            employee.EmploymentStatus = employmentStatus;
            employee.ModifiedBy = _userContextService.GetUserEmployeeNumber();
            employee.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated employment status for employee {EmployeeNumber} to {EmploymentStatus}", 
                employeeNumber, employmentStatus);

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = $"Employment status updated successfully for employee {employeeNumber}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employment status for employee {EmployeeNumber} to {EmploymentStatus}", 
                employeeNumber, employmentStatus);
            
            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Error updating employment status: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
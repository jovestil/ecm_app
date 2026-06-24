using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Interfaces;

public interface IEmployeeService
{
    Task<ApiResponse<List<EmployeeDto>>> SearchEmployeesAsync(string searchTerm, string? companyCode = null);
    Task<ApiResponse<EmployeeDto>> GetEmployeeByNumberAsync(string employeeNumber);
    Task<PagedResponse<List<EmployeeDto>>> GetEmployeesByCompanyAsync(string companyCode, int page, int pageSize);
    Task<PagedResponse<List<EmployeeDto>>> GetEmployeesByHRRequestAsync(string requestType, int page = 1, int pageSize = 25, string? orderBy = null, bool orderByDesc = false, string? search = null, bool isEditMode = false, int[]? employeeIds = null);
    [Obsolete("Use GetEmployeesByHRRequestAsync with requestType 'return-to-work' instead")]
    Task<PagedResponse<List<EmployeeDto>>> GetEmployeesByLayoffAsync(int page = 1, int pageSize = 25, string? orderBy = null, bool orderByDesc = false, string? search = null, bool isEditMode = false);
    Task<PagedResponse<List<EmployeeDto>>> GetEmployeesByActiveAsync(int page = 1, int pageSize = 25, string? orderBy = null, bool orderByDesc = false, string? search = null);
    Task<ApiResponse<EmployeeSyncResultDto>> SyncEmployeesFromViewpointAsync(int page = 1, int pageSize = 100, string? filter = null);
    Task<ApiResponse<EmployeeSyncResultDto>> SyncAllEmployeesFromViewpointAsync();
    Task<ApiResponse<bool>> UpdateEmploymentStatusAsync(int employeeNumber, string employmentStatus);
}
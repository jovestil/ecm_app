using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Interfaces;

public interface IViewpointService
{
    Task<ViewpointEmployeesResponse?> GetAllEmployeesAsync(int page = 1, int pageSize = 25, string? filter = null);
    Task<List<ViewpointCompanyDto>> GetAllCompaniesAsync();
    Task<List<ViewpointPayrollGroupDto>> GetAllPayrollGroupsAsync();
    Task<List<ViewpointDepartmentDto>> GetAllDepartmentsAsync();
    Task<List<ViewpointPositionDto>> GetAllPositionsAsync();
    Task<List<ViewpointCraftDto>> GetAllCraftsAsync();
    Task<List<ViewpointEmploymentStatusDto>> GetAllEmploymentStatusesAsync();
    Task<List<ViewpointEmployeeSalaryTypeDto>> GetAllEarningCodesAsync();
    Task<List<ViewpointPREHEmployeeDto>> GetPREHEmployeesAsync();
    Task<ViewpointEmployeeDto?> GetEmployeeByNumberAsync(string employeeNumber);
    Task<List<ViewpointEmployeeDto>> SearchEmployeesAsync(int hrRef);
    Task<List<ViewpointEmployeeDto>> SearchEmployeeInNewHireWithAPIAsync(int HRCo, string PRDept, string LastName, string HireDate);
    Task<ViewpointEmployeeDto?> GetEmployeeByEmailAsync(string emailAddress);
    Task<bool> UpdateEmployeeFromViewpointForReturnToWorkAsync(List<ViewpointEmployeeDto> employees);
    Task<ViewpointUpdateResult> UpdateEmployeeStatusInViewpointAsync(List<ViewpointEmployeeDto> employees, string status, Enums.RequestType? requestType = null);
    Task<UpdateEmployeeNewHireResultDto> UpdateEmployeeForNewHireInViewPointAsync(UpdateEmployeeNewHireRequestDto request);
    Task<ViewpointUpdateResult> UpdateEmployeeForPromotionTransferInViewPointAsync(ViewpointEmployeeDto employee);
    Task<ViewpointUpdateResult> UpdateEmployeeForTerminationInViewPointAsync(ViewpointEmployeeDto employee, DateTime? termDate, string? termReason);
    Task<ViewpointUpdateResult> UpdateEmployeeForReturnToWorkInViewPointAsync(ViewpointEmployeeDto employee, DateTime? returnToWorkDate);
    Task<ViewpointActionDetailResponseDto?> VerifyViewpointActionAsync(string actionId);
}
using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Interfaces;

public interface INewHireRequestDetailsService
{
    Task<ApiResponse<NewHireRequestDetailDto>> CreateNewHireRequestDetailsAsync(
        int hrRequestDetailId,
        CreateNewHireRequestDto newHireData,
        string? networkId = null,
        string? workEmail = null,
        string? adPassword = null);

    Task<ApiResponse<NewHireRequestDetailDto>> GetByHRRequestDetailIdAsync(int hrRequestDetailId);

    Task<ApiResponse<NewHireRequestViewDto>> GetNewHireRequestViewByParentIdAsync(int parentRequestId);

    Task<ApiResponse<List<HRRequestDetailDto>>> SaveNewHireRequestAsDraftAsync(CreateNewHireRequestDto request);

    Task<ApiResponse<List<HRRequestDetailDto>>> UpdateNewHireRequestAsDraftAsync(int parentRequestId, CreateNewHireRequestDto request);

    Task<ApiResponse<List<HRRequestDetailDto>>> UpdateNewHireRequestAsync(int parentRequestId, CreateNewHireRequestDto request);

    Task<(bool success, string? username, string? email, string? password)> CreateUserInADOU(
        int companyCode,
        int payrollDeptCode,
        string? preferredFirstName,
        string firstName,
        string lastName,
        string? middleInitial = null,
        string? title = null,
        string? department = null,
        string? preGeneratedEmail = null);

    Task<bool> AddUserToADGroup(
        int companyCode,
        string username,
        string groupName);

    Task<bool> DeleteUserFromAD(
        string username,
        int companyCode);
}
using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Interfaces;

public interface ITerminationRequestDetailsService
{
    Task<ApiResponse<List<int>>> CreateTerminationRequestDetailsAsync(List<int> hrRequestDetailIds, CreateTerminationRequestDto? terminationDetails = null);
    Task<ApiResponse<TerminationRequestDetailDto>> GetByHRRequestDetailIdAsync(int hrRequestDetailId);
    Task<ApiResponse<bool>> DeleteByHRRequestDetailIdsAsync(List<int> hrRequestDetailIds);
}
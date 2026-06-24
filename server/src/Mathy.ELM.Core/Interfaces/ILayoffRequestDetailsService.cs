using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Interfaces;

public interface ILayoffRequestDetailsService
{
    Task<ApiResponse<List<int>>> CreateLayoffRequestDetailsAsync(List<int> hrRequestDetailIds);
    Task<ApiResponse<LayoffRequestDetailDto>> GetByHRRequestDetailIdAsync(int hrRequestDetailId);
    Task<ApiResponse<bool>> DeleteByHRRequestDetailIdsAsync(List<int> hrRequestDetailIds);
}
using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Interfaces;

public interface IReturnToWorkRequestDetailsService
{
    /// <summary>
    /// Create return to work request details for multiple HR request details
    /// </summary>
    /// <param name="hrRequestDetailIds">List of HR request detail IDs</param>
    /// <returns>Success indicator with created detail IDs</returns>
    Task<ApiResponse<List<int>>> CreateReturnToWorkRequestDetailsAsync(List<int> hrRequestDetailIds);

    /// <summary>
    /// Get return to work request detail by HR request detail ID
    /// </summary>
    /// <param name="hrRequestDetailId">HR request detail ID</param>
    /// <returns>Return to work request detail</returns>
    Task<ApiResponse<ReturnToWorkRequestDetailDto>> GetByHRRequestDetailIdAsync(int hrRequestDetailId);

    /// <summary>
    /// Delete return to work request details by HR request detail IDs (for rollback)
    /// </summary>
    /// <param name="hrRequestDetailIds">List of HR request detail IDs</param>
    /// <returns>Success indicator</returns>
    Task<ApiResponse<bool>> DeleteByHRRequestDetailIdsAsync(List<int> hrRequestDetailIds);
}
using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Interfaces;

public interface IPromotionRequestDetailsService
{
    Task<ApiResponse<PromotionRequestDetailDto>> CreatePromotionRequestDetailsAsync(
        int hrRequestDetailId,
        CreatePromotionRequestDto promotionData,
        string? currentNetworkId = null);

    Task<ApiResponse<PromotionRequestDetailDto>> GetByHRRequestDetailIdAsync(int hrRequestDetailId);

    Task<ApiResponse<PromotionRequestViewDto>> GetPromotionRequestViewByParentIdAsync(int parentRequestId);

    Task<ApiResponse<List<HRRequestDetailDto>>> SavePromotionRequestAsDraftAsync(CreatePromotionRequestDto request);

    Task<ApiResponse<List<HRRequestDetailDto>>> UpdatePromotionRequestAsDraftAsync(int parentRequestId, CreatePromotionRequestDto request);

    Task<ApiResponse<List<HRRequestDetailDto>>> UpdatePromotionRequestAsync(int parentRequestId, CreatePromotionRequestDto request);
}

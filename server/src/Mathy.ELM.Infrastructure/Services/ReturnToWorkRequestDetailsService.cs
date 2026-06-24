using Microsoft.EntityFrameworkCore;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Data;

namespace Mathy.ELM.Infrastructure.Services;

public class ReturnToWorkRequestDetailsService : IReturnToWorkRequestDetailsService
{
    private readonly MathyELMContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IEcmLogger _ecmLogger;

    public ReturnToWorkRequestDetailsService(
        MathyELMContext context,
        IUserContextService userContextService,
        IEcmLogger ecmLogger)
    {
        _context = context;
        _userContextService = userContextService;
        _ecmLogger = ecmLogger;
    }

    public async Task<ApiResponse<List<int>>> CreateReturnToWorkRequestDetailsAsync(List<int> hrRequestDetailIds)
    {
        try
        {
            // Batch check if HR request details exist
            var existingHRRequestDetails = await _context.HRRequestDetails
                .Where(hrd => hrRequestDetailIds.Contains(hrd.Id) && !hrd.IsDeleted)
                .Select(hrd => hrd.Id)
                .ToListAsync();

            var missingIds = hrRequestDetailIds.Except(existingHRRequestDetails).ToList();
            if (missingIds.Any())
            {
                var errorMessage = $"HR request details not found for IDs: {string.Join(", ", missingIds)}";
                _ecmLogger.LogSave(false, "ReturnToWorkRequest", null, _userContextService.GetUserEmployeeNumber().ToString(), errorMessage);
                return new ApiResponse<List<int>>
                {
                    Success = false,
                    Message = errorMessage
                };
            }

            // Batch check existing return to work details
            var existingReturnToWorkDetails = await _context.ReturnToWorkRequestDetails
                .Where(rtw => hrRequestDetailIds.Contains(rtw.RequestDetailId) && !rtw.IsDeleted)
                .ToListAsync();

            var createdIds = new List<int>();

            // Add existing details to result
            createdIds.AddRange(existingReturnToWorkDetails.Select(rtw => rtw.Id));

            // Find IDs that need new details created
            var existingRequestDetailIds = existingReturnToWorkDetails.Select(rtw => rtw.RequestDetailId).ToList();
            var newRequestDetailIds = hrRequestDetailIds.Except(existingRequestDetailIds).ToList();

            // Create new details in batch
            if (newRequestDetailIds.Any())
            {
                var currentUserId = _userContextService.GetUserEmployeeNumber();
                var newDetails = newRequestDetailIds.Select(id => new ReturnToWorkRequestDetail
                {
                    RequestDetailId = id,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = false
                }).ToList();

                _context.ReturnToWorkRequestDetails.AddRange(newDetails);
                await _context.SaveChangesAsync();

                createdIds.AddRange(newDetails.Select(d => d.Id));

                // Log successful save to ECM
                foreach (var detail in newDetails)
                {
                    _ecmLogger.LogSave(true, "ReturnToWorkRequest", detail.Id, currentUserId.ToString());
                    _ecmLogger.LogHRRequest(true, "ReturnToWork", "CREATE", detail.Id, currentUserId.ToString());
                }
            }

            return new ApiResponse<List<int>>
            {
                Success = true,
                Data = createdIds,
                Message = $"Successfully processed {createdIds.Count} return to work request details"
            };
        }
        catch (Exception ex)
        {
            _ecmLogger.LogSave(false, "ReturnToWorkRequest", null, _userContextService.GetUserEmployeeNumber().ToString(), $"Error creating return to work request details: {ex.Message}");
            _ecmLogger.LogHRRequest(false, "ReturnToWork", "CREATE", null, _userContextService.GetUserEmployeeNumber().ToString(), ex.Message);

            return new ApiResponse<List<int>>
            {
                Success = false,
                Message = $"Error creating return to work request details: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<ReturnToWorkRequestDetailDto>> GetByHRRequestDetailIdAsync(int hrRequestDetailId)
    {
        try
        {
            var detail = await _context.ReturnToWorkRequestDetails
                .Where(rtw => rtw.RequestDetailId == hrRequestDetailId && !rtw.IsDeleted)
                .Select(rtw => new ReturnToWorkRequestDetailDto
                {
                    Id = rtw.Id,
                    RequestDetailId = rtw.RequestDetailId,
                    CreatedBy = rtw.CreatedBy,
                    CreatedDate = rtw.CreatedDate,
                    ModifiedBy = rtw.ModifiedBy,
                    ModifiedDate = rtw.ModifiedDate,
                    IsDeleted = rtw.IsDeleted
                })
                .FirstOrDefaultAsync();

            if (detail == null)
            {
                return new ApiResponse<ReturnToWorkRequestDetailDto>
                {
                    Success = false,
                    Message = $"Return to work request detail for HR request detail ID {hrRequestDetailId} not found"
                };
            }

            return new ApiResponse<ReturnToWorkRequestDetailDto>
            {
                Success = true,
                Data = detail
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ReturnToWorkRequestDetailDto>
            {
                Success = false,
                Message = $"Error retrieving return to work request detail: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteByHRRequestDetailIdsAsync(List<int> hrRequestDetailIds)
    {
        try
        {
            var details = await _context.ReturnToWorkRequestDetails
                .Where(rtw => hrRequestDetailIds.Contains(rtw.RequestDetailId) && !rtw.IsDeleted)
                .ToListAsync();

            if (details.Any())
            {
                foreach (var detail in details)
                {
                    detail.IsDeleted = true;
                    detail.ModifiedBy = _userContextService.GetUserEmployeeNumber();
                    detail.ModifiedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Log successful deletion to ECM
                foreach (var detail in details)
                {
                    _ecmLogger.LogCancel(true, "ReturnToWorkRequest", detail.Id, _userContextService.GetUserEmployeeNumber().ToString(), "Soft delete", null);
                }
            }

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = $"Successfully deleted {details.Count} return to work request details"
            };
        }
        catch (Exception ex)
        {
            _ecmLogger.LogCancel(false, "ReturnToWorkRequest", null, _userContextService.GetUserEmployeeNumber().ToString(), "Soft delete", ex.Message);

            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Error deleting return to work request details: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
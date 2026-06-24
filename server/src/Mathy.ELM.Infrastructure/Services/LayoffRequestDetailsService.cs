using Microsoft.EntityFrameworkCore;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Data;

namespace Mathy.ELM.Infrastructure.Services;

public class LayoffRequestDetailsService : ILayoffRequestDetailsService
{
    private readonly MathyELMContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IEcmLogger _ecmLogger;

    public LayoffRequestDetailsService(
        MathyELMContext context,
        IUserContextService userContextService,
        IEcmLogger ecmLogger)
    {
        _context = context;
        _userContextService = userContextService;
        _ecmLogger = ecmLogger;
    }

    public async Task<ApiResponse<List<int>>> CreateLayoffRequestDetailsAsync(List<int> hrRequestDetailIds)
    {
        try
        {
            var createdIds = new List<int>();

            foreach (var hrRequestDetailId in hrRequestDetailIds)
            {
                // Check if HR request detail exists
                var hrRequestDetail = await _context.HRRequestDetails
                    .FirstOrDefaultAsync(hrd => hrd.Id == hrRequestDetailId && !hrd.IsDeleted);

                if (hrRequestDetail == null)
                {
                    _ecmLogger.LogSave(false, "LayoffRequest", hrRequestDetailId, null, $"HR request detail with ID {hrRequestDetailId} not found");
                    return new ApiResponse<List<int>>
                    {
                        Success = false,
                        Message = $"HR request detail with ID {hrRequestDetailId} not found"
                    };
                }

                // Check if layoff detail already exists
                var existingDetail = await _context.LayoffRequestDetails
                    .FirstOrDefaultAsync(lrd => lrd.RequestDetailId == hrRequestDetailId && !lrd.IsDeleted);

                if (existingDetail != null)
                {
                    // Already exists, skip
                    createdIds.Add(existingDetail.Id);
                    continue;
                }

                // Create new layoff request detail using the EffectiveDate as LastDayWorked
                var layoffDetail = new LayoffRequestDetail
                {
                    RequestDetailId = hrRequestDetailId,
                    LastDayWorked = hrRequestDetail.EffectiveDate?.Date ?? DateTime.Today,
                    CreatedBy = _userContextService.GetUserEmployeeNumber(),
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.LayoffRequestDetails.Add(layoffDetail);
                await _context.SaveChangesAsync();

                createdIds.Add(layoffDetail.Id);

                // Log successful save to ECM
                _ecmLogger.LogSave(true, "LayoffRequest", layoffDetail.Id, _userContextService.GetUserEmployeeNumber().ToString());
                _ecmLogger.LogHRRequest(true, "Layoff", "CREATE", layoffDetail.Id, _userContextService.GetUserEmployeeNumber().ToString());
            }

            return new ApiResponse<List<int>>
            {
                Success = true,
                Data = createdIds,
                Message = $"Successfully created {createdIds.Count} layoff request details"
            };
        }
        catch (Exception ex)
        {
            _ecmLogger.LogSave(false, "LayoffRequest", null, _userContextService.GetUserEmployeeNumber().ToString(), $"Error creating layoff request details: {ex.Message}");
            _ecmLogger.LogHRRequest(false, "Layoff", "CREATE", null, _userContextService.GetUserEmployeeNumber().ToString(), ex.Message);

            return new ApiResponse<List<int>>
            {
                Success = false,
                Message = $"Error creating layoff request details: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<LayoffRequestDetailDto>> GetByHRRequestDetailIdAsync(int hrRequestDetailId)
    {
        try
        {
            var detail = await _context.LayoffRequestDetails
                .Where(lrd => lrd.RequestDetailId == hrRequestDetailId && !lrd.IsDeleted)
                .Select(lrd => new LayoffRequestDetailDto
                {
                    Id = lrd.Id,
                    RequestDetailId = lrd.RequestDetailId,
                    LastDayWorked = lrd.LastDayWorked,
                    CreatedBy = lrd.CreatedBy,
                    CreatedDate = lrd.CreatedDate,
                    ModifiedBy = lrd.ModifiedBy,
                    ModifiedDate = lrd.ModifiedDate,
                    IsDeleted = lrd.IsDeleted
                })
                .FirstOrDefaultAsync();

            if (detail == null)
            {
                return new ApiResponse<LayoffRequestDetailDto>
                {
                    Success = false,
                    Message = $"Layoff request detail for HR request detail ID {hrRequestDetailId} not found"
                };
            }

            return new ApiResponse<LayoffRequestDetailDto>
            {
                Success = true,
                Data = detail
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<LayoffRequestDetailDto>
            {
                Success = false,
                Message = $"Error retrieving layoff request detail: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteByHRRequestDetailIdsAsync(List<int> hrRequestDetailIds)
    {
        try
        {
            var details = await _context.LayoffRequestDetails
                .Where(lrd => hrRequestDetailIds.Contains(lrd.RequestDetailId) && !lrd.IsDeleted)
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
                    _ecmLogger.LogCancel(true, "LayoffRequest", detail.Id, _userContextService.GetUserEmployeeNumber().ToString(), "Soft delete", null);
                }
            }

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = $"Successfully deleted {details.Count} layoff request details"
            };
        }
        catch (Exception ex)
        {
            _ecmLogger.LogCancel(false, "LayoffRequest", null, _userContextService.GetUserEmployeeNumber().ToString(), "Soft delete", ex.Message);

            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Error deleting layoff request details: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
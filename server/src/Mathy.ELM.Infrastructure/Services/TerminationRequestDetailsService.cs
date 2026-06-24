using Microsoft.EntityFrameworkCore;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Data;

namespace Mathy.ELM.Infrastructure.Services;

public class TerminationRequestDetailsService : ITerminationRequestDetailsService
{
    private readonly MathyELMContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IEcmLogger _ecmLogger;

    public TerminationRequestDetailsService(
        MathyELMContext context,
        IUserContextService userContextService,
        IEcmLogger ecmLogger)
    {
        _context = context;
        _userContextService = userContextService;
        _ecmLogger = ecmLogger;
    }

    public async Task<ApiResponse<List<int>>> CreateTerminationRequestDetailsAsync(List<int> hrRequestDetailIds, CreateTerminationRequestDto? terminationDetails = null)
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
                    _ecmLogger.LogSave(false, "TerminationRequest", hrRequestDetailId, null, $"HR request detail with ID {hrRequestDetailId} not found");
                    return new ApiResponse<List<int>>
                    {
                        Success = false,
                        Message = $"HR request detail with ID {hrRequestDetailId} not found"
                    };
                }

                // Check if termination detail already exists
                var existingDetail = await _context.TerminationRequestDetails
                    .FirstOrDefaultAsync(trd => trd.RequestDetailId == hrRequestDetailId && !trd.IsDeleted);

                if (existingDetail != null)
                {
                    // Already exists, skip
                    createdIds.Add(existingDetail.Id);
                    continue;
                }

                // Create new termination request detail with provided values or defaults
                var terminationDetail = new TerminationRequestDetail
                {
                    RequestDetailId = hrRequestDetailId,
                    ReasonCode = terminationDetails?.ReasonCode ?? "resignation", // Use provided value or default
                    ForwardEmail = terminationDetails?.ForwardEmail,
                    ForwardDeskPhone = terminationDetails?.ForwardDeskPhone,
                    ForwardCellPhone = terminationDetails?.ForwardCellPhone,
                    AutoReply = terminationDetails?.AutoReply,
                    GiveOneDriveAccessTo = terminationDetails?.GiveOneDriveAccessTo,
                    WithKwikTripCard = terminationDetails?.WithKwikTripCard ?? false,
                    KwikCard4DigitNo = terminationDetails?.KwikCard4DigitNo,
                    CreatedBy = _userContextService.GetUserEmployeeNumber(),
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.TerminationRequestDetails.Add(terminationDetail);
                await _context.SaveChangesAsync();

                createdIds.Add(terminationDetail.Id);

                // Log successful save to ECM
                _ecmLogger.LogSave(true, "TerminationRequest", terminationDetail.Id, _userContextService.GetUserEmployeeNumber().ToString());
                _ecmLogger.LogHRRequest(true, "Termination", "CREATE", terminationDetail.Id, _userContextService.GetUserEmployeeNumber().ToString());
            }

            return new ApiResponse<List<int>>
            {
                Success = true,
                Data = createdIds,
                Message = $"Successfully created {createdIds.Count} termination request details"
            };
        }
        catch (Exception ex)
        {
            _ecmLogger.LogSave(false, "TerminationRequest", null, _userContextService.GetUserEmployeeNumber().ToString(), $"Error creating termination request details: {ex.Message}");
            _ecmLogger.LogHRRequest(false, "Termination", "CREATE", null, _userContextService.GetUserEmployeeNumber().ToString(), ex.Message);

            return new ApiResponse<List<int>>
            {
                Success = false,
                Message = $"Error creating termination request details: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<TerminationRequestDetailDto>> GetByHRRequestDetailIdAsync(int hrRequestDetailId)
    {
        try
        {
            var detail = await _context.TerminationRequestDetails
                .Where(trd => trd.RequestDetailId == hrRequestDetailId && !trd.IsDeleted)
                .Select(trd => new TerminationRequestDetailDto
                {
                    Id = trd.Id,
                    RequestDetailId = trd.RequestDetailId,
                    ReasonCode = trd.ReasonCode,
                    ForwardEmail = trd.ForwardEmail,
                    ForwardDeskPhone = trd.ForwardDeskPhone,
                    ForwardCellPhone = trd.ForwardCellPhone,
                    AutoReply = trd.AutoReply,
                    GiveOneDriveAccessTo = trd.GiveOneDriveAccessTo,
                    WithKwikTripCard = trd.WithKwikTripCard,
                    KwikCard4DigitNo = trd.KwikCard4DigitNo,
                    CreatedBy = trd.CreatedBy,
                    CreatedDate = trd.CreatedDate,
                    ModifiedBy = trd.ModifiedBy,
                    ModifiedDate = trd.ModifiedDate,
                    IsDeleted = trd.IsDeleted
                })
                .FirstOrDefaultAsync();

            if (detail == null)
            {
                return new ApiResponse<TerminationRequestDetailDto>
                {
                    Success = false,
                    Message = $"Termination request detail for HR request detail ID {hrRequestDetailId} not found"
                };
            }

            return new ApiResponse<TerminationRequestDetailDto>
            {
                Success = true,
                Data = detail
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<TerminationRequestDetailDto>
            {
                Success = false,
                Message = $"Error retrieving termination request detail: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<bool>> DeleteByHRRequestDetailIdsAsync(List<int> hrRequestDetailIds)
    {
        try
        {
            var details = await _context.TerminationRequestDetails
                .Where(trd => hrRequestDetailIds.Contains(trd.RequestDetailId) && !trd.IsDeleted)
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
                    _ecmLogger.LogCancel(true, "TerminationRequest", detail.Id, _userContextService.GetUserEmployeeNumber().ToString(), "Soft delete", null);
                }
            }

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = $"Successfully deleted {details.Count} termination request details"
            };
        }
        catch (Exception ex)
        {
            _ecmLogger.LogCancel(false, "TerminationRequest", null, _userContextService.GetUserEmployeeNumber().ToString(), "Soft delete", ex.Message);

            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = $"Error deleting termination request details: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
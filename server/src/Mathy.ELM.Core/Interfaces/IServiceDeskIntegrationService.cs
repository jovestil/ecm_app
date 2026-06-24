using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Interfaces;

/// <summary>
/// Service interface for integrating with ManageEngine Service Desk Plus
/// Handles creation of tickets for HR requests (New Hire, Promotion/Transfer, etc.)
/// </summary>
public interface IServiceDeskIntegrationService
{
    /// <summary>
    /// Creates a ServiceDesk ticket for a new hire request
    /// </summary>
    /// <param name="request">The ServiceDesk record creation request with all new hire details</param>
    /// <returns>Response containing success status and ServiceDesk ticket ID</returns>
    Task<ServiceDeskRecordResponseDto> CreateServiceDeskRecord(CreateServiceDeskRecordDto request);

    /// <summary>
    /// Creates a ServiceDesk ticket for a promotion/transfer request
    /// </summary>
    /// <param name="request">The ServiceDesk record creation request with all promotion/transfer details</param>
    /// <returns>Response containing success status and ServiceDesk ticket ID</returns>
    Task<ServiceDeskRecordResponseDto> CreatePromotionServiceDeskRecord(CreatePromotionServiceDeskRecordDto request);

    /// <summary>
    /// Creates a ServiceDesk ticket for a termination request (Off-Boarding template)
    /// </summary>
    /// <param name="request">The ServiceDesk record creation request with all termination details</param>
    /// <returns>Response containing success status and ServiceDesk ticket ID</returns>
    Task<ServiceDeskRecordResponseDto> CreateTerminationServiceDeskRecord(CreateTerminationServiceDeskRecordDto request);
}

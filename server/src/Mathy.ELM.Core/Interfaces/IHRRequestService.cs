using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Interfaces;

public interface IHRRequestService
{
    /// <summary>
    /// Get all HR requests with pagination
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="requestTypeId">Optional filter by request type</param>
    /// <param name="statusId">Optional filter by status</param>
    /// <param name="submittedBy">Optional filter by submitter</param>
    /// <returns>Paginated list of HR requests</returns>
    Task<PagedResponse<List<HRRequestDto>>> GetHRRequestsAsync(
        int page = 1, 
        int pageSize = 25, 
        int? requestTypeId = null, 
        int? statusId = null, 
        int? submittedBy = null);

    /// <summary>
    /// Get HR request by ID
    /// </summary>
    /// <param name="id">HR request ID</param>
    /// <returns>HR request details</returns>
    Task<ApiResponse<HRRequestDto>> GetHRRequestByIdAsync(int id);

    /// <summary>
    /// Create a new HR request
    /// </summary>
    /// <param name="createDto">HR request creation data</param>
    /// <returns>Created HR request details</returns>
    Task<ApiResponse<List<HRRequestDetailDto>>> CreateHRRequestAsync(CreateHRRequestDto createDto);

    /// <summary>
    /// Create a new HR request for multiple employees with the same request type
    /// </summary>
    /// <param name="createDto">Multi-employee HR request creation data</param>
    /// <returns>Created HR request details</returns>
    Task<ApiResponse<List<HRRequestDetailDto>>> CreateMultiEmployeeHRRequestAsync(CreateMultiEmployeeHRRequestDto createDto);

    /// <summary>
    /// Update an existing HR request
    /// </summary>
    /// <param name="updateDto">HR request update data</param>
    /// <returns>Updated HR request</returns>
    Task<ApiResponse<HRRequestDto>> UpdateHRRequestAsync(UpdateHRRequestDto updateDto);

    /// <summary>
    /// Update HR request detail status
    /// </summary>
    /// <param name="detailId">HR request detail ID</param>
    /// <param name="updateDto">Update data</param>
    /// <returns>Updated HR request detail</returns>
    Task<ApiResponse<HRRequestDetailDto>> UpdateHRRequestDetailAsync(int detailId, UpdateHRRequestDetailDto updateDto);

    /// <summary>
    /// Update effective date for all details under a parent HR request
    /// </summary>
    Task<ApiResponse<bool>> UpdateEffectiveDateByParentIdAsync(int parentId, string effectiveDate);

    /// <summary>
    /// Delete an HR request (soft delete)
    /// </summary>
    /// <param name="id">HR request ID</param>
    /// <returns>Success indicator</returns>
    Task<ApiResponse<bool>> DeleteHRRequestAsync(int id);

    /// <summary>
    /// Get HR requests by status
    /// </summary>
    /// <param name="statusId">Status ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of HR requests</returns>
    Task<PagedResponse<List<HRRequestDto>>> GetHRRequestsByStatusAsync(int statusId, int page = 1, int pageSize = 25);

    /// <summary>
    /// Get HR requests submitted by a specific user
    /// </summary>
    /// <param name="submittedBy">User ID who submitted the requests</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of HR requests</returns>
    Task<PagedResponse<List<HRRequestDto>>> GetHRRequestsBySubmitterAsync(int submittedBy, int page = 1, int pageSize = 25);

    /// <summary>
    /// Get HR requests by type
    /// </summary>
    /// <param name="requestTypeId">Request type ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of HR requests</returns>
    Task<PagedResponse<List<HRRequestDto>>> GetHRRequestsByTypeAsync(int requestTypeId, int page = 1, int pageSize = 25);

    /// <summary>
    /// Get HR request details for a specific request
    /// </summary>
    /// <param name="requestId">HR request ID</param>
    /// <returns>List of HR request details</returns>
    Task<ApiResponse<List<HRRequestDetailDto>>> GetHRRequestDetailsAsync(int requestId);

    /// <summary>
    /// Get all HR request details across all request types with optional filtering, pagination, and sorting
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="requestTypeId">Optional filter by request type</param>
    /// <param name="statusId">Optional filter by status</param>
    /// <param name="employeeId">Optional filter by employee</param>
    /// <param name="submittedBy">Optional filter by submitter</param>
    /// <param name="searchTerm">Optional search term to filter results</param>
    /// <param name="sortField">Optional field to sort by</param>
    /// <param name="sortDirection">Optional sort direction (asc or desc)</param>
    /// <returns>Paginated list of HR request details</returns>
    Task<PagedResponse<List<HRRequestDetailDto>>> GetAllHRRequestDetailsAsync(
        int page = 1, 
        int pageSize = 25, 
        int? requestTypeId = null, 
        int? statusId = null, 
        int? employeeId = null, 
        int? submittedBy = null,
        string? searchTerm = null,
        string? sortField = null,
        string? sortDirection = null);

    /// <summary>
    /// Process HR request details with Viewpoint integration
    /// </summary>
    /// <param name="detailId">HR request detail ID</param>
    /// <returns>Processing result</returns>
    Task<ApiResponse<bool>> ProcessHRRequestDetailAsync(int detailId);

    /// <summary>
    /// Retry a failed HR request detail by resetting it to pending and rescheduling the background job
    /// </summary>
    /// <param name="detailId">HR request detail ID</param>
    /// <returns>Retry result</returns>
    Task<ApiResponse<bool>> RetryHRRequestDetailAsync(int detailId);

    /// <summary>
    /// Cancel an HR request detail
    /// </summary>
    /// <param name="detailId">HR request detail ID</param>
    /// <returns>Updated HR request detail with Cancelled status</returns>
    Task<ApiResponse<HRRequestDetailDto>> CancelHRRequestDetailAsync(int detailId);
}
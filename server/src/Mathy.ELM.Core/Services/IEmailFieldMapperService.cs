using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Core.Services;

/// <summary>
/// Service for mapping email template field names to actual request data
/// </summary>
public interface IEmailFieldMapperService
{
    /// <summary>
    /// Maps a list of field names from EmailTemplate.Body to actual values from New Hire request data
    /// </summary>
    /// <param name="request">The New Hire request DTO containing the data</param>
    /// <param name="fieldNames">List of field names to map (e.g., "Start Date", "New Employee", "Company")</param>
    /// <returns>Dictionary mapping field names to their corresponding values</returns>
    Task<Dictionary<string, string>> MapNewHireFieldsToDataAsync(CreateNewHireRequestDto request, List<string> fieldNames);

    /// <summary>
    /// Maps a list of field names from EmailTemplate.Body to actual values from Promotion request data
    /// </summary>
    /// <param name="request">The Promotion request DTO containing the data</param>
    /// <param name="fieldNames">List of field names to map (e.g., "Employee Name", "New Position", "Effective Date")</param>
    /// <returns>Dictionary mapping field names to their corresponding values</returns>
    Task<Dictionary<string, string>> MapPromotionFieldsToDataAsync(CreatePromotionRequestDto request, List<string> fieldNames);

    /// <summary>
    /// Maps a list of field names from EmailTemplate.Body to actual values from Termination request data
    /// </summary>
    /// <param name="request">The Termination email data DTO containing the data</param>
    /// <param name="fieldNames">List of field names to map (e.g., "Employee Name", "Company", "Effective Date")</param>
    /// <returns>Dictionary mapping field names to their corresponding values</returns>
    Task<Dictionary<string, string>> MapTerminationFieldsToDataAsync(TerminationEmailDataDto request, List<string> fieldNames);

    /// <summary>
    /// Maps a list of field names from EmailTemplate.Body to actual values from Layoff request data
    /// </summary>
    /// <param name="request">The Layoff email data DTO containing the data</param>
    /// <param name="fieldNames">List of field names to map (e.g., "Employee Name", "Company", "Effective Date")</param>
    /// <returns>Dictionary mapping field names to their corresponding values</returns>
    Task<Dictionary<string, string>> MapLayoffFieldsToDataAsync(LayoffEmailDataDto request, List<string> fieldNames);

    /// <summary>
    /// Maps a list of field names from EmailTemplate.Body to actual values from ReturnToWork request data
    /// </summary>
    /// <param name="request">The ReturnToWork email data DTO containing the data</param>
    /// <param name="fieldNames">List of field names to map (e.g., "Employee Name", "Company", "Effective Date")</param>
    /// <returns>Dictionary mapping field names to their corresponding values</returns>
    Task<Dictionary<string, string>> MapReturnToWorkFieldsToDataAsync(ReturnToWorkEmailDataDto request, List<string> fieldNames);
}

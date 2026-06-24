using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mathy.ELM.Core.Configuration;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Constants;
using Mathy.ELM.Core.Converters;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Infrastructure.Data;

namespace Mathy.ELM.Infrastructure.Services;

public class ViewpointService : IViewpointService
{
    private readonly HttpClient _httpClient;
    private readonly ViewpointApiSettings _settings;
    private readonly ILogger<ViewpointService> _logger;
    private readonly IEcmLogger _ecmLogger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly MathyELMContext _context;

    public ViewpointService(
        HttpClient httpClient,
        IOptions<ViewpointApiSettings> settings,
        ILogger<ViewpointService> logger,
        IEcmLogger ecmLogger,
        MathyELMContext context)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _ecmLogger = ecmLogger;
        _context = context;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Set default headers
        _httpClient.DefaultRequestHeaders.Add("X-Application-Key", _settings.ApplicationKey);
    }

    public async Task<ViewpointEmployeesResponse?> GetAllEmployeesAsync(int page = 1, int pageSize = 25, string? filter = null)
    {
        try
        {
            // Get all employees from the API first
            var allEmployees = await GetAllEmployeesFromApiAsync();
            
            if (allEmployees == null || !allEmployees.Any())
            {
                return new ViewpointEmployeesResponse 
                { 
                    Data = new List<ViewpointEmployeeDto>(),
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize,
                    HasMore = false
                };
            }

            // Apply filtering if specified
            if (!string.IsNullOrEmpty(filter) && filter.Equals("RTW", StringComparison.OrdinalIgnoreCase))
            {
                allEmployees = allEmployees.Where(e => 
                    !string.IsNullOrEmpty(e.Status) && 
                    e.Status.Equals("U-LAYOFF", StringComparison.OrdinalIgnoreCase)
                ).ToList();
                
                _logger.LogInformation("Applied RTW filter: {FilteredCount} employees with Status 'U-LAYOFF' found", allEmployees.Count);
            }

            // Sort employees by FirstName, LastName, PRCo, PRDept
            allEmployees = allEmployees.OrderBy(e => e.FirstName ?? string.Empty)
                                     .ThenBy(e => e.LastName ?? string.Empty)
                                     .ThenBy(e => e.PRCo ?? 0)
                                     .ThenBy(e => e.PRDept ?? string.Empty)
                                     .ToList();

            // Apply client-side pagination
            var totalCount = allEmployees.Count;
            var skip = (page - 1) * pageSize;
            var paginatedEmployees = allEmployees.Skip(skip).Take(pageSize).ToList();
            
            var result = new ViewpointEmployeesResponse
            {
                Data = paginatedEmployees,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasMore = skip + pageSize < totalCount
            };
            
            _logger.LogInformation("Successfully fetched {Count} employees (page {Page}/{TotalPages}) from Viewpoint API. Total: {TotalCount}",
                paginatedEmployees.Count, page, (int)Math.Ceiling((double)totalCount / pageSize), totalCount);

            _ecmLogger.LogViewpointIntegration(true, "GetAllEmployees", "/employees", totalCount, null);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching employees from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Network error occurred while fetching employees from Viewpoint API");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing response from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Error deserializing response from Viewpoint API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching employees from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Unexpected error occurred while fetching employees from Viewpoint API");
            throw;
        }
    }

    private async Task<List<ViewpointEmployeeDto>?> GetAllEmployeesFromApiAsync()
    {
        try
        {
            var baseUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/resources/cache";
            var allEmployees = new List<ViewpointEmployeeDto>();
            string? nextUrl = baseUrl;
            
            _logger.LogInformation("Fetching all employees from Viewpoint API: {Url}", baseUrl);

            do
            {
                var response = await _httpClient.GetAsync(nextUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch employees. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                    
                    // If this is not the first request (we have some employees already), return what we have
                    if (allEmployees.Any())
                    {
                        _logger.LogWarning("Returning {Count} employees fetched before error occurred", allEmployees.Count);
                        return allEmployees;
                    }
                    
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Empty response received from Viewpoint API");
                    break;
                }

                // Parse the Viewpoint API response
                var viewpointResponse = JsonSerializer.Deserialize<ViewpointApiResponse>(content, _jsonOptions);
                
                if (viewpointResponse?.Data != null)
                {
                    // Convert ViewpointApiEmployeeData to ViewpointEmployeeDto
                    var employees = viewpointResponse.Data.ToList();
                    
                    allEmployees.AddRange(employees);
                    
                    // Use next URL from response for subsequent requests
                    nextUrl = viewpointResponse.Next;
                    
                    _logger.LogInformation("Fetched {Count} employees in this batch. Total so far: {TotalCount}. HasMore: {HasMore}",
                        employees.Count, allEmployees.Count, !string.IsNullOrEmpty(nextUrl));
                }
                else
                {
                    break;
                }
                
            } while (!string.IsNullOrEmpty(nextUrl));

            _logger.LogInformation("Successfully fetched all {TotalCount} employees from Viewpoint API", allEmployees.Count);
            _ecmLogger.LogViewpointIntegration(true, "GetAllEmployeesFromApi", $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/resources/cache", allEmployees.Count, null);
            return allEmployees;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all employees from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Error occurred while fetching all employees from Viewpoint API");
            throw;
        }
    }

    private async Task<List<ViewpointCompanyDto>?> GetAllCompaniesFromApiAsync()
    {
        try
        {
            var baseUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hq/2/data/company_parameters/cache";
            var allCompanies = new List<ViewpointCompanyDto>();
            string? nextUrl = baseUrl;
            
            _logger.LogInformation("Fetching all companies from Viewpoint API: {Url}", baseUrl);

            do
            {
                var response = await _httpClient.GetAsync(nextUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch companies. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                    
                    // If this is not the first request (we have some companies already), return what we have
                    if (allCompanies.Any())
                    {
                        _logger.LogWarning("Returning {Count} companies fetched before error occurred", allCompanies.Count);
                        return allCompanies;
                    }
                    
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Empty response received from Viewpoint API");
                    break;
                }

                // Log the raw response for debugging
                _logger.LogInformation("Raw companies response batch: {Response}", content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);

                // Try to parse as wrapper object first (preferred format with Next URL)
                try
                {
                    var viewpointResponse = JsonSerializer.Deserialize<ViewpointCompaniesApiResponse>(content, _jsonOptions);
                    
                    if (viewpointResponse?.Data != null)
                    {
                        var companies = viewpointResponse.Data;
                        allCompanies.AddRange(companies);
                        
                        // Use next URL from response for subsequent requests
                        nextUrl = viewpointResponse.Next;
                        
                        _logger.LogInformation("Fetched {Count} companies in this batch (wrapper). Total so far: {TotalCount}. HasMore: {HasMore}",
                            companies.Count, allCompanies.Count, !string.IsNullOrEmpty(nextUrl));
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse companies response as wrapper format");
                        break;
                    }
                }
                catch (JsonException)
                {
                    // Fallback: try to parse as direct array
                    var companiesArray = JsonSerializer.Deserialize<List<ViewpointCompanyDto>>(content, _jsonOptions);
                    if (companiesArray != null)
                    {
                        allCompanies.AddRange(companiesArray);
                        _logger.LogInformation("Fetched {Count} companies in this batch (direct array). Total so far: {TotalCount}",
                            companiesArray.Count, allCompanies.Count);
                        // No next URL in direct array format
                        nextUrl = null;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse companies response in both formats");
                        break;
                    }
                }
                
            } while (!string.IsNullOrEmpty(nextUrl));

            _logger.LogInformation("Successfully fetched all {TotalCount} companies from Viewpoint API", allCompanies.Count);

            // Filter to only include companies where udActivePRCo = "Y"
            var activeCompanies = allCompanies
                .Where(c => c.CustomFields?.ActivePRCo == "Y")
                .ToList();

            _logger.LogInformation("Filtered to {ActiveCount} active PR companies (udActivePRCo = 'Y') out of {TotalCount} total",
                activeCompanies.Count, allCompanies.Count);

            return activeCompanies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all companies from Viewpoint API");
            throw;
        }
    }

    private async Task<List<ViewpointPayrollGroupDto>?> GetAllPayrollGroupsFromApiAsync()
    {
        try
        {
            var baseUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/pr/2/data/groups/cache";
            var allPayrollGroups = new List<ViewpointPayrollGroupDto>();
            string? nextUrl = baseUrl;
            
            _logger.LogInformation("Fetching all payroll groups from Viewpoint API: {Url}", baseUrl);

            do
            {
                var response = await _httpClient.GetAsync(nextUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch payroll groups. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                    
                    // If this is not the first request (we have some payroll groups already), return what we have
                    if (allPayrollGroups.Any())
                    {
                        _logger.LogWarning("Returning {Count} payroll groups fetched before error occurred", allPayrollGroups.Count);
                        return allPayrollGroups;
                    }
                    
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Empty response received from Viewpoint API");
                    break;
                }

                // Log the raw response for debugging
                _logger.LogInformation("Raw payroll groups response batch: {Response}", content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);

                // Try to parse as wrapper object first (preferred format with Next URL)
                try
                {
                    var viewpointResponse = JsonSerializer.Deserialize<ViewpointPayrollGroupsApiResponse>(content, _jsonOptions);
                    
                    if (viewpointResponse?.Data != null)
                    {
                        var payrollGroups = viewpointResponse.Data;
                        allPayrollGroups.AddRange(payrollGroups);
                        
                        // Use next URL from response for subsequent requests
                        nextUrl = viewpointResponse.Next;
                        
                        _logger.LogInformation("Fetched {Count} payroll groups in this batch (wrapper). Total so far: {TotalCount}. HasMore: {HasMore}",
                            payrollGroups.Count, allPayrollGroups.Count, !string.IsNullOrEmpty(nextUrl));
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse payroll groups response as wrapper format");
                        break;
                    }
                }
                catch (JsonException)
                {
                    // Fallback: try to parse as direct array
                    var payrollGroupsArray = JsonSerializer.Deserialize<List<ViewpointPayrollGroupDto>>(content, _jsonOptions);
                    if (payrollGroupsArray != null)
                    {
                        allPayrollGroups.AddRange(payrollGroupsArray);
                        _logger.LogInformation("Fetched {Count} payroll groups in this batch (direct array). Total so far: {TotalCount}",
                            payrollGroupsArray.Count, allPayrollGroups.Count);
                        // No next URL in direct array format
                        nextUrl = null;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse payroll groups response in both formats");
                        break;
                    }
                }
                
            } while (!string.IsNullOrEmpty(nextUrl));
            
            _logger.LogInformation("Successfully fetched all {TotalCount} payroll groups from Viewpoint API", allPayrollGroups.Count);
            return allPayrollGroups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all payroll groups from Viewpoint API");
            throw;
        }
    }

    private async Task<List<ViewpointDepartmentDto>?> GetAllDepartmentsFromApiAsync()
    {
        try
        {
            var baseUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/pr/2/data/departments/cache";
            var allDepartments = new List<ViewpointDepartmentDto>();
            string? nextUrl = baseUrl;
            
            _logger.LogInformation("Fetching all departments from Viewpoint API: {Url}", baseUrl);

            do
            {
                var response = await _httpClient.GetAsync(nextUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch departments. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                    
                    // If this is not the first request (we have some departments already), return what we have
                    if (allDepartments.Any())
                    {
                        _logger.LogWarning("Returning {Count} departments fetched before error occurred", allDepartments.Count);
                        return allDepartments;
                    }
                    
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Empty response received from Viewpoint API");
                    break;
                }

                // Log the raw response for debugging
                _logger.LogInformation("Raw departments response batch: {Response}", content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);

                // Try to parse as wrapper object first (preferred format with Next URL)
                try
                {
                    var viewpointResponse = JsonSerializer.Deserialize<ViewpointDepartmentsApiResponse>(content, _jsonOptions);
                    
                    if (viewpointResponse?.Data != null)
                    {
                        var departments = viewpointResponse.Data;
                        allDepartments.AddRange(departments);
                        
                        // Use next URL from response for subsequent requests
                        nextUrl = viewpointResponse.Next;
                        
                        _logger.LogInformation("Fetched {Count} departments in this batch (wrapper). Total so far: {TotalCount}. HasMore: {HasMore}",
                            departments.Count, allDepartments.Count, !string.IsNullOrEmpty(nextUrl));
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse departments response as wrapper format");
                        break;
                    }
                }
                catch (JsonException)
                {
                    // Fallback: try to parse as direct array
                    var departmentsArray = JsonSerializer.Deserialize<List<ViewpointDepartmentDto>>(content, _jsonOptions);
                    if (departmentsArray != null)
                    {
                        allDepartments.AddRange(departmentsArray);
                        _logger.LogInformation("Fetched {Count} departments in this batch (direct array). Total so far: {TotalCount}",
                            departmentsArray.Count, allDepartments.Count);
                        // No next URL in direct array format
                        nextUrl = null;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse departments response in both formats");
                        break;
                    }
                }
                
            } while (!string.IsNullOrEmpty(nextUrl));

            // Filter to only include companies where udActivePRCo = "Y"
            var activeDepartments = allDepartments 
                .Where(c => c.CustomFields?.IsActive == "Y")
                .ToList();

            _logger.LogInformation("Successfully fetched all {TotalCount} departments from Viewpoint API", activeDepartments.Count);
            return activeDepartments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all departments from Viewpoint API");
            throw;
        }
    }

    private async Task<List<ViewpointPositionDto>?> GetAllPositionsFromApiAsync()
    {
        try
        {
            var baseUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/position_codes/cache";
            var allPositions = new List<ViewpointPositionDto>();
            string? nextUrl = baseUrl;
            
            _logger.LogInformation("Fetching all positions from Viewpoint API: {Url}", baseUrl);

            do
            {
                var response = await _httpClient.GetAsync(nextUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch positions. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                    
                    // If this is not the first request (we have some positions already), return what we have
                    if (allPositions.Any())
                    {
                        _logger.LogWarning("Returning {Count} positions fetched before error occurred", allPositions.Count);
                        return allPositions;
                    }
                    
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Empty response received from Viewpoint API");
                    break;
                }

                // Log the raw response for debugging
                _logger.LogInformation("Raw positions response batch: {Response}", content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);

                // Try to parse as wrapper object first (preferred format with Next URL)
                try
                {
                    var viewpointResponse = JsonSerializer.Deserialize<ViewpointPositionsApiResponse>(content, _jsonOptions);
                    
                    if (viewpointResponse?.Data != null)
                    {
                        var positions = viewpointResponse.Data;
                        allPositions.AddRange(positions);
                        
                        // Use next URL from response for subsequent requests
                        nextUrl = viewpointResponse.Next;
                        
                        _logger.LogInformation("Fetched {Count} positions in this batch (wrapper). Total so far: {TotalCount}. HasMore: {HasMore}",
                            positions.Count, allPositions.Count, !string.IsNullOrEmpty(nextUrl));
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse positions response as wrapper format");
                        break;
                    }
                }
                catch (JsonException)
                {
                    // Fallback: try to parse as direct array
                    var positionsArray = JsonSerializer.Deserialize<List<ViewpointPositionDto>>(content, _jsonOptions);
                    if (positionsArray != null)
                    {
                        allPositions.AddRange(positionsArray);
                        _logger.LogInformation("Fetched {Count} positions in this batch (direct array). Total so far: {TotalCount}",
                            positionsArray.Count, allPositions.Count);
                        // No next URL in direct array format
                        nextUrl = null;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse positions response in both formats");
                        break;
                    }
                }
                
            } while (!string.IsNullOrEmpty(nextUrl));
            
            _logger.LogInformation("Successfully fetched all {TotalCount} positions from Viewpoint API", allPositions.Count);
            return allPositions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all positions from Viewpoint API");
            throw;
        }
    }

    private async Task<List<ViewpointPREHEmployeeDto>?> GetPREHEmployeesFromApiAsync()
    {
        try
        {
            var baseUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/pr/2/data/employees/cache";
            var allEmployees = new List<ViewpointPREHEmployeeDto>();
            string? nextUrl = baseUrl;
            
            _logger.LogInformation("Fetching all PREH employees from Viewpoint API: {Url}", baseUrl);

            do
            {
                var response = await _httpClient.GetAsync(nextUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch PREH employees. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                    
                    // If this is not the first request (we have some employees already), return what we have
                    if (allEmployees.Any())
                    {
                        _logger.LogWarning("Returning {Count} PREH employees fetched before error occurred", allEmployees.Count);
                        return allEmployees;
                    }
                    
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Empty response received from Viewpoint API");
                    break;
                }

                // Log the raw response for debugging
                _logger.LogInformation("Raw PREH employees response batch: {Response}", content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);

                // Try to parse as wrapper object first (preferred format with Next URL)
                try
                {
                    var viewpointResponse = JsonSerializer.Deserialize<ViewpointPREHEmployeesApiResponse>(content, _jsonOptions);
                    
                    if (viewpointResponse?.Data != null)
                    {
                        var employees = viewpointResponse.Data;
                        allEmployees.AddRange(employees);
                        
                        // Use next URL from response for subsequent requests
                        nextUrl = viewpointResponse.Next;
                        
                        _logger.LogInformation("Fetched {Count} PREH employees in this batch (wrapper). Total so far: {TotalCount}. HasMore: {HasMore}",
                            employees.Count, allEmployees.Count, !string.IsNullOrEmpty(nextUrl));
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse PREH employees response as wrapper format");
                        break;
                    }
                }
                catch (JsonException)
                {
                    // Fallback: try to parse as direct array
                    var employeesArray = JsonSerializer.Deserialize<List<ViewpointPREHEmployeeDto>>(content, _jsonOptions);
                    if (employeesArray != null)
                    {
                        allEmployees.AddRange(employeesArray);
                        _logger.LogInformation("Fetched {Count} PREH employees in this batch (direct array). Total so far: {TotalCount}",
                            employeesArray.Count, allEmployees.Count);
                        // No next URL in direct array format
                        nextUrl = null;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse PREH employees response in both formats");
                        break;
                    }
                }
                
            } while (!string.IsNullOrEmpty(nextUrl));
            
            _logger.LogInformation("Successfully fetched all {TotalCount} PREH employees from Viewpoint API", allEmployees.Count);
            return allEmployees;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all PREH employees from Viewpoint API");
            throw;
        }
    }

    public async Task<ViewpointEmployeeDto?> GetEmployeeByNumberAsync(string employeeNumber)
    {
        try
        {
            var url = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/resources/cache/search";
            
            _logger.LogInformation("Searching employee by number in Viewpoint API: {Url}, EmployeeNumber: {EmployeeNumber}", url, employeeNumber);

            // Create request with employee number search parameter
            var requestUrl = $"{url}?employeeNumber={Uri.EscapeDataString(employeeNumber)}";
            
            var response = await _httpClient.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
                    "Failed to search employee by number. Status: {StatusCode}, Reason: {ReasonPhrase}. Falling back to full search.",
                    response.StatusCode, response.ReasonPhrase);

                // Fallback to the original method if search endpoint doesn't support employee number
                return await GetEmployeeByNumberFallbackAsync(employeeNumber);
            }

            var content = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogInformation("Empty response received from Viewpoint search API for employee number: {EmployeeNumber}", employeeNumber);
                return null;
            }

            // Log the response for debugging
            _logger.LogDebug("Viewpoint employee search response: {Content}", content);

            var searchResult = JsonSerializer.Deserialize<ViewpointEmployeesResponse>(content, _jsonOptions);
            
            // Find employee with matching employee number (case-insensitive)
            var employee = searchResult?.Data?.FirstOrDefault(e => 
                e.PREmp.HasValue && e.PREmp.Value.ToString().Equals(employeeNumber, StringComparison.OrdinalIgnoreCase));

            if (employee != null)
            {
                _logger.LogInformation("Found employee with number {EmployeeNumber}: {FirstName} {LastName}",
                    employeeNumber, employee.FirstName, employee.LastName);
                _ecmLogger.LogViewpointIntegration(true, "GetEmployeeByNumber", $"{url}?employeeNumber={employeeNumber}", 1, null);
            }
            else
            {
                _logger.LogInformation("No employee found with number: {EmployeeNumber}", employeeNumber);
                _ecmLogger.LogViewpointIntegration(true, "GetEmployeeByNumber", $"{url}?employeeNumber={employeeNumber}", 0, null);
            }

            return employee;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while searching employee by number {EmployeeNumber} from Viewpoint API", employeeNumber);
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"Network error occurred while searching employee by number {employeeNumber}");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing search response from Viewpoint API for employee number {EmployeeNumber}", employeeNumber);
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"Error deserializing search response for employee number {employeeNumber}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while searching employee by number {EmployeeNumber} from Viewpoint API", employeeNumber);
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"Unexpected error occurred while searching employee by number {employeeNumber}");
            throw;
        }
    }

    /// <summary>
    /// Fallback method for employee number search - uses the original approach of fetching all employees
    /// </summary>
    private async Task<ViewpointEmployeeDto?> GetEmployeeByNumberFallbackAsync(string employeeNumber)
    {
        try
        {
            _logger.LogInformation("Using fallback method to search for employee number {EmployeeNumber} by fetching all employees", employeeNumber);
            
            // Fallback to the original method: get all employees and search locally
            var allEmployees = await GetAllEmployeesFromApiAsync();
            
            return allEmployees?.FirstOrDefault(e => 
                e.PREmp.HasValue && e.PREmp.Value.ToString().Equals(employeeNumber, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in fallback employee search for {EmployeeNumber}", employeeNumber);
            throw;
        }
    }

    public async Task<List<ViewpointEmployeeDto>> SearchEmployeesAsync(int hrRef)
    {
        try
        {
            if (hrRef <= 0)
            {
                return new List<ViewpointEmployeeDto>();
            }

            string searchTerm = hrRef.ToString();

            // Try using the search endpoint first
            var searchResults = await SearchEmployeesWithApiAsync(searchTerm);
            if (searchResults != null)
            {
                _ecmLogger.LogViewpointIntegration(true, "SearchEmployees", "/search", searchResults.Count, null);
                return searchResults;
            }

            // Fallback to the original method: get all employees and search locally
            var fallbackResults = await SearchEmployeesFallbackAsync(searchTerm);
            _ecmLogger.LogViewpointIntegration(true, "SearchEmployees", "/cache", fallbackResults.Count, null);
            return fallbackResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching employees with HRRef '{HRRef}' from Viewpoint API", hrRef);
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"Error occurred while searching employees with HRRef '{hrRef}'");
            throw;
        }
    }

    public async Task<List<ViewpointEmployeeDto>> SearchEmployeeInNewHireWithAPIAsync(int HRCo, string PRDept, string LastName, string HireDate)
    {
        try
        {
            var url = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/resources/cache/search";

            _logger.LogInformation("Searching employees for new hire with API endpoint: {Url}, HRCo: {HRCo}, PRDept: {PRDept}, LastName: {LastName}, HireDate: {HireDate}",
                url, HRCo, PRDept, LastName, HireDate);

            // Build the filters array based on the RestSharp example
            var searchRequest = new ViewpointSearchRequest
            {
                Filters = new List<ViewpointSearchFilter>
                {
                    new ViewpointSearchFilter { PropertyName = "HRCo", Value = HRCo, Operator = "Equal" },
                    new ViewpointSearchFilter { PropertyName = "PRDept", Value = PRDept, Operator = "Equal" },
                    new ViewpointSearchFilter { PropertyName = "LastName", Value = LastName, Operator = "Equal" },
                    new ViewpointSearchFilter { PropertyName = "HireDate", Value = HireDate, Operator = "Equal" }
                }
            };

            var jsonContent = JsonSerializer.Serialize(searchRequest, _jsonOptions);
            _logger.LogDebug("New hire search request JSON: {JsonContent}", jsonContent);

            var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, httpContent);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("New hire search API response: {JsonResponse}", jsonResponse);

                var searchResponse = JsonSerializer.Deserialize<ViewpointSearchResponseDto>(jsonResponse, _jsonOptions);

                if (searchResponse?.Data != null)
                {
                    _logger.LogInformation("Successfully found {Count} employees for new hire search (API reported count: {ApiCount})",
                        searchResponse.Data.Count, searchResponse.Count);
                    _ecmLogger.LogViewpointIntegration(true, "SearchEmployeeInNewHire", "/cache/search", searchResponse.Data.Count, null);
                    return searchResponse.Data;
                }
                else
                {
                    _logger.LogWarning("New hire search API response was successful but contained no data or was null");
                    _ecmLogger.LogViewpointIntegration(true, "SearchEmployeeInNewHire", "/cache/search", 0, "No data in response");
                }
            }
            else
            {
                _logger.LogWarning("New hire search API returned non-success status: {StatusCode} - {ReasonPhrase}",
                    response.StatusCode, response.ReasonPhrase);
                _ecmLogger.LogViewpointIntegration(false, "SearchEmployeeInNewHire", "/cache/search", 0, $"Status: {response.StatusCode}");
            }

            return new List<ViewpointEmployeeDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while searching employees for new hire from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Network error occurred while searching employees for new hire");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error occurred while processing new hire search response from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "JSON deserialization error occurred while processing new hire search response");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while searching employees for new hire from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Unexpected error occurred while searching employees for new hire");
            throw;
        }
    }

    /// <summary>
    /// Try to use the Viewpoint search API endpoint for employee search
    /// </summary>
    private async Task<List<ViewpointEmployeeDto>?> SearchEmployeesWithApiAsync(string searchTerm)
    {
        try
        {
            var url = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/resources/cache/search";
            
            _logger.LogInformation("Searching employees with API endpoint: {Url}, SearchTerm: {SearchTerm}", url, searchTerm);

            // Try different search parameter names that might be supported
            var searchParams = new[]
            {
                $"search={Uri.EscapeDataString(searchTerm)}",
                $"q={Uri.EscapeDataString(searchTerm)}",
                $"query={Uri.EscapeDataString(searchTerm)}",
                $"name={Uri.EscapeDataString(searchTerm)}"
            };

            foreach (var param in searchParams)
            {
                var requestUrl = $"{url}?{param}";
                
                var response = await _httpClient.GetAsync(requestUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        _logger.LogDebug("Successful search response with parameter '{Param}': {Content}", param, content.Length > 200 ? content.Substring(0, 200) + "..." : content);
                        
                        var searchResult = JsonSerializer.Deserialize<ViewpointEmployeesResponse>(content, _jsonOptions);
                        
                        if (searchResult?.Data != null && searchResult.Data.Any())
                        {
                            _logger.LogInformation("Found {Count} employees using search API with parameter '{Param}'", searchResult.Data.Count, param);
                            return searchResult.Data;
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("Search parameter '{Param}' failed with status: {StatusCode}", param, response.StatusCode);
                }
            }

            _logger.LogInformation("Search API did not return results, will use fallback method");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error using search API for term '{SearchTerm}', will use fallback method", searchTerm);
            return null;
        }
    }

    /// <summary>
    /// Fallback method for employee search - uses the original approach of fetching all employees
    /// </summary>
    private async Task<List<ViewpointEmployeeDto>> SearchEmployeesFallbackAsync(string searchTerm)
    {
        try
        {
            _logger.LogInformation("Using fallback method to search for employees with term '{SearchTerm}' by fetching all employees", searchTerm);
            
            // Fallback to the original method: get all employees and search locally
            var allEmployees = await GetAllEmployeesFromApiAsync();
            
            if (allEmployees == null || !allEmployees.Any())
            {
                _logger.LogWarning("No employees retrieved from API for fallback search");
                return new List<ViewpointEmployeeDto>();
            }

            // Filter employees based on search term
            var filteredEmployees = allEmployees.Where(emp => 
                (!string.IsNullOrEmpty(emp.FirstName) && emp.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(emp.LastName) && emp.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (emp.HRRef.HasValue && emp.HRRef.Value.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(emp.Email) && emp.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            _logger.LogInformation("Fallback search found {Count} employees matching term '{SearchTerm}'", 
                filteredEmployees.Count, searchTerm);
            
            return filteredEmployees;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while performing fallback employee search for term '{SearchTerm}'", searchTerm);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing response during fallback employee search for term '{SearchTerm}'", searchTerm);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in fallback employee search for term '{SearchTerm}'", searchTerm);
            throw;
        }
    }

    public async Task<ViewpointEmployeeDto?> GetEmployeeByEmailAsync(string emailAddress)
    {
        try
        {
            var url = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/resources/cache/search";
            
            _logger.LogInformation("Searching employee by email in Viewpoint API: {Url}, Email: {Email}", url, emailAddress);

            // Create request with email search parameter
            var requestUrl = $"{url}?email={Uri.EscapeDataString(emailAddress)}";
            
            var response = await _httpClient.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to search employee by email. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Empty response received from Viewpoint search API for email: {Email}", emailAddress);
                return null;
            }

            // Log the response for debugging
            _logger.LogInformation("Viewpoint search response: {Content}", content);

            var searchResult = JsonSerializer.Deserialize<ViewpointEmployeesResponse>(content, _jsonOptions);
            
            // Find employee with matching email (case-insensitive) - check both Email and WorkEmail fields
            var employee = searchResult?.Data?.FirstOrDefault(e => 
                (!string.IsNullOrEmpty(e.Email) && string.Equals(e.Email, emailAddress, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(e.CustomFields?.WorkEmail) && string.Equals(e.CustomFields.WorkEmail, emailAddress, StringComparison.OrdinalIgnoreCase)));

            if (employee != null)
            {
                _logger.LogInformation("Found employee with email {Email}: {EmployeeNumber} - {FirstName} {LastName}",
                    emailAddress, employee.PREmp, employee.FirstName, employee.LastName);
                _ecmLogger.LogViewpointIntegration(true, "GetEmployeeByEmail", $"{url}?email={emailAddress}", 1, null);
            }
            else
            {
                _logger.LogWarning("No employee found with email: {Email}", emailAddress);
                _ecmLogger.LogViewpointIntegration(true, "GetEmployeeByEmail", $"{url}?email={emailAddress}", 0, null);
            }

            return employee;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while searching employee by email {Email} from Viewpoint API", emailAddress);
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"Network error occurred while searching employee by email {emailAddress}");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing search response from Viewpoint API for email {Email}", emailAddress);
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"Error deserializing search response for email {emailAddress}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while searching employee by email {Email} from Viewpoint API", emailAddress);
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"Unexpected error occurred while searching employee by email {emailAddress}");
            throw;
        }
    }

    public async Task<bool> UpdateEmployeeFromViewpointForReturnToWorkAsync(List<ViewpointEmployeeDto> employees)
    {
        var result = await UpdateEmployeeStatusInViewpointAsync(employees, ViewpointEmployeeStatus.Active);
        return result.Success;
    }
    
    public async Task<ViewpointUpdateResult> UpdateEmployeeStatusInViewpointAsync(List<ViewpointEmployeeDto> employees, string status, Core.Enums.RequestType? requestType = null)
    {
        if (employees == null || !employees.Any())
        {
            _logger.LogWarning("No employees provided for status update");
            return new ViewpointUpdateResult 
            { 
                Success = false, 
                ActualStatusUsed = status,
                TotalEmployeeCount = 0,
                SuccessfulUpdateCount = 0,
                ErrorMessage = "No employees provided for status update"
            };
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            _logger.LogWarning("No status provided for employee update");
            return new ViewpointUpdateResult 
            { 
                Success = false, 
                ActualStatusUsed = string.Empty,
                TotalEmployeeCount = employees.Count,
                SuccessfulUpdateCount = 0,
                ErrorMessage = "No status provided for employee update"
            };
        }

        var url = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/resources/actions/update";
        _logger.LogInformation("Updating {Count} employee(s) status to '{Status}' in Viewpoint API: {Url}", 
            employees.Count, status, url);

        var successCount = 0;
        var failedEmployees = new List<string>();
        var actualStatusUsed = status; // Default fallback
        var actionIds = new List<string>(); // Collect action IDs from each employee update

        foreach (var employee in employees)
        {
            try

            {
                // Determine the target status based on request type, current employee status, and company union status
                var targetStatus = await GetTargetStatusForEmployeeAsync(employee.Status, requestType, employee.HRCo);
                
                // Capture the actual status used for the first employee (assuming all employees will use the same transformed status logic)
                if (successCount == 0 && failedEmployees.Count == 0)
                {
                    actualStatusUsed = targetStatus;
                }
                
                // Log the status transformation
                if (!string.Equals(targetStatus, status, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Employee {EmployeeNumber}: Status transformation applied - Current: '{CurrentStatus}' → Target: '{TargetStatus}' (Request type: {RequestType})", 
                        employee.PREmp, employee.Status, targetStatus, requestType?.ToString() ?? "None");
                }
                else
                {
                    _logger.LogInformation("Employee {EmployeeNumber}: No status transformation needed - Using status '{Status}' (Request type: {RequestType})", 
                        employee.PREmp, targetStatus, requestType?.ToString() ?? "None");
                }

                _logger.LogInformation("Updating employee status for: {EmployeeNumber} to '{Status}'",
                    employee.PREmp, targetStatus);

                // Build the request body according to the specified format
                // For Layoff requests, also set ActiveYN to "N"
                // For ReturnToWork requests, set ActiveYN to "Y"
                object requestBody;
                if (requestType == Core.Enums.RequestType.Layoff)
                {
                    requestBody = new
                    {
                        __key = new
                        {
                            HRCo = employee.HRCo,
                            HRRef = employee.PREmp
                        },
                        Status = targetStatus,
                        ActiveYN = "N"
                    };
                    _logger.LogInformation("Layoff request - including ActiveYN='N' for employee {EmployeeNumber}", employee.PREmp);
                }
                else if (requestType == Core.Enums.RequestType.ReturnToWork)
                {
                    requestBody = new
                    {
                        __key = new
                        {
                            HRCo = employee.HRCo,
                            HRRef = employee.PREmp
                        },
                        Status = targetStatus,
                        ActiveYN = "Y"
                    };
                    _logger.LogInformation("ReturnToWork request - including ActiveYN='Y' for employee {EmployeeNumber}", employee.PREmp);
                }
                else
                {
                    requestBody = new
                    {
                        __key = new
                        {
                            HRCo = employee.HRCo,
                            HRRef = employee.PREmp
                        },
                        Status = targetStatus
                    };
                }

                var jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);
                var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending update request to Viewpoint for employee {EmployeeNumber}: {RequestBody}", 
                    employee.PREmp, jsonContent);

                var response = await _httpClient.PostAsync(url, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Successfully updated employee {EmployeeNumber} status to '{Status}' in Viewpoint. Response: {Response}",
                        employee.PREmp, targetStatus, responseContent);

                    // Parse the response to extract the action ID
                    try
                    {
                        var actionResponse = JsonSerializer.Deserialize<ViewpointActionResponseDto>(responseContent, _jsonOptions);
                        if (actionResponse != null && !string.IsNullOrEmpty(actionResponse.Id))
                        {
                            actionIds.Add(actionResponse.Id);
                            _logger.LogInformation("Captured action ID {ActionId} for employee {EmployeeNumber}",
                                actionResponse.Id, employee.PREmp);
                        }
                        else
                        {
                            _logger.LogWarning("No action ID returned for employee {EmployeeNumber} update", employee.PREmp);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse action response for employee {EmployeeNumber}, but update was successful",
                            employee.PREmp);
                    }

                    successCount++;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to update employee {EmployeeNumber} in Viewpoint. Status: {StatusCode}, Response: {Response}", 
                        employee.PREmp, response.StatusCode, errorContent);
                    failedEmployees.Add(employee.PREmp?.ToString() ?? "Unknown");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error occurred while updating employee {EmployeeNumber} in Viewpoint API",
                    employee.PREmp);
                _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"Network error occurred while updating employee {employee.PREmp}");
                failedEmployees.Add(employee.PREmp?.ToString() ?? "Unknown");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error serializing request for employee {EmployeeNumber} update in Viewpoint API",
                    employee.PREmp);
                _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"Error serializing request for employee {employee.PREmp}");
                failedEmployees.Add(employee.PREmp?.ToString() ?? "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating employee {EmployeeNumber} in Viewpoint API",
                    employee.PREmp);
                _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"Unexpected error occurred while updating employee {employee.PREmp}");
                failedEmployees.Add(employee.PREmp?.ToString() ?? "Unknown");
            }
        }

        var wasSuccessful = successCount == employees.Count;

        _logger.LogInformation("Employee status update completed. Success: {SuccessCount}/{TotalCount}. ActionIds collected: {ActionIdCount}. Failed employees: [{FailedEmployees}]",
            successCount, employees.Count, actionIds.Count, string.Join(", ", failedEmployees));

        if (wasSuccessful)
        {
            _ecmLogger.LogViewpointIntegration(true, "UpdateEmployeeStatus", "/actions/update", successCount, null);
        }
        else
        {
            var errorMsg = $"Failed to update {failedEmployees.Count} out of {employees.Count} employees";
            _ecmLogger.LogViewpointIntegration(false, "UpdateEmployeeStatus", "/actions/update", successCount, errorMsg);
        }

        return new ViewpointUpdateResult
        {
            Success = wasSuccessful,
            ActualStatusUsed = actualStatusUsed,
            TotalEmployeeCount = employees.Count,
            SuccessfulUpdateCount = successCount,
            ActionId = actionIds.FirstOrDefault(), // Set first action ID for backward compatibility
            ActionIds = actionIds, // All action IDs for multiple employee verification
            ErrorMessage = wasSuccessful ? null : $"Failed to update {failedEmployees.Count} out of {employees.Count} employees. Failed employees: [{string.Join(", ", failedEmployees)}]"
        };
    }

    /// <summary>
    /// Determines the target status for an employee based on their current status and the request type
    /// Uses the EmploymentStatusMapper table with IsUnion filtering based on company
    /// </summary>
    /// <param name="currentStatus">The employee's current status</param>
    /// <param name="requestType">The type of HR request being processed</param>
    /// <param name="companyCode">The employee's company code (HRCo) to determine union status</param>
    /// <returns>The target status that should be set for the employee</returns>
    private async Task<string> GetTargetStatusForEmployeeAsync(string? currentStatus, Core.Enums.RequestType? requestType, int? companyCode)
    {
        if (string.IsNullOrWhiteSpace(currentStatus) || !requestType.HasValue)
        {
            return currentStatus ?? string.Empty;
        }

        // Normalize the status string to handle space vs hyphen variations
        var status = currentStatus.ToUpperInvariant().Trim();

        // Determine if the employee's company is a union company
        var isUnion = await IsCompanyUnionAsync(companyCode);

        return requestType.Value switch
        {
            Core.Enums.RequestType.ReturnToWork => await GetReturnToWorkStatusAsync(status, isUnion),
            Core.Enums.RequestType.Layoff => await GetLayoffStatusAsync(status, isUnion),
            Core.Enums.RequestType.Termination => await GetTerminationStatusAsync(status, isUnion),
            Core.Enums.RequestType.Promotion => status, // Promotions typically don't change employment status
            _ => status // Return original status if no transformation rule applies
        };
    }

    /// <summary>
    /// Checks if a company is a union company based on CompanyTypeLocation table
    /// </summary>
    /// <param name="companyCode">The company code (HRCo)</param>
    /// <returns>True if the company is union, false otherwise</returns>
    private async Task<bool> IsCompanyUnionAsync(int? companyCode)
    {
        if (!companyCode.HasValue)
        {
            _logger.LogWarning("Company code is null, defaulting to non-union");
            return false;
        }

        var companyTypeLocation = await _context.CompanyTypeLocations
            .Where(c => !c.IsDeleted && c.CompanyCode == companyCode.Value)
            .FirstOrDefaultAsync();

        if (companyTypeLocation == null)
        {
            _logger.LogWarning("CompanyTypeLocation not found for company code {CompanyCode}, defaulting to non-union", companyCode);
            return false;
        }

        _logger.LogDebug("Company {CompanyCode} IsUnion={IsUnion}", companyCode, companyTypeLocation.IsUnion);
        return companyTypeLocation.IsUnion;
    }

    /// <summary>
    /// Gets the appropriate status for return to work requests from EmploymentStatusMapper table
    /// Looks up by LayOffStatus (current) to get ReturnToWorkStatus (target)
    /// </summary>
    private async Task<string> GetReturnToWorkStatusAsync(string currentStatus, bool isUnion)
    {
        var mapping = await _context.EmploymentStatusMappers
            .Where(m => m.LayOffStatus == currentStatus && m.IsUnion == isUnion)
            .FirstOrDefaultAsync();

        if (mapping != null)
        {
            _logger.LogInformation("ReturnToWork status mapping found: {CurrentStatus} -> {TargetStatus} (IsUnion={IsUnion})",
                currentStatus, mapping.ReturnToWorkStatus, isUnion);
            return mapping.ReturnToWorkStatus;
        }

        _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
            "No ReturnToWork status mapping found for status '{CurrentStatus}' with IsUnion={IsUnion}, returning original status",
            currentStatus, isUnion);
        return currentStatus;
    }

    /// <summary>
    /// Gets the appropriate layoff status from EmploymentStatusMapper table
    /// Looks up by ActiveStatus (current) to get LayOffStatus (target)
    /// </summary>
    private async Task<string> GetLayoffStatusAsync(string currentStatus, bool isUnion)
    {
        // Handle hyphenated variant - normalize "FULL-TIME" to "FULL TIME"
        var normalizedStatus = currentStatus == "FULL-TIME" ? "FULL TIME" : currentStatus;

        var mapping = await _context.EmploymentStatusMappers
            .Where(m => m.ActiveStatus == normalizedStatus && m.IsUnion == isUnion)
            .FirstOrDefaultAsync();

        if (mapping != null)
        {
            _logger.LogInformation("Layoff status mapping found: {CurrentStatus} -> {TargetStatus} (IsUnion={IsUnion})",
                currentStatus, mapping.LayOffStatus, isUnion);
            return mapping.LayOffStatus;
        }

        _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
            "No Layoff status mapping found for status '{CurrentStatus}' with IsUnion={IsUnion}, returning original status",
            currentStatus, isUnion);
        return currentStatus;
    }

    /// <summary>
    /// Gets the appropriate termination status from EmploymentStatusMapper table
    /// Looks up by ActiveStatus (current) to get TerminationStatus (target)
    /// </summary>
    private async Task<string> GetTerminationStatusAsync(string currentStatus, bool isUnion)
    {
        // Handle hyphenated variant - normalize "FULL-TIME" to "FULL TIME"
        var normalizedStatus = currentStatus == "FULL-TIME" ? "FULL TIME" : currentStatus;

        var mapping = await _context.EmploymentStatusMappers
            .Where(m => m.ActiveStatus == normalizedStatus && m.IsUnion == isUnion)
            .FirstOrDefaultAsync();

        if (mapping != null)
        {
            _logger.LogInformation("Termination status mapping found: {CurrentStatus} -> {TargetStatus} (IsUnion={IsUnion})",
                currentStatus, mapping.TerminationStatus, isUnion);
            return mapping.TerminationStatus;
        }

        // For termination, if no mapping found, default to TERM (non-union) or return original
        _ecmLogger.LogWarning(LogCategory.ViewpointIntegration,
            "No Termination status mapping found for status '{CurrentStatus}' with IsUnion={IsUnion}, defaulting to 'TERM'",
            currentStatus, isUnion);
        return isUnion ? currentStatus : "TERM";
    }

    public async Task<List<ViewpointCompanyDto>> GetAllCompaniesAsync()
    {
        try
        {
            // Get all companies from the API with pagination
            var allCompanies = await GetAllCompaniesFromApiAsync();
            
            if (allCompanies == null || !allCompanies.Any())
            {
                _ecmLogger.LogViewpointIntegration(true, "GetAllCompanies", "/companies", 0, null);
                return new List<ViewpointCompanyDto>();
            }

            _ecmLogger.LogViewpointIntegration(true, "GetAllCompanies", "/companies", allCompanies.Count, null);
            return allCompanies;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching companies from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Network error occurred while fetching companies from Viewpoint API");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing companies response from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Error deserializing companies response from Viewpoint API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching companies from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Unexpected error occurred while fetching companies from Viewpoint API");
            throw;
        }
    }

    public async Task<List<ViewpointPayrollGroupDto>> GetAllPayrollGroupsAsync()
    {
        try
        {
            // Get all payroll groups from the API with pagination
            var allPayrollGroups = await GetAllPayrollGroupsFromApiAsync();
            
            if (allPayrollGroups == null || !allPayrollGroups.Any())
            {
                _ecmLogger.LogViewpointIntegration(true, "GetAllPayrollGroups", "/payrollgroups", 0, null);
                return new List<ViewpointPayrollGroupDto>();
            }

            _ecmLogger.LogViewpointIntegration(true, "GetAllPayrollGroups", "/payrollgroups", allPayrollGroups.Count, null);
            return allPayrollGroups;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching payroll groups from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Network error occurred while fetching payroll groups from Viewpoint API");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing payroll groups response from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Error deserializing payroll groups response from Viewpoint API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching payroll groups from Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Unexpected error occurred while fetching payroll groups from Viewpoint API");
            throw;
        }
    }

    public async Task<List<ViewpointDepartmentDto>> GetAllDepartmentsAsync()
    {
        try
        {
            // Get all departments from the API with pagination
            var allDepartments = await GetAllDepartmentsFromApiAsync();
            
            if (allDepartments == null || !allDepartments.Any())
            {
                return new List<ViewpointDepartmentDto>();
            }
            
            return allDepartments;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching departments from Viewpoint API");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing departments response from Viewpoint API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching departments from Viewpoint API");
            throw;
        }
    }

    public async Task<List<ViewpointPositionDto>> GetAllPositionsAsync()
    {
        try
        {
            // Get all positions from the API with pagination
            var allPositions = await GetAllPositionsFromApiAsync();
            
            if (allPositions == null || !allPositions.Any())
            {
                return new List<ViewpointPositionDto>();
            }
            
            return allPositions;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching positions from Viewpoint API");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing positions response from Viewpoint API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching positions from Viewpoint API");
            throw;
        }
    }

    public async Task<List<ViewpointPREHEmployeeDto>> GetPREHEmployeesAsync()
    {
        try
        {
            // Get all PREH employees from the API with pagination
            var allEmployees = await GetPREHEmployeesFromApiAsync();

            if (allEmployees == null || !allEmployees.Any())
            {
                return new List<ViewpointPREHEmployeeDto>();
            }

            return allEmployees;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching PREH employees from Viewpoint API");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing PREH employees response from Viewpoint API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching PREH employees from Viewpoint API");
            throw;
        }
    }

    public async Task<List<ViewpointCraftDto>> GetAllCraftsAsync()
    {
        try
        {
            // Get all crafts from the API with pagination
            var allCrafts = await GetAllCraftsFromApiAsync();

            if (allCrafts == null || !allCrafts.Any())
            {
                return new List<ViewpointCraftDto>();
            }

            return allCrafts;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching crafts from Viewpoint API");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing crafts response from Viewpoint API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching crafts from Viewpoint API");
            throw;
        }
    }

    public async Task<List<ViewpointEmploymentStatusDto>> GetAllEmploymentStatusesAsync()
    {
        try
        {
            // Get all employment statuses from the API with filter
            var allEmploymentStatuses = await GetAllEmploymentStatusesFromApiAsync();

            if (allEmploymentStatuses == null || !allEmploymentStatuses.Any())
            {
                return new List<ViewpointEmploymentStatusDto>();
            }

            return allEmploymentStatuses;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching employment statuses from Viewpoint API");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing employment statuses response from Viewpoint API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching employment statuses from Viewpoint API");
            throw;
        }
    }

    public async Task<List<ViewpointEmployeeSalaryTypeDto>> GetAllEarningCodesAsync()
    {
        try
        {
            // Get all earning codes from the API with pagination
            var allEarningCodes = await GetAllEarningCodesFromApiAsync();

            return allEarningCodes ?? new List<ViewpointEmployeeSalaryTypeDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching earning codes from Viewpoint API");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing earning codes response from Viewpoint API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching earning codes from Viewpoint API");
            throw;
        }
    }

    private async Task<List<ViewpointCraftDto>?> GetAllCraftsFromApiAsync()
    {
        try
        {
            var baseUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/pr/2/data/crafts/cache";
            var allCrafts = new List<ViewpointCraftDto>();
            string? nextUrl = baseUrl;

            _logger.LogInformation("Fetching all crafts from Viewpoint API: {Url}", baseUrl);

            do
            {
                var response = await _httpClient.GetAsync(nextUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch crafts. Status: {StatusCode}, Reason: {ReasonPhrase}",
                        response.StatusCode, response.ReasonPhrase);

                    // If this is not the first request (we have some crafts already), return what we have
                    if (allCrafts.Any())
                    {
                        _logger.LogWarning("Returning {Count} crafts fetched before error occurred", allCrafts.Count);
                        return allCrafts;
                    }

                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Empty response received from Viewpoint API");
                    break;
                }

                // Log the raw response for debugging
                _logger.LogInformation("Raw crafts response batch: {Response}", content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);

                // Try to parse as wrapper object first (preferred format with Next URL)
                try
                {
                    var viewpointResponse = JsonSerializer.Deserialize<ViewpointCraftsApiResponse>(content, _jsonOptions);

                    if (viewpointResponse?.Data != null)
                    {
                        var crafts = viewpointResponse.Data;
                        allCrafts.AddRange(crafts);

                        // Use next URL from response for subsequent requests
                        nextUrl = viewpointResponse.Next;

                        _logger.LogInformation("Fetched {Count} crafts in this batch (wrapper). Total so far: {TotalCount}. HasMore: {HasMore}",
                            crafts.Count, allCrafts.Count, !string.IsNullOrEmpty(nextUrl));
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse crafts response as wrapper format");
                        break;
                    }
                }
                catch (JsonException)
                {
                    // Fallback: try to parse as direct array
                    var craftsArray = JsonSerializer.Deserialize<List<ViewpointCraftDto>>(content, _jsonOptions);
                    if (craftsArray != null)
                    {
                        allCrafts.AddRange(craftsArray);
                        _logger.LogInformation("Fetched {Count} crafts in this batch (direct array). Total so far: {TotalCount}",
                            craftsArray.Count, allCrafts.Count);
                        // No next URL in direct array format
                        nextUrl = null;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse crafts response in both formats");
                        break;
                    }
                }

            } while (!string.IsNullOrEmpty(nextUrl));

            _logger.LogInformation("Successfully fetched all {TotalCount} crafts from Viewpoint API", allCrafts.Count);
            return allCrafts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all crafts from Viewpoint API");
            throw;
        }
    }

    private async Task<List<ViewpointEmploymentStatusDto>?> GetAllEmploymentStatusesFromApiAsync()
    {
        try
        {
            // First call: employment-status codes (Type = "H") filtered by Notes values.
            var notesFilterRequest = new ViewpointSearchRequest
            {
                Filters = new List<ViewpointSearchFilter>
                {
                    new ViewpointSearchFilter
                    {
                        PropertyName = "Notes",
                        Value = new List<string>
                        {
                            "ACTIVE",
                            "UNION ACTIVE",
                            "CHANGE",
                            "TERM",
                            "UNION TERM",
                            "LAYOFF",
                            "UNION LAYOFF"
                        },
                        Operator = "In"
                    }
                }
            };

            var notesResults = await SearchEmploymentStatusCodesAsync(notesFilterRequest, "Notes-in");

            // Second call: termination-reason codes (Type = "N").
            var typeFilterRequest = new ViewpointSearchRequest
            {
                Filters = new List<ViewpointSearchFilter>
                {
                    new ViewpointSearchFilter
                    {
                        PropertyName = "Type",
                        Value = "N",
                        Operator = "Equal"
                    }
                }
            };

            var typeResults = await SearchEmploymentStatusCodesAsync(typeFilterRequest, "Type-N");

            // Merge results, deduplicating by (HRCo, Code) in case a row matches both filters.
            var combined = new List<ViewpointEmploymentStatusDto>();
            combined.AddRange(notesResults);
            combined.AddRange(typeResults);

            var deduped = combined
                .GroupBy(s => new { s.HRCo, s.Code, s.Type })
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("Combined employment statuses: {Notes} (Notes filter) + {Type} (Type=N filter) = {Total} unique records",
                notesResults.Count, typeResults.Count, deduped.Count);

            return deduped;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all employment statuses from Viewpoint API");
            throw;
        }
    }

    private async Task<List<ViewpointEmploymentStatusDto>> SearchEmploymentStatusCodesAsync(ViewpointSearchRequest searchRequest, string filterLabel)
    {
        var baseUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/codes/cache/search";
        _logger.LogInformation("Fetching employment statuses from Viewpoint API ({Filter}): {Url}", filterLabel, baseUrl);

        var jsonContent = JsonSerializer.Serialize(searchRequest, _jsonOptions);
        _logger.LogInformation("Employment status search request JSON ({Filter}): {JsonContent}", filterLabel, jsonContent);

        var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(baseUrl, httpContent);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch employment statuses ({Filter}). Status: {StatusCode}, Reason: {ReasonPhrase}",
                filterLabel, response.StatusCode, response.ReasonPhrase);
            return new List<ViewpointEmploymentStatusDto>();
        }

        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrEmpty(content))
        {
            _logger.LogWarning("Empty response received from Viewpoint API ({Filter})", filterLabel);
            return new List<ViewpointEmploymentStatusDto>();
        }

        _logger.LogInformation("Raw employment statuses response ({Filter}): {Response}",
            filterLabel, content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);

        try
        {
            var viewpointResponse = JsonSerializer.Deserialize<ViewpointEmploymentStatusesApiResponse>(content, _jsonOptions);

            if (viewpointResponse?.Data != null)
            {
                _logger.LogInformation("Successfully fetched {Count} employment statuses ({Filter})",
                    viewpointResponse.Data.Count, filterLabel);
                return viewpointResponse.Data;
            }

            _logger.LogWarning("Failed to parse employment statuses response as wrapper format ({Filter})", filterLabel);
            return new List<ViewpointEmploymentStatusDto>();
        }
        catch (JsonException)
        {
            var statusesArray = JsonSerializer.Deserialize<List<ViewpointEmploymentStatusDto>>(content, _jsonOptions);
            if (statusesArray != null)
            {
                _logger.LogInformation("Successfully fetched {Count} employment statuses (direct array, {Filter})",
                    statusesArray.Count, filterLabel);
                return statusesArray;
            }

            _logger.LogWarning("Failed to parse employment statuses response in both formats ({Filter})", filterLabel);
            return new List<ViewpointEmploymentStatusDto>();
        }
    }

    private async Task<List<ViewpointEmployeeSalaryTypeDto>?> GetAllEarningCodesFromApiAsync()
    {
        try
        {
            var baseUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/pr/2/data/earning_codes/cache";
            var allEarningCodes = new List<ViewpointEmployeeSalaryTypeDto>();
            string? nextUrl = baseUrl;

            _logger.LogInformation("Fetching all earning codes from Viewpoint API: {Url}", baseUrl);

            do
            {
                var response = await _httpClient.GetAsync(nextUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch earning codes. Status: {StatusCode}, Reason: {ReasonPhrase}",
                        response.StatusCode, response.ReasonPhrase);

                    // If this is not the first request (we have some earning codes already), return what we have
                    if (allEarningCodes.Any())
                    {
                        _logger.LogWarning("Returning {Count} earning codes fetched before error occurred", allEarningCodes.Count);
                        return allEarningCodes;
                    }

                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Empty response received from Viewpoint API");
                    break;
                }

                // Try to parse as wrapper object first (preferred format with Next URL)
                try
                {
                    var viewpointResponse = JsonSerializer.Deserialize<ViewpointEarningCodesApiResponse>(content, _jsonOptions);

                    if (viewpointResponse?.Data != null)
                    {
                        var earningCodes = viewpointResponse.Data;
                        allEarningCodes.AddRange(earningCodes);

                        // Use next URL from response for subsequent requests
                        nextUrl = viewpointResponse.Next;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse earning codes response as wrapper format");
                        break;
                    }
                }
                catch (JsonException)
                {
                    // Fallback: try to parse as direct array
                    var earningCodesArray = JsonSerializer.Deserialize<List<ViewpointEmployeeSalaryTypeDto>>(content, _jsonOptions);
                    if (earningCodesArray != null)
                    {
                        allEarningCodes.AddRange(earningCodesArray);
                        // No next URL in direct array format
                        nextUrl = null;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse earning codes response in both formats");
                        break;
                    }
                }

            } while (!string.IsNullOrEmpty(nextUrl));

            _logger.LogInformation("Successfully fetched all {TotalCount} earning codes from Viewpoint API", allEarningCodes.Count);
            return allEarningCodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all earning codes from Viewpoint API");
            throw;
        }
    }

    public async Task<UpdateEmployeeNewHireResultDto> UpdateEmployeeForNewHireInViewPointAsync(UpdateEmployeeNewHireRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting new hire employee update process for HRCo: {HRCo}, HRRef: {HRRef}, LastName: {LastName}",
                request.HRCo, request.HRRef, request.LastName);

            var result = new UpdateEmployeeNewHireResultDto
            {
                Success = false,
                EmployeeFound = false,
                UpdateQueued = false
            };

            // Step 1: Search for the employee to verify they exist
            _logger.LogInformation("Searching for employee in Viewpoint: HRCo={HRCo}, PRDept={PRDept}, LastName={LastName}, HireDate={HireDate}",
                request.HRCo, request.PRDept, request.LastName, request.HireDate);

            var searchResults = await SearchEmployeeInNewHireWithAPIAsync(
                request.HRCo,
                request.PRDept ?? string.Empty,
                request.LastName ?? string.Empty,
                request.HireDate ?? string.Empty
            );

            // Step 2: Check if employee exists
            if (searchResults == null || !searchResults.Any())
            {
                _logger.LogWarning("Employee not found in Viewpoint with provided search criteria");
                result.Message = "Employee not found in Viewpoint system with the provided criteria";
                result.ErrorMessage = "No matching employee found. Please verify the employee details and ensure they exist in Viewpoint.";
                return result;
            }

            // If multiple employees found, this could be an issue
            if (searchResults.Count > 1)
            {
                _logger.LogWarning("Multiple employees ({Count}) found matching search criteria. Using first match.", searchResults.Count);
            }

            var employee = searchResults.First();
            result.EmployeeFound = true;
            result.Employee = employee;

            _logger.LogInformation("Employee found in Viewpoint: HRRef={HRRef}, Name={FirstName} {LastName}",
                employee.HRRef, employee.FirstName, employee.LastName);

            // Step 3: Verify HRRef matches if provided
            if (request.HRRef > 0 && employee.HRRef != request.HRRef)
            {
                _logger.LogWarning("HRRef mismatch. Requested: {RequestedHRRef}, Found: {FoundHRRef}",
                    request.HRRef, employee.HRRef);
                result.Message = $"HRRef mismatch. Requested: {request.HRRef}, but found employee with HRRef: {employee.HRRef}";
                result.ErrorMessage = "The employee reference number does not match the employee found in Viewpoint.";
                return result;
            }

            // Step 4: Prepare update request
            var updateUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/resources/actions/update";

            //SET FOR THE MEANTIME CUSTOM FIELD udWorkEmail COZ WE NOT YET USE AD CREATION FOR FETCHING udWorkEmail
            request.CustomFields.udWorkEmail = "testWorkEmail@AdCreation.com";

            var updateRequestBody = new
            {
                __key = new
                {
                    HRCo = request.HRCo,
                    HRRef = employee.HRRef ?? request.HRRef
                },
                __custom_fields = request.CustomFields
            };

            var jsonContent = JsonSerializer.Serialize(updateRequestBody, _jsonOptions);
            _logger.LogInformation("Sending update request to Viewpoint: {RequestBody}", jsonContent);

            var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Step 5: Send update request
            var updateResponse = await _httpClient.PostAsync(updateUrl, httpContent);

            if (!updateResponse.IsSuccessStatusCode)
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to queue employee update in Viewpoint. Status: {StatusCode}, Response: {Response}",
                    updateResponse.StatusCode, errorContent);
                _ecmLogger.LogViewpointIntegration(false, "UpdateEmployeeForNewHire", "/actions/update", 0, $"API returned status {updateResponse.StatusCode}");
                result.Message = "Failed to queue employee update in Viewpoint";
                result.ErrorMessage = $"API returned status {updateResponse.StatusCode}: {errorContent}";
                return result;
            }

            var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Update queued successfully. Response: {Response}", updateResponseContent);

            var actionResponse = JsonSerializer.Deserialize<ViewpointActionResponseDto>(updateResponseContent, _jsonOptions);

            if (actionResponse == null || string.IsNullOrEmpty(actionResponse.Id))
            {
                _logger.LogError("Failed to parse action response or action ID is missing");
                _ecmLogger.LogViewpointIntegration(false, "UpdateEmployeeForNewHire", "/actions/update", 0, "Invalid response format - action ID missing");
                result.Message = "Update request sent but action ID not received";
                result.ErrorMessage = "Invalid response format from Viewpoint API";
                return result;
            }

            result.UpdateQueued = true;
            result.ActionId = actionResponse.Id;
            result.ActionStatus = actionResponse.Status;
            result.Success = true;
            result.Message = $"Employee {employee.FirstName} {employee.LastName} (HRRef: {employee.HRRef}) update action queued successfully in Viewpoint";

            _logger.LogInformation("Update action queued successfully with ID: {ActionId}, Status: {Status}",
                actionResponse.Id, actionResponse.Status);
            _ecmLogger.LogViewpointIntegration(true, "UpdateEmployeeForNewHire", "/actions/update", 1, null);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while updating employee for new hire in Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Network error occurred while updating employee for new hire");
            return new UpdateEmployeeNewHireResultDto
            {
                Success = false,
                Message = "Network error occurred during update",
                ErrorMessage = ex.Message
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON error occurred while processing new hire employee update");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "JSON error occurred while processing new hire employee update");
            return new UpdateEmployeeNewHireResultDto
            {
                Success = false,
                Message = "Data format error occurred during update",
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating employee for new hire in Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Unexpected error occurred while updating employee for new hire");
            return new UpdateEmployeeNewHireResultDto
            {
                Success = false,
                Message = "Unexpected error occurred during update",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Updates employee in Viewpoint for promotion/transfer request
    /// Updates: PRCo, PRGroup, PRDept, PositionCode, Status, udSupervisor, udPhysicalLocation
    /// </summary>
    public async Task<ViewpointUpdateResult> UpdateEmployeeForPromotionTransferInViewPointAsync(ViewpointEmployeeDto employee)
    {
        try
        {
            _logger.LogInformation("Starting promotion/transfer employee update process for HRRef: {HRRef}",
                employee.HRRef);

            var result = new ViewpointUpdateResult
            {
                Success = false
            };

            if (employee.HRRef == null || employee.HRCo == null)
            {
                _logger.LogWarning("Employee HRRef or HRCo is null");
                result.ErrorMessage = "Employee HRRef or HRCo is missing";
                return result;
            }

            // Prepare update request
            var updateUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/resources/actions/update";

            var updateRequestBody = new
            {
                __key = new
                {
                    HRCo = employee.HRCo,
                    HRRef = employee.HRRef
                },
                PRCo = employee.PRCo,
                PRGroup = employee.PRGroup,
                PRDept = employee.PRDept,
                PositionCode = employee.PositionCode,
                Status = employee.Status,
                EarnCode = employee.EarnCode,
                __custom_fields = new
                {
                    udSupervisor = employee.CustomFields?.SupervisorId,
                    udPhysicalLocation = employee.CustomFields?.PhysicalLocation
                }
            };

            var jsonContent = JsonSerializer.Serialize(updateRequestBody, _jsonOptions);
            _logger.LogInformation("Sending promotion/transfer update request to Viewpoint: {RequestBody}", jsonContent);

            var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Send update request
            var updateResponse = await _httpClient.PostAsync(updateUrl, httpContent);

            if (!updateResponse.IsSuccessStatusCode)
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to queue promotion/transfer update in Viewpoint. Status: {StatusCode}, Response: {Response}",
                    updateResponse.StatusCode, errorContent);
                _ecmLogger.LogViewpointIntegration(false, "UpdateEmployeeForPromotionTransfer", "/actions/update", 0, $"API returned status {updateResponse.StatusCode}");
                result.ErrorMessage = $"API returned status {updateResponse.StatusCode}: {errorContent}";
                return result;
            }

            var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Promotion/transfer update queued successfully. Response: {Response}", updateResponseContent);

            var actionResponse = JsonSerializer.Deserialize<ViewpointActionResponseDto>(updateResponseContent, _jsonOptions);

            if (actionResponse == null || string.IsNullOrEmpty(actionResponse.Id))
            {
                _logger.LogError("Failed to parse action response or action ID is missing");
                _ecmLogger.LogViewpointIntegration(false, "UpdateEmployeeForPromotionTransfer", "/actions/update", 0, "Invalid response format - action ID missing");
                result.ErrorMessage = "Update request sent but action ID not received";
                return result;
            }

            result.Success = true;
            result.ActionId = actionResponse.Id; // Store the action ID for verification
            result.ActualStatusUsed = employee.Status; // Return the status that was set
            _logger.LogInformation("Promotion/transfer update action queued successfully with ID: {ActionId}, Status: {Status}",
                actionResponse.Id, actionResponse.Status);
            _ecmLogger.LogViewpointIntegration(true, "UpdateEmployeeForPromotionTransfer", "/actions/update", 1, null);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while updating employee for promotion/transfer in Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Network error occurred while updating employee for promotion/transfer");
            return new ViewpointUpdateResult
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON error occurred while processing promotion/transfer employee update");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "JSON error occurred while processing promotion/transfer employee update");
            return new ViewpointUpdateResult
            {
                Success = false,
                ErrorMessage = $"Data format error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating employee for promotion/transfer in Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Unexpected error occurred while updating employee for promotion/transfer");
            return new ViewpointUpdateResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Updates employee in Viewpoint for termination request
    /// Updates: Status, ActiveYN, TermDate, TermReason
    /// </summary>
    public async Task<ViewpointUpdateResult> UpdateEmployeeForTerminationInViewPointAsync(ViewpointEmployeeDto employee, DateTime? termDate, string? termReason)
    {
        try
        {
            _logger.LogInformation("Starting termination employee update process for HRRef: {HRRef}, TermDate: {TermDate}, TermReason: {TermReason}",
                employee.HRRef, termDate, termReason);

            var result = new ViewpointUpdateResult
            {
                Success = false
            };

            if (employee.HRRef == null || employee.HRCo == null)
            {
                _logger.LogWarning("Employee HRRef or HRCo is null");
                result.ErrorMessage = "Employee HRRef or HRCo is missing";
                return result;
            }

            // Determine the target status based on current employee status and company union status
            var isUnion = await IsCompanyUnionAsync(employee.HRCo);
            var targetStatus = await GetTerminationStatusAsync(employee.Status?.ToUpperInvariant().Trim() ?? string.Empty, isUnion);

            _logger.LogInformation("Termination status transformation: Current '{CurrentStatus}' → Target '{TargetStatus}' (IsUnion={IsUnion})",
                employee.Status, targetStatus, isUnion);

            // Prepare update request
            var updateUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/resources/actions/update";

            var updateRequestBody = new
            {
                __key = new
                {
                    HRCo = employee.HRCo,
                    HRRef = employee.HRRef
                },
                Status = targetStatus,
                ActiveYN = "N",
                TermDate = termDate?.ToString("yyyy-MM-dd"),
                TermReason = termReason
            };

            var jsonContent = JsonSerializer.Serialize(updateRequestBody, _jsonOptions);
            _logger.LogInformation("Sending termination update request to Viewpoint: {RequestBody}", jsonContent);

            var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Send update request
            var updateResponse = await _httpClient.PostAsync(updateUrl, httpContent);

            if (!updateResponse.IsSuccessStatusCode)
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to queue termination update in Viewpoint. Status: {StatusCode}, Response: {Response}",
                    updateResponse.StatusCode, errorContent);
                _ecmLogger.LogViewpointIntegration(false, "UpdateEmployeeForTermination", "/actions/update", 0, $"API returned status {updateResponse.StatusCode}");
                result.ErrorMessage = $"API returned status {updateResponse.StatusCode}: {errorContent}";
                return result;
            }

            var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Termination update queued successfully. Response: {Response}", updateResponseContent);

            var actionResponse = JsonSerializer.Deserialize<ViewpointActionResponseDto>(updateResponseContent, _jsonOptions);

            if (actionResponse == null || string.IsNullOrEmpty(actionResponse.Id))
            {
                _logger.LogError("Failed to parse action response or action ID is missing");
                _ecmLogger.LogViewpointIntegration(false, "UpdateEmployeeForTermination", "/actions/update", 0, "Invalid response format - action ID missing");
                result.ErrorMessage = "Update request sent but action ID not received";
                return result;
            }

            result.Success = true;
            result.ActionId = actionResponse.Id;
            result.ActualStatusUsed = targetStatus;
            _logger.LogInformation("Termination update action queued successfully with ID: {ActionId}, Status: {Status}",
                actionResponse.Id, actionResponse.Status);
            _ecmLogger.LogViewpointIntegration(true, "UpdateEmployeeForTermination", "/actions/update", 1, null);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while updating employee for termination in Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Network error occurred while updating employee for termination");
            return new ViewpointUpdateResult
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON error occurred while processing termination employee update");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "JSON error occurred while processing termination employee update");
            return new ViewpointUpdateResult
            {
                Success = false,
                ErrorMessage = $"Data format error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating employee for termination in Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Unexpected error occurred while updating employee for termination");
            return new ViewpointUpdateResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Updates employee in Viewpoint for return to work request
    /// Updates: Status, ActiveYN, udReturntoworkdate (custom field)
    /// </summary>
    public async Task<ViewpointUpdateResult> UpdateEmployeeForReturnToWorkInViewPointAsync(ViewpointEmployeeDto employee, DateTime? returnToWorkDate)
    {
        try
        {
            _logger.LogInformation("Starting return to work employee update process for HRRef: {HRRef}, ReturnToWorkDate: {ReturnToWorkDate}",
                employee.HRRef, returnToWorkDate);

            var result = new ViewpointUpdateResult
            {
                Success = false
            };

            if (employee.HRRef == null || employee.HRCo == null)
            {
                _logger.LogWarning("Employee HRRef or HRCo is null");
                result.ErrorMessage = "Employee HRRef or HRCo is missing";
                return result;
            }

            // Determine the target status based on current employee status and company union status
            var isUnion = await IsCompanyUnionAsync(employee.HRCo);
            var targetStatus = await GetReturnToWorkStatusAsync(employee.Status?.ToUpperInvariant().Trim() ?? string.Empty, isUnion);

            _logger.LogInformation("Return to work status transformation: Current '{CurrentStatus}' → Target '{TargetStatus}' (IsUnion={IsUnion})",
                employee.Status, targetStatus, isUnion);

            // Prepare update request
            var updateUrl = $"{_settings.BaseUrl}/{_settings.SubscriberCode}/vista/hr/2/data/resources/actions/update";

            var updateRequestBody = new
            {
                __key = new
                {
                    HRCo = employee.HRCo,
                    HRRef = employee.HRRef
                },
                Status = targetStatus,
                ActiveYN = "Y",
                __custom_fields = new
                {
                    udReturntoworkdate = returnToWorkDate?.ToString("yyyy-MM-dd")
                }
            };

            var jsonContent = JsonSerializer.Serialize(updateRequestBody, _jsonOptions);
            _logger.LogInformation("Sending return to work update request to Viewpoint: {RequestBody}", jsonContent);

            var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Send update request
            var updateResponse = await _httpClient.PostAsync(updateUrl, httpContent);

            if (!updateResponse.IsSuccessStatusCode)
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to queue return to work update in Viewpoint. Status: {StatusCode}, Response: {Response}",
                    updateResponse.StatusCode, errorContent);
                _ecmLogger.LogViewpointIntegration(false, "UpdateEmployeeForReturnToWork", "/actions/update", 0, $"API returned status {updateResponse.StatusCode}");
                result.ErrorMessage = $"API returned status {updateResponse.StatusCode}: {errorContent}";
                return result;
            }

            var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Return to work update queued successfully. Response: {Response}", updateResponseContent);

            var actionResponse = JsonSerializer.Deserialize<ViewpointActionResponseDto>(updateResponseContent, _jsonOptions);

            if (actionResponse == null || string.IsNullOrEmpty(actionResponse.Id))
            {
                _logger.LogError("Failed to parse action response or action ID is missing");
                _ecmLogger.LogViewpointIntegration(false, "UpdateEmployeeForReturnToWork", "/actions/update", 0, "Invalid response format - action ID missing");
                result.ErrorMessage = "Update request sent but action ID not received";
                return result;
            }

            result.Success = true;
            result.ActionId = actionResponse.Id;
            result.ActualStatusUsed = targetStatus;
            _logger.LogInformation("Return to work update action queued successfully with ID: {ActionId}, Status: {Status}",
                actionResponse.Id, actionResponse.Status);
            _ecmLogger.LogViewpointIntegration(true, "UpdateEmployeeForReturnToWork", "/actions/update", 1, null);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while updating employee for return to work in Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Network error occurred while updating employee for return to work");
            return new ViewpointUpdateResult
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON error occurred while processing return to work employee update");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "JSON error occurred while processing return to work employee update");
            return new ViewpointUpdateResult
            {
                Success = false,
                ErrorMessage = $"Data format error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating employee for return to work in Viewpoint API");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, "Unexpected error occurred while updating employee for return to work");
            return new ViewpointUpdateResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Verifies a Viewpoint action status by action ID
    /// Checks if the update was successfully applied and retrieves the updated data
    /// Can be used by any request type (Promotion, New Hire, Layoff, Termination, etc.)
    /// </summary>
    public async Task<ViewpointActionDetailResponseDto?> VerifyViewpointActionAsync(string actionId)
    {
        try
        {
            _logger.LogInformation("Verifying Viewpoint action status for action ID: {ActionId}", actionId);

            var verificationUrl = $"{_settings.ActionVerificationUrl}/{actionId}";
            _logger.LogInformation("Verification URL: {Url}", verificationUrl);

            var response = await _httpClient.GetAsync(verificationUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to verify action status. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Verification response received: {Response}", responseContent);

            var actionDetails = JsonSerializer.Deserialize<ViewpointActionDetailResponseDto>(responseContent, _jsonOptions);

            if (actionDetails == null)
            {
                _logger.LogError("Failed to deserialize verification response");
                return null;
            }

            _logger.LogInformation("Action verification completed. Status: {Status}", actionDetails.Status);
            _ecmLogger.LogViewpointIntegration(true, "VerifyViewpointAction", $"/actions/{actionId}", 1, null);
            return actionDetails;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while verifying action status");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"Network error occurred while verifying action {actionId}");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON error occurred while processing verification response");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"JSON error occurred while processing verification response for action {actionId}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while verifying action status");
            _ecmLogger.LogError(LogCategory.ViewpointIntegration, ex, $"Unexpected error occurred while verifying action {actionId}");
            return null;
        }
    }

}

// DTOs for the actual Viewpoint API response format
public class ViewpointApiResponse
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("data")]
    public List<ViewpointEmployeeDto>? Data { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
}

public class ViewpointCompaniesApiResponse
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("data")]
    public List<ViewpointCompanyDto>? Data { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
}

public class ViewpointPayrollGroupsApiResponse
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("data")]
    public List<ViewpointPayrollGroupDto>? Data { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
}

public class ViewpointDepartmentsApiResponse
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("data")]
    public List<ViewpointDepartmentDto>? Data { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
}

public class ViewpointPositionsApiResponse
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("data")]
    public List<ViewpointPositionDto>? Data { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
}

public class ViewpointPREHEmployeesApiResponse
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("data")]
    public List<ViewpointPREHEmployeeDto>? Data { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
}

public class ViewpointCraftsApiResponse
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("data")]
    public List<ViewpointCraftDto>? Data { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
}

public class ViewpointEmploymentStatusesApiResponse
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("data")]
    public List<ViewpointEmploymentStatusDto>? Data { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
}

public class ViewpointEarningCodesApiResponse
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("data")]
    public List<ViewpointEmployeeSalaryTypeDto>? Data { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
}

public class ViewpointApiEmployeeData
{
    [JsonPropertyName("PRCo")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? PRCo { get; set; }

    [JsonPropertyName("Employee")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? Employee { get; set; }

    [JsonPropertyName("LastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("FirstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("MidName")]
    public string? MidName { get; set; }

    [JsonPropertyName("SortName")]
    public string? SortName { get; set; }

    [JsonPropertyName("Address")]
    public string? Address { get; set; }

    [JsonPropertyName("Address2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("City")]
    public string? City { get; set; }

    [JsonPropertyName("State")]
    public string? State { get; set; }

    [JsonPropertyName("Zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("Phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("CellPhone")]
    public string? CellPhone { get; set; }

    [JsonPropertyName("Email")]
    public string? Email { get; set; }

    [JsonPropertyName("HireDate")]
    public string? HireDate { get; set; }

    [JsonPropertyName("TermDate")]
    public string? TermDate { get; set; }

    [JsonPropertyName("PRGroup")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? PRGroup { get; set; }

    [JsonPropertyName("PRDept")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? PRDept { get; set; }

    [JsonPropertyName("Craft")]
    public string? Craft { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("ActiveYN")]
    public string? ActiveYN { get; set; }
}
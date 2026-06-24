using System.Text;
using System.Text.Json;
using IdentityModel.Client;
using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mathy.ELM.Infrastructure.Services;

/// <summary>
/// Service for integrating with ManageEngine Service Desk Plus
/// Creates tickets for new hire requests with all necessary IT provisioning information
/// </summary>
public class ServiceDeskIntegrationService : IServiceDeskIntegrationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceDeskIntegrationService> _logger;
    private readonly IEcmLogger _ecmLogger;
    private readonly HttpClient _httpClient;

    public ServiceDeskIntegrationService(
        IConfiguration configuration,
        ILogger<ServiceDeskIntegrationService> logger,
        IEcmLogger ecmLogger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _ecmLogger = ecmLogger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Creates a ServiceDesk ticket for a new hire request
    /// </summary>
    public async Task<ServiceDeskRecordResponseDto> CreateServiceDeskRecord(CreateServiceDeskRecordDto request)
    {
        try
        {
            _logger.LogInformation($"[ServiceDesk] Creating New Hire ticket for {request.FirstName} {request.LastName}");

            // Validate required fields
            if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName) ||
                string.IsNullOrEmpty(request.NetworkUserName))
            {
                _ecmLogger.LogServiceTicket(false, "CREATE", null, "NewHire", "Required fields (FirstName, LastName, NetworkUserName) are missing", employeeName: $"{request.FirstName} {request.LastName}".Trim());
                return new ServiceDeskRecordResponseDto
                {
                    Success = false,
                    Message = "Required fields (FirstName, LastName, NetworkUserName) are missing"
                };
            }

            // Build the ServiceDesk request JSON
            var serviceDeskRequest = BuildNewHireServiceDeskRequest(request);

            // Send to ServiceDesk API
            var result = await SendServiceDeskRequest(serviceDeskRequest, $"{request.FirstName} {request.LastName}", "NewHire");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ServiceDesk] Exception occurred while creating New Hire ServiceDesk record");
            _ecmLogger.LogServiceTicket(false, "CREATE", null, "NewHire", ex.Message, employeeName: $"{request.FirstName} {request.LastName}".Trim());
            return new ServiceDeskRecordResponseDto
            {
                Success = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Creates a ServiceDesk ticket for a promotion/transfer request
    /// </summary>
    public async Task<ServiceDeskRecordResponseDto> CreatePromotionServiceDeskRecord(CreatePromotionServiceDeskRecordDto request)
    {
        try
        {
            _logger.LogInformation($"[ServiceDesk] Creating Promotion/Transfer ticket for {request.FirstName} {request.LastName}");

            // Validate required fields
            if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName))
            {
                _ecmLogger.LogServiceTicket(false, "CREATE", null, "Promotion", "Required fields (FirstName, LastName) are missing", employeeName: $"{request.FirstName} {request.LastName}".Trim());
                return new ServiceDeskRecordResponseDto
                {
                    Success = false,
                    Message = "Required fields (FirstName, LastName) are missing"
                };
            }

            // Build the ServiceDesk request JSON
            var serviceDeskRequest = BuildPromotionServiceDeskRequest(request);

            // Send to ServiceDesk API
            var result = await SendServiceDeskRequest(serviceDeskRequest, $"{request.FirstName} {request.LastName}", "Promotion");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ServiceDesk] Exception occurred while creating Promotion/Transfer ServiceDesk record");
            _ecmLogger.LogServiceTicket(false, "CREATE", null, "Promotion", ex.Message, employeeName: $"{request.FirstName} {request.LastName}".Trim());
            return new ServiceDeskRecordResponseDto
            {
                Success = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Shared method to send ServiceDesk request to the API
    /// </summary>
    private async Task<ServiceDeskRecordResponseDto> SendServiceDeskRequest(dynamic serviceDeskRequest, string employeeName, string requestType = "Unknown")
    {
        // Get configuration
        var serviceDeskConfig = _configuration.GetSection("ServiceDeskPlus");
        var basePath = serviceDeskConfig["BasePath"];
        var refreshTokenAddress = serviceDeskConfig["RefreshTokenAddress"];
        var clientId = serviceDeskConfig["ClientID"];
        var clientSecret = serviceDeskConfig["ClientSecret"];
        var refreshToken = serviceDeskConfig["RefreshToken"];
        var isActive = serviceDeskConfig["IsActive"]?.ToLower();

        // Check if ServiceDesk is enabled
        if (isActive != "yes")
        {
            _logger.LogWarning($"[ServiceDesk] ServiceDesk integration is disabled");
            _ecmLogger.LogServiceTicket(false, "CREATE", null, requestType, "ServiceDesk integration is not enabled", employeeName: employeeName);
            return new ServiceDeskRecordResponseDto
            {
                Success = false,
                Message = "ServiceDesk integration is not enabled"
            };
        }

        // Validate configuration
        if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(clientId) ||
            string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogError($"[ServiceDesk] Missing configuration for ServiceDesk integration");
            _ecmLogger.LogServiceTicket(false, "CREATE", null, requestType, "ServiceDesk configuration is incomplete", employeeName: employeeName);
            return new ServiceDeskRecordResponseDto
            {
                Success = false,
                Message = "ServiceDesk configuration is incomplete"
            };
        }

        // Get OAuth token
        _logger.LogInformation($"[ServiceDesk] Requesting OAuth token");
        var token = await _httpClient.RequestRefreshTokenAsync(new()
        {
            Address = refreshTokenAddress,
            ClientId = clientId,
            ClientSecret = clientSecret,
            RefreshToken = refreshToken
        });

        if (token == null || string.IsNullOrEmpty(token.AccessToken))
        {
            _logger.LogError($"[ServiceDesk] Failed to obtain access token");
            _ecmLogger.LogServiceTicket(false, "CREATE", null, requestType, "Failed to obtain ServiceDesk access token", employeeName: employeeName);
            return new ServiceDeskRecordResponseDto
            {
                Success = false,
                Message = "Failed to obtain ServiceDesk access token"
            };
        }

        _logger.LogInformation($"[ServiceDesk] Access token obtained successfully");

        // Encode the JSON for form submission
        _logger.LogDebug($"[ServiceDesk] Request payload: {JsonSerializer.Serialize(serviceDeskRequest)}");
        var encodedJson = System.Web.HttpUtility.UrlEncode(JsonSerializer.Serialize(serviceDeskRequest));
        var stringContent = new StringContent($"input_data={encodedJson}", Encoding.UTF8, "application/x-www-form-urlencoded");

        // Create request message with proper headers
        var requestUri = new Uri(basePath).ToString().TrimEnd('/') + "/requests";
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = stringContent
        };
        requestMessage.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
        requestMessage.Headers.Add("Accept", "application/vnd.manageengine.sdp.v3+json");

        // Send request to ServiceDesk
        _logger.LogInformation($"[ServiceDesk] Sending ticket creation request to {requestUri}");
        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"[ServiceDesk] Failed to create ticket. Status: {response.StatusCode}, Error: {errorContent}");
            _ecmLogger.LogServiceTicket(false, "CREATE", null, requestType, $"ServiceDesk API returned error: {response.StatusCode}", employeeName: employeeName);
            return new ServiceDeskRecordResponseDto
            {
                Success = false,
                Message = $"ServiceDesk API returned error: {response.StatusCode} - {errorContent}"
            };
        }

        // Parse response
        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"[ServiceDesk] Response: {responseContent}");

        // Extract ticket ID from response
        var ticketId = ExtractTicketId(responseContent);

        if (string.IsNullOrEmpty(ticketId))
        {
            _logger.LogWarning($"[ServiceDesk] Could not extract ticket ID from response");
            _ecmLogger.LogServiceTicket(true, "CREATE", "Unknown", requestType, employeeName: employeeName);
            return new ServiceDeskRecordResponseDto
            {
                Success = true,
                Message = "Ticket created but could not extract ticket ID from response",
                ServiceDeskTicketId = "Unknown"
            };
        }

        _logger.LogInformation($"[ServiceDesk] SUCCESS: Ticket created with ID {ticketId} for {employeeName}");
        _ecmLogger.LogServiceTicket(true, "CREATE", ticketId, requestType, employeeName: employeeName);
        return new ServiceDeskRecordResponseDto
        {
            Success = true,
            ServiceDeskTicketId = ticketId,
            Message = $"ServiceDesk ticket created successfully: {ticketId}"
        };
    }

    /// <summary>
    /// Builds the ServiceDesk request JSON structure for New Hire requests
    /// </summary>
    private dynamic BuildNewHireServiceDeskRequest(CreateServiceDeskRecordDto request)
    {
        var description = BuildDescription(request);
        var udfFields = BuildUDFFields(request);

        // Get service center from configuration
        var serviceDeskConfig = _configuration.GetSection("ServiceDeskPlus");
        var serviceCenter = serviceDeskConfig["ServiceCenter"] ?? "Service Center";

        var serviceDeskRequest = new
        {
            request = new
            {
                template = new { name = "On-Boarding" },
                subject = $"New Hire - {request.PreferredFirstName ?? request.FirstName} {request.LastName} - {request.FirstDayOfEmployment:MM/dd/yyyy}",
                resolution = (string?)null,
                group = new { name = serviceCenter },
                category = new { name = "Account" },
                subcategory = new { name = "Employee" },
                item = new { name = "New Hire" },
                priority = new { name = "3 - Medium" },
                urgency = new { name = "3 - Medium" },
                requester = new { name = request.RequestorName },
                udf_fields = udfFields,
                description = description
            }
        };

        return serviceDeskRequest;
    }

    /// <summary>
    /// Builds the description text for the ServiceDesk ticket
    /// </summary>
    private static string V(string? value) => string.IsNullOrEmpty(value) ? "N/A" : value;

    private string BuildDescription(CreateServiceDeskRecordDto request)
    {
        var sb = new StringBuilder();

        sb.Append($"New Hire - {request.PreferredFirstName ?? request.FirstName} {request.LastName} - {request.FirstDayOfEmployment:MM/dd/yyyy}<br/>");
        sb.Append($"First Name (Legal): {request.FirstName}<br/>");
        sb.Append($"Last Name: {request.LastName}<br/>");
        sb.Append($"Preferred Name: {request.PreferredFirstName ?? request.FirstName}<br/>");
        sb.Append($"Rehire: {(request.Rehire ? "Yes" : "No")}<br/>");
        sb.Append($"<br/>");

        // Network & Email
        sb.Append($"Does New Hire Require a Network User Name & Password? {(request.RequireNetworkUser == "True" ? "Yes" : "No")}<br/>");
        if (request.RequireNetworkUser == "True")
        {
            sb.Append($"User Name: {V(request.NetworkUserName)}<br/>");
            sb.Append($"Does New Hire Require an Email Address? {(request.RequireEmailAddress == "True" ? "Yes" : "No")}<br/>");
            if (request.RequireEmailAddress == "True")
            {
                sb.Append($"Email Address: {V(request.EmailAddress)}<br/>");
                sb.Append($"MS License: {V(request.MicrosoftLicenses)}<br/>");
            }
        }
        sb.Append($"<br/>");

        // Phone Requirements
        sb.Append("<b><u>Phone Requirements</u></b><br/>");
        sb.Append($"Desk Phone Req? {V(request.DeskPhoneRequired)}<br/>");
        sb.Append($"Reuse Existing? {V(request.ReuseExistingPhone)}<br/>");
        sb.Append($"Company Cell Phone Req? {V(request.CompanyCellPhoneRequired)}<br/>");
        sb.Append($"Cell Plan? {V(request.CompanyCellPlan)}<br/>");
        sb.Append($"BYOD Cell Phone: {V(request.BYODCellPhone)}<br/>");
        sb.Append($"<br/>");

        // Building Access
        if (request.Requirements.HasBuildingAccess && request.BuildingAccess?.Any() == true)
        {
            sb.Append("<b><u>Building Access</u></b><br/>");
            foreach (var access in request.BuildingAccess)
            {
                sb.Append($"{V(access.AccessDescription)}<br/>");
            }
            sb.Append($"<br/>");
        }

        // Tablet Profile
        if (request.Requirements.HasTabletProfiles && request.TabletProfiles?.Any() == true)
        {
            sb.Append("<b><u>Tablet Profile</u></b><br/>");
            foreach (var tablet in request.TabletProfiles)
            {
                sb.Append($"{V(tablet.TabletProfileName)}<br/>");
            }
            sb.Append($"<br/>");
        }

        // Computer Requirements
        if (request.Requirements.HasComputerRequirements && request.ComputerRequirements?.Any() == true)
        {
            sb.Append("<b><u>Computer Requirements</u></b><br/>");
            foreach (var computer in request.ComputerRequirements)
            {
                sb.Append($"{V(computer.ComputerRequirementsDescription)}<br/>");
            }
            sb.Append($"<br/>");
        }

        // Applications
        if (request.Requirements.HasITApplications && request.Applications?.Any() == true)
        {
            sb.Append("<b><u>Applications</u></b><br/>");
            foreach (var app in request.Applications)
            {
                var applicationDisplay = !string.IsNullOrEmpty(app.ApplicationName) ? app.ApplicationName : $"Application ID: {app.ApplicationId}";
                sb.Append($"{applicationDisplay} - Notes: {V(app.AccessNotes)}<br/>");
            }
            sb.Append($"<br/>");
        }

        // Sharepoint/Folder Access
        if (request.Requirements.HasSoftwareAccessReq && request.SharepointAndFolderAccess?.Any() == true)
        {
            sb.Append("<b><u>Access Info</u></b><br/>");
            foreach (var access in request.SharepointAndFolderAccess)
            {
                var accessType = access.FolderType switch
                {
                    "1" => "Outlook Public Folder",
                    "2" => "Shared Mailbox",
                    "3" => "Sharepoint Site",
                    "4" => "Windows Folder",
                    _ => V(access.FolderType)
                };
                sb.Append($"{accessType} - {V(access.FolderName)}<br/>");
            }
            sb.Append($"<br/>");
        }

        // Additional Notes
        if (!string.IsNullOrEmpty(request.AdditionalNotes))
        {
            sb.Append("<b><u>Additional Notes</u></b><br/>");
            sb.Append($"{request.AdditionalNotes}<br/>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the UDF (User Defined Fields) for ServiceDesk
    /// Maps to the specific ServiceDesk field IDs
    /// </summary>
    private dynamic BuildUDFFields(CreateServiceDeskRecordDto request)
    {
        return new
        {
            udf_char105 = request.FirstName, // First Name
            udf_char106 = request.LastName, // Last Name
            udf_char113 = $"{request.PreferredFirstName ?? request.FirstName} {request.LastName}", // Preferred Name
            udf_char126 = $"Desk Phone Req?: {request.DeskPhoneRequired}\r\nReuse Existing?: {request.ReuseExistingPhone}", // Desk Phone + Reuse
            udf_char127 = request.CompanyCellPhoneRequired, // Company Cell Phone
            udf_date2 = new { value = new DateTimeOffset(request.FirstDayOfEmployment.Date).ToUnixTimeMilliseconds() }, // Start Date
            // udf_char114 = request.NetworkUserName, // Network Username -- Workstation Information � Label and fields can be removed. It�s on the Request and the Applications team doesn�t fulfill it. --from Morray
            udf_char115 = request.MicrosoftLicenses, // MS License
            udf_char117 = request.EmailAddress, // Email Address
            udf_char118 = string.Join(", ", request.BuildingAccess?.Select(b => b.AccessDescription) ?? new List<string>()), // Building Access
            udf_char129 = string.Join(", ", request.TabletProfiles?.Select(t => t.TabletProfileName) ?? new List<string>()), // Tablet Profiles
            //udf_char128 = string.Join(", ", request.ComputerRequirements?.Select(c => !string.IsNullOrEmpty(c.ComputerRequirementsDescription) ? c.ComputerRequirementsDescription : c.ComputerRequirementsId.ToString()) ?? new List<string>()), // Computer Requirements -- Workstation Information � Label and fields can be removed. It�s on the Request and the Applications team doesn�t fulfill it. --from Morray
            udf_char132 = string.Join("\r\n", request.Applications?.Select(a => !string.IsNullOrEmpty(a.ApplicationName) ? $"{a.ApplicationName} - {a.AccessNotes}" : a.AccessNotes) ?? new List<string>()), // Applications
            udf_char133 = string.Join("\r\n", request.SharepointAndFolderAccess?.Select(f => $"{f.FolderType} - {f.FolderName}") ?? new List<string>()) // Access Info
        };
    }

    /// <summary>
    /// Extracts the ticket ID from the ServiceDesk API response
    /// Handles multiple response formats from ManageEngine ServiceDesk Plus
    /// </summary>
    private string? ExtractTicketId(string responseContent)
    {
        try
        {
            _logger.LogDebug($"[ServiceDesk] Extracting ticket ID from response: {responseContent}");

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogWarning("[ServiceDesk] Response content is empty");
                return null;
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            // Format 0: Root request object with id (new format from API v3)
            if (root.TryGetProperty("request", out var requestObj))
            {
                _logger.LogDebug("[ServiceDesk] Found 'request' property at root level");
                if (requestObj.TryGetProperty("id", out var requestId))
                {
                    var idStr = requestId.GetString();
                    if (!string.IsNullOrEmpty(idStr))
                    {
                        _logger.LogInformation($"[ServiceDesk] Extracted id from request object: {idStr}");
                        return idStr;
                    }
                }
            }

            // Format 1: response.response.ticketid
            if (root.TryGetProperty("response", out var response))
            {
                _logger.LogDebug("[ServiceDesk] Found 'response' property in root");

                // Check if status is success
                if (response.TryGetProperty("status", out var status))
                {
                    var statusStr = status.GetString();
                    _logger.LogDebug($"[ServiceDesk] Status: {statusStr}");

                    if (statusStr == "success")
                    {
                        // Try ticketid
                        if (response.TryGetProperty("ticketid", out var ticketId))
                        {
                            var idStr = ticketId.GetString();
                            _logger.LogInformation($"[ServiceDesk] Extracted ticketid: {idStr}");
                            return idStr;
                        }

                        // Try id
                        if (response.TryGetProperty("id", out var id))
                        {
                            var idStr = id.GetString();
                            _logger.LogInformation($"[ServiceDesk] Extracted id from response: {idStr}");
                            return idStr;
                        }

                        // Try request_id
                        if (response.TryGetProperty("request_id", out var requestId))
                        {
                            var idStr = requestId.GetString();
                            _logger.LogInformation($"[ServiceDesk] Extracted request_id: {idStr}");
                            return idStr;
                        }

                        // Try udf_fields.some_id (some APIs return it differently)
                        if (response.TryGetProperty("udf_fields", out var udfFields))
                        {
                            if (udfFields.TryGetProperty("id", out var udfId))
                            {
                                var idStr = udfId.GetString();
                                _logger.LogInformation($"[ServiceDesk] Extracted id from udf_fields: {idStr}");
                                return idStr;
                            }
                        }
                    }
                }

                // Even if status not success, try to get any ID
                if (response.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in response.EnumerateObject())
                    {
                        if (property.Name.Contains("id", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Contains("ticket", StringComparison.OrdinalIgnoreCase))
                        {
                            var idStr = property.Value.GetString();
                            if (!string.IsNullOrEmpty(idStr))
                            {
                                _logger.LogInformation($"[ServiceDesk] Extracted {property.Name}: {idStr}");
                                return idStr;
                            }
                        }
                    }
                }
            }

            // Format 2: Root level ticketid
            if (root.TryGetProperty("ticketid", out var rootTicketId))
            {
                var idStr = rootTicketId.GetString();
                _logger.LogInformation($"[ServiceDesk] Extracted ticketid from root: {idStr}");
                return idStr;
            }

            // Format 3: Root level id
            if (root.TryGetProperty("id", out var rootId))
            {
                var idStr = rootId.GetString();
                _logger.LogInformation($"[ServiceDesk] Extracted id from root: {idStr}");
                return idStr;
            }

            // Format 4: Root level request_id
            if (root.TryGetProperty("request_id", out var rootRequestId))
            {
                var idStr = rootRequestId.GetString();
                _logger.LogInformation($"[ServiceDesk] Extracted request_id from root: {idStr}");
                return idStr;
            }

            // Last resort: search entire response for any ID-like field
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if ((property.Name.Contains("id", StringComparison.OrdinalIgnoreCase) ||
                         property.Name.Contains("ticket", StringComparison.OrdinalIgnoreCase) ||
                         property.Name.Contains("request", StringComparison.OrdinalIgnoreCase)) &&
                        property.Value.ValueKind == JsonValueKind.String)
                    {
                        var idStr = property.Value.GetString();
                        if (!string.IsNullOrEmpty(idStr))
                        {
                            _logger.LogInformation($"[ServiceDesk] Extracted {property.Name} from root: {idStr}");
                            return idStr;
                        }
                    }
                }
            }

            _logger.LogWarning("[ServiceDesk] Could not extract any ticket ID from response");
            return null;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "[ServiceDesk] JSON parsing failed while extracting ticket ID");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ServiceDesk] Failed to extract ticket ID from response");
            return null;
        }
    }

    /// <summary>
    /// Builds the ServiceDesk request JSON structure for Promotion/Transfer requests
    /// </summary>
    private dynamic BuildPromotionServiceDeskRequest(CreatePromotionServiceDeskRecordDto request)
    {
        var description = BuildPromotionDescription(request);
        var udfFields = BuildPromotionUDFFields(request);

        // Get service center from configuration
        var serviceDeskConfig = _configuration.GetSection("ServiceDeskPlus");
        var serviceCenter = serviceDeskConfig["ServiceCenter"] ?? "Service Center";

        var serviceDeskRequest = new
        {
            request = new
            {
                //template = new { name = "Promotion/Transfer" },
                //subject = $"Promotion/Transfer - {request.PreferredFirstName ?? request.FirstName} {request.LastName} - {request.EffectiveDate:MM/dd/yyyy}",
                //resolution = (string?)null,
                //group = new { name = serviceCenter },
                //category = new { name = "Account" },
                //subcategory = new { name = "Employee" },
                //item = new { name = "Promotion/Request" },
                //priority = new { name = "3 - Medium" },
                //urgency = new { name = "3 - Medium" },
                //requester = new { name = request.RequestorName },
                //udf_fields = udfFields,
                //description = description

                template = new { name = "Promotion/Transfer" },
                subject = $"Promotion/Transfer - {request.FirstName} {request.LastName} - {request.EffectiveDate:MM/dd/yyyy}",
                resolution = (string?)null,
                group = new { name = serviceCenter },
                category = new { name = "Account" },
                subcategory = new { name = "Employee" },
                item = new { name = "Promotion/Transfer" },
                priority = new { name = "3 - Medium" },
                urgency = new { name = "3 - Medium" },
                requester = new { name = request.RequestorName },
                udf_fields = udfFields,
                description = description
            }
        };

        return serviceDeskRequest;
    }

    /// <summary>
    /// Builds the description text for Promotion/Transfer ServiceDesk ticket
    /// </summary>
    private string BuildPromotionDescription(CreatePromotionServiceDeskRecordDto request)
    {
        var sb = new StringBuilder();

        sb.Append($"Promotion/Transfer - {request.PreferredFirstName ?? request.FirstName} {request.LastName} - {request.EffectiveDate:MM/dd/yyyy}<br/>");
        sb.Append($"<br/>");
        sb.Append($"<b>Employee Information</b><br/>");
        sb.Append($"First Name (Legal): {request.FirstName}<br/>");
        sb.Append($"Last Name: {request.LastName}<br/>");
        sb.Append($"Name: {request.PreferredFirstName ?? request.FirstName} {request.LastName}<br/>");
        sb.Append($"Effective Date: {request.EffectiveDate:MM/dd/yyyy}<br/>");
        sb.Append($"<br/>");

        // Current Position Information
        sb.Append($"<b>Current Position</b><br/>");
        sb.Append($"Company: {V(request.CurrentCompanyName)} ({request.CurrentCompanyCode})<br/>");
        sb.Append($"Department: {V(request.CurrentPayrollDeptName)} ({request.CurrentPayrollDeptCode})<br/>");
        sb.Append($"Payroll Group: {V(request.CurrentPayrollGroupName)} ({request.CurrentPayrollGroupCode})<br/>");
        sb.Append($"Position: {V(request.CurrentPositionName)} ({request.CurrentPositionCode})<br/>");
        sb.Append($"Location: {V(request.CurrentLocationName)} ({request.CurrentLocationCode})<br/>");
        sb.Append($"Email: {V(request.CurrentEmailAddress)}<br/>");
        sb.Append($"<br/>");

        // New Position Information
        sb.Append($"<b>New Position</b><br/>");
        sb.Append($"Company: {V(request.NewCompanyName)} ({request.NewCompanyCode})<br/>");
        sb.Append($"Department: {V(request.NewPayrollDeptName)} ({request.NewPayrollDeptCode})<br/>");
        sb.Append($"Payroll Group: {V(request.NewPayrollGroupName)} ({request.NewPayrollGroupCode})<br/>");
        sb.Append($"Position: {V(request.NewPositionName)} ({request.NewPositionCode})<br/>");
        sb.Append($"Location: {V(request.NewLocationName)} ({request.NewLocationCode})<br/>");
        sb.Append($"Supervisor: {V(request.NewSupervisorName)} ({request.NewSupervisorId})<br/>");
        if (!string.IsNullOrEmpty(request.NewEmailAddress))
        {
            sb.Append($"New Email: {request.NewEmailAddress}<br/>");
        }
        sb.Append($"<br/>");

        // IT Requirements
        if (request.RequiresITSupport)
        {
            sb.Append($"<b>IT Support Required</b><br/>");
            if (!string.IsNullOrEmpty(request.ITSupportNotes))
            {
                sb.Append($"Notes: {request.ITSupportNotes}<br/>");
            }
            sb.Append($"<br/>");
        }

        // Phone Requirements
        if (!string.IsNullOrEmpty(request.DeskPhoneRequired) || !string.IsNullOrEmpty(request.CompanyCellPhoneRequired))
        {
            sb.Append("<b><u>Phone Requirements</u></b><br/>");
            sb.Append($"Desk Phone Req? {V(request.DeskPhoneRequired)}<br/>");
            sb.Append($"Reuse Existing? {V(request.ReuseExistingPhone)}<br/>");
            sb.Append($"Company Cell Phone Req? {V(request.CompanyCellPhoneRequired)}<br/>");
            sb.Append($"Cell Plan? {V(request.CompanyCellPlan)}<br/>");
            sb.Append($"BYOD Cell Phone: {V(request.BYODCellPhone)}<br/>");
            sb.Append($"<br/>");
        }

        // Building Access
        if (request.Requirements.HasBuildingAccess && request.BuildingAccess?.Any() == true)
        {
            sb.Append("<b><u>Building Access</u></b><br/>");
            foreach (var access in request.BuildingAccess)
            {
                sb.Append($"{V(access.AccessDescription)}<br/>");
            }
            if (request.UseExistingKeyFob == true)
            {
                sb.Append($"Use Existing Key Fob: Yes - Needs to be reprogrammed<br/>");
            }
            sb.Append($"<br/>");
        }

        // Tablet Profile
        if (request.Requirements.HasTabletProfiles && request.TabletProfiles?.Any() == true)
        {
            sb.Append("<b><u>Tablet Profile</u></b><br/>");
            foreach (var tablet in request.TabletProfiles)
            {
                sb.Append($"{V(tablet.TabletProfileName)}<br/>");
            }
            sb.Append($"<br/>");
        }

        // Computer Requirements
        if (request.Requirements.HasComputerRequirements && request.ComputerRequirements?.Any() == true)
        {
            sb.Append("<b><u>Computer Requirements</u></b><br/>");
            foreach (var computer in request.ComputerRequirements)
            {
                sb.Append($"{V(computer.ComputerRequirementsDescription)}<br/>");
            }
            sb.Append($"<br/>");
        }

        // Applications
        if (request.Requirements.HasITApplications && request.Applications?.Any() == true)
        {
            sb.Append("<b><u>Applications</u></b><br/>");
            foreach (var app in request.Applications)
            {
                var applicationDisplay = !string.IsNullOrEmpty(app.ApplicationName) ? app.ApplicationName : $"Application ID: {app.ApplicationId}";
                sb.Append($"{applicationDisplay} - Notes: {V(app.AccessNotes)}<br/>");
            }
            sb.Append($"<br/>");
        }

        // Sharepoint/Folder Access
        if (request.Requirements.HasSoftwareAccessReq && request.SharepointAndFolderAccess?.Any() == true)
        {
            sb.Append("<b><u>Access Info</u></b><br/>");
            foreach (var access in request.SharepointAndFolderAccess)
            {
                var accessType = access.FolderType switch
                {
                    "1" => "Outlook Public Folder",
                    "2" => "Shared Mailbox",
                    "3" => "Sharepoint Site",
                    "4" => "Windows Folder",
                    _ => V(access.FolderType)
                };
                sb.Append($"{accessType} - {V(access.FolderName)}<br/>");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the UDF (User Defined Fields) for Promotion/Transfer ServiceDesk
    /// Maps to the specific ServiceDesk field IDs (using On-Boarding template fields)
    /// </summary>
    private dynamic BuildPromotionUDFFields(CreatePromotionServiceDeskRecordDto request)
    {
        return new
        {
            udf_char105 = request.FirstName, // First Name
            udf_char106 = request.LastName, // Last Name
            udf_char113 = $"{request.PreferredFirstName ?? request.FirstName} {request.LastName}", // Preferred Name
            udf_char126 = $"{request.DeskPhoneRequired}, {request.ReuseExistingPhone}", // Desk Phone + Reuse
            udf_char127 = request.CompanyCellPhoneRequired, // Company Cell Phone
            udf_date2 = new { value = request.EffectiveDateMilliseconds }, // Effective Date (using same field as Start Date in New Hire)
            udf_char114 = request.CurrentNetworkUserName, // Network Username (using current network username)
            udf_char115 = request.ITDetails?.MSOfficeLicenseE5 == true ? "E5" : request.ITDetails?.MSOfficeLicenseF3 == true ? "F3" : "", // MS License
            udf_char117 = request.NewEmailAddress ?? request.CurrentEmailAddress, // Email Address (prefer new email if available)
            udf_char118 = string.Join(", ", request.BuildingAccess?.Select(b => b.AccessDescription) ?? new List<string>()), // Building Access
            udf_char129 = string.Join(", ", request.TabletProfiles?.Select(t => t.TabletProfileName) ?? new List<string>()), // Tablet Profiles
            udf_char128 = string.Join(", ", request.ComputerRequirements?.Select(c => !string.IsNullOrEmpty(c.ComputerRequirementsDescription) ? c.ComputerRequirementsDescription : c.ComputerRequirementsId.ToString()) ?? new List<string>()), // Computer Requirements
            udf_char132 = string.Join(", ", request.Applications?.Select(a => !string.IsNullOrEmpty(a.ApplicationName) ? $"{a.ApplicationName} - {a.AccessNotes}" : a.AccessNotes) ?? new List<string>()), // Applications
            udf_char133 = string.Join(", ", request.SharepointAndFolderAccess?.Select(f => $"{f.FolderType} - {f.FolderName}") ?? new List<string>()) // Access Info
        };
    }

    /// <summary>
    /// Creates a ServiceDesk ticket for a termination request (Off-Boarding template)
    /// </summary>
    public async Task<ServiceDeskRecordResponseDto> CreateTerminationServiceDeskRecord(CreateTerminationServiceDeskRecordDto request)
    {
        try
        {
            _logger.LogInformation($"[ServiceDesk] Creating Termination ticket for {request.FirstName} {request.LastName}");

            if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName))
            {
                _ecmLogger.LogServiceTicket(false, "CREATE", null, "Termination", "Required fields (FirstName, LastName) are missing", employeeName: $"{request.FirstName} {request.LastName}".Trim());
                return new ServiceDeskRecordResponseDto
                {
                    Success = false,
                    Message = "Required fields (FirstName, LastName) are missing"
                };
            }

            var serviceDeskRequest = BuildTerminationServiceDeskRequest(request);

            var result = await SendServiceDeskRequest(serviceDeskRequest, $"{request.FirstName} {request.LastName}", "Termination");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ServiceDesk] Exception occurred while creating Termination ServiceDesk record");
            _ecmLogger.LogServiceTicket(false, "CREATE", null, "Termination", ex.Message, employeeName: $"{request.FirstName} {request.LastName}".Trim());
            return new ServiceDeskRecordResponseDto
            {
                Success = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Builds the ServiceDesk request JSON structure for Termination requests (Off-Boarding template)
    /// </summary>
    private dynamic BuildTerminationServiceDeskRequest(CreateTerminationServiceDeskRecordDto request)
    {
        var description = BuildTerminationDescription(request);
        var udfFields = BuildTerminationUDFFields(request);

        var serviceDeskConfig = _configuration.GetSection("ServiceDeskPlus");
        var serviceCenter = serviceDeskConfig["ServiceCenter"] ?? "Service Center";

        var serviceDeskRequest = new
        {
            request = new
            {
                template = new { name = "Off-Boarding" },
                subject = $"Off-Boarding - {request.FirstName} {request.LastName} - {request.OffBoardDate:MM/dd/yyyy}",
                resolution = (string?)null,
                group = new { name = serviceCenter },
                category = new { name = "Account" },
                subcategory = new { name = "Employee" },
                priority = new { name = "3 - Medium" },
                urgency = new { name = "3 - Medium" },
                requester = new { name = request.RequestorName },
                udf_fields = udfFields,
                description = description
            }
        };

        return serviceDeskRequest;
    }

    /// <summary>
    /// Builds the description text for Termination ServiceDesk ticket.
    /// Mirrors legacy ServiceDeskNotifications_Terminations/Program.cs layout.
    /// </summary>
    private string BuildTerminationDescription(CreateTerminationServiceDeskRecordDto request)
    {
        var equipment = NormalizeReclaimEquipment(request.ReclaimEquipment);

        var sb = new StringBuilder();
        sb.Append($"Users Name - {request.FirstName} {request.LastName}</p>");
        sb.Append($"Last Day of Employment: {request.OffBoardDate:MM/dd/yyyy}</p>");
        sb.Append($"Forward Email to: {request.ForwardEmail}</p>");
        sb.Append($"Email Auto Reply: {request.EmailAutoReply}</p>");
        sb.Append($"Forward Desk Phone To: {request.ForwardDeskPhone}</p>");
        sb.Append($"Forward Cell Phone To: {request.ForwardCellPhone}</p>");
        sb.Append($"OneDrive Access To: {request.OneDriveAccessTo}</p>");
        sb.Append("Reclaim Equipment: ");

        foreach (var item in equipment)
        {
            switch (item.Trim())
            {
                case "no":
                    sb.Append(" No IT Equipment to reclaim.</p>");
                    break;
                case "computer":
                    sb.Append("Computer, Monitor, or Accessories.</p>");
                    break;
                case "deskPhone":
                    sb.Append("Desk Phone.</p>");
                    break;
                case "cellPhone":
                    sb.Append("Cell phone</p>");
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the UDF (User Defined Fields) for Termination ServiceDesk.
    /// Field IDs match legacy production: udf_char119 (Name), udf_date1 (Off-Board Date ms),
    /// udf_char120-124 (forwarding fields), udf_char125 (Reclaim Equipment array).
    /// </summary>
    private dynamic BuildTerminationUDFFields(CreateTerminationServiceDeskRecordDto request)
    {
        var equipmentDisplay = NormalizeReclaimEquipment(request.ReclaimEquipment)
            .Select(code => code.Trim() switch
            {
                "no" => "No IT Equipment to reclaim",
                "computer" => "Computer, Monitor, or Accessories",
                "deskPhone" => "Desk Phone",
                "cellPhone" => "Cell Phone",
                _ => "No IT Equipment to reclaim"
            })
            .ToList();

        return new
        {
            udf_char119 = $"{request.FirstName} {request.LastName}",
            udf_date1 = new { value = (request.OffBoardDateMilliseconds ?? new DateTimeOffset(request.OffBoardDate.Date).ToUnixTimeMilliseconds()).ToString() },
            udf_char120 = request.ForwardEmail,
            udf_char121 = request.EmailAutoReply,
            udf_char122 = request.ForwardDeskPhone,
            udf_char123 = request.ForwardCellPhone,
            udf_char124 = request.OneDriveAccessTo,
            udf_char125 = equipmentDisplay
        };
    }

    /// <summary>
    /// Applies the legacy null-default: when no reclaim equipment is supplied, treat it as "no".
    /// </summary>
    private static IEnumerable<string> NormalizeReclaimEquipment(List<string>? equipment)
    {
        if (equipment == null || equipment.Count == 0)
        {
            return new[] { "no" };
        }

        var cleaned = equipment.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
        return cleaned.Count == 0 ? new[] { "no" } : cleaned;
    }
}

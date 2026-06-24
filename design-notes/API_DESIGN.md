# HR Employee Change Management - API Design

## Overview

This document defines the .NET Core 9 Web API structure for the HR Employee Change Management system. The API follows RESTful principles with clear separation of concerns and consistent patterns across all endpoints.

## API Architecture

### Base URL Structure
```
https://{server}/api/v1/{resource}
```

### Authentication
- **Entra ID (Azure AD)** JWT bearer tokens
- All endpoints require authentication
- Authorization based on user company access and role-based permissions

### Response Format
```json
{
  "success": true,
  "data": {...},
  "message": "Operation completed successfully",
  "errors": [],
  "timestamp": "2025-06-18T10:30:00Z"
}
```

### Error Response Format
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    {
      "field": "EmployeeId",
      "message": "Employee ID is required"
    }
  ],
  "timestamp": "2025-06-18T10:30:00Z"
}
```

## Controllers and Endpoints

### 1. HRRequestsController

#### Get User Requests
```http
GET /api/v1/hr-requests
```
**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 10, max: 100)
- `requestType` (string, optional): Filter by request type
- `status` (string, optional): Filter by status
- `sortBy` (string, default: "SubmittedDate"): Sort field
- `sortDirection` (string, default: "desc"): "asc" or "desc"

**Response:**
```json
{
  "success": true,
  "data": {
    "requests": [
      {
        "requestId": 123,
        "requestType": "Promotion",
        "employeeCount": 1,
        "employeeNames": "John Smith",
        "submittedBy": "manager@company.com",
        "submittedByName": "Jane Manager",
        "submittedDate": "2025-06-18T09:00:00Z",
        "status": "Submitted",
        "notes": "Annual promotion review"
      }
    ],
    "totalCount": 25,
    "page": 1,
    "pageSize": 10,
    "totalPages": 3
  }
}
```

#### Get Request Details
```http
GET /api/v1/hr-requests/{requestId}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "requestId": 123,
    "submittedBy": "manager@company.com",
    "submittedByName": "Jane Manager",
    "submittedDate": "2025-06-18T09:00:00Z",
    "notes": "Annual promotion review",
    "employees": [
      {
        "requestDetailId": 456,
        "requestType": "Promotion",
        "requestStatus": "Pending",
        "employeeId": "EMP001",
        "employeeNetworkId": "jsmith",
        "employeeName": "John Smith",
        "employeeTitle": "Site Supervisor",
        "employeeDivision": "Field Operations",
        "employeeDepartment": "Commercial Construction",
        "effectiveDate": "2025-07-01",
        "promotionDetails": {
          "currentPosition": {...},
          "newPosition": {...},
          "payrollType": "Salary",
          "requiresAccess": true
        },
        "creditCardDetails": {...},
        "vehicleDetails": {...},
        "itDetails": {...}
      }
    ]
  }
}
```

#### Create Request
```http
POST /api/v1/hr-requests
```

**Request Body:**
```json
{
  "notes": "Batch promotion request",
  "employees": [
    {
      "requestType": "Promotion",
      "employeeId": "EMP001",
      "employeeNetworkId": "jsmith",
      "employeeName": "John Smith",
      "employeeTitle": "Site Supervisor",
      "employeeDivision": "Field Operations",
      "employeeDepartment": "Commercial Construction",
      "effectiveDate": "2025-07-01",
      "promotionDetails": {
        "currentPayrollCompany": "64",
        "currentPayrollGroup": "2",
        "newPayrollCompany": "64",
        "newPayrollGroup": "1",
        "payrollType": "Salary",
        "requiresAccess": true
      },
      "creditCardDetails": {
        "kwikTripCard": true,
        "expenseCard": "ee-expense",
        "weeklyLimit": 1000.00
      },
      "itDetails": {
        "emailRequired": true,
        "applications": [
          {
            "applicationId": 1,
            "accessNotes": "Full access required"
          }
        ],
        "folders": [
          {
            "folderType": "SharePoint Site",
            "folderName": "Project Management"
          }
        ]
      }
    }
  ]
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "requestId": 123,
    "employeeRequestIds": [456, 457, 458]
  },
  "message": "Request submitted successfully"
}
```

#### Update Request Status
```http
PUT /api/v1/hr-requests/{requestId}/status
```

**Request Body:**
```json
{
  "status": "Processing",
  "notes": "Request being processed by HR"
}
```

#### Delete Request
```http
DELETE /api/v1/hr-requests/{requestId}
```

### 2. EmployeesController

#### Search Employees
```http
GET /api/v1/employees/search
```

**Query Parameters:**
- `query` (string, required): Search term (min 2 chars)
- `page` (int, default: 1)
- `pageSize` (int, default: 25, max: 100)
- `includeInactive` (bool, default: false)
- `companyFilter` (string, optional): Filter by company code

**Response:**
```json
{
  "success": true,
  "data": {
    "employees": [
      {
        "employeeId": "EMP001",
        "employeeNetworkId": "jsmith",
        "name": "John Smith",
        "title": "Site Supervisor",
        "division": "Field Operations",
        "department": "Commercial Construction",
        "company": "MTS",
        "status": "Active",
        "currentPosition": {
          "payrollCompany": "64",
          "payrollGroup": "2",
          "payrollDept": "6401",
          "position": "SITESUPR",
          "physicalLocation": "100",
          "status": "FULL TIME"
        }
      }
    ],
    "totalCount": 15,
    "page": 1,
    "pageSize": 25
  }
}
```

#### Get Employee Details
```http
GET /api/v1/employees/{employeeId}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "employeeId": "EMP001",
    "employeeNetworkId": "jsmith",
    "name": "John Smith",
    "title": "Site Supervisor",
    "division": "Field Operations",
    "department": "Commercial Construction",
    "company": "MTS",
    "status": "Active",
    "hireDate": "2020-03-15",
    "currentPosition": {
      "payrollCompany": "64",
      "payrollGroup": "2",
      "payrollDept": "6401",
      "position": "SITESUPR",
      "timeCardSupervisor": "Mike Johnson",
      "vacationSupervisor": "Sarah Davis",
      "functionalDept": "None",
      "physicalLocation": "100",
      "status": "FULL TIME"
    },
    "contactInfo": {
      "email": "jsmith@company.com",
      "phone": "(555) 123-4567",
      "address": "123 Main St, City, State 12345"
    }
  }
}
```

#### Get Laid Off Employees
```http
GET /api/v1/employees/laid-off
```

**Query Parameters:**
- `query` (string, optional): Search term
- `division` (string, optional): Filter by division
- `page` (int, default: 1)
- `pageSize` (int, default: 25)

### 3. ReferenceDataController

#### Get All Reference Data
```http
GET /api/v1/reference-data
```

**Response:**
```json
{
  "success": true,
  "data": {
    "companies": [
      {
        "companyId": 1,
        "companyCode": "64",
        "companyName": "MTS",
        "isActive": true
      }
    ],
    "payrollGroups": [
      {
        "payrollGroupId": 1,
        "payrollGroupCode": "1",
        "payrollGroupName": "Management & Tech Svc.-Exempt",
        "isActive": true
      }
    ],
    "payrollDepartments": [...],
    "positions": [...],
    "physicalLocations": [...],
    "applications": [
      {
        "applicationId": 1,
        "applicationName": "AutoCAD",
        "applicationDescription": "Computer-aided design software",
        "isActive": true
      }
    ]
  }
}
```

#### Get Specific Reference Data
```http
GET /api/v1/reference-data/companies
GET /api/v1/reference-data/payroll-groups
GET /api/v1/reference-data/payroll-departments
GET /api/v1/reference-data/positions
GET /api/v1/reference-data/physical-locations
GET /api/v1/reference-data/applications
```

#### Sync Reference Data (Admin)
```http
POST /api/v1/reference-data/sync
```

### 4. AuthorizationController

#### Get User Permissions
```http
GET /api/v1/authorization/permissions
```

**Response:**
```json
{
  "success": true,
  "data": {
    "userId": "user@company.com",
    "userName": "John Manager",
    "isHR": false,
    "isIT": false,
    "companies": [
      {
        "companyId": 1,
        "companyCode": "64",
        "companyName": "MTS",
        "canSubmitRequests": true
      }
    ]
  }
}
```

#### Get User Companies
```http
GET /api/v1/authorization/companies
```

### 5. EmailTemplatesController (Admin)

#### Get Email Templates
```http
GET /api/v1/email-templates
```

**Query Parameters:**
- `requestType` (string, optional)
- `emailType` (string, optional)

#### Update Email Template
```http
PUT /api/v1/email-templates/{templateId}
```

**Request Body:**
```json
{
  "subject": "Updated subject with {{EmployeeName}}",
  "body": "Updated email body content..."
}
```

## DTOs and Models

### Request DTOs

#### CreateHRRequestDto
```csharp
public class CreateHRRequestDto
{
    public string Notes { get; set; }
    public List<CreateEmployeeRequestDto> Employees { get; set; }
}

public class CreateEmployeeRequestDto
{
    [Required]
    public string RequestType { get; set; }
    
    [Required]
    public string EmployeeId { get; set; }
    
    public string EmployeeNetworkId { get; set; }
    
    [Required]
    public string EmployeeName { get; set; }
    
    public string EmployeeTitle { get; set; }
    public string EmployeeDivision { get; set; }
    public string EmployeeDepartment { get; set; }
    
    public DateTime? EffectiveDate { get; set; }
    
    // Type-specific details
    public PromotionDetailsDto PromotionDetails { get; set; }
    public LayoffDetailsDto LayoffDetails { get; set; }
    public TerminationDetailsDto TerminationDetails { get; set; }
    
    // Shared details (when applicable)
    public CreditCardDetailsDto CreditCardDetails { get; set; }
    public VehicleDetailsDto VehicleDetails { get; set; }
    public ITDetailsDto ITDetails { get; set; }
}
```

#### PromotionDetailsDto
```csharp
public class PromotionDetailsDto
{
    // Current Position
    public string CurrentPayrollCompany { get; set; }
    public string CurrentPayrollGroup { get; set; }
    public string CurrentPayrollDept { get; set; }
    public string CurrentPosition { get; set; }
    public string CurrentTimeCardSupervisor { get; set; }
    public string CurrentVacationSupervisor { get; set; }
    public string CurrentFunctionalDept { get; set; }
    public string CurrentPhysicalLocation { get; set; }
    public string CurrentStatus { get; set; }
    
    // New Position
    [Required]
    public string NewPayrollCompany { get; set; }
    
    [Required]
    public string NewPayrollGroup { get; set; }
    
    [Required]
    public string NewPayrollDept { get; set; }
    
    [Required]
    public string NewPosition { get; set; }
    
    [Required]
    public string NewTimeCardSupervisor { get; set; }
    
    public string NewVacationSupervisor { get; set; }
    public string NewFunctionalDept { get; set; }
    
    [Required]
    public string NewPhysicalLocation { get; set; }
    
    [Required]
    public string NewStatus { get; set; }
    
    [Required]
    public string PayrollType { get; set; }
    
    public bool RequiresAccess { get; set; }
}
```

#### ITDetailsDto
```csharp
public class ITDetailsDto
{
    public bool EmailRequired { get; set; }
    public string AlternateDeliveryLocation { get; set; }
    public List<ApplicationRequestDto> Applications { get; set; }
    public List<FolderRequestDto> Folders { get; set; }
}

public class ApplicationRequestDto
{
    [Required]
    public int ApplicationId { get; set; }
    public string AccessNotes { get; set; }
}

public class FolderRequestDto
{
    [Required]
    public string FolderType { get; set; }
    
    [Required]
    public string FolderName { get; set; }
}
```

### Response DTOs

#### HRRequestSummaryDto
```csharp
public class HRRequestSummaryDto
{
    public int RequestId { get; set; }
    public string RequestType { get; set; }
    public int EmployeeCount { get; set; }
    public string EmployeeNames { get; set; }
    public string SubmittedBy { get; set; }
    public string SubmittedByName { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public string Status { get; set; }
    public string Notes { get; set; }
}
```

#### PagedResultDto<T>
```csharp
public class PagedResultDto<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

## Services Architecture

### Core Services

#### IHRRequestService
```csharp
public interface IHRRequestService
{
    Task<PagedResultDto<HRRequestSummaryDto>> GetUserRequestsAsync(
        string userId, bool isHROrIT, int page, int pageSize, 
        string requestType = null, string status = null, 
        string sortBy = "SubmittedDate", string sortDirection = "desc");
    
    Task<HRRequestDetailsDto> GetRequestDetailsAsync(int requestId, string userId, bool isHROrIT);
    
    Task<CreateRequestResultDto> CreateRequestAsync(CreateHRRequestDto request, string userId, string userName);
    
    Task<bool> UpdateRequestStatusAsync(int requestId, string status, string notes, string userId);
    
    Task<bool> DeleteRequestAsync(int requestId, string userId, bool isHROrIT);
}
```

#### IEmployeeService
```csharp
public interface IEmployeeService
{
    Task<PagedResultDto<EmployeeSummaryDto>> SearchEmployeesAsync(
        string query, int page, int pageSize, bool includeInactive = false, 
        string companyFilter = null, List<string> userCompanies = null);
    
    Task<EmployeeDetailsDto> GetEmployeeDetailsAsync(string employeeId);
    
    Task<PagedResultDto<EmployeeSummaryDto>> GetLaidOffEmployeesAsync(
        string query = null, string division = null, int page = 1, int pageSize = 25);
}
```

#### IReferenceDataService
```csharp
public interface IReferenceDataService
{
    Task<ReferenceDataDto> GetAllReferenceDataAsync();
    Task<List<CompanyDto>> GetCompaniesAsync();
    Task<List<PayrollGroupDto>> GetPayrollGroupsAsync();
    Task<List<PositionDto>> GetPositionsAsync();
    Task<List<ApplicationDto>> GetApplicationsAsync();
    Task SyncReferenceDataAsync();
}
```

#### IAuthorizationService
```csharp
public interface IAuthorizationService
{
    Task<UserPermissionsDto> GetUserPermissionsAsync(string userId);
    Task<List<CompanyDto>> GetUserCompaniesAsync(string userId);
    Task<bool> CanUserAccessCompanyAsync(string userId, string companyCode);
    Task<bool> IsUserInRoleAsync(string userId, string role); // "HR" or "IT"
}
```

## Integration Services

#### IViewpointIntegrationService
```csharp
public interface IViewpointIntegrationService
{
    Task<PagedResultDto<EmployeeSummaryDto>> SearchEmployeesAsync(
        string query, int page, int pageSize, bool includeInactive = false);
    
    Task<EmployeeDetailsDto> GetEmployeeDetailsAsync(string employeeId);
    
    Task<List<CompanyDto>> GetCompaniesAsync();
    Task<List<PayrollGroupDto>> GetPayrollGroupsAsync();
    Task<List<PositionDto>> GetPositionsAsync();
    
    Task<bool> ProcessPromotionRequestAsync(int requestDetailId);
    Task<bool> ProcessLayoffRequestAsync(int requestDetailId);
    Task<bool> ProcessTerminationRequestAsync(int requestDetailId);
    Task<bool> ProcessReturnToWorkRequestAsync(int requestDetailId);
}
```

#### IEmailNotificationService
```csharp
public interface IEmailNotificationService
{
    Task QueueConfirmationEmailAsync(int requestId, string requestType, string toEmail);
    Task QueueHRNotificationEmailAsync(int requestId, string requestType);
    Task QueueErrorNotificationEmailAsync(int requestId, string errorMessage);
    Task ProcessEmailQueueAsync();
}
```

## Middleware and Filters

### Authentication Middleware
- JWT token validation
- User context extraction
- Entra ID integration

### Authorization Filters
```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireHROrITAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check if user is in HR or IT role
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireCompanyAccessAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Validate user has access to companies referenced in request
    }
}
```

### Exception Handling Middleware
```csharp
public class ApiExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = GetUserFriendlyMessage(exception),
            Errors = GetErrorDetails(exception),
            Timestamp = DateTime.UtcNow
        };
        
        context.Response.StatusCode = GetStatusCode(exception);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

## Background Services

### ReferenceDataSyncService
```csharp
public class ReferenceDataSyncService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _referenceDataService.SyncReferenceDataAsync();
                await Task.Delay(TimeSpan.FromHours(4), stoppingToken); // Sync every 4 hours
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during reference data sync");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // Retry in 30 minutes
            }
        }
    }
}
```

### EmailProcessingService
```csharp
public class EmailProcessingService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _emailNotificationService.ProcessEmailQueueAsync();
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Process every 5 minutes
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during email processing");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
```

## Configuration

### appsettings.json Structure
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HRChangeManagement;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Authentication": {
    "EntraId": {
      "Instance": "https://login.microsoftonline.com/",
      "TenantId": "{tenant-id}",
      "ClientId": "{client-id}",
      "Audience": "{api-audience}"
    }
  },
  "ViewpointIntegration": {
    "BaseUrl": "https://viewpoint-api.company.com/api/",
    "ApiKey": "{viewpoint-api-key}",
    "Timeout": "00:00:30"
  },
  "Email": {
    "SmtpServer": "smtp.company.com",
    "SmtpPort": 587,
    "Username": "{smtp-username}",
    "Password": "{smtp-password}",
    "FromAddress": "hr-system@company.com",
    "HRNotificationEmail": "hr@company.com",
    "ITNotificationEmail": "it@company.com"
  },
  "ReferenceDataSync": {
    "SyncIntervalHours": 4,
    "RetryIntervalMinutes": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Error Handling

### Common HTTP Status Codes
- **200 OK**: Successful operation
- **201 Created**: Resource created successfully
- **400 Bad Request**: Validation errors, malformed request
- **401 Unauthorized**: Authentication required
- **403 Forbidden**: User doesn't have permission
- **404 Not Found**: Resource not found
- **409 Conflict**: Business rule violation
- **500 Internal Server Error**: Unexpected server error

### Validation Error Response
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    {
      "field": "Employees[0].EmployeeId",
      "message": "Employee ID is required"
    },
    {
      "field": "Employees[0].NewPayrollCompany", 
      "message": "New payroll company is required for promotion requests"
    }
  ],
  "timestamp": "2025-06-18T10:30:00Z"
}
```

## Security Considerations

1. **Authentication**: All endpoints require valid JWT tokens
2. **Authorization**: Company-based access control for all employee data
3. **Input Validation**: Comprehensive validation on all DTOs
4. **SQL Injection Prevention**: Entity Framework parameterized queries
5. **CORS**: Configured for Angular frontend domain only
6. **Rate Limiting**: Applied to prevent abuse
7. **Audit Logging**: All operations logged for compliance

## API Versioning

- URL-based versioning: `/api/v1/`
- Version header support: `X-API-Version: 1.0`
- Backward compatibility maintained for at least 2 versions

## Testing Strategy

1. **Unit Tests**: Service layer and business logic
2. **Integration Tests**: API endpoints with test database
3. **Contract Tests**: API response schema validation
4. **Performance Tests**: Load testing for search endpoints
5. **Security Tests**: Authentication and authorization scenarios
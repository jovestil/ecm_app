# HR Employee Change Management - Integration Interfaces & Open Questions

## Overview

This document defines the integration interfaces between the HR system and external systems (Viewpoint/Vista, Entra ID, Email), along with open questions that need to be resolved during implementation.

## Integration Architecture

```
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│    HR System       │    │   Viewpoint/Vista   │    │     Entra ID        │
│   (.NET Core)      │◄──►│      APIs           │    │   (Azure AD)        │
│                     │    │                     │    │                     │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
           │
           ▼
┌─────────────────────┐    ┌─────────────────────┐
│   Email System     │    │   Background Jobs   │
│    (SMTP)          │    │   (Hangfire/etc.)   │
└─────────────────────┘    └─────────────────────┘
```

## 1. Viewpoint/Vista Integration Interface

### IViewpointIntegrationService

```csharp
public interface IViewpointIntegrationService
{
    // Employee Data Access
    Task<PagedResultDto<ViewpointEmployeeDto>> SearchEmployeesAsync(
        string searchQuery, 
        int page = 1, 
        int pageSize = 25, 
        bool includeInactive = false,
        List<string> companyFilter = null);
    
    Task<ViewpointEmployeeDto> GetEmployeeDetailsAsync(string employeeId);
    
    Task<List<ViewpointEmployeeDto>> GetLaidOffEmployeesAsync(
        string searchQuery = null, 
        string division = null);
    
    // Reference Data Sync
    Task<List<ViewpointCompanyDto>> GetCompaniesAsync();
    Task<List<ViewpointPayrollGroupDto>> GetPayrollGroupsAsync();
    Task<List<ViewpointPayrollDeptDto>> GetPayrollDepartmentsAsync();
    Task<List<ViewpointPositionDto>> GetPositionsAsync();
    Task<List<ViewpointLocationDto>> GetPhysicalLocationsAsync();
    
    // HR Request Processing (Future)
    Task<ViewpointUpdateResultDto> ProcessPromotionAsync(PromotionRequestDetailsDto request);
    Task<ViewpointUpdateResultDto> ProcessLayoffAsync(LayoffRequestDetailsDto request);
    Task<ViewpointUpdateResultDto> ProcessTerminationAsync(TerminationRequestDetailsDto request);
    Task<ViewpointUpdateResultDto> ProcessReturnToWorkAsync(ReturnToWorkRequestDetailsDto request);
    Task<ViewpointUpdateResultDto> ProcessNewHireAsync(NewHireRequestDetailsDto request);
    
    // Health Check
    Task<bool> IsHealthyAsync();
}
```

### Viewpoint Data Transfer Objects

```csharp
public class ViewpointEmployeeDto
{
    public string EmployeeId { get; set; }
    public string EmployeeNetworkId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get; set; }
    public string Title { get; set; }
    public string Division { get; set; }
    public string Department { get; set; }
    public string Company { get; set; }
    public string Status { get; set; } // Active, Inactive, Laid-Off, Terminated
    public DateTime? HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public ViewpointPositionDto CurrentPosition { get; set; }
    public ViewpointContactDto ContactInfo { get; set; }
}

public class ViewpointPositionDto
{
    public string PayrollCompany { get; set; }
    public string PayrollGroup { get; set; }
    public string PayrollDept { get; set; }
    public string Position { get; set; }
    public string TimeCardSupervisor { get; set; }
    public string VacationSupervisor { get; set; }
    public string FunctionalDept { get; set; }
    public string PhysicalLocation { get; set; }
    public string Status { get; set; }
}

public class ViewpointContactDto
{
    public string EmailAddress { get; set; }
    public string PhoneNumber { get; set; }
    public string Address1 { get; set; }
    public string Address2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
}

public class ViewpointUpdateResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string ViewpointTransactionId { get; set; }
    public List<string> Errors { get; set; }
    public DateTime ProcessedDate { get; set; }
}
```

### Viewpoint API Configuration

```json
{
  "ViewpointIntegration": {
    "BaseUrl": "https://viewpoint-api.company.com/api/",
    "ApiKey": "{encrypted-api-key}",
    "ApiVersion": "v1",
    "Timeout": "00:01:00",
    "RetryPolicy": {
      "MaxRetries": 3,
      "BackoffMultiplier": 2,
      "InitialDelay": "00:00:05"
    },
    "Endpoints": {
      "EmployeeSearch": "/employees/search",
      "EmployeeDetails": "/employees/{employeeId}",
      "Companies": "/reference/companies",
      "PayrollGroups": "/reference/payroll-groups",
      "Positions": "/reference/positions",
      "UpdateEmployee": "/employees/{employeeId}/update"
    }
  }
}
```

## 2. Authorization Provider Interface

### IAuthorizationProvider

```csharp
public interface IAuthorizationProvider
{
    Task<UserAuthorizationDto> GetUserAuthorizationAsync(string userId);
    Task<List<string>> GetUserCompanyAccessAsync(string userId);
    Task<bool> IsUserInRoleAsync(string userId, string roleName);
    Task<bool> CanUserAccessCompanyAsync(string userId, string companyCode);
    Task RefreshUserAuthorizationAsync(string userId);
}

public class UserAuthorizationDto
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string DisplayName { get; set; }
    public List<string> CompanyCodes { get; set; }
    public List<string> Roles { get; set; } // "HR", "IT", "Manager"
    public DateTime LastUpdated { get; set; }
    public string Source { get; set; } // "Viewpoint" or "EntraGroups"
}
```

### Implementation Options

#### Option 1: Viewpoint User Profile Implementation
```csharp
public class ViewpointAuthorizationProvider : IAuthorizationProvider
{
    private readonly IViewpointIntegrationService _viewpointService;
    
    public async Task<UserAuthorizationDto> GetUserAuthorizationAsync(string userId)
    {
        // Call Viewpoint API to get user profile
        // Extract company access from user-defined field
        // Map to UserAuthorizationDto
    }
}
```

#### Option 2: Entra Groups Implementation
```csharp
public class EntraGroupAuthorizationProvider : IAuthorizationProvider
{
    private readonly IGraphServiceClient _graphClient;
    
    public async Task<UserAuthorizationDto> GetUserAuthorizationAsync(string userId)
    {
        // Call Microsoft Graph API to get user groups
        // Map group membership to company access
        // Extract roles from specific groups (HR-Role, IT-Role)
    }
}
```

### Entra Group Mapping Configuration
```json
{
  "EntraGroupMapping": {
    "CompanyGroups": {
      "Company-64-MTS": "64",
      "Company-19-Mathy": "19",
      "Company-22-ConstructionPlus": "22"
    },
    "RoleGroups": {
      "HR-Role": "HR",
      "IT-Role": "IT",
      "Managers": "Manager"
    }
  }
}
```

## 3. Email Notification Interface

### IEmailNotificationService

```csharp
public interface IEmailNotificationService
{
    // Queue Management
    Task QueueEmailAsync(EmailNotificationDto emailRequest);
    Task ProcessEmailQueueAsync();
    Task<List<PendingEmailDto>> GetPendingEmailsAsync(int maxCount = 50);
    
    // Template-based Emails
    Task QueueTemplatedEmailAsync(
        string templateName, 
        string requestType, 
        string emailType, 
        Dictionary<string, object> templateData,
        string toEmail,
        string ccEmail = null);
    
    // Specific Email Types
    Task QueueConfirmationEmailAsync(int requestId, string requestType, string submitterEmail);
    Task QueueHRNotificationEmailAsync(int requestId, string requestType);
    Task QueueErrorNotificationEmailAsync(int requestId, string errorMessage, string requestType);
    
    // Template Management
    Task<EmailTemplateDto> GetEmailTemplateAsync(string requestType, string emailType);
    Task UpdateEmailTemplateAsync(int templateId, string subject, string body);
    
    // Status Tracking
    Task MarkEmailAsSentAsync(int notificationId);
    Task MarkEmailAsFailedAsync(int notificationId, string errorMessage);
    Task<EmailDeliveryStatsDto> GetDeliveryStatsAsync(DateTime fromDate, DateTime toDate);
}
```

### Email DTOs

```csharp
public class EmailNotificationDto
{
    public string ToEmail { get; set; }
    public string CcEmail { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public string RequestType { get; set; }
    public int? RequestId { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

public class EmailTemplateDto
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; }
    public string RequestType { get; set; }
    public string EmailType { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsActive { get; set; }
}

public class EmailDeliveryStatsDto
{
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
    public int Pending { get; set; }
    public decimal SuccessRate { get; set; }
    public List<EmailFailureReasonDto> FailureReasons { get; set; }
}
```

### Email Template Variables

#### Common Variables (All Request Types)
- `{{RequestId}}` - HR Request ID
- `{{SubmittedBy}}` - Submitter name
- `{{SubmittedDate}}` - Submission date
- `{{EmployeeName}}` - Employee name (single employee requests)
- `{{EmployeeCount}}` - Number of employees (multi-employee requests)
- `{{EmployeeNames}}` - Comma-separated employee names
- `{{EffectiveDate}}` - Request effective date
- `{{Notes}}` - Request notes

#### Request-Specific Variables
- **Promotion**: `{{CurrentPosition}}`, `{{NewPosition}}`, `{{PayrollType}}`
- **Layoff**: `{{LastDayWorked}}`
- **Termination**: `{{TerminationReason}}`, `{{ContestUnemployment}}`
- **Return to Work**: `{{PreviousLayoffDate}}`

## 4. Background Job Processing Interface

### IBackgroundJobService

```csharp
public interface IBackgroundJobService
{
    // Reference Data Sync
    Task ScheduleReferenceDataSyncAsync();
    Task ExecuteReferenceDataSyncAsync();
    
    // Email Processing
    Task ScheduleEmailProcessingAsync();
    Task ExecuteEmailProcessingAsync();
    
    // Viewpoint Integration Processing
    Task ScheduleViewpointProcessingAsync();
    Task ExecuteViewpointProcessingAsync();
    
    // Health Checks
    Task<List<BackgroundJobStatusDto>> GetJobStatusAsync();
    Task<bool> IsJobRunningAsync(string jobName);
}

public class BackgroundJobStatusDto
{
    public string JobName { get; set; }
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
    public string Status { get; set; } // "Running", "Completed", "Failed"
    public string LastError { get; set; }
    public TimeSpan? Duration { get; set; }
}
```

## Open Questions & Design Decisions Needed

### 1. Viewpoint Integration Details

#### **CRITICAL: Data Model Mapping**
- **Question**: What are the actual Viewpoint API endpoints and data structures?
- **Need**: Complete API documentation from Viewpoint
- **Impact**: Core employee search and update functionality
- **Timeline**: Required before development starts

#### **Complex Dropdown Dependencies**
- **Question**: How are payroll company → payroll group relationships modeled in Viewpoint?
- **Options**: 
  1. Flat lookup tables with filtering logic
  2. Hierarchical relationship tables
  3. Real-time API calls for dependent dropdowns
- **Recommendation**: Start with flat tables, optimize later based on performance

#### **Employee Status Management**
- **Question**: How does Viewpoint track employee status (Active, Laid-Off, Terminated)?
- **Need**: Understanding of status field values and transitions
- **Impact**: Return to work functionality and employee filtering

### 2. Authorization Strategy

#### **User Company Access Method**
- **Current Plan**: Flexible interface supporting both Viewpoint and Entra groups
- **Decision Needed**: Which method to implement first?
- **Recommendation**: Start with Viewpoint user profile fields, migrate to Entra groups later

#### **Role-Based Access Control**
- **Question**: Should IT/HR roles be managed in Entra groups or Viewpoint?
- **Recommendation**: Entra groups for roles, Viewpoint for company access
- **Reason**: Easier security management through Azure AD

### 3. Email System Design

#### **Email Template Management**
- **Question**: Who manages email templates? HR, IT, or system admins?
- **Recommendation**: Start with database-stored templates, add admin UI later
- **Features Needed**: Template variables, HTML support, approval workflow

#### **Email Delivery Reliability**
- **Question**: What happens when SMTP server is down?
- **Solution**: Queue-based approach with retry logic and dead letter handling
- **Monitoring**: Email delivery dashboard for admins

### 4. Request Processing Workflow

#### **Submission vs. Processing**
- **Question**: What exactly happens when a request is "submitted"?
- **Current Understanding**: 
  1. Save to database
  2. Send confirmation email
  3. Queue for Viewpoint processing
  4. Send HR notification
- **Clarification Needed**: Approval workflows, manual vs. automatic processing

#### **Error Handling Strategy**
- **Scenario**: Viewpoint API is down during submission
- **Decision**: Allow submission to succeed, queue for later processing
- **Notification**: Email admin about integration failures
- **Recovery**: Manual retry mechanism for failed updates

### 5. Performance and Scalability

#### **Employee Search Performance**
- **Question**: How many employees are in Viewpoint? Expected search volume?
- **Optimization Options**:
  1. Real-time API calls (simple, potentially slow)
  2. Cached employee data (complex, fast)
  3. Hybrid approach (cache frequently accessed data)
- **Recommendation**: Start with real-time, add caching if needed

#### **Reference Data Sync Frequency**
- **Question**: How often do lookup values change in Viewpoint?
- **Current Plan**: Every 4 hours
- **Considerations**: API rate limits, data freshness requirements

### 6. Security and Compliance

#### **Data Retention**
- **Question**: How long should HR requests be retained?
- **Considerations**: Legal requirements, audit needs, database size
- **Recommendation**: Soft deletes with configurable archive policy

#### **Sensitive Data Handling**
- **Question**: Are there PII or sensitive data requirements?
- **Current Approach**: Store minimal employee data, reference Viewpoint for details
- **Compliance**: May need encryption at rest, access logging

### 7. Integration Testing Strategy

#### **Viewpoint API Testing**
- **Challenge**: Testing against live Viewpoint system
- **Options**:
  1. Mock Viewpoint service for development
  2. Viewpoint test environment
  3. Contract testing approach
- **Recommendation**: Mock service + contract tests

#### **End-to-End Testing**
- **Question**: How to test complete workflows without affecting live data?
- **Solution**: Test employee records in Viewpoint, isolated test companies

## Implementation Priority

### Phase 1: Core Foundation
1. ✅ Database schema
2. ✅ API structure
3. 🔄 Mock Viewpoint integration service
4. 🔄 Basic authentication/authorization
5. 🔄 Reference data management

### Phase 2: Basic Request Processing
1. Employee search (mocked)
2. Request creation and storage
3. Basic email notifications
4. Dashboard functionality

### Phase 3: Real Integration
1. Actual Viewpoint API integration
2. Entra ID/Graph API integration
3. Production email system
4. Background job processing

### Phase 4: Advanced Features
1. Complex approval workflows
2. Reporting and analytics
3. Admin interfaces
4. Performance optimization

## Risk Mitigation

### **High Risk**: Viewpoint API Dependencies
- **Mitigation**: Interface-based design, comprehensive mocking
- **Fallback**: Manual processing workflow if API unavailable

### **Medium Risk**: Authentication Complexity
- **Mitigation**: Flexible authorization provider interface
- **Fallback**: Simple role-based access until integration ready

### **Medium Risk**: Email Delivery Issues**
- **Mitigation**: Queue-based approach, multiple retry attempts
- **Monitoring**: Email delivery status dashboard

### **Low Risk**: Performance Issues
- **Mitigation**: Pagination, caching strategy, database optimization
- **Monitoring**: Application performance monitoring (APM)

## Next Steps

1. **Immediate**: Get Viewpoint API documentation and test environment access
2. **Week 1**: Implement mock services and basic API structure
3. **Week 2**: Build core request management functionality
4. **Week 3**: Integrate authentication and basic Viewpoint calls
5. **Month 1**: Complete MVP with mocked integrations
6. **Month 2**: Replace mocks with real integrations

## Success Criteria

- ✅ All request types can be created and stored
- ✅ Employee search works (mocked initially)
- ✅ Email notifications are sent reliably
- ✅ User authorization works correctly
- ✅ Reference data syncs automatically
- ✅ System handles integration failures gracefully
- ✅ Performance meets user expectations (< 2 second response times)
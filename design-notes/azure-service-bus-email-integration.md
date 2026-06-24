# Azure Service Bus Email Integration - New Hire "On Submission" Notifications

## Overview

This document describes the implementation of Azure Service Bus email notifications for New Hire requests, triggered when a request is submitted.

**Implementation Date**: January 2025
**Module**: New Hire Requests
**Trigger**: On Submission
**Status**: ✅ Complete

---

## Architecture

### Email Flow

```
New Hire Request Submission
    ↓
NewHireRequestsController
    ↓
SendNewHireSubmissionNotificationsAsync()
    ↓
IAzureServiceBusEmailService.SendEmailNotificationAsync()
    ↓
Azure Service Bus Queue
    ↓
Power Automate (Consumer)
    ↓
Actual Email Delivery
```

### Key Design Decisions

1. **Non-blocking**: Email failures do not block request creation
2. **Isolated errors**: Each notification wrapped in try-catch
3. **Structured data**: Template data sent as dictionary for Power Automate processing
4. **Conditional logic**: Task emails only sent when relevant conditions are met

---

## Implementation Details

### File Modified

**Location**: `server/src/Mathy.ELM.Api/Controllers/NewHireRequestsController.cs`

### Changes Summary

#### 1. Service Injection (Lines 1-25)

Added `IAzureServiceBusEmailService` dependency:

```csharp
private readonly IAzureServiceBusEmailService _emailService;

public NewHireRequestsController(
    IHRRequestService hrRequestService,
    INewHireRequestDetailsService newHireDetailsService,
    IAzureServiceBusEmailService emailService)
{
    _hrRequestService = hrRequestService;
    _newHireDetailsService = newHireDetailsService;
    _emailService = emailService;
}
```

#### 2. Helper Methods

**BuildNewHireTemplateData()** (Lines 156-187)
- Maps `CreateNewHireRequestDto` to email template data dictionary
- Returns `Dictionary<string, string>` with all relevant fields

**Template Data Fields:**
- `StartDate`: First day of employment
- `NewEmployeeName`: Full name
- `PreferredName`: Preferred first name
- `Company`: Company code
- `Division`: Location code
- `Position`: Position code
- `HourlySalaried`: Employment type
- `EmploymentStatus`: Employment status
- `Supervisor`: Supervisor ID
- `Rehire`: Y/N
- `BYOD`: Yes/No
- `EmailRequired`: Yes/No
- `CreditCardRequested`: Yes/No
- `VehicleRequested`: Yes/No
- `DoorAccessRequested`: Yes/No
- `FuelCardlockRequested`: Yes/No
- `RequestId`: Parent request ID

**GetNotificationRecipients()** (Lines 192-209)
- Resolves recipient email addresses
- Returns tuple: `(managerEmail, submitterEmail, siteDLs)`

**Current Implementation:**
```csharp
string managerEmail = "manager@example.com"; // TODO: Lookup from SupervisorId
string submitterEmail = "submitter@example.com"; // TODO: Get from user context
var siteDLs = new List<string>
{
    "hr-team@example.com",
    "payroll-team@example.com",
    "safety-team@example.com"
};
```

**SendNewHireSubmissionNotificationsAsync()** (Lines 214-415)
- Orchestrates all submission email notifications
- Non-blocking: failures logged but don't throw exceptions
- Comprehensive logging for debugging

#### 3. Integration Points

**CreateNewHireRequest Endpoint** (Lines 591-595)

After successful New Hire details creation:
```csharp
// Step 3: Send email notifications (non-blocking)
Console.WriteLine($"[NEW HIRE] Step 3: Sending email notifications");
var parentRequestId = hrRequestResult.Data.First().ParentRequestId;
await SendNewHireSubmissionNotificationsAsync(request, parentRequestId);
Console.WriteLine($"[NEW HIRE] Step 3 COMPLETE: Email notifications processed");
```

**UpdateNewHireRequest Endpoint** (Lines 821-824)

After successful update and submit:
```csharp
// Send email notifications after successful update and submit
Console.WriteLine($"[NEW HIRE UPDATE] Sending email notifications for parent ID: {parentId}");
await SendNewHireSubmissionNotificationsAsync(request, parentId);
Console.WriteLine($"[NEW HIRE UPDATE] Email notifications processed");
```

---

## Email Notifications

### 1. Confirmation Email

**Trigger**: Always sent on submission
**Recipients**: Manager, Submitter, Site DLs (HR, Payroll, Safety)
**Type**: `Confirmation`
**Priority**: `2` (Normal)
**Module**: `NewHire`
**Trigger Field**: `OnSubmission`

**Template Data**: All fields from BuildNewHireTemplateData()

**Subject Format**:
```
New Hire Request Confirmation - {FirstName} {LastName}
```

**Body**:
```
New hire request submitted for {FirstName} {LastName} starting {StartDate}.
```

---

### 2. Physical Security Task

**Trigger**: Conditional - Only if door access requested
**Condition**: `BuildingAccess.Any() == true`
**Recipient**: `physical-security@example.com`
**Type**: `Task`
**Priority**: `2` (Normal)
**Module**: `NewHire`
**Trigger Field**: `OnSubmission`

**Subject Format**:
```
Door Access Request - {FirstName} {LastName}
```

**Body**:
```
Door access requested for new hire {FirstName} {LastName}.
Building access details: {AccessDescriptions}
```

---

### 3. Credit Card Task

**Trigger**: Conditional - Only if any credit card issued
**Condition**: `KwikTripCard || CompanyExpenseCard`
**Recipient**: `credit-card-team@example.com`
**Type**: `Task`
**Priority**: `2` (Normal)
**Module**: `NewHire`
**Trigger Field**: `OnSubmission`

**Subject Format**:
```
Credit Card Request - {FirstName} {LastName}
```

**Body**:
```
Credit card requested for new hire {FirstName} {LastName}.
Card types: KwikTrip={bool}, CompanyExpense={bool}, FuelOnly={bool}, EEExpense={bool}
```

---

### 4. Fuel Fob Task

**Trigger**: Conditional - Only if fuel cardlock access requested
**Condition**: `FuelCardlockAccess == true`
**Recipient**: `fuel-fob-team@example.com`
**Type**: `Task`
**Priority**: `2` (Normal)
**Module**: `NewHire`
**Trigger Field**: `OnSubmission`

**Subject Format**:
```
Fuel Fob Request - {FirstName} {LastName}
```

**Body**:
```
Fuel fob requested for new hire {FirstName} {LastName}.
Fuel cardlock address: {FuelCardlockAddress}
```

---

### 5. Fleet Task

**Trigger**: Conditional - Only if company vehicle requested
**Condition**: `NeedCompanyCar == true`
**Recipient**: `fleet-team@example.com`
**Type**: `Task`
**Priority**: `2` (Normal)
**Module**: `NewHire`
**Trigger Field**: `OnSubmission`

**Subject Format**:
```
Vehicle Assignment Request - {FirstName} {LastName}
```

**Body**:
```
Company vehicle requested for new hire {FirstName} {LastName} starting {StartDate}.
Driver classification: {DriverClassification}
```

---

### 6. Compliance Task

**Trigger**: Always sent on submission
**Recipient**: `compliance-team@example.com`
**Type**: `Task`
**Priority**: `2` (Normal)
**Module**: `NewHire`
**Trigger Field**: `OnSubmission`

**Subject Format**:
```
New Hire Compliance - {FirstName} {LastName}
```

**Body**:
```
New hire compliance notification for {FirstName} {LastName}.
Position: {PositionCode}, Start Date: {StartDate}
```

---

### 7. Safety Task

**Trigger**: Always sent on submission
**Recipient**: `safety-team@example.com`
**Type**: `Task`
**Priority**: `2` (Normal)
**Module**: `NewHire`
**Trigger Field**: `OnSubmission`

**Subject Format**:
```
New Hire Safety Notification - {FirstName} {LastName}
```

**Body**:
```
New hire safety notification for {FirstName} {LastName}.
Position: {PositionCode}, Start Date: {StartDate}
```

---

## Error Handling

### Non-Blocking Design

All email operations are wrapped in try-catch blocks to prevent failures from blocking request creation:

```csharp
try
{
    await _emailService.SendEmailNotificationAsync(emailNotification);
    Console.WriteLine($"[NEW HIRE NOTIFICATIONS] {Type} email queued successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"[NEW HIRE NOTIFICATIONS] ERROR: Failed to queue {Type} email: {ex.Message}");
}
```

### Logging Strategy

- **Success**: Logged to console with notification type
- **Failure**: Error logged with exception message
- **Summary**: Overall status logged at end of method

Example logs:
```
[NEW HIRE NOTIFICATIONS] Starting email notifications for request ID: 123
[NEW HIRE NOTIFICATIONS] Confirmation email queued successfully
[NEW HIRE NOTIFICATIONS] Physical Security task email queued successfully
[NEW HIRE NOTIFICATIONS] Credit Card task email queued successfully
[NEW HIRE NOTIFICATIONS] Compliance task email queued successfully
[NEW HIRE NOTIFICATIONS] Safety task email queued successfully
[NEW HIRE NOTIFICATIONS] All notifications processed for request ID: 123
```

---

## Email Notification DTO Structure

Each email sent to Azure Service Bus includes:

```csharp
public class EmailNotificationDto
{
    public string ToEmail { get; set; }              // Recipient(s), comma-separated
    public string? CcEmail { get; set; }             // Optional CC recipients
    public string Subject { get; set; }              // Email subject
    public string Body { get; set; }                 // Simple text description
    public int? RequestId { get; set; }              // HR Request ID for tracking
    public int? TemplateId { get; set; }             // Optional template ID
    public string NotificationType { get; set; }     // "Confirmation", "Task", etc.
    public int Priority { get; set; }                // 1=High, 2=Normal, 3=Low
    public Dictionary<string, string>? TemplateData { get; set; }  // Structured data
    public string? Module { get; set; }              // "NewHire"
    public string? Trigger { get; set; }             // "OnSubmission"
}
```

---

## Testing

### Build Status

✅ **Build Successful**: No compilation errors
⚠️ **Warnings**: Only pre-existing warnings (nullable types, async methods, etc.)

### Manual Testing Checklist

- [ ] Submit New Hire request with all optional fields
- [ ] Verify Confirmation email queued
- [ ] Verify Compliance email queued (always sent)
- [ ] Verify Safety email queued (always sent)
- [ ] Submit request with door access - verify Physical Security email
- [ ] Submit request with credit card - verify Credit Card email
- [ ] Submit request with fuel cardlock - verify Fuel Fob email
- [ ] Submit request with company car - verify Fleet email
- [ ] Update existing draft and submit - verify notifications sent
- [ ] Check Azure Service Bus queue for messages
- [ ] Verify Power Automate processes messages correctly

---

## Future Enhancements

### Phase 2: Recipient Resolution

**TODO Items in Code** (Lines 194-196):

1. **Manager Email Lookup**
   - Query database using `SupervisorId`
   - Get email from Employee or Supervisor table

2. **Submitter Email Resolution**
   - Get from user context/authentication
   - Use current user's email address

3. **Site DL Configuration**
   - Load from configuration/database
   - Support company-specific distribution lists
   - Example: `Companies` table with `HREmail`, `PayrollEmail`, `SafetyEmail` columns

### Phase 3: Team Distribution Lists

Replace placeholder emails with configurable values:

**Suggested Configuration** (appsettings.json):
```json
{
  "EmailNotifications": {
    "Teams": {
      "PhysicalSecurity": "physical.security@mathyconstructioncompany.com",
      "CreditCard": "creditcard@mathyconstructioncompany.com",
      "FuelFob": "fuelfob@mathyconstructioncompany.com",
      "Fleet": "fleet@mathyconstructioncompany.com",
      "Compliance": "compliance@mathyconstructioncompany.com",
      "Safety": "safety@mathyconstructioncompany.com"
    }
  }
}
```

### Phase 4: Additional Notification Triggers

**Implemented:**

1. ✅ **Pre-Start Reminder** (3 days before start date)
   - Status: **IMPLEMENTED** (see section below)
   - Trigger: Daily recurring Hangfire job at 8:00 AM
   - Recipients: Manager, Submitter
   - CC: j.vestil@tritontek.ph, k.sevilla@tritontek.ph
   - Type: "Reminder"
   - Implementation: Background job

**Not Yet Implemented:**

1. **Draft Reminder** (Daily)
   - Trigger: Daily scheduled job
   - Recipients: Submitter
   - Type: "Draft"
   - Requires: Background job implementation

2. **Welcome Email** (On start date)
   - Trigger: Scheduled job on start date
   - Recipients: New employee (if email created)
   - Type: "Welcome"
   - Requires: Background job implementation

3. **Post-Start Follow-up** (1 week after start date)
   - Trigger: Scheduled job
   - Recipients: Manager, Submitter, HR, Payroll
   - Type: "Post Start Date"
   - Condition: Request still in "Submitted" status
   - Requires: Background job implementation

4. **Hot Potato** (Urgent requests)
   - Trigger: Start date < 3 days OR IT equipment requested
   - Recipients: IT, HR
   - Priority: 1 (High)
   - **Deferred**: Per user request

---

## Pre-Start Reminder - 3 Days Before Start Date (IMPLEMENTED)

### Overview

**Implementation Date**: January 2025
**Module**: New Hire Requests
**Trigger**: Three days before FirstDayEmployment date
**Status**: ✅ Complete

This feature sends reminder emails to managers and submitters three days before a new hire's start date to confirm readiness (equipment, credentials, workspace, etc.).

---

### Architecture

#### Background Job Flow

```
Hangfire Recurring Job (Daily @ 8:00 AM)
    ↓
BackgroundJobService.ProcessNewHireStartDateRemindersAsync()
    ↓
Query New Hire requests where:
  - RequestTypeId = 5 (New Hire)
  - RequestStatusId = 1 (Submitted)
  - FirstDayEmployment = Today + 3 days
    ↓
For each eligible request:
  - Build template data
  - Resolve recipients (Manager, Submitter)
  - Send to Azure Service Bus
    ↓
Power Automate (Consumer)
    ↓
Email Delivery
```

---

### Implementation Details

#### Files Modified

1. **IBackgroundJobService.cs** (Interface)
   - Location: `server/src/Mathy.ELM.Core/Services/IBackgroundJobService.cs`
   - Added method signatures (lines 83-91)

2. **BackgroundJobService.cs** (Implementation)
   - Location: `server/src/Mathy.ELM.Infrastructure/Services/BackgroundJobService.cs`
   - Added IAzureServiceBusEmailService dependency injection
   - Implemented recurring job setup method
   - Implemented reminder processing method

3. **Program.cs** (Startup Registration)
   - Location: `server/src/Mathy.ELM.Api/Program.cs`
   - Registered recurring job on application startup (lines 308-318)

---

### Code Details

#### 1. Interface Definition

```csharp
/// <summary>
/// Sets up recurring job to check for New Hire start date reminders (runs daily)
/// </summary>
void SetupNewHireStartDateReminderJob();

/// <summary>
/// Processes New Hire start date reminders for requests starting in 3 days (background job method)
/// </summary>
Task ProcessNewHireStartDateRemindersAsync();
```

#### 2. Job Setup Method

```csharp
public void SetupNewHireStartDateReminderJob()
{
    _logger.LogInformation("Setting up recurring New Hire start date reminder job");

    // Run every day at 8 AM
    RecurringJob.AddOrUpdate(
        "newhire-startdate-reminder",
        () => ProcessNewHireStartDateRemindersAsync(),
        Cron.Daily(8));

    _logger.LogInformation("New Hire start date reminder job scheduled to run daily at 8:00 AM");
}
```

**Key Configuration:**
- **Job ID**: `newhire-startdate-reminder`
- **Schedule**: `Cron.Daily(8)` - Every day at 8:00 AM
- **Method**: `ProcessNewHireStartDateRemindersAsync()`

#### 3. Reminder Processing Method

**Database Query:**
```csharp
var targetDate = DateTime.Today.AddDays(3).Date;

var eligibleRequests = await context.HRRequestDetails
    .Include(rd => rd.ParentRequest)
    .Include(rd => rd.NewHireDetails)
    .Where(rd =>
        rd.RequestTypeId == 5 && // New Hire
        rd.RequestStatusId == 1 && // Submitted/Pending
        !rd.IsDeleted &&
        rd.NewHireDetails != null &&
        rd.NewHireDetails.FirstDayEmployment.HasValue &&
        rd.NewHireDetails.FirstDayEmployment.Value.Date == targetDate
    )
    .ToListAsync();
```

**Filter Criteria:**
- Request Type: New Hire (ID = 5)
- Request Status: Submitted (ID = 1)
- Not deleted
- Has New Hire details with FirstDayEmployment
- FirstDayEmployment date equals Today + 3 days

**Template Data:**
```csharp
var templateData = new Dictionary<string, string>
{
    ["StartDate"] = newHireDetails.FirstDayEmployment?.ToString("yyyy-MM-dd") ?? "N/A",
    ["DaysUntilStart"] = "3",
    ["NewEmployeeName"] = $"{newHireDetails.FirstName} {newHireDetails.LastName}",
    ["PreferredName"] = newHireDetails.PreferredFirstName ?? newHireDetails.FirstName ?? "",
    ["Company"] = newHireDetails.CompanyCode?.ToString() ?? "N/A",
    ["Division"] = newHireDetails.LocationCode?.ToString() ?? "N/A",
    ["Position"] = newHireDetails.PositionCode ?? "N/A",
    ["HourlySalaried"] = newHireDetails.HourlySalaried ?? "N/A",
    ["EmploymentStatus"] = newHireDetails.EmploymentStatus ?? "N/A",
    ["Supervisor"] = newHireDetails.SupervisorId?.ToString() ?? "N/A",
    ["Rehire"] = newHireDetails.Rehire.HasValue ? (newHireDetails.Rehire.Value ? "Y" : "N") : "N",
    ["RequestId"] = requestDetail.ParentRequestId.ToString()
};
```

**Email Notification:**
```csharp
var reminderEmail = new EmailNotificationDto
{
    ToEmail = $"{managerEmail}, {submitterEmail}",
    CcEmail = "j.vestil@tritontek.ph, k.sevilla@tritontek.ph",
    Subject = $"New Hire Start Date Reminder - {employeeName} starts in 3 days",
    Body = $"Reminder: New employee {employeeName} is scheduled to start in 3 days on {newHireDetails.FirstDayEmployment:yyyy-MM-dd}. Please ensure equipment, login credentials, and workspace are ready.",
    NotificationType = "Reminder",
    Priority = 2,
    RequestId = requestDetail.ParentRequestId,
    Module = "NewHire",
    Trigger = "ThreeDaysBeforeStartDate",
    TemplateData = templateData
};

await _azureServiceBusEmailService.SendEmailNotificationAsync(reminderEmail);
```

#### 4. Startup Registration

```csharp
// Setup recurring background jobs
try
{
    var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
    backgroundJobService.SetupNewHireStartDateReminderJob();
    logger.LogInformation("New Hire start date reminder job registered successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to register New Hire start date reminder job");
}
```

**Execution Context:**
- Runs during application startup
- After database migrations
- After Azure Service Bus queue initialization
- Non-blocking: errors logged but don't prevent startup

---

### Email Notification Details

**Type**: Reminder
**Priority**: 2 (Normal)
**Module**: NewHire
**Trigger**: ThreeDaysBeforeStartDate

**Recipients:**
- **To**: Manager email, Submitter email (comma-separated)
- **CC**: j.vestil@tritontek.ph, k.sevilla@tritontek.ph

**Subject Format:**
```
New Hire Start Date Reminder - {FirstName} {LastName} starts in 3 days
```

**Body:**
```
Reminder: New employee {FirstName} {LastName} is scheduled to start in 3 days on {StartDate}.
Please ensure equipment, login credentials, and workspace are ready.
```

**Template Data Fields:**
- StartDate
- DaysUntilStart
- NewEmployeeName
- PreferredName
- Company
- Division
- Position
- HourlySalaried
- EmploymentStatus
- Supervisor
- Rehire
- RequestId

---

### Error Handling

#### Non-Blocking Design

Individual email failures do not stop processing other requests:

```csharp
foreach (var requestDetail in eligibleRequests)
{
    try
    {
        // Process email notification
    }
    catch (Exception ex)
    {
        failureCount++;
        _logger.LogError(ex, "[NEW HIRE REMINDER] ERROR: Failed to send reminder for request {ParentRequestId}",
            requestDetail.ParentRequestId);
        // Continue processing other requests
    }
}
```

#### Logging Strategy

**Log Prefix**: `[NEW HIRE REMINDER]`

**Key Log Points:**
1. Job start: `"Starting daily reminder job for {TargetDate}"`
2. Query results: `"Found {Count} eligible requests for start date reminders"`
3. Per-request success: `"Email queued successfully for request {ParentRequestId}"`
4. Per-request failure: `"Failed to queue email for request {ParentRequestId}: {Error}"`
5. Job completion: `"Daily reminder job completed. Total: {Total}, Success: {Success}, Failed: {Failed}"`
6. Critical errors: `"CRITICAL ERROR: Failed to process start date reminders"`

**Example Log Output:**
```
[NEW HIRE REMINDER] Starting daily reminder job for 2025-01-20
[NEW HIRE REMINDER] Found 3 eligible requests for start date reminders
[NEW HIRE REMINDER] Email queued successfully for request 45
[NEW HIRE REMINDER] Email queued successfully for request 67
[NEW HIRE REMINDER] Email queued successfully for request 89
[NEW HIRE REMINDER] Daily reminder job completed. Total: 3, Success: 3, Failed: 0
```

---

### Testing

#### Build Status

✅ **Build Successful**: No compilation errors
⚠️ **Warnings**: Only pre-existing warnings (99 total)

#### Manual Testing Checklist

**Preparation:**
- [ ] Create test New Hire request with FirstDayEmployment = Today + 3 days
- [ ] Set RequestStatusId = 1 (Submitted)
- [ ] Ensure request has valid manager and submitter data

**Job Execution:**
- [ ] Verify job registered on application startup
- [ ] Check Hangfire Dashboard at `/hangfire` for job presence
- [ ] Verify job schedule: Daily at 8:00 AM
- [ ] Manually trigger job from Hangfire Dashboard

**Email Verification:**
- [ ] Check logs for `[NEW HIRE REMINDER]` entries
- [ ] Verify correct number of eligible requests found
- [ ] Verify email queued to Azure Service Bus
- [ ] Check Azure Service Bus queue for messages
- [ ] Verify Power Automate processes message
- [ ] Confirm email delivered to recipients

**Edge Cases:**
- [ ] No requests matching criteria (empty result)
- [ ] Request with missing FirstDayEmployment (should be excluded)
- [ ] Request in Draft status (should be excluded)
- [ ] Request with start date 2 days away (should be excluded)
- [ ] Request with start date 4 days away (should be excluded)
- [ ] Multiple requests on same day (all should process)

**Error Scenarios:**
- [ ] Azure Service Bus unavailable (should log error, continue)
- [ ] Invalid request data (should skip, log error, continue)
- [ ] Database connection issue (should throw, Hangfire retries)

---

### Future Enhancements

#### Phase 1: Recipient Resolution

**Current Implementation** (Placeholders):
```csharp
string managerEmail = "manager@example.com"; // TODO
string submitterEmail = "submitter@example.com"; // TODO
```

**Planned Enhancement:**
1. **Manager Email Lookup**
   - Query Employees table using SupervisorId from NewHireDetails
   - Fallback to default HR contact if not found

2. **Submitter Email Resolution**
   - Store submitter email in ParentRequest or HRRequestDetails
   - Add CreatedByEmail field during request creation
   - Get from user context at submission time

#### Phase 2: Configurable Schedule

Allow administrators to configure:
- Reminder timing (currently hardcoded to 3 days)
- Email send time (currently 8:00 AM)
- Additional reminder intervals (e.g., 7 days, 1 day)

#### Phase 3: Enhanced Template Data

Add additional context:
- IT equipment details
- Door access requirements
- Vehicle assignment status
- Credit card status
- Direct links to request details in application

---

### Dependencies

**Required Services:**
- Hangfire (background job processing)
- Azure Service Bus (email queueing)
- Entity Framework Core (database queries)
- IServiceScopeFactory (scoped service creation)

**Database Tables:**
- HRRequestDetails
- NewHireRequestDetails
- (Future: Employees for manager lookup)

---

### Hangfire Dashboard

**Access URL**: `/hangfire`
**Job ID**: `newhire-startdate-reminder`
**Schedule**: `0 8 * * *` (Cron: Daily at 8:00 AM)

**Dashboard Features:**
- View job history
- See execution times
- Monitor success/failure
- Manually trigger job
- View retry attempts
- Check job state

---

## Related Documentation

- **Notifications Table**: `/design-notes/notifications_table.md`
- **API Design**: `/design-notes/API_DESIGN.md`
- **Architecture**: `/design-notes/ARCHITECTURE.md`
- **Email DTO**: `server/src/Mathy.ELM.Core/DTOs/EmailNotificationDto.cs`
- **Service Interface**: `server/src/Mathy.ELM.Core/Services/IAzureServiceBusEmailService.cs`
- **Service Implementation**: `server/src/Mathy.ELM.Infrastructure/Services/AzureServiceBusEmailService.cs`

---

## Commit Information

**Branch**: `initial-structure`
**Files Modified**:
- `server/src/Mathy.ELM.Api/Controllers/NewHireRequestsController.cs`

**Summary**:
```
Integrate Azure Service Bus email notifications for New Hire "On Submission"

- Add IAzureServiceBusEmailService injection to controller
- Implement BuildNewHireTemplateData() helper method
- Implement GetNotificationRecipients() helper method
- Implement SendNewHireSubmissionNotificationsAsync() orchestration method
- Add 7 email notifications triggered on submission:
  1. Confirmation (always sent)
  2. Physical Security Task (conditional)
  3. Credit Card Task (conditional)
  4. Fuel Fob Task (conditional)
  5. Fleet Task (conditional)
  6. Compliance Task (always sent)
  7. Safety Task (always sent)
- Integrate notifications into CreateNewHireRequest endpoint
- Integrate notifications into UpdateNewHireRequest endpoint
- Non-blocking design: email failures do not block request creation
- Comprehensive logging for debugging
```

---

## Contact

For questions or issues related to this implementation, contact the development team.

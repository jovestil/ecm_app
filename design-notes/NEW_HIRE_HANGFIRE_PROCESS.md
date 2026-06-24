# New Hire Request - Hangfire Background Jobs

This document describes the Hangfire background job processes for New Hire requests, including Viewpoint integration.

## Overview

When a new hire request is submitted, several background jobs are scheduled to handle:
1. Email notifications (immediate and scheduled)
2. Pre-employment processing (3 days before start)
3. Employee verification in Viewpoint (on start date)

---

## Frontend Submission Flow

### 1. onSubmit() (`new-hire-request.component.ts:2065`)

- Validates form (`this.newHireForm.valid`)
- If invalid: marks all fields as touched, shows error toast
- If valid: calls `transformFormToNewHireRequest()`

### 2. transformFormToNewHireRequest() (`new-hire-request.component.ts:2139`)

Transforms form data into `CreateNewHireRequest` DTO:

| Section | Fields |
|---------|--------|
| **Personal Info** | firstName, lastName, suffix, preferredName, firstDayEmployment, referredBy, rehire |
| **Position Info** | companyCode, locationCode, employmentStatus, isUnion, unionCraftId, isApprentice, isUnionWage, salaryCode, positionCode, payrollDeptCode, supervisorId, appPercentage |
| **IT Info** | emailRequired, alternateDeliveryLocation, msOfficeLicenseE5/F3 |
| **Credit Card Info** | kwikTripCard, companyExpenseCard, fuelOnlyCard, eeExpenseCard, weeklyLimit, fuelCardlockAccess |
| **Vehicle Info** | isApprovedToOperate, driverClassification, drugAndAlcoholProfile, needCompanyCar, isApplicationPart2Complete |
| **Phone Info** | deskPhone, companyCellphone, byodCellphone, workPhoneNumber, workExtension, reusingExistingPhone |
| **Applications** | applicationId, accessNotes |
| **Folders** | folderType, folderName |
| **Tablet Profiles** | tabletProfileId, tabletProfileName, rolesRequiredForNewHire |
| **Computer Requirements** | computerRequirementsId, isChild, parentId |
| **Building Access** | accessId, accessDescription |
| **Notes** | combined from delivery note and role notes |

### 3. API Call (`hr-request.service.ts:277`)

```
POST /api/v1/NewHireRequests/CreateNewHireRequest
```

---

## Backend Controller Flow

**Location**: `NewHireRequestsController.cs:507`

### Step 0: Create AD User (lines 521-578)
- Checks if AD integration is enabled (`ActiveDirectory:IsActive`)
- Creates user in Active Directory OU
- Returns: `adUsername`, `adEmail`, `adPassword`
- On failure: Attempts rollback, returns error

### Step 1: Create HR Request (lines 580-620)
- Creates `CreateMultiEmployeeHRRequestDto` with `RequestTypeId = 5` (NewHire)
- Calls `_hrRequestService.CreateMultiEmployeeHRRequestAsync()`
- On failure: Rolls back AD user, returns error

### Step 2: Create New Hire Request Details (lines 624-679)
- Calls `_newHireDetailsService.CreateNewHireRequestDetailsAsync()`
- Saves all detail records
- On failure: Rolls back HR request + AD user, returns error

### Step 2.5: Create ServiceDesk Record (lines 683-707)
- Non-blocking - won't fail request if this fails
- Creates ticket in external ServiceDesk system

### Step 3: Send Email Notifications (lines 709-712)
- Calls `SendNewHireSubmissionNotificationsAsync()`
- Sends immediate notifications to manager, submitter, site DLs

### Step 4: Schedule Email Notifications (lines 714-791)
- Processes all active scheduled email templates
- Calculates trigger dates based on `FirstDayEmployment + SubmissionFreq`
- Uses Hangfire to schedule/enqueue emails

---

## Hangfire Background Jobs

### 1. SendScheduledNewHireEmailAsync

**Location**: `BackgroundJobService.cs:1474`

**Purpose**: Sends scheduled email notifications for new hire requests

**Flow**:
1. Fetches email template by `templateId`
2. Fetches request details by `parentRequestId`
3. Validates request status (e.g., skips "Past Start Date" if already completed)
4. Checks for duplicate notifications in `NotificationQueue`
5. Maps `NewHireDetails` to DTO
6. Resolves recipients from template (Manager, Submitter, Site DLs)
7. Sends email via `IAzureServiceBusEmailService`
8. Logs success/failure
9. Throws exception on failure (so Hangfire retries)

---

### 2. ProcessNewHireEmailNotificationsAsync

**Location**: `BackgroundJobService.cs:1298`

**Purpose**: Daily recurring job that processes all pending new hire email notifications

**Schedule**: Runs daily at **12:00 AM (midnight)**

**Flow**:
1. Gets all active `NEWHIRE` email templates with `TriggerType = "Scheduled"`
2. For each template, gets all submitted new hire requests (`RequestStatusId = 1 or 2`)
3. Calculates `TriggerDate = FirstDayEmployment + SubmissionFreq`
4. If `TriggerDate <= today`: **Enqueue immediately** via `BackgroundJob.Enqueue()`
5. If `TriggerDate > today`: **Schedule for later** via `BackgroundJob.Schedule()`
6. Calls `SendScheduledNewHireEmailAsync()` for each job

---

### 3. TriggerOverdueScheduledEmailsAsync

**Location**: `BackgroundJobService.cs:1635`

**Purpose**: Immediately triggers any overdue scheduled emails when a new hire is created/updated

**Flow**:
1. Gets the new hire request and `FirstDayEmployment`
2. Gets all active `NEWHIRE` email templates
3. For each template, calculates `TriggerDate`
4. If `TriggerDate <= today`: Checks for duplicates, then enqueues immediately
5. Supports both positive and negative `SubmissionFreq` (post-start and pre-start emails)

---

### 4. ProcessNewHirePreEmploymentAsync

**Location**: `BackgroundJobService.cs:2822`

**Purpose**: Pre-employment processing 3 days before `FirstDayEmployment`

**Scheduled by**: `ScheduleNewHirePreEmploymentProcessingJob()` (line 2782)
- `ScheduledDate = FirstDayEmployment - 3 days`

**Flow**:
1. Fetches HR request detail with `NewHireDetails`
2. Builds `UpdateEmployeeNewHireRequestDto`
3. Calls `viewpointService.UpdateEmployeeForNewHireInViewPointAsync()`
4. Polls Viewpoint for verification (up to 10 attempts, 1 minute intervals)
5. Updates `RequestStatusId` to **Completed (3)** or **Failed (4)**
6. Sends completion/failure notification email

---

### 5. VerifyNewHireEmployeeInViewpointAsync

**Location**: `BackgroundJobService.cs:1031`

**Purpose**: Verifies new hire employee exists in Viewpoint after start date

**Scheduled by**: `ScheduleViewpointVerifyNewHireEmployee()` (line 1015)
- Scheduled for `FirstDayEmployment` date

**Flow**:
1. Checks if employee exists in Viewpoint by `EmployeeNumber`
2. If found: Updates `RequestStatusId` to **Completed**
3. If not found: Retries up to 2 times with 2-hour delays
4. After max retries: Sets status to **Failed**, sends notification

#### Retry Logic

| Attempt | Timing | Action |
|---------|--------|--------|
| 1 | On `FirstDayEmployment` | Initial verification |
| 2 | +2 hours | Retry if not found or API failed |
| 3+ | N/A | Mark as **Failed** after 2 attempts |

#### Status Updates

| Scenario | RequestStatusId | Message |
|----------|-----------------|---------|
| Processing | 2 (Processing) | "Verifying new hire employee in Viewpoint" |
| Single employee found | 3 (Completed) | "Successfully verified with HRRef: X" |
| Multiple employees found | 4 (Failed) | "Employee is duplicate in Viewpoint HRRM" |
| Not found after retries | 4 (Failed) | "Employee not found after 2 attempts" |
| API failure after retries | 4 (Failed) | "API failures exhausted" |

---

## Viewpoint Integration

### UpdateEmployeeForNewHireInViewPointAsync

**Location**: `ViewpointService.cs:1927`

**Purpose**: Updates a new hire employee's custom fields in Viewpoint/Vista system

#### Request DTO

```csharp
public class UpdateEmployeeNewHireRequestDto
{
    public int HRCo { get; set; }              // Company code
    public int HRRef { get; set; }              // Employee reference number
    public string? PRDept { get; set; }         // Payroll department
    public string? LastName { get; set; }       // For verification
    public string? HireDate { get; set; }       // For verification
    public ViewpointCustomFieldsUpdateDto? CustomFields { get; set; }
}

public class ViewpointCustomFieldsUpdateDto
{
    public string? udSupervisor { get; set; }       // Supervisor employee number
    public string? udNetworkUserID { get; set; }    // AD username
    public string? udWorkEmail { get; set; }        // Work email
    public string? udPhysicalLocation { get; set; } // Location code
    public string? udNickname { get; set; }         // Preferred name
    public string? PositionCode { get; set; }       // Position
    public string? Status { get; set; }             // Employment status
}
```

#### Flow

```
Step 1: Search for employee in Viewpoint
         └─► SearchEmployeeInNewHireWithAPIAsync(HRCo, PRDept, LastName, HireDate)
                   │
                   ▼
Step 2: Validate employee exists
         ├─► Not found → Return error
         └─► Found → Continue
                   │
                   ▼
Step 3: Verify HRRef matches (if provided)
         ├─► Mismatch → Return error
         └─► Match → Continue
                   │
                   ▼
Step 4: Prepare update request
         └─► Build JSON payload with __key and __custom_fields
                   │
                   ▼
Step 5: POST to Viewpoint API
         └─► {BaseUrl}/{SubscriberCode}/vista/hr/2/data/resources/actions/update
                   │
                   ▼
Step 6: Parse response
         └─► Get ActionId for tracking
                   │
                   ▼
Return: UpdateEmployeeNewHireResultDto
         ├─► Success = true/false
         ├─► EmployeeFound = true/false
         ├─► UpdateQueued = true/false
         ├─► ActionId = "..." (for verification)
         └─► ActionStatus = "..."
```

#### Result DTO

```csharp
public class UpdateEmployeeNewHireResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public bool EmployeeFound { get; set; }
    public ViewpointEmployeeDto? Employee { get; set; }
    public bool UpdateQueued { get; set; }
    public string? ActionId { get; set; }        // For tracking/verification
    public string? ActionStatus { get; set; }
    public string? ErrorMessage { get; set; }
}
```

---

### ScheduleViewpointVerifyNewHireEmployee

**Location**: `BackgroundJobService.cs:1015`

**Purpose**: Schedules a Hangfire job to verify the new hire exists in Viewpoint on their first day of employment

#### Flow

```
ScheduleViewpointVerifyNewHireEmployee(hrRequestDetailId, firstDayEmployment, submitterEmail)
         │
         ▼
BackgroundJob.Schedule(VerifyNewHireEmployeeInViewpointAsync, firstDayEmployment)
         │
         ▼
Return: jobId (stored in HRRequestDetail.HangfireJobId)
```

---

## Email Templates Timing

| Template | SubmissionFreq | Trigger Date |
|----------|----------------|--------------|
| Pre-Start Date | -7, -3 | 7 or 3 days BEFORE FirstDayEmployment |
| Welcome Email | 0 | ON FirstDayEmployment |
| Past Start Date | +7, +14 | 7 or 14 days AFTER FirstDayEmployment |

---

## Summary Flow Diagrams

### Complete Submission Flow

```
Frontend                          Backend
   │                                 │
   ├─► Validate Form                 │
   ├─► Transform to DTO              │
   ├─► POST /CreateNewHireRequest ──►│
   │                                 ├─► Step 0: Create AD User
   │                                 ├─► Step 1: Create HR Request
   │                                 ├─► Step 2: Create New Hire Details
   │                                 ├─► Step 2.5: Create ServiceDesk Record
   │                                 ├─► Step 3: Send Immediate Emails
   │                                 ├─► Step 4: Schedule Future Emails
   │◄── Success/Error Response ◄─────┤
   │                                 │
   ├─► Show Toast Message            │
   └─► Navigate Back                 │
```

### Hangfire Jobs Timeline

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      NEW HIRE REQUEST SUBMITTED                          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
         ┌─────────────────────────┼─────────────────────────┐
         ▼                         ▼                         ▼
┌─────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│ Step 4: Email   │    │ Pre-Employment Job  │    │ Viewpoint Verify    │
│ Scheduling      │    │ (3 days before)     │    │ (On FirstDay)       │
└─────────────────┘    └─────────────────────┘    └─────────────────────┘
         │                         │                         │
         ▼                         ▼                         ▼
┌─────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│ Hangfire.       │    │ ProcessNewHire      │    │ VerifyNewHire       │
│ Schedule/Enqueue│    │ PreEmploymentAsync  │    │ EmployeeInViewpoint │
└─────────────────┘    └─────────────────────┘    └─────────────────────┘
         │                         │                         │
         ▼                         ▼                         ▼
┌─────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│ SendScheduled   │    │ Update Viewpoint    │    │ Check Employee      │
│ NewHireEmailAsync│   │ → Set Status        │    │ → Set Status        │
└─────────────────┘    └─────────────────────┘    └─────────────────────┘
```

### Viewpoint Integration Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│ ProcessNewHirePreEmploymentAsync (3 days BEFORE FirstDayEmployment)     │
│   └─► UpdateEmployeeForNewHireInViewPointAsync()                        │
│       - Search employee in Viewpoint                                     │
│       - Update custom fields (udWorkEmail, udNetworkUserID, etc.)       │
│       - Queue update action in Viewpoint                                 │
│       - Poll for verification (up to 10 attempts)                        │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ VerifyNewHireEmployeeInViewpointAsync (ON FirstDayEmployment)           │
│   └─► SearchEmployeeInNewHireWithAPIAsync()                             │
│       - Verify employee exists in Viewpoint                              │
│       - Update EmployeeId with HRRef from Viewpoint                      │
│       - Set status to Completed or Failed                                │
│       - Retry up to 2 times if not found                                 │
└─────────────────────────────────────────────────────────────────────────┘
```

### Verification Flow Detail

```
VerifyNewHireEmployeeInViewpointAsync(hrRequestDetailId, attemptNumber, submitterEmail)
         │
         ▼
Step 1: Get HRRequestDetail with NewHireDetails
         │
         ▼
Step 2: Set status to "Processing"
         │
         ▼
Step 3: Search for employee in Viewpoint
         └─► SearchEmployeeInNewHireWithAPIAsync(CompanyCode, PRDept, LastName, HireDate)
                   │
         ┌─────────┼─────────────────────────────────────┐
         ▼         ▼                                     ▼
    1 FOUND    MULTIPLE FOUND                      NOT FOUND / API FAILED
         │         │                                     │
         ▼         ▼                                     ▼
    ✅ SUCCESS  ❌ FAILED                          Check attempt #
    - Update EmployeeId     - Set ViewpointErrorMessage    │
      with HRRef           - "Duplicate in Viewpoint"      ├─► attemptNumber < 2
    - Status = Completed                                   │   └─► Schedule retry in 2 hours
    - Send success notification                            │
                                                           └─► attemptNumber >= 2
                                                               └─► ❌ FAILED
                                                                   - Status = Failed
                                                                   - Send failure notification
```

---

## Daily Recurring Job

```
┌─────────────────────────────────────────────────────────────────────┐
│              DAILY RECURRING JOB (12:00 AM Midnight)                 │
│                ProcessNewHireEmailNotificationsAsync                 │
│    - Checks all pending new hires                                    │
│    - Schedules/Enqueues emails based on TriggerDate                  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Request Status Values

| ID | Status | Description |
|----|--------|-------------|
| 1 | Pending | Initial state after submission |
| 2 | Processing | Being processed by background job |
| 3 | Completed | Successfully verified in Viewpoint |
| 4 | Failed | Verification failed |
| 5 | Cancelled | Request was cancelled |
| 6 | Rejected | Request was rejected |

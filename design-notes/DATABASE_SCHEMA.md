# HR Employee Change Management - Database Schema

## Overview

This document defines the SQL Server database schema for the HR Employee Change Management system. The design supports parent-child request relationships, reference data caching, audit trails, and soft deletes throughout.

## Design Principles

- **Primary Keys**: Integer identity columns for performance
- **Soft Deletes**: All tables include `IsDeleted` flag
- **Audit Trail**: Standard audit fields on all tables
- **Parent-Child Requests**: Individual employee requests with optional parent grouping
- **Reference Data**: Local caching of Viewpoint lookup data
- **Extensibility**: Designed to support future request types

## Core Tables

### 1. HRRequests (Parent Request Table)

```sql
CREATE TABLE HRRequests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SubmittedBy INT NOT NULL, -- HRREF / Employee ID
    SubmittedDate DATETIME2 NULL,
    SubmitterEmail NVARCHAR(255) NULL, -- Email of the user who submitted the request (captured at submission time)
    Notes VARCHAR(MAX) NULL,

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,

    INDEX IX_HRRequests_SubmittedBy (SubmittedBy),
    INDEX IX_HRRequests_SubmittedDate (SubmittedDate),
    INDEX IX_HRRequests_IsDeleted (IsDeleted)
);
```

## Reference Data Tables (Request Management)

### 2. RequestTypes

```sql
CREATE TABLE RequestTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RequestTypeName VARCHAR(50) NOT NULL UNIQUE, -- 'Promotion', 'Layoff', 'Termination', 'ReturnToWork', 'NewHire'
    RequestTypeDescription VARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_RequestTypes_RequestTypeName (RequestTypeName),
    INDEX IX_RequestTypes_IsActive (IsActive),
    INDEX IX_RequestTypes_IsDeleted (IsDeleted)
);
```

### 3. RequestStatuses

```sql
CREATE TABLE RequestStatuses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RequestStatusName VARCHAR(50) NOT NULL UNIQUE, -- 'Pending', 'Processing', 'Completed', 'Failed', 'Cancelled', Draft
    RequestStatusDescription VARCHAR(255) NULL,
    RequestDisplayStatusName VARCHAR(50) NULL,   -- 'Pending/Processing/Completed/ Failed / Cancelled' = Submitted, 'Draft' = 'Draft'
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_RequestStatuses_RequestStatusName (RequestStatusName),
    INDEX IX_RequestStatuses_IsActive (IsActive),
    INDEX IX_RequestStatuses_IsDeleted (IsDeleted)
);
```

### 4. HRRequestDetails (Individual Employee Requests)

```sql
CREATE TABLE HRRequestDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ParentRequestId INT NOT NULL,
    
    -- Request Type and Status (foreign keys to lookup tables)
    RequestTypeId INT NOT NULL,
    RequestStatusId INT NOT NULL DEFAULT 1, -- Default to 'Pending'
    
    -- Employee Information (from Viewpoint)
    EmployeeId INT NOT NULL, -- Viewpoint Employee ID
    EmployeeNetworkId VARCHAR(255) NULL, -- AD Network ID
    EmployeePositionCode VARCHAR(10) NULL,
    EmployeeCompanyCode INT NULL, --Company Code
    EmployeeDeparmentCode INT NULL, -- Department Code
    
    -- Request Specific Details
    EffectiveDate DATE NULL,
    ProcessingNotes VARCHAR(MAX) NULL,
    
    -- Viewpoint Integration
    ViewpointProcessed BIT NOT NULL DEFAULT 0,
    ViewpointProcessedDate DATETIME2 NULL,
    ViewpointErrorMessage VARCHAR(MAX) NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (ParentRequestId) REFERENCES HRRequests(Id),
    FOREIGN KEY (RequestTypeId) REFERENCES RequestTypes(Id),
    FOREIGN KEY (RequestStatusId) REFERENCES RequestStatuses(Id),
    INDEX IX_HRRequestDetails_ParentRequestId (ParentRequestId),
    INDEX IX_HRRequestDetails_RequestTypeId (RequestTypeId),
    INDEX IX_HRRequestDetails_RequestStatusId (RequestStatusId),
    INDEX IX_HRRequestDetails_EmployeeId (EmployeeId),
    INDEX IX_HRRequestDetails_EmployeeNetworkId (EmployeeNetworkId),
    INDEX IX_HRRequestDetails_EffectiveDate (EffectiveDate),
    INDEX IX_HRRequestDetails_IsDeleted (IsDeleted)
);
```

## Request Type Specific Tables

### 5. PromotionRequestDetails

```sql
CREATE TABLE PromotionRequestDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RequestDetailId INT NOT NULL,
    
    -- Current Position
    CurrentPayrollCompanyCode INT NULL,
    CurrentPayrollGroupCode INT NULL,
    CurrentPayrollDeptCode INT NULL,
    CurrentPositionCode VARCHAR(10) NULL,
    CurrentSupervisorId INT NULL,
    CurrentPhysicalLocationCode INT NULL,
    CurrentStatus VARCHAR(10) NULL,
    CurrentSalaryCode INT NULL, -- 'Refer to EmployeeSalaryTypes
    
    -- New Position
    NewPayrollCompanyCode INT NOT NULL,
    NewPayrollGroupCode INT NOT NULL,
    NewPayrollDeptCode INT NOT NULL,
    NewPositionCode VARCHAR(10) NOT NULL,
    NewSupervisorId INT NULL,
    NewPhysicalLocationCode INT NOT NULL,
    NewStatus VARCHAR(10) NOT NULL,
    NewSalaryCode INT NULL, -- 'Refer to EmployeeSalaryTypes

    -- Work Email
    CurrentWorkEmail NVARCHAR(255) NULL,
    NewWorkEmail NVARCHAR(255) NULL,

    -- Building Access
    UseExistingKeyFob BIT NULL,

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (RequestDetailId) REFERENCES HRRequestDetails(Id),
    INDEX IX_PromotionRequestDetails_RequestDetailId (RequestDetailId),
    INDEX IX_PromotionRequestDetails_IsDeleted (IsDeleted)
);
```

### 6. CreditCardDetails

```sql
CREATE TABLE CreditCardDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NewHireRequestId INT NOT NULL,

    -- Credit Card Types
    KwikTripCard BIT NULL,

    -- New Hire Form Specific Fields (Option A expansion)
    CompanyExpenseCard BIT NULL,    -- Parent: "Does employee need Company Expense Card?"
    CreditExpenseType VARCHAR(50) NULL,
    WeeklyLimit DECIMAL(10,2) NULL, -- "Company Credit Card Weekly Limit"

    -- Shared Fields
    FuelCardlockAccess BIT NULL,            -- "Fuel Cardlock Access"
    FuelCardlockAddress VARCHAR(500) NULL,  -- "Cardlock - ship address"
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (NewHireRequestId) REFERENCES NewHireRequestDetails(Id),
    INDEX IX_CreditCardDetails_RequestDetailId (RequestDetailId),
    INDEX IX_CreditCardDetails_CompanyExpenseCard (CompanyExpenseCard),
    INDEX IX_CreditCardDetails_IsDeleted (IsDeleted)
);
```

### 7. VehicleDetails

```sql
CREATE TABLE VehicleDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NewHireRequestId INT NOT NULL,
    
    IsApprovedToOperate BIT NULL,
    LicenseClass VARCHAR(10) NULL, -- 'LicenseClass
    DrugAndAlcoholProfile VARCHAR(30) NULL, -- 'DOT Drug Pool', 'WI Prevailing Wage Pool', 'No Testing'
    NeedCompanyCar BIT NULL,
    IsApplicationPart2Complete BIT NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (NewHireRequestId) REFERENCES NewHireRequestDetails(Id),
    INDEX IX_VehicleDetails_LicenseClass (LicenseClass),
    INDEX IX_VehicleDetails_RequestDetailId (RequestDetailId),
    INDEX IX_VehicleDetails_IsDeleted (IsDeleted)
);
```

### 8. ITDetails

```sql
CREATE TABLE ITDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NewHireRequestId INT NOT NULL,
    
    -- Email and Delivery
    EmailRequired BIT NULL,                -- "Does New Hire Require an Email Address"
    AlternateDeliveryLocation VARCHAR(500) NULL,         -- Alternative delivery location override
    
    -- Microsoft Office License Requirements (New Hire Form expansion)
    MSOfficeLicenseE5 BIT NULL,      -- "Will need an email address" checkbox
    MSOfficeLicenseF3 BIT NULL,      -- "Will need an E5 M$ license" checkbox
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (NewHireRequestId) REFERENCES NewHireRequestDetails(Id),
    INDEX IX_ITDetails_EmailRequired (EmailRequired),
    INDEX IX_ITDetails_IsDeleted (IsDeleted)
);
```

### 9. Applications

```sql
CREATE TABLE Applications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LocationType VARCHAR(50) NOT NULL,  
    [Name] VARCHAR(255) NOT NULL,
    [Description] VARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_Applications_Name ([Name]),
    INDEX IX_Applications_IsActive (IsActive),
    INDEX IX_Applications_IsDeleted (IsDeleted)
);
```

### 10. ApplicationRequests

```sql
CREATE TABLE ApplicationRequests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NewHireRequestId INT NOT NULL,
    ApplicationId INT NOT NULL,
    AccessNotes VARCHAR(500) NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (NewHireRequestId) REFERENCES NewHireRequestDetails(Id),
    FOREIGN KEY (ApplicationId) REFERENCES Applications(Id),
    INDEX IX_ApplicationRequests_ApplicationId (ApplicationId),
    INDEX IX_ApplicationRequests_IsDeleted (IsDeleted)
);
```

### 11. FolderRequests

```sql
CREATE TABLE FolderRequests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NewHireRequestId INT NOT NULL,
    
    FolderType VARCHAR(100) NOT NULL, -- 'Shared Folder', 'SharePoint Site', 'Mailbox', 'Distribution List', 'OneDrive Access'
    FolderName VARCHAR(500) NOT NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (NewHireRequestId) REFERENCES NewHireRequestDetails(Id),
    INDEX IX_FolderRequests_ITDetailId (ITDetailId),
    INDEX IX_FolderRequests_IsDeleted (IsDeleted)
);
```

### 12. ITPhoneRequirements

```sql
CREATE TABLE ITPhoneRequirements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NewHireRequestId INT NOT NULL,
    
    -- Phone Requirements
    DeskPhone BIT NULL,
    CompanyCellphone BIT NULL,
    BYODCellphone BIT NULL,
    WorkPhoneNumber VARCHAR(50) NULL,
    WorkExtension VARCHAR(50) NULL,
    WorkCell VARCHAR(50) NULL,
    ReusingExistingPhone BIT NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (NewHireRequestId) REFERENCES NewHireRequestDetails(Id),
    INDEX IX_ITPhoneRequirements_IsDeleted (IsDeleted)
);
```

### 13. ITTabletProfiles

```sql
CREATE TABLE ITTabletProfiles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NewHireRequestId INT NOT NULL,
    
    -- Tablet Profile Information
    TabletProfileId INT NOT NULL,
    TabletProfileName VARCHAR(255) NULL,
    RolesRequiredForNewHire VARCHAR(1000) NOT NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NOT NULL,
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (NewHireRequestId) REFERENCES NewHireRequestDetails(Id),
    FOREIGN KEY (TabletProfileID) REFERENCES TabletProfiles(Id),
    INDEX IX_ITTabletProfiles_TabletProfileID (TabletProfileId),
    INDEX IX_ITTabletProfiles_IsDeleted (IsDeleted)
);
```

### 14. ITComputerRequirements

```sql
CREATE TABLE ITComputerRequirements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NewHireRequestId INT NOT NULL,
    
    -- Computer Requirements Information
    ComputerRequirementsId INT NOT NULL,
    ComputerRequirementsDescription VARCHAR(255) NULL,
    IsChild BIT NULL DEFAULT 0, 
    ParentId INT NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (NewHireRequestId) REFERENCES NewHireRequestDetails(Id),
    FOREIGN KEY (ComputerRequirementsId) REFERENCES ComputerRequirements(Id),
    INDEX IX_ITComputerRequirements_IsDeleted (IsDeleted)
);
```

### 15. LayoffRequestDetails

```sql
CREATE TABLE LayoffRequestDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RequestDetailId INT NOT NULL,
    
    LastDayWorked DATE NOT NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (RequestDetailId) REFERENCES HRRequestDetails(Id),
    INDEX IX_LayoffRequestDetails_RequestDetailId (RequestDetailId),
    INDEX IX_LayoffRequestDetails_LastDayWorked (LastDayWorked),
    INDEX IX_LayoffRequestDetails_IsDeleted (IsDeleted)
);
```

### 16. TerminationRequestDetails

```sql
CREATE TABLE TerminationRequestDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RequestDetailId INT NOT NULL,
    
    ReasonCode VARCHAR(20) NOT NULL, -- 'performance', 'misconduct', 'attendance', 'restructuring', 'violation', 'resignation'
    
    -- Communication Forwarding
    ForwardEmail VARCHAR(255) NULL,
    ForwardDeskPhone VARCHAR(50) NULL,
    ForwardCellPhone VARCHAR(50) NULL,
    AutoReply VARCHAR(MAX) NULL,
    GiveOneDriveAccessTo VARCHAR(255) NULL,

    -- Kwik Trip Card
    WithKwikTripCard BIT NOT NULL DEFAULT 0,
    KwikCard4DigitNo VARCHAR(4) NULL,

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (RequestDetailId) REFERENCES HRRequestDetails(Id),
    INDEX IX_TerminationRequestDetails_RequestDetailId (RequestDetailId),
    INDEX IX_TerminationRequestDetails_Reason (ReasonCode),
    INDEX IX_TerminationRequestDetails_IsDeleted (IsDeleted)
);
```

### 17. ReturnToWorkRequestDetails

```sql
CREATE TABLE ReturnToWorkRequestDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RequestDetailId INT NOT NULL,
    
    -- Additional fields specific to return to work can be added here
    -- Currently using base fields from HRRequestDetails
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (RequestDetailId) REFERENCES HRRequestDetails(Id),
    INDEX IX_ReturnToWorkRequestDetails_RequestDetailId (RequestDetailId),
    INDEX IX_ReturnToWorkRequestDetails_IsDeleted (IsDeleted)
);
```

### 18. NewHireRequestDetails

```sql
CREATE TABLE NewHireRequestDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RequestDetailId INT NOT NULL,
    
    -- Personal Information
    EmployeeId INT NULL,
    FirstName VARCHAR(30) NULL, -- 'required
    LastName VARCHAR(30) NULL, -- 'required
    Suffix VARCHAR(10) NULL,
    PreferredFirstName VARCHAR(50) NULL,
    FirstDayEmployment DATE NULL, -- 'required
    ReferredBy VARCHAR(100) NULL,
    Rehire BIT NULL DEFAULT 0, -- 'required
    
    -- Position Information
    CompanyCode INT NULL, -- 'required
    LocationCode INT NULL, -- 'required
    EmploymentStatus VARCHAR(20) NULL, -- 'required
    IsUnion BIT NULL,
    CraftCode VARCHAR(10) NULL,
    IsApprentice BIT NULL,
    AppPercentage VARCHAR(20) NULL, 
    IsUnionWage BIT NULL,
    SalaryCode INT NULL, -- 'Refer to EmployeeSalaryTypes
    PositionCode VARCHAR(10) NULL, -- 'required
    PayrollDeptCode INT NULL, -- 'required
    SupervisorId INT NULL, -- 'required
    NetworkId VARCHAR(255) NULL,
    WorkEmail VARCHAR(255) NULL,
    AdPassword VARCHAR(1000) NULL,
    Notes NVARCHAR(MAX) NULL,
    UseExistingKeyFob BIT NULL,

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (RequestDetailId) REFERENCES HRRequestDetails(Id),
    INDEX IX_NewHireRequestDetails_RequestDetailId (RequestDetailId),
    INDEX IX_NewHireRequestDetails_LastName (LastName),
    INDEX IX_NewHireRequestDetails_FirstDayEmployment (FirstDayEmployment),
    INDEX IX_NewHireRequestDetails_CompanyCode (CompanyCode),
    INDEX IX_NewHireRequestDetails_PayrollDeptCode (PayrollDeptCode),
    INDEX IX_NewHireRequestDetails_IsDeleted (IsDeleted)
);
```

## Reference Data Tables

### 19. Companies

```sql
CREATE TABLE Companies (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyCode INT NOT NULL,
    CompanyName VARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Viewpoint Sync
    ViewpointSyncDate DATETIME2 NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_Companies_IsActive (IsActive),
    INDEX IX_Companies_IsDeleted (IsDeleted),
    UNIQUE (CompanyCode) -- Prevent duplicate access records
);
```

### 20. PayrollGroups

```sql
CREATE TABLE PayrollGroups (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyCode INT NOT NULL,
    GroupCode INT NOT NULL,
    GroupName VARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Viewpoint Sync
    ViewpointSyncDate DATETIME2 NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
 
    INDEX IX_PayrollGroups_GroupCode (GroupCode),
    INDEX IX_PayrollGroups_IsActive (IsActive),
    INDEX IX_PayrollGroups_IsDeleted (IsDeleted),
    UNIQUE (CompanyCode, GroupCode) -- Prevent duplicate access records
);
```

### 21. PayrollDepartments

```sql
CREATE TABLE PayrollDepartments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyCode INT NOT NULL,
    DeptCode INT NOT NULL,
    DeptName VARCHAR(50) NOT NULL,
    EmailDomain VARCHAR(500) NULL,
    HRPartner INT NULL,
    HRRep INT NULL,
    SafetyRep INT NULL,
    PayrollRep INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Viewpoint Sync
    ViewpointSyncDate DATETIME2 NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
       
    INDEX IX_PayrollDepartments_DeptCode (DeptCode),
    INDEX IX_PayrollDepartments_IsActive (IsActive),
    INDEX IX_PayrollDepartments_IsDeleted (IsDeleted),
    UNIQUE (CompanyCode, DeptCode) -- Prevent duplicate access records
);
```

### 22. PayrollDepartmentShortNames

```sql
CREATE TABLE PayrollDepartmentShortNames (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyCode INT NOT NULL,
    DeptCode INT NOT NULL,
    DeptShortName VARCHAR(25) NOT NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_PayrollDepartmentShortNames_CompanyCode (CompanyCode),
    INDEX IX_PayrollDepartmentShortNames_DeptCode (DeptCode),
    INDEX IX_PayrollDepartmentShortNames_IsDeleted (IsDeleted),
    UNIQUE (CompanyCode, DeptCode) -- Prevent duplicate records
);
```

### 23. Positions

```sql
CREATE TABLE Positions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyCode INT NOT NULL,
    PositionCode VARCHAR(10) NOT NULL,
    PositionName VARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Viewpoint Sync
    ViewpointSyncDate DATETIME2 NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
  
    INDEX IX_Positions_PositionCode (PositionCode),
    INDEX IX_Positions_IsActive (IsActive),
    INDEX IX_Positions_IsDeleted (IsDeleted),
    UNIQUE (CompanyCode, PositionCode) -- Prevent duplicate access records
);
```

### 24. PhysicalLocations

```sql
CREATE TABLE PhysicalLocations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LocationCode INT NOT NULL,
    LocationName VARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Viewpoint Sync
    ViewpointSyncDate DATETIME2 NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_PhysicalLocations_IsActive (IsActive),
    INDEX IX_PhysicalLocations_IsDeleted (IsDeleted),
    UNIQUE (LocationCode) -- Prevent duplicate access records
);
```
### 25. FunctionalDepartments

```sql
CREATE TABLE FunctionalDepartments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FunctionalDeptCode INT NOT NULL,
    FunctionalDeptName VARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Viewpoint Sync
    ViewpointSyncDate DATETIME2 NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_FunctionalDepartment_IsActive (IsActive),
    INDEX IX_FunctionalDepartment_IsDeleted (IsDeleted),
    UNIQUE (FunctionalDeptCode) -- Prevent duplicate access records
);
```

### 26. UnionCrafts

```sql
CREATE TABLE UnionCrafts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyCode INT NOT NULL,
    [CraftCode] VARCHAR(10) NOT NULL,
    [Description] VARCHAR(30) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,

    -- Viewpoint Sync
    ViewpointSyncDate DATETIME2 NULL,    
    
    FOREIGN KEY (CompanyCode) REFERENCES Companies(CompanyCode),
    INDEX IX_UnionCrafts_CraftCode(CraftCode),
    INDEX IX_UnionCrafts_IsActive (IsActive),
    INDEX IX_UnionCrafts_IsDeleted (IsDeleted),
    UNIQUE (CompanyCode, [CraftCode])
);
```

### 27. ComputerRequirements

```sql
CREATE TABLE ComputerRequirements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Description] VARCHAR(255) NOT NULL,
    IsChild BIT NULL DEFAULT 0, -- 0 Main , 1 Child
    ParentId INT NULL, -- What is the Id of the parent
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_ComputerRequirements_IsActive (IsActive),
    INDEX IX_ComputerRequirements_IsDeleted (IsDeleted)
);
```

### 28. TabletProfiles

```sql
CREATE TABLE TabletProfiles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LocationType VARCHAR(50) NOT NULL,  -- Values are 'Mathy/Energy', 'Pavement'
    ProfileName VARCHAR(255) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_TabletProfiles_IsActive (IsActive),
    INDEX IX_TabletProfiles_IsDeleted (IsDeleted),
    INDEX IX_TabletProfiles_LocationType (LocationType),
    INDEX IX_TabletProfiles_ProfileName (ProfileName)

);
```

## Notification and Email Tables

### 29. EmailTemplates

```sql
CREATE TABLE EmailTemplates (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TemplateName VARCHAR(255) NOT NULL, -- 'NEWHIRE-Confirmation', 'NEWHIRE-TaskEmail-01', ..., 'NEWHIRE-TaskEmail-07', 'NEWHIRE-ReminderEmail', ...
    RequestType VARCHAR(50) NOT NULL, -- 'NEWHIRE', 'LAY-OFF','TERMINATION'
    EmailType VARCHAR(100) NOT NULL, -- 'NOTIFICATION','ERROR','WARNING'
    Recipients VARCHAR(1000) NOT NULL, -- 'ITDL, HRDL, EMPLOYEE' 
    Subject VARCHAR(500) NOT NULL,  -- 'see NOTES' 
    Body VARCHAR(MAX) NOT NULL, -- 'see CONTENT tab'
    TriggerType VARCHAR(10) NOT NULL, -- 'Immediate', 'Scheduled'
    SubmissionFreq INT DEFAULT 0, --- -3 days before, 7 days after, 0 on that day
    ContentStyling VARCHAR(25) NULL, -- TableColumn, TableRow, Text
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_EmailTemplates_RequestType (RequestType),
    INDEX IX_EmailTemplates_EmailType (EmailType),
    INDEX IX_EmailTemplates_IsActive (IsActive),
    INDEX IX_EmailTemplates_IsDeleted (IsDeleted)
);
```

### 30. NotificationQueue

```sql
CREATE TABLE NotificationQueue (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RequestId INT NOT NULL,
    TemplateId INT NOT NULL,
    
    ToEmail VARCHAR(255) NOT NULL,
    CcEmail VARCHAR(500) NULL,
    [Subject] VARCHAR(500) NOT NULL,
    Body VARCHAR(MAX) NOT NULL,
    
    [Status] VARCHAR(50) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Sent', 'Failed'
    AttemptCount INT NOT NULL DEFAULT 0,
    LastAttempt DATETIME2 NULL,
    ErrorMessage VARCHAR(MAX) NULL,
    SentDate DATETIME2 NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (RequestId) REFERENCES HRRequests(Id),
    FOREIGN KEY (TemplateId) REFERENCES EmailTemplates(Id),
    INDEX IX_NotificationQueue_RequestId (RequestId),
    INDEX IX_NotificationQueue_Status (Status),
    INDEX IX_NotificationQueue_CreatedDate (CreatedDate),
    INDEX IX_NotificationQueue_IsDeleted (IsDeleted)
);
```

## User and Authorization Tables

### 31. UserCompanyAccess

```sql
CREATE TABLE UserCompanyAccess (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId VARCHAR(255) NOT NULL, -- Entra ID user identifier
    UserName VARCHAR(255) NOT NULL,
    CompanyCode INT NOT NULL,
    
    -- Access Control
    CanSubmitRequests BIT NOT NULL DEFAULT 1,
    
    -- Sync Information
    Source VARCHAR(50) NOT NULL, -- 'Viewpoint', 'EntraGroups'
    LastSyncDate DATETIME2 NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    --FOREIGN KEY (CompanyCode) REFERENCES Companies(CompanyCode),
    INDEX IX_UserCompanyAccess_UserId (UserId),
    INDEX IX_UserCompanyAccess_CompanyCode (CompanyCode),
    INDEX IX_UserCompanyAccess_IsDeleted (IsDeleted),
    
    UNIQUE (UserId, CompanyCode) -- Prevent duplicate access records
);
```

### 32. TerminationReasons

```sql
CREATE TABLE TerminationReasons (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyCode INT NOT NULL,
    ReasonCode VARCHAR(20) NOT NULL,
    ReasonDescription VARCHAR(255) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
      -- Viewpoint Sync
    ViewpointSyncDate DATETIME2 NULL,


    FOREIGN KEY (CompanyCode) REFERENCES Companies(CompanyCode),
    INDEX IX_TerminationReasons_CompanyCode (CompanyCode),
    INDEX IX_TerminationReasons_IsActive (IsActive),
    INDEX IX_TerminationReasons_IsDeleted (IsDeleted)
);

-- Create composite index to optimize queries on CompanyCode and ReasonCode (allows duplicates)
CREATE NONCLUSTERED INDEX [IX_TerminationReasons_ReasonCode] ON [dbo].[TerminationReasons]
(
    [CompanyCode] ASC,
    [ReasonCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
```

### 33. Employees

```sql
CREATE TABLE Employees (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyCode INT NOT NULL,
    EmployeeNumber INT NOT NULL, 
    FirstName VARCHAR(30) NOT NULL,
    MiddleName VARCHAR(15) NULL,
    LastName VARCHAR(30) NOT NULL,
    PersonalEmail VARCHAR(255) NULL,
    WorkEmail VARCHAR(255) NULL,
    NetworkId VARCHAR(255) NULL, -- AD Network ID
    PayrollCompanyCode INT NULL,
    PayrollGroupCode INT NULL,
    PayrollDeptCode INT NULL,
    PositionCode VARCHAR(10) NULL,
    SupervisorId INT NULL,
    FunctionalDeptCode INT NULL,
    PhysicalLocationCode INT NULL,
    TerminationDate DATETIME2 NULL,
    TerminationReasonCode VARCHAR(20) NULL,
    ReturnToWorkDate DATETIME2 NULL,
    EmploymentStatus VARCHAR(20) NULL,
    SalaryCode INT NOT NULL,
    WorkPhoneNumber VARCHAR(50) NULL,
    WorkExtension VARCHAR(50) NULL,
    WorkCell VARCHAR(50) NULL,    
    
    -- Viewpoint Sync
    ViewpointSyncDate DATETIME2 NULL,

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,

    -- Indexes
    INDEX IX_Employees_CompanyCode (CompanyCode),
    INDEX IX_Employees_EmployeeNumber (EmployeeNumber),
    INDEX IX_Employees_FirstName (FirstName),
    INDEX IX_Employees_LastName (LastName),
    INDEX IX_Employees_NetworkId (NetworkId),
    INDEX IX_Employees_EmploymentStatus (EmploymentStatus),
    INDEX IX_Employees_IsDeleted (IsDeleted),
    INDEX IX_Employees_SupervisorId (SupervisorId),
    UNIQUE (CompanyCode, EmployeeNumber) -- Prevent duplicate access records
);
```


### 34. EmploymentStatuses

```sql
CREATE TABLE EmploymentStatuses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyCode INT NOT NULL,
    [Status] VARCHAR(20) NOT NULL,
    [Description] VARCHAR(255) NOT NULL,
    [Notes] VARCHAR(20) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,

    -- Viewpoint Sync
    ViewpointSyncDate DATETIME2 NULL,    
    
    FOREIGN KEY (CompanyCode) REFERENCES Companies(CompanyCode),
    INDEX IX_EmploymentStatuses_CompanyCode (CompanyCode),
    INDEX IX_EmploymentStatuses_IsActive (IsActive),
    INDEX IX_EmploymentStatuses_IsDeleted (IsDeleted),
    UNIQUE (CompanyCode,[Status]) 
);
```


### 35. EmployeeSalaryTypes

```sql
CREATE TABLE EmployeeSalaryTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyCode INT NOT NULL,
    SalaryCode INT NOT NULL,
    [Description] VARCHAR(255) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,

    -- Viewpoint Sync
    ViewpointSyncDate DATETIME2 NULL,    
    
    FOREIGN KEY (CompanyCode) REFERENCES Companies(CompanyCode),
    INDEX IX_EmployeeSalaryTypes_CompanyCode (CompanyCode),
    INDEX IX_EmployeeSalaryTypes_IsActive (IsActive),
    INDEX IX_EmployeeSalaryTypes_IsDeleted (IsDeleted),
    UNIQUE (CompanyCode, SalaryCode) 
);
```

### 36. ApprenticePercentages

```sql
CREATE TABLE ApprenticePercentages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    appPercentage VARCHAR(20) NOT NULL,
    appDescription VARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_ApprenticePercentages_IsActive (IsActive),
    INDEX IX_ApprenticePercentages_IsDeleted (IsDeleted)
);
```

### 37. BuildingAccessRequirements

```sql
CREATE TABLE BuildingAccessRequirements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LocationType VARCHAR(50) NOT NULL,  -- Values are 'Mathy/Energy', 'Pavement'
    [Description] VARCHAR(100), 
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    INDEX IX_BuildingAccessRequirements_IsActive (IsActive),
    INDEX IX_BuildingAccessRequirements_IsDeleted (IsDeleted)
);
```

### 38. NewHireBuildingAccessRequirements

```sql
CREATE TABLE NewHireBuildingAccessRequirements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NewHireRequestId INT NOT NULL,
    AccessId INT NOT NULL,
    AccessDescription VARCHAR(100) NOT NULL, 

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (NewHireRequestId) REFERENCES NewHireRequestDetails(Id),
    INDEX IX_BuildingAccessRequirements_IsActive (IsActive),
    INDEX IX_BuildingAccessRequirements_IsDeleted (IsDeleted)
);
```

### 39. CompanyTypeLocation

```sql
CREATE TABLE CompanyTypeLocation (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyCode INT NOT NULL,   -- 19,65,78,88
    LocationType VARCHAR(50) NOT NULL,  -- Mathy, Pavement, TNW
    IsUnion BIT NOT NULL DEFAULT 0,  -- 0 No, 1 Yes
    Domain VARCHAR(200) NULL, -- ex. mathy.com, pavementmaterials.com, internal.tnw.com 


    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0)
);
```


### 40. EmployeeLicenseClasses

```sql
CREATE TABLE EmployeeLicenseClasses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    LicenseClass VARCHAR(10) NOT NULL,   
    [Description] VARCHAR(70) NULL,  
    IsUnion BIT NOT NULL DEFAULT 0,  -- 0 No, 1 Yes

    -- Viewpoint Sync
    ViewpointSyncDate DATETIME2 NULL,  

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0)
);

```


### 40. CompanyDL

```sql
CREATE TABLE CompanyDL (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyCode INT NOT NULL,   -- 19
    DeptCode INT NOT NULL, -- 1900
    SiteDL VARCHAR(255) NULL,  -- 'DL-NewHireNote-Mathy-Corp-Lab-Shop@corpmts.com'
    SecurityDL VARCHAR(255) NULL, -- ''
    CreditCardDL VARCHAR(255)  NULL, -- 'DL-NewHireCashMgmt-Corp@corpmts.com'
    FleetDL VARCHAR(255) NULL, -- 'DL-NewHireTransportationMgmt-Corp@corpmts.com'
    ComplianceDL VARCHAR(255) NULL, -- 'DL-NewHireCompliance-Corp@corpmts.com'
    SafetyDL VARCHAR(255) NULL, -- 'DL-Construction-Safety@corpmts.com;DL-Corporate-Safety@corpmts.com'
    FuelFobDL VARCHAR(255) NULL, 
    HRDL VARCHAR(255)  NULL,  -- 'DL-NewHireNote-Milestone@corpmts.com'
    ITDL VARCHAR(255) NULL, -- 
    PAYROLLDL VARCHAR(255) NULL,

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);

```

### 41. EmailContentMappers

```sql
CREATE TABLE EmailContentMappers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ContentCode VARCHAR(255) NULL,   -- EMPLOYEE-NAME, START-DATE
    Contentfield VARCHAR(255) NULL,   -- EMPLOYEE-NAME --> fullname
    ContentPartType VARCHAR(50) NULL, -- Body, Subject, Recipients
    ContentLabel VARCHAR(255) NULL, -- Start Date, Employee Name
    ContentSource VARCHAR(50) NULL -- NEWHIRE, APPLICATION, COMPANYDL 
);
```

### 42. ServiceDeskSyncData

```sql
CREATE TABLE ServiceDeskSyncData (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NewHireRequestId INT NOT NULL,
    ServiceDeskID VARCHAR(100) NOT NULL, -- 179024000000991083
    HasBuildingAccess BIT NULL DEFAULT 0, 
    HasPhoneRequirements BIT NULL DEFAULT 0,
    HasComputerRequirements BIT NULL DEFAULT 0,
    HasTabletProfiles BIT NULL DEFAULT 0,
    HasITApplications BIT NULL DEFAULT 0,
    HasSoftwareAccessReq BIT NULL DEFAULT 0,

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    FOREIGN KEY (NewHireRequestId) REFERENCES NewHireRequestDetails(Id),
    INDEX IX_ServiceDeskSyncData_ServiceDeskID (ServiceDeskID)
);
```


### 43. PTCreditCardDetails

```sql
CREATE TABLE PTCreditCardDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PTRequestDetailId INT NOT NULL,
    
    -- Credit Card Types
    KwikTripCard BIT NULL,
    
    -- New Hire Form Specific Fields (Option A expansion)
    CompanyExpenseCard BIT NULL,    -- Parent: "Does employee need Company Expense Card?"
    FuelOnlyCard BIT NULL,          -- Child: "Fuel Only" option
    EEExpenseCard BIT NULL,         -- Child: "EE Expense Credit Card" option
    WeeklyLimit DECIMAL(10,2) NULL, -- "Company Credit Card Weekly Limit"
    
    -- Shared Fields
    FuelCardlockAccess BIT NULL,            -- "Fuel Cardlock Access"
    FuelCardlockAddress VARCHAR(500) NULL,  -- "Cardlock - ship address"
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (PTRequestDetailId) REFERENCES PromotionRequestDetails(Id),
    INDEX IX_PTCreditCardDetails_PTRequestDetailId (PTRequestDetailId),
    INDEX IX_PTCreditCardDetails_CompanyExpenseCard (CompanyExpenseCard),
    INDEX IX_PTCreditCardDetails_IsDeleted (IsDeleted)
);
```

### 44. PTVehicleDetails

```sql
CREATE TABLE PTVehicleDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PTRequestDetailId INT NOT NULL,
    
    IsApprovedToOperate BIT NULL,
    LicenseClass VARCHAR(10) NULL, -- 'LicenseClass
    DrugAndAlcoholProfile VARCHAR(30) NULL, -- 'DOT Drug Pool', 'WI Prevailing Wage Pool', 'No Testing'
    NeedCompanyCar BIT NULL,
    IsApplicationPart2Complete BIT NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (PTRequestDetailId) REFERENCES PromotionRequestDetails(Id),
    INDEX IX_PTVehicleDetails_LicenseClass (LicenseClass),
    INDEX IX_PTVehicleDetails_PTRequestDetailId (PTRequestDetailId),
    INDEX IX_PTVehicleDetails_IsDeleted (IsDeleted)
);
```

### 45. ITDetails

```sql
CREATE TABLE PTITDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PTRequestDetailId INT NOT NULL,
    
    -- Email and Delivery
    EmailRequired BIT NULL,                -- "Does New Hire Require an Email Address"
    AlternateDeliveryLocation VARCHAR(500) NULL,         -- Alternative delivery location override
    
    -- Microsoft Office License Requirements (New Hire Form expansion)
    MSOfficeLicenseE5 BIT NULL,      -- "Will need an email address" checkbox
    MSOfficeLicenseF3 BIT NULL,      -- "Will need an E5 M$ license" checkbox
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (PTRequestDetailId) REFERENCES PromotionRequestDetails(Id),
    INDEX IX_PTITDetails_PTRequestDetailId (PTRequestDetailId),
    INDEX IX_PTITDetails_EmailRequired (EmailRequired),
    INDEX IX_PTITDetails_IsDeleted (IsDeleted)
);
```

### 46. PTApplicationRequests

```sql
CREATE TABLE PTApplicationRequests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PTRequestDetailId INT NOT NULL,
    ApplicationId INT NOT NULL,
    AccessNotes VARCHAR(500) NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (PTRequestDetailId) REFERENCES PromotionRequestDetails(Id),
    FOREIGN KEY (ApplicationId) REFERENCES Applications(Id),
    INDEX IX_PTApplicationRequests_ApplicationId (ApplicationId),
    INDEX IX_PTApplicationRequests_IsDeleted (IsDeleted)
);
```

### 47. PTFolderRequests

```sql
CREATE TABLE PTFolderRequests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PTRequestDetailId INT NOT NULL,
    
    FolderType VARCHAR(100) NOT NULL, -- 'Shared Folder', 'SharePoint Site', 'Mailbox', 'Distribution List', 'OneDrive Access'
    FolderName VARCHAR(500) NOT NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (PTRequestDetailId) REFERENCES PromotionRequestDetails(Id),
    INDEX IX_PTFolderRequests_IsDeleted (IsDeleted)
);
```

### 48. PTITPhoneRequirements

```sql
CREATE TABLE PTITPhoneRequirements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PTRequestDetailId INT NOT NULL,
    
    -- Phone Requirements
    DeskPhone BIT NULL,
    CompanyCellphone BIT NULL,
    BYODCellphone BIT NULL,
    WorkPhoneNumber VARCHAR(50) NULL,
    WorkExtension VARCHAR(50) NULL,
    WorkCell VARCHAR(50) NULL,
    ReusingExistingPhone BIT NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (PTRequestDetailId) REFERENCES PromotionRequestDetails(Id),
    INDEX IX_PTITPhoneRequirements_IsDeleted (IsDeleted)
);
```

### 49. PTITTabletProfiles

```sql
CREATE TABLE PTITTabletProfiles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PTRequestDetailId INT NOT NULL,
    
    -- Tablet Profile Information
    TabletProfileId INT NOT NULL,
    TabletProfileName VARCHAR(255) NULL,
    RolesRequiredForNewHire VARCHAR(1000) NOT NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NOT NULL,
    ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (PTRequestDetailId) REFERENCES PromotionRequestDetails(Id),
    FOREIGN KEY (TabletProfileID) REFERENCES TabletProfiles(Id),
    INDEX IX_PTITTabletProfiles_TabletProfileID (TabletProfileId),
    INDEX IX_PTITTabletProfiles_IsDeleted (IsDeleted)
);
```

### 50. PTITComputerRequirements

```sql
CREATE TABLE PTITComputerRequirements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PTRequestDetailId INT NOT NULL,
    
    -- Computer Requirements Information
    ComputerRequirementsId INT NOT NULL,
    ComputerRequirementsDescription VARCHAR(255) NULL,
    IsChild BIT NULL DEFAULT 0, 
    ParentId INT NULL,
    
    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (PTRequestDetailId) REFERENCES PromotionRequestDetails(Id),
    FOREIGN KEY (ComputerRequirementsId) REFERENCES ComputerRequirements(Id),
    INDEX IX_PTITComputerRequirements_IsDeleted (IsDeleted)
);
```

### 51. PTBuildingAccessRequirements

```sql
CREATE TABLE NewHireBuildingAccessRequirements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PTRequestDetailId INT NOT NULL,
    AccessId INT NOT NULL,
    AccessDescription VARCHAR(100) NOT NULL, 

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (PTRequestDetailId) REFERENCES PromotionRequestDetails(Id),
    INDEX IX_PromotionRequestDetails_IsActive (IsActive),
    INDEX IX_PromotionRequestDetails_IsDeleted (IsDeleted)
);
```

### 52. PTServiceDeskSyncData

```sql
CREATE TABLE PTServiceDeskSyncData (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PTRequestDetailId INT NOT NULL,
    ServiceDeskID VARCHAR(100) NOT NULL, -- 179024000000991083
    HasBuildingAccess BIT NULL DEFAULT 0, 
    HasPhoneRequirements BIT NULL DEFAULT 0,
    HasComputerRequirements BIT NULL DEFAULT 0,
    HasTabletProfiles BIT NULL DEFAULT 0,
    HasITApplications BIT NULL DEFAULT 0,
    HasSoftwareAccessReq BIT NULL DEFAULT 0,

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    FOREIGN KEY (PTRequestDetailId) REFERENCES PromotionRequestDetails(RequestId),
    INDEX IX_PTServiceDeskSyncData_ServiceDeskID (ServiceDeskID)
);
```

### 51. PTBuildingAccessRequirements

```sql
CREATE TABLE NewHireBuildingAccessRequirements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PTRequestDetailId INT NOT NULL,
    AccessId INT NOT NULL,
    AccessDescription VARCHAR(100) NOT NULL, 

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (PTRequestDetailId) REFERENCES PromotionRequestDetails(Id),
    INDEX IX_PromotionRequestDetails_IsActive (IsActive),
    INDEX IX_PromotionRequestDetails_IsDeleted (IsDeleted)
);
```

### 53. EmploymentStatusMapper

```sql
CREATE TABLE EmploymentStatusMapper (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ActiveStatus VARCHAR(20) NOT NULL,
    LayOffStatus VARCHAR(20) NOT NULL, 
    ReturnToWorkStatus VARCHAR(20) NOT NULL,
    TerminationStatus VARCHAR(20) NOT NULL,
    IsUnion BIT NOT NULL DEFAULT 0, 

    -- Audit Fields
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

## Initial Data Scripts

### Default Request Types and Statuses

```sql
-- Insert default request types
INSERT INTO RequestTypes (RequestTypeName, RequestTypeDescription, CreatedBy) VALUES
('Promotion', 'Employee promotion or transfer request', 1),
('Layoff', 'Employee layoff request', 1),
('Termination', 'Employee termination request', 1),
('ReturnToWork', 'Return to work request for laid-off employees', 1),
('NewHire', 'New hire request (future implementation)', 1);

-- Insert default request statuses
INSERT INTO RequestStatuses (RequestStatusName, RequestStatusDescription, CreatedBy) VALUES
('Pending', 'Request submitted and awaiting processing', 1),
('Processing', 'Request is currently being processed', 1),
('Completed', 'Request has been completed successfully', 1),
('Failed', 'Request processing failed', 1),
('Cancelled', 'Request was cancelled', 1);
```

### Default Email Templates

```sql
-- Insert default email templates
INSERT INTO EmailTemplates (TemplateName, RequestType, EmailType, Subject, Body, CreatedBy) VALUES
('Promotion Confirmation', 'Promotion', 'Confirmation', 'Promotion Request Submitted - {{EmployeeName}}', 
 'Your promotion request for {{EmployeeName}} has been submitted successfully. Request ID: {{RequestId}}', 1),

('Promotion HR Notification', 'Promotion', 'HR_Notification', 'New Promotion Request - {{EmployeeName}}', 
 'A new promotion request has been submitted for {{EmployeeName}} by {{SubmittedBy}}.', 1),

('Layoff Confirmation', 'Layoff', 'Confirmation', 'Layoff Request Submitted - {{EmployeeCount}} Employee(s)', 
 'Your layoff request for {{EmployeeCount}} employee(s) has been submitted successfully.', 1),

('Termination Confirmation', 'Termination', 'Confirmation', 'Termination Request Submitted - {{EmployeeName}}', 
 'Your termination request for {{EmployeeName}} has been submitted successfully.', 1),

('Return to Work Confirmation', 'ReturnToWork', 'Confirmation', 'Return to Work Request Submitted - {{EmployeeCount}} Employee(s)', 
 'Your return to work request for {{EmployeeCount}} employee(s) has been submitted successfully.', 1);
```

### Default Terminatiom Reason

```sql
-- Insert default email templates
INSERT INTO TerminationReasons (ReasonCode, ReasonDescription,  CreatedBy) VALUES
            ('VT SCHOOL','VT SCHOOL',1),
            ('VT SALARY','VT SALARY',1),
            ('VT RETIRE','VT RETIRE',1),
            ('VT PERSONL','VT PERSONL',1),
            ('VT NOSHOW','VT NOSHOW',1),
            ('VT NOAVAIL','VT NOAVAIL',1),
            ('VT NO WORK','VT NO WORK',1),
            ('VT NO FIT','VT NO FIT',1),
            ('VT MOVE','VT MOVE',1),
            ('VT FAMILY','VT FAMILY',1),
            ('VT EVERIFY','VT EVERIFY',1),
            ('VT DIF JOB','VT DIF JOB',1),
            ('VT DEGREE','VT DEGREE',1),
            ('VOLUNTARY','VOLUNTARY',1),
            ('UR','UR',1),
            ('TRANSFER','TRANSFER',1),
            ('RETIRED','RETIRED',1),
            ('MERIT','MERIT',1),
            ('IT SAFETY','IT SAFETY',1),
            ('IT PERF','IT PERF',1),
            ('IT DA POL','IT DA POL',1),
            ('IT BEHAVR','IT BEHAVR',1),
            ('IT ATTEND','IT ATTEND',1),
            ('INVOLUNTAR','INVOLUNTAR',1),
            ('DISABLED','DISABLED',1),
            ('DECEASED','DECEASED',1);
```



### Default Applications

```sql
-- Insert default IT applications
INSERT INTO Applications (ApplicationName, ApplicationDescription, CreatedBy) VALUES
('Cargas Energy', 'Energy management software', 1),
('AutoCAD', 'Computer-aided design software', 1),
('Microsoft Project', 'Project management software', 1),
('Sage 300', 'Enterprise resource planning software', 1),
('Bluebeam Revu', 'PDF markup and collaboration software', 1),
('SketchUp', '3D modeling software', 1),
('Primavera P6', 'Project portfolio management software', 1),
('Procore', 'Construction management software', 1),
('PlanGrid', 'Construction productivity software', 1),
('Viewpoint', 'Construction ERP software', 1);
```

## Views for Common Queries

### Request Summary View

```sql
CREATE VIEW vw_RequestSummary AS
SELECT 
    hr.RequestId,
    rt.RequestTypeName as RequestType,
    rs.RequestStatusName as RequestStatus,
    hr.SubmittedBy,
    hr.SubmittedByName,
    hr.SubmittedDate,
    hr.Notes,
    COUNT(hrd.RequestDetailId) as EmployeeCount,
    STRING_AGG(hrd.EmployeeName, ', ') as EmployeeNames
FROM HRRequests hr
LEFT JOIN HRRequestDetails hrd ON hr.RequestId = hrd.ParentRequestId 
    AND hrd.IsDeleted = 0
LEFT JOIN RequestTypes rt ON hrd.RequestTypeId = rt.RequestTypeId
LEFT JOIN RequestStatuses rs ON hrd.RequestStatusId = rs.RequestStatusId
WHERE hr.IsDeleted = 0
GROUP BY hr.RequestId, rt.RequestTypeName, rs.RequestStatusName, hr.SubmittedBy, 
         hr.SubmittedByName, hr.SubmittedDate, hr.Notes;
```

## Stored Procedures

### Get User Requests

```sql
CREATE PROCEDURE sp_GetUserRequests
    @UserId VARCHAR(255),
    @IsHROrIT BIT = 0
AS
BEGIN
    SELECT * FROM vw_RequestSummary
    WHERE (@IsHROrIT = 1 OR SubmittedBy = @UserId)
    ORDER BY SubmittedDate DESC;
END
```

### Get Request Details

```sql
CREATE PROCEDURE sp_GetRequestDetails
    @RequestId INT
AS
BEGIN
    -- Main request info
    SELECT * FROM HRRequests WHERE RequestId = @RequestId AND IsDeleted = 0;
    
    -- Employee details
    SELECT * FROM HRRequestDetails WHERE ParentRequestId = @RequestId AND IsDeleted = 0;
    
    -- Type-specific details based on request type
    -- (Additional queries would be added based on request type)
END
```

### Default Employment Status Mapper
```sql
-- Insert default Employement Status Mapper
INSERT INTO EmploymentStatusMapper (ActiveStatus, LayOffStatus, ReturnToWorkStatus, TerminationStatus, IsUnion, CreatedBy) VALUES
('FULL TIME', 'LAYOFF-B', 'FULL TIME', 'TERM', 0, 1),
('PART-TIME', 'LAYOFF-NB', 'PART-TIME','TERM', 0, 1),
('SEASON-PT', 'LAYOFF-B', 'FULL TIME','TERM', 0, 1),
('SEASON-PT', 'LAYOFF-NB', 'PART-TIME','TERM', 0, 1),
('TEMPORARY', 'LAYOFF-NB', 'PART-TIME','TERM', 0, 1),
('U-ACTIVE', 'U-LAYOFF', 'U-ACTIVE','U-TERM', 1, 1),
('U-MANAGER', 'U-LAYOFF', 'U-ACTIVE','U-TERM', 1, 1),
```


## Indexes and Performance

All tables include appropriate indexes for:
- Primary keys (clustered)
- Foreign keys
- Soft delete filtering
- Common query patterns (user, date ranges, status)
- Audit field queries

## Data Retention Policy

- Soft deletes preserve data for audit purposes
- Consider archiving old requests after defined retention period
- Email notifications can be purged after successful delivery

### Get Request Details

```sql
CREATE PROCEDURE sp_GetRequestDetails
    @RequestId INT
AS
BEGIN
    -- Main request info
    SELECT * FROM HRRequests WHERE RequestId = @RequestId AND IsDeleted = 0;
    
    -- Employee details
    SELECT * FROM HRRequestDetails WHERE ParentRequestId = @RequestId AND IsDeleted = 0;
    
    -- Type-specific details based on request type
    -- (Additional queries would be added based on request type)
END
```

## Indexes and Performance

All tables include appropriate indexes for:
- Primary keys (clustered)
- Foreign keys
- Soft delete filtering
- Common query patterns (user, date ranges, status)
- Audit field queries

## Data Retention Policy

- Soft deletes preserve data for audit purposes
- Consider archiving old requests after defined retention period
- Email notifications can be purged after successful delivery
- Reference data sync history can be cleaned up periodically
# Promotion Email Notification - Database Updates Required

This document outlines the database table updates required for the Promotion/Transfer email notification system to work correctly.

## Summary of Issues Found

| Table | Issue | Status |
|-------|-------|--------|
| EmailTemplates | Recipients column has typos that don't match CompanyDL columns | Needs Fix |
| CompanyDL | Records must exist for CompanyCode + DeptCode combinations used | Verify |
| EmailContentMappers | Some typos in ContentCode/ContentField values | Optional Fix |

---

## 1. EmailTemplates - Recipients Column Fixes

The `Recipients` column values must match the actual column names in the `CompanyDL` table (case-insensitive).

### Issues Found:

| Template Id | TemplateName | Current Recipients | Issue | Fix |
|-------------|--------------|-------------------|-------|-----|
| 18 | Task Email - Credit Card | `CREDICARDDL` | Missing 'T' | `CreditCardDL` |
| 21 | Task Email - Compliance | `COMPLIANCE-DL` | Has hyphen | `ComplianceDL` |

### SQL Fix:

```sql
-- Fix Credit Card template recipient (CREDICARDDL → CreditCardDL)
UPDATE EmailTemplates
SET Recipients = 'CreditCardDL'
WHERE TemplateName = 'Task Email - Credit Card'
  AND RequestType = 'PROMOTION';

-- Fix Compliance template recipient (COMPLIANCE-DL → ComplianceDL)
UPDATE EmailTemplates
SET Recipients = 'ComplianceDL'
WHERE TemplateName = 'Task Email - Compliance'
  AND RequestType = 'PROMOTION';
```

### Reference - Correct Mapping:

| Template Recipients | CompanyDL Column |
|---------------------|------------------|
| `SecurityDL` | SecurityDL |
| `CreditCardDL` | CreditCardDL |
| `FuelFobDL` | FuelFobDL |
| `FleetDL` | FleetDL |
| `ComplianceDL` | ComplianceDL |
| `SafetyDL` | SafetyDL |
| `HRDL` | HRDL |
| `ITDL` | ITDL |
| `SiteDL` | SiteDL |
| `PayrollDL` | PayrollDL |

---

## 2. CompanyDL - Required Records

The email recipient resolution requires a `CompanyDL` record matching **both** `CompanyCode` AND `DeptCode` from the promotion request.

### How it works:

```csharp
companyDL = await _context.CompanyDLs
    .Where(c => c.CompanyCode == companyCode.Value
            && c.DeptCode == deptCode
            && !c.IsDeleted)
    .FirstOrDefaultAsync();
```

### Verification Query:

```sql
-- Check existing CompanyDL records
SELECT Id, CompanyCode, DeptCode, SecurityDL, CreditCardDL, FuelFobDL, FleetDL, ComplianceDL, SafetyDL, HRDL, ITDL
FROM CompanyDL
WHERE IsDeleted = 0;
```

### Required Action:

Ensure a `CompanyDL` record exists for each `CompanyCode + DeptCode` combination that will be used in promotion requests. If not, the task emails (Door Access, Credit Card, etc.) will not be sent because no recipients will be resolved.

### Example Insert:

```sql
-- Add CompanyDL record for a new company/department combination
INSERT INTO CompanyDL (
    CompanyCode,
    DeptCode,
    SiteDL,
    SecurityDL,
    CreditCardDL,
    FleetDL,
    ComplianceDL,
    SafetyDL,
    HRDL,
    ITDL,
    FuelFobDL,
    CreatedBy,
    CreatedDate,
    IsDeleted
)
VALUES (
    <CompanyCode>,
    <DeptCode>,
    'sitedl@company.com',
    'securitydl@company.com',
    'creditcarddl@company.com',
    'fleetdl@company.com',
    'compliancedl@company.com',
    'safetydl@company.com',
    'hrdl@company.com',
    'itdl@company.com',
    'fuelfobdl@company.com',
    1,
    GETDATE(),
    0
);
```

---

## 3. EmailContentMappers - Optional Fixes

These are optional consistency fixes. The code now has fallback logic that handles these cases.

### Issues Found:

| Id | ContentCode | ContentField | Issue | Fix |
|----|-------------|--------------|-------|-----|
| 36 | `CREDICARDDL` | `credicarddl` | Missing 'T' | `CreditCardDL` / `creditcarddl` |
| 39 | `COMPLIANCE-DL` | `compliancedl` | Hyphen mismatch | `ComplianceDL` / `compliancedl` |

### SQL Fix (Optional):

```sql
-- Fix CREDICARDDL typo (optional - for consistency)
UPDATE EmailContentMappers
SET ContentCode = 'CreditCardDL',
    ContentField = 'creditcarddl'
WHERE Id = 36;

-- Fix COMPLIANCE-DL hyphen (optional - for consistency)
UPDATE EmailContentMappers
SET ContentCode = 'ComplianceDL'
WHERE Id = 39;
```

---

## 4. Promotion Email Templates - Body Field Codes

The task email templates use these ContentCodes in the Body field:

```
EFFECTIVE-DATE,EMPLOYEE-NAME,OLD-POSITION,OLD-COMPANY,OLD-DIVISION,NEW-POSITION,NEW-COMPANY,NEW-DIVISION,NEW-MANAGER,SUBMITTER
```

### Required EmailContentMappers (ContentSource='PROMOTION'):

| ContentCode | ContentField | Status |
|-------------|--------------|--------|
| EMPLOYEE-NAME | employeename | Exists |
| EFFECTIVE-DATE | effectivedate | Exists |
| OLD-POSITION | oldposition | Exists |
| OLD-COMPANY | oldcompany | Exists |
| OLD-DIVISION | olddivision | Exists |
| NEW-POSITION | newposition | Exists |
| NEW-COMPANY | newcompany | Exists |
| NEW-DIVISION | newdivision | Exists |
| NEW-MANAGER | newmanager | Exists |
| OLD-MANAGER | oldmanager | Exists |
| OLD-PAYGROUP | oldpaygroup | Exists |
| NEW-PAYGROUP | newpaygroup | Exists |
| OLD-PHYSICALLOC | oldphysicalloc | Exists |
| NEW-PHYSICALLOC | newphysicalloc | Exists |
| OLD-STATUS | oldstatus | Exists |
| NEW-STATUS | newstatus | Exists |
| OLD-PAYRATE | oldpayrate | Exists |
| NEW-PAYRATE | newpayrate | Exists |

### Shared Fields (ContentSource='APPLICATION'):

| ContentCode | ContentField | Notes |
|-------------|--------------|-------|
| SUBMITTER | submitter | Shared across all request types - code has fallback logic |

---

## 5. Email Notification Conditions

Task emails are only sent when specific conditions are met:

| Email Template | Condition |
|----------------|-----------|
| Confirmation | Always sent |
| Task Email - Door Access | `BuildingAccess` has items |
| Task Email - Credit Card | Any credit card type is true (KwikTripCard, CompanyExpenseCard) |
| Task Email - Fuel Fob | `FuelCardlockAccess = true` |
| Task Email - Fleet | `NeedCompanyCar = true` |
| Task Email - Compliance | `IsApprovedToOperate = true` |
| Task Email - Safety | Always sent (if template exists) |

---

## Quick Verification Checklist

- [ ] Run EmailTemplates fix SQL for Recipients typos
- [ ] Verify CompanyDL records exist for all CompanyCode + DeptCode combinations
- [ ] Verify CompanyDL columns have email addresses populated
- [ ] (Optional) Run EmailContentMappers fix SQL for consistency
- [ ] Rebuild and deploy backend code changes
- [ ] Test promotion submission with all options selected

---

## Related Code Files

- `AzureServiceBusEmailService.cs` - Email sending and content mapping
- `EmailFieldMapperService.cs` - Maps ContentField to actual data values
- `EmailRecipientsService.cs` - Resolves recipients from CompanyDL
- `PromotionRequestsController.cs` - Triggers email notifications on submission

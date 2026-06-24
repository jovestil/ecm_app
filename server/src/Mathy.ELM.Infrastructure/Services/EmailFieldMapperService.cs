using Mathy.ELM.Core.DTOs;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mathy.ELM.Infrastructure.Services;

/// <summary>
/// Service for mapping email template field names to actual New Hire request data
/// </summary>
public class EmailFieldMapperService : IEmailFieldMapperService
{
    private readonly MathyELMContext _context;
    private readonly ILogger<EmailFieldMapperService> _logger;
    private readonly IUserContextService _userContextService;

    public EmailFieldMapperService(
        MathyELMContext context,
        ILogger<EmailFieldMapperService> logger,
        IUserContextService userContextService)
    {
        _context = context;
        _logger = logger;
        _userContextService = userContextService;
    }

    /// <summary>
    /// Maps a list of field names from EmailTemplate.Body to actual values from New Hire request data
    /// </summary>
    public async Task<Dictionary<string, string>> MapNewHireFieldsToDataAsync(
        CreateNewHireRequestDto request,
        List<string> fieldNames)
    {
        var fieldData = new Dictionary<string, string>();

        foreach (var fieldName in fieldNames)
        {
            var normalizedFieldName = fieldName.Trim();
            string? value = null;

            // Map display names to actual data
            switch (normalizedFieldName.ToLower())
            {
                case "start date":
                case "startdate":
                case "first day employment":
                case "firstdayemployment":
                case "newlastdayworked":
                case "newstartdate":
                    value = request.PersonalInfo.FirstDayEmployment?.ToString("MM/dd/yyyy") ?? "N/A";
                    break;

                case "prevlastdayworked":
                case "previouslastdayworked":
                case "prevstartdate":
                    value = request.PersonalInfo.PreviousFirstDayEmployment?.ToString("MM/dd/yyyy") ?? "N/A";
                    break;

                case "new employee":
                case "newemployee":
                case "employee name":
                case "employeename":
                case "full name":
                case "fullname":
                    var firstName = request.PersonalInfo.FirstName ?? "";
                    var lastName = request.PersonalInfo.LastName ?? "";
                    value = $"{firstName} {lastName}".Trim();
                    break;

                case "first name":
                case "firstname":
                    value = request.PersonalInfo.FirstName ?? "N/A";
                    break;

                case "last name":
                case "lastname":
                    value = request.PersonalInfo.LastName ?? "N/A";
                    break;

                case "preferred first name":
                case "preferredfirstname":
                    value = request.PersonalInfo.PreferredFirstName ?? "N/A";
                    break;

                case "company":
                case "companyname":
                    if (request.PositionInfo.CompanyCode.HasValue)
                    {
                        var company = await _context.Companies
                            .Where(c => c.CompanyCode == request.PositionInfo.CompanyCode.Value && !c.IsDeleted)
                            .FirstOrDefaultAsync();
                        value = company?.CompanyName ?? $"Company {request.PositionInfo.CompanyCode}";
                    }
                    else
                    {
                        value = "N/A";
                    }
                    break;

                case "division":
                case "department":
                case "dept":
                case "payroll department":
                case "payrolldepartment":
                    if (request.PositionInfo.PayrollDeptCode.HasValue)
                    {
                        var dept = await _context.PayrollDepartments
                            .Where(d => d.DeptCode == request.PositionInfo.PayrollDeptCode.Value && !d.IsDeleted)
                            .FirstOrDefaultAsync();
                        value = dept?.DeptName ?? $"Dept {request.PositionInfo.PayrollDeptCode}";
                    }
                    else
                    {
                        value = "N/A";
                    }
                    break;

                case "position":
                case "positioncode":
                case "position code":
                case "job title":
                case "jobtitle":
                    if (!string.IsNullOrEmpty(request.PositionInfo.PositionCode))
                    {
                        var position = await _context.Positions
                            .Where(p => p.PositionCode == request.PositionInfo.PositionCode && !p.IsDeleted)
                            .FirstOrDefaultAsync();
                        value = position?.PositionName ?? request.PositionInfo.PositionCode;
                    }
                    else
                    {
                        value = "N/A";
                    }
                    break;

                case "supervisor":
                case "supervisorname":
                case "supervisor name":
                case "manager":
                case "hiring manager":
                case "hiringmanager":
                    if (request.PositionInfo.SupervisorId.HasValue)
                    {
                        var supervisor = await _context.Employees
                            .Where(e => e.EmployeeNumber == request.PositionInfo.SupervisorId.Value && !e.IsDeleted)
                            .FirstOrDefaultAsync();
                        value = supervisor != null ? $"{supervisor.FirstName} {supervisor.LastName}" : $"Employee #{request.PositionInfo.SupervisorId}";
                    }
                    else
                    {
                        value = "N/A";
                    }
                    break;

                case "location":
                case "physical location":
                case "physicallocation":
                    if (request.PositionInfo.LocationCode.HasValue)
                    {
                        var location = await _context.PhysicalLocations
                            .Where(l => l.LocationCode == request.PositionInfo.LocationCode.Value && !l.IsDeleted)
                            .FirstOrDefaultAsync();
                        value = location?.LocationName ?? $"Location {request.PositionInfo.LocationCode}";
                    }
                    else
                    {
                        value = "N/A";
                    }
                    break;

                case "employment status":
                case "employmentstatus":
                    // Look up employment status description from EmploymentStatuses table
                    if (!string.IsNullOrEmpty(request.PositionInfo.EmploymentStatus) && request.PositionInfo.CompanyCode.HasValue)
                    {
                        // Try to convert EmploymentStatus to int (it contains an Id value)
                        if (int.TryParse(request.PositionInfo.EmploymentStatus, out int employmentStatusId))
                        {
                            var employmentStatus = await _context.EmploymentStatuses
                                .Where(es => es.Id == employmentStatusId
                                    && es.CompanyCode == request.PositionInfo.CompanyCode.Value
                                    && !es.IsDeleted)
                                .FirstOrDefaultAsync();

                            value = employmentStatus?.Description ?? request.PositionInfo.EmploymentStatus;
                            _logger.LogDebug("[EMAIL MAPPER] Looked up EmploymentStatusId {EmploymentStatusId} for CompanyCode {CompanyCode}: {Description}",
                                employmentStatusId, request.PositionInfo.CompanyCode.Value, value);
                        }
                        else
                        {
                            // If EmploymentStatus is not a valid int, use it as-is
                            _logger.LogWarning("[EMAIL MAPPER] EmploymentStatus value '{Value}' is not a valid integer for lookup",
                                request.PositionInfo.EmploymentStatus);
                            value = request.PositionInfo.EmploymentStatus;
                        }
                    }
                    else
                    {
                        value = "N/A";
                    }
                    break;

                case "hourly/salaried":
                case "hourly or salaried":
                case "hourlysalaried":
                case "hourly salaried":
                case "salarytype":
                    // Look up salary type description from EmployeeSalaryTypes table
                    if (request.PositionInfo.SalaryCode.HasValue && request.PositionInfo.CompanyCode.HasValue)
                    {
                        // Extract values before LINQ query (dynamic can't be used in EF expressions)
                        int salaryCode = request.PositionInfo.SalaryCode.Value;
                        int companyCode = request.PositionInfo.CompanyCode.Value;

                        var salaryType = await _context.EmployeeSalaryTypes
                            .Where(st => st.SalaryCode == salaryCode
                                && st.CompanyCode == companyCode
                                && !st.IsDeleted)
                            .FirstOrDefaultAsync();

                        value = salaryType?.Description ?? salaryCode.ToString();
                        _logger.LogDebug("[EMAIL MAPPER] Looked up SalaryCode {SalaryCode} for CompanyCode {CompanyCode}: {Description}",
                            salaryCode, companyCode, value);
                    }
                    else
                    {
                        value = "N/A";
                    }
                    break;

                case "rehire":
                case "rehire y/n":
                    value = request.PersonalInfo.Rehire.HasValue && request.PersonalInfo.Rehire.Value ? "Yes" : "No";
                    break;

                case "referred by":
                case "referredby":
                    value = request.PersonalInfo.ReferredBy ?? "N/A";
                    break;

                // Building Access
                case "building access":
                case "buildingaccess":
                case "door access":
                case "dooraccess":
                    if (request.BuildingAccess != null && request.BuildingAccess.Any())
                    {
                        value = string.Join(", ", request.BuildingAccess.Select(b => b.AccessDescription));
                    }
                    else
                    {
                        value = "None";
                    }
                    break;

                // Building Access Requirements (bullet list format)
                case "building access requirements":
                case "buildingaccessrequirements":
                case "list of building access requirements":
                case "listofbuildingaccessrequirements":
                    if (request.BuildingAccess != null && request.BuildingAccess.Any())
                    {
                        // Format as HTML bullet list for email display
                        var bulletList = "<ul style='margin: 10px 0; padding-left: 20px;'>";
                        foreach (var requirement in request.BuildingAccess)
                        {
                            bulletList += $"<li>{requirement.AccessDescription}</li>";
                        }
                        bulletList += "</ul>";
                        value = bulletList;
                    }
                    else
                    {
                        value = "Not applicable";
                    }
                    break;

                // Credit Card Info
                case "card types":
                case "cardtypes":
                case "credit card types":
                case "creditcardtypes":
                    if (request.CreditCardInfo != null)
                    {
                        var cards = new List<string>();
                        if (request.CreditCardInfo.KwikTripCard == true) cards.Add("KwikTrip");
                        if (request.CreditCardInfo.CompanyExpenseCard == true) cards.Add("Company Expense");
                        value = cards.Any() ? string.Join(", ", cards) : "None";
                    }
                    else
                    {
                        value = "None";
                    }
                    break;

                case "weekly limit":
                case "weeklylimit":
                case "credit card limit":
                case "creditcardlimit":
                case "cc-weekly-limit":
                    value = request.CreditCardInfo?.WeeklyLimit?.ToString("C") ?? "N/A";
                    break;

                case "credit expense type":
                case "creditexpensetype":
                case "credit-expense-type":
                    value = request.CreditCardInfo?.CreditExpenseType ?? "N/A";
                    break;

                case "is-kwik-trip-card":
                case "kwiktripcardlock":
                case "kwik trip card":
                case "kwiktripcard":
                    value = request.CreditCardInfo?.KwikTripCard == true ? "Yes" : "No";
                    break;

                case "needs card lock":
                case "needscardlock":
                case "fuel cardlock access":
                case "fuelcardlockaccess":
                case "cardlock access":
                case "cardlockaccess":
                case "is-need-card-lock":
                case "needcardlock":
                    value = request.CreditCardInfo?.FuelCardlockAccess.HasValue == true && request.CreditCardInfo.FuelCardlockAccess.Value ? "Yes" : "No";
                    break;

                case "fuel cardlock address":
                case "fuelcardlockaddress":
                case "cardlock address":
                case "cardlockaddress":
                    value = request.CreditCardInfo?.FuelCardlockAddress ?? "N/A";
                    break;

                // Vehicle Info
                case "license class":
                case "licenseclass":
                case "driver class":
                case "driverclass":
                case "driver classification":
                case "driverclassification":
                    value = request.VehicleInfo?.DriverClassification ?? "N/A";
                    break;

                case "application part 2 complete":
                case "applicationpart2complete":
                case "part 2 complete":
                case "part2complete":
                    value = request.VehicleInfo?.IsApplicationPart2Complete.HasValue == true && request.VehicleInfo.IsApplicationPart2Complete.Value ? "Yes" : "No";
                    break;

                case "drug testing profile":
                case "drugtestingprofile":
                case "drug and alcohol profile":
                case "drugandalcoholprofile":
                    value = request.VehicleInfo?.DrugAndAlcoholProfile ?? "N/A";
                    break;

                case "needs company car":
                case "needscompanycar":
                case "company car":
                case "companycar":
                case "is-need-vehicle":
                case "needvehicle":
                    value = request.VehicleInfo?.NeedCompanyCar.HasValue == true && request.VehicleInfo.NeedCompanyCar.Value ? "Yes" : "No";
                    break;

                // IT Info
                case "email required":
                case "emailrequired":
                case "needs email":
                case "needsemail":
                    value = request.ITInfo?.EmailRequired.HasValue == true && request.ITInfo.EmailRequired.Value ? "Yes" : "No";
                    break;

                case "ship to address":
                case "shiptoaddress":
                    // For Door Access emails, ship to address is the fuel cardlock address
                    value = request.CreditCardInfo?.FuelCardlockAddress ?? "N/A";
                    break;

                case "alternate delivery location":
                case "alternatedeliverylocation":
                case "delivery location":
                case "deliverylocation":
                    // For IT emails, delivery location is the alternate delivery address
                    value = request.ITInfo?.AlternateDeliveryLocation ?? "N/A";
                    break;

                // Request metadata (placeholders - these need user context service integration)
                case "request created by":
                case "requestcreatedby":
                case "created by":
                case "createdby":
                case "submitter":
                    // Get current user's display name (full name, not email) from user context
                    try
                    {
                        value = _userContextService.GetUserDisplayName();
                        _logger.LogDebug("[EMAIL MAPPER] Retrieved submitter display name from user context: {SubmitterName}", value);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogWarning(ex, "[EMAIL MAPPER] Unable to retrieve current user for submitter field");
                        value = "N/A";
                    }
                    break;

                case "ecm":
                case "ecmlink":
                case "ecm link":
                    // ECM Link is injected by AzureServiceBusEmailService after field mapping
                    // (requires requestId which is not available here)
                    value = "";
                    break;

                case "by:od":
                case "byod":
                    // TODO: Clarify what BY:OD means (Bring Your Own Device?)
                    value = request.PhoneInfo?.BYODCellphone.HasValue == true && request.PhoneInfo.BYODCellphone.Value ? "Yes" : "No";
                    _logger.LogWarning("Field 'BY:OD' mapping assumption: BYOD Cellphone");
                    break;

                case "primary hr rep":
                case "primaryhrrep":
                case "hr rep":
                case "hrrep":
                    if (request.PositionInfo.PayrollDeptCode.HasValue && request.PositionInfo.CompanyCode.HasValue)
                    {
                        var deptForHRRep = await _context.PayrollDepartments
                            .FirstOrDefaultAsync(d => d.DeptCode == request.PositionInfo.PayrollDeptCode.Value
                                && d.CompanyCode == request.PositionInfo.CompanyCode.Value && !d.IsDeleted);
                        if (deptForHRRep?.HRRep != null)
                        {
                            var hrRepEmp = await _context.Employees
                                .FirstOrDefaultAsync(e => e.EmployeeNumber == deptForHRRep.HRRep.Value && !e.IsDeleted);
                            value = hrRepEmp != null ? $"{hrRepEmp.FirstName} {hrRepEmp.LastName}".Trim() : "N/A";
                        }
                    }
                    break;

                case "backup hr rep":
                case "backuphrrep":
                    if (request.PositionInfo.PayrollDeptCode.HasValue && request.PositionInfo.CompanyCode.HasValue)
                    {
                        var deptForHRPartner = await _context.PayrollDepartments
                            .FirstOrDefaultAsync(d => d.DeptCode == request.PositionInfo.PayrollDeptCode.Value
                                && d.CompanyCode == request.PositionInfo.CompanyCode.Value && !d.IsDeleted);
                        if (deptForHRPartner?.HRPartner != null)
                        {
                            var hrPartnerEmp = await _context.Employees
                                .FirstOrDefaultAsync(e => e.EmployeeNumber == deptForHRPartner.HRPartner.Value && !e.IsDeleted);
                            value = hrPartnerEmp != null ? $"{hrPartnerEmp.FirstName} {hrPartnerEmp.LastName}".Trim() : "N/A";
                        }
                    }
                    break;

                // Error Message (for failed request notifications)
                case "errormessage":
                    value = request.ErrorMessage ?? "N/A";
                    break;

                // Notes
                case "notes":
                    value = request.Notes ?? "N/A";
                    break;

                default:
                    _logger.LogWarning("Unknown field name in email template: {FieldName}", normalizedFieldName);
                    value = $"[{normalizedFieldName}]";
                    break;
            }

            fieldData[normalizedFieldName] = value ?? "N/A";
        }

        return fieldData;
    }

    /// <summary>
    /// Maps a list of field names from EmailTemplate.Body to actual values from Promotion request data
    /// Uses EmailContentMappers with ContentSource='PROMOTION' field names
    /// </summary>
    public async Task<Dictionary<string, string>> MapPromotionFieldsToDataAsync(
        CreatePromotionRequestDto request,
        List<string> fieldNames)
    {
        var fieldData = new Dictionary<string, string>();

        // Pre-load reference data for lookups
        var employee = await _context.Employees
            .Where(e => e.EmployeeNumber == request.EmployeeId && !e.IsDeleted)
            .FirstOrDefaultAsync();

        var oldCompany = await _context.Companies
            .Where(c => c.CompanyCode == request.CurrentPayrollCompanyCode && !c.IsDeleted)
            .FirstOrDefaultAsync();

        var newCompany = await _context.Companies
            .Where(c => c.CompanyCode == request.NewPayrollCompanyCode && !c.IsDeleted)
            .FirstOrDefaultAsync();

        var oldDivision = await _context.PayrollDepartments
            .Where(d => d.DeptCode == request.CurrentPayrollDeptCode
                && d.CompanyCode == request.CurrentPayrollCompanyCode
                && !d.IsDeleted)
            .FirstOrDefaultAsync();

        var newDivision = await _context.PayrollDepartments
            .Where(d => d.DeptCode == request.NewPayrollDeptCode
                && d.CompanyCode == request.NewPayrollCompanyCode
                && !d.IsDeleted)
            .FirstOrDefaultAsync();

        var oldPosition = await _context.Positions
            .Where(p => p.PositionCode == request.CurrentPositionCode
                && p.CompanyCode == request.CurrentPayrollCompanyCode
                && !p.IsDeleted)
            .FirstOrDefaultAsync();

        var newPosition = await _context.Positions
            .Where(p => p.PositionCode == request.NewPositionCode
                && p.CompanyCode == request.NewPayrollCompanyCode
                && !p.IsDeleted)
            .FirstOrDefaultAsync();

        var oldManager = await _context.Employees
            .Where(e => e.EmployeeNumber == request.CurrentSupervisorId && !e.IsDeleted)
            .FirstOrDefaultAsync();

        var newManager = await _context.Employees
            .Where(e => e.EmployeeNumber == request.NewSupervisorId && !e.IsDeleted)
            .FirstOrDefaultAsync();

        var oldPayGroup = await _context.PayrollGroups
            .Where(g => g.GroupCode == request.CurrentPayrollGroupCode
                && g.CompanyCode == request.CurrentPayrollCompanyCode
                && !g.IsDeleted)
            .FirstOrDefaultAsync();

        var newPayGroup = await _context.PayrollGroups
            .Where(g => g.GroupCode == request.NewPayrollGroupCode
                && g.CompanyCode == request.NewPayrollCompanyCode
                && !g.IsDeleted)
            .FirstOrDefaultAsync();

        var oldPhysicalLoc = await _context.PhysicalLocations
            .Where(l => l.LocationCode == request.CurrentPhysicalLocationCode && !l.IsDeleted)
            .FirstOrDefaultAsync();

        var newPhysicalLoc = await _context.PhysicalLocations
            .Where(l => l.LocationCode == request.NewPhysicalLocationCode && !l.IsDeleted)
            .FirstOrDefaultAsync();

        // Pre-load salary types for pay rate lookups
        Core.Entities.EmployeeSalaryType? oldSalaryType = null;
        Core.Entities.EmployeeSalaryType? newSalaryType = null;

        if (request.CurrentSalaryCode.HasValue && request.CurrentPayrollCompanyCode.HasValue)
        {
            oldSalaryType = await _context.EmployeeSalaryTypes
                .Where(st => st.SalaryCode == request.CurrentSalaryCode.Value
                    && st.CompanyCode == request.CurrentPayrollCompanyCode.Value
                    && !st.IsDeleted)
                .FirstOrDefaultAsync();
        }

        if (request.NewSalaryCode.HasValue && request.NewPayrollCompanyCode > 0)
        {
            newSalaryType = await _context.EmployeeSalaryTypes
                .Where(st => st.SalaryCode == request.NewSalaryCode.Value
                    && st.CompanyCode == request.NewPayrollCompanyCode
                    && !st.IsDeleted)
                .FirstOrDefaultAsync();
        }

        foreach (var fieldName in fieldNames)
        {
            var normalizedFieldName = fieldName.Trim().Replace(" ", "").ToLowerInvariant();
            string value = string.Empty;

            switch (normalizedFieldName)
            {
                // Employee Information (ContentSource='PROMOTION')
                case "employeename":
                    value = employee != null ? $"{employee.FirstName} {employee.LastName}" : "N/A";
                    break;

                case "employeeid":
                    value = request.EmployeeId.ToString();
                    break;

                // Effective Date (ContentSource='PROMOTION')
                case "effectivedate":
                case "newlastdayworked":
                case "neweffectivedate":
                    value = request.EffectiveDate.ToString("MM/dd/yyyy");
                    break;

                // Previous Effective Date (ContentSource='PROMOTION')
                case "prevlastdayworked":
                case "previouslastdayworked":
                case "preveffectivedate":
                    value = request.PreviousEffectiveDate?.ToString("MM/dd/yyyy") ?? "N/A";
                    break;

                // OLD (Current) Position Information (ContentSource='PROMOTION')
                case "oldposition":
                    value = oldPosition?.PositionName ?? request.CurrentPositionCode ?? "N/A";
                    break;

                case "oldcompany":
                    value = oldCompany?.CompanyName ?? request.CurrentPayrollCompanyCode?.ToString() ?? "N/A";
                    break;

                case "olddivision":
                    value = oldDivision?.DeptName ?? request.CurrentPayrollDeptCode?.ToString() ?? "N/A";
                    break;

                case "oldmanager":
                    value = oldManager != null ? $"{oldManager.FirstName} {oldManager.LastName}" : request.CurrentSupervisorId?.ToString() ?? "N/A";
                    break;

                case "oldpaygroup":
                    value = oldPayGroup?.GroupName ?? request.CurrentPayrollGroupCode?.ToString() ?? "N/A";
                    break;

                case "oldphysicalloc":
                    value = oldPhysicalLoc?.LocationName ?? request.CurrentPhysicalLocationCode?.ToString() ?? "N/A";
                    break;

                case "oldstatus":
                    value = request.CurrentStatus ?? "N/A";
                    break;

                case "oldpayrate":
                    value = oldSalaryType?.Description ?? request.CurrentSalaryCode?.ToString() ?? "N/A";
                    break;

                // NEW Position Information (ContentSource='PROMOTION')
                case "newposition":
                    value = newPosition?.PositionName ?? request.NewPositionCode ?? "N/A";
                    break;

                case "newcompany":
                    value = newCompany?.CompanyName ?? request.NewPayrollCompanyCode.ToString();
                    break;

                case "newdivision":
                    value = newDivision?.DeptName ?? request.NewPayrollDeptCode.ToString();
                    break;

                case "newmanager":
                    value = newManager != null ? $"{newManager.FirstName} {newManager.LastName}" : request.NewSupervisorId?.ToString() ?? "N/A";
                    break;

                case "newpaygroup":
                    value = newPayGroup?.GroupName ?? request.NewPayrollGroupCode.ToString();
                    break;

                case "newphysicalloc":
                    value = newPhysicalLoc?.LocationName ?? request.NewPhysicalLocationCode.ToString();
                    break;

                case "newstatus":
                    value = request.NewStatus ?? "N/A";
                    break;

                case "newpayrate":
                    value = newSalaryType?.Description ?? request.NewSalaryCode?.ToString() ?? "N/A";
                    break;

                // Submitter (ContentSource='APPLICATION')
                case "submitter":
                    // Get current user's display name (full name, not email) from user context
                    try
                    {
                        value = _userContextService.GetUserDisplayName();
                        _logger.LogDebug("[PROMOTION FIELD MAPPER] Retrieved submitter display name from user context: {SubmitterName}", value);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogWarning(ex, "[PROMOTION FIELD MAPPER] Unable to retrieve current user for submitter field");
                        value = "N/A";
                    }
                    break;

                // Notes
                case "notes":
                    value = request.Notes ?? "N/A";
                    break;

                // Vehicle Info (Driver Classification = LicenseClass in PromotionVehicleInfoDto)
                case "driverclass":
                case "driverclassification":
                case "licenseclass":
                    value = request.VehicleInfo?.LicenseClass ?? "N/A";
                    break;

                case "part2complete":
                case "applicationpart2complete":
                    value = request.VehicleInfo?.IsApplicationPart2Complete == true ? "Yes" : "No";
                    break;

                case "drugtestingprofile":
                case "drugandalcoholprofile":
                    value = request.VehicleInfo?.DrugAndAlcoholProfile ?? "N/A";
                    break;

                case "needscompanycar":
                case "needvehicle":
                case "companycar":
                    value = request.VehicleInfo?.NeedCompanyCar == true ? "Yes" : "No";
                    break;

                case "isapprovedtooperate":
                case "approvedtooperate":
                    value = request.VehicleInfo?.IsApprovedToOperate == true ? "Yes" : "No";
                    break;

                // Work Email
                case "currentworkemail":
                case "currentemail":
                    value = request.CurrentWorkEmail ?? "N/A";
                    break;

                case "newworkemail":
                case "newemail":
                    value = request.NewWorkEmail ?? "N/A";
                    break;

                // Building Access
                case "useexistingkeyfob":
                case "existingkeyfob":
                    value = request.UseExistingKeyFob == true ? "Yes - Needs to be reprogrammed" : "No";
                    break;

                // Error Message (for failed request notifications)
                case "errormessage":
                    value = request.ErrorMessage ?? "N/A";
                    break;

                // Default case - return field name in brackets for debugging
                default:
                    _logger.LogDebug("[PROMOTION FIELD MAPPER] Unknown field: {FieldName}", fieldName);
                    value = $"[{normalizedFieldName}]";
                    break;
            }

            fieldData[normalizedFieldName] = value ?? "N/A";
        }

        return fieldData;
    }

    /// <summary>
    /// Maps a list of field names from EmailTemplate.Body to actual values from Termination request data
    /// Uses EmailContentMappers with ContentSource='TERMINATION' field names
    /// </summary>
    public async Task<Dictionary<string, string>> MapTerminationFieldsToDataAsync(
        TerminationEmailDataDto request,
        List<string> fieldNames)
    {
        var fieldData = new Dictionary<string, string>();

        // Pre-load reference data for lookups if not already provided
        Core.Entities.Employee? employee = null;
        if (request.EmployeeId > 0)
        {
            employee = await _context.Employees
                .Where(e => e.EmployeeNumber == request.EmployeeId && !e.IsDeleted)
                .FirstOrDefaultAsync();
        }

        Core.Entities.Company? company = null;
        if (request.CompanyCode.HasValue)
        {
            company = await _context.Companies
                .Where(c => c.CompanyCode == request.CompanyCode.Value && !c.IsDeleted)
                .FirstOrDefaultAsync();
        }

        Core.Entities.PayrollDepartment? division = null;
        if (request.DeptCode.HasValue && request.CompanyCode.HasValue)
        {
            division = await _context.PayrollDepartments
                .Where(d => d.DeptCode == request.DeptCode.Value
                    && d.CompanyCode == request.CompanyCode.Value
                    && !d.IsDeleted)
                .FirstOrDefaultAsync();
        }

        foreach (var fieldName in fieldNames)
        {
            var normalizedFieldName = fieldName.Trim().Replace(" ", "").ToLowerInvariant();
            string value = string.Empty;

            switch (normalizedFieldName)
            {
                // Employee Name (ContentSource='TERMINATION')
                case "employeename":
                    value = request.EmployeeName
                        ?? (employee != null ? $"{employee.FirstName} {employee.LastName}" : "N/A");
                    break;

                // Company (ContentSource='TERMINATION')
                case "company":
                    value = request.CompanyName
                        ?? company?.CompanyName
                        ?? request.CompanyCode?.ToString()
                        ?? "N/A";
                    break;

                // Division (ContentSource='TERMINATION')
                case "division":
                    value = request.DivisionName
                        ?? division?.DeptName
                        ?? request.DeptCode?.ToString()
                        ?? "N/A";
                    break;

                // Employment Status (ContentSource='TERMINATION')
                case "status":
                case "employmentstatus":
                    value = request.EmploymentStatus
                        ?? employee?.EmploymentStatus
                        ?? "N/A";
                    break;

                // Effective Date (ContentSource='TERMINATION')
                case "effectivedate":
                case "lastdayworked":
                case "newlastdayworked":
                case "neweffectivedate":
                    value = request.EffectiveDate?.ToString("MM/dd/yyyy") ?? "N/A";
                    break;

                // Previous Effective Date (ContentSource='TERMINATION')
                case "prevlastdayworked":
                case "previouslastdayworked":
                case "preveffectivedate":
                    value = request.PreviousEffectiveDate?.ToString("MM/dd/yyyy") ?? "N/A";
                    break;

                // Notes (ContentSource='TERMINATION')
                case "notes":
                    value = request.Notes ?? "N/A";
                    break;

                // Submitter (ContentSource='APPLICATION')
                case "submitter":
                    if (!string.IsNullOrEmpty(request.Submitter))
                    {
                        value = request.Submitter;
                    }
                    else
                    {
                        try
                        {
                            value = _userContextService.GetUserDisplayName();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            value = "N/A";
                        }
                    }
                    break;

                // Kwik Trip Card fields
                case "kwiktripcard":
                case "kwiktripcardlock":
                case "withkwiktripcard":
                case "is-kwik-trip-card":
                    value = request.WithKwikTripCard ? "Yes" : "No";
                    break;

                case "kwikcard4digitno":
                case "kwiktripcardlast4":
                case "last4digits":
                    value = request.WithKwikTripCard && !string.IsNullOrEmpty(request.KwikCard4DigitNo)
                        ? request.KwikCard4DigitNo
                        : "N/A";
                    break;

                // Error Message (for failed request notifications)
                case "errormessage":
                    value = request.ErrorMessage ?? "N/A";
                    break;

                // Default case - return field name in brackets for debugging
                default:
                    _logger.LogDebug("[TERMINATION FIELD MAPPER] Unknown field: {FieldName}", fieldName);
                    value = $"[{normalizedFieldName}]";
                    break;
            }

            fieldData[normalizedFieldName] = value ?? "N/A";
        }

        return fieldData;
    }

    /// <summary>
    /// Maps a list of field names from EmailTemplate.Body to actual values from Layoff request data
    /// Uses EmailContentMappers with ContentSource='LAYOFF' field names
    /// </summary>
    public async Task<Dictionary<string, string>> MapLayoffFieldsToDataAsync(
        LayoffEmailDataDto request,
        List<string> fieldNames)
    {
        var fieldData = new Dictionary<string, string>();

        // Pre-load reference data for lookups if not already provided
        Core.Entities.Employee? employee = null;
        if (request.EmployeeId > 0)
        {
            employee = await _context.Employees
                .Where(e => e.EmployeeNumber == request.EmployeeId && !e.IsDeleted)
                .FirstOrDefaultAsync();
        }

        Core.Entities.Company? company = null;
        if (request.CompanyCode.HasValue)
        {
            company = await _context.Companies
                .Where(c => c.CompanyCode == request.CompanyCode.Value && !c.IsDeleted)
                .FirstOrDefaultAsync();
        }

        Core.Entities.PayrollDepartment? division = null;
        if (request.DeptCode.HasValue && request.CompanyCode.HasValue)
        {
            division = await _context.PayrollDepartments
                .Where(d => d.DeptCode == request.DeptCode.Value
                    && d.CompanyCode == request.CompanyCode.Value
                    && !d.IsDeleted)
                .FirstOrDefaultAsync();
        }

        foreach (var fieldName in fieldNames)
        {
            var normalizedFieldName = fieldName.Trim().Replace(" ", "").ToLowerInvariant();
            string value = string.Empty;

            switch (normalizedFieldName)
            {
                // Employee Name (ContentSource='LAYOFF')
                case "employeename":
                    value = request.EmployeeName
                        ?? (employee != null ? $"{employee.FirstName} {employee.LastName}" : "N/A");
                    break;

                // Company (ContentSource='LAYOFF')
                case "company":
                    value = request.CompanyName
                        ?? company?.CompanyName
                        ?? request.CompanyCode?.ToString()
                        ?? "N/A";
                    break;

                // Division (ContentSource='LAYOFF')
                case "division":
                    value = request.DivisionName
                        ?? division?.DeptName
                        ?? request.DeptCode?.ToString()
                        ?? "N/A";
                    break;

                // Employment Status (ContentSource='LAYOFF')
                case "status":
                case "employmentstatus":
                    value = request.EmploymentStatus
                        ?? employee?.EmploymentStatus
                        ?? "N/A";
                    break;

                // Effective Date (ContentSource='LAYOFF')
                case "effectivedate":
                case "lastdayworked":
                case "newlastdayworked":
                    value = request.EffectiveDate?.ToString("MM/dd/yyyy") ?? "N/A";
                    break;

                // Previous Effective Date (ContentSource='LAYOFF')
                case "prevlastdayworked":
                case "previouslastdayworked":
                    value = request.PreviousEffectiveDate?.ToString("MM/dd/yyyy") ?? "N/A";
                    break;

                // Notes (ContentSource='LAYOFF')
                case "notes":
                    value = request.Notes ?? "N/A";
                    break;

                // Submitter (ContentSource='APPLICATION')
                case "submitter":
                    if (!string.IsNullOrEmpty(request.Submitter))
                    {
                        value = request.Submitter;
                    }
                    else
                    {
                        try
                        {
                            value = _userContextService.GetUserDisplayName();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            value = "N/A";
                        }
                    }
                    break;

                // Error Message (for failed request notifications)
                case "errormessage":
                    value = request.ErrorMessage ?? "N/A";
                    break;

                // Default case - return field name in brackets for debugging
                default:
                    _logger.LogDebug("[LAYOFF FIELD MAPPER] Unknown field: {FieldName}", fieldName);
                    value = $"[{normalizedFieldName}]";
                    break;
            }

            fieldData[normalizedFieldName] = value ?? "N/A";
        }

        return fieldData;
    }

    /// <summary>
    /// Maps a list of field names from EmailTemplate.Body to actual values from ReturnToWork request data
    /// Uses EmailContentMappers with ContentSource='RETURNTOWORK' field names
    /// </summary>
    public async Task<Dictionary<string, string>> MapReturnToWorkFieldsToDataAsync(
        ReturnToWorkEmailDataDto request,
        List<string> fieldNames)
    {
        var fieldData = new Dictionary<string, string>();

        // Pre-load reference data for lookups if not already provided
        Core.Entities.Employee? employee = null;
        if (request.EmployeeId > 0)
        {
            employee = await _context.Employees
                .Where(e => e.EmployeeNumber == request.EmployeeId && !e.IsDeleted)
                .FirstOrDefaultAsync();
        }

        Core.Entities.Company? company = null;
        if (request.CompanyCode.HasValue)
        {
            company = await _context.Companies
                .Where(c => c.CompanyCode == request.CompanyCode.Value && !c.IsDeleted)
                .FirstOrDefaultAsync();
        }

        Core.Entities.PayrollDepartment? division = null;
        if (request.DeptCode.HasValue && request.CompanyCode.HasValue)
        {
            division = await _context.PayrollDepartments
                .Where(d => d.DeptCode == request.DeptCode.Value
                    && d.CompanyCode == request.CompanyCode.Value
                    && !d.IsDeleted)
                .FirstOrDefaultAsync();
        }

        foreach (var fieldName in fieldNames)
        {
            var normalizedFieldName = fieldName.Trim().Replace(" ", "").ToLowerInvariant();
            string value = string.Empty;

            switch (normalizedFieldName)
            {
                // Employee Name (ContentSource='RETURNTOWORK')
                case "employeename":
                    value = request.EmployeeName
                        ?? (employee != null ? $"{employee.FirstName} {employee.LastName}" : "N/A");
                    break;

                // Company (ContentSource='RETURNTOWORK')
                case "company":
                    value = request.CompanyName
                        ?? company?.CompanyName
                        ?? request.CompanyCode?.ToString()
                        ?? "N/A";
                    break;

                // Division (ContentSource='RETURNTOWORK')
                case "division":
                    value = request.DivisionName
                        ?? division?.DeptName
                        ?? request.DeptCode?.ToString()
                        ?? "N/A";
                    break;

                // Employment Status (ContentSource='RETURNTOWORK')
                case "status":
                case "employmentstatus":
                    value = request.EmploymentStatus
                        ?? employee?.EmploymentStatus
                        ?? "N/A";
                    break;

                // Effective Date / Return Date (ContentSource='RETURNTOWORK')
                case "effectivedate":
                case "returndate":
                case "returntoworkdate":
                case "newlastdayworked":
                case "newreturntoworkdate":
                    value = request.EffectiveDate?.ToString("MM/dd/yyyy") ?? "N/A";
                    break;

                // Previous Effective Date (ContentSource='RETURNTOWORK')
                case "prevlastdayworked":
                case "previouslastdayworked":
                case "prevreturntoworkdate":
                    value = request.PreviousEffectiveDate?.ToString("MM/dd/yyyy") ?? "N/A";
                    break;

                // Notes (ContentSource='RETURNTOWORK')
                case "notes":
                    value = request.Notes ?? "N/A";
                    break;

                // Submitter (ContentSource='APPLICATION')
                case "submitter":
                    if (!string.IsNullOrEmpty(request.Submitter))
                    {
                        value = request.Submitter;
                    }
                    else
                    {
                        try
                        {
                            value = _userContextService.GetUserDisplayName();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            value = "N/A";
                        }
                    }
                    break;

                // Error Message (for failed request notifications)
                case "errormessage":
                    value = request.ErrorMessage ?? "N/A";
                    break;

                // Default case - return field name in brackets for debugging
                default:
                    _logger.LogDebug("[RETURNTOWORK FIELD MAPPER] Unknown field: {FieldName}", fieldName);
                    value = $"[{normalizedFieldName}]";
                    break;
            }

            fieldData[normalizedFieldName] = value ?? "N/A";
        }

        return fieldData;
    }
}

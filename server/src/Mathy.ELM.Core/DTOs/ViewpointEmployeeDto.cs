using System.Text.Json;
using System.Text.Json.Serialization;
using Mathy.ELM.Core.Converters;

namespace Mathy.ELM.Core.DTOs;

public class ViewpointEmployeeDto
{
    [JsonPropertyName("__ryvitKeys")]
    public List<string>? RyvitKeys { get; set; }

    [JsonPropertyName("__modifiedUTC")]
    public string? ModifiedUTC { get; set; }

    [JsonPropertyName("HRCo")]
    public int? HRCo { get; set; }

    [JsonPropertyName("HRRef")]
    public int? HRRef { get; set; }

    [JsonPropertyName("PRCo")]
    public int? PRCo { get; set; }

    [JsonPropertyName("PREmp")]
    public int? PREmp { get; set; }

    [JsonPropertyName("LastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("FirstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("MiddleName")]
    public string? MiddleName { get; set; }

    [JsonPropertyName("SortName")]
    public string? SortName { get; set; }

    [JsonPropertyName("Address")]
    public string? Address { get; set; }

    [JsonPropertyName("City")]
    public string? City { get; set; }

    [JsonPropertyName("State")]
    public string? State { get; set; }

    [JsonPropertyName("Zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("Address2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("Phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("WorkPhone")]
    public string? WorkPhone { get; set; }

    [JsonPropertyName("Pager")]
    public string? Pager { get; set; }

    [JsonPropertyName("CellPhone")]
    public string? CellPhone { get; set; }

    [JsonPropertyName("SSN")]
    public string? SSN { get; set; }

    [JsonPropertyName("Sex")]
    public string? Sex { get; set; }

    [JsonPropertyName("Race")]
    public string? Race { get; set; }

    [JsonPropertyName("BirthDate")]
    public string? BirthDate { get; set; }

    [JsonPropertyName("HireDate")]
    public string? HireDate { get; set; }

    [JsonPropertyName("TermDate")]
    public string? TermDate { get; set; }

    [JsonPropertyName("TermReason")]
    public string? TermReason { get; set; }

    [JsonPropertyName("ActiveYN")]
    public string? ActiveYN { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("PRGroup")]
    public int? PRGroup { get; set; }

    [JsonPropertyName("PRDept")]
    public string? PRDept { get; set; }

    [JsonPropertyName("StdCraft")]
    public string? StdCraft { get; set; }

    [JsonPropertyName("StdClass")]
    public string? StdClass { get; set; }

    [JsonPropertyName("StdInsCode")]
    public string? StdInsCode { get; set; }

    [JsonPropertyName("StdTaxState")]
    public string? StdTaxState { get; set; }

    [JsonPropertyName("StdUnempState")]
    public string? StdUnempState { get; set; }

    [JsonPropertyName("StdInsState")]
    public string? StdInsState { get; set; }

    [JsonPropertyName("StdLocal")]
    public string? StdLocal { get; set; }

    [JsonPropertyName("W4CompleteYN")]
    public string? W4CompleteYN { get; set; }

    [JsonPropertyName("PositionCode")]
    public string? PositionCode { get; set; }

    [JsonPropertyName("NoRehireYN")]
    public string? NoRehireYN { get; set; }

    [JsonPropertyName("MaritalStatus")]
    public string? MaritalStatus { get; set; }

    [JsonPropertyName("MaidenName")]
    public string? MaidenName { get; set; }

    [JsonPropertyName("SpouseName")]
    public string? SpouseName { get; set; }

    [JsonPropertyName("PassPort")]
    public string? PassPort { get; set; }

    [JsonPropertyName("RelativesYN")]
    public string? RelativesYN { get; set; }

    [JsonPropertyName("HandicapYN")]
    public string? HandicapYN { get; set; }

    [JsonPropertyName("HandicapDesc")]
    public string? HandicapDesc { get; set; }

    [JsonPropertyName("VetJobCategory")]
    public string? VetJobCategory { get; set; }

    [JsonPropertyName("PhysicalYN")]
    public string? PhysicalYN { get; set; }

    [JsonPropertyName("PhysDate")]
    public string? PhysDate { get; set; }

    [JsonPropertyName("PhysExpireDate")]
    public string? PhysExpireDate { get; set; }

    [JsonPropertyName("PhysResults")]
    public string? PhysResults { get; set; }

    [JsonPropertyName("LicNumber")]
    public string? LicNumber { get; set; }

    [JsonPropertyName("LicType")]
    public string? LicType { get; set; }

    [JsonPropertyName("LicState")]
    public string? LicState { get; set; }

    [JsonPropertyName("LicExpDate")]
    public string? LicExpDate { get; set; }

    [JsonPropertyName("DriveCoVehiclesYN")]
    public string? DriveCoVehiclesYN { get; set; }

    [JsonPropertyName("I9Status")]
    public string? I9Status { get; set; }

    [JsonPropertyName("I9Citizen")]
    public string? I9Citizen { get; set; }

    [JsonPropertyName("I9ReviewDate")]
    public string? I9ReviewDate { get; set; }

    [JsonPropertyName("TrainingBudget")]
    public string? TrainingBudget { get; set; }

    [JsonPropertyName("CafeteriaPlanBudget")]
    public string? CafeteriaPlanBudget { get; set; }

    [JsonPropertyName("HighSchool")]
    public string? HighSchool { get; set; }

    [JsonPropertyName("HSGradDate")]
    public string? HSGradDate { get; set; }

    [JsonPropertyName("College1")]
    public string? College1 { get; set; }

    [JsonPropertyName("College1BegDate")]
    public string? College1BegDate { get; set; }

    [JsonPropertyName("College1EndDate")]
    public string? College1EndDate { get; set; }

    [JsonPropertyName("College1Degree")]
    public string? College1Degree { get; set; }

    [JsonPropertyName("College2")]
    public string? College2 { get; set; }

    [JsonPropertyName("College2BegDate")]
    public string? College2BegDate { get; set; }

    [JsonPropertyName("College2EndDate")]
    public string? College2EndDate { get; set; }

    [JsonPropertyName("College2Degree")]
    public string? College2Degree { get; set; }

    [JsonPropertyName("ApplicationDate")]
    public string? ApplicationDate { get; set; }

    [JsonPropertyName("AvailableDate")]
    public string? AvailableDate { get; set; }

    [JsonPropertyName("LastContactDate")]
    public string? LastContactDate { get; set; }

    [JsonPropertyName("ContactPhone")]
    public string? ContactPhone { get; set; }

    [JsonPropertyName("AltContactPhone")]
    public string? AltContactPhone { get; set; }

    [JsonPropertyName("ExpectedSalary")]
    public string? ExpectedSalary { get; set; }

    [JsonPropertyName("Source")]
    public string? Source { get; set; }

    [JsonPropertyName("SourceCost")]
    public string? SourceCost { get; set; }

    [JsonPropertyName("CurrentEmployer")]
    public string? CurrentEmployer { get; set; }

    [JsonPropertyName("CurrentTime")]
    public string? CurrentTime { get; set; }

    [JsonPropertyName("PrevEmployer")]
    public string? PrevEmployer { get; set; }

    [JsonPropertyName("PrevTime")]
    public string? PrevTime { get; set; }

    [JsonPropertyName("NoContactEmplYN")]
    public string? NoContactEmplYN { get; set; }

    [JsonPropertyName("HistSeq")]
    public string? HistSeq { get; set; }

    [JsonPropertyName("Notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("ExistsInPR")]
    public string? ExistsInPR { get; set; }

    [JsonPropertyName("EarnCode")]
    public int? EarnCode { get; set; }

    [JsonPropertyName("PhotoName")]
    public string? PhotoName { get; set; }

    [JsonPropertyName("UniqueAttchID")]
    public string? UniqueAttchID { get; set; }

    [JsonPropertyName("TempWorker")]
    public string? TempWorker { get; set; }

    [JsonPropertyName("Email")]
    public string? Email { get; set; }

    [JsonPropertyName("Suffix")]
    public string? Suffix { get; set; }

    [JsonPropertyName("DisabledVetYN")]
    public string? DisabledVetYN { get; set; }

    [JsonPropertyName("VietnamVetYN")]
    public string? VietnamVetYN { get; set; }

    [JsonPropertyName("OtherVetYN")]
    public string? OtherVetYN { get; set; }

    [JsonPropertyName("VetDischargeDate")]
    public string? VetDischargeDate { get; set; }

    [JsonPropertyName("OccupCat")]
    public string? OccupCat { get; set; }

    [JsonPropertyName("CatStatus")]
    public string? CatStatus { get; set; }

    [JsonPropertyName("LicClass")]
    public string? LicClass { get; set; }

    [JsonPropertyName("DOLHireState")]
    public string? DOLHireState { get; set; }

    [JsonPropertyName("NonResAlienYN")]
    public string? NonResAlienYN { get; set; }

    [JsonPropertyName("KeyID")]
    public int? KeyID { get; set; }

    [JsonPropertyName("Country")]
    public string? Country { get; set; }

    [JsonPropertyName("LicCountry")]
    public string? LicCountry { get; set; }

    [JsonPropertyName("OTOpt")]
    public string? OTOpt { get; set; }

    [JsonPropertyName("OTSched")]
    public int? OTSched { get; set; }

    [JsonPropertyName("Shift")]
    public int? Shift { get; set; }

    [JsonPropertyName("PTOAppvrGrp")]
    public string? PTOAppvrGrp { get; set; }

    [JsonPropertyName("HDAmt")]
    public string? HDAmt { get; set; }

    [JsonPropertyName("F1Amt")]
    public string? F1Amt { get; set; }

    [JsonPropertyName("LCFStock")]
    public string? LCFStock { get; set; }

    [JsonPropertyName("LCPStock")]
    public string? LCPStock { get; set; }

    [JsonPropertyName("AFServiceMedalVetYN")]
    public string? AFServiceMedalVetYN { get; set; }

    [JsonPropertyName("WOTaxState")]
    public string? WOTaxState { get; set; }

    [JsonPropertyName("WOLocalCode")]
    public string? WOLocalCode { get; set; }

    [JsonPropertyName("StatusCode")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("LookBackGroup")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? LookBackGroup { get; set; }

    [JsonPropertyName("InitialMeasurementStartDate")]
    public string? InitialMeasurementStartDate { get; set; }

    [JsonPropertyName("InitialMeasurementEndDate")]
    public string? InitialMeasurementEndDate { get; set; }

    [JsonPropertyName("InitialAdministrativeStartDate")]
    public string? InitialAdministrativeStartDate { get; set; }

    [JsonPropertyName("InitialAdministrativeEndDate")]
    public string? InitialAdministrativeEndDate { get; set; }

    [JsonPropertyName("InitialStabilityStartDate")]
    public string? InitialStabilityStartDate { get; set; }

    [JsonPropertyName("InitialStabilityEndDate")]
    public string? InitialStabilityEndDate { get; set; }

    [JsonPropertyName("HiringLocationID")]
    public int? HiringLocationID { get; set; }

    [JsonPropertyName("ActiveDutyWartimeVetYN")]
    public string? ActiveDutyWartimeVetYN { get; set; }

    [JsonPropertyName("GenderIdentity")]
    public string? GenderIdentity { get; set; }

    [JsonPropertyName("GenderIdentityOther")]
    public string? GenderIdentityOther { get; set; }

    [JsonPropertyName("FederalTaxExemptYN")]
    public string? FederalTaxExemptYN { get; set; }

    [JsonPropertyName("__custom_fields")]
    public ViewpointCustomFields? CustomFields { get; set; }
}

public class ViewpointCustomFields
{
    [JsonPropertyName("udEmpLicClass")]
    public JsonElement? EmpLicClass { get; set; }

    [JsonPropertyName("udReturntoworkdate")]
    public JsonElement? ReturnToWorkDate { get; set; }

    [JsonPropertyName("udI9ONFile")]
    public string? I9OnFile { get; set; }

    [JsonPropertyName("udHRWLRC")]
    public JsonElement? HRWLRC { get; set; }

    [JsonPropertyName("udHRHireActAffidavitDate")]
    public JsonElement? HRHireActAffidavitDate { get; set; }

    [JsonPropertyName("udHRHireActQualIndividual")]
    public string? HRHireActQualIndividual { get; set; }

    [JsonPropertyName("udHREVerify")]
    public string? HREVerify { get; set; }

    [JsonPropertyName("udHRChart16")]
    public JsonElement? HRChart16 { get; set; }

    [JsonPropertyName("udAACheckBox")]
    public string? AACheckBox { get; set; }

    [JsonPropertyName("udHRAbstractDrugPool")]
    public JsonElement? HRAbstractDrugPool { get; set; }

    [JsonPropertyName("udHREVerifyDate")]
    public JsonElement? HREVerifyDate { get; set; }

    [JsonPropertyName("udHRDriverLicUpdated")]
    public JsonElement? HRDriverLicUpdated { get; set; }

    [JsonPropertyName("udCongDistrict")]
    public string? CongDistrict { get; set; }

    [JsonPropertyName("udStateSenateDistrict")]
    public string? StateSenateDistrict { get; set; }

    [JsonPropertyName("udStateHouseDistrict")]
    public string? StateHouseDistrict { get; set; }

    [JsonPropertyName("udCounty")]
    public JsonElement? County { get; set; }

    [JsonPropertyName("udApprenticeID")]
    public JsonElement? ApprenticeID { get; set; }

    [JsonPropertyName("udApprenticeWagePct")]
    public JsonElement? ApprenticeWagePct { get; set; }

    [JsonPropertyName("udWorkEmail")]
    public string? WorkEmail { get; set; }

    [JsonPropertyName("udWorkExt")]
    public JsonElement? WorkExt { get; set; }

    [JsonPropertyName("udWorkCell")]
    public string? WorkCell { get; set; }

    [JsonPropertyName("udPhysicalLocation")]
    public JsonElement? PhysicalLocation { get; set; }

    [JsonPropertyName("udNetworkUserID")]
    public string? NetworkUserID { get; set; }

    [JsonPropertyName("udNickname")]
    public JsonElement? Nickname { get; set; }

    [JsonPropertyName("udMathyNetPhone")]
    public JsonElement? MathyNetPhone { get; set; }

    [JsonPropertyName("udProtectedVeteran")]
    public string? ProtectedVeteran { get; set; }

    [JsonPropertyName("udFunction")]
    public JsonElement? Function { get; set; }

    [JsonPropertyName("udReservedUserID")]
    public JsonElement? ReservedUserID { get; set; }

    [JsonPropertyName("udBYODYN")]
    public string? BYODYN { get; set; }

    [JsonPropertyName("udBYODDate")]
    public JsonElement? BYODDate { get; set; }

    [JsonPropertyName("udMNLaw")]
    public string? MNLaw { get; set; }

    [JsonPropertyName("udMNWageNoticeDate")]
    public JsonElement? MNWageNoticeDate { get; set; }

    [JsonPropertyName("udCommsMethod")]
    public string? CommsMethod { get; set; }

    [JsonPropertyName("udBenefitElectionMethod")]
    public JsonElement? BenefitElectionMethod { get; set; }

    [JsonPropertyName("udPhotoRelease")]
    public string? PhotoRelease { get; set; }

    [JsonPropertyName("udAACompletionDate")]
    public JsonElement? AACompletionDate { get; set; }

    [JsonPropertyName("udHazMatExpire")]
    public JsonElement? HazMatExpire { get; set; }

    [JsonPropertyName("udElectronicConsentYN")]
    public string? ElectronicConsentYN { get; set; }

    [JsonPropertyName("udDAProfile")]
    public JsonElement? DAProfile { get; set; }

    [JsonPropertyName("udICARECompletionDate")]
    public JsonElement? ICARECompletionDate { get; set; }

    [JsonPropertyName("udICAREGroupID")]
    public JsonElement? ICAREGroupID { get; set; }

    [JsonPropertyName("udCOVIDTestExempt")]
    public string? COVIDTestExempt { get; set; }

    [JsonPropertyName("udPRWageBonusApprGrp")]
    public JsonElement? PRWageBonusApprGrp { get; set; }

    [JsonPropertyName("udPRWageEffectiveWeek")]
    public JsonElement? PRWageEffectiveWeek { get; set; }

    [JsonPropertyName("udCYElectionMethod")]
    public JsonElement? CYElectionMethod { get; set; }

    [JsonPropertyName("udWageBonusApprover")]
    public JsonElement? WageBonusApprover { get; set; }

    [JsonPropertyName("udSupervisor")]
    public JsonElement? SupervisorId { get; set; }

    // Helper method to safely extract string value from JsonElement properties
    public string? GetStringValue(JsonElement? element)
    {
        if (!element.HasValue)
            return null;
            
        var value = element.Value;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => value.GetRawText()
        };
    }
}

public class ViewpointEmployeesResponse
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("data")]
    public List<ViewpointEmployeeDto>? Data { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("continuation_token")]
    public string? ContinuationToken { get; set; }

    [JsonPropertyName("next_url")]
    public string? NextUrl { get; set; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
}
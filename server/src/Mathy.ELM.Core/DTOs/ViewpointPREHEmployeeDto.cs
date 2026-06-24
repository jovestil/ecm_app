using System.Text.Json;
using System.Text.Json.Serialization;
using Mathy.ELM.Core.Converters;

namespace Mathy.ELM.Core.DTOs;

public class ViewpointPREHEmployeeDto
{
    [JsonPropertyName("__ryvitKeys")]
    public List<string>? RyvitKeys { get; set; }

    [JsonPropertyName("__modifiedUTC")]
    public string? ModifiedUTC { get; set; }

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

    [JsonPropertyName("SSN")]
    public string? SSN { get; set; }

    [JsonPropertyName("Race")]
    public string? Race { get; set; }

    [JsonPropertyName("Sex")]
    public string? Sex { get; set; }

    [JsonPropertyName("BirthDate")]
    public string? BirthDate { get; set; }

    [JsonPropertyName("HireDate")]
    public string? HireDate { get; set; }

    [JsonPropertyName("TermDate")]
    public string? TermDate { get; set; }

    [JsonPropertyName("PRGroup")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? PRGroup { get; set; }

    [JsonPropertyName("PRDept")]
    public string? PRDept { get; set; }

    [JsonPropertyName("Craft")]
    public string? Craft { get; set; }

    [JsonPropertyName("Class")]
    public string? Class { get; set; }

    [JsonPropertyName("InsCode")]
    public string? InsCode { get; set; }

    [JsonPropertyName("TaxState")]
    public string? TaxState { get; set; }

    [JsonPropertyName("UnempState")]
    public string? UnempState { get; set; }

    [JsonPropertyName("InsState")]
    public string? InsState { get; set; }

    [JsonPropertyName("LocalCode")]
    public string? LocalCode { get; set; }

    [JsonPropertyName("GLCo")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? GLCo { get; set; }

    [JsonPropertyName("UseState")]
    public string? UseState { get; set; }

    [JsonPropertyName("UseLocal")]
    public string? UseLocal { get; set; }

    [JsonPropertyName("UseIns")]
    public string? UseIns { get; set; }

    [JsonPropertyName("JCCo")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? JCCo { get; set; }

    [JsonPropertyName("Job")]
    public string? Job { get; set; }

    [JsonPropertyName("Crew")]
    public string? Crew { get; set; }

    [JsonPropertyName("LastUpdated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("EarnCode")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? EarnCode { get; set; }

    [JsonPropertyName("HrlyRate")]
    public string? HrlyRate { get; set; }

    [JsonPropertyName("SalaryAmt")]
    public string? SalaryAmt { get; set; }

    [JsonPropertyName("OTOpt")]
    public string? OTOpt { get; set; }

    [JsonPropertyName("OTSched")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? OTSched { get; set; }

    [JsonPropertyName("JCFixedRate")]
    public string? JCFixedRate { get; set; }

    [JsonPropertyName("EMFixedRate")]
    public string? EMFixedRate { get; set; }

    [JsonPropertyName("YTDSUI")]
    public string? YTDSUI { get; set; }

    [JsonPropertyName("OccupCat")]
    public string? OccupCat { get; set; }

    [JsonPropertyName("CatStatus")]
    public string? CatStatus { get; set; }

    [JsonPropertyName("DirDeposit")]
    public string? DirDeposit { get; set; }

    [JsonPropertyName("RoutingId")]
    public string? RoutingId { get; set; }

    [JsonPropertyName("BankAcct")]
    public string? BankAcct { get; set; }

    [JsonPropertyName("AcctType")]
    public string? AcctType { get; set; }

    [JsonPropertyName("ActiveYN")]
    public string? ActiveYN { get; set; }

    [JsonPropertyName("PensionYN")]
    public string? PensionYN { get; set; }

    [JsonPropertyName("PostToAll")]
    public string? PostToAll { get; set; }

    [JsonPropertyName("CertYN")]
    public string? CertYN { get; set; }

    [JsonPropertyName("ChkSort")]
    public string? ChkSort { get; set; }

    [JsonPropertyName("AuditYN")]
    public string? AuditYN { get; set; }

    [JsonPropertyName("Notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("UniqueAttchID")]
    public string? UniqueAttchID { get; set; }

    [JsonPropertyName("DefaultPaySeq")]
    public string? DefaultPaySeq { get; set; }

    [JsonPropertyName("DDPaySeq")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? DDPaySeq { get; set; }

    [JsonPropertyName("Suffix")]
    public string? Suffix { get; set; }

    [JsonPropertyName("TradeSeq")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? TradeSeq { get; set; }

    [JsonPropertyName("CSLimit")]
    public string? CSLimit { get; set; }

    [JsonPropertyName("CSGarnGroup")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? CSGarnGroup { get; set; }

    [JsonPropertyName("CSAllocMethod")]
    public string? CSAllocMethod { get; set; }

    [JsonPropertyName("Shift")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? Shift { get; set; }

    [JsonPropertyName("NonResAlienYN")]
    public string? NonResAlienYN { get; set; }

    [JsonPropertyName("KeyID")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? KeyID { get; set; }

    [JsonPropertyName("Country")]
    public string? Country { get; set; }

    [JsonPropertyName("HDAmt")]
    public string? HDAmt { get; set; }

    [JsonPropertyName("F1Amt")]
    public string? F1Amt { get; set; }

    [JsonPropertyName("LCFStock")]
    public string? LCFStock { get; set; }

    [JsonPropertyName("LCPStock")]
    public string? LCPStock { get; set; }

    [JsonPropertyName("NAICS")]
    public string? NAICS { get; set; }

    [JsonPropertyName("EMCo")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? EMCo { get; set; }

    [JsonPropertyName("Equipment")]
    public string? Equipment { get; set; }

    [JsonPropertyName("EMGroup")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? EMGroup { get; set; }

    [JsonPropertyName("PayMethodDelivery")]
    public string? PayMethodDelivery { get; set; }

    [JsonPropertyName("AUEFTYN")]
    public string? AUEFTYN { get; set; }

    [JsonPropertyName("AUAccountNumber")]
    public string? AUAccountNumber { get; set; }

    [JsonPropertyName("AUBSB")]
    public string? AUBSB { get; set; }

    [JsonPropertyName("AUReference")]
    public string? AUReference { get; set; }

    [JsonPropertyName("CPPQPPExempt")]
    public string? CPPQPPExempt { get; set; }

    [JsonPropertyName("EIExempt")]
    public string? EIExempt { get; set; }

    [JsonPropertyName("PPIPExempt")]
    public string? PPIPExempt { get; set; }

    [JsonPropertyName("TimesheetRevGroup")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? TimesheetRevGroup { get; set; }

    [JsonPropertyName("UpdatePRAEYN")]
    public string? UpdatePRAEYN { get; set; }

    [JsonPropertyName("WOTaxState")]
    public string? WOTaxState { get; set; }

    [JsonPropertyName("WOLocalCode")]
    public string? WOLocalCode { get; set; }

    [JsonPropertyName("UseUnempState")]
    public string? UseUnempState { get; set; }

    [JsonPropertyName("UseInsState")]
    public string? UseInsState { get; set; }

    [JsonPropertyName("NewHireActStartDate")]
    public string? NewHireActStartDate { get; set; }

    [JsonPropertyName("NewHireActEndDate")]
    public string? NewHireActEndDate { get; set; }

    [JsonPropertyName("ArrearsActiveYN")]
    public string? ArrearsActiveYN { get; set; }

    [JsonPropertyName("RecentRehireDate")]
    public string? RecentRehireDate { get; set; }

    [JsonPropertyName("RecentSeparationDate")]
    public string? RecentSeparationDate { get; set; }

    [JsonPropertyName("SeparationRedundancyRetirement")]
    public string? SeparationRedundancyRetirement { get; set; }

    [JsonPropertyName("SeparationReason")]
    public string? SeparationReason { get; set; }

    [JsonPropertyName("SeparationReasonExplanation")]
    public string? SeparationReasonExplanation { get; set; }

    [JsonPropertyName("SMFixedRate")]
    public string? SMFixedRate { get; set; }

    [JsonPropertyName("WeeklyHours")]
    public string? WeeklyHours { get; set; }

    [JsonPropertyName("ETPPostedYN")]
    public string? ETPPostedYN { get; set; }

    [JsonPropertyName("AdditionalSourceDedns")]
    public string? AdditionalSourceDedns { get; set; }

    [JsonPropertyName("AuthorizedSourceDedns")]
    public string? AuthorizedSourceDedns { get; set; }

    [JsonPropertyName("ExemptHealthContribution")]
    public string? ExemptHealthContribution { get; set; }

    [JsonPropertyName("AlwaysCalcQPIP")]
    public string? AlwaysCalcQPIP { get; set; }

    [JsonPropertyName("PrintInFrench")]
    public string? PrintInFrench { get; set; }

    [JsonPropertyName("CAProgramAccount")]
    public string? CAProgramAccount { get; set; }

    [JsonPropertyName("QCFileNumber")]
    public string? QCFileNumber { get; set; }

    [JsonPropertyName("EmailW2YN")]
    public string? EmailW2YN { get; set; }

    [JsonPropertyName("Email1095CYN")]
    public string? Email1095CYN { get; set; }

    [JsonPropertyName("EmailT4YN")]
    public string? EmailT4YN { get; set; }

    [JsonPropertyName("EmailPAYGSumYN")]
    public string? EmailPAYGSumYN { get; set; }

    [JsonPropertyName("PAYGIncomeType")]
    public string? PAYGIncomeType { get; set; }

    [JsonPropertyName("StatusIndianYN")]
    public string? StatusIndianYN { get; set; }

    [JsonPropertyName("APVendorGroup")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? APVendorGroup { get; set; }

    [JsonPropertyName("APVendor")]
    public string? APVendor { get; set; }

    [JsonPropertyName("PRUpdateAPVMYN")]
    public string? PRUpdateAPVMYN { get; set; }

    [JsonPropertyName("JobKeeperStartFN")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? JobKeeperStartFN { get; set; }

    [JsonPropertyName("JobKeeperFinishFN")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? JobKeeperFinishFN { get; set; }

    [JsonPropertyName("JobKeeperTierLevel")]
    public string? JobKeeperTierLevel { get; set; }

    [JsonPropertyName("GeographicCode")]
    public string? GeographicCode { get; set; }

    [JsonPropertyName("JobMakerNominate")]
    public string? JobMakerNominate { get; set; }

    [JsonPropertyName("JobMakerRenominate")]
    public string? JobMakerRenominate { get; set; }

    [JsonPropertyName("JobMakerEligPeriod01")]
    public string? JobMakerEligPeriod01 { get; set; }

    [JsonPropertyName("JobMakerEligPeriod02")]
    public string? JobMakerEligPeriod02 { get; set; }

    [JsonPropertyName("JobMakerEligPeriod03")]
    public string? JobMakerEligPeriod03 { get; set; }

    [JsonPropertyName("JobMakerEligPeriod04")]
    public string? JobMakerEligPeriod04 { get; set; }

    [JsonPropertyName("JobMakerEligPeriod05")]
    public string? JobMakerEligPeriod05 { get; set; }

    [JsonPropertyName("JobMakerEligPeriod06")]
    public string? JobMakerEligPeriod06 { get; set; }

    [JsonPropertyName("JobMakerEligPeriod07")]
    public string? JobMakerEligPeriod07 { get; set; }

    [JsonPropertyName("JobMakerEligPeriod08")]
    public string? JobMakerEligPeriod08 { get; set; }

    [JsonPropertyName("GenderIdentity")]
    public string? GenderIdentity { get; set; }

    [JsonPropertyName("GenderIdentityOther")]
    public string? GenderIdentityOther { get; set; }

    [JsonPropertyName("EmploymentBasis")]
    public string? EmploymentBasis { get; set; }

    [JsonPropertyName("CessationType")]
    public string? CessationType { get; set; }

    [JsonPropertyName("MedicareLevySurchargeYN")]
    public string? MedicareLevySurchargeYN { get; set; }

    [JsonPropertyName("MedicareLevySurchargeTier")]
    public string? MedicareLevySurchargeTier { get; set; }

    [JsonPropertyName("MedicareLevyReductionYN")]
    public string? MedicareLevyReductionYN { get; set; }

    [JsonPropertyName("IncomeStreamCountry")]
    public string? IncomeStreamCountry { get; set; }

    [JsonPropertyName("FederalTaxExemptYN")]
    public string? FederalTaxExemptYN { get; set; }

    [JsonPropertyName("DentalBenefitsCode")]
    public string? DentalBenefitsCode { get; set; }

    [JsonPropertyName("__custom_fields")]
    public ViewpointPREHCustomFields? CustomFields { get; set; }
}

public class ViewpointPREHCustomFields
{
    [JsonPropertyName("udCellPhoneCarrier")]
    public string? CellPhoneCarrier { get; set; }
}
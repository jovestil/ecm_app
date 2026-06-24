using System.Text.Json.Serialization;

namespace Mathy.ELM.Core.DTOs;

public class ViewpointCompanyDto
{
    [JsonPropertyName("__ryvitKeys")]
    public List<string>? RyvitKeys { get; set; }

    [JsonPropertyName("__modifiedUTC")]
    public string? ModifiedUTC { get; set; }

    [JsonPropertyName("HQCo")]
    public int? HQCo { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

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

    [JsonPropertyName("FedTaxId")]
    public string? FedTaxId { get; set; }

    [JsonPropertyName("Phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("Fax")]
    public string? Fax { get; set; }

    [JsonPropertyName("VendorGroup")]
    public int? VendorGroup { get; set; }

    [JsonPropertyName("MatlGroup")]
    public int? MatlGroup { get; set; }

    [JsonPropertyName("PhaseGroup")]
    public int? PhaseGroup { get; set; }

    [JsonPropertyName("CustGroup")]
    public int? CustGroup { get; set; }

    [JsonPropertyName("TaxGroup")]
    public int? TaxGroup { get; set; }

    [JsonPropertyName("EMGroup")]
    public int? EMGroup { get; set; }

    [JsonPropertyName("Vendor")]
    public int? Vendor { get; set; }

    [JsonPropertyName("Customer")]
    public int? Customer { get; set; }

    [JsonPropertyName("AuditCoParams")]
    public string? AuditCoParams { get; set; }

    [JsonPropertyName("AuditTax")]
    public string? AuditTax { get; set; }

    [JsonPropertyName("AuditMatl")]
    public string? AuditMatl { get; set; }

    [JsonPropertyName("Notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("UniqueAttchID")]
    public string? UniqueAttchID { get; set; }

    [JsonPropertyName("ShopGroup")]
    public int? ShopGroup { get; set; }

    [JsonPropertyName("STEmpId")]
    public string? STEmpId { get; set; }

    [JsonPropertyName("MenuImage")]
    public string? MenuImage { get; set; }

    [JsonPropertyName("KeyID")]
    public int? KeyID { get; set; }

    [JsonPropertyName("Country")]
    public string? Country { get; set; }

    [JsonPropertyName("DefaultCountry")]
    public string? DefaultCountry { get; set; }

    [JsonPropertyName("ReportDateFormat")]
    public int? ReportDateFormat { get; set; }

    [JsonPropertyName("ContactGroup")]
    public int? ContactGroup { get; set; }

    [JsonPropertyName("AuditContact")]
    public string? AuditContact { get; set; }

    [JsonPropertyName("DFId")]
    public string? DFId { get; set; }

    [JsonPropertyName("CurrencyID")]
    public string? CurrencyID { get; set; }

    [JsonPropertyName("MaskId")]
    public string? MaskId { get; set; }

    [JsonPropertyName("QuebecEmployerID")]
    public string? QuebecEmployerID { get; set; }

    [JsonPropertyName("DUNS")]
    public string? DUNS { get; set; }

    [JsonPropertyName("NAICSCode")]
    public string? NAICSCode { get; set; }

    [JsonPropertyName("__custom_fields")]
    public ViewpointCompanyCustomFields? CustomFields { get; set; }
}

public class ViewpointCompanyCustomFields
{
    [JsonPropertyName("udJWSBackfill")]
    public string? JWSBackfill { get; set; }

    [JsonPropertyName("udNAICS")]
    public string? NAICS { get; set; }

    [JsonPropertyName("udUseInDCA")]
    public string? UseInDCA { get; set; }

    [JsonPropertyName("udCOVIDTestingRequiredYN")]
    public string? COVIDTestingRequiredYN { get; set; }

    [JsonPropertyName("udActivePRCo")]
    public string? ActivePRCo { get; set; }
}
namespace Mathy.ELM.Core.Configuration;

public class ViewpointApiSettings
{
    public const string SectionName = "ViewpointApi";

    public string BaseUrl { get; set; } = string.Empty;
    public string SubscriberCode { get; set; } = string.Empty;
    public string ApplicationKey { get; set; } = string.Empty;
    public string ActionVerificationUrl { get; set; } = string.Empty;
}
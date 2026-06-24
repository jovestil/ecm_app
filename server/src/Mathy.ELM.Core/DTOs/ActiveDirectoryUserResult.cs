namespace Mathy.ELM.Core.DTOs;

public class ActiveDirectoryUserResult
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool SamePersonExists { get; set; }
}

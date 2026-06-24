using System.DirectoryServices.AccountManagement;
using Mathy.ELM.Core.DTOs;

namespace Mathy.ELM.Infrastructure.Services;

public class AdUserService
{
    private readonly string _domain;
    private readonly string? _ouPath;
    private readonly string _adUsername;
    private readonly string _adPassword;

    public AdUserService(string domain, string? ouPath, string adUsername, string adPassword)
    {
        _domain = domain;
        _ouPath = ouPath;
        _adUsername = adUsername;
        _adPassword = adPassword;
    }

    /// <summary>
    /// Checks AD recursively for the given username.
    /// If it exists and belongs to a different person, increments and recurses until finding an available one.
    /// If it belongs to the same person (matching DisplayName), returns SamePersonExists = true.
    /// </summary>
    public ActiveDirectoryUserResult GenerateUniqueUserId(string username, string expectedDisplayName)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty.", nameof(username));

        // Parse base name and starting counter from the DB-generated username
        // GenerateUsernameAsync always produces a 3-digit suffix (D3 format),
        // so split on the last 3 characters to avoid truncating names that contain digits
        // e.g., "kim003" -> baseName="kim", counter=3
        //        "kim101001" -> baseName="kim101", counter=1
        const int digitCount = 3;
        if (username.Length <= digitCount)
            throw new ArgumentException($"Username '{username}' is too short to contain a valid suffix.", nameof(username));

        var baseName = username.Substring(0, username.Length - digitCount);
        var suffix = username.Substring(username.Length - digitCount);
        int counter = int.Parse(suffix);

        return FindAvailableUserId(baseName, counter, digitCount, expectedDisplayName, lastDisplayName: string.Empty);
    }

    /// <summary>
    /// Recursively checks AD for the candidate UserId.
    /// - If it does not exist → returns as available (base case).
    /// - If it exists and DisplayName matches expectedDisplayName → returns SamePersonExists = true (base case).
    /// - If it exists but belongs to a different person → increments counter and recurses.
    /// </summary>
    private ActiveDirectoryUserResult FindAvailableUserId(
        string baseName, int counter, int digitCount, string expectedDisplayName, string lastDisplayName)
    {
        if (counter > 999)
            throw new InvalidOperationException(
                $"Exhausted all possible UserIds for '{baseName}'.");

        string candidateUserId = $"{baseName}{counter.ToString($"D{digitCount}")}";

        var existingUser = FindUserInAd(candidateUserId);

        if (existingUser == null)
        {
            // Base case: UserId does not exist in AD — safe to use
            return new ActiveDirectoryUserResult
            {
                UserId = candidateUserId,
                DisplayName = lastDisplayName,
                SamePersonExists = false
            };
        }

        // Read display name before disposing
        var adDisplayName = existingUser.DisplayName?.Trim() ?? string.Empty;
        existingUser.Dispose();

        // Base case: same person already exists in AD
        if (string.Equals(adDisplayName, expectedDisplayName, StringComparison.OrdinalIgnoreCase))
        {
            return new ActiveDirectoryUserResult
            {
                UserId = candidateUserId,
                DisplayName = adDisplayName,
                SamePersonExists = true
            };
        }

        // Recursive case: different person — increment and try again
        return FindAvailableUserId(baseName, counter + 1, digitCount, expectedDisplayName, adDisplayName);
    }

    private UserPrincipal? FindUserInAd(string samAccountName)
    {
        using var context = new PrincipalContext(ContextType.Domain, _domain, _ouPath, _adUsername, _adPassword);
        return UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, samAccountName);
    }
}

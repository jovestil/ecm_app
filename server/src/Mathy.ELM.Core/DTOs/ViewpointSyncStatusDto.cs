namespace Mathy.ELM.Core.DTOs;

public class ViewpointSyncStatusDto
{
    public DateTime? LastCompanySync { get; set; }
    public DateTime? LastDepartmentSync { get; set; }
    public DateTime? LastPositionSync { get; set; }
    public DateTime? LastPayrollGroupSync { get; set; }
    public DateTime? LastUnionCraftSync { get; set; }
    public DateTime? LastEmploymentStatusSync { get; set; }
    public DateTime? LastEmployeeSalaryTypeSync { get; set; }
    public DateTime? LastEmployeeSync { get; set; }

    public int TotalCompanies { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalPositions { get; set; }
    public int TotalPayrollGroups { get; set; }
    public int TotalUnionCrafts { get; set; }
    public int TotalEmploymentStatuses { get; set; }
    public int TotalEmployeeSalaryTypes { get; set; }
    public int TotalEmployees { get; set; }

    public string CompanySyncStatus => GetSyncStatusText(LastCompanySync);
    public string DepartmentSyncStatus => GetSyncStatusText(LastDepartmentSync);
    public string PositionSyncStatus => GetSyncStatusText(LastPositionSync);
    public string PayrollGroupSyncStatus => GetSyncStatusText(LastPayrollGroupSync);
    public string UnionCraftSyncStatus => GetSyncStatusText(LastUnionCraftSync);
    public string EmploymentStatusSyncStatus => GetSyncStatusText(LastEmploymentStatusSync);
    public string EmployeeSalaryTypeSyncStatus => GetSyncStatusText(LastEmployeeSalaryTypeSync);
    public string EmployeeSyncStatus => GetSyncStatusText(LastEmployeeSync);
    
    private static string GetSyncStatusText(DateTime? lastSync)
    {
        if (!lastSync.HasValue)
            return "Never synced";
            
        var timeDiff = DateTime.UtcNow - lastSync.Value;
        
        return timeDiff.TotalDays switch
        {
            < 1 => "Synced today",
            < 7 => $"Synced {(int)timeDiff.TotalDays} days ago",
            < 30 => $"Synced {(int)(timeDiff.TotalDays / 7)} weeks ago",
            _ => $"Synced {(int)(timeDiff.TotalDays / 30)} months ago"
        };
    }
}
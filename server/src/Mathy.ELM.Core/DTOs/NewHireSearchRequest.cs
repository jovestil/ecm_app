using System.ComponentModel.DataAnnotations;

namespace Mathy.ELM.Core.DTOs;

public class NewHireSearchRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "HRCo must be a positive integer")]
    public int HRCo { get; set; }

    [Required(ErrorMessage = "PRDept is required")]
    public string PRDept { get; set; } = string.Empty;

    [Required(ErrorMessage = "LastName is required")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "HireDate is required")]
    public string HireDate { get; set; } = string.Empty;
}
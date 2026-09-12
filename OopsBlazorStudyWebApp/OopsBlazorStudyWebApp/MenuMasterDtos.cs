using System.ComponentModel.DataAnnotations;

namespace OopsBlazorStudyWebApp;

public sealed record MenuMasterListDto(
    int Id,
    string MenuName,
    string Status,
    string CreatedBy,
    DateTime CreatedOn);

public sealed class MenuMasterSaveRequest : IValidatableObject
{
    [Required]
    public string MenuName { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "Activate";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.Equals(Status, "Activate", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Status, "DeActivate", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("Status must be Activate or DeActivate.", new[] { nameof(Status) });
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace OopsBlazorStudyWebApp;

public sealed record RoleMasterListDto(
    int RoleId,
    string RoleName,
    string RoleCode,
    string LandingPage,
    string Status,
    string Remark,
    string MenuRights,
    int MenuRightCount,
    DateTime CreatedOn,
    DateTime? UpdatedOn);

public sealed record RoleMasterDetailDto(
    int RoleId,
    string RoleName,
    string RoleCode,
    string LandingPage,
    string Status,
    string Remark,
    bool IsActive,
    List<int> SelectedMenuIds);

public sealed class RoleMasterSaveRequest : IValidatableObject
{
    [Required]
    public string RoleName { get; set; } = string.Empty;

    [Required]
    public string RoleCode { get; set; } = string.Empty;

    [Required]
    public string LandingPage { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "Active";

    public string Remark { get; set; } = string.Empty;

    public List<int> SelectedMenuIds { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Status, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("Status must be Active or Inactive.", new[] { nameof(Status) });
        }

        if (SelectedMenuIds.Count == 0)
        {
            yield return new ValidationResult("Select at least one menu right.", new[] { nameof(SelectedMenuIds) });
        }
    }
}

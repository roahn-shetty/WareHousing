using System.ComponentModel.DataAnnotations;

namespace OopsBlazorStudyWebApp;

public sealed record RoleLookupDto(int RoleId, string RoleName, string RoleCode);

public sealed record UserMasterListDto(
    int UserMasterId,
    string UserId,
    string UserName,
    string EmailAddress,
    string MobileNo,
    string Remark,
    bool IsActive,
    string SelectedRoles,
    int RoleCount,
    DateTime CreatedOn,
    DateTime? UpdatedOn);

public sealed record UserMasterDetailDto(
    int UserMasterId,
    string UserId,
    string UserName,
    string Password,
    string EmailAddress,
    string MobileNo,
    string Remark,
    bool IsActive,
    List<int> SelectedRoleIds);

public sealed class UserMasterSaveRequest : IValidatableObject
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Password and Confirm Password must match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string EmailAddress { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Enter a valid mobile number.")]
    public string MobileNo { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<int> SelectedRoleIds { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SelectedRoleIds.Count == 0)
        {
            yield return new ValidationResult("Select at least one role.", new[] { nameof(SelectedRoleIds) });
        }
    }
}

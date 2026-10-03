namespace OopsBlazorStudyWebApp;

public sealed record LoginUserDto(
    int UserMasterId,
    string UserId,
    string UserName,
    byte[] PasswordHash,
    byte[] PasswordSalt,
    bool IsActive);

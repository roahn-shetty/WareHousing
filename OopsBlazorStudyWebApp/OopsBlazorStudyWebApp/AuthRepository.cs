using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace OopsBlazorStudyWebApp;

public interface IAuthRepository
{
    Task<LoginUserDto?> ValidateUserAsync(string userId, string password, CancellationToken cancellationToken = default);
}

public sealed class SqlAuthRepository : IAuthRepository
{
    private readonly string _connectionString;

    public SqlAuthRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("WarehouseDb")
            ?? throw new InvalidOperationException("Connection string 'WarehouseDb' was not found.");
    }

    public async Task<LoginUserDto?> ValidateUserAsync(string userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var candidateHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            user.PasswordSalt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return CryptographicOperations.FixedTimeEquals(candidateHash, user.PasswordHash)
            ? user
            : null;
    }

    private async Task<LoginUserDto?> GetUserAsync(string userId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT
                UserMasterId,
                UserId,
                UserName,
                PasswordHash,
                PasswordSalt,
                IsActive
            FROM dbo.UserMaster
            WHERE UserId = @UserId;
            """,
            connection);

        command.Parameters.AddWithValue("@UserId", userId.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new LoginUserDto(
            reader.GetInt32(reader.GetOrdinal("UserMasterId")),
            reader.GetString(reader.GetOrdinal("UserId")),
            reader.GetString(reader.GetOrdinal("UserName")),
            (byte[])reader["PasswordHash"],
            (byte[])reader["PasswordSalt"],
            reader.GetBoolean(reader.GetOrdinal("IsActive")));
    }
}

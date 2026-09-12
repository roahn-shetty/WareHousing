using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace OopsBlazorStudyWebApp;

public interface IUserMasterRepository
{
    Task<IReadOnlyList<RoleLookupDto>> GetActiveRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserMasterListDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserMasterDetailDto?> GetUserByIdAsync(int userMasterId, CancellationToken cancellationToken = default);
    Task<int> CreateUserAsync(UserMasterSaveRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserAsync(int userMasterId, UserMasterSaveRequest request, CancellationToken cancellationToken = default);
}

public sealed class SqlUserMasterRepository : IUserMasterRepository
{
    private readonly string _connectionString;

    public SqlUserMasterRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("WarehouseDb")
            ?? throw new InvalidOperationException("Connection string 'WarehouseDb' was not found.");
    }

    public async Task<IReadOnlyList<RoleLookupDto>> GetActiveRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = new List<RoleLookupDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.usp_RoleMaster_GetActive", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            roles.Add(new RoleLookupDto(
                reader.GetInt32(reader.GetOrdinal("RoleId")),
                reader.GetString(reader.GetOrdinal("RoleName")),
                reader.GetString(reader.GetOrdinal("RoleCode"))));
        }

        return roles;
    }

    public async Task<IReadOnlyList<UserMasterListDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<UserMasterListDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.usp_UserMaster_GetAll", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(ReadUserListItem(reader));
        }

        return users;
    }

    public async Task<UserMasterDetailDto?> GetUserByIdAsync(int userMasterId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.usp_UserMaster_GetById", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@UserMasterId", userMasterId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var detail = new UserMasterDetailDto(
            reader.GetInt32(reader.GetOrdinal("UserMasterId")),
            reader.GetString(reader.GetOrdinal("UserId")),
            reader.GetString(reader.GetOrdinal("UserName")),
            reader.GetString(reader.GetOrdinal("Password")),
            reader.GetString(reader.GetOrdinal("EmailAddress")),
            reader.GetString(reader.GetOrdinal("MobileNo")),
            reader.IsDBNull(reader.GetOrdinal("Remark")) ? string.Empty : reader.GetString(reader.GetOrdinal("Remark")),
            reader.GetBoolean(reader.GetOrdinal("IsActive")),
            new List<int>());

        var roleIds = new List<int>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                roleIds.Add(reader.GetInt32(reader.GetOrdinal("RoleId")));
            }
        }

        return detail with { SelectedRoleIds = roleIds };
    }

    public async Task<int> CreateUserAsync(UserMasterSaveRequest request, CancellationToken cancellationToken = default)
    {
        var (passwordHash, passwordSalt) = HashPassword(request.Password);
        var roleIds = string.Join(",", request.SelectedRoleIds.Distinct().OrderBy(x => x));

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.usp_UserMaster_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddUserParameters(command, request, passwordHash, passwordSalt, roleIds);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<bool> UpdateUserAsync(int userMasterId, UserMasterSaveRequest request, CancellationToken cancellationToken = default)
    {
        var (passwordHash, passwordSalt) = await GetExistingPasswordAsync(userMasterId, cancellationToken);
        var roleIds = request.SelectedRoleIds.Distinct().OrderBy(x => x).ToList();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var updateCommand = new SqlCommand(
                """
                UPDATE dbo.UserMaster
                SET
                    UserId = @UserId,
                    UserName = @UserName,
                    PasswordHash = @PasswordHash,
                    PasswordSalt = @PasswordSalt,
                    EmailAddress = @EmailAddress,
                    MobileNo = @MobileNo,
                    Remark = @Remark,
                    IsActive = @IsActive,
                    UpdatedOn = SYSUTCDATETIME()
                WHERE UserMasterId = @UserMasterId;
                """,
                connection,
                transaction);

            updateCommand.Parameters.AddWithValue("@UserMasterId", userMasterId);
            AddUserParameters(updateCommand, request, passwordHash, passwordSalt);

            var rowsAffected = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await using var deleteCommand = new SqlCommand(
                "DELETE FROM dbo.UserMasterRoleMap WHERE UserMasterId = @UserMasterId;",
                connection,
                transaction);
            deleteCommand.Parameters.AddWithValue("@UserMasterId", userMasterId);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

            foreach (var roleId in roleIds)
            {
                await using var roleCommand = new SqlCommand(
                    "INSERT INTO dbo.UserMasterRoleMap (UserMasterId, RoleId) VALUES (@UserMasterId, @RoleId);",
                    connection,
                    transaction);
                roleCommand.Parameters.AddWithValue("@UserMasterId", userMasterId);
                roleCommand.Parameters.AddWithValue("@RoleId", roleId);
                await roleCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void AddUserParameters(SqlCommand command, UserMasterSaveRequest request, byte[] passwordHash, byte[] passwordSalt, string roleIds)
    {
        AddUserParameters(command, request, passwordHash, passwordSalt);
        command.Parameters.AddWithValue("@SelectedRoleIds", roleIds);
    }

    private static void AddUserParameters(SqlCommand command, UserMasterSaveRequest request, byte[] passwordHash, byte[] passwordSalt)
    {
        command.Parameters.AddWithValue("@UserId", request.UserId);
        command.Parameters.AddWithValue("@UserName", request.UserName);
        command.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 32).Value = passwordHash;
        command.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 16).Value = passwordSalt;
        command.Parameters.AddWithValue("@EmailAddress", request.EmailAddress);
        command.Parameters.AddWithValue("@MobileNo", request.MobileNo);
        command.Parameters.AddWithValue("@Remark", string.IsNullOrWhiteSpace(request.Remark) ? DBNull.Value : request.Remark);
        command.Parameters.AddWithValue("@IsActive", request.IsActive);
    }

    private static UserMasterListDto ReadUserListItem(SqlDataReader reader)
    {
        return new UserMasterListDto(
            reader.GetInt32(reader.GetOrdinal("UserMasterId")),
            reader.GetString(reader.GetOrdinal("UserId")),
            reader.GetString(reader.GetOrdinal("UserName")),
            reader.GetString(reader.GetOrdinal("EmailAddress")),
            reader.GetString(reader.GetOrdinal("MobileNo")),
            reader.IsDBNull(reader.GetOrdinal("Remark")) ? string.Empty : reader.GetString(reader.GetOrdinal("Remark")),
            reader.GetBoolean(reader.GetOrdinal("IsActive")),
            reader.IsDBNull(reader.GetOrdinal("SelectedRoles")) ? string.Empty : reader.GetString(reader.GetOrdinal("SelectedRoles")),
            reader.GetInt32(reader.GetOrdinal("RoleCount")),
            reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            reader.IsDBNull(reader.GetOrdinal("UpdatedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedOn")));
    }

    private static (byte[] Hash, byte[] Salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return (hash, salt);
    }

    private async Task<(byte[] Hash, byte[] Salt)> GetExistingPasswordAsync(int userMasterId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT PasswordHash, PasswordSalt FROM dbo.UserMaster WHERE UserMasterId = @UserMasterId",
            connection);
        command.Parameters.AddWithValue("@UserMasterId", userMasterId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("User record was not found.");
        }

        return (
            (byte[])reader["PasswordHash"],
            (byte[])reader["PasswordSalt"]);
    }
}

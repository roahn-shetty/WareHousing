using System.Data;
using Microsoft.Data.SqlClient;

namespace OopsBlazorStudyWebApp;

public interface IRoleMasterRepository
{
    Task<IReadOnlyList<RoleMasterListDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleMasterDetailDto?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken = default);
    Task<int> CreateRoleAsync(RoleMasterSaveRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateRoleAsync(int roleId, RoleMasterSaveRequest request, CancellationToken cancellationToken = default);
}

public sealed class SqlRoleMasterRepository : IRoleMasterRepository
{
    private readonly string _connectionString;

    public SqlRoleMasterRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("WarehouseDb")
            ?? throw new InvalidOperationException("Connection string 'WarehouseDb' was not found.");
    }

    public async Task<IReadOnlyList<RoleMasterListDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = new List<RoleMasterListDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.usp_RoleMaster_GetAll", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            roles.Add(ReadRoleListItem(reader));
        }

        return roles;
    }

    public async Task<RoleMasterDetailDto?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.usp_RoleMaster_GetById", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@RoleId", roleId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var detail = new RoleMasterDetailDto(
            reader.GetInt32(reader.GetOrdinal("RoleId")),
            reader.GetString(reader.GetOrdinal("RoleName")),
            reader.GetString(reader.GetOrdinal("RoleCode")),
            reader.GetString(reader.GetOrdinal("LandingPage")),
            reader.GetString(reader.GetOrdinal("Status")),
            reader.IsDBNull(reader.GetOrdinal("Remark")) ? string.Empty : reader.GetString(reader.GetOrdinal("Remark")),
            reader.GetBoolean(reader.GetOrdinal("IsActive")),
            new List<int>());

        var menuIds = new List<int>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                menuIds.Add(reader.GetInt32(reader.GetOrdinal("MenuId")));
            }
        }

        return detail with { SelectedMenuIds = menuIds };
    }

    public async Task<int> CreateRoleAsync(RoleMasterSaveRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.usp_RoleMaster_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddRoleParameters(command, request);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<bool> UpdateRoleAsync(int roleId, RoleMasterSaveRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.usp_RoleMaster_Update", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@RoleId", roleId);
        AddRoleParameters(command, request);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private static void AddRoleParameters(SqlCommand command, RoleMasterSaveRequest request)
    {
        var menuIds = string.Join(",", request.SelectedMenuIds.Distinct().OrderBy(menuId => menuId));

        command.Parameters.Add("@RoleName", SqlDbType.NVarChar, 100).Value = request.RoleName.Trim();
        command.Parameters.Add("@RoleCode", SqlDbType.NVarChar, 30).Value = request.RoleCode.Trim();
        command.Parameters.Add("@LandingPage", SqlDbType.NVarChar, 100).Value = request.LandingPage.Trim();
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = request.Status.Trim();
        command.Parameters.Add("@Remark", SqlDbType.NVarChar, 500).Value = string.IsNullOrWhiteSpace(request.Remark) ? DBNull.Value : request.Remark.Trim();
        command.Parameters.Add("@SelectedMenuIds", SqlDbType.NVarChar, -1).Value = menuIds;
    }

    private static RoleMasterListDto ReadRoleListItem(SqlDataReader reader) =>
        new(
            reader.GetInt32(reader.GetOrdinal("RoleId")),
            reader.GetString(reader.GetOrdinal("RoleName")),
            reader.GetString(reader.GetOrdinal("RoleCode")),
            reader.GetString(reader.GetOrdinal("LandingPage")),
            reader.GetString(reader.GetOrdinal("Status")),
            reader.IsDBNull(reader.GetOrdinal("Remark")) ? string.Empty : reader.GetString(reader.GetOrdinal("Remark")),
            reader.IsDBNull(reader.GetOrdinal("MenuRights")) ? string.Empty : reader.GetString(reader.GetOrdinal("MenuRights")),
            reader.GetInt32(reader.GetOrdinal("MenuRightCount")),
            reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            reader.IsDBNull(reader.GetOrdinal("UpdatedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedOn")));
}

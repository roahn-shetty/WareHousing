using System.Data;
using Microsoft.Data.SqlClient;

namespace OopsBlazorStudyWebApp;

public interface IMenuMasterRepository
{
    Task<IReadOnlyList<MenuMasterListDto>> GetMenusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuMasterListDto>> GetMenusForUserAsync(int userMasterId, CancellationToken cancellationToken = default);
    Task<int> CreateMenuAsync(MenuMasterSaveRequest request, string createdBy, DateTime createdOn, CancellationToken cancellationToken = default);
    Task<bool> UpdateMenuStatusAsync(int id, MenuMasterUpdateRequest request, string updatedBy, DateTime updatedOn, CancellationToken cancellationToken = default);
}

public sealed class SqlMenuMasterRepository : IMenuMasterRepository
{
    private readonly string _connectionString;

    public SqlMenuMasterRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("WarehouseDb")
            ?? throw new InvalidOperationException("Connection string 'WarehouseDb' was not found.");
    }

    public async Task<IReadOnlyList<MenuMasterListDto>> GetMenusAsync(CancellationToken cancellationToken = default)
    {
        var menus = new List<MenuMasterListDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT Id, MenuName, Status, CreatedBy, CreatedOn
            FROM dbo.TblMnu
            ORDER BY Id DESC;
            """,
            connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            menus.Add(new MenuMasterListDto(
                reader.GetInt32(reader.GetOrdinal("Id")),
                reader.GetString(reader.GetOrdinal("MenuName")),
                reader.GetString(reader.GetOrdinal("Status")),
                reader.GetString(reader.GetOrdinal("CreatedBy")),
                reader.GetDateTime(reader.GetOrdinal("CreatedOn"))));
        }

        return menus;
    }

    public async Task<IReadOnlyList<MenuMasterListDto>> GetMenusForUserAsync(int userMasterId, CancellationToken cancellationToken = default)
    {
        var menus = new List<MenuMasterListDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT DISTINCT
                m.Id,
                m.MenuName,
                m.Status,
                m.CreatedBy,
                m.CreatedOn
            FROM dbo.UserMasterRoleMap AS urm
            INNER JOIN dbo.RoleMaster AS rm
                ON rm.RoleId = urm.RoleId
               AND rm.IsActive = 1
            INNER JOIN dbo.RoleMasterMenuMap AS rmm
                ON rmm.RoleId = rm.RoleId
            INNER JOIN dbo.TblMnu AS m
                ON m.Id = rmm.MenuId
               AND m.Status = N'Activate'
            WHERE urm.UserMasterId = @UserMasterId
            ORDER BY m.Id;
            """,
            connection);

        command.Parameters.Add("@UserMasterId", SqlDbType.Int).Value = userMasterId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            menus.Add(new MenuMasterListDto(
                reader.GetInt32(reader.GetOrdinal("Id")),
                reader.GetString(reader.GetOrdinal("MenuName")),
                reader.GetString(reader.GetOrdinal("Status")),
                reader.GetString(reader.GetOrdinal("CreatedBy")),
                reader.GetDateTime(reader.GetOrdinal("CreatedOn"))));
        }

        return menus;
    }

    public async Task<int> CreateMenuAsync(MenuMasterSaveRequest request, string createdBy, DateTime createdOn, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.usp_TblMnu_Insert", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add("@MenuName", SqlDbType.NVarChar, 150).Value = request.MenuName.Trim();
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = request.Status.Trim();
        command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100).Value = createdBy.Trim();
        command.Parameters.Add("@CreatedOn", SqlDbType.DateTime2).Value = createdOn;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<bool> UpdateMenuStatusAsync(int id, MenuMasterUpdateRequest request, string updatedBy, DateTime updatedOn, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.usp_TblMnu_UpdateStatus", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = request.Status.Trim();
        command.Parameters.Add("@UpdatedBy", SqlDbType.NVarChar, 100).Value = updatedBy.Trim();
        command.Parameters.Add("@UpdatedOn", SqlDbType.DateTime2).Value = updatedOn;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) > 0;
    }
}

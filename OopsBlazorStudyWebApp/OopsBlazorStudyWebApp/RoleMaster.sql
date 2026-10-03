USE WarehouseDb;
GO

IF OBJECT_ID(N'dbo.TblMnu', N'U') IS NULL
BEGIN
    THROW 50002, 'Create dbo.TblMnu first by running MenuMaster.sql.', 1;
END
GO

IF OBJECT_ID(N'dbo.RoleMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleMaster
    (
        RoleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RoleMaster PRIMARY KEY,
        RoleName NVARCHAR(100) NOT NULL,
        RoleCode NVARCHAR(30) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_RoleMaster_IsActive DEFAULT (1),
        CreatedOn DATETIME2(0) NOT NULL CONSTRAINT DF_RoleMaster_CreatedOn DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_RoleMaster_RoleCode'
      AND object_id = OBJECT_ID(N'dbo.RoleMaster')
)
BEGIN
    CREATE UNIQUE INDEX UX_RoleMaster_RoleCode ON dbo.RoleMaster(RoleCode);
END
GO

IF COL_LENGTH(N'dbo.RoleMaster', N'LandingPage') IS NULL
BEGIN
    ALTER TABLE dbo.RoleMaster
    ADD LandingPage NVARCHAR(100) NOT NULL
        CONSTRAINT DF_RoleMaster_LandingPage DEFAULT (N'Dashboard');
END
GO

IF COL_LENGTH(N'dbo.RoleMaster', N'Remark') IS NULL
BEGIN
    ALTER TABLE dbo.RoleMaster
    ADD Remark NVARCHAR(500) NULL;
END
GO

IF COL_LENGTH(N'dbo.RoleMaster', N'UpdatedOn') IS NULL
BEGIN
    ALTER TABLE dbo.RoleMaster
    ADD UpdatedOn DATETIME2(0) NULL;
END
GO

IF OBJECT_ID(N'dbo.RoleMasterMenuMap', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleMasterMenuMap
    (
        RoleId INT NOT NULL,
        MenuId INT NOT NULL,
        CONSTRAINT PK_RoleMasterMenuMap PRIMARY KEY (RoleId, MenuId),
        CONSTRAINT FK_RoleMasterMenuMap_RoleMaster FOREIGN KEY (RoleId) REFERENCES dbo.RoleMaster(RoleId) ON DELETE CASCADE,
        CONSTRAINT FK_RoleMasterMenuMap_TblMnu FOREIGN KEY (MenuId) REFERENCES dbo.TblMnu(Id)
    );
END
GO

CREATE OR ALTER VIEW dbo.vw_RoleMasterList
AS
SELECT
    rm.RoleId,
    rm.RoleName,
    rm.RoleCode,
    rm.LandingPage,
    CASE WHEN rm.IsActive = 1 THEN N'Active' ELSE N'Inactive' END AS Status,
    rm.Remark,
    ISNULL(STRING_AGG(m.MenuName, N', ') WITHIN GROUP (ORDER BY m.MenuName), N'') AS MenuRights,
    COUNT(rmm.MenuId) AS MenuRightCount,
    rm.CreatedOn,
    rm.UpdatedOn
FROM dbo.RoleMaster AS rm
LEFT JOIN dbo.RoleMasterMenuMap AS rmm ON rmm.RoleId = rm.RoleId
LEFT JOIN dbo.TblMnu AS m ON m.Id = rmm.MenuId
GROUP BY
    rm.RoleId,
    rm.RoleName,
    rm.RoleCode,
    rm.LandingPage,
    rm.IsActive,
    rm.Remark,
    rm.CreatedOn,
    rm.UpdatedOn;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RoleMaster_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        RoleId,
        RoleName,
        RoleCode,
        LandingPage,
        Status,
        Remark,
        MenuRights,
        MenuRightCount,
        CreatedOn,
        UpdatedOn
    FROM dbo.vw_RoleMasterList
    ORDER BY RoleName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RoleMaster_GetActive
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RoleId, RoleName, RoleCode
    FROM dbo.RoleMaster
    WHERE IsActive = 1
    ORDER BY RoleName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RoleMaster_GetById
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rm.RoleId,
        rm.RoleName,
        rm.RoleCode,
        rm.LandingPage,
        CASE WHEN rm.IsActive = 1 THEN N'Active' ELSE N'Inactive' END AS Status,
        ISNULL(rm.Remark, N'') AS Remark,
        rm.IsActive
    FROM dbo.RoleMaster AS rm
    WHERE rm.RoleId = @RoleId;

    SELECT rmm.MenuId
    FROM dbo.RoleMasterMenuMap AS rmm
    WHERE rmm.RoleId = @RoleId
    ORDER BY rmm.MenuId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RoleMaster_Insert
    @RoleName NVARCHAR(100),
    @RoleCode NVARCHAR(30),
    @LandingPage NVARCHAR(100),
    @Status NVARCHAR(20),
    @Remark NVARCHAR(500) = NULL,
    @SelectedMenuIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Status NOT IN (N'Active', N'Inactive')
    BEGIN
        THROW 50003, 'Status must be Active or Inactive.', 1;
    END;

    DECLARE @NewIds TABLE (RoleId INT);

    INSERT INTO dbo.RoleMaster
    (
        RoleName,
        RoleCode,
        LandingPage,
        IsActive,
        Remark
    )
    OUTPUT INSERTED.RoleId INTO @NewIds(RoleId)
    VALUES
    (
        @RoleName,
        @RoleCode,
        @LandingPage,
        CASE WHEN @Status = N'Active' THEN 1 ELSE 0 END,
        @Remark
    );

    DECLARE @RoleId INT = (SELECT TOP 1 RoleId FROM @NewIds);

    INSERT INTO dbo.RoleMasterMenuMap (RoleId, MenuId)
    SELECT DISTINCT @RoleId, m.Id
    FROM STRING_SPLIT(@SelectedMenuIds, N',') AS selected
    INNER JOIN dbo.TblMnu AS m ON m.Id = TRY_CAST(selected.value AS INT)
    WHERE TRY_CAST(selected.value AS INT) IS NOT NULL;

    SELECT @RoleId AS RoleId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_RoleMaster_Update
    @RoleId INT,
    @RoleName NVARCHAR(100),
    @RoleCode NVARCHAR(30),
    @LandingPage NVARCHAR(100),
    @Status NVARCHAR(20),
    @Remark NVARCHAR(500) = NULL,
    @SelectedMenuIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Status NOT IN (N'Active', N'Inactive')
    BEGIN
        THROW 50004, 'Status must be Active or Inactive.', 1;
    END;

    DECLARE @RowsAffected INT = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.RoleMaster
        SET
            RoleName = @RoleName,
            LandingPage = @LandingPage,
            IsActive = CASE WHEN @Status = N'Active' THEN 1 ELSE 0 END,
            Remark = @Remark,
            UpdatedOn = SYSUTCDATETIME()
        WHERE RoleId = @RoleId;

        SET @RowsAffected = @@ROWCOUNT;

        IF @RowsAffected = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS RowsAffected;
            RETURN;
        END;

        DELETE FROM dbo.RoleMasterMenuMap
        WHERE RoleId = @RoleId;

        INSERT INTO dbo.RoleMasterMenuMap (RoleId, MenuId)
        SELECT DISTINCT @RoleId, m.Id
        FROM STRING_SPLIT(@SelectedMenuIds, N',') AS selected
        INNER JOIN dbo.TblMnu AS m ON m.Id = TRY_CAST(selected.value AS INT)
        WHERE TRY_CAST(selected.value AS INT) IS NOT NULL;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;
    END CATCH;

    SELECT @RowsAffected AS RowsAffected;
END
GO

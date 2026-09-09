

USE WarehouseDb;
GO

IF OBJECT_ID('dbo.UserMasterRoleMap', 'U') IS NOT NULL DROP TABLE dbo.UserMasterRoleMap;
IF OBJECT_ID('dbo.UserMaster', 'U') IS NOT NULL DROP TABLE dbo.UserMaster;
IF OBJECT_ID('dbo.RoleMaster', 'U') IS NOT NULL DROP TABLE dbo.RoleMaster;
GO

CREATE TABLE dbo.RoleMaster
(
    RoleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RoleMaster PRIMARY KEY,
    RoleName NVARCHAR(100) NOT NULL,
    RoleCode NVARCHAR(30) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_RoleMaster_IsActive DEFAULT (1),
    CreatedOn DATETIME2(0) NOT NULL CONSTRAINT DF_RoleMaster_CreatedOn DEFAULT (SYSUTCDATETIME())
);
GO

CREATE UNIQUE INDEX UX_RoleMaster_RoleCode ON dbo.RoleMaster(RoleCode);
GO

CREATE TABLE dbo.UserMaster
(
    UserMasterId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserMaster PRIMARY KEY,
    UserId NVARCHAR(50) NOT NULL,
    UserName NVARCHAR(150) NOT NULL,
    PasswordHash VARBINARY(32) NOT NULL,
    PasswordSalt VARBINARY(16) NOT NULL,
    EmailAddress NVARCHAR(256) NOT NULL,
    MobileNo NVARCHAR(15) NOT NULL,
    Remark NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_UserMaster_IsActive DEFAULT (1),
    CreatedOn DATETIME2(0) NOT NULL CONSTRAINT DF_UserMaster_CreatedOn DEFAULT (SYSUTCDATETIME()),
    UpdatedOn DATETIME2(0) NULL
);
GO

CREATE UNIQUE INDEX UX_UserMaster_UserId ON dbo.UserMaster(UserId);
GO

CREATE TABLE dbo.UserMasterRoleMap
(
    UserMasterId INT NOT NULL,
    RoleId INT NOT NULL,
    CONSTRAINT PK_UserMasterRoleMap PRIMARY KEY (UserMasterId, RoleId),
    CONSTRAINT FK_UserMasterRoleMap_UserMaster FOREIGN KEY (UserMasterId) REFERENCES dbo.UserMaster(UserMasterId) ON DELETE CASCADE,
    CONSTRAINT FK_UserMasterRoleMap_RoleMaster FOREIGN KEY (RoleId) REFERENCES dbo.RoleMaster(RoleId)
);
GO

INSERT INTO dbo.RoleMaster (RoleName, RoleCode, IsActive)
VALUES
    ('Administrator', 'ADM', 1),
    ('Inventory Controller', 'INV', 1),
    ('Dispatch Supervisor', 'DSP', 1),
    ('Gate Operator', 'GOP', 1),
    ('Auditor', 'AUD', 1),
    ('Viewer', 'VWR', 1);
GO

CREATE OR ALTER VIEW dbo.vw_UserMasterList
AS
SELECT
    um.UserMasterId,
    um.UserId,
    um.UserName,
    um.EmailAddress,
    um.MobileNo,
    um.Remark,
    um.IsActive,
    ISNULL(STRING_AGG(CONCAT(r.RoleName, ' (', r.RoleCode, ')'), ', ') WITHIN GROUP (ORDER BY r.RoleName), '') AS SelectedRoles,
    COUNT(urm.RoleId) AS RoleCount,
    um.CreatedOn,
    um.UpdatedOn
FROM dbo.UserMaster um
LEFT JOIN dbo.UserMasterRoleMap urm ON urm.UserMasterId = um.UserMasterId
LEFT JOIN dbo.RoleMaster r ON r.RoleId = urm.RoleId
GROUP BY
    um.UserMasterId,
    um.UserId,
    um.UserName,
    um.EmailAddress,
    um.MobileNo,
    um.Remark,
    um.IsActive,
    um.CreatedOn,
    um.UpdatedOn;
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

CREATE OR ALTER PROCEDURE dbo.usp_UserMaster_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        UserMasterId,
        UserId,
        UserName,
        EmailAddress,
        MobileNo,
        Remark,
        IsActive,
        SelectedRoles,
        RoleCount,
        CreatedOn,
        UpdatedOn
    FROM dbo.vw_UserMasterList
    ORDER BY UserName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_UserMaster_GetById
    @UserMasterId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        um.UserMasterId,
        um.UserId,
        um.UserName,
        CONVERT(NVARCHAR(200), '') AS Password,
        um.EmailAddress,
        um.MobileNo,
        ISNULL(um.Remark, '') AS Remark,
        um.IsActive
    FROM dbo.UserMaster um
    WHERE um.UserMasterId = @UserMasterId;

    SELECT RoleId
    FROM dbo.UserMasterRoleMap
    WHERE UserMasterId = @UserMasterId
    ORDER BY RoleId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_UserMaster_Insert
    @UserId NVARCHAR(50),
    @UserName NVARCHAR(150),
    @PasswordHash VARBINARY(32),
    @PasswordSalt VARBINARY(16),
    @EmailAddress NVARCHAR(256),
    @MobileNo NVARCHAR(15),
    @Remark NVARCHAR(500) = NULL,
    @IsActive BIT = 1,
    @SelectedRoleIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NewIds TABLE (UserMasterId INT);

    INSERT INTO dbo.UserMaster
    (
        UserId,
        UserName,
        PasswordHash,
        PasswordSalt,
        EmailAddress,
        MobileNo,
        Remark,
        IsActive
    )
    OUTPUT INSERTED.UserMasterId INTO @NewIds(UserMasterId)
    VALUES
    (
        @UserId,
        @UserName,
        @PasswordHash,
        @PasswordSalt,
        @EmailAddress,
        @MobileNo,
        @Remark,
        @IsActive
    );

    DECLARE @UserMasterId INT = (SELECT TOP 1 UserMasterId FROM @NewIds);

    INSERT INTO dbo.UserMasterRoleMap (UserMasterId, RoleId)
    SELECT @UserMasterId, TRY_CAST(value AS INT)
    FROM STRING_SPLIT(@SelectedRoleIds, ',')
    WHERE TRY_CAST(value AS INT) IS NOT NULL;

    SELECT @UserMasterId AS UserMasterId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_UserMaster_Update
    @UserMasterId INT,
    @UserId NVARCHAR(50),
    @UserName NVARCHAR(150),
    @PasswordHash VARBINARY(32),
    @PasswordSalt VARBINARY(16),
    @EmailAddress NVARCHAR(256),
    @MobileNo NVARCHAR(15),
    @Remark NVARCHAR(500) = NULL,
    @IsActive BIT = 1,
    @SelectedRoleIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RowsAffected INT = 0;

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

    SET @RowsAffected = @@ROWCOUNT;

    DELETE FROM dbo.UserMasterRoleMap
    WHERE UserMasterId = @UserMasterId;

    INSERT INTO dbo.UserMasterRoleMap (UserMasterId, RoleId)
    SELECT @UserMasterId, TRY_CAST(value AS INT)
    FROM STRING_SPLIT(@SelectedRoleIds, ',')
    WHERE TRY_CAST(value AS INT) IS NOT NULL;

    SELECT @RowsAffected AS RowsAffected;
END
GO

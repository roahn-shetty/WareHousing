USE WarehouseDb;
GO

IF OBJECT_ID(N'dbo.TblMnu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TblMnu
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TblMnu PRIMARY KEY,
        MenuName NVARCHAR(150) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_TblMnu_Status DEFAULT ('Activate'),
        CreatedBy NVARCHAR(100) NOT NULL,
        CreatedOn DATETIME2(0) NOT NULL CONSTRAINT DF_TblMnu_CreatedOn DEFAULT (SYSDATETIME()),
        CONSTRAINT CK_TblMnu_Status CHECK (Status IN ('Activate', 'DeActivate'))
    );
END
GO

-- Optional cleanup if an older script already added these columns to TblMnu.
IF COL_LENGTH(N'dbo.TblMnu', N'UpdatedBy') IS NOT NULL
BEGIN
    ALTER TABLE dbo.TblMnu DROP COLUMN UpdatedBy;
END
GO

IF COL_LENGTH(N'dbo.TblMnu', N'UpdatedOn') IS NOT NULL
BEGIN
    ALTER TABLE dbo.TblMnu DROP COLUMN UpdatedOn;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TblMnu_MenuName'
      AND object_id = OBJECT_ID(N'dbo.TblMnu')
)
BEGIN
    CREATE INDEX IX_TblMnu_MenuName ON dbo.TblMnu(MenuName);
END
GO

IF OBJECT_ID(N'dbo.TblMnu_Log', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TblMnu_Log
    (
        LogId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TblMnu_Log PRIMARY KEY,
        MenuId INT NOT NULL,
        MenuName NVARCHAR(150) NOT NULL,
        BeforeStatus NVARCHAR(20) NOT NULL,
        AfterStatus NVARCHAR(20) NOT NULL,
        UpdatedBy NVARCHAR(100) NOT NULL,
        UpdatedOn DATETIME2(0) NOT NULL CONSTRAINT DF_TblMnu_Log_UpdatedOn DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_TblMnu_Log_TblMnu FOREIGN KEY (MenuId) REFERENCES dbo.TblMnu(Id),
        CONSTRAINT CK_TblMnu_Log_BeforeStatus CHECK (BeforeStatus IN ('Activate', 'DeActivate')),
        CONSTRAINT CK_TblMnu_Log_AfterStatus CHECK (AfterStatus IN ('Activate', 'DeActivate'))
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TblMnu_Log_MenuId_UpdatedOn'
      AND object_id = OBJECT_ID(N'dbo.TblMnu_Log')
)
BEGIN
    CREATE INDEX IX_TblMnu_Log_MenuId_UpdatedOn ON dbo.TblMnu_Log(MenuId, UpdatedOn DESC);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_TblMnu_Insert
    @MenuName NVARCHAR(150),
    @Status NVARCHAR(20),
    @CreatedBy NVARCHAR(100),
    @CreatedOn DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.TblMnu
    (
        MenuName,
        Status,
        CreatedBy,
        CreatedOn
    )
    VALUES
    (
        @MenuName,
        @Status,
        @CreatedBy,
        @CreatedOn
    );

    SELECT CONVERT(INT, SCOPE_IDENTITY()) AS Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_TblMnu_UpdateStatus
    @Id INT,
    @Status NVARCHAR(20),
    @UpdatedBy NVARCHAR(100),
    @UpdatedOn DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Status NOT IN (N'Activate', N'DeActivate')
    BEGIN
        THROW 50001, 'Status must be Activate or DeActivate.', 1;
    END;

    DECLARE @BeforeStatus NVARCHAR(20);
    DECLARE @MenuName NVARCHAR(150);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @BeforeStatus = Status,
            @MenuName = MenuName
        FROM dbo.TblMnu WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @Id;

        IF @BeforeStatus IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS RowsAffected;
            RETURN;
        END;

        IF @BeforeStatus <> @Status
        BEGIN
            UPDATE dbo.TblMnu
            SET Status = @Status
            WHERE Id = @Id;

            INSERT INTO dbo.TblMnu_Log
            (
                MenuId,
                MenuName,
                BeforeStatus,
                AfterStatus,
                UpdatedBy,
                UpdatedOn
            )
            VALUES
            (
                @Id,
                @MenuName,
                @BeforeStatus,
                @Status,
                @UpdatedBy,
                @UpdatedOn
            );
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;
    END CATCH;

    SELECT 1 AS RowsAffected;
END
GO

CREATE OR ALTER VIEW dbo.vw_TblMnu_Log
AS
    SELECT
        log.LogId,
        log.MenuId,
        log.MenuName,
        log.BeforeStatus,
        log.AfterStatus,
        log.UpdatedBy,
        log.UpdatedOn
    FROM dbo.TblMnu_Log AS log;
GO

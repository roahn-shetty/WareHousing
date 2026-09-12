USE WarehouseDb;
GO

CREATE TABLE dbo.TblMnu
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TblMnu PRIMARY KEY,
    MenuName NVARCHAR(150) NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_TblMnu_Status DEFAULT ('Activate'),
    CreatedBy NVARCHAR(100) NOT NULL,
    CreatedOn DATETIME2(0) NOT NULL CONSTRAINT DF_TblMnu_CreatedOn DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_TblMnu_Status CHECK (Status IN ('Activate', 'DeActivate'))
);
GO

CREATE INDEX IX_TblMnu_MenuName ON dbo.TblMnu(MenuName);
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

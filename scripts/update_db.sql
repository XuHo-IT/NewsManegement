IF COL_LENGTH('NewsArticle','NewsStatus') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID('NewsArticle')
          AND c.name = 'NewsStatus'
          AND t.name = 'bit'
    )
    BEGIN
        IF COL_LENGTH('NewsArticle','NewsStatusInt') IS NULL
            ALTER TABLE NewsArticle ADD NewsStatusInt int NOT NULL CONSTRAINT DF_NewsArticle_NewsStatusInt DEFAULT(1);
    END
END
GO

IF COL_LENGTH('NewsArticle','NewsStatusInt') IS NOT NULL
BEGIN
    UPDATE NewsArticle
    SET NewsStatusInt = CASE WHEN NewsStatus = 1 THEN 4 ELSE 1 END;

    ALTER TABLE NewsArticle DROP COLUMN NewsStatus;
    EXEC sp_rename 'NewsArticle.NewsStatusInt', 'NewsStatus', 'COLUMN';
END
GO

IF COL_LENGTH('Category','IsDeleted') IS NULL
    ALTER TABLE Category ADD IsDeleted bit NOT NULL CONSTRAINT DF_Category_IsDeleted DEFAULT(0);
IF COL_LENGTH('Category','DeletedAt') IS NULL
    ALTER TABLE Category ADD DeletedAt datetime NULL;
IF COL_LENGTH('Category','DeletedBy') IS NULL
    ALTER TABLE Category ADD DeletedBy nvarchar(100) NULL;
GO

IF COL_LENGTH('NewsArticle','IsDeleted') IS NULL
    ALTER TABLE NewsArticle ADD IsDeleted bit NOT NULL CONSTRAINT DF_NewsArticle_IsDeleted DEFAULT(0);
IF COL_LENGTH('NewsArticle','DeletedAt') IS NULL
    ALTER TABLE NewsArticle ADD DeletedAt datetime NULL;
IF COL_LENGTH('NewsArticle','DeletedBy') IS NULL
    ALTER TABLE NewsArticle ADD DeletedBy nvarchar(100) NULL;
GO

IF COL_LENGTH('Tag','IsDeleted') IS NULL
    ALTER TABLE Tag ADD IsDeleted bit NOT NULL CONSTRAINT DF_Tag_IsDeleted DEFAULT(0);
IF COL_LENGTH('Tag','DeletedAt') IS NULL
    ALTER TABLE Tag ADD DeletedAt datetime NULL;
IF COL_LENGTH('Tag','DeletedBy') IS NULL
    ALTER TABLE Tag ADD DeletedBy nvarchar(100) NULL;
GO

IF COL_LENGTH('SystemAccount','IsDeleted') IS NULL
    ALTER TABLE SystemAccount ADD IsDeleted bit NOT NULL CONSTRAINT DF_SystemAccount_IsDeleted DEFAULT(0);
IF COL_LENGTH('SystemAccount','DeletedAt') IS NULL
    ALTER TABLE SystemAccount ADD DeletedAt datetime NULL;
IF COL_LENGTH('SystemAccount','DeletedBy') IS NULL
    ALTER TABLE SystemAccount ADD DeletedBy nvarchar(100) NULL;
GO

IF OBJECT_ID('AuditLog','U') IS NULL
BEGIN
    CREATE TABLE AuditLog (
        AuditLogID int IDENTITY(1,1) PRIMARY KEY,
        TableName nvarchar(128) NOT NULL,
        Action nvarchar(20) NOT NULL,
        RecordKey nvarchar(128) NOT NULL,
        OldValues nvarchar(max) NULL,
        NewValues nvarchar(max) NULL,
        ChangedBy nvarchar(100) NULL,
        ChangedAt datetime NOT NULL CONSTRAINT DF_AuditLog_ChangedAt DEFAULT(GETDATE())
    );
END
GO

IF OBJECT_ID('Role','U') IS NULL
BEGIN
    CREATE TABLE Role (
        RoleID int NOT NULL PRIMARY KEY,
        RoleName nvarchar(50) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM Role WHERE RoleID = 1)
    INSERT INTO Role (RoleID, RoleName) VALUES (1, 'Admin');
IF NOT EXISTS (SELECT 1 FROM Role WHERE RoleID = 2)
    INSERT INTO Role (RoleID, RoleName) VALUES (2, 'Staff');
GO

IF COL_LENGTH('SystemAccount','AccountRole') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SystemAccount_Role')
BEGIN
    ALTER TABLE SystemAccount WITH NOCHECK
        ADD CONSTRAINT FK_SystemAccount_Role
        FOREIGN KEY (AccountRole) REFERENCES Role(RoleID);
END
GO

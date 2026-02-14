IF OBJECT_ID('TR_Audit_Category', 'TR') IS NOT NULL DROP TRIGGER TR_Audit_Category;
GO
CREATE TRIGGER TR_Audit_Category ON Category
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO AuditLog (TableName, Action, RecordKey, OldValues, NewValues, ChangedBy, ChangedAt)
    SELECT 'Category',
           CASE WHEN d.CategoryID IS NULL THEN 'INSERT' WHEN i.CategoryID IS NULL THEN 'DELETE' ELSE 'UPDATE' END,
           CAST(COALESCE(i.CategoryID, d.CategoryID) AS nvarchar(128)),
           CASE WHEN d.CategoryID IS NULL THEN NULL ELSE (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.CategoryID IS NULL THEN NULL ELSE (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           COALESCE(CAST(SESSION_CONTEXT(N'ChangedBy') AS nvarchar(100)), 
                    CASE WHEN i.CreatedByID IS NOT NULL THEN 'User_' + CAST(i.CreatedByID AS nvarchar(10))
                         WHEN d.CreatedByID IS NOT NULL THEN 'User_' + CAST(d.CreatedByID AS nvarchar(10))
                         ELSE 'System' END,
                    'System'),
           GETDATE()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.CategoryID = d.CategoryID;
END
GO

IF OBJECT_ID('TR_Audit_Tag', 'TR') IS NOT NULL DROP TRIGGER TR_Audit_Tag;
GO
CREATE TRIGGER TR_Audit_Tag ON Tag
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO AuditLog (TableName, Action, RecordKey, OldValues, NewValues, ChangedBy, ChangedAt)
    SELECT 'Tag',
           CASE WHEN d.TagID IS NULL THEN 'INSERT' WHEN i.TagID IS NULL THEN 'DELETE' ELSE 'UPDATE' END,
           CAST(COALESCE(i.TagID, d.TagID) AS nvarchar(128)),
           CASE WHEN d.TagID IS NULL THEN NULL ELSE (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.TagID IS NULL THEN NULL ELSE (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           COALESCE(CAST(SESSION_CONTEXT(N'ChangedBy') AS nvarchar(100)), 
                    CASE WHEN i.CreatedByID IS NOT NULL THEN 'User_' + CAST(i.CreatedByID AS nvarchar(10))
                         WHEN d.CreatedByID IS NOT NULL THEN 'User_' + CAST(d.CreatedByID AS nvarchar(10))
                         ELSE 'System' END,
                    'System'),
           GETDATE()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.TagID = d.TagID;
END
GO

IF OBJECT_ID('TR_Audit_SystemAccount', 'TR') IS NOT NULL DROP TRIGGER TR_Audit_SystemAccount;
GO
CREATE TRIGGER TR_Audit_SystemAccount ON SystemAccount
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO AuditLog (TableName, Action, RecordKey, OldValues, NewValues, ChangedBy, ChangedAt)
    SELECT 'SystemAccount',
           CASE WHEN d.AccountID IS NULL THEN 'INSERT' WHEN i.AccountID IS NULL THEN 'DELETE' ELSE 'UPDATE' END,
           CAST(COALESCE(i.AccountID, d.AccountID) AS nvarchar(128)),
           CASE WHEN d.AccountID IS NULL THEN NULL ELSE (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.AccountID IS NULL THEN NULL ELSE (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           COALESCE(CAST(SESSION_CONTEXT(N'ChangedBy') AS nvarchar(100)), 
                    CASE WHEN i.AccountID IS NOT NULL THEN 'User_' + CAST(i.AccountID AS nvarchar(10))
                         WHEN d.AccountID IS NOT NULL THEN 'User_' + CAST(d.AccountID AS nvarchar(10))
                         ELSE 'System' END,
                    'System'),
           GETDATE()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.AccountID = d.AccountID;
END
GO

IF OBJECT_ID('TR_Audit_NewsArticle', 'TR') IS NOT NULL DROP TRIGGER TR_Audit_NewsArticle;
GO
CREATE TRIGGER TR_Audit_NewsArticle ON NewsArticle
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO AuditLog (TableName, Action, RecordKey, OldValues, NewValues, ChangedBy, ChangedAt)
    SELECT 'NewsArticle',
           CASE WHEN d.NewsArticleID IS NULL THEN 'INSERT' WHEN i.NewsArticleID IS NULL THEN 'DELETE' ELSE 'UPDATE' END,
           CAST(COALESCE(i.NewsArticleID, d.NewsArticleID) AS nvarchar(128)),
           CASE WHEN d.NewsArticleID IS NULL THEN NULL ELSE (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           CASE WHEN i.NewsArticleID IS NULL THEN NULL ELSE (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
           COALESCE(CAST(SESSION_CONTEXT(N'ChangedBy') AS nvarchar(100)), 
                    CASE WHEN i.CreatedByID IS NOT NULL THEN 'User_' + CAST(i.CreatedByID AS nvarchar(10))
                         WHEN i.UpdatedByID IS NOT NULL THEN 'User_' + CAST(i.UpdatedByID AS nvarchar(10))
                         WHEN d.CreatedByID IS NOT NULL THEN 'User_' + CAST(d.CreatedByID AS nvarchar(10))
                         WHEN d.UpdatedByID IS NOT NULL THEN 'User_' + CAST(d.UpdatedByID AS nvarchar(10))
                         ELSE 'System' END,
                    'System'),
           GETDATE()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.NewsArticleID = d.NewsArticleID;
END
GO

-- Add Google Auth columns to database manually
-- Run this in SQL Server Management Studio or use sqlcmd

-- Add GoogleId to SystemAccount
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'GoogleId' AND Object_ID = Object_ID(N'SystemAccount'))
BEGIN
    ALTER TABLE SystemAccount ADD GoogleId NVARCHAR(MAX) NULL;
END

-- Add AvatarUrl to SystemAccount  
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'AvatarUrl' AND Object_ID = Object_ID(N'SystemAccount'))
BEGIN
    ALTER TABLE SystemAccount ADD AvatarUrl NVARCHAR(MAX) NULL;
END

-- Add ImageUrl to NewsArticle
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ImageUrl' AND Object_ID = Object_ID(N'NewsArticle'))
BEGIN
    ALTER TABLE NewsArticle ADD ImageUrl NVARCHAR(MAX) NULL;
END

-- Add ImageUrl to Category
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ImageUrl' AND Object_ID = Object_ID(N'Category'))
BEGIN
    ALTER TABLE Category ADD ImageUrl NVARCHAR(MAX) NULL;
END

-- Add ImageUrl to Tag
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ImageUrl' AND Object_ID = Object_ID(N'Tag'))
BEGIN
    ALTER TABLE Tag ADD ImageUrl NVARCHAR(MAX) NULL;
END

PRINT 'Database columns added successfully!';

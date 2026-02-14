IF COL_LENGTH('SystemAccount','AccountPasswordHash') IS NULL
    ALTER TABLE SystemAccount ADD AccountPasswordHash nvarchar(200) NULL;
GO

IF OBJECT_ID('RefreshToken','U') IS NULL
BEGIN
    CREATE TABLE RefreshToken (
        RefreshTokenID int IDENTITY(1,1) PRIMARY KEY,
        AccountID smallint NOT NULL,
        Token nvarchar(200) NOT NULL,
        CreatedAt datetime NOT NULL CONSTRAINT DF_RefreshToken_CreatedAt DEFAULT(GETDATE()),
        ExpiresAt datetime NOT NULL,
        RevokedAt datetime NULL,
        CONSTRAINT FK_RefreshToken_SystemAccount FOREIGN KEY (AccountID) REFERENCES SystemAccount(AccountID)
    );
    CREATE INDEX IX_RefreshToken_Token ON RefreshToken(Token);
END
GO

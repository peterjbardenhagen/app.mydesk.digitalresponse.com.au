-- Migration 002b: Create PasswordResetTokens table
-- Stores password reset tokens for the forgot-password feature
-- Required by Program.cs endpoints: /api/auth/forgot-password and /api/auth/reset-password

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'PasswordResetTokens')
BEGIN
    CREATE TABLE PasswordResetTokens (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        Token NVARCHAR(MAX) NOT NULL,  -- SHA256 hex string of the actual token
        ExpiresAt DATETIME2 NOT NULL,
        IsUsed BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_PasswordResetTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
    );

    CREATE INDEX IX_PasswordResetTokens_Token ON PasswordResetTokens([Token](255));
    CREATE INDEX IX_PasswordResetTokens_UserId ON PasswordResetTokens(UserId);
    CREATE INDEX IX_PasswordResetTokens_ExpiresAt ON PasswordResetTokens(ExpiresAt);

    PRINT 'Created PasswordResetTokens table with indexes';
END
ELSE
    PRINT 'PasswordResetTokens table already exists';

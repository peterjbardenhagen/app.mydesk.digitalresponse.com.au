-- Personal Access Tokens for external AI agent integrations
-- (MCP / Claude / ChatGPT / Copilot / Gemini and others)
--
-- Each token encodes user + tenant identity so external agents can
-- authenticate, scope their queries to the correct tenant's data,
-- and respect the user's role/permissions.
--
-- Token format:  mdk_<base64url(32 random bytes)>
-- Stored value:  SHA-256 hex digest of the raw token (never the raw token)

CREATE TABLE PersonalAccessTokens (
    TokenId       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()     PRIMARY KEY,
    UserId        INT              NOT NULL,
    TenantId      UNIQUEIDENTIFIER NOT NULL,
    TokenName     NVARCHAR(100)    NOT NULL,
    TokenHash     NVARCHAR(64)     NOT NULL,   -- SHA-256 hex of raw token
    Scopes        NVARCHAR(500)    NOT NULL DEFAULT 'chat tools',
    CreatedAt     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt     DATETIME2        NULL,
    LastUsedAt    DATETIME2        NULL,
    IsRevoked     BIT              NOT NULL DEFAULT 0,

    CONSTRAINT FK_PAT_Users   FOREIGN KEY (UserId)   REFERENCES Users(UserId)   ON DELETE CASCADE,
    CONSTRAINT FK_PAT_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
);

-- Fast lookup when validating incoming tokens
CREATE UNIQUE INDEX IX_PAT_Hash ON PersonalAccessTokens(TokenHash)
    WHERE IsRevoked = 0;

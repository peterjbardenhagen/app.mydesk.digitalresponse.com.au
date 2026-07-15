-- Migration 023: Add Administrator Users
-- Purpose: Create initial administrator users for Digital Response
-- Users: Peter Bardenhagen (CEO), John Bardenhagen (CFO)

SET IDENTITY_INSERT dbo.Users ON;

-- ─────────────────────────────────────────────────────────────────────────────
-- Peter Bardenhagen - CEO (Tenant Administrator)
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'peterb@digitalresponse.com.au')
BEGIN
    INSERT INTO dbo.Users (
        UserId,
        TenantId,
        Email,
        [Name],
        [Password],
        [Role],
        [Status],
        Position,
        CreatedAt,
        UpdatedAt
    )
    VALUES (
        1000,
        1,
        'peterb@digitalresponse.com.au',
        'Peter Bardenhagen',
        'Omnfxop09!',
        'Director',
        'Active',
        'CEO',
        GETUTCDATE(),
        GETUTCDATE()
    );
    PRINT 'Created user: Peter Bardenhagen (CEO)';
END

-- ─────────────────────────────────────────────────────────────────────────────
-- John Bardenhagen - CFO (Tenant Administrator)
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'johnb@digitalresponse.com.au')
BEGIN
    INSERT INTO dbo.Users (
        UserId,
        TenantId,
        Email,
        [Name],
        [Password],
        [Role],
        [Status],
        Position,
        CreatedAt,
        UpdatedAt
    )
    VALUES (
        1001,
        1,
        'johnb@digitalresponse.com.au',
        'John Bardenhagen',
        'Omnfxop90!',
        'Director',
        'Active',
        'CFO',
        GETUTCDATE(),
        GETUTCDATE()
    );
    PRINT 'Created user: John Bardenhagen (CFO)';
END

SET IDENTITY_INSERT dbo.Users OFF;

-- ─────────────────────────────────────────────────────────────────────────────
-- Add users to default tenant if not already assigned
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.TenantMembers WHERE UserId = 1000)
BEGIN
    INSERT INTO dbo.TenantMembers (TenantId, UserId, [Role], [Status], JoinedAt)
    VALUES (1, 1000, 'Administrator', 'Active', GETUTCDATE());
    PRINT 'Added Peter Bardenhagen to default tenant as Administrator';
END

IF NOT EXISTS (SELECT 1 FROM dbo.TenantMembers WHERE UserId = 1001)
BEGIN
    INSERT INTO dbo.TenantMembers (TenantId, UserId, [Role], [Status], JoinedAt)
    VALUES (1, 1001, 'Administrator', 'Active', GETUTCDATE());
    PRINT 'Added John Bardenhagen to default tenant as Administrator';
END

-- Verify administrators created
SELECT 'Administrator Users Created Successfully' AS [Status];
SELECT UserId, [Name], Email, [Role], Position FROM dbo.Users WHERE UserId IN (1000, 1001);

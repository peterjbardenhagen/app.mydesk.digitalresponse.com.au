-- Fix RolePermissions table foreign key issue
-- The UserRoles table does NOT have a UserTypeId column, but UserTypes does.
-- This migration fixes the FK to reference the correct table.
-- Created: 2026-05-02

-- Step 1: Drop the existing broken FK if it exists
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_RolePermissions_UserRoles')
BEGIN
    ALTER TABLE RolePermissions DROP CONSTRAINT FK_RolePermissions_UserRoles;
    PRINT 'Dropped broken FK_RolePermissions_UserRoles constraint.';
END
GO

-- Step 2: Add correct FK to UserTypes table
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RolePermissions')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_RolePermissions_UserTypes')
    BEGIN
        ALTER TABLE RolePermissions
        ADD CONSTRAINT FK_RolePermissions_UserTypes FOREIGN KEY (UserTypeId) REFERENCES UserTypes(UserTypeId);
        PRINT 'Added FK_RolePermissions_UserTypes constraint.';
    END
END
ELSE
BEGIN
    -- Create the table from scratch if it doesn't exist
    CREATE TABLE RolePermissions (
        RolePermissionId INT IDENTITY(1,1) PRIMARY KEY,
        UserTypeId INT NOT NULL,
        PermissionKey NVARCHAR(100) NOT NULL,
        IsAllowed BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL,
        CONSTRAINT FK_RolePermissions_UserTypes FOREIGN KEY (UserTypeId) REFERENCES UserTypes(UserTypeId)
    );
    
    CREATE INDEX IX_RolePermissions_UserType_Permission ON RolePermissions(UserTypeId, PermissionKey);
    PRINT 'Created RolePermissions table with correct FK to UserTypes.';
END
GO

-- Step 3: Ensure UserRoles table has Active column (needed by PermissionService)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserRoles' AND COLUMN_NAME = 'Active')
BEGIN
    ALTER TABLE UserRoles ADD Active BIT NOT NULL DEFAULT 1;
    PRINT 'Added Active column to UserRoles.';
END
GO

-- Step 4: Ensure UserRoles table has UserTypeId column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserRoles' AND COLUMN_NAME = 'UserTypeId')
BEGIN
    ALTER TABLE UserRoles ADD UserTypeId INT NULL;
    PRINT 'Added UserTypeId column to UserRoles.';
END
GO

-- Step 4b: Map existing UserRoles to UserTypes based on name matching
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserRoles' AND COLUMN_NAME = 'UserTypeId')
BEGIN
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserTypes' AND COLUMN_NAME = 'UserTypeId')
    BEGIN
        UPDATE ur
        SET ur.UserTypeId = ut.UserTypeId
        FROM UserRoles ur
        JOIN UserTypes ut ON ur.UserRole = ut.UserType
        WHERE ur.UserTypeId IS NULL;
        
        PRINT 'Mapped existing UserRoles to UserTypes where names match.';
    END
END
GO

-- Step 5: Create non-clustered index on UserTypeId if not exists
IF OBJECT_ID('UserRoles') IS NOT NULL
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserRoles_UserTypeId' AND object_id = OBJECT_ID('UserRoles'))
BEGIN
    CREATE INDEX IX_UserRoles_UserTypeId ON UserRoles(UserTypeId);
    PRINT 'Created IX_UserRoles_UserTypeId index.';
END
GO

PRINT 'RolePermissions foreign key fix completed.';

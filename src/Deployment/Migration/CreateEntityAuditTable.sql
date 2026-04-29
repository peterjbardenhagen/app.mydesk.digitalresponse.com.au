-- =============================================================================
-- Techlight MyDesk — Create EntityAudit Table
-- =============================================================================

USE [Techlight_MyDesk];
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EntityAudit')
BEGIN
    CREATE TABLE EntityAudit (
        AuditId INT IDENTITY PRIMARY KEY,
        EntityType NVARCHAR(20) NOT NULL,  -- Quote, Invoice, PO, Contact, Company
        EntityId INT NOT NULL,
        Code NVARCHAR(50) NOT NULL,        -- User Code
        Action NVARCHAR(100) NOT NULL,
        Details NVARCHAR(500) NULL,
        Timestamp DATETIME DEFAULT GETDATE()
    );
    PRINT 'EntityAudit table created.';
END
ELSE
BEGIN
    PRINT 'EntityAudit table already exists.';
END
GO

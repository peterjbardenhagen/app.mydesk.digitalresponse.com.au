-- Migration 019: User Profile Photos
-- Purpose: Enable users to upload, crop, and display profile photos
-- Features: Photo storage, cropping metadata, circular avatar display with fallback to initials

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserPhotos')
BEGIN
    CREATE TABLE dbo.UserPhotos (
        PhotoId INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        TenantId INT NOT NULL,

        -- Original upload metadata
        OriginalFileName NVARCHAR(255) NOT NULL,
        OriginalContentType VARCHAR(50) NOT NULL,  -- image/jpeg, image/png
        OriginalSizeBytes BIGINT NOT NULL,
        UploadedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Processed photo paths
        StoragePath NVARCHAR(500) NOT NULL,  -- /tenant/{id}/photos/{userId}/{guid}.jpg
        ThumbnailPath NVARCHAR(500),          -- Optional: /tenant/{id}/photos/{userId}/{guid}_thumb.jpg

        -- Crop metadata (for UI to remember last crop)
        CropLeft INT DEFAULT 0,
        CropTop INT DEFAULT 0,
        CropWidth INT DEFAULT 0,
        CropHeight INT DEFAULT 0,

        -- Photo dimensions after processing
        ProcessedWidth INT NOT NULL DEFAULT 500,  -- Square dimension
        ProcessedHeight INT NOT NULL DEFAULT 500,

        -- Status & audit
        Status VARCHAR(20) NOT NULL DEFAULT 'Active',  -- Active, Archived, Deleted
        CreatedBy INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedBy INT,
        ModifiedAt DATETIME2,

        CONSTRAINT FK_UserPhotos_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_UserPhotos_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        INDEX IX_UserPhotos_UserId_Active (UserId, Status),
        INDEX IX_UserPhotos_TenantId (TenantId),
        INDEX IX_UserPhotos_CreatedAt (CreatedAt)
    );
    PRINT 'Created table: UserPhotos';
END

-- Add PhotoId column to Users table for quick lookup of current photo
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'CurrentPhotoId')
BEGIN
    ALTER TABLE dbo.Users ADD CurrentPhotoId INT NULL;
    ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_CurrentPhoto FOREIGN KEY (CurrentPhotoId) REFERENCES dbo.UserPhotos(PhotoId);
    CREATE INDEX IX_Users_CurrentPhotoId ON dbo.Users(CurrentPhotoId);
    PRINT 'Added CurrentPhotoId column to Users table';
END

-- Audit table for photo changes (compliance requirement)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserPhotoAudit')
BEGIN
    CREATE TABLE dbo.UserPhotoAudit (
        AuditId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        UserId INT NOT NULL,
        PhotoId INT,
        Action VARCHAR(50) NOT NULL,  -- Uploaded, Cropped, Activated, Deleted
        Details NVARCHAR(MAX),
        IpAddress VARCHAR(45),
        UserAgent NVARCHAR(500),
        AuditedBy INT,
        AuditedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_UserPhotoAudit_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_UserPhotoAudit_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        INDEX IX_UserPhotoAudit_UserId (UserId),
        INDEX IX_UserPhotoAudit_AuditedAt (AuditedAt),
        INDEX IX_UserPhotoAudit_TenantId (TenantId)
    );
    PRINT 'Created table: UserPhotoAudit';
END

-- Add storage configuration table for photo settings per tenant
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TenantPhotoSettings')
BEGIN
    CREATE TABLE dbo.TenantPhotoSettings (
        SettingId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL UNIQUE,

        -- Photo constraints
        MaxPhotoSizeBytes BIGINT NOT NULL DEFAULT 5242880,  -- 5 MB default
        AllowedContentTypes VARCHAR(200) NOT NULL DEFAULT 'image/jpeg,image/png',

        -- Processing settings
        SquareDimensionPixels INT NOT NULL DEFAULT 500,
        ThumbnailDimensionPixels INT NOT NULL DEFAULT 100,
        CompressionQuality INT NOT NULL DEFAULT 85,  -- JPEG quality 1-100

        -- Display settings
        ShowCircularAvatarInLists BIT NOT NULL DEFAULT 1,
        FallbackToInitials BIT NOT NULL DEFAULT 1,

        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAt DATETIME2,

        CONSTRAINT FK_TenantPhotoSettings_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId)
    );
    PRINT 'Created table: TenantPhotoSettings';
END

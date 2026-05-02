-- Migration 007: Create Favourites Table
-- Ensures the favourites system has a backing database table.

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Favourites')
BEGIN
    CREATE TABLE Favourites (
        FavouriteId INT IDENTITY(1,1) PRIMARY KEY,
        UserCode NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(50) NOT NULL,
        EntityId INT NOT NULL,
        EntityName NVARCHAR(255) NULL,
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
    PRINT 'Favourites table created.';
END
GO

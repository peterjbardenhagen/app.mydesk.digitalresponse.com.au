-- Migration 011: Create StaffWhereabouts table
-- Date: 2026-05-02
-- Purpose: Track staff whereabouts Monday-Friday, 8am-6pm

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StaffWhereabouts')
BEGIN
    CREATE TABLE StaffWhereabouts (
        WhereaboutId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        UserName NVARCHAR(100) NOT NULL,
        WeekStartDate DATE NOT NULL, -- Monday of the week
        DayOfWeek INT NOT NULL, -- 1=Monday, 2=Tuesday, ..., 5=Friday
        TimeSlot INT NOT NULL, -- 0=8am, 1=9am, ..., 10=6pm (hourly slots)
        Status NVARCHAR(50) NOT NULL DEFAULT 'Available', -- Available, In Office, WFH, In Meeting, On Leave, Client Visit, Lunch, Out Of Office
        Location NVARCHAR(200) NULL,
        Notes NVARCHAR(500) NULL,
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_StaffWhereabouts_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );

    -- Create index for weekly view queries
    CREATE UNIQUE INDEX IX_StaffWhereabouts_UserWeekDayTime ON StaffWhereabouts(UserId, WeekStartDate, DayOfWeek, TimeSlot);
    CREATE INDEX IX_StaffWhereabouts_WeekStartDate ON StaffWhereabouts(WeekStartDate DESC);
    CREATE INDEX IX_StaffWhereabouts_Status ON StaffWhereabouts(Status);

    PRINT 'StaffWhereabouts table created successfully';
END
ELSE
BEGIN
    -- Add missing columns if they don't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StaffWhereabouts') AND name = 'Location')
        ALTER TABLE StaffWhereabouts ADD Location NVARCHAR(200) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StaffWhereabouts') AND name = 'Notes')
        ALTER TABLE StaffWhereabouts ADD Notes NVARCHAR(500) NULL;

    PRINT 'StaffWhereabouts table columns verified/updated';
END

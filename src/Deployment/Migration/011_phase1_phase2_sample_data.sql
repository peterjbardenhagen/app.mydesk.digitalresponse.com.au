-- Migration 011: Add sample data for Phase 1 & 2 mobile modules
-- Populates Expenses, Timesheets, Tasks, Despatch, Contacts, CashFlow, Goals, and Projects
-- with realistic demo data for Demo Lighting and Carter Capner Law tenants

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 1: Define Tenant and User IDs
-- ══════════════════════════════════════════════════════════════════════════════

DECLARE @DemoLightingTenantId UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555555555';
DECLARE @CarterCapnerTenantId UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';
DECLARE @DemoUserId INT = (SELECT TOP 1 UserId FROM Users WHERE UPPER(Code) = 'DEMO');
DECLARE @PeterUserId INT = (SELECT TOP 1 UserId FROM Users WHERE Email = 'peter@bardenhagen.xyz');

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 2: Add Sample Expenses for Demo Lighting
-- ══════════════════════════════════════════════════════════════════════════════

IF @DemoUserId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Expenses WHERE Reference = 'EXP-DL-001' AND TenantId = @DemoLightingTenantId)
    BEGIN
        INSERT INTO Expenses (Reference, EmployeeId, TenantId, Description, [Status], TotalAmount, CurrencyCode, [Date], CreatedAt)
        VALUES
            ('EXP-DL-001', @DemoUserId, @DemoLightingTenantId, 'Melbourne Conference Travel', 'Submitted', 1250.00, 'AUD', DATEADD(DAY, -10, GETDATE()), GETDATE()),
            ('EXP-DL-002', @DemoUserId, @DemoLightingTenantId, 'Client Lunch - Solar Systems', 'Approved', 185.50, 'AUD', DATEADD(DAY, -8, GETDATE()), GETDATE()),
            ('EXP-DL-003', @DemoUserId, @DemoLightingTenantId, 'Equipment Training Materials', 'Draft', 450.00, 'AUD', GETDATE(), GETDATE());

        -- Add line items for first expense
        DECLARE @ExpenseId1 INT = (SELECT TOP 1 ExpenseId FROM Expenses WHERE Reference = 'EXP-DL-001' AND TenantId = @DemoLightingTenantId);
        IF @ExpenseId1 IS NOT NULL
        BEGIN
            INSERT INTO ExpenseItems (ExpenseId, Category, Description, Amount, [Date], TenantId, CreatedAt)
            VALUES
                (@ExpenseId1, 'Travel', 'Flight MEL-SYD return', 600.00, DATEADD(DAY, -10, GETDATE()), @DemoLightingTenantId, GETDATE()),
                (@ExpenseId1, 'Accommodation', 'Hotel 2 nights', 400.00, DATEADD(DAY, -9, GETDATE()), @DemoLightingTenantId, GETDATE()),
                (@ExpenseId1, 'Travel', 'Taxi and local transport', 250.00, DATEADD(DAY, -8, GETDATE()), @DemoLightingTenantId, GETDATE());
        END

        -- Add line items for second expense
        DECLARE @ExpenseId2 INT = (SELECT TOP 1 ExpenseId FROM Expenses WHERE Reference = 'EXP-DL-002' AND TenantId = @DemoLightingTenantId);
        IF @ExpenseId2 IS NOT NULL
        BEGIN
            INSERT INTO ExpenseItems (ExpenseId, Category, Description, Amount, [Date], TenantId, CreatedAt)
            VALUES
                (@ExpenseId2, 'Meals', 'Lunch with client at Chin Chin', 185.50, DATEADD(DAY, -8, GETDATE()), @DemoLightingTenantId, GETDATE());
        END

        PRINT 'Created sample expenses for Demo Lighting';
    END
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 3: Add Sample Timesheets for Demo Lighting
-- ══════════════════════════════════════════════════════════════════════════════

IF @DemoUserId IS NOT NULL
BEGIN
    DECLARE @WeekStart1 DATE = DATEADD(DAY, -7 - (DATEPART(DW, GETDATE()) - 1), CAST(GETDATE() AS DATE));
    DECLARE @WeekStart2 DATE = DATEADD(DAY, -14 - (DATEPART(DW, GETDATE()) - 1), CAST(GETDATE() AS DATE));

    IF NOT EXISTS (SELECT 1 FROM Timesheets WHERE Reference = 'TS-DL-001' AND TenantId = @DemoLightingTenantId)
    BEGIN
        INSERT INTO Timesheets (Reference, EmployeeId, TenantId, WeekStartDate, [Status], TotalHours, CreatedAt)
        VALUES
            ('TS-DL-001', @DemoUserId, @DemoLightingTenantId, @WeekStart2, 'Approved', 40.0, GETDATE()),
            ('TS-DL-002', @DemoUserId, @DemoLightingTenantId, @WeekStart1, 'Submitted', 38.5, GETDATE());

        -- Add entries for first timesheet
        DECLARE @TimesheetId1 INT = (SELECT TOP 1 TimesheetId FROM Timesheets WHERE Reference = 'TS-DL-001' AND TenantId = @DemoLightingTenantId);
        IF @TimesheetId1 IS NOT NULL
        BEGIN
            INSERT INTO TimesheetEntries (TimesheetId, [Date], ProjectName, [Description], Hours, TenantId, CreatedAt)
            VALUES
                (@TimesheetId1, DATEADD(DAY, 0, @WeekStart2), 'Solar Systems - Chatfield', 'Site survey and measurements', 8.0, @DemoLightingTenantId, GETDATE()),
                (@TimesheetId1, DATEADD(DAY, 1, @WeekStart2), 'Solar Systems - Chatfield', 'Design documentation and quotes', 8.0, @DemoLightingTenantId, GETDATE()),
                (@TimesheetId1, DATEADD(DAY, 2, @WeekStart2), 'Smart Lighting - CBD', 'Installation supervision', 8.0, @DemoLightingTenantId, GETDATE()),
                (@TimesheetId1, DATEADD(DAY, 3, @WeekStart2), 'Smart Lighting - CBD', 'System testing and commissioning', 8.0, @DemoLightingTenantId, GETDATE()),
                (@TimesheetId1, DATEADD(DAY, 4, @WeekStart2), 'Administrative', 'Client reporting and invoicing', 8.0, @DemoLightingTenantId, GETDATE());
        END

        -- Add entries for second timesheet
        DECLARE @TimesheetId2 INT = (SELECT TOP 1 TimesheetId FROM Timesheets WHERE Reference = 'TS-DL-002' AND TenantId = @DemoLightingTenantId);
        IF @TimesheetId2 IS NOT NULL
        BEGIN
            INSERT INTO TimesheetEntries (TimesheetId, [Date], ProjectName, [Description], Hours, TenantId, CreatedAt)
            VALUES
                (@TimesheetId2, DATEADD(DAY, 0, @WeekStart1), 'Bright LED - Commercial', 'Tender preparation', 8.0, @DemoLightingTenantId, GETDATE()),
                (@TimesheetId2, DATEADD(DAY, 1, @WeekStart1), 'Bright LED - Commercial', 'Technical specifications', 8.0, @DemoLightingTenantId, GETDATE()),
                (@TimesheetId2, DATEADD(DAY, 2, @WeekStart1), 'Smart Lighting - CBD', 'Warranty follow-up and support', 7.5, @DemoLightingTenantId, GETDATE()),
                (@TimesheetId2, DATEADD(DAY, 3, @WeekStart1), 'Training and Development', 'LED technology certification course', 8.0, @DemoLightingTenantId, GETDATE()),
                (@TimesheetId2, DATEADD(DAY, 4, @WeekStart1), 'Administrative', 'Monthly reporting and timesheets', 7.0, @DemoLightingTenantId, GETDATE());
        END

        PRINT 'Created sample timesheets for Demo Lighting';
    END
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 4: Add Sample Tasks for Demo Lighting
-- ══════════════════════════════════════════════════════════════════════════════

IF @DemoUserId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Tasks WHERE Reference = 'TSK-DL-001' AND TenantId = @DemoLightingTenantId)
    BEGIN
        INSERT INTO Tasks (Reference, Title, [Description], [Status], Priority, DueDate, ProjectName, TenantId, CreatedById, CreatedAt)
        VALUES
            ('TSK-DL-001', 'Complete Solar Systems site survey', 'Conduct detailed site survey and generate report for Chatfield installation', 'Done', 'High', DATEADD(DAY, -5, GETDATE()), 'Solar Systems - Chatfield', @DemoLightingTenantId, @DemoUserId, GETDATE()),
            ('TSK-DL-002', 'Prepare installation timeline', 'Develop detailed project schedule for Smart Lighting CBD project', 'InProgress', 'High', DATEADD(DAY, 3, GETDATE()), 'Smart Lighting - CBD', @DemoLightingTenantId, @DemoUserId, GETDATE()),
            ('TSK-DL-003', 'Order LED panels and equipment', 'Submit purchase order for LED panels and control systems', 'ToDo', 'Normal', DATEADD(DAY, 5, GETDATE()), 'Bright LED - Commercial', @DemoLightingTenantId, @DemoUserId, GETDATE()),
            ('TSK-DL-004', 'Client approval for design changes', 'Obtain written approval from Solar Systems for design modifications', 'InProgress', 'High', DATEADD(DAY, 2, GETDATE()), 'Solar Systems - Chatfield', @DemoLightingTenantId, @DemoUserId, GETDATE()),
            ('TSK-DL-005', 'Safety inspection and certification', 'Complete electrical safety inspection before handover', 'ToDo', 'Urgent', DATEADD(DAY, 7, GETDATE()), 'Smart Lighting - CBD', @DemoLightingTenantId, @DemoUserId, GETDATE());

        PRINT 'Created sample tasks for Demo Lighting';
    END
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 5: Add Sample Despatch Records for Demo Lighting
-- ══════════════════════════════════════════════════════════════════════════════

IF @DemoUserId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Despatch WHERE Reference = 'DSP-DL-001' AND TenantId = @DemoLightingTenantId)
    BEGIN
        INSERT INTO Despatch (Reference, OrderReference, DeliveryDate, [Status], RecipientName, RecipientAddress, RecipientPhone, TenantId, CreatedAt)
        VALUES
            ('DSP-DL-001', 'PO-2026-001', DATEADD(DAY, 3, GETDATE()), 'Pending', 'Solar Systems Pty Ltd', '100 Solar Road, Melbourne VIC', '03 9000 1234', @DemoLightingTenantId, GETDATE()),
            ('DSP-DL-002', 'INV-2026-001', DATEADD(DAY, 1, GETDATE()), 'InTransit', 'Bright LED Solutions', '200 LED Lane, Sydney NSW', '03 9000 5678', @DemoLightingTenantId, GETDATE()),
            ('DSP-DL-003', 'DL-ORD-345', DATEADD(DAY, -2, GETDATE()), 'Delivered', 'Smart Lighting Co', '300 Tech Street, Brisbane QLD', '07 3000 9999', @DemoLightingTenantId, GETDATE());

        -- Add items for first despatch
        DECLARE @DespatchId1 INT = (SELECT TOP 1 DespatchId FROM Despatch WHERE Reference = 'DSP-DL-001' AND TenantId = @DemoLightingTenantId);
        IF @DespatchId1 IS NOT NULL
        BEGIN
            INSERT INTO DespatchItems (DespatchId, LineNumber, Description, Quantity, Unit, TenantId, CreatedAt)
            VALUES
                (@DespatchId1, 1, 'Solar Panel 5kW System', 5, 'unit', @DemoLightingTenantId, GETDATE()),
                (@DespatchId1, 2, 'Inverter 10kWh with battery storage', 1, 'unit', @DemoLightingTenantId, GETDATE());
        END

        -- Add items for third despatch (delivered)
        DECLARE @DespatchId3 INT = (SELECT TOP 1 DespatchId FROM Despatch WHERE Reference = 'DSP-DL-003' AND TenantId = @DemoLightingTenantId);
        IF @DespatchId3 IS NOT NULL
        BEGIN
            INSERT INTO DespatchItems (DespatchId, LineNumber, Description, Quantity, Unit, TenantId, CreatedAt)
            VALUES
                (@DespatchId3, 1, 'LED Downlights 10W', 50, 'unit', @DemoLightingTenantId, GETDATE()),
                (@DespatchId3, 2, 'Smart Dimmer Switches', 25, 'unit', @DemoLightingTenantId, GETDATE());

            -- Update to mark as delivered
            UPDATE Despatch SET DeliveredDate = DATEADD(DAY, -1, GETDATE()), DeliveredBy = @DemoUserId WHERE DespatchId = @DespatchId3;
        END

        PRINT 'Created sample despatch records for Demo Lighting';
    END
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 6: Add Sample Contacts for Demo Lighting and Carter Capner Law
-- ══════════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM Contacts WHERE Reference = 'CNT-DL-001' AND TenantId = @DemoLightingTenantId)
BEGIN
    INSERT INTO Contacts (Reference, FirstName, LastName, Email, Phone, Mobile, [Address], [Role], TenantId, CreatedAt)
    VALUES
        ('CNT-DL-001', 'John', 'Smith', 'john.smith@solarsystems.com.au', '03 9000 1234', '0412 345 678', '100 Solar Road, Melbourne VIC', 'Project Manager', @DemoLightingTenantId, GETDATE()),
        ('CNT-DL-002', 'Sarah', 'Johnson', 'sarah.j@brightled.com.au', '03 9000 5678', '0433 456 789', '200 LED Lane, Sydney NSW', 'Purchasing Manager', @DemoLightingTenantId, GETDATE()),
        ('CNT-DL-003', 'Michael', 'Chen', 'mchen@smartlighting.com.au', '07 3000 9999', '0422 111 222', '300 Tech Street, Brisbane QLD', 'Technical Lead', @DemoLightingTenantId, GETDATE()),
        ('CNT-DL-004', 'Emma', 'Wilson', 'emma@demolighting.com.au', '03 8000 0001', '0498 765 432', '50 Demo Way, Melbourne VIC', 'Sales Director', @DemoLightingTenantId, GETDATE());

    PRINT 'Created sample contacts for Demo Lighting';
END

-- Add contacts for Carter Capner Law if tenant exists
IF @CarterCapnerTenantId IS NOT NULL AND @PeterUserId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Contacts WHERE Reference = 'CNT-CCL-001' AND TenantId = @CarterCapnerTenantId)
    BEGIN
        INSERT INTO Contacts (Reference, FirstName, LastName, Email, Phone, Mobile, [Address], [Role], TenantId, CreatedAt)
        VALUES
            ('CNT-CCL-001', 'Peter', 'Bardenhagen', 'peter@bardenhagen.xyz', '03 8000 0002', '0401 111 222', '10 Legal Street, Melbourne VIC', 'Senior Lawyer', @CarterCapnerTenantId, GETDATE()),
            ('CNT-CCL-002', 'Rebecca', 'Carter', 'rebecca@cartercapner.com.au', '03 8000 0003', '0412 222 333', '10 Legal Street, Melbourne VIC', 'Managing Partner', @CarterCapnerTenantId, GETDATE()),
            ('CNT-CCL-003', 'David', 'Capner', 'david@cartercapner.com.au', '03 8000 0004', '0433 333 444', '10 Legal Street, Melbourne VIC', 'Partner', @CarterCapnerTenantId, GETDATE());

        PRINT 'Created sample contacts for Carter Capner Law';
    END
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 7: Add Sample Cash Flow Forecasts for Demo Lighting
-- ══════════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM CashFlowForecasts WHERE TenantId = @DemoLightingTenantId)
BEGIN
    DECLARE @ForecastDate DATE = CAST(GETDATE() AS DATE);
    DECLARE @WeekNum INT = 1;

    WHILE @WeekNum <= 12
    BEGIN
        INSERT INTO CashFlowForecasts (TenantId, ForecastDate, WeekNumber, ProjectedIncoming, ProjectedOutgoing, CashPosition, CreatedAt)
        VALUES
            (@DemoLightingTenantId, @ForecastDate, @WeekNum,
             CASE
                 WHEN @WeekNum IN (2, 5, 8, 11) THEN 35000.00  -- Invoice payment weeks
                 WHEN @WeekNum IN (3, 6, 9, 12) THEN 25000.00  -- Partial payments
                 ELSE 5000.00  -- Miscellaneous income
             END,
             CASE
                 WHEN @WeekNum IN (1, 4, 7, 10) THEN 22000.00  -- Major expense weeks
                 ELSE 8500.00   -- Regular operational expenses
             END,
             NULL,
             GETDATE());

        SET @ForecastDate = DATEADD(WEEK, 1, @ForecastDate);
        SET @WeekNum = @WeekNum + 1;
    END

    PRINT 'Created 12-week cash flow forecasts for Demo Lighting';
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 8: Add Sample Business Goals (KPIs) for Demo Lighting
-- ══════════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM BusinessGoals WHERE Reference = 'GOAL-DL-001' AND TenantId = @DemoLightingTenantId)
BEGIN
    INSERT INTO BusinessGoals (Reference, Title, [Description], TargetValue, CurrentValue, UnitOfMeasure, [Period], [Status], TenantId, CreatedAt)
    VALUES
        ('GOAL-DL-001', 'Q3 Revenue Target', 'Generate $500,000 in revenue by end of Q3', 500000.00, 385000.00, 'AUD', 'Quarterly', 'Active', @DemoLightingTenantId, GETDATE()),
        ('GOAL-DL-002', 'Project Completion Rate', 'Complete 95% of projects on time', 95.00, 87.00, '%', 'Quarterly', 'Active', @DemoLightingTenantId, GETDATE()),
        ('GOAL-DL-003', 'Customer Satisfaction', 'Achieve NPS score of 75+', 75.00, 68.00, 'NPS', 'Annual', 'Active', @DemoLightingTenantId, GETDATE()),
        ('GOAL-DL-004', 'Sales Team Growth', 'Add 3 new team members to sales division', 3.00, 1.00, 'headcount', 'Annual', 'Active', @DemoLightingTenantId, GETDATE()),
        ('GOAL-DL-005', 'Safety Record', 'Zero lost-time injuries for 52 weeks', 0.00, 0.00, 'incidents', 'Annual', 'Active', @DemoLightingTenantId, GETDATE());

    PRINT 'Created sample business goals for Demo Lighting';
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 9: Extend Projects Table with Sample Data
-- ══════════════════════════════════════════════════════════════════════════════

IF EXISTS (SELECT 1 FROM sys.columns WHERE Object_ID = Object_ID('Projects') AND Name = 'Health')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Projects WHERE ProjectCode = 'DL-PROJ-001' AND TenantId = @DemoLightingTenantId)
    BEGIN
        INSERT INTO Projects (ProjectCode, ProjectName, [Status], [Percent], Health, Milestone, TenantId, DateCreated)
        VALUES
            ('DL-PROJ-001', 'Solar Systems - Chatfield', 'Active', 65, 'On Track', 'Design approval completed; awaiting installation schedule', @DemoLightingTenantId, GETDATE()),
            ('DL-PROJ-002', 'Smart Lighting - CBD', 'Active', 82, 'At Risk', 'Installation progressing; minor delays in permit approvals', @DemoLightingTenantId, GETDATE()),
            ('DL-PROJ-003', 'Bright LED - Commercial', 'Planning', 15, 'On Track', 'Initial site survey scheduled for week of 24th July', @DemoLightingTenantId, GETDATE()),
            ('DL-PROJ-004', 'LED Retrofit - Government Building', 'Active', 45, 'Behind Schedule', 'Awaiting approval from infrastructure dept', @DemoLightingTenantId, GETDATE());

        PRINT 'Created sample extended projects for Demo Lighting';
    END
END

PRINT '✓ Phase 1 & 2 sample data created successfully';
PRINT '  - Expenses: 3 records with line items';
PRINT '  - Timesheets: 2 records with 10 entries';
PRINT '  - Tasks: 5 records';
PRINT '  - Despatch: 3 records with items';
PRINT '  - Contacts: 4 for Demo Lighting, 3 for Carter Capner Law';
PRINT '  - Cash Flow Forecasts: 12-week forecast';
PRINT '  - Business Goals: 5 KPI goals';
PRINT '  - Projects: 4 extended project records';

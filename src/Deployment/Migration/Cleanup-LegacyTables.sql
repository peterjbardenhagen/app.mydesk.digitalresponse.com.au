-- =============================================================================
-- Techlight MyDesk — drop legacy/retired tables from Techlight_MyDesk DB
-- Safe to run multiple times (IF EXISTS). Mirrors EXCLUDED_TABLES in
-- migration_config.py so re-migration + cleanup produce an identical schema.
-- =============================================================================

USE [Techlight_MyDesk];
GO

-- Drop FKs that reference any table we're about to drop ---------------------
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))
           + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id))
           + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';' + CHAR(10)
FROM sys.foreign_keys
WHERE OBJECT_NAME(referenced_object_id) IN (
    'CallReports','CallReportTypes',
    'ExpenseTypeGroups','ExpenseTypes','ExpensesSignOffs',
    'FilesCategoriesUserLevelAccess',
    'FilesCategoriesUserAccess','FilesCategoriesDivisionAccess',
    'Employment','TMail',
    'TimesheetItems','Timesheets','TimesheetStatus',
    'Tables','TableFiles',
    'Paste Errors','ContactsDef',
    'SalesResults','SalesResults_ByCustomer',
    'MarketSeg','ProjectHistory'
);
EXEC sp_executesql @sql;
GO

-- Drop the tables themselves ------------------------------------------------
DROP TABLE IF EXISTS [CallReports];
DROP TABLE IF EXISTS [CallReportTypes];

DROP TABLE IF EXISTS [ExpensesSignOffs];
-- NOTE: We KEEP [Expenses] table as it's still in use!
DROP TABLE IF EXISTS [ExpenseTypes];
DROP TABLE IF EXISTS [ExpenseTypeGroups];

DROP TABLE IF EXISTS [FilesCategoriesUserLevelAccess];
DROP TABLE IF EXISTS [FilesCategoriesUserAccess];
DROP TABLE IF EXISTS [FilesCategoriesDivisionAccess];
DROP TABLE IF EXISTS [FilesCategories];
DROP TABLE IF EXISTS [Files];

DROP TABLE IF EXISTS [Employment];
DROP TABLE IF EXISTS [TMail];

DROP TABLE IF EXISTS [TimesheetItems];
DROP TABLE IF EXISTS [Timesheets];
DROP TABLE IF EXISTS [TimesheetStatus];

DROP TABLE IF EXISTS [TableFiles];
DROP TABLE IF EXISTS [Tables];

DROP TABLE IF EXISTS [Paste Errors];
DROP TABLE IF EXISTS [ContactsDef];

DROP TABLE IF EXISTS [SalesResults_ByCustomer];
DROP TABLE IF EXISTS [SalesResults];

DROP TABLE IF EXISTS [MarketSeg];
DROP TABLE IF EXISTS [ProjectHistory];
GO

-- Report what's left --------------------------------------------------------
SELECT
    t.name                                            AS TableName,
    SUM(p.rows)                                       AS [RowCount]
FROM sys.tables t
JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0,1)
GROUP BY t.name
ORDER BY t.name;
GO

PRINT 'Cleanup complete.';

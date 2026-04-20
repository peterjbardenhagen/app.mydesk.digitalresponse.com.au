#!/usr/bin/env python3
"""
Configuration for Access to SQL Server Migration
Edit these settings for your environment
"""

# ============================================================================
# REQUIRED: Path to your Microsoft Access database
# ============================================================================
ACCESS_DB_PATH = r"C:\Development\Techlight.digitalresponse.com.au\Database\Techlight2.mdb"

# ============================================================================
# REQUIRED: SQL Server Connection Settings
# ============================================================================

# Option 1: Windows Authentication (most common)
SQL_SERVER_CONN_STR = (
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=localhost;"              # Or your server name\instance
    "Database=Techlight;"              # Target database (must exist)
    "Trusted_Connection=yes;"          # Use Windows login
)

# Option 2: SQL Server Authentication (comment out Option 1 and use this)
# SQL_SERVER_CONN_STR = (
#     "Driver={ODBC Driver 17 for SQL Server};"
#     "Server=localhost;"
#     "Database=Techlight;"
#     "UID=sa;"                        # SQL Server username
#     "PWD=your_password;"             # SQL Server password
# )

# ============================================================================
# OPTIONAL: Migration Settings
# ============================================================================

# Number of rows to insert at once (higher = faster but more memory)
BATCH_SIZE = 1000

# If True, drop and recreate existing tables (WARNING: destroys existing data!)
DROP_EXISTING_TABLES = True

# If True, create foreign keys after data migration
CREATE_FOREIGN_KEYS = True

# If True, stop on first error. If False, continue and log errors
STOP_ON_ERROR = False

# Tables to skip (if any)
SKIP_TABLES = []  # Example: ['TempTable', 'OldData']

# ============================================================================
# Advanced: Data Type Overrides (optional)
# Use this to force specific data types for specific columns
# ============================================================================

# Format: 'TableName.ColumnName': 'SQL_Server_Type'
DATA_TYPE_OVERRIDES = {
    # Example: Force 'Notes' column in 'Customers' table to NVARCHAR(MAX)
    # 'Customers.Notes': 'NVARCHAR(MAX)',
}

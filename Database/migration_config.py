#!/usr/bin/env python3
"""
Configuration for Access to SQL Server Migration
Edit these settings for your environment
"""

# ============================================================================
# REQUIRED: Path to your Microsoft Access database
# ============================================================================
ACCESS_DB_PATH = r"C:\Development\Techlight.digitalresponse.com.au\Database\AccessDB\Techlight2.mdb"

# ============================================================================
# REQUIRED: SQL Server Connection Settings
# ============================================================================
# Target database: Techlight_MyDesk (must already exist)
# Compatibility: SQL Server 2016+ (production runs SQL Server 2016)

# Option 1: LOCAL DEV - LocalDB with Windows Authentication (DEFAULT)
SQL_SERVER_CONN_STR = (
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=(localdb)\\MSSQLLocalDB;"
    "Database=Techlight_MyDesk;"
    "Trusted_Connection=yes;"
    "Encrypt=yes;"
    "TrustServerCertificate=no;"
)

# Option 2: LOCAL DEV - SQL Authentication (if Windows Auth fails)
# SQL_SERVER_CONN_STR = (
#     "Driver={ODBC Driver 17 for SQL Server};"
#     "Server=(localdb)\\MSSQLLocalDB;"
#     "Database=Techlight_MyDesk;"
#     "UID=Techlight_MyDesk;"
#     "PWD=DigitalResponse2595!;"
#     "Encrypt=yes;"
#     "TrustServerCertificate=yes;"
# )

# Option 3: PRODUCTION - SQL Server 2016 on techlight.digitalresponse.com.au
# PROD_SQL_SERVER_CONN_STR = (
#     "Driver={ODBC Driver 17 for SQL Server};"
#     "Server=localhost\\SQL2016;"
#     "Database=Techlight_MyDesk;"
#     "UID=Techlight_MyDesk;"
#     "PWD=DigitalResponse2595!;"
#     "Encrypt=yes;"
#     "TrustServerCertificate=yes;"
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

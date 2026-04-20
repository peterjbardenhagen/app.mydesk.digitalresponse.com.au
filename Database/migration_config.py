import os

# Microsoft Access to SQL Server Migration Configuration

ACCESS_DB_PATH = r"C:\Database\Techlight2.mdb"

# Connect to master first if we need to create the database, but for simplicity let's assume we'll pre-create it 
# or the script will try. Actually, the script uses the connection string and we can use local DB.
# TrustServerCertificate=True is sometimes needed for newer ODBC drivers
# LOCAL DEV - LocalDB with Windows Authentication
# Note: LocalDB does NOT support encryption, so Encrypt=no is required
SQL_SERVER_CONN_STR = (
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=(localdb)\\MSSQLLocalDB;"
    "Database=Techlight_MyDesk;"
    "Trusted_Connection=yes;"
    "Encrypt=no;"
)

# ALTERNATIVE: SQL Auth to LocalDB (uncomment if Windows Auth fails)
# SQL_SERVER_CONN_STR = (
#     "Driver={ODBC Driver 17 for SQL Server};"
#     "Server=(localdb)\\MSSQLLocalDB;"
#     "Database=Techlight_MyDesk;"
#     "UID=Techlight_MyDesk;"
#     "PWD=DigitalResponse2595!;"
#     "TrustServerCertificate=yes;"
# )

# PRODUCTION - SQL Server 2016 (for reference - DON'T use in migration!)
# PROD_SQL_SERVER_CONN_STR = (
#     "Driver={ODBC Driver 17 for SQL Server};"
#     "Server=localhost\\SQL2016;"
#     "Database=Techlight_MyDesk;"
#     "UID=Techlight_MyDesk;"
#     "PWD=DigitalResponse2595!;"
#     "TrustServerCertificate=yes;"
# )

# Number of rows to insert in a single batch
BATCH_SIZE = 1000

# Whether to drop existing tables in the target database before migrating
DROP_EXISTING_TABLES = True

# Whether to recreate foreign keys after data migration
CREATE_FOREIGN_KEYS = True

# Whether to stop on the first error or continue with other tables
STOP_ON_ERROR = False

# List of tables to skip (e.g., ['TempTable', 'BackupTable'])
SKIP_TABLES = []

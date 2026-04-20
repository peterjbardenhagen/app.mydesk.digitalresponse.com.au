import os

# Microsoft Access to SQL Server Migration Configuration

ACCESS_DB_PATH = r"C:\Development\Techlight.digitalresponse.com.au\Database\Techlight2.mdb"

# Connect to master first if we need to create the database, but for simplicity let's assume we'll pre-create it 
# or the script will try. Actually, the script uses the connection string and we can use local DB.
# TrustServerCertificate=True is sometimes needed for newer ODBC drivers
SQL_SERVER_CONN_STR = (
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=localhost\\SQLEXPRESS;"
    "Database=Techlight;"
    "Trusted_Connection=yes;"
    "TrustServerCertificate=True;"
)

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

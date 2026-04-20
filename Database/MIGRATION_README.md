# Access to SQL Server Migration Tool

Complete migration tool that converts Microsoft Access databases to SQL Server, including:
- ✅ All tables with proper data types
- ✅ Primary keys
- ✅ Foreign keys
- ✅ All data (batch migrated)
- ✅ Views (manual SQL required)

## Prerequisites

### 1. Install Python
Download and install Python 3.8+ from https://python.org

### 2. Install pyodbc
```bash
pip install pyodbc
```

### 3. Install ODBC Drivers

#### For Access:
- **64-bit Windows**: Download "Microsoft Access Database Engine 2016 Redistributable" (64-bit)
- **32-bit Windows**: Download "Microsoft Access Database Engine 2016 Redistributable" (32-bit)
  - https://www.microsoft.com/en-us/download/details.aspx?id=54920

#### For SQL Server:
- Download "ODBC Driver 17 for SQL Server" or "ODBC Driver 18 for SQL Server"
  - https://docs.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server

### 4. Setup SQL Server

Create the target database first:
```sql
CREATE DATABASE Techlight;
```

## Configuration

Edit `migrate_access_to_sqlserver.py` and modify these values:

```python
# Path to your Access database
ACCESS_DB_PATH = r"C:\Development\Techlight.digitalresponse.com.au\Database\Techlight2.mdb"

# SQL Server connection string
SQL_SERVER_CONN_STR = (
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=localhost;"           # Your SQL Server instance
    "Database=Techlight;"         # Target database name
    "Trusted_Connection=yes;"     # Windows Authentication
)
```

### Connection String Examples

**Windows Authentication:**
```python
SQL_SERVER_CONN_STR = (
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=localhost;"
    "Database=Techlight;"
    "Trusted_Connection=yes;"
)
```

**SQL Server Authentication:**
```python
SQL_SERVER_CONN_STR = (
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=localhost;"
    "Database=Techlight;"
    "UID=sa;"                     # Username
    "PWD=your_password;"          # Password
)
```

**Named Instance:**
```python
SQL_SERVER_CONN_STR = (
    "Driver={ODBC Driver 17 for SQL Server};"
    "Server=localhost\SQLEXPRESS;"  # Instance name
    "Database=Techlight;"
    "Trusted_Connection=yes;"
)
```

## Running the Migration

```bash
cd "C:\Development\Techlight.digitalresponse.com.au\Database"
python migrate_access_to_sqlserver.py
```

The tool will:
1. Show you what it will migrate
2. Ask for confirmation
3. Create tables in SQL Server
4. Migrate all data in batches
5. Create foreign keys
6. Create views (if SQL is available)

## Data Type Mapping

| Access Type | SQL Server Type |
|-------------|-----------------|
| COUNTER (AutoNumber) | INT IDENTITY(1,1) |
| INTEGER | INT |
| LONG | BIGINT |
| SHORT | SMALLINT |
| BYTE | TINYINT |
| SINGLE | REAL |
| DOUBLE | FLOAT |
| NUMERIC/DECIMAL | DECIMAL(18,6) |
| CURRENCY | MONEY |
| BOOLEAN/YESNO | BIT |
| DATETIME | DATETIME2 |
| DATE | DATE |
| TIME | TIME |
| TEXT/VARCHAR | NVARCHAR(n) |
| MEMO/LONGTEXT | NVARCHAR(MAX) |
| BINARY | VARBINARY |
| OLE | VARBINARY(MAX) |
| GUID | UNIQUEIDENTIFIER |
| HYPERLINK | NVARCHAR(2048) |

## Troubleshooting

### "Driver not found" Error
Install the Microsoft Access Database Engine 2016 Redistributable from Microsoft.

### "Cannot open database" Error
Ensure the Access database is not open in Access. Close Microsoft Access first.

### Permission Denied
Ensure you have write permissions to both the Access file location and SQL Server.

### Data Truncation
If you get truncation errors, the tool may need adjustment for specific column sizes. Check the migration.log file.

### Foreign Key Failures
If foreign keys fail to create due to orphaned data:
1. Check migration.log for details
2. Clean up orphaned records in Access first
3. Or, comment out the FK creation section and add them manually later

## Post-Migration Steps

After migration, you'll need to manually:

1. **Recreate Views**: Access queries need to be manually converted to SQL Server views
2. **Update Connection Strings**: Change your Classic ASP to use SQL Server:
   ```asp
   ' Old Access connection
   conn.Open "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & Server.MapPath("/Database/Techlight2.mdb")
   
   ' New SQL Server connection
   conn.Open "Provider=SQLOLEDB;Server=localhost;Database=Techlight;Trusted_Connection=yes;"
   ```

3. **Test Queries**: Some Access-specific SQL syntax may need adjustment:
   - `IIF()` → `CASE WHEN`
   - `NZ()` → `ISNULL()`
   - `Date()` → `GETDATE()`
   - `&` concatenation → `+` or `CONCAT()`

## Alternative: SSMA (Microsoft Tool)

Microsoft provides SQL Server Migration Assistant (SSMA) for Access:
- https://docs.microsoft.com/en-us/sql/ssma/access/

This is a GUI tool that may be easier for one-time migrations, but the Python script gives you more control and can be automated.

## Support

Check `migration.log` for detailed error information if the migration fails.

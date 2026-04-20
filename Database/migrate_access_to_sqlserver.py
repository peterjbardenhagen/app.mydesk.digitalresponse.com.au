#!/usr/bin/env python3
"""
Microsoft Access to SQL Server Migration Tool
Migrates: Tables, Columns, Data Types, Primary Keys, Foreign Keys, Data, Views
"""

import pyodbc
import sys
from typing import List, Dict, Tuple, Optional
from dataclasses import dataclass
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('migration.log'),
        logging.StreamHandler(sys.stdout)
    ]
)
logger = logging.getLogger(__name__)


@dataclass
class ColumnInfo:
    name: str
    data_type: str
    max_length: Optional[int]
    is_nullable: bool
    is_identity: bool
    default_value: Optional[str]


@dataclass
class TableInfo:
    name: str
    columns: List[ColumnInfo]
    primary_keys: List[str]


@dataclass
class ForeignKeyInfo:
    name: str
    table: str
    column: str
    ref_table: str
    ref_column: str
    on_update: str
    on_delete: str


@dataclass
class ViewInfo:
    name: str
    sql: str


class AccessToSqlServerMigrator:
    """Migrates Microsoft Access database to SQL Server"""

    # Data type mapping: Access -> SQL Server
    TYPE_MAPPING = {
        'COUNTER': 'INT IDENTITY(1,1)',  # AutoNumber
        'INTEGER': 'INT',
        'LONG': 'BIGINT',
        'SHORT': 'SMALLINT',
        'BYTE': 'TINYINT',
        'SINGLE': 'REAL',
        'DOUBLE': 'FLOAT',
        'NUMERIC': 'DECIMAL(18, 6)',
        'DECIMAL': 'DECIMAL(18, 6)',
        'CURRENCY': 'MONEY',
        'BOOLEAN': 'BIT',
        'BIT': 'BIT',
        'DATETIME': 'DATETIME2',
        'DATE': 'DATE',
        'TIME': 'TIME',
        'TEXT': 'NVARCHAR',  # Will add length
        'VARCHAR': 'NVARCHAR',
        'MEMO': 'NVARCHAR(MAX)',
        'LONGTEXT': 'NVARCHAR(MAX)',
        'OLE': 'VARBINARY(MAX)',
        'BINARY': 'VARBINARY',
        'GUID': 'UNIQUEIDENTIFIER',
        'HYPERLINK': 'NVARCHAR(2048)',
    }

    def __init__(self, access_db_path: str, sql_server_conn_str: str,
                 batch_size: int = 1000,
                 drop_existing: bool = True,
                 create_fks: bool = True,
                 stop_on_error: bool = False,
                 skip_tables: list = None):
        """
        Initialize migrator
        
        Args:
            access_db_path: Full path to .mdb or .accdb file
            sql_server_conn_str: SQL Server connection string
            batch_size: Rows to insert per batch
            drop_existing: Drop existing tables before creating
            create_fks: Create foreign keys after data migration
            stop_on_error: Stop on first error if True
            skip_tables: List of table names to skip
        """
        self.access_db_path = access_db_path
        self.sql_server_conn_str = sql_server_conn_str
        self.batch_size = batch_size
        self.drop_existing = drop_existing
        self.create_fks = create_fks
        self.stop_on_error = stop_on_error
        self.skip_tables = skip_tables or []
        self.access_conn = None
        self.sql_conn = None
        
    def connect_access(self) -> pyodbc.Connection:
        """Connect to Access database with validation and driver auto-detection"""
        import os
        
        # Validate the Access DB file exists FIRST
        if not os.path.isfile(self.access_db_path):
            logger.error(f"Access database file not found: {self.access_db_path}")
            logger.error(f"Please verify ACCESS_DB_PATH in migration_config.py")
            raise FileNotFoundError(f"Access database not found: {self.access_db_path}")
        
        file_size_mb = os.path.getsize(self.access_db_path) / (1024 * 1024)
        logger.info(f"Found Access DB: {self.access_db_path} ({file_size_mb:.2f} MB)")
        
        # Detect available Access drivers
        available_drivers = [d for d in pyodbc.drivers() if 'Access' in d]
        logger.info(f"Available Access drivers: {available_drivers}")
        
        if not available_drivers:
            logger.error("=" * 70)
            logger.error("NO MICROSOFT ACCESS ODBC DRIVER FOUND!")
            logger.error("=" * 70)
            logger.error("You need to install the Microsoft Access Database Engine.")
            logger.error("")
            logger.error("Download (pick ONE that matches your Python architecture):")
            logger.error("  64-bit Python -> AccessDatabaseEngine_X64.exe")
            logger.error("  32-bit Python -> AccessDatabaseEngine.exe")
            logger.error("")
            logger.error("URL: https://www.microsoft.com/en-us/download/details.aspx?id=54920")
            logger.error("")
            logger.error("Check your Python architecture with:")
            logger.error("  python -c \"import platform; print(platform.architecture())\"")
            logger.error("=" * 70)
            
            # Report which architecture Python is running
            import platform
            arch = platform.architecture()[0]
            bits = '64-bit' if '64' in arch else '32-bit'
            logger.error(f"Your Python is: {bits}")
            logger.error(f"You need the {bits} Access Database Engine installer")
            
            raise RuntimeError("Microsoft Access ODBC Driver not installed")
        
        # Try each available driver in preferred order
        # Prefer the newer .mdb, .accdb combined driver
        preferred_order = [
            '{Microsoft Access Driver (*.mdb, *.accdb)}',
            'Microsoft Access Driver (*.mdb, *.accdb)',
            '{Microsoft Access Driver (*.mdb)}',
            'Microsoft Access Driver (*.mdb)',
        ]
        
        # Match available drivers against preferred order
        drivers_to_try = []
        for pref in preferred_order:
            pref_clean = pref.strip('{}')
            for avail in available_drivers:
                if avail == pref_clean and avail not in drivers_to_try:
                    drivers_to_try.append(avail)
        
        # Fallback: any driver we found
        for avail in available_drivers:
            if avail not in drivers_to_try:
                drivers_to_try.append(avail)
        
        last_error = None
        for driver_name in drivers_to_try:
            conn_str = f'DRIVER={{{driver_name}}};DBQ={self.access_db_path};'
            try:
                conn = pyodbc.connect(conn_str)
                logger.info(f"Connected using driver: {driver_name}")
                return conn
            except pyodbc.Error as e:
                logger.warning(f"Driver '{driver_name}' failed: {e}")
                last_error = e
        
        logger.error(f"All available drivers failed to connect")
        raise last_error

    def connect_sql_server(self) -> pyodbc.Connection:
        """Connect to SQL Server"""
        try:
            conn = pyodbc.connect(self.sql_server_conn_str)
            logger.info("Connected to SQL Server")
            return conn
        except pyodbc.Error as e:
            logger.error(f"Failed to connect to SQL Server: {e}")
            raise

    def get_access_tables(self) -> List[str]:
        """Get list of user tables from Access"""
        cursor = self.access_conn.cursor()
        # Query MSysObjects for user tables (Type=1, not system)
        cursor.execute("""
            SELECT Name 
            FROM MSysObjects 
            WHERE Type = 1 
            AND Name NOT LIKE 'MSys%' 
            AND Name NOT LIKE '~%'
            AND Name NOT LIKE 'f_'
            ORDER BY Name
        """)
        tables = [row[0] for row in cursor.fetchall()]
        logger.info(f"Found {len(tables)} tables: {tables}")
        return tables

    def get_access_table_schema(self, table_name: str) -> TableInfo:
        """Get column information for a table"""
        cursor = self.access_conn.cursor()
        
        columns = []
        primary_keys = []
        
        # Get column info using ADO-like schema
        for row in cursor.columns(table=table_name):
            col_name = row.column_name
            data_type = row.type_name.upper()
            max_length = row.column_size
            is_nullable = row.is_nullable == 1
            default_value = row.column_def
            
            # Check if it's an AutoNumber (COUNTER in Access)
            is_identity = data_type == 'COUNTER'
            
            col_info = ColumnInfo(
                name=col_name,
                data_type=data_type,
                max_length=max_length,
                is_nullable=is_nullable,
                is_identity=is_identity,
                default_value=default_value
            )
            columns.append(col_info)
        
        # Get primary keys
        for row in cursor.primaryKeys(table_name):
            primary_keys.append(row[3])  # Column name is 4th field
            
        return TableInfo(name=table_name, columns=columns, primary_keys=primary_keys)

    def get_foreign_keys(self) -> List[ForeignKeyInfo]:
        """Get all foreign keys from Access database"""
        cursor = self.access_conn.cursor()
        fks = []
        
        for table in self.get_access_tables():
            try:
                for row in cursor.foreignKeys(table):
                    # Row structure: [pktable_cat, pktable_schem, pktable_name, pkcolumn_name,
                    #                 fktable_cat, fktable_schem, fktable_name, fkcolumn_name,
                    #                 key_seq, update_rule, delete_rule, fk_name, pk_name]
                    fk = ForeignKeyInfo(
                        name=row[11] or f"FK_{table}_{row[3]}",
                        table=table,
                        column=row[7],
                        ref_table=row[2],
                        ref_column=row[3],
                        on_update=row[9] or 'NO ACTION',
                        on_delete=row[10] or 'NO ACTION'
                    )
                    fks.append(fk)
            except Exception as e:
                logger.warning(f"Could not get FKs for {table}: {e}")
                
        logger.info(f"Found {len(fks)} foreign keys")
        return fks

    def get_views(self) -> List[ViewInfo]:
        """Get all views (queries) from Access"""
        cursor = self.access_conn.cursor()
        views = []
        
        # Views in Access are stored as Type=5 in MSysObjects
        cursor.execute("""
            SELECT Name 
            FROM MSysObjects 
            WHERE Type = 5 
            AND Name NOT LIKE '~%'
            ORDER BY Name
        """)
        
        for row in cursor.fetchall():
            view_name = row[0]
            try:
                # Get the SQL from the query
                cursor.execute(f"SELECT TOP 1 * FROM [{view_name}]")
                # We can't easily get the SQL text in Access via ODBC
                # Would need to use DAO or Access Automation
                views.append(ViewInfo(name=view_name, sql=""))
            except Exception as e:
                logger.warning(f"Could not get view {view_name}: {e}")
                
        logger.info(f"Found {len(views)} views")
        return views

    def map_data_type(self, column: ColumnInfo) -> str:
        """Map Access data type to SQL Server"""
        access_type = column.data_type
        
        if access_type in self.TYPE_MAPPING:
            sql_type = self.TYPE_MAPPING[access_type]
            
            # Handle variable-length types
            if 'NVARCHAR' in sql_type and column.max_length:
                # Access max_length is in bytes, unicode needs 2x
                length = min(column.max_length, 4000)
                if access_type in ('MEMO', 'LONGTEXT'):
                    sql_type = 'NVARCHAR(MAX)'
                else:
                    sql_type = f'NVARCHAR({length})'
            elif 'VARBINARY' in sql_type and column.max_length:
                if column.max_length > 8000:
                    sql_type = 'VARBINARY(MAX)'
                else:
                    sql_type = f'VARBINARY({column.max_length})'
                    
            return sql_type
        else:
            logger.warning(f"Unknown type '{access_type}', using NVARCHAR(255)")
            return 'NVARCHAR(255)'

    def generate_create_table_sql(self, table: TableInfo) -> str:
        """Generate CREATE TABLE SQL for SQL Server"""
        columns_sql = []
        
        for col in table.columns:
            sql_type = self.map_data_type(col)
            
            # Handle identity
            if col.is_identity:
                sql_type = sql_type.replace('INT', 'INT IDENTITY(1,1)')
                
            nullable = 'NULL' if col.is_nullable else 'NOT NULL'
            
            col_def = f"    [{col.name}] {sql_type} {nullable}"
            columns_sql.append(col_def)
        
        # Add primary key constraint
        if table.primary_keys:
            pk_cols = ', '.join(f'[{pk}]' for pk in table.primary_keys)
            columns_sql.append(f"    CONSTRAINT [PK_{table.name}] PRIMARY KEY CLUSTERED ({pk_cols})")
        
        columns_str = ',\n'.join(columns_sql)
        sql = f"CREATE TABLE [{table.name}] (\n{columns_str}\n)"
        
        return sql

    def generate_create_fk_sql(self, fk: ForeignKeyInfo) -> str:
        """Generate ALTER TABLE ADD FOREIGN KEY SQL"""
        sql = f"""
ALTER TABLE [{fk.table}]
    ADD CONSTRAINT [{fk.name}] 
    FOREIGN KEY ([{fk.column}]) 
    REFERENCES [{fk.ref_table}]([{fk.ref_column}])
    ON UPDATE {fk.on_update}
    ON DELETE {fk.on_delete}
"""
        return sql

    def create_tables(self, tables: List[TableInfo]):
        """Create tables in SQL Server"""
        cursor = self.sql_conn.cursor()
        
        for table in tables:
            try:
                # Drop if exists (only if drop_existing is True)
                if self.drop_existing:
                    cursor.execute(f"IF OBJECT_ID('[{table.name}]', 'U') IS NOT NULL DROP TABLE [{table.name}]")
                else:
                    # Check if table exists
                    cursor.execute(f"SELECT COUNT(*) FROM sys.tables WHERE name = '{table.name}'")
                    if cursor.fetchone()[0] > 0:
                        logger.info(f"Table {table.name} already exists, skipping")
                        continue
                
                # Create table
                sql = self.generate_create_table_sql(table)
                cursor.execute(sql)
                logger.info(f"Created table: {table.name}")
            except pyodbc.Error as e:
                logger.error(f"Failed to create table {table.name}: {e}")
                logger.error(f"SQL: {sql}")
                if self.stop_on_error:
                    raise
                else:
                    continue
                    
        self.sql_conn.commit()

    def migrate_data(self, tables: List[TableInfo]):
        """Migrate data from Access to SQL Server"""
        access_cursor = self.access_conn.cursor()
        sql_cursor = self.sql_conn.cursor()
        
        for table in tables:
            # Skip if table is in skip_tables list
            if table.name in self.skip_tables:
                logger.info(f"Skipping table {table.name} (in skip list)")
                continue
                
            try:
                # Get column names (exclude identity columns)
                identity_cols = [c.name for c in table.columns if c.is_identity]
                data_cols = [c.name for c in table.columns if not c.is_identity]
                
                if not data_cols:
                    logger.info(f"Skipping data migration for {table.name} (all columns are identity)")
                    continue
                
                cols_str = ', '.join(f'[{c}]' for c in data_cols)
                placeholders = ', '.join('?' for _ in data_cols)
                
                # Select from Access
                access_cursor.execute(f"SELECT {cols_str} FROM [{table.name}]")
                
                # Insert into SQL Server in batches
                batch = []
                row_count = 0
                
                insert_sql = f"INSERT INTO [{table.name}] ({cols_str}) VALUES ({placeholders})"
                
                for row in access_cursor:
                    batch.append(row)
                    
                    if len(batch) >= self.batch_size:
                        sql_cursor.executemany(insert_sql, batch)
                        row_count += len(batch)
                        batch = []
                        
                # Insert remaining rows
                if batch:
                    sql_cursor.executemany(insert_sql, batch)
                    row_count += len(batch)
                    
                self.sql_conn.commit()
                logger.info(f"Migrated {row_count} rows to {table.name}")
                
            except pyodbc.Error as e:
                logger.error(f"Failed to migrate data for {table.name}: {e}")
                self.sql_conn.rollback()
                if self.stop_on_error:
                    raise
                else:
                    logger.warning(f"Continuing after error in {table.name}")
                    continue

    def create_foreign_keys(self, fks: List[ForeignKeyInfo]):
        """Create foreign keys after data is migrated"""
        cursor = self.sql_conn.cursor()
        
        for fk in fks:
            try:
                # Drop if exists
                cursor.execute(f"""
                    IF EXISTS (SELECT * FROM sys.foreign_keys 
                               WHERE name = '{fk.name}' AND parent_object_id = OBJECT_ID('{fk.table}'))
                    ALTER TABLE [{fk.table}] DROP CONSTRAINT [{fk.name}]
                """)
                
                # Create FK
                sql = self.generate_create_fk_sql(fk)
                cursor.execute(sql)
                logger.info(f"Created FK: {fk.name}")
            except pyodbc.Error as e:
                logger.error(f"Failed to create FK {fk.name}: {e}")
                if self.stop_on_error:
                    raise
                else:
                    logger.warning(f"Continuing after FK error")
                    continue
                    
        self.sql_conn.commit()

    def create_views(self, views: List[ViewInfo]):
        """Create views in SQL Server"""
        cursor = self.sql_conn.cursor()
        
        for view in views:
            try:
                # Drop if exists
                cursor.execute(f"""
                    IF OBJECT_ID('[{view.name}]', 'V') IS NOT NULL 
                    DROP VIEW [{view.name}]
                """)
                
                # Note: We'd need the actual SQL text to recreate views
                # Access doesn't easily expose this via ODBC
                logger.warning(f"View {view.name} needs manual recreation - Access SQL not available via ODBC")
                
            except pyodbc.Error as e:
                logger.error(f"Failed to create view {view.name}: {e}")
                
        self.sql_conn.commit()

    def migrate(self, create_database: bool = False, database_name: str = None):
        """
        Run complete migration
        
        Args:
            create_database: If True, create database before migration
            database_name: Name of database to create (if create_database=True)
        """
        try:
            # Connect to databases
            self.access_conn = self.connect_access()
            
            if create_database and database_name:
                # Connect to master first to create database
                master_conn_str = self.sql_server_conn_str.replace(
                    self.sql_server_conn_str.split(';')[-2].split('=')[1] if '=' in self.sql_server_conn_str.split(';')[-2] else '',
                    'master'
                )
                # Actually, need better connection string parsing
                temp_conn = pyodbc.connect(self.sql_server_conn_str)
                temp_conn.autocommit = True
                cursor = temp_conn.cursor()
                cursor.execute(f"CREATE DATABASE [{database_name}]")
                logger.info(f"Created database: {database_name}")
                temp_conn.close()
            
            self.sql_conn = self.connect_sql_server()
            self.sql_conn.autocommit = False
            
            # Step 1: Get schema
            logger.info("=== STEP 1: Reading Access Schema ===")
            tables = []
            for table_name in self.get_access_tables():
                table_info = self.get_access_table_schema(table_name)
                tables.append(table_info)
                
            foreign_keys = self.get_foreign_keys()
            views = self.get_views()
            
            # Step 2: Create tables
            logger.info("=== STEP 2: Creating Tables in SQL Server ===")
            self.create_tables(tables)
            
            # Step 3: Migrate data
            logger.info("=== STEP 3: Migrating Data ===")
            self.migrate_data(tables)
            
            # Step 4: Create foreign keys (after data to avoid constraint violations)
            if self.create_fks:
                logger.info("=== STEP 4: Creating Foreign Keys ===")
                self.create_foreign_keys(foreign_keys)
            else:
                logger.info("=== STEP 4: Skipping Foreign Keys (create_fks=False) ===")
            
            # Step 5: Create views
            logger.info("=== STEP 5: Creating Views ===")
            self.create_views(views)
            
            logger.info("=== MIGRATION COMPLETE ===")
            
        except Exception as e:
            logger.error(f"Migration failed: {e}")
            raise
        finally:
            if self.access_conn:
                self.access_conn.close()
            if self.sql_conn:
                self.sql_conn.close()


def main():
    """Example usage"""
    
    # Import configuration from config file
    try:
        from migration_config import (
            ACCESS_DB_PATH,
            SQL_SERVER_CONN_STR,
            BATCH_SIZE,
            DROP_EXISTING_TABLES,
            CREATE_FOREIGN_KEYS,
            STOP_ON_ERROR,
            SKIP_TABLES
        )
    except ImportError:
        # Fallback to defaults if config file doesn't exist
        print("Warning: migration_config.py not found. Using default settings.")
        ACCESS_DB_PATH = r"C:\Development\Techlight.digitalresponse.com.au\Database\Techlight2.mdb"
        SQL_SERVER_CONN_STR = (
            "Driver={ODBC Driver 17 for SQL Server};"
            "Server=localhost;"
            "Database=Techlight;"
            "Trusted_Connection=yes;"
        )
        BATCH_SIZE = 1000
        DROP_EXISTING_TABLES = True
        CREATE_FOREIGN_KEYS = True
        STOP_ON_ERROR = False
        SKIP_TABLES = []
    
    print("=" * 60)
    print("Microsoft Access to SQL Server Migration Tool")
    print("=" * 60)
    print(f"Source: {ACCESS_DB_PATH}")
    print(f"Target: SQL Server")
    print(f"Batch Size: {BATCH_SIZE} rows")
    print("=" * 60)
    
    # Confirm before proceeding
    confirm = input("\nWARNING: This will DROP and recreate tables in SQL Server!\nContinue? (yes/no): ")
    if confirm.lower() != 'yes':
        print("Migration cancelled.")
        return
    
    try:
        migrator = AccessToSqlServerMigrator(
            ACCESS_DB_PATH, 
            SQL_SERVER_CONN_STR,
            batch_size=BATCH_SIZE,
            drop_existing=DROP_EXISTING_TABLES,
            create_fks=CREATE_FOREIGN_KEYS,
            stop_on_error=STOP_ON_ERROR,
            skip_tables=SKIP_TABLES
        )
        migrator.migrate()
        print("\n✓ Migration completed successfully!")
        print("Check migration.log for details.")
    except Exception as e:
        print(f"\n✗ Migration failed: {e}")
        print("Check migration.log for details.")
        sys.exit(1)


if __name__ == "__main__":
    main()

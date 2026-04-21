# Database

Techlight MyDesk uses SQL Server as its database. The application connects directly to the existing Techlight database.

## Connection

The connection string is configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=Techlight;Trusted_Connection=True;"
  }
}
```

## Database Objects

### Views
The `Views.sql` file contains custom SQL Server views used by the application for reporting and data aggregation.

### Legacy Migration Scripts
The following files are retained for historical reference but no longer needed:
- `migrate_access_to_sqlserver.py` - Original Access to SQL migration tool
- `Backup-Database.ps1` - Database backup utility
- `Deploy-Database.ps1` - Deployment script

## Schema

The application works with the existing Techlight database schema. Key tables include:

- **Quotes** - Customer quotations
- **Invoices** - Billing records
- **PurchaseOrders** - Procurement records
- **Contacts** - Customer contacts
- **Companies** - Customer and supplier companies
- **Products** - Product catalog
- **Users** - System users and permissions

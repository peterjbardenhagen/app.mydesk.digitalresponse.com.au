# MyDeskV3 - Multi-Client Platform Architecture

## Client Platforms

| Platform | Slug | Description | Integrations |
|----------|------|-------------|--------------|
| **Techlight** | `techlight` | Commercial electrical projects | MYOB, Xero (planned) |
| **Digital Response** | `digital-response` | Business management consulting | QuickBooks, Frollo |
| **Carter Capner Law** | `carter-capner-law` | Legal services firm | Xero, bank feeds |

## Isolation Strategy

- **Multi-tenant database schema** - `CompanyId` foreign key on all tables
- **Platform-specific branding** - Dynamic loaded from `PlatformSettings`
- **Separate IIS applications** - One site per platform with different bindings
- **Role-based access control** - Platform-scoped user permissions

## IIS Deployment

Each platform deploys to:
- Techlight: `http://pb-legion/Techlight`
- Digital Response: `http://pb-legion/DigitalResponse`  
- Carter Capner Law: `http://pb-legion/CarterCapnerLaw`

## Composio Integration

### QuickBooks Setup (Digital Response)
```
1. Navigate to /integrations
2. Click "Connect QuickBooks"
3. Authorise via Intuit OAuth
4. Bank accounts auto-sync via Frollo
```

### Frollo Bank Feeds
- Automatic statement fetching every 24 hours
- Real-time transaction categorisation
- Direct feed into reconciliation engine

## Key Services

- `BankReconciliationService` - Bank statement processing
- `EndOfMonthAccountingService` - Month-end close automation
- `ComposioIntegrationService` - QuickBooks/Frollo API bridge
- `SupplierQuoteParseService` - AI-powered quote extraction

## Environment Variables

```
COMPOSIO_API_KEY=your_key_here
QUICKBOOKS_CLIENT_ID=your_client_id
QUICKBOOKS_CLIENT_SECRET=your_secret
FROLLO_INSTITUTION_ID=your_institution
FROLLO_ACCOUNT_ID=your_account
```
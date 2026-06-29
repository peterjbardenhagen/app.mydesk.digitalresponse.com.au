# Composio Integration Setup Guide

## QuickBooks Integration (Digital Response)

### 1. Get Composio API Key
Navigate to https://composio.dev and sign in to obtain your API key.

### 2. Add to appsettings.json
```json
{
  "Composio": {
    "ApiKey": "YOUR_COMPOSIO_API_KEY",
    "ApiUrl": "https://backend.composio.dev/api/v1"
  }
}
```

### 3. Register in DI Container (Program.cs)
```csharp
builder.Services.Configure<ComposioOptions>(builder.Configuration.GetSection("Composio"));
builder.Services.AddHttpClient("Composio", c => {
    c.BaseAddress = new Uri("https://backend.composio.dev/api/v1");
});
```

### 4. Platform-Specific Configuration
For Digital Response platform, set in `platformsettings.json`:
```json
{
  "EnableQuickBooksIntegration": true,
  "QuickBooks": {
    "Enabled": true,
    "SyncInvoices": true,
    "SyncBills": true,
    "AutoExportInvoices": true
  }
}
```

## Frollo Bank Feed Integration

### 1. Setup Bank Account Mapping
Each Digital Response client gets:
- Frollo Institution ID (bank identifier)
- Frollo Account ID (specific account)
- Auto-sync enabled (24-hour interval)

### 2. Bank Reconciliation Flow
1. Frollo fetches daily bank statements
2. Transactions stored in BankTransactions table
3. ReconciliationService matches to invoices/purchase orders
4. EndOfMonthAccountingService generates reconciliation reports

## Multi-Client Isolation

| Platform | QuickBooks | Frollo | Notes |
|----------|------------|--------|-------|
| Techlight | No | No | Uses MYOB integration |
| Digital Response | Yes | Yes | Full accounting automation |
| Carter Capner Law | No | No | Xero focus |

## IIS Deployment (pb-legion)

Run as Administrator on Windows host:
```powershell
cd C:\Development\Techlight-Projects\Techlight.digitalresponse.com.au
.\Deploy-To-IIS.ps1
```

Creates sites:
- http://pb-legion/Techlight
- http://pb-legion/DigitalResponse
- http://pb-legion/CarterCapnerLaw
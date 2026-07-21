# Supplier Quote AI Automation - Techlight Demo

## Executive Summary

**Problem:** Manual supplier quote entry is time-consuming and error-prone. Electricians spend 30-45 minutes per quote transcribing line items, costs, and descriptions.

**Solution:** MyDeskV3 Supplier Quote AI Automation extracts data from supplier quotes (PDF, JPG, PNG) in under 10 seconds.

## Demo Flow

### 1. Upload
Drag & drop supplier quote PDF/image onto the upload zone.

### 2. AI Extraction
- Azure Document Intelligence extracts text/tables
- AI identifies line items, quantities, unit costs
- Extracts supplier name, ABN, contact details

### 3. Auto-Populate
- Creates MyDesk quote with line items pre-filled
- GP margin (30%) automatically applied
- Pricing validated against market benchmarks

### 4. Review & Send
- One-click review of extracted items
- Manual adjustments if needed
- Generate quote for client

## Technical Implementation

| Component | Technology | Purpose |
|-----------|------------|---------|
| Quote Parser | Azure Document Intelligence | OCR + table extraction |
| AI Validation | Azure OpenAI GPT-4 | Line item classification |
| Pricing Engine | Azure OpenAI | Market price comparison |
| Export | Composio → QuickBooks | Direct invoice creation |

## Demo Data (Clipsal Supplier Quote)

| Item | Quantity | Supplier Cost | MyDesk Price (30% GP) |
|------|----------|---------------|----------------------|
| Clipsal Saturn 2 Gang Switch Plate | 4 | $24.95 | $35.64 |
| Clipsal Saturn Single Switch Mechanism | 4 | $18.50 | $25.71 |
| Switch Installation (labour) | 2 | $85.00 | $123.81 |
| Metro Delivery | 1 | $35.00 | $50.00 |

**Total:** $299 supplier → $608 retail

## Bank Reconciliation Integration

When quotes convert to invoices and sync to QuickBooks:
- Frollo automatically fetches daily bank statements
- Reconciliation service matches invoices to deposits
- End-of-month close generates automated reports

## Platform Isolation

| Platform | URL | QuickBooks | Frollo |
|----------|-----|------------|--------|
| Techlight | `http://pb-legion/Techlight` | No | No |
| Digital Response | `http://pb-legion/DigitalResponse` | Yes | Yes |
| Carter Capner Law | `http://pb-legion/CarterCapnerLaw` | No | No (Xero) |

## IIS Deployment Command

Run as Administrator on Windows host:
```powershell
cd C:\Development\Techlight-Projects\Techlight.digitalresponse.com.au
.\Deploy-To-IIS.ps1
```

---

**Ready for demo — 10-second quote creation from supplier PDFs.**
# 50 — ASP.NET Interop Layer

Status: **IN REVIEW** — verified against source in `MyDeskASPNet/`.

The ASP.NET Interop layer provides modern .NET capabilities to the legacy Classic ASP application. It handles PDF generation, document rendering, and other tasks that are better suited to .NET Framework than VBScript.

---

## 1. Architecture Overview

### 1.1 Purpose

The Classic ASP application (VBScript) calls out to ASP.NET handlers for:
- PDF generation using ABCpdf
- Complex rendering tasks
- Document conversion

### 1.2 Communication Flow

```
Classic ASP Page
    ↓ HTTP Redirect/POST
ASP.NET Handler (.aspx)
    ↓ Server-side processing
PDF/Document Generated
    ↓ File saved or streamed
Return to ASP or Download
```

### 1.3 Project Structure

```
/MyDeskASPNet/
├── GenerateQuote.aspx              → Quote PDF generation
├── GenerateQuote.aspx.cs           → C# code-behind
├── GenerateInvoice.aspx            → Invoice PDF generation
├── GenerateInvoice.aspx.cs         → C# code-behind
├── GeneratePurchaseOrder.aspx      → PO PDF generation
├── GeneratePurchaseOrder.aspx.cs   → C# code-behind
├── GenerateDeliveryNote.aspx       → DN PDF generation
├── GenerateDeliveryNote.aspx.cs    → C# code-behind
├── ScrapeToPDF.aspx                → Generic page-to-PDF
├── ScrapeToPDF.aspx.cs             → C# code-behind
├── Web.config                      → Configuration
└── bin/                            → Compiled assemblies
```

---

## 2. PDF Generation Handlers

### 2.1 GenerateQuote.aspx

**Purpose**: Render Quote as PDF document.

**Entry Points**:
- `Quotes/GenerateQuote.asp` → redirects here
- `JobOrders/GenerateQuote.asp` (VB.NET fallback) → redirects here

**Parameters** (Query String):
| Parameter | Description |
|---|---|
| `Qid` | Quote ID to render |
| `WorkingDir` | Client working directory (e.g., "/Clients/SalesEngineTL") |
| `Print` | Force print mode styling |
| `Email` | Force email mode styling |

**Process**:
1. Accept Qid parameter
2. Load Quote data via database query
3. Use ABCpdf to render `Quotes/View.asp?Qid=<n>&Email=True`
4. Apply Techlight styling template
5. Save to `/Quotes/Files/Quote.pdf`
6. Stream to browser or attach to email

### 2.2 GenerateInvoice.aspx

**Purpose**: Render Invoice as PDF document.

**Entry Points**:
- `Invoices/GenerateInvoice.asp` → redirects here

**Parameters**:
| Parameter | Description |
|---|---|
| `InvoiceId` | Invoice ID to render |
| `WorkingDir` | Client working directory |
| `Email` | Email mode flag |

**Process**:
1. Load Invoice header and line items
2. Render `Invoices/View.asp?InvoiceId=<n>&Email=True`
3. Apply division logo and styling
4. Save to `/Invoices/Files/Invoice.pdf`
5. Handle delivery note generation if needed

### 2.3 GeneratePurchaseOrder.aspx

**Purpose**: Render Purchase Order as PDF document.

**Entry Points**:
- `PurchaseOrders/GeneratePO.asp` → redirects here

**Parameters**:
| Parameter | Description |
|---|---|
| `POid` | PO ID to render |
| `WorkingDir` | Client working directory |
| `Email` | Email mode flag |

**Process**:
1. Load PO header and contents
2. Render `PurchaseOrders/View.asp?POid=<n>&Email=True`
3. Apply supplier-specific formatting
4. Save to `/PurchaseOrders/Files/PurchaseOrder.pdf`

### 2.4 GenerateDeliveryNote.aspx

**Purpose**: Render Delivery Note as PDF document.

**Entry Points**:
- `Invoices/GenerateDeliveryNote.asp` → redirects here

**Parameters**:
| Parameter | Description |
|---|---|
| `InvoiceId` | Source invoice ID |
| `WorkingDir` | Client working directory |

**Process**:
1. Load Invoice data (delivery notes are derived from invoices)
2. Render quantities and descriptions only (no pricing)
3. Apply delivery-specific template
4. Save to `/DeliveryNotes/Files/DeliveryNote.pdf`

### 2.5 ScrapeToPDF.aspx

**Purpose**: Generic HTML-to-PDF conversion.

**Parameters**:
| Parameter | Description |
|---|---|
| `Url` | Page URL to scrape |
| `OutputPath` | Where to save PDF |

**Use Case**: On-demand PDF generation for any page.

---

## 3. Technical Implementation

### 3.1 ABCpdf Library

All PDF generation uses the ABCpdf library (WebSupergoo):

```csharp
// Typical usage pattern (from .aspx.cs files)
Doc doc = new Doc();
doc.HtmlOptions.Engine = EngineType.Gecko;
doc.HtmlOptions.PageCache.Enabled = true;

// Add HTML page
string htmlUrl = GetPageUrl(quoteId);
doc.AddImageUrl(htmlUrl);

// Apply settings
doc.Rect.Inset(36, 36); // Margins
doc.Page = doc.AddPage();

// Save
string pdfPath = GetOutputPath();
doc.Save(pdfPath);
doc.Clear();
```

### 3.2 C# Code-Behind Structure

Each .aspx.cs file follows this pattern:

```csharp
public partial class Generate[Document] : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Get parameters
        int id = Convert.ToInt32(Request.QueryString["Id"]);
        string workingDir = Request.QueryString["WorkingDir"];
        
        // Generate PDF
        string pdfPath = GeneratePdf(id, workingDir);
        
        // Return or redirect
        if (Request.QueryString["Download"] == "true")
        {
            ServeFile(pdfPath);
        }
        else
        {
            Response.Redirect(ReturnUrl);
        }
    }
    
    private string GeneratePdf(int id, string workingDir)
    {
        // ABCpdf implementation
    }
}
```

### 3.3 Configuration (Web.config)

```xml
<configuration>
  <system.web>
    <compilation debug="true" targetFramework="4.8"/>
    <httpRuntime targetFramework="4.8"/>
    <sessionState mode="Off"/>
  </system.web>
  
  <system.webServer>
    <handlers>
      <add path="*.aspx" verb="*" 
           type="System.Web.UI.PageHandlerFactory"/>
    </handlers>
  </system.webServer>
</configuration>
```

---

## 4. Integration with Classic ASP

### 4.1 Redirect Pattern

Classic ASP pages redirect to ASP.NET handlers:

```asp
' PurchaseOrders/GeneratePO.asp
poId = Request("POid")
UpdateStatus(poId, 4) ' Mark as issued

' Redirect to .NET handler
Response.Redirect("/MyDeskASPNet/GeneratePurchaseOrder.aspx?POid=" & poId)
```

### 4.2 Fallback VB.NET Handler

Some modules include in-place VB.NET handlers as fallback:

```
JobOrders/GenerateQuote.aspx (.vb)
├── Uses ABCpdf
├── Same logic as /MyDeskASPNet/GenerateQuote.aspx
└── Used when /MyDeskASPNet/ unavailable
```

### 4.3 File Output Locations

| Document | Save Path |
|---|---|
| Quote PDF | `/Quotes/Files/Quote.pdf` |
| Invoice PDF | `/Invoices/Files/Invoice.pdf` |
| PO PDF | `/PurchaseOrders/Files/PurchaseOrder.pdf` |
| Delivery Note PDF | `/DeliveryNotes/Files/DeliveryNote.pdf` |

---

## 5. Security Considerations

### 5.1 Authentication

ASP.NET handlers rely on Classic ASP session management:
- Session cookie passed from ASP
- No separate login in .NET layer
- Relies on ASP `ssi_Security.inc` checks before redirect

### 5.2 Path Traversal Protection

```csharp
// WorkingDir validation
string workingDir = Request.QueryString["WorkingDir"];
if (!workingDir.StartsWith("/Clients/"))
{
    throw new SecurityException("Invalid working directory");
}
```

### 5.3 File Access

- PDFs saved to web-accessible directories
- Directory permissions restricted to Application Pool identity
- No direct database access from PDF files

---

## 6. Deployment

### 6.1 Build Process

1. Compile in Visual Studio
2. Copy to `/MyDeskASPNet/bin/`
3. Ensure ABCpdf.dll is in bin directory
4. Configure Web.config for environment

### 6.2 Dependencies

- .NET Framework 4.8
- ABCpdf library license
- Gecko engine (for HTML rendering)
- IIS with ASP.NET module

---

## 7. Known Baseline Issues

1. **Single PDF File**: Each handler overwrites the same filename (e.g., `Quote.pdf`) rather than generating per-ID files.

2. **No Queue System**: Simultaneous PDF requests may cause conflicts.

3. **Gecko Engine Dependency**: Requires Gecko installation for HTML rendering.

4. **Session State Off**: Cannot share session data between ASP and ASP.NET.

5. **Hardcoded Paths**: Output paths not configurable without code changes.

6. **No Async Support**: Synchronous rendering may timeout for complex pages.

---

## 8. Related Modules

- **10-Quotes.md** — Uses GenerateQuote.aspx
- **11-Invoices.md** — Uses GenerateInvoice.aspx, GenerateDeliveryNote.aspx
- **12-PurchaseOrders.md** — Uses GeneratePurchaseOrder.aspx
- **15-JobOrders.md** — Uses GenerateQuote.aspx

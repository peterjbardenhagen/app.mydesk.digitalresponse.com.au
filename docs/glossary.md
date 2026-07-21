## Canonical Meaning of Terms

### A
- **Aged Receivables**: Report showing overdue invoices grouped by aging buckets (30/60/90+ days)
- **AI Audit Log**: Record of all Ask AI interactions (stored in `AiAudit` table)
- **Approve**: Manager action on submitted timesheets (changes status to "Approved")

### B
- **Billable Time**: Client-facing work that can be invoiced (linked to Company/Project)
- **Branding**: Visual identity (logo, colors) configured in `/admin/brand-assets`

### C
- **Company**: Customer or supplier organisation (stored in `Companies` table)
- **Contact**: Individual at a company (stored in `Contacts` table)
- **CRM**: Customer Relationship Management module (contacts + companies)

### D
- **Dashboard**: Main landing page with stats, charts, and quick actions
- **DatabaseService**: Core data access layer (wraps `Microsoft.Data.SqlClient`)

### E
- **ErrorLog**: System error record (stored in `ErrorLogs` table)
- **Expense**: Business cost (travel, meals, etc.) linked to company/project

### F
- **File Library**: Document management with folder structure (stored in `FileLibrary` table)
- **Favourite**: Bookmarked record (quotes, invoices, etc.) for quick access

### I
- **Invoice**: Billed quote with payment status (Draft → Sent → Paid)
- **In/Out Board**: See **Staff Whereabouts**

### N
- **Non-Billable Time**: Internal work (admin, meetings) that cannot be invoiced

### P
- **PO**: Purchase Order (buying from suppliers)
- **Project**: Client project belonging to a Company (can have multiple timesheet entries)
- **Portal**: External access for customers (`/customer-login`) or suppliers (`/supplier-login`)

### Q
- **Quote**: Sales proposal with line items (Draft → Sent → Accepted → Invoiced)

### R
- **Report**: Generated, immutable document (stored in `Reports` table or PDF)

### S
- **Staff Whereabouts**: In/Out board showing staff availability (M-F, 8am-6pm)
- **Submission**: Timesheet action (changes status from "Draft" to "Submitted")
- **Supplier**: Company with `IsSupplier = 1`

### T
- **Timesheet**: Weekly time tracking (Draft → Submitted → Approved)
- **Timesheet Entry**: Individual time record (date, hours, billable/non-billable, company, project)
- **Timer**: Stopwatch widget for quick time entry (see Timesheets module)

### U
- **User**: Internal staff member (stored in `Users` table)
- **User Role**: Permission set (Administrator, Director, Accounts, etc.)

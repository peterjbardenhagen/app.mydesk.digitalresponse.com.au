## Domain Terminology
- **Company**: Customer or supplier organisation (stored in `Companies` table)
- **Contact**: Individual at a company (stored in `Contacts` table)
- **Quote**: Sales quote for products/services (linked to company)
- **Invoice**: Billed quote (status flows: Draft → Sent → Paid)
- **Purchase Order (PO)**: Purchase from suppliers
- **Timesheet**: Weekly time tracking for consultants (Draft → Submitted → Approved)
- **Billable Time**: Client-facing work (linked to Company/Project)
- **Non-Billable Time**: Internal work (admin, meetings, etc.)
- **Project**: Client project (belongs to a Company, can have multiple timesheet entries)
- **Staff Whereabouts**: In/Out board showing staff availability (M-F, 8am-6pm)

## User Personas
- **Consultant**: Tracks time, submits timesheets, views own data
- **Manager**: Approves timesheets, views team timesheets, manages projects
- **Administrator**: Full system access, user management, system configuration
- **Director**: Same as Admin plus financial oversight
- **Customer**: Portal access (view quotes, invoices, files)
- **Supplier**: Portal access (view POs, submit quotes)

## Regulatory or Industry Constraints
- **Data Residency**: Australia only (on-premise deployments)
- **No SaaS**: Must run on client infrastructure (air-gapped capable)
- **Aged Receivables**: Must track overdue invoices (30/60/90 days)
- **Audit Trail**: AI interactions logged via `AiAuditService`

## Business Priorities
1. **Time Tracking**: Accurate billable/non-billable time for invoicing
2. **Customer Management**: Efficient CRM with quick access to quotes/invoices
3. **Financial Visibility**: Real-time dashboards, aged receivables, reconciliation
4. **Document Management**: File library with company/folder organisation
5. **AI Integration**: Ask AI for business insights (Azure OpenAI)

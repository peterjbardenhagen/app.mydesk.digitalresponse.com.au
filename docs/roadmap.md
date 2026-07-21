## Recently Completed ✓
1. **Phase 1 & 2 Modules**: Complete implementation of 8 new modules (Expenses, Timesheets, Tasks, Despatch, Contacts, CashFlow, Goals, Projects) on both web and mobile.
2. **Mobile App**: Native Android app with PAT authentication, offline support, module gating, and full Phase 1/2 integration.
3. **Module Gating**: Tenant-specific module availability (e.g., Carter Capner Law excludes POs/Despatch).
4. **AI Chat Integration**: Desky ChatAgent on home screen and dedicated chat with full tool loop.
5. **Forgot Password Flow**: Complete password reset with token validation and strength checking.

## Short-term Goals
1. **Timesheet Approval**: Manager approval workflows for submitted timesheets.
2. **Expense Approval**: Expense claim review and approval process with delegated approvers.
3. **Task Comments**: Add collaborative comments to tasks with @mentions.
4. **Push Notifications**: Email/SMS alerts for overdue items, approvals, and reminders.
5. **Offline Sync**: Write-back queue for offline expense/timesheet entry with auto-sync.

## Explicit Future Plans
- **Advanced AI Features**: Expense scanning with OCR, automated quote generation from requirements.
- **Accounting Integrations**: MYOB, Xero sync for invoices and payments.
- **Barcode Scanning**: QR code scanning for despatch item verification.
- **Photo Capture**: Camera integration for expense receipts on mobile.
- **Biometric Login**: Fingerprint/Face unlock for mobile app.
- **Advanced Reporting**: Custom report builder, scheduled report delivery.

## Things Planned but Not Yet Built
- **Recurring Invoices**: Auto-generate invoices from quotes on schedule.
- **Inventory Management**: Product stock tracking, low-stock alerts.
- **Multi-currency Support**: Beyond AUD, for international consulting.
- **Client Portal Enhancements**: Online quote acceptance, invoice payment.
- **Document Generation**: PDF quotes/invoices with company branding.

## AI Development Direction
AI agents should:
- Implement features in the direction of short-term goals above.
- Add approval workflows to existing Phase 1/2 modules.
- Maintain backward compatibility with existing SQL Server schema.
- Prefer extending DatabaseService over introducing new data access patterns.
- Follow Timesheets/Tasks modules as template for future CRUD features.
- Keep mobile app synchronized with web app features (see MOBILE-APP.md).
- Use PAT authentication pattern for new mobile endpoints.

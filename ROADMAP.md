## Short-term Goals
1. **Timesheets Module**: Complete stopwatch integration, weekly submission workflow, manager approval process.
2. **Staff Whereabouts Enhancement**: Consolidate with Recusant Apps In/Out Board, add real-time status updates.
3. **Contact Stopwatch**: Add quick time entry directly from contact view.
4. **Build Stability**: Fix remaining CS0246 and CS0121 build errors in shared services.
5. **File Library Enhancements**: Complete thumbnail previews, add drag-and-drop upload.

## Explicit Future Plans
- **Mobile-responsive Improvements**: Better tablet/mobile experience for field consultants.
- **Advanced AI Features**: Expense scanning, automated quote generation from requirements.
- **Accounting Integrations**: MYOB, Xero sync for invoices and payments.
- **Notification System**: Email/SMS alerts for overdue invoices, timesheet approvals.
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
- Maintain backward compatibility with existing SQL Server schema.
- Prefer extending DatabaseService over introducing new data access patterns.
- Keep Timesheets module as template for future CRUD features.

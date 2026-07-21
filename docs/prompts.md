## AI Development Guidelines

When adding functionality:
- Prefer extending existing services over creating new abstractions
- Ask before introducing new patterns or dependencies
- Maintain backward compatibility with existing SQL Server schema
- Follow established code style (see CODE_GUIDELINES.md)

## Component Development
- Keep business logic in service layer, not in Razor components
- Use DatabaseService for all data access (no direct SQL in UI)
- Follow the Timesheets module pattern for new CRUD features
- Use MudBlazor v7.15.0 components (stable version)

## Database Guidelines
- Use Dictionary<string, object?> for parameterized SQL queries
- Call EnsureTableAsync() for new tables on first use
- Avoid SELECT * in production queries
- Use ADO.NET methods in DatabaseService (not Dapper directly)

## Time Tracking & Billing
- Timesheets are weekly (Monday-Sunday)
- Billable time must link to Company and/or Project
- Non-billable time is for internal work (admin, meetings)
- Stopwatch functionality should be easily accessible from Contacts and Staff Whereabouts

## UI/UX Expectations
- Match the Login page branding (hero section, gradients, fonts)
- Use the quick nav menu structure for new modules
- Maintain consistent spacing (MudPaper with pa-4, mb-4)
- Support both list and grid/view modes where applicable

## Error Handling
- Use try-catch in service methods with ILogger
- Show user-facing errors via Snackbar.Add()
- Let exceptions bubble up to UI layer (don't swallow silently)
- Log with message templates: _logger.LogError(ex, "SQL Error: {Sql}", sql);

## Testing Approach
- No automated tests currently (manual testing only)
- Run `dotnet build` to verify compilation (must be 0 errors, 0 new warnings)
- Run app locally via `Run.bat option 4` to verify UI changes
- Test with real SQL Server data (not mocks)

## Security Rules
- No SaaS dependencies (except optional Azure OpenAI)
- All data must reside in Australia
- Use Forms authentication (no external providers)
- Role-based authorization via claims (Administrator, Director, etc.)

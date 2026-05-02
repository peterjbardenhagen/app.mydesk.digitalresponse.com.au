## Security
- **No SaaS dependencies**: All services must be deployable on air-gapped networks.
- **Data residency**: Must remain in Australia (no foreign cloud services).
- **Authentication**: Forms authentication via `/api/auth/login` (no external providers).
- **Authorisation**: Role-based (Administrator, Director, Accounts, etc.) via claims.
- **Secrets**: Azure OpenAI keys stored in `appsettings.json` (no Azure Key Vault dependency).

## Compliance
- **Audit trail**: All AI interactions logged via `AiAuditService`.
- **Data privacy**: No PII logging in ErrorLogs (sanitise before logging).
- **Invoice retention**: Follow Australian tax laws (7-year retention).

## Performance
- **SQL queries**: No `SELECT *` in production code (explicit columns only).
- **DataTable limit**: Maximum 500 rows for UI grids (use paging).
- **Image thumbnails**: Max 100px height in file library grid view.
- **Batch operations**: Use `ExecuteAsync` with Dictionary parameters, not individual calls.

## Hard Rules
- **Deployable on air-gapped networks** (no external API calls except optional Azure OpenAI).
- **No SaaS dependencies** (all libraries must be NuGet packages, no external services).
- **Data residency: Australia only** (SQL Server must be on-premise or Australian cloud).
- **Build must pass with 0 errors and 0 new warnings** (per AGENTS.md).
- **No business logic in Razor components** (all logic in service layer).

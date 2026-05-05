# Delivery Checklist

Use this before calling a task complete.

## Build
- `dotnet build` succeeds with zero warnings and zero errors.
- No new analyzer warnings are introduced.

## Data
- New tables are idempotent.
- New columns are backfilled before becoming required.
- New indexes exist for hot filters, especially `TenantId`.

## Auth
- Cookie claims are internally consistent.
- `UserId`, `Code`, and `tenant_id` are not mixed up.
- Multi-tenant users are forced through tenant selection before session creation.

## Tenant Isolation
- Tenant-scoped reads and writes are filtered or enforced.
- Global/reference tables are explicitly excluded from tenant scoping.
- File paths and exports do not leak across tenants.

## API
- GraphQL and REST endpoints require auth where appropriate.
- MCP endpoints validate caller identity and tenant context.
- Error responses do not leak secrets or internal SQL.

## UI
- Login, customer portal, and supplier portal branding remains intact.
- New dialogs and flows degrade safely when data is missing.

## Verification
- Run the app locally when the change is substantial.
- Update docs if the operator workflow changed.

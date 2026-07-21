## General Rules
- Prefer composition over inheritance
- No static helpers for domain logic (use instance methods)
- Fail fast with explicit errors (throw meaningful exceptions)
- Use `Task`/`async` consistently - never mix sync/async in same call chain

## API Rules
- Controllers do validation only (services contain business logic)
- Services return Result objects, not exceptions (for service-layer errors)
- Use `DatabaseService` for all SQL access (no direct `SqlConnection` outside services)

## Preferred Patterns
- **Dependency Injection**: Constructor injection for all services
- **Dictionary parameters**: Use `Dictionary<string, object?>` for SQL parameters
- **Null handling**: Use null-conditional (`?.`) and null-coalescing (`??`) operators
- **DateTime**: Use `DateTime.Now` for current time, `DateTime.Today` for date only

## Anti-Patterns
- No business logic in Razor components
- No direct SQL in UI layer
- No `Select *` in production queries (explicit column lists)
- Avoid `async void` - always return `Task`

## Error Handling Conventions
- Use `try-catch` in service methods, log with `ILogger`
- Let exceptions bubble up to UI layer (don't swallow silently)
- Use `Snackbar.Add()` in UI for user-facing errors

## Logging Style
- Use structured logging with message templates: `_logger.LogError(ex, "SQL Error: {Sql}", sql);`
- Log at appropriate levels: `LogInformation` for user actions, `LogError` for exceptions

## Dependency Rules
- **Shared project**: No framework dependencies (pure C#)
- **Web project**: Can reference MudBlazor, Blazor, etc.
- Services should only depend on `DatabaseService` and other services, not on UI components

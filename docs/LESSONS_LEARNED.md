# MyDesk CI/CD Lessons Learned

**Last Updated:** 2026-07-08

## Overview
This document tracks common build failures and their solutions to prevent repeating mistakes in future development iterations.

---

## Critical Build Failures

### 1. Missing Service Class Registration (CRITICAL)
**Issue:** Service registered in DI container but class doesn't exist
**Symptom:** Type resolution compilation error
**Example:** `ClientNotificationService` was registered in `Program.cs` line 472 but the class file didn't exist
**Solution:** Always create the class file before registering in DI, or remove registration if intentionally disabled
**Prevention:** 
- Verify all `builder.Services.AddScoped<T>()` registrations have corresponding `src/MyDesk.Web/Services/T.cs` files
- Run `find src/MyDesk.Web/Services -name "*.cs" | sort` and cross-reference with Program.cs registrations

### 2. Service Compilation Exclusions Mismatch
**Issue:** Service excluded from compilation but still being injected/used
**Symptom:** Type not found compilation error
**Example:** `NotificationService.cs` was in exclusion list but `NotificationController` tried to inject it
**Solution:** 
- If excluding service: comment out DI registration in Program.cs
- If including service: remove from `<Compile Remove>` list in csproj
**Prevention:** Audit `MyDesk.Web.csproj` `<Compile Remove>` section quarterly against Program.cs registrations

### 3. Razor Component Using Directives
**Issue:** Missing or incorrect `@using` directives in Razor components
**Example:** `DeliveryStatus.razor` used `NotificationLog` type but lacked `@using MyDesk.Web.Models`
**Solution:** Add appropriate using directives for all types used in component
**Prevention:** 
- Use IDE intellisense to auto-import types
- When extracting classes to new files, update all consumers' using directives
- Review Razor compile errors first - they often cascade to other errors

### 4. JavaScript Interop Incorrect API Usage
**Issue:** Calling `JS.InvokeVoidAsync` with invalid parameters
**Example:** `await JS.InvokeVoidAsync("document.getElementById", "fileInput")` is not valid
**Solution:** Create proper JavaScript helper functions in `wwwroot/js/app.js` and call them
**Pattern:**
```csharp
// In Razor component
await JS.InvokeVoidAsync("clickElement", "elementId");

// In wwwroot/js/app.js
window.clickElement = function(elementId) {
    const element = document.getElementById(elementId);
    if (element) element.click();
};
```
**Prevention:** Never call browser APIs directly through JS.InvokeVoidAsync - always wrap in JavaScript functions

### 5. Obsolete API Usage
**Issue:** Using deprecated .NET methods that no longer exist
**Example:** `HttpContent.ReadAsAsync<T>()` removed in .NET 10, replaced with `ReadFromJsonAsync<T>()`
**Solution:** Update to modern equivalents
- `ReadAsAsync<T>()` → `ReadFromJsonAsync<T>()` (add `using System.Net.Http.Json`)
- Check .NET upgrade migration guides before major version jumps
**Prevention:** Run code analysis tools that flag obsolete APIs during build

### 6. MudBlazor Component Type Parameter Issues
**Issue:** MudBlazor components require explicit type parameters that can't be inferred
**Example:** `<MudChip>` without `T="string"` type parameter
**Solution:** Always provide explicit type parameters for components: `<MudChip T="string" ... />`
**Prevention:** Reference MudBlazor documentation for component type requirements

### 7. Duplicate Using Directives
**Issue:** Duplicate `using` statements clutter code (minor but worth cleaning)
**Example:** `using MyDesk.Shared.Services;` appearing twice
**Solution:** Remove duplicate imports
**Prevention:** Use IDE formatting to remove unused/duplicate directives (`dotnet format`)

### 8. Unnecessary Using Directives
**Issue:** Import unused namespaces (code quality issue)
**Example:** `@using MyDesk.Web.Api.Controllers` in component that doesn't use it
**Solution:** Only import what's needed
**Prevention:** IDE analyzers will flag unused imports with warnings

---

## GitHub API Limitations Encountered

**Empty Check Run Output:** The GitHub API's `get_check_run` tool consistently returned empty `output.text` even for failed builds. This made diagnosis very difficult.

**Workaround:** Systematic code inspection and logical deduction based on:
- Recent changes and commit messages
- Type resolution errors (missing classes, wrong namespaces)
- Service registration mismatches
- Razor syntax errors

---

## Prevention Checklist for New Features

Before committing code with new services:

- [ ] Service class file exists: `src/MyDesk.Web/Services/ServiceName.cs`
- [ ] DI registration exists: `builder.Services.AddScoped<ServiceName>()`
- [ ] Registration matches class name exactly
- [ ] No conflicting `<Compile Remove>` entries in csproj
- [ ] No duplicate using directives
- [ ] All Razor components have proper `@using` directives
- [ ] No obsolete .NET API calls (check against .NET 10 docs)
- [ ] MudBlazor components have explicit type parameters
- [ ] JavaScript interop uses helper functions, not direct API calls
- [ ] Test locally with `dotnet build MyDesk.slnx --configuration Release`

---

## Service Architecture Best Practices

**Serviceable vs. Excluded Services:**

**Services to Include in Compilation:**
- `NotificationService` - Phase 5 core feature
- `ApprovalNotificationService` - Approval workflow notifications
- `NotificationAuditService` - Audit trail
- `NotificationRetryService` - Retry logic for failed notifications
- `NotificationBackgroundJobService` - Hangfire scheduled jobs
- `BudgetAlertService` - Budget threshold monitoring
- `ClientNotificationService` - Real-time SignalR notifications

**Services to Exclude (Intentionally Disabled):**
- `PhotoProcessingService` - Not implemented yet
- `RateLimitingService` - Not implemented yet
- `WorkflowApprovalService` - Not implemented yet

**Rule:** If a service is registered in `Program.cs`, it MUST be included in compilation.

---

## Testing Strategy for Services

1. **Unit Tests:** Test service methods in isolation with mocked dependencies
2. **Integration Tests:** Test service with actual DatabaseService
3. **DI Tests:** Verify service can be instantiated by DI container
4. **Compilation Tests:** `dotnet build` should pass with no warnings for disabled services

---

## CI/CD Best Practices

1. **Small Commits:** Keep commits focused on single features/fixes
2. **Frequent Pushes:** Push regularly to catch issues early
3. **Message Detail:** Include rationale in commit messages for future reference
4. **Pre-commit Checks:** Run `dotnet build` locally before pushing
5. **Monitor Builds:** Don't let builds fail silently - investigate immediately

---

## Future Optimization

- [ ] Create pre-commit hook to verify all DI registrations have corresponding files
- [ ] Add analyzer rule for detecting missing service classes
- [ ] Document all service dependencies and their purposes
- [ ] Create service checklist template for new Phase features
- [ ] Set up GitHub Actions to return full build logs (not just empty text)

---

## Related Documentation

- See [CLAUDE.md](./CLAUDE.md) - Development environment setup and standards
- See [agents.md](./agents.md) - Phase-specific implementation agents and responsibilities
- See [SOLUTION-ARCHITECTURE.md](./SOLUTION-ARCHITECTURE.md) - Service architecture and dependencies

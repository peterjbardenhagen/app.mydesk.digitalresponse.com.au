## Branching Strategy
- **Master only**: All commits go directly to `master` branch at this time
- No feature branches - work directly on master
- Ensure build passes (`dotnet build`) before committing

## Commit Conventions
- Use imperative mood ("Add timesheets module" not "Added timesheets module")
- Keep commits focused - one logical change per commit
- Reference issue numbers if applicable: `Fix #123: Correct null reference in service`

## File Naming Rules
- **Razor files**: PascalCase with descriptive names (e.g., `Timesheets.razor`, `CustomerLogin.razor`)
- **C# files**: PascalCase matching class name (e.g., `TimesheetService.cs`, `ErrorLogService.cs`)
- **Folders**: kebab-case for feature folders (e.g., `Components/Pages/Admin/`)

## Testing Expectations
- No automated tests currently (manual testing only)
- Run `dotnet build` to verify compilation
- Run app locally via `Run.bat option 4` to verify UI changes
- Ensure no new warnings are introduced (clean build required per AGENTS.md)

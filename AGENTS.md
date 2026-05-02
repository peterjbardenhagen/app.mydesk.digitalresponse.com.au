# Agent Guidelines for Techlight.MyDesk

To ensure the system remains stable and production-ready during development, all agents must follow these rules:

1.  **Local Development:** Always run the application locally using Kestrel (`Run.bat` option 4) after any changes to verify functionality.
2.  **Error Handling:** Every round of changes must result in a clean build. If `dotnet build` reports errors or warnings, they **must** be addressed immediately before proceeding to the next task.
3.  **Code Quality:** Do not introduce new compiler warnings (e.g., CS8600, CS0168, CS0169, CS0105).
4.  **UI Consistency:** Ensure all portals (Customer, Supplier, Login) maintain the established branding and visual design patterns.
5.  **Validation:** Before marking a task as complete, run local tests (Playwright) to ensure no regressions were introduced.

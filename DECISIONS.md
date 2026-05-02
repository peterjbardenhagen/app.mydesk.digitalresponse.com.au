## Key Tradeoffs
- **Blazor Server over WebAssembly**: Chosen for rapid development, real-time SignalR, and full .NET access. Tradeoff: Requires persistent server connection.
- **Direct SQL over ORM (EF Core)**: Chosen for performance and SQL Server control. Tradeoff: More manual mapping, less compile-time safety.
- **MudBlazor v7.15.0 (not latest v9)**: Stability choice. Tradeoff: Missing newer components, but no breaking API changes.
- **Monolith over Microservices**: Simplicity for single-tenant deployments. Tradeoff: Less scalable, but easier to deploy/maintain.

## Rejected Alternatives
- **Entity Framework Core**: Rejected due to performance overhead and less control over SQL queries.
- **React/Vue frontend**: Rejected to maintain full-stack C# and leverage Blazor's component model.
- **Multi-tenant architecture**: Rejected as each client gets their own deployment (air-gapped requirement).

## Historical Constraints
- **.NET 10 preview**: Using preview for latest features, accepting some tooling instability.
- **Windows/IIS hosting**: Client infrastructure requirement, limits cross-platform deployment.
- **Dapper + DatabaseService pattern**: Evolved from pure ADO.NET to hybrid approach for better maintainability.

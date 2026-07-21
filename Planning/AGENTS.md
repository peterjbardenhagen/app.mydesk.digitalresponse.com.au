# MyDesk Agent Assignments & Specializations

**Purpose:** Coordinate parallel development across specialized agents  
**Last Updated:** July 16, 2026  
**Next Sync:** July 30, 2026

---

## Agent Development Model

MyDesk uses an **Orchestrator-Worker** agentic pattern where:
- **Orchestrator** routes incoming work requests to appropriate agents
- **Worker Agents** specialize in specific features, modules, or technology stacks
- **Stateless Design** ensures agents can be scaled horizontally and replaced
- **Clear Hand-Offs** via JSON schemas for inter-agent communication

### Core Principles
1. **Specialization** — Each agent owns specific features or technology stacks
2. **Stateless Operations** — All context passed in request, no agent memory
3. **Structured Communication** — JSON schemas for all hand-offs
4. **Auth Middleware** — All agent calls validated via backend
5. **PII Filtering** — Sensitive data filtered before agent processing
6. **Audit Logging** — Every agent invocation logged for compliance

---

## Agent Roster

### 🎯 Agent 1: Core Platform & Expenses
**Specialization:** Expense management, approval workflows, multi-tenancy  
**Status:** ✅ Active  
**Code Locations:**
- `src/MyDesk.Web/Services/ExpenseService.cs`
- `src/MyDesk.Web/Api/Controllers/ExpenseController.cs`
- `src/MyDesk.Web/Components/Pages/Expenses.razor`

**Owned Features:**
- Phase 1: Core expense submission & receipt capture
- Phase 2: Approval workflows & escalation
- Phase 3: Multi-currency & MYOB/Xero integration
- Phase 4: Budget tracking by department

**Recent Work:**
- ✅ Completed Phase 4: Team & Department Management
- 🔄 Supporting Phase 5 notification integration
- 📋 Preparing Phase 8 AI receipt parsing

**Contact:** @expense-team  
**Blockers:** None currently

---

### 📱 Agent 2: Mobile & Cross-Platform
**Specialization:** iOS/Android development, offline-first architecture, React Native  
**Status:** 📋 Planned (Phase 7)  
**Code Locations:**
- `Mobile/` — React Native mobile app
- `Mobile/ios/` — iOS native modules
- `Mobile/android/` — Android native modules

**Owned Features:**
- Phase 7: Mobile iOS/Android apps
- Offline-first expense submission
- Mobile camera integration
- Push notifications on mobile

**Next Milestone:** Q1 2027 (Phase 7 start)  
**Dependencies:**
- Phase 5 (Notification system) — BLOCKING
- Phase 1 (Expense API) — ✅ Ready

**Blockers:**
- Awaiting Phase 5 completion for notification integration
- Mobile notification infrastructure spec needed

---

### 🔗 Agent 3: Integrations & Connectors
**Specialization:** Third-party API integration, SaaS connectors, data sync  
**Status:** ✅ Active  
**Code Locations:**
- `src/MyDesk.Web/Services/AccountingIntegration/`
- `src/MyDesk.Web/Services/Notifications/`
- `src/MyDesk.Web/Api/Controllers/OutlookAddinController.cs`

**Owned Features:**
- Phase 3: MYOB GL export, Xero sync, bank reconciliation
- Phase 4: Teams integration (departments, org structure)
- Phase 5: Email/SMS notification integrations
- Outlook Add-in (3 workflows: Change Request, Email→Contact, Legal Folio)

**Recent Work:**
- ✅ Completed Phase 3: Accounting integrations
- ✅ Completed Phase 4: Teams integration
- 🔄 Integrating Phase 5 notification services
- ✅ Completed Outlook add-in (3 workflows, production-ready)

**Blockers:**
- Twilio SMS API credentials needed (Phase 5)
- Teams API scope validation (Phase 4 completion)

---

### 📊 Agent 4: Analytics, Dashboards & Reporting
**Specialization:** Business intelligence, data visualization, performance optimization  
**Status:** 🔄 In Progress  
**Code Locations:**
- `src/MyDesk.Web/Components/Pages/Dashboards/`
- `src/MyDesk.Web/Services/DashboardService.cs`
- `src/MyDesk.Web/Api/Controllers/AnalyticsController.cs`

**Owned Features:**
- Phase 6: Executive, Manager, Employee dashboards
- Phase 6: Advanced analytics & drill-down
- Phase 6: CSV/PDF/JSON export
- Phase 6: Scheduled report delivery (Hangfire)
- Phase 9: Predictive forecasting & ML insights

**Current Sprint:**
- 🔄 MudBlazor chart integration
- 🔄 Dashboard performance optimization
- 🔄 PDF export with QuestPDF
- 🔄 Scheduled report background jobs

**Branch:** `claude/deploy-mydesk-iis-dns-6o5qn0`  
**Blockers:** None

---

### 🤖 Agent 5: AI & Machine Learning
**Specialization:** Receipt parsing, anomaly detection, forecasting models  
**Status:** 📋 Planned (Phase 8)  
**Code Locations:**
- `src/MyDesk.Web/Services/AiServices/` (future)
- `ML/receipt-parser/` (future)

**Owned Features:**
- Phase 8: AI receipt parsing & OCR
- Phase 9: Spend anomaly detection
- Phase 9: Budget forecasting models
- Phase 10: Procurement ML insights

**Next Milestone:** Q1-Q2 2027  
**Dependencies:**
- Phase 1 (Receipt capture) — ✅ Ready
- Phase 6 (Analytics infrastructure) — 🔄 In progress

**Blockers:**
- ML infrastructure & model training pipeline needed
- Data labeling for receipt training set

---

### 🔒 Agent 6: Security & Compliance
**Specialization:** Authentication, data privacy, audit trails, encryption  
**Status:** ✅ Active  
**Code Locations:**
- `src/MyDesk.Web/Services/AuthenticationService.cs`
- `src/MyDesk.Web/Middleware/ApiKeyAuthenticationHandler.cs`
- `src/MyDesk.Web/Services/ComplianceAuditLog.cs`

**Owned Features:**
- Multi-tenancy enforcement
- User authentication & authorization
- API key management & rotation
- Data encryption (PII fields)
- Audit trail logging
- GDPR compliance

**Recent Work:**
- ✅ Implemented X-Api-Key authentication
- ✅ Tenant isolation via TenantId claims
- 🔄 Supporting all phases with audit logging
- 📋 GDPR data export functionality

**Blockers:**
- Field-level encryption performance testing
- GDPR compliance audit timeline needed

---

### 📝 Agent 7: Documentation & DevOps
**Specialization:** Technical documentation, CI/CD, deployment, infrastructure  
**Status:** ✅ Active  
**Code Locations:**
- `CLAUDE.md` — Development guide
- `.github/workflows/` — CI/CD pipelines
- `docs/` — Architecture & design docs
- `Planning/` — Roadmap & coordination

**Owned Features:**
- Documentation (CLAUDE.md, architecture specs)
- CI/CD pipelines (GitHub Actions)
- Deployment automation
- Infrastructure as code
- Disaster recovery & backup procedures

**Recent Work:**
- ✅ Created comprehensive CLAUDE.md guide
- ✅ Set up GitHub Actions CI/CD
- ✅ Planning folder structure & coordination docs
- 🔄 Maintaining documentation during development

**Blockers:** None

---

## Specialization Matrix

| Agent | Core Area | Tech Stack | Phases |
|-------|-----------|-----------|--------|
| Agent 1 | Expense Management | C#, Blazor, SQL | 1-4 |
| Agent 2 | Mobile Apps | React Native, TypeScript | 7 |
| Agent 3 | Integrations | REST APIs, OAuth, GraphQL | 3-5 |
| Agent 4 | Analytics | C#, MudBlazor, QuestPDF | 6, 9 |
| Agent 5 | AI/ML | Python, TensorFlow, OpenAI | 8-9 |
| Agent 6 | Security | C#, Cryptography, OAuth | All |
| Agent 7 | DevOps | YAML, Bash, Azure | All |

---

## Work Coordination Rules

### 1. Before Starting Work
- [ ] Review DEVELOPMENT-PHASES.md for feature specs
- [ ] Check DEPENDENCIES-AND-BLOCKERS.md for conflicts
- [ ] Update RESOURCE-ALLOCATION.md with your claim
- [ ] Announce in team channel (status updates)

### 2. During Development
- [ ] Follow code standards in CLAUDE.md
- [ ] Write tests (aim for >80% coverage)
- [ ] Keep commits atomic & descriptive
- [ ] Link commits to backlog items: `git commit -m "feat(phase5): message\n\nBacklog: Phase 5 / Feature name"`

### 3. Before Merging
- [ ] Ensure CI passes (build, tests, security)
- [ ] Get code review from 1+ peer agent
- [ ] Update DEPENDENCIES-AND-BLOCKERS.md if any status changed
- [ ] Update IMPLEMENTATION-TIMELINE.md with completion date

### 4. Blockers & Escalation
**If you're blocked:**
1. Document in DEPENDENCIES-AND-BLOCKERS.md with:
   - What's blocking you
   - Who can unblock (which agent)
   - Impact (timeline, milestones affected)
2. Notify blocking agent in team channel
3. Escalate to Orchestrator if unresolved >24hrs

---

## Agent Hand-Off Protocol

When Agent A needs to hand off work to Agent B:

```csharp
// 1. Define clear JSON schema
public class NotificationHandoff
{
    public int[] ApproverIds { get; set; }
    public string EventType { get; set; }     // "approval_required"
    public Expense Expense { get; set; }       // Full context
    public DateTime CreatedAt { get; set; }
}

// 2. Validate schema before hand-off
if (handoff?.ApproverIds?.Length == 0)
    throw new ArgumentException("ApproverIds required");

// 3. Log hand-off for audit
await _auditLog.LogAsync("HandoffInitiated", agentName, handoff);

// 4. Route to Agent B
var result = await _orchestrator.RouteAsync("notification_send", context, handoff);
```

---

## Status Dashboard

| Agent | Current Phase | Progress | Blockers | Next Milestone |
|-------|---|---|---|---|
| Agent 1 | Phase 4 ✅ | 100% | None | Support Phase 5 |
| Agent 2 | Phase 7 📋 | 0% | Phase 5 blocker | Q1 2027 |
| Agent 3 | Phase 5 🔄 | 75% | SMS creds | Phase 5 complete |
| Agent 4 | Phase 6 🔄 | 70% | None | Phase 6 complete |
| Agent 5 | Phase 8 📋 | 0% | Infrastructure | Q1 2027 |
| Agent 6 | All 🔄 | 85% | Encryption testing | GDPR ready |
| Agent 7 | All ✅ | 90% | None | Q3 2026 |

---

## Communication Channels

- **Daily Standups:** Async status updates in #mydesk-dev
- **Blockers & Escalations:** @mention in #mydesk-blockers
- **Architectural Decisions:** Discussion in #mydesk-architecture
- **Code Reviews:** GitHub PR comments with line-specific feedback
- **Weekly Sync:** Friday 10 AM AEST (all agents)

---

## For New Agents Joining

1. Read the [Planning/README.md](./README.md) overview
2. Understand your specialization (this file)
3. Review DEVELOPMENT-PHASES.md for your assigned phases
4. Check DEPENDENCIES-AND-BLOCKERS.md for current issues
5. Create a feature branch and start coding!

Example:
```bash
# You're Agent 4 (Analytics), assigned Phase 6
git checkout -b feature/phase6-executive-dashboard-export
# Work on CSV/PDF export...
git push -u origin feature/phase6-executive-dashboard-export
# Create PR linking to backlog
```

---

## Questions?

- **"What should I work on?"** → Check BACKLOG.md & RESOURCE-ALLOCATION.md
- **"Who owns X feature?"** → Find it in this file
- **"Why is Y blocked?"** → See DEPENDENCIES-AND-BLOCKERS.md
- **"How do I deploy?"** → Consult CLAUDE.md Deployment section

**Next Agent Sync:** July 30, 2026, 10 AM AEST

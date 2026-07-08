# MyDesk Risk Mitigation & Contingency Planning

**Last Updated:** July 2026  
**Risk Assessment Date:** Q3 2026  
**Review Cadence:** Quarterly

---

## Risk Management Framework

### Risk Scoring Matrix

```
Impact vs Probability

         LOW      MEDIUM      HIGH
HIGH   [6]      [9]        [15]
       Yellow   Orange     Red

MED    [4]      [8]        [12]
       Green    Orange     Orange

LOW    [2]      [4]        [6]
       Green    Yellow     Yellow

Score = Probability × Impact
Threshold for action: Score ≥ 8
```

**Probability Levels:**
- P1 (Low): < 20% chance in next 12 months
- P2 (Medium): 20-50% chance
- P3 (High): > 50% chance

**Impact Levels:**
- I1 (Low): Schedule delay < 1 week, no revenue impact
- I2 (Medium): Schedule delay 1-4 weeks, 5-15% revenue impact
- I3 (High): Schedule delay > 1 month, > 15% revenue impact, reputational damage

---

## Technical Risks

### TECH-001: Database Scalability (Score: 12 - High)

**Risk Description:**
Current database architecture may not scale to 100K+ users. Shared database-per-table model has limitations:
- Single points of failure (one master database)
- Query contention across tenants (noisy neighbor)
- Backup/restore time increases with data size
- Disk I/O saturation at high volume

**Probability:** P2 (Medium) - Will face this at 50K+ users  
**Impact:** I3 (High) - Would require major architectural redesign, potential downtime

**Current Mitigation:**
- Materialized views for heavy queries (offload from main tables)
- Index optimization (reduce query time)
- Connection pooling (reduce overhead)
- Caching layer (reduce database hits)

**Early Warning Signals:**
- Database CPU > 80%
- Query execution time > 1000ms (p95)
- Connection pool saturation
- Disk I/O > 50% utilization

**Trigger Threshold:** When any signal triggers, begin migration planning

**Long-Term Mitigation (Phase 7-8):**
- Implement database sharding by TenantId
  - Shard 1: Tenants 1-1000
  - Shard 2: Tenants 1001-2000
  - etc.
- Migrate read-heavy operations to read replicas
- Implement CQRS pattern (separate read/write databases)
- Evaluate Azure SQL Database Hyperscale (auto-sharding)

**Contingency Plan:**
1. **Immediate (Week 1):** Scale up database server (add CPU/RAM)
2. **Short-term (Weeks 2-4):** Implement aggressive caching, query optimization
3. **Medium-term (Weeks 5-12):** Plan and execute sharding strategy
4. **Long-term:** Migrate to cloud-native database with auto-scaling

**Cost Impact:**
- Database upgrade: $10K
- Sharding implementation: 200 engineer-hours ($20K)
- Cloud migration: 300 engineer-hours ($30K)

**Owner:** Tech Lead (Peter)  
**Monitoring:** Weekly database metrics review (CPU, query time, connections)

---

### TECH-002: Third-Party API Dependency (Score: 10 - High)

**Risk Description:**
Application depends on external APIs (SendGrid, Twilio, OpenAI, MYOB, Xero). Outages impact core functionality:
- SendGrid outage → Email notifications fail
- Twilio outage → SMS notifications fail
- OpenAI outage → Receipt OCR fails
- MYOB/Xero outage → Expense posting fails

**Probability:** P2 (Medium) - Each vendor has ~99.9% uptime, but multiple vendors = higher risk  
**Impact:** I2 (Medium) - Features degrade but core app still works

**Current Mitigation:**
- Circuit breakers on API calls (fail fast)
- Retry logic with exponential backoff
- SMTP fallback for email (if SendGrid down)
- Vonage as SMS fallback (if Twilio down)
- Manual posting fallback (if MYOB/Xero down)

**Early Warning Signals:**
- API response time > 5s
- Error rate > 5%
- Circuit breaker trips
- Customer reports notifications not received

**Trigger Threshold:** If any API down > 1 hour, activate fallback

**Fallback Strategy:**

| API | Fallback | Recovery Time |
|-----|----------|----------------|
| SendGrid | SMTP | Immediate (manual config) |
| Twilio | Vonage | 1 hour (test integration) |
| OpenAI | Claude API | 30 min (already integrated) |
| MYOB | Manual journal entry | Next business day |
| Xero | Manual CSV import | Same day |

**Contingency Plan:**
1. **Detect:** CloudWatch alert on API error rate > 5%
2. **Notify:** PagerDuty alert to on-call engineer
3. **Failover:** Automatic circuit breaker activation (code already handles)
4. **Communicate:** Email users about degraded feature
5. **Restore:** Re-try after 15 minutes, escalate to vendor if still down
6. **Post-mortem:** Analyze root cause, improve monitoring

**Testing:**
- Monthly chaos engineering (intentionally fail APIs)
- Quarterly failover drills
- Document runbooks for each API outage scenario

**Owner:** Backend Engineering Team  
**Monitoring:** Per-API error rate, response time, circuit breaker status

---

### TECH-003: Blazor Server Memory Leak (Score: 8 - Medium)

**Risk Description:**
Blazor Server keeps UI state in memory per connected client. Memory leaks possible if:
- Components not properly disposed
- Event handlers not unsubscribed
- Large objects retained in component state
- SignalR connections not cleaned up on disconnect

**Probability:** P2 (Medium) - Known issue in early Blazor releases, mostly fixed in 8.0+  
**Impact:** I2 (Medium) - Would cause server memory exhaustion, require restart

**Current Mitigation:**
- Using .NET 10 with latest Blazor (memory management improved)
- Implementing IAsyncDisposable on heavy components
- Unsubscribing from events in OnInitializedAsync cleanup
- Monitoring memory usage per component

**Early Warning Signals:**
- Server memory > 80%
- Memory grows over time (never freed)
- SignalR connection count > allocated memory/connection

**Trigger Threshold:** If memory reaches 85%, restart application (rolling restart with load balancing)

**Long-Term Mitigation (Phase 7):**
- Migrate Blazor Server → Blazor WASM (stateless architecture)
- WASM runs in browser, offloads state to client
- Server only handles API requests (stateless)

**Code Example (Proper Cleanup):**

```csharp
public class ExpenseComponent : ComponentBase, IAsyncDisposable
{
    private IDisposable? notificationSubscription;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to notifications
        notificationSubscription = NotificationService
            .OnExpenseUpdated += HandleExpenseUpdated;

        await LoadExpensesAsync();
    }

    private void HandleExpenseUpdated(Expense expense)
    {
        // Handle event
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        // Cleanup subscriptions
        notificationSubscription?.Dispose();

        // Async cleanup if needed
        await _expenseService.CleanupAsync();
    }
}
```

**Contingency Plan:**
1. **Prevention:** Code review checklist includes disposal patterns
2. **Detection:** Weekly memory leak test (load test + memory analysis)
3. **Response:** If server memory exhausted, trigger rolling restart
4. **Recovery:** Switch to WASM architecture (longer-term)

**Cost Impact:**
- WASM migration: 400 engineer-hours ($40K)
- In the meantime: monitoring tools ($5K/year)

**Owner:** Frontend Engineer  
**Monitoring:** Daily server memory metrics, weekly memory profile analysis

---

### TECH-004: SignalR Scalability (Score: 8 - Medium)

**Risk Description:**
SignalR maintains stateful connections per client. At scale (10K+ concurrent):
- Each connection consumes ~1MB memory
- Requires Redis backplane for load balancing
- Network bandwidth can become bottleneck
- Scaling horizontally requires session affinity

**Probability:** P2 (Medium) - Will occur at ~10K concurrent users  
**Impact:** I2 (Medium) - Real-time features degrade (higher latency)

**Current Mitigation:**
- Using Redis backplane (already in architecture)
- Connection pooling
- Monitoring connection count and bandwidth

**Scaling Roadmap:**

| Concurrent Users | Solution | Timeline |
|------------------|----------|----------|
| < 1K | In-memory SignalR | Phase 5 |
| 1K-10K | Redis backplane | Phase 6 |
| 10K-100K | Azure SignalR Service | Phase 7 |
| > 100K | Azure SignalR Premium + partitioning | Phase 9 |

**Trigger Threshold:** When concurrent connections > 10K, migrate to Azure SignalR Service

**Azure SignalR Service:**
- Managed service (Microsoft handles scaling)
- Transparent to application code
- Auto-scales to 1M+ connections
- Cost: ~$100/month to $5K/month (depends on units)

**Contingency Plan:**
1. **Detect:** SignalR connection latency > 100ms or message loss > 1%
2. **Notify:** Alert when connections > 8000 (80% of threshold)
3. **Scale:** Migrate to Azure SignalR Service within 2 weeks
4. **Test:** Load test new configuration before rollout

**Owner:** Backend Engineering Team  
**Monitoring:** Connection count, message latency, Redis backplane health

---

### TECH-005: AI/OCR Model Accuracy (Score: 6 - Medium)

**Risk Description:**
Receipt OCR relies on AI models (OpenAI Vision API). Accuracy not guaranteed:
- Model hallucination (invents data not on receipt)
- Poor quality receipt images (blurry, low contrast)
- Non-English receipts may have lower accuracy
- Model updates could change behavior

**Probability:** P1 (Low) - Current API accuracy ~95%, acceptable for expense app  
**Impact:** I2 (Medium) - Incorrect expenses require manual correction

**Current Mitigation:**
- Confidence scoring on extracted data
- User can override OCR results
- Validation rules (amount < threshold requires review)
- Fallback to manual entry

**Target Accuracy:**
- Merchant name: 95%
- Amount: 99%
- Date: 98%
- Tax amount: 90%

**Early Warning Signals:**
- Extraction confidence < 70%
- User override rate > 20%
- Complaints about incorrect extraction

**Trigger Threshold:** If accuracy < 85%, rollback to simpler model or switch vendor

**Contingency Plan:**
1. **Monitor:** Track extraction accuracy daily
2. **Feedback:** Collect user corrections to identify patterns
3. **Improve:** Fine-tune model on MyDesk-specific data
4. **Fallback:** Always allow manual entry
5. **Fallback Vendor:** Switch to Claude Vision API if OpenAI fails

**Cost Impact:**
- GPT-4 Vision: ~$0.03 per receipt
- Claude Vision: ~$0.03 per receipt
- Manual review team (if accuracy degrades): $10K/month

**Owner:** Backend Engineering + ML Engineer  
**Monitoring:** Daily accuracy metrics per merchant type, user feedback

---

## Infrastructure Risks

### INFRA-001: Cloud Provider Outage (Score: 6 - Medium)

**Risk Description:**
Application deployed on Azure. Azure outage would take application offline.

**Probability:** P1 (Low) - Azure SLA 99.95%, actual uptime 99.99%+  
**Impact:** I3 (High) - Complete application unavailability, significant revenue impact

**Current Mitigation:**
- High availability (geo-redundant storage)
- Automated backups (daily)
- Disaster recovery plan (not yet implemented)

**Disaster Recovery Plan:**

| RPO (Recovery Point Objective) | RTO (Recovery Time Objective) |
|------|------|
| 1 hour | 4 hours |

**Backup Strategy:**
- Daily full database backup (to geo-redundant storage)
- Transaction log backups every 15 minutes
- Backup retention: 30 days

**Recovery Procedure:**
1. **Detect:** Azure health dashboard shows region unavailable
2. **Notify:** PagerDuty alert, page on-call team
3. **Assess:** Check if failover region ready
4. **Failover:** Activate standby in different region (4 hours)
5. **Validate:** Run smoke tests on restored infrastructure
6. **Communicate:** Email customers about restoration
7. **Post-mortem:** Document lessons learned

**Contingency Plan:**
1. **Short-term (Phase 6):** Implement database replication to secondary region
2. **Medium-term (Phase 7):** Multi-region deployment (active-active)
3. **Long-term (Phase 9):** Multi-cloud strategy (Azure + AWS)

**Cost Impact:**
- Geo-redundant storage: +$500/month
- Secondary region compute: +$1000/month
- Testing + runbooks: 80 engineer-hours

**Owner:** DevOps Team  
**Monitoring:** Azure service health dashboard, recovery drill quarterly

---

### INFRA-002: Data Loss (Score: 12 - High)

**Risk Description:**
Accidental deletion or corruption of production data. Could be triggered by:
- Bug in code (batch delete all records)
- SQL injection attack
- Storage account deletion
- Backup failure

**Probability:** P1 (Low) - But consequences are severe  
**Impact:** I3 (High) - Loss of customer data, regulatory fines, business closure

**Current Mitigation:**
- Automated daily backups
- Read-only replica database
- Backup retention: 30 days
- Soft deletes (logical delete, not hard delete)
- Database auditing (log all changes)

**Enhanced Mitigation (Phase 6):**

```sql
-- Soft delete pattern (not true delete)
ALTER TABLE dbo.Expenses ADD DeletedAt DATETIME2 NULL;

-- When deleting:
UPDATE dbo.Expenses SET DeletedAt = GETUTCDATE() 
WHERE ExpenseId = @Id;

-- When querying:
SELECT * FROM dbo.Expenses 
WHERE TenantId = @TenantId AND DeletedAt IS NULL;

-- Create audit trail
CREATE TABLE dbo.AuditLog (
    AuditId INT PRIMARY KEY IDENTITY,
    TableName NVARCHAR(100),
    EntityId INT,
    Operation VARCHAR(50),  -- 'INSERT', 'UPDATE', 'DELETE'
    ChangeData NVARCHAR(MAX),  -- JSON of changes
    ChangedBy INT,
    ChangedAt DATETIME2,
    TenantId INT
);
```

**Backup & Recovery:**

```bash
# Full backup daily at 2 AM
BACKUP DATABASE MyDesk 
TO DISK = '\\backups\MyDesk_FULL_20260708.bak' 
WITH COMPRESSION;

# Transaction log backup every 15 minutes
BACKUP LOG MyDesk 
TO DISK = '\\backups\MyDesk_LOG_20260708_0200.trn' 
WITH COMPRESSION;

# Test restoration quarterly
RESTORE DATABASE MyDesk_Test 
FROM DISK = '\\backups\MyDesk_FULL_20260708.bak' 
WITH REPLACE, RECOVERY;
```

**Recovery Procedure:**
1. **Detect:** Alert on unexpected data disappearance
2. **Assess:** Determine scope (which tables, which date range)
3. **Stop:** Kill application connections to prevent further damage
4. **Restore:** Restore from backup to point-in-time before incident
5. **Validate:** Verify data integrity
6. **Resume:** Resume application operations
7. **Communicate:** Notify affected customers

**Contingency Plan:**
- **Disaster Recovery Team:** Designated on-call (24/7 rotation)
- **Runbook:** Step-by-step recovery procedure (tested quarterly)
- **Insurance:** Consider cyber liability insurance ($5K/year)

**Cost Impact:**
- Backup infrastructure: $2K/month
- Disaster recovery drills: 40 engineer-hours/quarter
- Additional tools (Point-in-Time Recovery): $5K/year

**Owner:** DevOps + Database Administrator  
**Monitoring:** Backup completion status daily, restore drills quarterly

---

## Security Risks

### SEC-001: Data Breach (Score: 12 - High)

**Risk Description:**
Unauthorized access to customer data (expenses, personal info, payment details). Could be via:
- SQL injection attack
- Insecure API endpoint
- Compromised employee credentials
- Malware on employee device
- Vulnerable third-party library

**Probability:** P1 (Low) - Industry average: 1 breach per 5000 companies  
**Impact:** I3 (High) - Regulatory fines (GDPR up to €20M), reputational damage, lawsuits

**Current Mitigation:**
- HTTPS (TLS 1.3) for all communications
- Parameterized SQL queries (prevent injection)
- Authentication required on all API endpoints
- Row-level security (TenantId filtering)
- Password complexity requirements
- IP-based access controls

**Enhanced Mitigation (Phase 6):**

```csharp
// Input validation - prevent injection
public async Task<Expense> CreateExpenseAsync(CreateExpenseRequest request)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(request.Description))
        throw new ValidationException("Description required");
    
    if (request.Amount <= 0 || request.Amount > 100000)
        throw new ValidationException("Invalid amount");
    
    // Use parameterized query
    var expense = await _db.CreateAsync<Expense>(
        sql: "INSERT INTO Expenses (TenantId, UserId, Amount, Description) VALUES (@TenantId, @UserId, @Amount, @Description)",
        parameters: new() 
        { 
            ["TenantId"] = _context.TenantId,
            ["UserId"] = _context.UserId,
            ["Amount"] = request.Amount,
            ["Description"] = request.Description
        });
    
    return expense;
}

// API endpoint authentication
[ApiController]
[Route("api/[controller]")]
[Authorize]  // ← Required
public class ExpenseController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        var tenantId = User.GetTenantId();  // From JWT token
        var userId = User.GetUserId();
        
        // Verify tenant isolation
        if (!await AuthorizeAsync("CanCreateExpense", new { tenantId, userId }))
            return Forbid();
        
        var expense = await _expenseService.CreateAsync(tenantId, userId, request);
        return CreatedAtAction(nameof(GetExpense), new { id = expense.ExpenseId }, expense);
    }
}
```

**Security Checklist (Before Every Release):**

- [ ] All API endpoints require [Authorize]
- [ ] All database queries use parameters (@param, not string concat)
- [ ] No hardcoded secrets (use Azure Key Vault)
- [ ] No console.log() with sensitive data in JavaScript
- [ ] TenantId validated on every API call
- [ ] Input validation on all user inputs
- [ ] Error messages don't leak implementation details
- [ ] CORS restricted to known domains
- [ ] Dependency vulnerabilities scanned (dotnet list package --vulnerable)
- [ ] Code reviewed by security-focused reviewer

**Incident Response Plan:**

```
BREACH DETECTED
├─ Step 1: Isolate (Disable account if compromised)
├─ Step 2: Investigate (Determine scope, start date, data affected)
├─ Step 3: Notify (Legal, leadership, customers within 72 hours per GDPR)
├─ Step 4: Remediate (Patch vulnerability, force password reset)
├─ Step 5: Monitor (Check for unauthorized access)
└─ Step 6: Post-mortem (Document lessons, improve controls)
```

**Penetration Testing:**
- Annual third-party pen test ($10K)
- Bug bounty program ($5K/month budget)
- Internal red team exercises (quarterly)

**Owner:** Security Team + Tech Lead  
**Monitoring:** WAF rules, intrusion detection, anomalous login attempts

---

### SEC-002: Compliance Failure (Score: 8 - Medium)

**Risk Description:**
Failure to meet compliance requirements (GDPR, SOC 2, ISO 27001). Could result in:
- Regulatory fines
- Contract cancellations (enterprise customers require certifications)
- Reputational damage

**Probability:** P1 (Low) - Not on critical path until enterprise phase  
**Impact:** I2 (Medium) - Lost enterprise deals, regulatory fines

**Current Compliance Status:**

| Standard | Target | Timeline | Status |
|----------|--------|----------|--------|
| GDPR | Full compliance | Q4 2026 | 70% |
| SOC 2 Type II | Audit passed | Q1 2027 | Not started |
| ISO 27001 | Certification | Q2 2027 | Not started |

**GDPR Compliance Roadmap:**
- [x] Privacy policy (done)
- [x] Data processing agreement (done)
- [ ] Right to be forgotten (delete user data)
- [ ] Data portability (export data in standard format)
- [ ] Breach notification procedures
- [ ] Data retention policies

**Cost Impact:**
- GDPR: 100 engineer-hours ($10K)
- SOC 2 audit: $25K
- ISO 27001: 200 engineer-hours ($20K)
- Ongoing compliance: $5K/month (external auditor)

**Owner:** Legal + Security Team  
**Monitoring:** Quarterly compliance assessment

---

## Operational Risks

### OPS-001: Team Member Departure (Score: 6 - Medium)

**Risk Description:**
Loss of key personnel (especially Tech Lead) would impact:
- Architectural decisions slowed
- Knowledge gaps (critical systems known only by one person)
- Project timeline delays
- Institutional memory lost

**Probability:** P1 (Low) - Startup team usually committed, but market competitive  
**Impact:** I2 (Medium) - 1-3 month delay on critical features

**Current Mitigation:**
- Competitive compensation
- Transparent communication about company direction
- Professional development opportunities
- Flexible work environment

**Knowledge Transfer Strategy:**

```
For each critical system:
├─ Architecture documentation (ARCHITECTURE-DECISIONS.md)
├─ Code comments (why decisions made)
├─ Automated tests (document expected behavior)
├─ Runbooks (how to operate/debug)
└─ Knowledge-sharing sessions (monthly tech talks)
```

**Contingency Plan:**
1. **Cross-training:** Pair critical knowledge holders with juniors
2. **Documentation:** Maintain up-to-date runbooks
3. **Succession Plan:** Identify backup for each critical role
4. **Exit Interview:** Conduct thorough interview on departure
5. **Knowledge Preservation:** Record system walkthroughs, store in wiki

**Cost Impact:**
- Documentation overhead: 20% of engineering time
- Knowledge-sharing sessions: 2 hours/month
- Onboarding new hire: $10K (recruiting + training)

**Owner:** Tech Lead + HR  
**Monitoring:** Turnover rate, knowledge distribution assessment (quarterly)

---

### OPS-002: Feature Scope Creep (Score: 8 - Medium)

**Risk Description:**
Unplanned feature requests delay critical path items. Could result in:
- Phase delays (roadmap misses)
- Quality issues (rushed development)
- Team burnout (overwork)
- Revenue targets missed

**Probability:** P3 (High) - Common in startups with customer feedback loop  
**Impact:** I2 (Medium) - 1-2 month delay, affects quarterly revenue goals

**Current Mitigation:**
- Published roadmap (ROADMAP.md) sets customer expectations
- Clear phase prioritization
- Regular stakeholder meetings

**Scope Management Framework:**

```
Feature Request Received
├─ Assess impact (size, dependencies)
├─ Prioritize (critical vs nice-to-have)
├─ Decide
│  ├─ In Phase: Add to current phase backlog
│  ├─ Future Phase: Schedule for phase (e.g., Phase 7)
│  └─ Out of Scope: Decline politely
└─ Communicate: Update roadmap, notify stakeholder
```

**Feature Evaluation Criteria:**
- Does it align with current phase goal?
- How many engineering hours required?
- Is it on the critical path (affects other features)?
- Can it be deferred to next phase?
- What's the business impact (revenue, churn risk)?

**Contingency Plan:**
- **Monthly:** Review feature requests, update roadmap
- **Quarterly:** Assess phase progress, adjust timeline if needed
- **If behind:** Cut low-priority features, extend phase timeline
- **If ahead:** Add stretch features from future phases

**Owner:** Product Manager  
**Monitoring:** Feature backlog size, phase progress vs plan (weekly)

---

## Business Risks

### BIZ-001: Market Competition (Score: 8 - Medium)

**Risk Description:**
Existing players (Concur, Expensify, Emburse) have more resources and market reach. Risk:
- Customers choose competitor (similar features, better brand)
- Price competition erodes margins
- Features copied by competitors

**Probability:** P3 (High) - Competitive market for expense management  
**Impact:** I2 (Medium) - Lower adoption, pricing pressure, 10-30% revenue impact

**Current Competitive Position:**
- **Strengths:** Modern architecture, easy to use, local support
- **Weaknesses:** Smaller brand, limited integrations, no mobile yet
- **Opportunities:** AI-powered features, vertical focus (specific industries)
- **Threats:** Larger competitors, price war, feature parity

**Differentiation Strategy:**
1. **AI-First:** Better expense categorization than competitors (Phase 8)
2. **Vertical Focus:** Tailor to specific industries (travel, consulting, etc.)
3. **Local Support:** Personalized onboarding, responsive support
4. **Integrations:** Deep MYOB/Xero integration (Australian focus)
5. **Pricing:** Value-based pricing for SMBs ($50-200/month per user)

**Mitigation Actions:**
- [ ] Complete Phase 7 mobile before Concur
- [ ] Launch Phase 8 AI features (competitive advantage)
- [ ] Build customer success team (retention)
- [ ] Gather testimonials and case studies
- [ ] Industry partnerships (accounting firms, consulting)

**Cost Impact:**
- Product development: Already budgeted (roadmap)
- Marketing: +$20K/quarter (content, events)
- Sales: +$50K/quarter (sales team)

**Owner:** Product Manager + Sales Team  
**Monitoring:** Market share (quarterly), competitive feature comparison

---

### BIZ-002: Regulatory Changes (Score: 4 - Low)

**Risk Description:**
New regulations could require features or compliance work:
- Tax law changes (GST, income tax tracking)
- Labor law changes (expense policy requirements)
- Data protection (new privacy regulations)

**Probability:** P1 (Low) - Regulations change slowly  
**Impact:** I2 (Medium) - Compliance work, possible feature changes

**Current Mitigation:**
- Legal review quarterly
- Compliance roadmap (ROADMAP.md documents Phase 11)
- Vendor relationships with accountants/lawyers

**Contingency Plan:**
- Maintain 2-week buffer for regulatory changes
- Monitor regulatory bodies (ATO, Fair Work Commission)
- Partner with legal firm for guidance

**Owner:** Legal Team + Product Manager  
**Monitoring:** Regulatory news, legal update meetings (quarterly)

---

### BIZ-003: Customer Acquisition Cost (Score: 6 - Medium)

**Risk Description:**
CAC too high to achieve profitability targets. If CAC = $200 but LTV = $300, margins thin.

**Probability:** P2 (Medium) - Depends on sales/marketing effectiveness  
**Impact:** I2 (Medium) - Business model viability at risk

**Targets:**
- CAC: < $100/customer
- LTV: > $500/customer
- Payback period: < 6 months

**Strategies to Reduce CAC:**
1. **Free trial:** 30-day free trial to reduce barrier to entry
2. **Self-serve:** Optimize onboarding so customers don't need sales calls
3. **Referrals:** Offer $50 credit for referrals (viral loop)
4. **Content:** Invest in SEO/content to reduce paid ads
5. **Partnerships:** Work with accountants, HR firms for distribution

**Contingency Plan:**
- If CAC > $150: Increase marketing spend on SEO/content (reduce paid ads)
- If LTV < $400: Focus on retention (improve NPS, reduce churn)
- If payback > 9 months: Adjust pricing or focus on higher-value customers

**Owner:** Sales + Marketing Team  
**Monitoring:** CAC per channel (monthly), LTV (quarterly), payback period

---

## Risk Monitoring & Escalation

### Risk Review Cadence

**Weekly (Monday 9 AM):**
- Tech Lead reviews technical risks
- Check early warning signals
- Escalate if threshold crossed

**Monthly (First Friday):**
- Full team risk review
- Update risk scores
- Discuss mitigation actions

**Quarterly (End of quarter):**
- Board-level risk review
- Strategic risk assessment
- Update risk mitigation plan

### Escalation Path

```
Early Warning Signal Detected
│
├─ Severity P1 (Critical) → Page on-call immediately
├─ Severity P2 (High) → Alert within 1 hour
└─ Severity P3 (Medium) → Alert in next standup

Action Required
├─ Tech Lead authorizes action
├─ Assign owner
├─ Implement mitigation
├─ Monitor progress
└─ Post-mortem (if incident occurred)
```

### Risk Register

All risks tracked in central register (this document + spreadsheet):

| Risk ID | Description | Prob | Impact | Score | Status | Owner | Mitigation |
|---------|-------------|------|--------|-------|--------|-------|-----------|
| TECH-001 | Database scalability | P2 | I3 | 12 | ACTIVE | Tech Lead | Caching + indexing |
| TECH-002 | Third-party API failure | P2 | I2 | 10 | ACTIVE | Backend | Circuit breakers |
| TECH-003 | Memory leak (Blazor) | P2 | I2 | 8 | MONITORING | Frontend | Monitor + WASM migration |
| SEC-001 | Data breach | P1 | I3 | 12 | ACTIVE | Security | Penetration testing |
| BIZ-001 | Market competition | P3 | I2 | 8 | ACTIVE | PM | Differentiation |

---

## Insurance & Financial Protection

### Recommended Insurance Coverage

1. **Cyber Liability Insurance**
   - Covers breach costs (legal, notification, credit monitoring)
   - Estimated premium: $5K-$20K/year
   - Coverage: $1M-$5M

2. **Professional Liability Insurance**
   - Covers errors & omissions in software
   - Estimated premium: $3K-$10K/year
   - Coverage: $1M-$2M

3. **Directors & Officers Insurance**
   - Covers executive liability
   - Estimated premium: $5K-$15K/year
   - Coverage: $5M-$10M

**Total Insurance Cost:** $15K-$45K/year

---

## Lessons Learned Template

When any risk materializes (incident occurs):

```
INCIDENT REPORT
Date: [Date]
Title: [Brief description]
Duration: [Start time - End time]
Impact: [What went wrong, how many users affected]

ROOT CAUSE:
[Why did it happen]

IMMEDIATE ACTIONS:
[What was done to stop the bleeding]

LONG-TERM MITIGATION:
[What changes prevent recurrence]

FOLLOW-UP TASKS:
- [ ] Task 1 (owner, due date)
- [ ] Task 2 (owner, due date)
```

---

*For questions on risk management, contact the Tech Lead.*

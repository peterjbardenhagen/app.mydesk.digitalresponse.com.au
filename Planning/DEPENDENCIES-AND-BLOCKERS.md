# MyDesk Dependencies & Blockers

**Last Updated:** July 2026  
**Critical Path Analysis:** Phases 5-6 complete Phase 7-10 roadmap  
**Blocker Review Cadence:** Weekly standup + monthly deep dive

---

## Dependency Graph

```
External Dependencies (Vendors, Third-parties)
├─ SendGrid (Email) ─→ Phase 5 Notifications
├─ Twilio (SMS) ─────→ Phase 5 Notifications
├─ Vonage (Backup SMS) → Phase 5 fallback
├─ OpenAI (OCR) ─────→ Phase 1, Phase 8
├─ MYOB API ────────→ Phase 3 Integration
├─ Xero API ────────→ Phase 3 Integration
├─ Azure Cloud ─────→ All phases
├─ SQL Server ──────→ All phases
└─ GitHub/CI/CD ────→ All phases (DevOps)

Internal Dependencies (Team, Skills, Knowledge)
├─ Backend Engineers ────→ All phases
├─ Frontend Engineer ────→ Phases 1-6, 7 (mobile API support)
├─ Mobile Engineer ──────→ Phase 7 (NEW HIRE Q1 2027)
├─ ML/AI Engineer ──────→ Phase 8 (NEW HIRE Q4 2026)
├─ Data Engineer ───────→ Phase 6 (NEW HIRE Q4 2026)
├─ Tech Lead (Peter) ────→ Architecture decisions (all phases)
└─ Product Manager ─────→ All phases

Organizational Dependencies
├─ Budget approval ──────→ Hiring, tools, cloud services
├─ Customer feedback ────→ Feature prioritization
├─ Security review ──────→ Before production releases
└─ Legal review ────────→ Compliance, terms & conditions
```

---

## Critical Path Dependencies

### Phase 5: Notifications & Alerts

**Blocking:** Phase 6 (dashboards depend on notifications working)

**Dependencies:**

| Dependency | Status | Risk | Mitigation |
|------------|--------|------|-----------|
| SendGrid account + API key | ✅ Acquired | Low | SMTP fallback available |
| Twilio account + credentials | ✅ Acquired | Low | Vonage fallback ready |
| Backend infrastructure (Hangfire) | ✅ Implemented | Low | - |
| Email templates designed | 🔄 60% | Medium | Use simple HTML for MVP |
| SMS templates designed | ⏸️ 0% | Medium | Text-only, no fancy formatting |
| Database schema (NotificationLog, etc.) | ✅ Designed | Low | Schema review complete |
| SignalR integration | ✅ Done | Low | Used in approval system |
| Backend Engineer availability | ✅ Full-time | Low | Primary owner allocated |
| Performance targets (99%+ delivery) | 🔄 In progress | Medium | Load testing scheduled |

**Blockers:**
- None currently blocking. Phase 5 on track for completion.

**Risk Items:**
- SendGrid rate limits: 100K emails/day (sufficient for 10K users)
- Twilio pricing: $0.0075 per SMS, budget ~$500/month for testing
- Hangfire scalability: Works well up to 10M jobs/year

**Contingency:**
- If SendGrid rates exceeded: Upgrade to dedicated IP ($10/month)
- If Twilio down: Auto-fallback to Vonage (no code changes needed)
- If Hangfire job queue grows: Switch to Azure Service Bus (Phase 7)

---

### Phase 6: Dashboard & Analytics

**Blocking:** Phase 7 (mobile app needs dashboard API endpoints)

**Dependencies:**

| Dependency | Status | Risk | Mitigation |
|------------|--------|------|-----------|
| Phase 5 complete | 🔄 80% | High | On track for Nov 2026 |
| Database materialized views | ⏸️ 50% | Medium | Scheduled for Oct 2026 |
| Caching layer (Redis) | ✅ Deployed | Low | Production-ready |
| MudBlazor charts | ✅ Available | Low | Component library ready |
| QuestPDF (PDF export) | ⏸️ 0% | Medium | Evaluate in Sept 2026 |
| Data model for analytics | ✅ Designed | Low | Schema reviewed |
| Frontend Engineer availability | ✅ 100% | Low | Dedicated resource |
| Performance targets (< 1s load) | 🔄 Testing | Medium | Optimization in progress |

**Blockers:**
- **BLOCKED:** PDF export library (QuestPDF vs iText comparison needed by Sept 30)
- **BLOCKED:** Data model finalization (pending approval from CFO persona interview)

**Risk Items:**
- Chart rendering performance at scale: MudBlazor may struggle with 1000+ data points
- Caching invalidation complexity: Could introduce stale data issues
- Database view materialization: Requires migration planning

**Contingency:**
- If MudBlazor performance poor: Switch to Syncfusion charts or Chart.js
- If caching issues: Implement event-sourced cache invalidation
- If data model changes: Add schema migration buffer (1 week)

**Unblock Actions:**
1. **PDF library decision:** Complete evaluation by Sept 15
   - Owner: Frontend Engineer
   - Options: QuestPDF (cheaper), iText (more features)
   
2. **Data model approval:** Get CFO sign-off by Sept 20
   - Owner: Product Manager
   - Method: Schedule user interview with finance director

---

### Phase 7: Mobile Applications

**Blocking:** Phase 8 (if AI features needed on mobile)

**Dependencies:**

| Dependency | Status | Risk | Mitigation |
|-----------|--------|------|-----------|
| Technical decision (React Native vs Flutter) | 🔄 Planning | High | Decision due by Dec 1 |
| Mobile Engineer hire | ⏸️ Recruiting | High | Candidate search in progress |
| Blazor API endpoints stabilized | ✅ Stable | Low | API contracts defined |
| Mobile CI/CD pipeline | ⏸️ Not started | Medium | Design scheduled for Jan 2027 |
| Device farm setup (iOS/Android) | ⏸️ Not started | Medium | Needed for testing |
| App store preparation (Apple/Google) | ⏸️ Not started | Medium | Requires app store accounts |
| Offline sync architecture | ⏸️ Design phase | Medium | Complex feature |

**Blockers:**
- **CRITICAL:** Mobile Engineer hire (expected start: Jan 15, 2027)
  - Risk: If hire delayed > 1 month, Phase 7 timeline extends
  - Mitigation: Start recruiting in Oct 2026 (12-week hiring cycle)
  
- **HIGH:** Framework decision (React Native vs Flutter)
  - Risk: Choice impacts team hiring, development speed
  - Mitigation: Complete POC with both frameworks by Dec 1

**Risk Items:**
- Mobile engineer availability: Competitive market, $55K-$65K salary required
- Framework learning curve: Team must learn new technology
- Platform-specific bugs: iOS and Android have different behaviors

**Contingency:**
- If Flutter engineer unavailable: Hire React Native engineer (larger talent pool)
- If framework selection delayed: Start with both and consolidate later
- If onboarding slow: Plan 6-week ramp-up time

**Unblock Actions:**
1. **Framework decision:** Complete POC evaluation by Nov 15
   - Owner: Tech Lead + Backend Engineer
   - Decision criteria: time-to-market, code sharing, performance
   
2. **Mobile Engineer hire:** Begin recruiting immediately
   - Owner: HR + Tech Lead
   - Target: 5 candidates by Nov 1, hire by Dec 15
   
3. **CI/CD pipeline design:** Finalize in Dec 2026
   - Owner: DevOps Team
   - Must support iOS + Android builds

---

### Phase 8: AI-Powered Receipt Processing

**Blocking:** Phase 9 (predictive analytics depends on good data quality from Phase 8)

**Dependencies:**

| Dependency | Status | Risk | Mitigation |
|-----------|--------|------|-----------|
| Model evaluation (GPT-4, Claude, Gemini) | ⏸️ Planning | High | POC needed by Dec 2026 |
| ML Engineer hire | ⏸️ Recruiting | High | Candidate search in progress |
| Training data (1000+ labeled receipts) | ⏸️ Data collection | High | User feedback needed |
| API costs (OpenAI vs alternatives) | ⏸️ Analysis | Medium | Budget planning required |
| Confidence scoring model | ⏸️ Not started | Medium | Complex ML feature |
| Integration with existing OCR | ✅ Designed | Low | POC complete |

**Blockers:**
- **CRITICAL:** ML Engineer hire (expected start: Dec 1, 2026)
  - Risk: If delayed, Phase 8 starts late, delays Phase 9
  - Mitigation: Start recruiting in Sept 2026
  
- **HIGH:** Model evaluation (decision needed by Dec 15)
  - Risk: Wrong choice means rework, wasted ML engineer time
  - Mitigation: Complete POC with 3 models using 100 test receipts

**Risk Items:**
- Model accuracy insufficient (< 85% on receipts)
- Training data hard to get (need manual labeling)
- API costs high (OpenAI charges per vision call)
- Model hallucination (invents data)

**Contingency:**
- If accuracy < 85%: Consider hybrid approach (ML + heuristics)
- If training data scarce: Use transfer learning from pre-trained models
- If costs too high: Implement local processing with open-source models

**Unblock Actions:**
1. **Model evaluation POC:** Complete by Dec 1
   - Owner: Tech Lead + Backend Engineer (temp)
   - Test: 100 real MyDesk receipts with each model
   - Metric: Accuracy, cost per receipt, confidence score
   
2. **ML Engineer hire:** Begin recruiting in Sept
   - Owner: HR + Tech Lead
   - Role: Python/ML frameworks, computer vision experience

---

## External Vendor Dependencies

### Email Service (SendGrid)

**Contract Status:** Active, pay-as-you-go  
**Cost:** ~$100/month for 1M emails  
**Uptime SLA:** 99.9%

**Risks:**
- Rate limiting (100K/day in standard, unlimited in higher tiers)
- Account suspension (if deliverability issues)
- Price changes

**Mitigation:**
- Monitor reputation score weekly
- Implement bounce handling
- SMTP fallback for critical emails
- Upgrade to higher tier if approaching limits

**Exit Strategy:**
- SMTP fallback implemented (zero switchover cost)
- Alternative: Mailgun ($20/month) or AWS SES ($0.10 per 1K)

---

### SMS Service (Twilio)

**Contract Status:** Active, pay-as-you-go  
**Cost:** $0.0075 per SMS (~$500/month for 66K SMS)  
**Uptime SLA:** 99.95%

**Risks:**
- Rate limiting (default 1 per second)
- Number spoofing detection (some carriers block)
- International restrictions (some countries blocked)

**Mitigation:**
- Vonage backup configured
- Monitor delivery rate (target: > 98%)
- Use short codes for better deliverability (Phase 8)

**Exit Strategy:**
- Vonage fallback available (compatible API)
- AWS SNS alternative ($0.0645 per SMS)

---

### Cloud Provider (Microsoft Azure)

**Contract Status:** Enterprise agreement (discounted rates)  
**Cost:** $50K-$100K/year (compute + storage + bandwidth)  
**Uptime SLA:** 99.95% (standard), 99.99% (premium)

**Risks:**
- Vendor lock-in (moving to AWS would require re-architecture)
- Price increases (Microsoft raises rates ~5% annually)
- Regional outage (could take app offline if no failover)

**Mitigation:**
- Cloud-agnostic architecture (minimize Azure-specific features)
- Multi-region backup (by Phase 7)
- Reserved instances for cost predictability

**Exit Strategy:**
- Containerized with Docker (could move to AWS)
- Database not Azure-dependent (SQL Server on-prem or multi-cloud)
- Plan Phase 9: Multi-cloud strategy (Azure + AWS)

---

### Accounting Software (MYOB, Xero)

**MYOB Status:** Integration active, API credentials from customer  
**Xero Status:** Integration active, API credentials from customer  

**Risks:**
- API changes (breaking changes could occur)
- Rate limiting (throttle excessive requests)
- Regional restrictions (Xero APIs differ by region)

**Mitigation:**
- Monitor API status pages weekly
- Implement circuit breakers + retry logic
- Fallback to manual posting (queue for review)

**Exit Strategy:**
- Could switch to QuickBooks Online (similar API)
- Core functionality doesn't depend on integrations (they're optional)

---

## Internal Team Dependencies

### Tech Lead (Peter Bardenhagen)

**Allocation:** 100% on MyDesk  
**Critical Functions:**
- Architecture decisions
- Code review (especially critical paths)
- Hiring decisions
- Strategic planning

**Risk:** Single point of failure (no backup)

**Mitigation:**
- Document architecture decisions (ARCHITECTURE-DECISIONS.md)
- Cross-train Backend Engineer 1 on critical systems
- Monthly knowledge-sharing sessions
- Maintain runbooks for critical operations

**Contingency:**
- If unavailable > 1 week: Designate Backend Engineer 1 as acting tech lead
- If leave planned: Hire contract architect for overlap period

---

### Backend Engineers (2 FTE)

**Current Allocation:**
- Engineer 1: Phase 5 (Notifications) - 100%
- Engineer 2: Phase 6 (Dashboards) - 100%

**Capacity Constraints:**
- No excess capacity for Phase 7 mobile API work
- Both fully allocated through Q1 2027

**New Hires Needed:**
- Phase 7: Mobile API support (Jan 2027)
- Phase 8: ML integration (Dec 2026 - part-time)
- Phase 10: Procurement module (Apr 2027)

**Contingency:**
- If hire delayed: Push non-critical features to later phases
- If team member departs: Redistribute work, extend timeline

---

### Frontend Engineer

**Current Allocation:**
- Blazor component development: 80%
- Tech debt / bug fixes: 20%

**Blocked By:**
- Backend APIs must be stable before UI development
- Design decisions must be finalized before implementation

**Risks:**
- Single FTE for all Blazor work (Phase 1-6)
- Mobile phase needs API support from this engineer
- High cognitive load across multiple components

**Contingency:**
- If unavailable: Hire contract Blazor developer ($100/hour)
- For Phase 7: Hire second frontend engineer (Jan 2027) or mobile engineer with Blazor skills

---

### QA Engineer (0.5 FTE transitioning to 1.0 FTE)

**Current Allocation:**
- Integration testing: 40%
- Manual UAT: 30%
- Bug reporting: 30%

**Gaps:**
- Load testing not scheduled (critical for Phase 6)
- Security testing manual only (no automated scanning)
- Mobile testing expertise needed (Phase 7)

**Needed:**
- Upgrade to 1.0 FTE (Q4 2026)
- Mobile QA specialist (Q1 2027)
- Automated testing framework (ongoing)

---

## Organizational Dependencies

### Budget Approval

**Current Budget:** $450K/year (through Q4 2026)  
**Planned Budget Q4 2026+:** $128K/quarter increasing to $185K/quarter (Phase 7-9)  

**Approval Process:**
- Quarterly budget review with board
- Hiring approvals: $10K/engineer salary increase requires VP approval
- Tool/vendor purchases > $1K require approval

**Risk:**
- Budget cuts could delay hiring (impacts timeline)
- If company fundraising fails, budget constrained

**Contingency:**
- Identify low-priority features to cut if budget reduced 20%
- Prioritize critical path (Phase 5-6 complete before hiring for Phase 7)

---

### Customer Input & Feedback

**Dependencies on Customer Feedback:**
- Phase 6: Dashboard design needs CFO/Manager personas validation
- Phase 7: Mobile priorities (which features most used?)
- Phase 8: AI accuracy targets (what accuracy is acceptable?)

**Process:**
- Monthly customer advisory board (3-5 enterprise customers)
- Quarterly NPS survey (all customers)
- Weekly support ticket review

**Risk:**
- Conflicting feedback from different customers
- Feature requests misaligned with roadmap

**Mitigation:**
- Product Manager evaluates feedback against phase goals
- Advisory board provides strategic guidance
- Clear communication of roadmap (manage expectations)

---

### Security & Compliance Reviews

**Pre-Release Requirements:**
- Code review by security-focused reviewer
- Automated dependency scanning (dotnet list package --vulnerable)
- CORS/authentication validation
- SQL injection testing

**Scheduled Reviews:**
- Monthly: Internal security checklist
- Quarterly: External penetration testing (Phase 6+)
- Annually: Full SOC 2 assessment (Phase 7+)

**Risk:**
- Security issues discovered late (impacts release timeline)
- Compliance gaps required for enterprise deals

**Mitigation:**
- Shift-left security (build secure from start)
- Security checklist before every release
- External auditor engaged quarterly (Phase 6+)

---

## Timeline Impact Analysis

### If Phase 5 Delayed

**Delay Duration:** 2 weeks  
**Impact on Dependent Phases:**
- Phase 6: Delayed 2 weeks (notifications required for approval notifications)
- Phase 7: Delayed 2 weeks (mobile needs notifications)
- Overall impact: 2-week delay to mobile launch

**Recovery Options:**
- Parallelize Phase 5 & 6: Dashboard features don't all depend on notifications
- De-scope Phase 5: Release without SMS first, add later
- Accelerate Phase 6: Use additional resources

---

### If Phase 6 Delayed

**Delay Duration:** 4 weeks  
**Impact on Dependent Phases:**
- Phase 7: Delayed 4 weeks (mobile API endpoints from Phase 6)
- Phase 9: Delayed 4 weeks (predictive analytics needs dashboard metrics)

**Recovery Options:**
- Parallelize: Start Phase 7 mobile development with beta dashboard APIs
- Front-load simple dashboards: Get basic metrics working early

---

### If Mobile Engineer Hire Delayed

**Delay Duration:** 8 weeks (2 months)  
**Impact:**
- Phase 7 starts in March instead of January
- Mobile launch pushed from Q2 to Q3 2027
- Misses market window (competitors releasing mobile)

**Recovery Options:**
- Contract mobile developer (temporary, expensive)
- Delay Phase 7 officially, focus Phase Q1 2027 on Phase 8 (AI)
- Cross-train Backend Engineer on mobile (not ideal)

---

### If ML Engineer Hire Delayed

**Delay Duration:** 6 weeks (1.5 months)  
**Impact:**
- Phase 8 research pushed from Oct to Dec 2026
- Phase 8 implementation starts later, completes in Q3 instead of Q2 2027

**Recovery Options:**
- Use Tech Lead for initial model evaluation (2 weeks)
- Contract ML consultant for POC ($10K)
- Defer Phase 8 to Phase 9

---

## Dependency Management Process

### Weekly Standup (Every Monday)

**Attendees:** Tech Lead + Relevant Engineer  
**Duration:** 15 minutes

**Agenda:**
1. Any blockers encountered this week?
2. Are external dependencies on track?
3. Any new risks identified?
4. Action items from last week - complete?

**Escalation:** If blocker > 1 week, escalate to Product Manager

---

### Monthly Dependency Review (First Friday)

**Attendees:** Full team + Product Manager  
**Duration:** 1 hour

**Agenda:**
1. Review all dependencies from this document
2. Update status (✅ on track / 🔄 at risk / ⏸️ blocked)
3. Discuss any blockers > 2 weeks
4. Plan contingency actions if needed
5. Update timeline if changes detected

**Deliverable:** Updated dependency status document

---

### Quarterly Strategic Review (End of Quarter)

**Attendees:** Executive team + Product Manager  
**Duration:** 2 hours

**Agenda:**
1. Assess timeline accuracy vs plan
2. Identify emerging risks
3. Adjust hiring plan if needed
4. Discuss competitive/market changes affecting roadmap
5. Confirm priorities for next quarter

---

## Dependency Tracking Matrix

| Dependency | Owner | Status | Target Date | Risk Level |
|-----------|-------|--------|-------------|-----------|
| SendGrid integration | Backend 1 | ✅ Complete | 2026-11-15 | Low |
| Twilio integration | Backend 1 | ✅ Complete | 2026-11-15 | Low |
| Phase 5 complete | Backend 1 | 🔄 80% | 2027-01-15 | Medium |
| Phase 6 complete | Backend 2 | 🔄 70% | 2027-01-31 | Medium |
| Mobile Engineer hire | HR + Tech Lead | ⏸️ Recruiting | 2027-01-15 | High |
| Framework decision | Tech Lead | ⏸️ Planning | 2026-12-01 | High |
| ML Engineer hire | HR + Tech Lead | ⏸️ Recruiting | 2026-12-01 | High |
| PDF library choice | Frontend | ⏸️ Decision pending | 2026-09-15 | Medium |
| Data model approval | Product Manager | 🔄 60% | 2026-09-20 | Medium |

---

## Critical Decisions Pending

### Decision 1: Framework for Mobile (React Native vs Flutter)

**Timeline:** Decision by Dec 1, 2026  
**Owner:** Tech Lead  
**Input Needed:** POC evaluation results, team feedback  
**Decision Criteria:**
- Time-to-market (React Native faster?)
- Code sharing (Flutter better? ~90% vs 70%)
- Performance (Flutter native faster?)
- Team expertise (which easier to hire?)
- Long-term scalability

**Action:** Complete POC with both frameworks by Nov 15

---

### Decision 2: PDF Export Library (QuestPDF vs iText)

**Timeline:** Decision by Sept 30, 2026  
**Owner:** Frontend Engineer  
**Input Needed:** Feature comparison, licensing review  
**Decision Criteria:**
- Feature set (tables, charts, multiple pages?)
- Licensing cost ($100-$500 per developer)
- Learning curve (time to implement)
- Performance (report generation speed)

**Action:** Complete evaluation by Sept 15, demo to team

---

### Decision 3: AI Model Selection (GPT-4, Claude, Gemini)

**Timeline:** Decision by Dec 15, 2026  
**Owner:** Tech Lead (temp, until ML Engineer hired)  
**Input Needed:** POC results from 100 test receipts  
**Decision Criteria:**
- Accuracy (merchant, amount, date)
- Cost per request ($0.01 - $0.05 per receipt)
- Rate limiting (can handle our volume?)
- Hallucination rate (confidence in results)

**Action:** Complete POC by Dec 1

---

*For questions on dependencies or blockers, contact the Product Manager.*

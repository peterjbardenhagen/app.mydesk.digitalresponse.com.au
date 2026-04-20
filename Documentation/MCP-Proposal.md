# Techlight MyDesk MCP Enhancement Proposal

**Prepared by:** Peter Bardenhagen, Digital Response  
**Date:** April 16, 2026  
**Subject:** Enhancing Techlight MyDesk with AI-Powered MCP (Model Context Protocol) Integration

---

## Executive Summary

This proposal outlines a transformative enhancement to Techlight MyDesk that will integrate **Claude AI** (via the Model Context Protocol) directly into your business operations. This integration will enable your team to interact with your business data using natural language—through text or voice—making your staff more efficient, reducing costly errors, and providing you with unprecedented visibility into your business.

**The Bottom Line:** Your team will be able to ask questions about customers, quotes, projects, and financials in plain English and get instant, accurate answers. No more hunting through screens or wondering where that invoice went.

---

## What is MCP and Why Does It Matter?

**MCP (Model Context Protocol)** is a secure bridge that allows AI assistants like Claude to directly access and interact with your MyDesk database—while maintaining complete security and data privacy.

### How It Works (The Simple Version)

```
Bert (or any staff member): "Hey Claude, show me all quotes pending approval over $10,000"

Claude (via MCP): [Securely queries MyDesk] → "I found 4 quotes: Acme Corp $15,500, 
TechFlow $12,800... Would you like me to email them to you or show details?"

Bert: "Email me the Acme Corp one"

Claude: "Done. I've attached the quote PDF and noted that you requested follow-up."
```

### Security & Privacy

- **No data leaves your server** - Claude only sees what you ask it to see
- **User authentication required** - Only authenticated MyDesk users can access the system
- **Audit trail** - Every interaction is logged for compliance
- **No training on your data** - Your business data remains yours alone

---

## Benefits by User Role

### 1. Bert (Managing Director / Owner)

**Current Pain Points:**
- Spending too much time looking for information across different systems
- Not having real-time visibility into cash flow, outstanding quotes, or project status
- Relying on staff to compile reports manually
- Making decisions without complete data

**With MCP + Claude:**

| Scenario | Traditional Way | With Claude |
|----------|----------------|-------------|
| "What's our cash position?" | Wait for accounts to run reports, check MYOB, call bank | "Claude, what's our current cash position including outstanding invoices?" → Instant answer |
| "How did we perform this month vs last year?" | Ask for reports, wait, compile data | "Claude, compare this month's revenue to the same month last year" → Visual comparison |
| "Which customers owe us money?" | Run aged receivables report | "Claude, who owes us money and which invoices are over 30 days?" → Sorted list with amounts |
| "What's the status of the Smith project?" | Hunt through emails, ask project manager | "Claude, show me everything about the Smith project—quotes, invoices, timeline" → Complete overview |
| "Show me all quotes over $50K pending approval" | Manually review quote list | Instant filtered results with customer details |

**Time Saved:** 5-10 hours per week on information gathering and reporting  
**Better Decisions:** Real-time data means faster, more informed choices

---

### 2. Sales Team

**Current Pain Points:**
- Spending 20+ minutes preparing for client calls (looking up history, previous quotes, etc.)
- Missing upsell opportunities because they don't see the full customer picture
- Manual quote follow-ups falling through cracks
- Duplicating effort when customers ask about previous orders

**With MCP + Claude:**

| Scenario | Traditional Way | With Claude |
|----------|----------------|-------------|
| Preparing for client call | 15-20 minutes researching client history | "Claude, give me a briefing on Acme Corp before my 2pm call" → Complete history, open quotes, recent activity |
| Finding similar past quotes | Search through files, ask colleagues | "Claude, show me quotes similar to this lighting project from the past year" → Comparable quotes with pricing |
| Follow-up reminders | Manual calendar entries, sticky notes | "Claude, remind me to follow up on all quotes I sent last week that haven't been accepted" → Auto-generated task list |
| Creating quotes on the go | Back to office, log into system | "Claude, create a new quote for TechFlow using the same items as their last order" → Pre-populated quote ready to edit |
| "What did we quote them last time?" | Search through quote history | Instant access to all previous quotes with pricing |

**Key Voice Scenario:**
> *Driving to a site visit:* "Hey Claude, tell me everything about the Johnson project. What's pending, what's been invoiced, and are there any issues I should know about?"
> 
> *Claude responds via car audio:* "The Johnson project has 3 open quotes totaling $45,000. Two invoices are outstanding for $12,500. There was a delivery issue noted on March 15th that may need follow-up."

**Time Saved:** 8-12 hours per week per salesperson  
**Revenue Impact:** Better preparation = higher close rates, fewer missed opportunities

---

### 3. Accounts Team (Payable & Receivable)

**Current Pain Points:**
- Manual data entry between MyDesk and MYOB (double handling)
- Errors in decimal places (you mentioned this specifically)
- Hours spent reconciling discrepancies
- Chasing payments without visibility into customer history
- Difficulty matching payments to invoices

**With MCP + Claude:**

| Scenario | Traditional Way | With Claude |
|----------|----------------|-------------|
| MYOB reconciliation | Hours of manual checking, finding errors | "Claude, show me all invoices from this month that haven't been entered in MYOB yet" → Accurate list |
| Catching decimal errors | Find them during reconciliation (too late) | "Claude, flag any invoices with unusual amounts or decimal place errors" → Proactive alerts |
| Payment matching | Manual cross-reference between systems | "Claude, I have a $5,250 payment from Acme Corp—what invoices does this match?" → Suggested allocations |
| Aged receivables analysis | Run report, manually analyze | "Claude, analyze our aged receivables and tell me which customers are developing payment pattern issues" → Trend analysis |
| Monthly reporting | Compile from multiple sources | "Claude, generate my month-end summary including sales, outstanding invoices, and expenses" → Comprehensive report |

**Critical MYOB Integration Pathway:**

This MCP implementation creates the foundation for **direct MYOB integration**. The same secure connection that lets Claude talk to MyDesk can eventually:

- Push approved invoices directly to MYOB (no manual entry)
- Pull MYOB bank reconciliations into MyDesk
- Sync customer records between systems
- Automatically flag discrepancies for review

**Time Saved:** 15-20 hours per week for the accounts team  
**Error Reduction:** 90%+ reduction in data entry errors  
**Cash Flow:** Faster invoicing = faster payments

---

### 4. Bookkeeping & Data Entry Staff

**Current Pain Points:**
- Repetitive data entry tasks
- Transposing numbers (the decimal place issue)
- Looking up codes and categories
- Manual cross-referencing

**With MCP + Claude:**

| Scenario | Traditional Way | With Claude |
|----------|----------------|-------------|
| Entering expense invoices | Manual typing, looking up expense codes | "Claude, create an expense entry for $1,250.50 from OfficeWorks dated today, code it as office supplies" → Pre-filled, just confirm |
| Finding the right project code | Search through project list | "Claude, which project code should I use for the Smith Street lighting installation?" → Correct code provided |
| Batch data entry | One by one manual entry | "Claude, I've got 10 receipts here—can you help me batch enter them?" → Guided batch entry |
| Checking for duplicates | Manual review | "Claude, check if this invoice from ABC Supplier has already been entered" → Duplicate detection |

**Voice Integration for Efficiency:**
> *Hands on receipts:* "Claude, create expense. Date March 15th. Supplier: Lighting Direct. Amount: Four thousand two hundred fifty dollars and thirty cents. Project: Westfield Mall. Category: Electrical Supplies."
>
> *Claude:* "Created expense entry for Lighting Direct, $4,250.30, Westfield Mall project, Electrical Supplies category. Would you like me to scan for a matching purchase order?"

**Time Saved:** 10-15 hours per week  
**Error Reduction:** Near elimination of transposition and decimal errors

---

### 5. Project Managers & Operations

**Current Pain Points:**
- Scattered project information across emails, files, and MyDesk
- Difficulty tracking project profitability in real-time
- Missing cost overruns until too late
- Time-consuming status report preparation

**With MCP + Claude:**

| Scenario | Traditional Way | With Claude |
|----------|----------------|-------------|
| Project health check | Compile data from multiple screens | "Claude, give me a health check on the City Center project—budget vs actual, timeline, issues" → Comprehensive dashboard |
| Resource allocation | Spreadsheet juggling | "Claude, which technicians are available next week and what projects are they assigned to?" → Resource view |
| Cost overrun alerts | Discover at month-end | "Claude, alert me if any active project exceeds 80% of its budget" → Proactive monitoring |
| Client status updates | 30 minutes writing email | "Claude, draft a status update email for the Johnson project covering this week's progress" → Professional draft ready to send |
| Finding related documents | Search through file system | "Claude, show me all documents, photos, and correspondence for the Smith Street job" → Complete file compilation |

**Time Saved:** 8-10 hours per week  
**Project Profitability:** Real-time visibility prevents overruns

---

## Real-World Daily Scenarios

### Morning Routine - Bert

**Traditional:**
1. Check emails (30 min)
2. Call accounts for cash position update (15 min)
3. Review sales pipeline report (20 min)
4. Check project status with PMs (30 min)
5. **Total: 1 hour 35 minutes**

**With Claude:**
1. "Good morning Claude, what's the business overview today?"
2. Claude provides: Cash position, yesterday's sales, outstanding approvals, project alerts, customer issues requiring attention
3. Ask follow-up questions as needed
4. **Total: 15 minutes** with **better information**

---

### Sales Call Preparation

**Traditional:**
1. Search for customer in MyDesk (2 min)
2. Look up previous quotes (3 min)
3. Check for outstanding invoices/credit issues (3 min)
4. Review project history (5 min)
5. Compile notes manually (5 min)
6. **Total: 18 minutes**

**With Claude:**
1. "Claude, I'm calling Johnson & Associates in 10 minutes—give me everything I need to know"
2. Claude provides comprehensive briefing including: Company overview, all previous quotes with outcomes, outstanding invoices, credit status, active projects, recent interactions, buying patterns
3. **Total: 30 seconds**

---

### End-of-Day Accounts Reconciliation

**Traditional:**
1. Export data from MyDesk (5 min)
2. Open MYOB (2 min)
3. Manually compare entries (30 min)
4. Find and fix 2-3 errors (15 min)
5. Generate report for Bert (10 min)
6. **Total: 1 hour 2 minutes**

**With Claude:**
1. "Claude, run the daily reconciliation between MyDesk and MYOB"
2. Claude identifies discrepancies, highlights the 3 items needing attention
3. "Claude, show me the details on these flagged items"
4. Fix issues, confirm reconciliation
5. "Claude, email the summary to Bert"
6. **Total: 15 minutes** with **zero errors missed**

---

## The MYOB Integration Roadmap

You specifically mentioned the pain of manual MYOB entry and decimal place errors. Here's how MCP paves the way for full integration:

### Phase 1 (Immediate with MCP)
- Claude can cross-reference MyDesk and MYOB data
- Identify discrepancies automatically
- Generate reconciliation reports
- Flag potential errors before they become problems

### Phase 2 (3-6 months)
- Semi-automated MYOB entry via guided workflow
- Pre-populated MYOB transactions from MyDesk data
- Validation checks before data leaves MyDesk
- Automatic matching of MyDesk invoices to MYOB records

### Phase 3 (6-12 months)
- Full API integration between MyDesk and MYOB
- Real-time sync of approved transactions
- Automatic bank reconciliation matching
- Single source of truth with bidirectional sync

**Estimated Savings from MYOB Integration:**
- Current: ~20 hours/week manual entry and reconciliation
- After integration: ~2 hours/week exception handling only
- **Annual savings: 936 hours ≈ $35,000-$50,000 in labor costs**
- **Error reduction: 95%+** (catching decimal errors before they hit MYOB)

---

## Investment Required

### Development Costs

| Phase | Description | Timeline | Investment |
|-------|-------------|----------|------------|
| **Phase 1** | MCP Core Infrastructure + Basic Query Functions | 4-6 weeks | $12,000 - $15,000 |
| **Phase 2** | Advanced Features (Voice, Proactive Alerts, Reporting) | 6-8 weeks | $15,000 - $20,000 |
| **Phase 3** | MYOB Integration Foundation | 4-6 weeks | $10,000 - $15,000 |
| **Phase 4** | Full MYOB Sync & Advanced AI Features | 6-8 weeks | $15,000 - $25,000 |

**Total Initial Investment: $52,000 - $75,000**

*Note: Can be phased. Phase 1 alone delivers immediate value.*

### Ongoing Costs

| Item | Monthly Cost | Annual Cost |
|------|--------------|-------------|
| Claude AI API Usage (estimated) | $200 - $500 | $2,400 - $6,000 |
| Server/Hosting (if upgrades needed) | $100 - $300 | $1,200 - $3,600 |
| Maintenance & Updates | $500 - $1,000 | $6,000 - $12,000 |
| **Total Annual Ongoing** | **$800 - $1,800** | **$9,600 - $21,600** |

### ROI Calculation

**Conservative Estimate (10 staff):**
- Time saved per staff member: 5 hours/week average
- Total hours saved: 50 hours/week = 2,600 hours/year
- Average loaded labor rate: $45/hour
- **Annual savings: $117,000**

**Payback Period: 5-7 months**

**Plus intangible benefits:**
- Better decision making with real-time data
- Reduced staff frustration
- Improved customer service
- Competitive advantage
- Foundation for future automation

---

## Future Scope & Strategic Value

### 12-Month Roadmap

**Q1-Q2:**
- Full MCP integration with voice capability
- Mobile app integration (ask Claude on the go)
- Proactive alerting (Claude watches for issues and alerts you)

**Q3-Q4:**
- MYOB full synchronization
- Predictive analytics (Claude identifies trends and opportunities)
- Automated reporting suite

### 24-Month Vision

**Intelligent Business Assistant:**
- "Claude, we're bidding on the Airport project—analyze our past similar projects and suggest pricing"
- "Claude, predict our cash flow for the next 3 months based on pipeline and historical patterns"
- "Claude, identify which customers are at risk of leaving based on declining order frequency"
- "Claude, optimize our inventory purchasing based on project pipeline"

**Integration Expansion:**
- Supplier integrations (automated ordering)
- Customer portal with AI support
- Field technician mobile AI assistant
- Automated compliance reporting

### Competitive Advantage

While your competitors are:
- Manually searching for customer information
- Double-handling data entry
- Discovering cash flow issues too late
- Making decisions without complete data

**You will be:**
- Asking questions and getting instant answers
- Focusing staff on high-value activities
- Proactively managing the business
- Operating with complete visibility

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Data Security | Enterprise-grade encryption, no external data storage, audit logging |
| Staff Adoption | Phased rollout with training, intuitive natural language interface |
| System Downtime | Redundant design, MyDesk operates independently if MCP is offline |
| Cost Overrun | Fixed-price milestones, clear scope per phase |
| Technology Changes | Open standard protocols, vendor-agnostic design |

---

## Why Now?

1. **AI technology has matured** - Claude is enterprise-ready and secure
2. **Staff efficiency is critical** - Labor costs rising, efficiency is competitive advantage
3. **Error costs compound** - The decimal place errors you mentioned will only multiply as you grow
4. **Foundation for growth** - MCP enables scalable processes without proportional staff increases
5. **First-mover advantage** - Your competitors aren't doing this yet

---

## Next Steps

If you'd like to proceed, I recommend:

1. **Phase 1 Pilot** ($12,000-$15,000)
   - Core MCP integration
   - Basic query functions
   - Limited user rollout (Bert + 2-3 key staff)
   - 4-6 week timeline

2. **Review & Expand**
   - Measure actual time savings
   - Gather user feedback
   - Prioritize Phase 2 features

3. **Full Rollout & MYOB Integration**
   - Scale to all users
   - Begin MYOB integration pathway
   - Implement advanced features

---

## Questions for You

To refine this proposal, I'd like to understand:

1. How many staff members would use this system initially?
2. Which pain point is most urgent—information access, MYOB errors, or sales efficiency?
3. Are there specific reports or queries you run frequently that take too much time?
4. What's your current annual spend on labor for data entry and reconciliation?
5. Would you prefer to start with a pilot group or roll out to everyone at once?

---

## Contact

**Peter Bardenhagen**  
Digital Response  
peter@digitalresponse.com.au  
[Your Phone Number]

---

*This proposal is valid for 30 days. I'm happy to meet in person to demonstrate MCP technology and discuss how it can specifically address your business needs.*

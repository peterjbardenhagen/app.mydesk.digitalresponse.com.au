# MyDesk AI — Product Roadmap

This document outlines the strategic direction and upcoming features for the MyDesk AI-powered business management platform, tailored for Australian Small to Medium Enterprises (SMEs).

> An in-app version of this roadmap is available at **/roadmap** inside MyDesk.

## 🚀 Current Status (v3.0 – v3.1)
- **AI Core:** Natural language querying of operational and financial data ("Ask AI").
- **Multi-Tenancy:** Architecture support for dynamic rebranding and tenant-specific configuration.
- **Voice-to-Action:** Telegram bot integration with voice transcription (Whisper) and task execution.
- **Unified Branding:** Dynamic UI and PDF generation based on tenant settings.
- **Customer & Supplier Portals:** Self-service portals for quotes, invoices, jobs, and POs.
- **Dashboard Carousel:** Personal vs company KPIs, attention items, business health score.
- **Roles & Permissions:** Fine-grained role-based access control across all modules.

---

## 🗺️ Future Roadmap

### Phase 1 — Financial & Compliance Deepening (AU-specific)
*   **Integrated Bank Feeds**: Direct connection to Australian banks (via Basiq/Yodlee) for real-time cash flow visibility and conversational reconciliation.
*   **ATO / STP Phase 2 Payroll**: Built-in payroll module supporting Single Touch Payroll Phase 2, super stream and award interpretation – allowing micro-SMEs to manage employees without separate software.
*   **BAS Preparation Assistant**: AI-driven categorisation and reporting to simplify quarterly BAS lodgement, with guided GST review.
*   **e-Invoicing (PEPPOL)**: Send and receive PEPPOL-compliant invoices to large customers and government agencies.
*   **TPAR (Taxable Payments Annual Report)**: Automated contractor payment reporting for building, cleaning, courier, IT and security industries.
*   **ABN / GST Validation**: Real-time ABN lookup and GST registration verification on every supplier and customer record.

### Phase 2 — AI Operations & Growth
*   **AI Marketing Automation**:
    *   Automatic draft generation for LinkedIn / Email based on "Project Wins".
    *   Predictive customer nurture sequences based on purchase patterns.
    *   AI-written quote follow-ups and review requests.
*   **Inventory & Asset Tracking**:
    *   "Scan to MyDesk" mobile companion for QR/barcode stock movements.
    *   Warehouse heatmaps driven by AI-analysed order frequency.
    *   Reorder point recommendations using lead-time and seasonality.
*   **Advanced Project Profitability**: Real-time cross-referencing of supplier costs against quote estimates with proactive "Overrun Alerts" surfaced on the dashboard.
*   **AI Job Scheduler**: Automatic technician/job dispatch with travel-time optimisation across Australian metro and regional areas.
*   **Smart Document Capture**: Drop a supplier bill, receipt or remittance into MyDesk – the AI reads it, matches it to a PO and books it.

### Phase 3 — Ecosystem & Platform
*   **White-Label Customer & Supplier Portals**: Tenants invite their own customers/suppliers to view quotes, pay invoices and chat to a restricted "Customer AI".
*   **Integrated Payment Gateway**: One-click "Pay Now" on invoices (Stripe/Square/Tyro) with automatic fee reconciliation.
*   **Predictive Analytics**:
    *   Cash flow forecasting (3–6 months) with scenario modelling.
    *   "Customer at Risk" detection based on order decay and engagement signals.
    *   Win-rate prediction on open quotes.
*   **Marketplace & Plugins**: Third-party connectors (MYOB, Xero, QuickBooks, HubSpot, Mailchimp, Shopify) with a public app SDK.
*   **Mobile App (iOS/Android)**: Offline-first field app for quotes, jobs, time and photo capture.

### Phase 4 — Australian SMB AI Differentiators
*   **Fair Work Award Assistant**: Plain-English answers about award rates, penalty hours, leave entitlements and casual conversion.
*   **OH&S / WHS Compliance Hub**: SWMS templates, toolbox talks, incident logging and AI safety insights.
*   **Grant & Tender Finder**: AI scans federal and state grants (R&D Tax Incentive, EMDG, state innovation grants) and pre-fills applications from MyDesk data.
*   **Energy & Sustainability Tracker**: Track Scope 1/2 emissions for tenders and ESG reporting; flag energy savings.
*   **Cyber Resilience Score**: Continuous Essential Eight self-assessment with AI remediation guidance.
*   **Local Supplier Intelligence**: Suggest Australian-owned alternatives, lead-time forecasts and freight cost comparisons.
*   **Multilingual AI**: Natural language support in English, Mandarin, Vietnamese, Arabic and Greek for diverse Australian workforces.

---

## 🛠️ Infrastructure Evolution
*   **Database Partitioning**: Schema-per-tenant for high-security Australian government / medical clients.
*   **Self-Healing Data Layer**: AI-monitored data integrity that automatically fixes common entry errors (e.g., incorrect ABN formats or GST mismatches).
*   **On-Premise / Sovereign LLM Support**: Locally hosted models (Llama 3 / Phi-3) for clients with extreme data sovereignty requirements – AU data centre only.
*   **SOC 2 Type II & ISO 27001**: Formal certification roadmap.
*   **Observability**: OpenTelemetry tracing, structured logs, and an in-app Error Logs viewer for tenant admins.

---

*Prepared by Digital Response*
*Last Updated: May 3, 2026*

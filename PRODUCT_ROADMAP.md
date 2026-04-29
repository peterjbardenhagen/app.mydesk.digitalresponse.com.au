# MyDesk AI — Product Roadmap

This document outlines the strategic direction and upcoming features for the MyDesk AI-powered business management platform, tailored for Australian Small to Medium Enterprises (SMEs).

## 🚀 Current Status (v3.0)
- **AI Core:** Natural language querying of operational and financial data.
- **Multi-Tenancy:** Architecture support for dynamic rebranding and tenant-specific configuration.
- **Voice-to-Action:** Telegram bot integration with voice transcription (Whisper) and task execution.
- **Unified Branding:** Dynamic UI and PDF generation based on tenant settings.

---

## 🗺️ Future Roadmap

### Phase 1: Financial & Compliance Deepening
*   **Integrated Bank Feeds**: Direct connection to Australian banks (via Basiq/Yodlee) for real-time cash flow visibility and "conversational reconciliation".
*   **ATO / STP Phase 2 Integration**: Built-in payroll module supporting Single Touch Payroll requirements, allowing micro-SMEs to manage employees without separate software.
*   **BAS Preparation Assistant**: AI-driven categorization and reporting to simplify quarterly BAS lodgement.

### Phase 2: Operations & Growth
*   **AI Marketing Automation**: 
    *   Automatic draft generation for LinkedIn/Email based on "Project Wins".
    *   Predictive customer nurture sequences based on purchase patterns.
*   **Inventory & Asset Tracking**: 
    *   "Scan to MyDesk" mobile app for QR-code based stock management.
    *   Warehouse heatmaps driven by AI-analyzed order frequency.
*   **Advanced Project Profitability**: Real-time cross-referencing of MYOB purchase costs against MyDesk quote estimates with proactive "Overrun Alerts".

### Phase 3: Ecosystem & Platform
*   **White-Label Portal**: A dedicated "Client Portal" where tenants can invite their own customers to view quotes, pay invoices, and talk to a restricted "Customer AI".
*   **Integrated Payment Gateway**: One-click "Pay Now" on invoices (Stripe/Square) with automatic fee reconciliation in MYOB.
*   **Predictive Analytics**: 
    *   Cash flow forecasting (3-6 months).
    *   "Customer at Risk" detection based on order decay.

---

## 🛠️ Infrastructure Evolution
*   **Database Partitioning**: Moving from shared schema to schema-per-tenant for high-security Australian government/medical clients.
*   **Self-Healing Data Layer**: AI-monitored data integrity that automatically fixes common entry errors (e.g., incorrect ABN formats or GST mismatches).
*   **On-Premise LLM Support**: Support for locally hosted models (Llama 3/Phi-3) for clients with extreme data sovereignty requirements.

---

*Prepared by Digital Response*
*Last Updated: April 27, 2026*

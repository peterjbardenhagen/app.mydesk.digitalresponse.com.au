# MyDesk — Design System & Wireframes

**Platform:** Enterprise AI Brain — MyDesk  
**Version:** 1.0 | July 2026  
**Stack:** Blazor Server (.NET 8) + Android Native (MAUI)

## Design Principles

1. **Clarity first** — Every screen answers "what can I do here?" in under 3 seconds
2. **Mobile-companion** — Web is the primary workspace; mobile handles approvals, notifications, and quick actions
3. **AI-augmented** — AgentsOS DAG workflows are first-class citizens, not add-ons
4. **Brand identity** — The Desky avatar and "⬡ MyDesk" icon maintain consistent presence

## Directory Structure

```
Design/
├── DESIGN-SDLC.md      ← Mandatory design-first development process
├── README.md            ← This file
├── web/
│   ├── index.html       ← Web wireframe prototype (all modules)
│   └── README.md        ← Web-specific notes
└── mobile/
    ├── index.html       ← Mobile wireframe prototype (all modules)
    └── README.md        ← Mobile-specific notes
```

## Modules Covered

| Module | Web | Mobile | Notes |
|--------|:---:|:------:|-------|
| Dashboard | ✅ | ✅ | KPI tiles, sidebar nav, AgentsOS widget |
| Companies/CRM | ✅ | 🔄 | Master-detail with search |
| Quotes | ✅ | 🔄 | Pipeline with conversion tracking |
| Invoices | ✅ | 🔄 | Tabular listing with status chips |
| Purchase Orders | 🔄 | ❌ | Not yet designed |
| Job Orders | 🔄 | ❌ | Not yet designed |
| Expenses | ✅ | ✅ | Receipt capture with approval workflow |
| Timesheets | 🔄 | 🔄 | Week view, submit, approve |
| AgentsOS DAG | ✅ | 🔄 | Node-based workflow visualizer |
| Approvals | ✅ | ✅ | Human-in-loop approve/reject |
| Banking | 🔄 | ❌ | Future financial dashboard |
| Notifications | ✅ | ✅ | Bell icon with badge count |
| Profile/Settings | 🔄 | ✅ | Account preferences |

✅ = Complete   🔄 = Partial   ❌ = Not yet created

## Viewing the Prototypes

Open directly in any browser:
- **Web:** `Design/web/index.html`
- **Mobile:** `Design/mobile/index.html`

No build step or server required.

## Color Palette

| Role | Hex | Usage |
|------|:---:|-------|
| Primary | `#00c8c8` | Links, buttons, active states |
| Dark | `#0a1628` | Sidebar backgrounds |
| Surface | `#f5f7fa` | Page background |
| Card | `#ffffff` | Card backgrounds |
| Text | `#1a1a1a` | Body text |
| Muted | `#94a3b8` | Secondary labels |
| Success | `#2e7d32` | Approved status |
| Warning | `#e65100` | Draft/Pending status |
| Info | `#1565c0` | Submitted status |

## Next Steps

1. High-fidelity mockups with full brand styling
2. Interactive prototype with clickable flows
3. Missing modules: Purchase Orders, Job Orders, Banking
4. Mobile-specific views for all CRM modules
5. User testing with demo tenant

# MyDesk — Low-Fi Wireframe Prototype (Legacy)

**This folder is preserved for reference. The canonical Design directory is now at `../Design/`.**

**Version 1.0 | July 2026**

This directory (legacy) contains low-fidelity wireframe prototypes for the MyDesk Enterprise AI Brain platform, covering both **web** and **mobile** experiences.

> **⚠️ NOTE:** The new `Design/` folder replaces this with a proper Web + Mobile structure.  
> See `../Design/` for the latest wireframes and `../Design/DESIGN-SDLC.md` for the design-first development process.

## Contents

| File | Description |
|------|-------------|
| `index.html` | Full web + mobile wireframe prototype (single page) |
| `README.md` | This file — overview and design rationale |

## How to View

Open `wireframes/index.html` in any browser. No build step or server required.

The page includes interactive navigation — click the section links at the top to jump between:

1. **Dashboard** — KPI tiles, sidebar nav, AgentsOS summary, upcoming tasks
2. **Companies/CRM** — Searchable company list with status chips
3. **Quotes** — Sales pipeline with conversion rate bar
4. **Expenses** — Tab-filterable with receipt attachment indicators
5. **AgentsOS DAG** — Node-based workflow visualizer with gated approval
6. **Mobile** — Three phone frames (Login, Dashboard, Approvals)

## Design Decisions

### Web Layout
- **Sidebar navigation** — Fixed left rail for primary modules (Blazor Server pattern)
- **KPI grid** — Top-level metrics row for at-a-glance status
- **Card-based detail** — Each module uses a table layout with status chips and action buttons
- **AgentsOS DAG** — Circular node visualization with status colors, matching the SVG renderer

### Mobile Layout
- **Bottom tab nav** — Standard mobile pattern (Home, Documents, Expenses, Settings)
- **Single-column KPI** — Smaller cards for mobile viewport
- **Swipeable approval queue** — Approve/reject actions on individual items
- **Push notification integration** — Bell icon with badge count

### Modules Covered
| Module | Web | Mobile |
|--------|:---:|:------:|
| Dashboard (KPI, tasks, weather) | ✅ | ✅ |
| Companies & Contacts (CRM) | ✅ | 🔄 |
| Quotes & Sales Pipeline | ✅ | 🔄 |
| Invoices & POs | ✅ | 🔄 |
| Expenses & Receipt Capture | ✅ | ✅ |
| Timesheets | 🔄 | 🔄 |
| AgentsOS DAG Workflow | ✅ | 🔄 |
| Approvals (Human-in-loop) | ✅ | ✅ |
| Profile & Settings | 🔄 | ✅ |

## Next Steps (Future Iterations)

1. **High-fidelity mockups** — Convert to Figma or HTML with full branding
2. **Interactive prototype** — Add clickable flows (login → dashboard → action)
3. **Mobile-specific views** — Detailed screens for expenses, quotes, agentsOS on mobile
4. **User testing** — Validate flows with actual demo users
5. **Animation spec** — Desky avatar expressions, DAG node transitions, approval micro-interactions

---

*MyDesk Enterprise AI Brain — Built by Digital Response. Powered by AgentsOS.*

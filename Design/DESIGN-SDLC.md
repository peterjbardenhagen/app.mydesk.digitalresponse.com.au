# Design-First SDLC — MyDesk Platform

> **This is a mandatory step** in the software development lifecycle for the MyDesk Enterprise AI Brain platform.
> Every new feature, module, or significant change **must** begin with a low-fidelity wireframe prototype.

## Why Design-First?

1. **Clarity before code** — Stakeholders and developers agree on layout, flow, and content before a single line of Blazor/React is written
2. **Cheap iteration** — HTML wireframes cost minutes to change vs. hours of refactoring production code
3. **Mobile-first awareness** — Forces consideration of responsive layout early
4. **AI-agent alignment** — Wireframes act as spec for both human developers and AI coding agents
5. **User validation** — Low-fi prototypes can be tested with real users before committing to build

## The Process

### Step 1: Information Architecture
Map out the pages, navigation hierarchy, and data relationships. Document in `Design/ARCH.md`.

### Step 2: Low-Fi Wireframes
Create a working HTML prototype in `Design/web/` or `Design/mobile/`:
- Use gray-scale boxes, placeholder text, simple shapes
- Cover all states: loading, empty, error, populated
- Include navigation flows (at minimum a nav bar showing all modules)
- No branding, no high-fidelity styling

### Step 3: Review & Approve
- Walk through with stakeholders
- Check for missing states, confusing flows, accessibility gaps
- Approve before any Blazor/React/MAUI code begins

### Step 4: Build From Wireframe
- Use the wireframe as the visual spec
- Implement component-by-component
- Reference the wireframe for layout proportions and spacing

### Step 5: Validate
- Compare implemented UI against the wireframe
- Update wireframe if design changed during implementation (keep it as source of truth)

## Folder Structure

```
Design/
├── DESIGN-SDLC.md        ← This file
├── README.md              ← Current design overview
├── web/
│   ├── index.html         ← All web page wireframes
│   └── README.md          ← Web-specific notes
└── mobile/
    ├── index.html         ← All mobile page wireframes
    └── README.md          ← Mobile-specific notes
```

## Module Coverage Checklist

| Module | Web | Mobile | Wireframe Exists |
|--------|:---:|:------:|:----------------:|
| Dashboard (KPI, tasks, weather) | ✅ | ✅ | ✅ |
| Companies & Contacts (CRM) | ✅ | 🔄 | ✅ |
| Quotes & Sales Pipeline | ✅ | 🔄 | ✅ |
| Invoices | ✅ | 🔄 | 🔄 |
| Purchase Orders | ✅ | 🔄 | ❌ |
| Job Orders | ✅ | 🔄 | ❌ |
| Expenses & Receipt Capture | ✅ | ✅ | ✅ |
| Timesheets | 🔄 | 🔄 | 🔄 |
| AgentsOS DAG Workflow | ✅ | 🔄 | ✅ |
| Approvals (Human-in-loop) | ✅ | ✅ | ✅ |
| Banking & Financials | 🔄 | ❌ | ❌ |
| Notifications | ✅ | ✅ | ✅ |
| Profile & Settings | 🔄 | ✅ | ✅ |
| Admin / Tenant Management | 🔄 | ❌ | ❌ |

✅ = Complete    🔄 = Partial / Needs review    ❌ = Not yet created

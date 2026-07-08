# MyDesk Outlook Add-in

An Outlook add-in that surfaces three MyDesk workflows next to your inbox — always
pointed at Production (`https://app.mydesk.digitalresponse.com.au`).

## Features

| # | Button | What it does |
|---|--------|--------------|
| 1 | **Log Change Request** | Records the open email as a client change-request in MyDesk. Handy for small "can you also do X?" asks that arrive without a formal quote — captures the context so you can decide later whether to bill for it. |
| 2 | **Add to Contact** | Saves the email against the sender's Contact record in MyDesk as an Email note. If no Contact with that email exists, a new one is created from the sender's display name. |
| 3 | **Legal Report** | Generates a PDF folio report for law-firm billing. Filter by date range (defaults to start of the current month → today) and optionally by one or more email addresses. Emails are collapsed to the latest message per conversation. Attachments are listed. Folios are counted at 1 per 100 words (or part thereof). |

### About folios

In legal billing a folio is a unit of written work. The convention we use:

- 1–100 words → **1 folio**
- 101–200 words → **2 folios**
- 201–300 words → **3 folios**
- … `folios = ceil(words / 100)` (minimum 1 for any non-empty message)

We deliberately don't include billing rates — you plug those into your practice
management system.

## Files

```
Outlook.Addin/
├── manifest.xml     ← sideload this
└── README.md        ← you're reading it

src/MyDesk.Web/wwwroot/outlook-addin/   ← served at
                                          https://app.mydesk.digitalresponse.com.au/outlook-addin/
├── taskpane.html
├── taskpane.js
├── taskpane.css
├── commands.html
├── commands.js
└── icons/
    ├── icon-16.png
    ├── icon-32.png
    ├── icon-64.png
    └── icon-80.png
```

The **task-pane HTML/JS/CSS lives inside `MyDesk.Web/wwwroot`** so that the same
deploy that ships the web app also ships the add-in. Nothing else to host.

## Sideloading

### Outlook on the web / new Outlook

1. Open Outlook and choose **Get add-ins** (⚙️ → Manage add-ins).
2. Go to **My add-ins → Custom add-ins → Add a custom add-in → Add from file…**.
3. Upload `Outlook.Addin/manifest.xml`.
4. Once installed, open any email — the **MyDesk** group appears on the ribbon
   with an **Open MyDesk** button.

### Outlook desktop (Windows / Mac)

Same flow via **File → Manage Add-ins**, or centrally deployed by your Microsoft
365 admin via the Admin Center → **Settings → Integrated apps**.

## First-time setup inside the pane

The first time you open the pane, it asks for your MyDesk **API key**. This is
the same key configured in `appsettings.json` under `Api:Key` on production. The
key is stored in Outlook's `roamingSettings` (per-mailbox, encrypted at rest by
Office 365) so you enter it once.

## Development notes

- All backend endpoints live under `/api/outlook-addin/*` in
  `src/MyDesk.Web/Api/Controllers/OutlookAddinController.cs`.
- Authentication: `X-Api-Key` header (existing MyDesk API-key scheme).
- The Legal Report PDF is generated server-side with QuestPDF (already in the
  solution).
- The task pane fetches messages using Office.js + the Outlook REST API
  (`Office.context.mailbox.getCallbackTokenAsync({isRest: true})`), so it uses
  the signed-in user's own mailbox — no separate Graph app registration needed.

## Not doing (out of scope for v1)

- Compose-mode buttons (this add-in only surfaces when reading a message, plus
  the Legal Report which needs no message).
- Attachment content extraction — the Legal Report counts attachments and lists
  their filenames, but does not OCR or word-count them.
- Multi-tenant switching from inside the pane — the API key already carries a
  tenant claim.

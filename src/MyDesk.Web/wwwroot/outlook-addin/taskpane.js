/*
 * MyDesk Outlook add-in — task pane logic.
 *
 * Always points at Production. All backend calls hit
 * https://app.mydesk.digitalresponse.com.au/api/outlook-addin/*.
 */
(function () {
  "use strict";

  const API_BASE = "https://app.mydesk.digitalresponse.com.au/api/outlook-addin";
  const ROAMING_KEY = "myDeskApiKey";
  const OUTLOOK_REST_VERSION = "v2.0";

  const state = {
    apiKey: null,
    currentItem: null, // enriched email metadata for change-request + contact panels
  };

  const $ = (id) => document.getElementById(id);

  // ------------------------------------------------------------------
  // Boot
  // ------------------------------------------------------------------
  Office.onReady(function (info) {
    if (info.host !== Office.HostType.Outlook) {
      showFatal("This add-in requires Outlook.");
      return;
    }

    wireTabs();
    wireForms();

    state.apiKey = Office.context.roamingSettings.get(ROAMING_KEY) || null;
    if (!state.apiKey) {
      openSetup();
    } else {
      loadCurrentItem();
      seedLegalDates();
    }
  });

  // ------------------------------------------------------------------
  // Setup / API-key flow
  // ------------------------------------------------------------------
  function openSetup() {
    setPanel("setup-panel");
    $("api-key-input").focus();
  }

  async function saveApiKey() {
    const key = ($("api-key-input").value || "").trim();
    setStatus("setup-status", "");
    if (!key) {
      setStatus("setup-status", "Enter a key.", "err");
      return;
    }
    disable("save-api-key", true);
    setStatus("setup-status", "Testing key…");

    try {
      const ok = await pingApi(key);
      if (!ok) throw new Error("Server rejected the key.");
      state.apiKey = key;
      await new Promise((resolve, reject) => {
        Office.context.roamingSettings.set(ROAMING_KEY, key);
        Office.context.roamingSettings.saveAsync((r) => {
          r.status === Office.AsyncResultStatus.Succeeded ? resolve() : reject(r.error);
        });
      });
      setStatus("setup-status", "Connected.", "ok");
      setPanel("change-request-panel");
      loadCurrentItem();
      seedLegalDates();
    } catch (err) {
      setStatus("setup-status", "Failed: " + err.message, "err");
    } finally {
      disable("save-api-key", false);
    }
  }

  async function pingApi(key) {
    try {
      const r = await fetch(API_BASE + "/ping", {
        method: "GET",
        headers: { "X-Api-Key": key },
      });
      return r.ok;
    } catch (_) {
      return false;
    }
  }

  // ------------------------------------------------------------------
  // Tabs
  // ------------------------------------------------------------------
  function wireTabs() {
    document.querySelectorAll(".tab").forEach((btn) => {
      btn.addEventListener("click", () => {
        const target = btn.dataset.tab + "-panel";
        setPanel(target);
      });
    });
  }

  function setPanel(id) {
    document.querySelectorAll(".panel").forEach((p) => p.classList.add("hidden"));
    $(id).classList.remove("hidden");
    document.querySelectorAll(".tab").forEach((t) =>
      t.setAttribute("aria-selected", id === t.dataset.tab + "-panel" ? "true" : "false")
    );
  }

  // ------------------------------------------------------------------
  // Current email context (used by Change Request + Contact panels)
  // ------------------------------------------------------------------
  async function loadCurrentItem() {
    const item = Office.context.mailbox.item;
    if (!item || !item.itemType) {
      renderEmailSummary("(No email open — open a message from the ribbon to log it.)");
      disable("cr-submit", true);
      disable("contact-submit", true);
      return;
    }

    // ItemId is EWS-format; for Outlook REST APIs it must be converted.
    const from = item.from || item.sender || {};
    const summary = {
      itemId: item.itemId,
      subject: item.subject || "(no subject)",
      fromName: from.displayName || "",
      fromEmail: (from.emailAddress || "").toLowerCase(),
      receivedAt: item.dateTimeCreated ? new Date(item.dateTimeCreated).toISOString() : null,
      body: "",
      attachments: (item.attachments || []).map((a) => ({
        name: a.name,
        size: a.size,
        contentType: a.contentType,
      })),
    };

    // Fetch plain-text body.
    try {
      summary.body = await getBodyAsync("text");
    } catch (_) {
      summary.body = "";
    }

    state.currentItem = summary;
    renderEmailSummary(summary);

    if (!$("cr-title").value) $("cr-title").value = summary.subject;
    disable("cr-submit", false);
    disable("contact-submit", false);
  }

  function renderEmailSummary(input) {
    const html =
      typeof input === "string"
        ? `<div class="skeleton">${escapeHtml(input)}</div>`
        : `<div class="from">${escapeHtml(input.fromName || input.fromEmail || "(unknown sender)")}${input.fromEmail ? ' &lt;' + escapeHtml(input.fromEmail) + '&gt;' : ''}</div>
           <div class="subject">${escapeHtml(input.subject)}</div>
           <div class="meta">${input.receivedAt ? new Date(input.receivedAt).toLocaleString() : ""}${
             input.attachments.length ? " · " + input.attachments.length + " attachment(s)" : ""
           }</div>`;
    $("cr-email-summary").innerHTML = html;
    $("contact-email-summary").innerHTML = html;
  }

  function getBodyAsync(format) {
    return new Promise((resolve, reject) => {
      Office.context.mailbox.item.body.getAsync(
        format === "html" ? Office.CoercionType.Html : Office.CoercionType.Text,
        (r) => (r.status === Office.AsyncResultStatus.Succeeded ? resolve(r.value || "") : reject(r.error))
      );
    });
  }

  // ------------------------------------------------------------------
  // Feature 1 — Log Change Request
  // ------------------------------------------------------------------
  function wireForms() {
    $("save-api-key").addEventListener("click", saveApiKey);
    $("api-key-input").addEventListener("keydown", (e) => {
      if (e.key === "Enter") saveApiKey();
    });
    $("reset-key").addEventListener("click", (e) => {
      e.preventDefault();
      Office.context.roamingSettings.remove(ROAMING_KEY);
      Office.context.roamingSettings.saveAsync(() => {
        state.apiKey = null;
        openSetup();
      });
    });

    $("cr-submit").addEventListener("click", submitChangeRequest);
    $("contact-submit").addEventListener("click", submitContact);
    $("legal-generate").addEventListener("click", generateLegalReport);
  }

  async function submitChangeRequest() {
    if (!state.currentItem) return;
    setStatus("cr-status", "Logging…");
    disable("cr-submit", true);

    const payload = {
      title: ($("cr-title").value || state.currentItem.subject).trim(),
      notes: ($("cr-notes").value || "").trim(),
      impactCost: parseFloat($("cr-impact").value) || null,
      fromEmail: state.currentItem.fromEmail,
      fromName: state.currentItem.fromName,
      subject: state.currentItem.subject,
      receivedAt: state.currentItem.receivedAt,
      body: state.currentItem.body,
      outlookItemId: state.currentItem.itemId,
    };

    try {
      const r = await apiPost("/change-request", payload);
      const j = await r.json();
      if (!r.ok) throw new Error(j.error || r.statusText);
      setStatus("cr-status", `Logged as change request #${j.id}.`, "ok");
      $("cr-notes").value = "";
      $("cr-impact").value = "";
    } catch (err) {
      setStatus("cr-status", "Failed: " + err.message, "err");
    } finally {
      disable("cr-submit", false);
    }
  }

  // ------------------------------------------------------------------
  // Feature 2 — Add to Contact
  // ------------------------------------------------------------------
  async function submitContact() {
    if (!state.currentItem) return;
    if (!state.currentItem.fromEmail) {
      setStatus("contact-status", "Cannot save — the email has no sender address.", "err");
      return;
    }
    setStatus("contact-status", "Saving…");
    disable("contact-submit", true);

    const payload = {
      fromEmail: state.currentItem.fromEmail,
      fromName: state.currentItem.fromName,
      subject: state.currentItem.subject,
      receivedAt: state.currentItem.receivedAt,
      body: state.currentItem.body,
      noteType: $("contact-note-type").value || "Email",
      outlookItemId: state.currentItem.itemId,
    };

    try {
      const r = await apiPost("/contact-email", payload);
      const j = await r.json();
      if (!r.ok) throw new Error(j.error || r.statusText);
      const action = j.contactCreated ? "created new contact" : "matched existing contact";
      setStatus("contact-status", `Saved — ${action} #${j.contactId}.`, "ok");
    } catch (err) {
      setStatus("contact-status", "Failed: " + err.message, "err");
    } finally {
      disable("contact-submit", false);
    }
  }

  // ------------------------------------------------------------------
  // Feature 3 — Legal Report
  // ------------------------------------------------------------------
  function seedLegalDates() {
    const today = new Date();
    const start = new Date(today.getFullYear(), today.getMonth(), 1);
    $("legal-from").value = toIsoDate(start);
    $("legal-to").value = toIsoDate(today);
  }

  async function generateLegalReport() {
    const from = $("legal-from").value;
    const to = $("legal-to").value;
    if (!from || !to) {
      setStatus("legal-status", "Choose both dates.", "err");
      return;
    }
    if (from > to) {
      setStatus("legal-status", "From date must be on or before To date.", "err");
      return;
    }

    const emailsRaw = ($("legal-emails").value || "").trim();
    const emails = emailsRaw
      ? emailsRaw.split(/[\s,]+/).map((s) => s.trim().toLowerCase()).filter(Boolean)
      : [];

    setStatus("legal-status", "Preparing…");
    disable("legal-generate", true);
    showProgress(0, "Fetching messages from Outlook…");

    try {
      const messages = await fetchLatestMessagesPerConversation({ from, to, emails, onProgress: (frac, label) => showProgress(frac, label) });

      if (messages.length === 0) {
        setStatus("legal-status", "No matching emails found in range.", "err");
        hideProgress();
        return;
      }

      showProgress(0.9, "Building PDF…");
      const r = await apiPost("/legal-report", {
        fromDate: from,
        toDate: to,
        emailAddresses: emails,
        messages: messages,
      });

      if (!r.ok) {
        const j = await r.json().catch(() => ({ error: r.statusText }));
        throw new Error(j.error || r.statusText);
      }

      const blob = await r.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `legal-folio-report-${from}_to_${to}.pdf`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);

      setStatus("legal-status", `PDF ready — ${messages.length} email(s), ${sumFolios(messages)} folio(s).`, "ok");
      hideProgress();
    } catch (err) {
      setStatus("legal-status", "Failed: " + err.message, "err");
      hideProgress();
    } finally {
      disable("legal-generate", false);
    }
  }

  function sumFolios(messages) {
    return messages.reduce((acc, m) => acc + Math.max(1, Math.ceil((m.wordCount || 0) / 100)), 0);
  }

  /**
   * Query Outlook REST for messages in the range (sent + inbox), filter by the
   * requested addresses, and reduce each conversation to just its latest message.
   */
  async function fetchLatestMessagesPerConversation({ from, to, emails, onProgress }) {
    const token = await getRestToken();
    const restUrl = Office.context.mailbox.restUrl; // e.g. https://outlook.office.com
    const startIso = new Date(from + "T00:00:00Z").toISOString();
    const endIso = new Date(to + "T23:59:59Z").toISOString();

    // Search across the whole mailbox (Inbox + Sent Items are the important two,
    // but AllItems folder isn't queryable — use /messages which spans all folders).
    const baseFilter = `ReceivedDateTime ge ${startIso} and ReceivedDateTime le ${endIso}`;

    const selected = new Map(); // conversationId -> message

    let nextUrl = `${restUrl}/api/${OUTLOOK_REST_VERSION}/me/messages?$filter=${encodeURIComponent(baseFilter)}&$top=100&$orderby=ReceivedDateTime desc&$select=Id,ConversationId,Subject,From,ToRecipients,CcRecipients,ReceivedDateTime,BodyPreview,HasAttachments,WebLink`;

    let pagesFetched = 0;
    const MAX_PAGES = 30; // 3000 messages cap

    while (nextUrl && pagesFetched < MAX_PAGES) {
      const r = await fetch(nextUrl, {
        headers: {
          Authorization: "Bearer " + token,
          Accept: "application/json",
          Prefer: 'outlook.body-content-type="text"',
        },
      });
      if (!r.ok) throw new Error(`Outlook REST ${r.status}`);
      const page = await r.json();
      for (const msg of page.value || []) {
        if (!matchesAddresses(msg, emails)) continue;
        const existing = selected.get(msg.ConversationId);
        if (!existing || new Date(msg.ReceivedDateTime) > new Date(existing.ReceivedDateTime)) {
          selected.set(msg.ConversationId, msg);
        }
      }
      pagesFetched++;
      onProgress(Math.min(0.7, 0.1 + pagesFetched * 0.05), `Scanning inbox… (${selected.size} conversations)`);
      nextUrl = page["@odata.nextLink"];
    }

    // Now hydrate each selected message with full body + attachment list.
    const messages = [];
    const list = Array.from(selected.values());
    for (let i = 0; i < list.length; i++) {
      const msg = list[i];
      onProgress(0.7 + (0.2 * i) / Math.max(1, list.length), `Reading message ${i + 1} / ${list.length}…`);

      const full = await fetchMessageDetail(restUrl, token, msg.Id);
      const bodyText = stripHtml(full.Body && full.Body.Content ? full.Body.Content : msg.BodyPreview || "");
      const wordCount = countWords(bodyText);

      let attachments = [];
      if (full.HasAttachments) {
        attachments = await fetchAttachments(restUrl, token, msg.Id);
      }

      messages.push({
        id: msg.Id,
        conversationId: msg.ConversationId,
        subject: msg.Subject || "(no subject)",
        receivedAt: msg.ReceivedDateTime,
        fromEmail: (msg.From && msg.From.EmailAddress && msg.From.EmailAddress.Address) || "",
        fromName: (msg.From && msg.From.EmailAddress && msg.From.EmailAddress.Name) || "",
        toRecipients: (msg.ToRecipients || []).map((r) => r.EmailAddress && r.EmailAddress.Address).filter(Boolean),
        wordCount,
        bodyPreview: bodyText.substring(0, 200),
        attachments,
        webLink: msg.WebLink,
      });
    }

    messages.sort((a, b) => new Date(a.receivedAt) - new Date(b.receivedAt));
    return messages;
  }

  async function fetchMessageDetail(restUrl, token, id) {
    const url = `${restUrl}/api/${OUTLOOK_REST_VERSION}/me/messages/${encodeURIComponent(id)}?$select=Body,HasAttachments`;
    const r = await fetch(url, {
      headers: {
        Authorization: "Bearer " + token,
        Accept: "application/json",
        Prefer: 'outlook.body-content-type="text"',
      },
    });
    if (!r.ok) throw new Error(`Message detail ${r.status}`);
    return r.json();
  }

  async function fetchAttachments(restUrl, token, id) {
    const url = `${restUrl}/api/${OUTLOOK_REST_VERSION}/me/messages/${encodeURIComponent(id)}/attachments?$select=Name,Size,ContentType`;
    const r = await fetch(url, {
      headers: { Authorization: "Bearer " + token, Accept: "application/json" },
    });
    if (!r.ok) return [];
    const j = await r.json();
    return (j.value || []).map((a) => ({ name: a.Name, size: a.Size, contentType: a.ContentType }));
  }

  function matchesAddresses(msg, emails) {
    if (!emails || emails.length === 0) return true;
    const participants = new Set();
    if (msg.From && msg.From.EmailAddress && msg.From.EmailAddress.Address) {
      participants.add(msg.From.EmailAddress.Address.toLowerCase());
    }
    for (const r of msg.ToRecipients || []) {
      if (r.EmailAddress && r.EmailAddress.Address) participants.add(r.EmailAddress.Address.toLowerCase());
    }
    for (const r of msg.CcRecipients || []) {
      if (r.EmailAddress && r.EmailAddress.Address) participants.add(r.EmailAddress.Address.toLowerCase());
    }
    return emails.some((e) => participants.has(e));
  }

  function getRestToken() {
    return new Promise((resolve, reject) => {
      Office.context.mailbox.getCallbackTokenAsync({ isRest: true }, (r) => {
        r.status === Office.AsyncResultStatus.Succeeded ? resolve(r.value) : reject(r.error);
      });
    });
  }

  // ------------------------------------------------------------------
  // API helpers
  // ------------------------------------------------------------------
  function apiPost(path, body) {
    return fetch(API_BASE + path, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Api-Key": state.apiKey || "",
      },
      body: JSON.stringify(body),
    });
  }

  // ------------------------------------------------------------------
  // Utilities
  // ------------------------------------------------------------------
  function setStatus(id, msg, cls) {
    const el = $(id);
    el.textContent = msg || "";
    el.classList.remove("ok", "err");
    if (cls) el.classList.add(cls);
  }

  function disable(id, on) {
    $(id).disabled = !!on;
  }

  function showProgress(frac, label) {
    $("legal-progress").classList.remove("hidden");
    $("legal-progress-fill").style.width = Math.round(frac * 100) + "%";
    $("legal-progress-label").textContent = label || "";
  }

  function hideProgress() {
    $("legal-progress").classList.add("hidden");
  }

  function toIsoDate(d) {
    return d.toISOString().slice(0, 10);
  }

  function countWords(text) {
    if (!text) return 0;
    return (text.trim().match(/\S+/g) || []).length;
  }

  function stripHtml(html) {
    if (!html) return "";
    // Remove tags and collapse whitespace. This is fine for word counting; the
    // server does the same with its own strip pass on submitted preview.
    return html
      .replace(/<style[\s\S]*?<\/style>/gi, " ")
      .replace(/<script[\s\S]*?<\/script>/gi, " ")
      .replace(/<[^>]+>/g, " ")
      .replace(/&nbsp;/gi, " ")
      .replace(/&amp;/gi, "&")
      .replace(/&lt;/gi, "<")
      .replace(/&gt;/gi, ">")
      .replace(/&quot;/gi, '"')
      .replace(/&#39;/gi, "'")
      .replace(/\s+/g, " ")
      .trim();
  }

  function escapeHtml(s) {
    if (s == null) return "";
    return String(s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  function showFatal(msg) {
    document.body.innerHTML = `<div style="padding:14px;color:#b91c1c;font-family:sans-serif">${escapeHtml(msg)}</div>`;
  }
})();

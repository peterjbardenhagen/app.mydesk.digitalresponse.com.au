# 41 — Global Search and Ask AI

Status: **PLANNED / PARTIALLY IMPLEMENTED** — Global Search UI exists in header but backend may not be complete. Ask AI is a planned feature with UI elements present.

Cross-cutting search and AI assistant features designed to help users quickly find records and get intelligent assistance.

---

## 1. Global Search

### 1.1 Status: **UI IMPLEMENTED, BACKEND STATUS UNKNOWN**

The Global Search UI is fully implemented in `Header.asp` but the search results page (`GlobalSearch.asp`) status needs verification.

### 1.2 UI Implementation

**Header Integration** (`Header.asp:288`):
```asp
<form action="<%= strWorkingDir %>/GlobalSearch.asp" method="GET" target="_top">
    <input type="text" name="q" id="searchModalInput" 
           placeholder="Search ID, Name, Keyword..." 
           class="tl-input" required>
    <button type="submit" class="tl-btn-primary">Search</button>
</form>
```

**Modal Trigger**: Search icon in header opens modal overlay with search form.

**Features**:
- Modal popup interface
- Search across all modules
- ID, name, and keyword search
- Real-time suggestions (planned)

### 1.3 Expected Search Scope

Global Search would query across:

| Table | Fields Searched |
|---|---|
| Quotes | Qid, Reference, Company, Contact |
| Invoices | InvoiceId, Customer, Reference |
| PurchaseOrders | POid, Project, Supplier |
| JobOrders | JobOrderId, Project, Customer |
| Contacts | Name, Email, Company |
| Companies | Company Name, Customer Code |
| Products | ProductCode, Description |

### 1.4 Expected URL Patterns

```
GlobalSearch.asp?q=<search_term>
    ↓ Returns results page with:
        - Quotes matching
        - Invoices matching
        - POs matching
        - Contacts/Companies matching
        - Grouped by entity type
```

---

## 2. Ask AI

### 2.1 Status: **PLANNED — UI EXISTS, BACKEND NOT IMPLEMENTED**

Ask AI is a planned AI assistant feature for helping users with:
- Natural language queries about data
- Report generation assistance
- System navigation help
- Data analysis questions

### 2.2 UI Implementation

**Header Button** (`Header.asp:198`):
```asp
<a href="#" onclick="openAskAI(); return false;" 
   class="tl-nav-link" 
   style="background: linear-gradient(135deg, #00c8c8 0%, #008b8b 100%); 
          color: white; padding: 6px 14px; border-radius: 8px;">
   <svg ...>AI Icon</svg>
   Ask AI
</a>
```

**JavaScript Handler** (`Header.asp:274`):
```javascript
function openAskAI() {
    window.open('<%= strWorkingDir %>/AskAI.asp', 'AskAI', 
                'width=450,height=600,scrollbars=yes,resizable=yes');
}
```

### 2.3 Expected Features

| Feature | Description |
|---|---|
| Chat Interface | Modal popup chat window |
| Natural Language | "Show me last month's sales" |
| Context Awareness | Knows current user's division/permissions |
| Data Queries | Answers questions about records |
| Navigation Help | "How do I create a purchase order?" |
| Report Assistance | "Generate a Q3 sales report" |

### 2.4 Technical Architecture (Expected)

```
User Input → AskAI.asp → 
    Option A: OpenAI/Claude API integration
    Option B: Local LLM endpoint
    Option C: Rule-based response system
↓
Response with:
    - Natural language answer
    - Relevant data snippets
    - Navigation links
    - Suggested actions
```

---

## 3. Implementation Requirements

### 3.1 Global Search Completion

To fully implement Global Search:

1. **Create GlobalSearch.asp**:
   - Accept `q` parameter
   - Query all relevant tables
   - Return grouped results
   - Respect user division permissions

2. **Search Algorithm**:
   ```sql
   -- Example multi-table search
   SELECT 'Quote' AS Type, Qid AS Id, 
          Reference AS Title, Company AS Detail
   FROM Quotes WHERE Reference LIKE '%{q}%'
   
   UNION ALL
   
   SELECT 'Invoice' AS Type, InvoiceId AS Id,
          InvoiceNum AS Title, Company AS Detail
   FROM Invoices WHERE InvoiceNum LIKE '%{q}%'
   
   -- etc. for other tables
   ```

3. **Results Page**:
   - Group by entity type
   - Show count per type
   - Link to view each record
   - Pagination for large result sets

### 3.2 Ask AI Completion

To implement Ask AI:

1. **Create AskAI.asp**:
   - Chat interface UI
   - Message history
   - Input handling

2. **Backend Integration**:
   - OpenAI API or Azure OpenAI
   - System prompt with schema context
   - Function calling for data queries
   - Response formatting

3. **Security**:
   - Rate limiting
   - Data access validation
   - No sensitive data in prompts

---

## 4. Known Baseline Issues

### Global Search
1. **Backend Missing**: `GlobalSearch.asp` referenced but may not exist.
2. **No Autocomplete**: Search suggestions not implemented.
3. **No Advanced Search**: Cannot filter by date, division, etc.
4. **Permission Handling**: Must ensure users only see their accessible records.

### Ask AI
1. **Not Implemented**: Entire backend missing.
2. **Hardcoded Window Size**: 450x600 may not suit all screens.
3. **No Fallback**: If AI service unavailable, no alternative provided.

---

## 5. UI Design Notes

### Global Search Modal
- Centered modal overlay
- Large search input with prominent button
- Clean, distraction-free interface
- Closes on ESC key or outside click

### Ask AI Button
- Distinctive teal gradient styling
- Positioned in main navigation
- Hover effects for engagement
- Popup window (not modal) for extended conversation

---

## 6. Related Modules

- **03-Navigation-Header.md** — Search and AI UI integrated here
- **All Modules** — Would be searchable via Global Search

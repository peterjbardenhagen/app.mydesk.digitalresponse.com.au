# MyDesk UI Design Standards

This document defines consistent UI/UX patterns and component standards across the MyDesk platform.

---

## Popup Dialogs

All popup dialogs in MyDesk should follow the pattern established in `QuickNavDialog.razor` and standardized in the `Bulk*Dialog` components. This ensures consistency, usability, and accessibility across the platform.

### Dialog Structure

Every dialog follows a three-part structure:

1. **Header Section** — Title + subtitle (contextual information)
2. **Body Section** — Form inputs, content, or main interaction area
3. **Action Footer** — Cancel and primary action buttons

### Anatomy

```razor
<MudDialog Class="tl-dialog">
    <DialogContent>
        <!-- HEADER: Title and subtitle -->
        <div class="tl-dialog-header">
            <MudText Typo="Typo.h6">Primary Action (verb-based)</MudText>
            <MudText Typo="Typo.body2" Class="tl-dialog-subtitle">
                Context: What will happen and to how many items.
            </MudText>
        </div>

        <!-- BODY: Inputs or content -->
        <div class="tl-dialog-body">
            <MudTextField @bind-Value="_field1" Label="Field Name" Variant="Variant.Outlined" FullWidth />
            <MudSelect T="int" @bind-Value="_field2" Label="Select Option" Variant="Variant.Outlined" FullWidth>
                <MudSelectItem T="int" Value="0">— Select an option —</MudSelectItem>
                <MudSelectItem T="int" Value="1">Option 1</MudSelectItem>
            </MudSelect>
        </div>
    </DialogContent>

    <!-- ACTION FOOTER: Buttons -->
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" OnClick="Execute" Disabled="@(!IsValid)">
            Primary Action
        </MudButton>
    </DialogActions>
</MudDialog>

<style>
    .tl-dialog .mud-dialog { border-radius: 12px; }
    
    .tl-dialog-header {
        padding-bottom: 16px;
        border-bottom: 1px solid var(--gray-200);
        margin-bottom: 16px;
    }
    
    .tl-dialog-header :deep(.mud-typography-h6) {
        margin: 0;
        color: var(--gray-900);
        font-weight: 700;
    }
    
    .tl-dialog-subtitle {
        margin-top: 4px;
        color: var(--gray-600);
    }
    
    .tl-dialog-body {
        padding: 16px 0;
    }
</style>
```

### Key Rules

#### 1. **Dialog Title**
- Use the **primary action verb** (not object).
- ✅ **Good:** "Update Status", "Send Emails", "Delete Invoices"
- ❌ **Bad:** "Bulk Status Update", "Bulk Email Dialog", "Invoice Actions"
- **Reasoning:** Users care about what they'll DO, not what the component is called.

#### 2. **Subtitle**
- Provide context: what will happen and how many items are affected.
- **Format:** "Action description for N selected item(s)."
- **Examples:**
  - "Change status for 5 selected invoices."
  - "Send to customers for 3 selected invoices."

#### 3. **Default Select Items**
- Always provide a non-action placeholder when the field is required.
- Use consistent phrasing: "— Select an option —" or "— Select a status —"
- **Value:** Always `0` (reserved, never a valid submission value)
- **Reasoning:** Prevents accidental submission; makes intent explicit.

#### 4. **Layout**
- **No padding inside `<DialogContent>`** — use `.tl-dialog-header` and `.tl-dialog-body` divs to structure space.
- Header: bottom border, medium spacing
- Body: inputs, content, forms
- Footer: Cancel button first (safe), primary action second (destructive or affirming color)

#### 5. **Styling (CSS Class Pattern)**
- Use `.tl-dialog` on the `<MudDialog>` to apply base styles.
- Use `.tl-dialog-header`, `.tl-dialog-subtitle`, `.tl-dialog-body` for internal layout.
- All styles are scoped to the component (use `<style>` block, not global).

#### 6. **Dark Mode**
- MudBlazor's dark mode is automatically applied via the `mud-dark-mode` class on the body.
- Test dialogs in both light and dark modes.
- Use CSS variables (`--gray-*`, `--mud-palette-*`) so theming is automatic.

#### 7. **Accessibility & Keyboard Support**
- Autofocus the primary input on first render (optional, use `@ref` and `.FocusAsync()`).
- Support `Escape` key to cancel (MudDialog handles this automatically).
- Use semantic HTML labels on form fields (MudTextField/MudSelect do this).
- Disabled state on primary button should reflect validation state.

#### 8. **Validation & Button State**
- Primary button **must be disabled** until the user provides valid input.
- **Example:**
  ```csharp
  <MudButton Color="Color.Primary" OnClick="Execute" Disabled="@(_statusId == 0)">
      Update Status
  </MudButton>
  ```
- Show snackbar feedback (success or error) after dialog closes.

---

## Examples

### Example 1: Bulk Status Update

**Component:** `BulkStatusDialog.razor`

**Title:** "Update Status"  
**Subtitle:** "Change status for 5 selected invoices."  
**Input:** Dropdown with "— Select a status —" as placeholder (value 0)  
**Action:** "Update Status" button (disabled until selection ≠ 0)

### Example 2: Bulk Email

**Component:** `BulkEmailDialog.razor`

**Title:** "Send Emails"  
**Subtitle:** "Send to customers for 3 selected invoices."  
**Inputs:**
- Subject field (required)
- Message textarea (optional)

**Action:** "Send Emails" button (disabled if subject is empty)

### Example 3: Quick Navigation

**Component:** `QuickNavDialog.razor`

**Structure:**
- Search input at top (styled as header, no title)
- Filtered list in body
- Keyboard hints in footer

**Pattern:** Different use case (command palette), but still follows the three-part structure (search/result list/footer).

---

## Common Mistakes to Avoid

| ❌ Anti-pattern | ✅ Correct Pattern | Why |
|---|---|---|
| Title: "Bulk Status Update Dialog" | Title: "Update Status" | Users care about the action, not the component name |
| Two headers (h6 + h6 in title area) | One h6 title + body2 subtitle | Cleaner visual hierarchy; single decision point |
| Select with value 0 labeled "0" | Select with value 0 labeled "— Select a status —" | Clearer intent; less confusion for users |
| No subtitle context | "Change status for N selected items" | Users need to know scope of action |
| Primary button always enabled | Primary button disabled until valid input | Prevents accidental incomplete submissions |
| Dialog padding on `<DialogContent>` | Explicit divs (header/body/actions) for padding | Easier to maintain consistent spacing |
| Hardcoded strings in dialog | Bound values from parameters or component state | Reusable across different entity types |
| No keyboard support | Autofocus + Escape + Enter support | Better UX for power users |

---

## Dialog Options (MudDialog Configuration)

When opening dialogs, use these standard options:

```csharp
var options = new DialogOptions
{
    CloseButton = false,          // User must click Cancel/Action, not X
    CloseOnEscapeKey = true,      // Escape closes the dialog (safe)
    BackdropClick = true,         // Click outside closes (safe)
    NoHeader = true,              // Dialog provides its own header
    MaxWidth = MaxWidth.Small,    // Responsive width
    FullWidth = true              // Stretch to screen width (up to MaxWidth)
};

await DialogService.ShowAsync<YourDialog>(title, parameters, options);
```

- **`CloseButton = false`** — Remove the X button; dialog controls its own closure via Cancel/Action buttons.
- **`CloseOnEscapeKey = true`** — Always allow Escape to cancel.
- **`BackdropClick = true`** — Allow clicking outside to cancel (UX convention).
- **`NoHeader = true`** — Dialog content provides its own header structure; don't add a duplicate.
- **`MaxWidth = MaxWidth.Small`** — Standard for data entry/action dialogs (not full-width).
- **`FullWidth = true`** — Responsive on mobile; stretches to available width (up to MaxWidth).

---

## Links & Buttons

### Standard Links (`.tl-link`)

For calls-to-action in text (not form buttons):

```razor
<p>
    Manage company and personal contacts. 
    <a href="/companies" class="tl-link">View Companies →</a>
</p>
```

**Styling:**
- Color: `--gray-700` (navy)
- Hover: Underline appears, color darkens to `--gray-900`
- Transition: 150ms ease
- No button styling (looks like text link, not a button)

---

## Color & Typography Reference

| Use Case | Class/Variable | Notes |
|----------|---|---|
| Dialog title | `.mud-typography-h6` (`Typo.h6`) | 18px, font-weight 600, gray-900 |
| Dialog subtitle | `.mud-typography-body2` (`Typo.body2`) + `.tl-dialog-subtitle` | 14px, gray-600 |
| Body text | `.mud-typography-body1` or `.mud-typography-body2` | Use body2 for descriptions |
| Muted text | `.tl-muted` | gray-500, use for hints/helpers |
| Primary action button | `Color="Color.Primary"` | Teal (--tl-primary) |
| Danger/Delete action | `Color="Color.Error"` | Red, use sparingly |
| Secondary action | `Variant="Variant.Outlined"` | No fill, no emphasis |

---

## Form Field Patterns

### Required vs. Optional

- Always make field labels clear about requirements.
- Use `Variant.Outlined` for all inputs (consistent with design system).
- Label text should describe the field, not the action.

### Disabled States

- Disable the primary action button until the form is valid.
- Do NOT disable inputs mid-form (confuses users).
- Show error messages inline under fields if validation fails.

### Validation

- Show errors in real-time or on blur (MudTextField supports this).
- Use `HelperText` to provide guidance.
- Keep validation messages short and actionable.

---

## Last Updated
May 2026 — Established standard dialog patterns for Bulk actions and general pop-ups.

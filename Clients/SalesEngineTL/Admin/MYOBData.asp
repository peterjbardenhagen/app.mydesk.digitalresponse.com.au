<% 
' Techlight MyDesk - MYOB Data Export (Director Only)
Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

' Security check - only Directors can access
Dim userTypeId
userTypeId = Request.Cookies("UserSettings")("UserTypeID") & ""
If userTypeId <> "1" Then
    Response.Redirect("../Portal/AccessDenied.asp")
End If

' Default date range (current month)
dteDateFrom = FormatDateU(DateSerial(Year(Date()), Month(Date()), 1), False)
dteDateTo = FormatDateU(DateAdd("D", -1, DateAdd("M", 1, DateSerial(Year(Date()), Month(Date()), 1))), False)
%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MYOB Data Export - Techlight MyDesk</title>
    <meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
    <meta http-equiv="Expires" content="0">
    <meta http-equiv="Pragma" content="no-store">
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
    <link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Style_Techlight.css">
    <script language="javascript" src="/System/cal2.js"></script>
    <script language="javascript" src="/System/cal_conf2.js"></script>
</head>
<body>
<!--#include virtual="/System/ssi_Header.inc"-->

<div class="tl-page-container">
    <!-- Breadcrumb -->
    <nav class="tl-breadcrumb">
        <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Dashboard.asp" target="_top">Home</a>
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
        <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Admin/" target="_top">Admin</a>
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
        <span>MYOB Data</span>
    </nav>

    <!-- Page Header -->
    <div class="tl-action-bar">
        <h1 class="tl-page-title">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                <polyline points="14 2 14 8 20 8"></polyline>
                <line x1="16" y1="13" x2="8" y2="13"></line>
                <line x1="16" y1="17" x2="8" y2="17"></line>
            </svg>
            MYOB Data Export
        </h1>
        <div class="tl-btn-group">
            <span class="tl-badge tl-badge-info">Director Access Only</span>
        </div>
    </div>

    <!-- Export Options Panel -->
    <div class="tl-panel">
        <h2 style="font-size: 16px; font-weight: 600; color: var(--tl-dark); margin-bottom: 20px;">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px; vertical-align: middle; margin-right: 8px;">
                <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                <line x1="16" y1="8" x2="8" y2="8"></line>
                <line x1="16" y1="12" x2="8" y2="12"></line>
                <line x1="16" y1="16" x2="8" y2="16"></line>
            </svg>
            Select Data to Export
        </h2>

        <form name="frmMYOB" id="frmMYOB" method="post" action="MYOBData_Export.asp" target="_blank">
            
            <!-- Date Range -->
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 24px;">
                <div class="tl-form-group">
                    <label class="tl-form-label">Date From</label>
                    <div style="display: flex; gap: 8px;">
                        <input type="text" name="DateFrom" value="<%= dteDateFrom %>" class="tl-form-input" readonly style="flex: 1;">
                        <a href="javascript:showCal('Calendar1')" class="tl-icon-btn" title="Select Date">
                            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
                                <line x1="16" y1="2" x2="16" y2="6"></line>
                                <line x1="8" y1="2" x2="8" y2="6"></line>
                                <line x1="3" y1="10" x2="21" y2="10"></line>
                            </svg>
                        </a>
                    </div>
                </div>
                <div class="tl-form-group">
                    <label class="tl-form-label">Date To</label>
                    <div style="display: flex; gap: 8px;">
                        <input type="text" name="DateTo" value="<%= dteDateTo %>" class="tl-form-input" readonly style="flex: 1;">
                        <a href="javascript:showCal('Calendar2')" class="tl-icon-btn" title="Select Date">
                            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
                                <line x1="16" y1="2" x2="16" y2="6"></line>
                                <line x1="8" y1="2" x2="8" y2="6"></line>
                                <line x1="3" y1="10" x2="21" y2="10"></line>
                            </svg>
                        </a>
                    </div>
                </div>
                <div class="tl-form-group">
                    <label class="tl-form-label">Division</label>
                    <select name="DivisionId" class="tl-form-select">
                        <option value="0">All Divisions</option>
                        <option value="1">Techlight</option>
                        <option value="2">SalesEngine</option>
                    </select>
                </div>
            </div>

            <!-- Export Options -->
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 16px; margin-bottom: 24px;">
                
                <!-- Invoices -->
                <div style="border: 2px solid var(--tl-border); border-radius: 10px; padding: 16px; background: #f8fafc;">
                    <label style="display: flex; align-items: center; gap: 10px; cursor: pointer; font-weight: 600; color: var(--tl-dark); margin-bottom: 8px;">
                        <input type="checkbox" name="ExportInvoices" value="1" checked style="width: 18px; height: 18px; accent-color: var(--tl-primary);">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px; color: var(--tl-primary);">
                            <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                            <line x1="3" y1="9" x2="21" y2="9"></line>
                            <line x1="9" y1="21" x2="9" y2="9"></line>
                        </svg>
                        Sales Invoices
                    </label>
                    <p style="font-size: 13px; color: var(--tl-text-light); margin-left: 28px;">
                        Invoice numbers, dates, customer details, amounts, GST breakdown
                    </p>
                </div>

                <!-- Purchase Orders -->
                <div style="border: 2px solid var(--tl-border); border-radius: 10px; padding: 16px; background: #f8fafc;">
                    <label style="display: flex; align-items: center; gap: 10px; cursor: pointer; font-weight: 600; color: var(--tl-dark); margin-bottom: 8px;">
                        <input type="checkbox" name="ExportPurchaseOrders" value="1" style="width: 18px; height: 18px; accent-color: var(--tl-primary);">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px; color: var(--tl-primary);">
                            <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
                            <line x1="3" y1="6" x2="21" y2="6"></line>
                        </svg>
                        Purchase Orders
                    </label>
                    <p style="font-size: 13px; color: var(--tl-text-light); margin-left: 28px;">
                        PO numbers, supplier details, costs, expenses for accounts payable
                    </p>
                </div>

                <!-- Payments -->
                <div style="border: 2px solid var(--tl-border); border-radius: 10px; padding: 16px; background: #f8fafc;">
                    <label style="display: flex; align-items: center; gap: 10px; cursor: pointer; font-weight: 600; color: var(--tl-dark); margin-bottom: 8px;">
                        <input type="checkbox" name="ExportPayments" value="1" style="width: 18px; height: 18px; accent-color: var(--tl-primary);">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px; color: var(--tl-primary);">
                            <rect x="1" y="4" width="22" height="16" rx="2" ry="2"></rect>
                            <line x1="1" y1="10" x2="23" y2="10"></line>
                        </svg>
                        Payment Receipts
                    </label>
                    <p style="font-size: 13px; color: var(--tl-text-light); margin-left: 28px;">
                        Customer payments, bank deposits, reconciliation data
                    </p>
                </div>

                <!-- General Ledger -->
                <div style="border: 2px solid var(--tl-border); border-radius: 10px; padding: 16px; background: #f8fafc;">
                    <label style="display: flex; align-items: center; gap: 10px; cursor: pointer; font-weight: 600; color: var(--tl-dark); margin-bottom: 8px;">
                        <input type="checkbox" name="ExportGL" value="1" style="width: 18px; height: 18px; accent-color: var(--tl-primary);">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px; color: var(--tl-primary);">
                            <line x1="8" y1="6" x2="21" y2="6"></line>
                            <line x1="8" y1="12" x2="21" y2="12"></line>
                            <line x1="8" y1="18" x2="21" y2="18"></line>
                            <line x1="3" y1="6" x2="3.01" y2="6"></line>
                            <line x1="3" y1="12" x2="3.01" y2="12"></line>
                            <line x1="3" y1="18" x2="3.01" y2="18"></line>
                        </svg>
                        General Ledger
                    </label>
                    <p style="font-size: 13px; color: var(--tl-text-light); margin-left: 28px;">
                        Journal entries, account codes, debits and credits
                    </p>
                </div>

            </div>

            <!-- Export Format -->
            <div style="display: flex; align-items: center; gap: 16px; margin-bottom: 24px; padding: 16px; background: #f0f4f8; border-radius: 8px;">
                <span style="font-weight: 600; color: var(--tl-dark);">Export Format:</span>
                <label style="display: flex; align-items: center; gap: 6px; cursor: pointer;">
                    <input type="radio" name="ExportFormat" value="CSV" checked style="accent-color: var(--tl-primary);">
                    <span>CSV (MYOB Import)</span>
                </label>
                <label style="display: flex; align-items: center; gap: 6px; cursor: pointer;">
                    <input type="radio" name="ExportFormat" value="Excel" style="accent-color: var(--tl-primary);">
                    <span>Excel (.xlsx)</span>
                </label>
                <label style="display: flex; align-items: center; gap: 6px; cursor: pointer;">
                    <input type="radio" name="ExportFormat" value="TXT" style="accent-color: var(--tl-primary);">
                    <span>Tab-Delimited (.txt)</span>
                </label>
            </div>

            <!-- Action Buttons -->
            <div style="display: flex; gap: 12px; justify-content: flex-end;">
                <button type="button" onclick="location.href='Default.asp'" class="tl-btn-secondary">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 16px; height: 16px; display: inline-block; vertical-align: middle; margin-right: 6px;">
                        <line x1="19" y1="12" x2="5" y2="12"></line>
                        <polyline points="12 19 5 12 12 5"></polyline>
                    </svg>
                    Back to Admin
                </button>
                <button type="button" onclick="previewData()" class="tl-btn-secondary">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 16px; height: 16px; display: inline-block; vertical-align: middle; margin-right: 6px;">
                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                        <circle cx="12" cy="12" r="3"></circle>
                    </svg>
                    Preview Data
                </button>
                <button type="submit" class="tl-btn-primary">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 16px; height: 16px; display: inline-block; vertical-align: middle; margin-right: 6px;">
                        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                        <polyline points="7 10 12 15 17 10"></polyline>
                        <line x1="12" y1="15" x2="12" y2="3"></line>
                    </svg>
                    Download MYOB Export
                </button>
            </div>

        </form>
    </div>

    <!-- Instructions Panel -->
    <div class="tl-panel" style="margin-top: 20px; background: linear-gradient(135deg, #e6f7f9 0%, #f0fdfa 100%); border-left: 4px solid var(--tl-primary);">
        <h3 style="font-size: 15px; font-weight: 600; color: var(--tl-dark); margin-bottom: 12px;">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px; vertical-align: middle; margin-right: 6px;">
                <circle cx="12" cy="12" r="10"></circle>
                <line x1="12" y1="16" x2="12" y2="12"></line>
                <line x1="12" y1="8" x2="12.01" y2="8"></line>
            </svg>
            MYOB Import Instructions
        </h3>
        <ol style="margin-left: 20px; font-size: 13px; color: var(--tl-text); line-height: 1.8;">
            <li>Select the date range for the period you want to import into MYOB</li>
            <li>Choose the data types needed (usually Invoices for sales data)</li>
            <li>Download as CSV format for MYOB AccountRight</li>
            <li>In MYOB: File → Import/Export Assistant → Import Data</li>
            <li>Select the appropriate import type (Sales / Purchases / General Journal)</li>
            <li>Match the columns and complete the import</li>
        </ol>
        <p style="margin-top: 12px; font-size: 12px; color: var(--tl-text-light);">
            <strong>Tip:</strong> For multi-currency transactions, ensure your MYOB company file has the same currency settings.
        </p>
    </div>

</div>

<script>
function previewData() {
    var form = document.frmMYOB;
    var hasSelection = form.ExportInvoices.checked || 
                       form.ExportPurchaseOrders.checked || 
                       form.ExportPayments.checked || 
                       form.ExportGL.checked;
    
    if (!hasSelection) {
        alert('Please select at least one data type to preview.');
        return;
    }
    
    form.action = 'MYOBData_Preview.asp';
    form.target = '_blank';
    form.submit();
    
    // Reset form action
    form.action = 'MYOBData_Export.asp';
    form.target = '_blank';
}
</script>

</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->

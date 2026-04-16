<% 
' Techlight MyDesk - Admin Dashboard (Director Only)
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
%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Admin - Techlight MyDesk</title>
    <meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
    <meta http-equiv="Expires" content="0">
    <meta http-equiv="Pragma" content="no-store">
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
    <link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Style_Techlight.css">
</head>
<body>
<!--#include virtual="/System/ssi_Header.inc"-->

<div class="tl-page-container">
    <!-- Breadcrumb -->
    <nav class="tl-breadcrumb">
        <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Dashboard.asp" target="_top">Home</a>
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
        <span>Admin</span>
    </nav>

    <!-- Page Header -->
    <div class="tl-action-bar">
        <h1 class="tl-page-title">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"></path>
            </svg>
            Admin
        </h1>
        <div class="tl-btn-group">
            <span class="tl-badge tl-badge-info">Director Access Only</span>
        </div>
    </div>

    <!-- Welcome Message -->
    <div class="tl-welcome" style="margin-bottom: 24px;">
        <h2 style="font-size: 18px; font-weight: 600; color: var(--tl-dark); margin-bottom: 8px;">MYOB Data & Administration</h2>
        <p style="color: var(--tl-text-light); font-size: 14px;">Access financial data exports, reports, and administrative tools required for MYOB integration and business analytics.</p>
    </div>

    <!-- Admin Modules Grid -->
    <div class="tl-modules-grid">
        
        <!-- MYOB Data Export -->
        <a href="MYOBData.asp" class="tl-module-card">
            <div class="tl-module-icon" style="background: linear-gradient(135deg, #00a8b5 0%, #00c4d3 100%);">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                    <polyline points="14 2 14 8 20 8"></polyline>
                    <line x1="16" y1="13" x2="8" y2="13"></line>
                    <line x1="16" y1="17" x2="8" y2="17"></line>
                    <polyline points="10 9 9 9 8 9"></polyline>
                </svg>
            </div>
            <div class="tl-module-content">
                <h3 class="tl-module-title">MYOB Data Export</h3>
                <p class="tl-module-desc">Export invoices, payments, and financial data for MYOB integration. Generate CSV and Excel files.</p>
                <span class="tl-module-arrow">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <line x1="5" y1="12" x2="19" y2="12"></line>
                        <polyline points="12 5 19 12 12 19"></polyline>
                    </svg>
                </span>
            </div>
        </a>

        <!-- Financial Reports -->
        <a href="FinancialReports.asp" class="tl-module-card">
            <div class="tl-module-icon" style="background: linear-gradient(135deg, #d4a574 0%, #e8c088 100%);">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect>
                    <line x1="8" y1="21" x2="16" y2="21"></line>
                    <line x1="12" y1="17" x2="12" y2="21"></line>
                    <path d="M8 13l2-2 2 2 4-4"></path>
                </svg>
            </div>
            <div class="tl-module-content">
                <h3 class="tl-module-title">Financial Reports</h3>
                <p class="tl-module-desc">View P&L reports, balance summaries, revenue analytics, and custom date range financial analysis.</p>
                <span class="tl-module-arrow">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <line x1="5" y1="12" x2="19" y2="12"></line>
                        <polyline points="12 5 19 12 12 19"></polyline>
                    </svg>
                </span>
            </div>
        </a>

        <!-- Sales Analytics -->
        <a href="SalesAnalytics.asp" class="tl-module-card">
            <div class="tl-module-icon" style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="18" y1="20" x2="18" y2="10"></line>
                    <line x1="12" y1="20" x2="12" y2="4"></line>
                    <line x1="6" y1="20" x2="6" y2="14"></line>
                    <path d="M3 20h18"></path>
                </svg>
            </div>
            <div class="tl-module-content">
                <h3 class="tl-module-title">Sales Analytics</h3>
                <p class="tl-module-desc">Sales performance by user, product, and customer. Commission calculations and revenue trends.</p>
                <span class="tl-module-arrow">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <line x1="5" y1="12" x2="19" y2="12"></line>
                        <polyline points="12 5 19 12 12 19"></polyline>
                    </svg>
                </span>
            </div>
        </a>

        <!-- Audit Trail -->
        <a href="AuditTrail.asp" class="tl-module-card">
            <div class="tl-module-icon" style="background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="12" cy="12" r="10"></circle>
                    <polyline points="12 6 12 12 16 14"></polyline>
                </svg>
            </div>
            <div class="tl-module-content">
                <h3 class="tl-module-title">Audit Trail</h3>
                <p class="tl-module-desc">System activity logs, quote status changes, invoice modifications, and user action history.</p>
                <span class="tl-module-arrow">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <line x1="5" y1="12" x2="19" y2="12"></line>
                        <polyline points="12 5 19 12 12 19"></polyline>
                    </svg>
                </span>
            </div>
        </a>

        <!-- Database Management -->
        <a href="DatabaseTools.asp" class="tl-module-card">
            <div class="tl-module-icon" style="background: linear-gradient(135deg, #fc466b 0%, #3f5efb 100%);">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <ellipse cx="12" cy="5" rx="9" ry="3"></ellipse>
                    <path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"></path>
                    <path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"></path>
                </svg>
            </div>
            <div class="tl-module-content">
                <h3 class="tl-module-title">Database Tools</h3>
                <p class="tl-module-desc">Data cleanup, archive old records, backup management, and advanced query tools.</p>
                <span class="tl-module-arrow">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <line x1="5" y1="12" x2="19" y2="12"></line>
                        <polyline points="12 5 19 12 12 19"></polyline>
                    </svg>
                </span>
            </div>
        </a>

        <!-- System Settings -->
        <a href="SystemSettings.asp" class="tl-module-card">
            <div class="tl-module-icon" style="background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="12" cy="12" r="3"></circle>
                    <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path>
                </svg>
            </div>
            <div class="tl-module-content">
                <h3 class="tl-module-title">System Settings</h3>
                <p class="tl-module-desc">Application configuration, email settings, division management, and global preferences.</p>
                <span class="tl-module-arrow">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <line x1="5" y1="12" x2="19" y2="12"></line>
                        <polyline points="12 5 19 12 12 19"></polyline>
                    </svg>
                </span>
            </div>
        </a>

    </div>
</div>

</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->

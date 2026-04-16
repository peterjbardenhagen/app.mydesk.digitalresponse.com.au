<%@Language="VBSCRIPT"%>
<!--#include virtual="/System/ssi_Logging.asp"-->
<%
  'Option Explicit
  On Error Resume Next

  Dim objError
  Set objError = Server.GetLastError()

  ' Check if there's actually an error
  Dim hasError
  hasError = False

  If Not objError Is Nothing Then
    If objError.Number <> 0 Then
      hasError = True
    End If
  End If

  ' If no error, redirect to Dashboard instead of showing error page
  If Not hasError Then
    On Error Resume Next
    Dim workingDir
    workingDir = "/Clients/SalesEngineTL"
    If Not Request.Cookies("ClientSettings") Is Nothing Then
      If Not IsEmpty(Request.Cookies("ClientSettings")("WorkingDir")) And Request.Cookies("ClientSettings")("WorkingDir") <> "" Then
        workingDir = Request.Cookies("ClientSettings")("WorkingDir")
      End If
    End If
    Response.Redirect workingDir & "/Dashboard.asp"
    Response.End
  End If

  ' Capture request details for debugging
  Dim strRequestURL, strQueryString, strFormData, strSessionVars, strCookies
  strRequestURL = Request.ServerVariables("URL")
  strQueryString = Request.QueryString

  ' Log the error to file
  Call LogASPError(objError)

  ' Clear any existing response to ensure clean error page display
  Response.Clear
  Response.Status = "500 Internal Server Error"
  Response.ContentType = "text/html"
%>
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Application Error - Techlight MyDesk</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

.error-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: linear-gradient(135deg, #0a1929 0%, #1a2332 100%);
  z-index: 99999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  font-family: 'Inter', Arial, sans-serif;
}

.error-container {
  background: white;
  border-radius: 16px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
  max-width: 900px;
  max-height: 90vh;
  overflow-y: auto;
  width: 100%;
  animation: slideIn 0.3s ease-out;
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.error-header {
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  padding: 24px 32px;
  border-radius: 16px 16px 0 0;
  color: white;
}

.error-header h1 {
  font-size: 24px;
  font-weight: 700;
  margin-bottom: 8px;
  display: flex;
  align-items: center;
  gap: 12px;
}

.error-header h1 svg {
  width: 32px;
  height: 32px;
}

.error-header p {
  font-size: 14px;
  opacity: 0.9;
}

.error-content {
  padding: 32px;
}

.error-section {
  margin-bottom: 24px;
}

.error-section-title {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 12px;
  display: flex;
  align-items: center;
  gap: 8px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.error-section-title svg {
  width: 16px;
  height: 16px;
  color: #ef4444;
}

.error-box {
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-left: 4px solid #ef4444;
  padding: 16px;
  border-radius: 8px;
  margin-bottom: 16px;
}

.error-box strong {
  color: #991b1b;
  font-weight: 600;
}

.error-box .code {
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 12px;
  background: white;
  padding: 2px 6px;
  border-radius: 4px;
  color: #dc2626;
  border: 1px solid #fecaca;
}

.error-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.error-table th {
  background: #f9fafb;
  color: #374151;
  font-weight: 600;
  padding: 12px 16px;
  text-align: left;
  border-bottom: 2px solid #e5e7eb;
  width: 180px;
}

.error-table td {
  padding: 12px 16px;
  border-bottom: 1px solid #e5e7eb;
  color: #374151;
}

.error-table tr:last-child td {
  border-bottom: none;
}

.error-table .code {
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 12px;
  background: #f3f4f6;
  padding: 4px 8px;
  border-radius: 4px;
  color: #dc2626;
}

.error-actions {
  display: flex;
  gap: 12px;
  padding-top: 24px;
  border-top: 1px solid #e5e7eb;
  margin-top: 24px;
}

.error-btn {
  padding: 10px 20px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s ease;
  cursor: pointer;
  border: none;
}

.error-btn-primary {
  background: #ef4444;
  color: white;
}

.error-btn-primary:hover {
  background: #dc2626;
  transform: translateY(-1px);
}

.error-btn-secondary {
  background: #f3f4f6;
  color: #374151;
}

.error-btn-secondary:hover {
  background: #e5e7eb;
}

.error-btn svg {
  width: 16px;
  height: 16px;
}

.debug-section {
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 16px;
}

.debug-section h3 {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
  margin-bottom: 12px;
}

.debug-section table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.debug-section th {
  background: #e5e7eb;
  color: #374151;
  font-weight: 600;
  padding: 10px 12px;
  text-align: left;
  width: 180px;
  border-radius: 4px 4px 0 0;
}

.debug-section td {
  padding: 10px 12px;
  border-bottom: 1px solid #e5e7eb;
  color: #374151;
}

.debug-section tr:last-child td {
  border-bottom: none;
}

/* Scrollbar styling */
.error-container::-webkit-scrollbar {
  width: 8px;
}

.error-container::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 0 0 16px 0;
}

.error-container::-webkit-scrollbar-thumb {
  background: #c1c1c1;
  border-radius: 4px;
}

.error-container::-webkit-scrollbar-thumb:hover {
  background: #a1a1a1;
}
</style>
</head>
<body>

<div class="error-overlay">
  <div class="error-container">
    <div class="error-header">
      <h1>
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="12" cy="12" r="10"></circle>
          <line x1="12" y1="8" x2="12" y2="12"></line>
          <line x1="12" y1="16" x2="12.01" y2="16"></line>
        </svg>
        Application Error
      </h1>
      <p>An unexpected error occurred while processing your request</p>
    </div>

    <div class="error-content">
      <div class="error-box">
        <strong>Error Location:</strong> <span class="code"><%=strRequestURL%></span><br>
        <% If strQueryString <> "" Then %>
        <strong>Query String:</strong> <span class="code"><%=strQueryString%></span>
        <% End If %>
      </div>

      <div class="error-section">
        <div class="error-section-title">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="16" x2="12" y2="12"></line>
            <line x1="12" y1="8" x2="12.01" y2="8"></line>
          </svg>
          Error Details
        </div>
        <table class="error-table">
          <tr>
            <th>Property</th>
            <th>Value</th>
          </tr>
          
          <% If objError.Number <> 0 Then %>
          <tr>
            <td><strong>Error Number</strong></td>
            <td class="code"><%=objError.Number%> (0x<%=Hex(objError.Number)%>)</td>
          </tr>
          <% End If %>
          
          <% If objError.Source <> "" Then %>
          <tr>
            <td><strong>Error Source</strong></td>
            <td><%=objError.Source%></td>
          </tr>
          <% End If %>
          
          <% If objError.File <> "" Then %>
          <tr>
            <td><strong>File Name</strong></td>
            <td class="code"><%=objError.File%></td>
          </tr>
          <% End If %>
          
          <% If objError.Line > 0 Then %>
          <tr>
            <td><strong>Line Number</strong></td>
            <td class="code"><%=objError.Line%></td>
          </tr>
          <% End If %>
          
          <% If objError.Description <> "" Then %>
          <tr>
            <td><strong>Description</strong></td>
            <td><%=objError.Description%></td>
          </tr>
          <% End If %>
          
          <% If objError.ASPDescription <> "" Then %>
          <tr>
            <td><strong>ASP Description</strong></td>
            <td><%=objError.ASPDescription%></td>
          </tr>
          <% End If %>
          
          <% If objError.Number = 0 And objError.Description = "" Then %>
          <tr>
            <td colspan="2" style="color: #dc2626;">
              <strong>No detailed error information available.</strong><br>
              This may be a connection error, missing file, or IIS configuration issue.<br>
              Check that all include files exist and paths are correct.
            </td>
          </tr>
          <% End If %>
          
          <% ' Also check for Err object which may have different info %>
          <% If Err.Number <> 0 Then %>
          <tr>
            <td><strong>Err.Number</strong></td>
            <td class="code"><%=Err.Number%></td>
          </tr>
          <tr>
            <td><strong>Err.Description</strong></td>
            <td><%=Err.Description%></td>
          </tr>
          <tr>
            <td><strong>Err.Source</strong></td>
            <td><%=Err.Source%></td>
          </tr>
          <% End If %>
        </table>
      </div>

      <div class="error-section">
        <div class="error-section-title">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <polyline points="12 6 12 12 16 14"></polyline>
          </svg>
          Request Information
        </div>
        <div class="debug-section">
          <table>
            <tr><th>Property</th><th>Value</th></tr>
            <tr><td>Request Method</td><td><%=Request.ServerVariables("REQUEST_METHOD")%></td></tr>
            <tr><td>User Agent</td><td><%=Request.ServerVariables("HTTP_USER_AGENT")%></td></tr>
            <tr><td>Remote Address</td><td><%=Request.ServerVariables("REMOTE_ADDR")%></td></tr>
            <% If Session("Name") <> "" Then %>
            <tr><td>User</td><td><%=Session("Name")%></td></tr>
            <% End If %>
          </table>
        </div>
      </div>

      <div class="error-section">
        <div class="error-section-title">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
            <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
          </svg>
          Session State
        </div>
        <div class="debug-section">
          <table>
            <tr><th>Property</th><th>Value</th></tr>
            <tr><td>LoggedIn</td><td><%=Session("LoggedIn")%></td></tr>
            <tr><td>WorkingDir</td><td><%=Session("WorkingDir")%></td></tr>
            <tr><td>Code</td><td><%=Session("Code")%></td></tr>
            <tr><td>Name</td><td><%=Session("Name")%></td></tr>
            <tr><td>Prefix</td><td><%=Session("Prefix")%></td></tr>
            <tr><td>State</td><td><%=Session("State")%></td></tr>
          </table>
        </div>
      </div>

      <div class="error-section">
        <div class="error-section-title">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
            <line x1="3" y1="9" x2="21" y2="9"></line>
          </svg>
          Cookies (ClientSettings)
        </div>
        <div class="debug-section">
          <table>
            <tr><th>Property</th><th>Value</th></tr>
            <tr><td>WorkingDir</td><td><%=Request.Cookies("ClientSettings")("WorkingDir")%></td></tr>
            <tr><td>Prefix</td><td><%=Request.Cookies("ClientSettings")("Prefix")%></td></tr>
            <tr><td>State</td><td><%=Request.Cookies("ClientSettings")("State")%></td></tr>
            <tr><td>Stylesheet</td><td><%=Request.Cookies("ClientSettings")("Stylesheet")%></td></tr>
          </table>
        </div>
      </div>

      <div class="error-actions">
        <a href="javascript:history.go(-1);" class="error-btn error-btn-primary">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M19 12H5"></path>
            <path d="M12 19l-7-7 7-7"></path>
          </svg>
          Go Back
        </a>
        <a href="/" class="error-btn error-btn-secondary">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
            <polyline points="9 22 9 12 15 12 15 22"></polyline>
          </svg>
          Home
        </a>
      </div>
    </div>
  </div>
</div>

</body>
</html>
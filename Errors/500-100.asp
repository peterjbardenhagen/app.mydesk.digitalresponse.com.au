<%@Language="VBSCRIPT"%>
<!--#include virtual="/System/ssi_Logging.asp"-->
<%
  Option Explicit
  On Error Resume Next
  
  Dim objError
  Set objError = Server.GetLastError()
  
  ' Capture request details for debugging
  Dim strRequestURL, strQueryString, strFormData, strSessionVars, strCookies
  strRequestURL = Request.ServerVariables("URL")
  strQueryString = Request.QueryString
  
  ' Log the error to file
  Call LogASPError()
%>
<!DOCTYPE html>
<html>
<head>
<title>Application Error</title>
<style>
BODY { 
    FONT-FAMILY: Arial, sans-serif; 
    FONT-SIZE: 11pt;
    BACKGROUND: #f5f5f5; 
    COLOR: #333;
    MARGIN: 20px;
    LINE-HEIGHT: 1.5;
}
H2 { 
    FONT-SIZE: 20pt; 
    COLOR: #c00; 
    MARGIN-BOTTOM: 10px;
}
H3 {
    FONT-SIZE: 14pt;
    COLOR: #333;
    BORDER-BOTTOM: 2px solid #c00;
    PADDING-BOTTOM: 5px;
    MARGIN-TOP: 30px;
}
.error-box {
    BACKGROUND: #ffebee;
    BORDER: 1px solid #c00;
    PADDING: 15px;
    MARGIN: 20px 0;
    BORDER-RADIUS: 4px;
}
TABLE { 
    WIDTH: 100%;
    BORDER-COLLAPSE: collapse;
    MARGIN: 10px 0;
}
TH { 
    BACKGROUND: #c00; 
    COLOR: #ffffff;
    PADDING: 8px;
    TEXT-ALIGN: left;
}
TD {
    BACKGROUND: #fff;
    COLOR: #333;
    PADDING: 8px;
    BORDER-BOTTOM: 1px solid #ddd;
}
TR:nth-child(even) TD {
    BACKGROUND: #f9f9f9;
}
.code {
    FONT-FAMILY: Consolas, monospace;
    FONT-SIZE: 10pt;
    BACKGROUND: #f4f4f4;
    PADDING: 2px 4px;
    BORDER-RADIUS: 3px;
}
.debug-section {
    BACKGROUND: #fff;
    BORDER: 1px solid #ddd;
    PADDING: 15px;
    MARGIN: 10px 0;
    BORDER-RADIUS: 4px;
}
.back-link {
    DISPLAY: inline-block;
    MARGIN-TOP: 20px;
    PADDING: 10px 20px;
    BACKGROUND: #c00;
    COLOR: #fff;
    TEXT-DECORATION: none;
    BORDER-RADIUS: 4px;
}
.back-link:hover {
    BACKGROUND: #a00;
}
</style>
</head>
<body>

<h2>An error has occurred</h2>

<div class="error-box">
    <strong>Error Location:</strong> <span class="code"><%=strRequestURL%></span><br>
    <% If strQueryString <> "" Then %>
    <strong>Query String:</strong> <span class="code"><%=strQueryString%></span>
    <% End If %>
</div>

<h3>Error Details</h3>
<table>
  <tr>
    <th width="150">Property</th>
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
    <td colspan="2" style="color: #c00;">
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

<div class="debug-section">
    <h3>Request Information</h3>
    <table>
        <tr><th width="150">Property</th><th>Value</th></tr>
        <tr><td>Request Method</td><td><%=Request.ServerVariables("REQUEST_METHOD")%></td></tr>
        <tr><td>User Agent</td><td><%=Request.ServerVariables("HTTP_USER_AGENT")%></td></tr>
        <tr><td>Remote Address</td><td><%=Request.ServerVariables("REMOTE_ADDR")%></td></tr>
        <% If Session("Name") <> "" Then %>
        <tr><td>User</td><td><%=Session("Name")%></td></tr>
        <% End If %>
    </table>
</div>

<div class="debug-section">
    <h3>Session State</h3>
    <table>
        <tr><th width="150">Property</th><th>Value</th></tr>
        <tr><td>LoggedIn</td><td><%=Session("LoggedIn")%></td></tr>
        <tr><td>WorkingDir</td><td><%=Session("WorkingDir")%></td></tr>
        <tr><td>Code</td><td><%=Session("Code")%></td></tr>
        <tr><td>Name</td><td><%=Session("Name")%></td></tr>
        <tr><td>Prefix</td><td><%=Session("Prefix")%></td></tr>
        <tr><td>State</td><td><%=Session("State")%></td></tr>
    </table>
</div>

<div class="debug-section">
    <h3>Cookies (ClientSettings)</h3>
    <table>
        <tr><th width="150">Property</th><th>Value</th></tr>
        <tr><td>WorkingDir</td><td><%=Request.Cookies("ClientSettings")("WorkingDir")%></td></tr>
        <tr><td>Prefix</td><td><%=Request.Cookies("ClientSettings")("Prefix")%></td></tr>
        <tr><td>State</td><td><%=Request.Cookies("ClientSettings")("State")%></td></tr>
        <tr><td>Stylesheet</td><td><%=Request.Cookies("ClientSettings")("Stylesheet")%></td></tr>
    </table>
</div>

<a href="javascript:history.go(-1);" class="back-link">Go Back</a>

</body>
</html>
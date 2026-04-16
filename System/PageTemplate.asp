<%
' ===============================================================================
' Techlight MyDesk - STANDARD PAGE TEMPLATE
' ===============================================================================
' COPY THIS FILE when creating new pages. Follow the structure exactly.
' RULES:
'   1. Option Explicit at the very top of ALL ASP blocks
'   2. Includes in correct order: Constants → Headers → DB → Functions
'   3. NEVER start an If/Loop in one include and end it in another
'   4. All variables must be declared with Dim
'   5. Use TL_* constants instead of Session/Cookies for static values
' ===============================================================================

Option Explicit

'-------------------------------------------------------------------------------
' LAYER 1: Constants (no dependencies)
'-------------------------------------------------------------------------------
%>
<!--#include virtual="/System/Constants.asp"-->

<%
'-------------------------------------------------------------------------------
' LAYER 2: Response Headers (no dependencies)
'-------------------------------------------------------------------------------
%>
<!--#include virtual="/System/ssi_ResponseHeaders.inc"-->

<%
'-------------------------------------------------------------------------------
' LAYER 3: Database Connection (depends on Constants)
'-------------------------------------------------------------------------------
%>
<!--#include virtual="/System/ssi_dbConn_open.inc"-->

<%
'-------------------------------------------------------------------------------
' LAYER 4: Functions (depends on Constants and dbConn)
'-------------------------------------------------------------------------------
%>
<!--#include virtual="/System/ssi_Functions.asp"-->

<%
'-------------------------------------------------------------------------------
' LAYER 5: Page Logic (your code goes here)
'-------------------------------------------------------------------------------

' EXAMPLE: Check authentication
If Session("LoggedIn") <> "True" Then
    Response.Redirect "/Default.asp"
    Response.End
End If

' EXAMPLE: Declare all your page variables at the top
Dim strPageTitle, intUserId, objRecordset
strPageTitle = "Page Name"
intUserId = Session("Code")

' EXAMPLE: Use TL_* constants instead of session/cookies
' GOOD:  strPath = TL_WORKING_DIR & "/Dashboard.asp"
' BAD:   strPath = Session("WorkingDir") & "/Dashboard.asp"
' BAD:   strPath = Request.Cookies("ClientSettings")("WorkingDir") & "/Dashboard.asp"

%>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><%= strPageTitle %></title>
    <link rel="stylesheet" href="/System/Style_Modern.css">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
</head>
<body>
    <!--#include virtual="/System/ssi_Header_Techlight.inc"-->
    
    <main class="tl-main">
        <div class="tl-container">
            <h1><%= strPageTitle %></h1>
            
            <!-- Your page content here -->
            
        </div>
    </main>
</body>
</html>

<%
'-------------------------------------------------------------------------------
' LAYER 6: Cleanup (always close DB connection)
'-------------------------------------------------------------------------------
If Not dbConn Is Nothing Then
    dbConn.Close
    Set dbConn = Nothing
End If
%>

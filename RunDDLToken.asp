<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%
On Error Resume Next
dbConn.Execute("ALTER TABLE Users ADD COLUMN LoginToken TEXT(255)")
dbConn.Execute("ALTER TABLE Users ADD COLUMN LoginTokenExpiry DATETIME")
If Err.Number <> 0 Then
    Response.Write "Error/Already exists: " & Err.Description
Else
    Response.Write "Schema updated successfully"
End If
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->

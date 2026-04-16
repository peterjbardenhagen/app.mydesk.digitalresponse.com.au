<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%
On Error Resume Next
Set rs = dbConn.Execute("SELECT TOP 20 * FROM SystemLog ORDER BY LogDate DESC")
If Err.Number <> 0 Then
    Response.Write "Error reading logs: " & Err.Description
Else
    Do Until rs.EOF
        Response.Write "<b>" & rs("LogDate") & "</b> | " & rs("LogSource") & " | <div style='color:red;'>" & Server.HTMLEncode(rs("LogMessage")) & "</div><hr>"
        rs.MoveNext
    Loop
End If
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->

<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%
On Error Resume Next
Response.Write "<h1>Data Check</h1>"

Response.Write "<h3>Latest Quotes:</h3>"
Set rs = dbConn.Execute("SELECT TOP 5 QuoteDate, QuoteStatusId, NettPriceTotal FROM Quotes ORDER BY QuoteDate DESC")
If Err.Number <> 0 Then
    Response.Write "Error: " & Err.Description & "<br>"
Else
    Do Until rs.EOF
        Response.Write "Date: " & rs("QuoteDate") & " | Status: " & rs("QuoteStatusId") & " | Val: " & rs("NettPriceTotal") & "<br>"
        rs.MoveNext
    Loop
End If
rs.Close

Response.Write "<h3>Quote Statuses:</h3>"
Set rs = dbConn.Execute("SELECT * FROM QuoteStatus")
Do Until rs.EOF
    Response.Write rs("QuoteStatusId") & ": " & rs("QuoteStatus") & "<br>"
    rs.MoveNext
Loop
rs.Close

Response.Write "<h3>Current Date:</h3>"
Response.Write Now()
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->

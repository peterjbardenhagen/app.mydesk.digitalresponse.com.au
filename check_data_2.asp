<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%
On Error Resume Next
Response.Write "<h1>Invoices & PO Check</h1>"

Response.Write "<h3>Latest Invoices:</h3>"
Set rs = dbConn.Execute("SELECT TOP 5 InvoiceDate, NettPriceTotal FROM Invoices ORDER BY InvoiceDate DESC")
If Err.Number <> 0 Then
    Response.Write "Error: " & Err.Description & "<br>"
Else
    Do Until rs.EOF
        Response.Write "Date: " & rs("InvoiceDate") & " | Val: " & rs("NettPriceTotal") & "<br>"
        rs.MoveNext
    Loop
End If
rs.Close

Response.Write "<h3>Latest POs:</h3>"
Set rs = dbConn.Execute("SELECT TOP 5 PODate, NettPriceTotal FROM PurchaseOrders ORDER BY PODate DESC")
If Err.Number <> 0 Then
    Response.Write "Error: " & Err.Description & "<br>"
Else
    Do Until rs.EOF
        Response.Write "Date: " & rs("PODate") & " | Val: " & rs("NettPriceTotal") & "<br>"
        rs.MoveNext
    Loop
End If
rs.Close
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->

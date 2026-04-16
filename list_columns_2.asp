<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%
On Error Resume Next
Response.Write "--- INVOICES ---<br>"
Set rs = dbConn.Execute("SELECT TOP 1 * FROM Invoices")
For Each field In rs.Fields
    Response.Write field.Name & "<br>"
Next
rs.Close

Response.Write "--- PURCHASE ORDERS ---<br>"
Set rs = dbConn.Execute("SELECT TOP 1 * FROM PurchaseOrders")
For Each field In rs.Fields
    Response.Write field.Name & "<br>"
Next
rs.Close
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->

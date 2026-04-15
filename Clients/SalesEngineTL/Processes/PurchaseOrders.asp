<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

sql = "SELECT PurchaseOrders.POid FROM (Users INNER JOIN (PurchaseOrders INNER JOIN PurchaseOrderAudit AS PA ON PurchaseOrders.POid = PA.POId) ON Users.Code = PurchaseOrders.Code) INNER JOIN Contacts_WithCustomersAndSuppliers_V2 ON PurchaseOrders.ContactId = Contacts_WithCustomersAndSuppliers_V2.ContactId WHERE PurchaseOrders.POStatusId = 3 AND PA.Action='Approved' AND DateDiff('d',#" & ServerToEST(Now()) & "#,PA.DateEntered) < 6"
Set rs = dbConn.Execute(sql)

Do Until rs.EOF
	sql = "UPDATE PurchaseOrders SET POStatusId = 2 WHERE POid = " & rs("POid")
	dbConn.Execute(sql)

	' Audit trail
	sql = "Insert Into PurchaseOrderAudit (POid, Code, Action, DateEntered) Values (" & rs("POid") & ", 'MDADMIN', 'Status changed to Pending Approval, as the purchase order was not actioned for over one week.', '" & ServerToEST(Now()) & "')"
	dbConn.Execute(sql)
	rs.MoveNext
Loop

rs.Close
Set rs = Nothing

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
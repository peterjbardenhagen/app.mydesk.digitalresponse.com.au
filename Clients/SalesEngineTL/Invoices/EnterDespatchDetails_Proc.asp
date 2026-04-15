<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

lngInvoiceId = CLng(Request("InvoiceId"))
strCarrier = Trim(Replace(Request("Carrier"),"'","''"))
strCarrierRef = Trim(Replace(Request("CarrierRef"),"'","''"))
strPackageDetails = Trim(Replace(Request("PackageDetails"),"'","''"))
strInternalNotes = Trim(Replace(Request("InternalNotes"),"'","''"))
strDespatchDate = Trim(Replace(Request("DespatchDate"),"'","''"))

strSql = "Insert Into Despatch (Code, DespatchDate, InvoiceId, Carrier, CarrierRef, PackageDetails, InternalNotes) " &_
			" Values ('" & strCode & "', '" & strDespatchDate & "', " & lngInvoiceId & ", '" & strCarrier & "', '" & strCarrierRef & "', '" & strPackageDetails & "', '" & strInternalNotes & "')"
dbConn.Execute(strSql)

' Audit trail
sql = "Insert Into InvoiceAudit (InvoiceId, Code, Action, DateEntered) Values (" & lngInvoiceId & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Despatch details entered', '" & ServerToEST(Now()) & "')"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("ViewDeliveryNote.asp?InvoiceId=" & lngInvoiceId)

%>
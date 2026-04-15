<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

'On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngPOid

lngPOid = CLng(Request("POid"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsPO = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT [PO].*, PO.IntroText AS IT, PO.InternalNotes AS [IN], [Users].*, PurchaseOrderStatus.POStatus, PurchaseOrderPaymentTypes.POPaymentType FROM (Users INNER JOIN (PurchaseOrderStatus INNER JOIN PurchaseOrders AS PO ON PurchaseOrderStatus.POStatusId = PO.POStatusId) ON Users.Code = PO.Code) INNER JOIN PurchaseOrderPaymentTypes ON PO.POPaymentTypeId = PurchaseOrderPaymentTypes.POPaymentTypeId WHERE PO.POid = " & lngPOid
Set rsPO = dbConn.Execute(sql)

Set rsDi = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Where DivisionId = " & rsPO("DivisionId")
Set rsDi = dbConn.Execute(sql)

Set rsLoc = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsPO("LocationId")
Set rsLoc = dbConn.Execute(sql)

Set rsCon = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Contacts_WithCustomersAndSuppliers_V2 Where ContactId = " & rsPO("ContactId")
Set rsCon = dbConn.Execute(sql)

Dim i
i = 1

sql = "Delete From PurchaseOrderInvoices Where POid = " & lngPOid
dbConn.Execute(sql)

Do Until i = 6
	If Request("InvoiceDate" & i) <> "Date not available" And IsNumeric(Request("InvoiceAmount" & i)) Then
		sql = "Insert Into PurchaseOrderInvoices (POid, InvoiceDate, InvoiceNumber, InvoiceAmount) " &_
				"Values (" & lngPOid & ", '" & Request("InvoiceDate" & i) & "', '" & Request("InvoiceNumber" & i) & "', " & Replace(FormatNumber(Request("InvoiceAmount" & i),2),",","") & ")"
		dbConn.Execute(sql)
	End If
	i = i + 1
Loop

%>
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<!--#include file="NavBar.asp"-->
		<br class="NoPrint">
		<table class="NoPrint">
			<tr>
				<td class="Header4">Enter Invoice Details / Result</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=400 ID="Table1">
			<tr>
				<td>Invoice details have been updated.</td>
			</tr>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
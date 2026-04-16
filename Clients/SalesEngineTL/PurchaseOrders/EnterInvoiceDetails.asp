<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngPOid

lngPOid = CLng(Request("POid"))

%>
<!--#include virtual="/System/ssi_Security.inc"-->
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

%>
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
		<script language="JavaScript">

		function emptyField(textObj) {
			if (textObj.value.length == 0) return true;
			for (var i=0; i < textObj.value.length; i++) {
				var ch = textObj.value.charAt(i);
				if (ch != ' ' && ch != '\t') return false;
			}
			return true
		}

		function checkForm() {

			var validFlag = true
		
			for(var i=1;i<=5;i++) {
				if (validFlag) {
				if (!emptyField(document.getElementById("InvoiceAmount"+i)) && (document.getElementById("InvoiceDate"+i).value=="Date not available" || document.getElementById("InvoiceDate"+i).value=="")) {
					alert("Please enter a date for the invoice for Invoice #"+i+".");
					validFlag = false;
					document.getElementById("InvoiceDate"+i).focus();
				}}
				if (validFlag) {
				if (document.getElementById("InvoiceDate"+i).value!="Date not available" && document.getElementById("InvoiceDate"+i).value!="" && (isNaN(document.getElementById("InvoiceAmount"+i).value) || emptyField(document.getElementById("InvoiceAmount"+i)))) {
					alert("Please enter a valid Invoice amount for Invoice #"+i+".");
					validFlag = false;
					document.getElementById("InvoiceAmount"+i).focus();
				}}
			}
			return validFlag;
		}
		
		function myClear(i) {
			document.getElementById("InvoiceDate"+i).value='';
			document.getElementById("InvoiceNumber"+i).value='';
			document.getElementById("InvoiceAmount"+i).value='';
		}
		
		</script>
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<!--#include file="NavBar.asp"-->
		<br class="NoPrint">
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">Enter Invoice Details</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=400 cellpadding=5>
			<form method="post" action="EnterInvoiceDetails_Proc.asp" name="Form1" onsubmit="return checkForm();">
			<input type="hidden" name="POid" value="<%= lngPOid %>">
			<tr>
				<td colspan=2 align="right"><input type="submit" value="Save"></td>
			</tr>
<%

Dim i
i = 1

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From PurchaseOrderInvoices Where POid = " & lngPOid
Set rs = dbConn.Execute(sql)

Do Until i = 6

	If Not(rs.EOF) Then
		strInvoiceDate = rs("InvoiceDate")
		strInvoiceNumber = rs("InvoiceNumber")
		dblInvoiceAmount = CDbl(rs("InvoiceAmount"))
		rs.MoveNext
	Else
		strInvoiceDate = ""
		strInvoiceNumber = ""
		dblInvoiceAmount = ""
	End If

%>
			<tr>
				<td colspan=2 bgcolor="#cccccc"><b>Invoice #<%= i %></b></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;">Invoice Date</td>
				<td valign="top" align="right"><input type="text" value="<% If IsDate(strInvoiceDate) Then Response.Write(FormatDateU(strInvoiceDate, False)) Else Response.Write("Date not available") %>" name="InvoiceDate<%= i %>" id="InvoiceDate<%= i %>" readonly> <a href="javascript:showCal('Calendar23_<%= i %>')"><img src="/Images/Calendar.gif" border=0></a></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;">Invoice Number</td>
				<td valign="top" align="right"><input maxlength=12 type="text" value="<%= strInvoiceNumber %>" name="InvoiceNumber<%= i %>" id="InvoiceNumber<%= i %>" style="width:122px;" /></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;">Invoice Amount</td>
				<td valign="top" align="right">$<input maxlength=12 type="text" value="<%= dblInvoiceAmount %>" name="InvoiceAmount<%= i %>" id="InvoiceAmount<%= i %>" style="width:122px;" /></td>
			</tr>
			<tr>
				<td colspan=2 align="right"><input type="button" value="Clear" onclick="myClear(<%= i %>);" /></td>
			</tr>
			<tr>
				<td colspan=2><hr /></td>
			</tr>
<%

	i = i + 1
Loop

%>
			<tr>
				<td colspan=2 align="right"><input type="submit" value="Save"></td>
			</tr>
			</form>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
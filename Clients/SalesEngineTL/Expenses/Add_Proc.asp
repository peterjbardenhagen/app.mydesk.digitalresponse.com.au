<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim strCode
Dim intDivisionId
Dim dteExpenseDate
Dim intExpenseTypeId
Dim strDescription
Dim intContactId
Dim decCostIncGST
Dim decGST
Dim intFBTTTL
Dim intFBTNon
Dim strReceipt
Dim strReimbursement
Dim strTTLCorporateCard
Dim strComment
Dim sql

strCode = Trim(Request.Cookies("UserSettings")("Code"))
intDivisionId = CInt(Request("DivisionId"))
dteExpenseDate = Trim(Request("ExpenseDate"))
intExpenseTypeId = CLng(Request("ExpenseTypeId"))
strDescription = Replace(Trim(Request("Description")), "'", "''")
intContactId = CLng(Request("ContactId"))
decCostIncGST = Trim(Request("CostIncGST"))
decGST = Trim(Request("GST"))
intFBTTTL = Trim(Request("FBTTTL"))
intFBTNon = Trim(Request("FBTNon"))
strReceipt = Trim(Request("Receipt"))
strReimbursement = Trim(Request("Reimbursement"))
strTTLCorporateCard = Trim(Request("TTLCorporateCard"))
strComment = Replace(Trim(Request("Comment")), "'", "''")

If Len(intFBTTTL) = 0 Then
	intFBTTTL = 0
End If

If Len(intFBTNon) = 0 Then
	intFBTNon = 0
End If

If Len(decCostIncGST) = 0 Then
	decCostIncGST = 0
End If

If Len(decGST) = 0 Then
	decGST = 0
End If

' Check for sign off first
Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM ExpensesSignOffs WHERE [Month] = " & Month(CDate(dteExpenseDate)) & " AND [Year] = " & Year(CDate(dteExpenseDate)) & " AND Code = '" & strCode & "'"
Set rs = dbConn.Execute(sql)
If Not(rs.BOF and rs.EOF) Then
%>
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
	</head>
	<body bgcolor="#dddddd">
<!--#include virtual="/System/ssi_Header.inc"-->
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Expenses</a> / Add Expense /></span>
				<br/><br/>
				<p>The month of the expense (<%= MonthName(Month(CDate(dteExpenseDate)), False) & " " & Year(CDate(dteExpenseDate)) %>) has already been signed off.</p>
				<p>Click here to go <a href="javascript:history.go(-1);">back</a> and redo the expense claim.</p>
			</td>
		</tr>
	</table>
	</body>
</html>
<%
	If IsObject(rs) Then
		rs.Close
		Set rs = Nothing
	End If
	Response.End
Else
	sql = "Insert Into Expenses (Code, DivisionId, ExpenseDate, ExpenseTypeId, Description, ContactId, CostIncGST, GST, FBTTTL, FBTNon, TTLCorporateCard, Comment) Values ('" & strCode & "', " & intDivisionId & ", '" & dteExpenseDate & "', " & intExpenseTypeId & ", '" & strDescription & "', " & intContactId & ", '" & decCostIncGST & "', '" & decGST & "', '" & intFBTTTL & "', '" & intFBTNon & "', " & strTTLCorporateCard & ", '" & strComment & "')"
	dbConn.Execute(sql)
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Expense+added")

%>
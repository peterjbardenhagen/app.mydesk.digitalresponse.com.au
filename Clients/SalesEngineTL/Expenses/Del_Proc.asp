<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

'On Error Resume Next

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim lngId
Dim sql
Dim strMsg
Dim strErrorDescription
Dim rsCheck
Dim rsCheck2
Dim intMonth
Dim intYear
Dim boolSignedOff

lngId = CLng(Request("Id"))

' *** Using user division id as we are not using the expenses division id ... yet.

Set rsCheck = Server.CreateObject("ADODB.RecordSet")
sql = "Select *, Expenses.Code As ExpenseOriginator, Users.DivisionId As UserDivisionId From Expenses Inner Join Users On Expenses.Code = Users.Code Where Eid = " & lngId
Set rsCheck = dbConn.Execute(sql)

If Not SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsPayroll"), rsCheck("UserDivisionId")) Then ' If not payroll
	If Not(rsCheck.BOF And rsCheck.EOF) Then
		intMonth = Month(rsCheck("ExpenseDate"))
		intYear = Year(rsCheck("ExpenseDate"))
		Set rsCheck2 = Server.CreateObject("ADODB.RecordSet")
		sql = "Select * From ExpensesSignOffs Where Code = '" & rsCheck("ExpenseOriginator") & "' And [Month] = " & intMonth & " And [Year] = " & intYear
		Set rsCheck2 = dbConn.Execute(sql)
		If Not(rsCheck2.BOF And rsCheck2.EOF) Then
			boolSignedOff = True
		Else
			boolSignedOff = False
			sql = "Delete From Expenses Where Eid = " & lngId
			dbConn.Execute(sql)
		End If
	End If

	If IsObject(rsCheck) Then
		rsCheck.Close
		Set rsCheck = Nothing
	End If
	strErrorDescription = err.Description
Else
	boolSignedOff = False
	sql = "Delete From Expenses Where Eid = " & lngId
	dbConn.Execute(sql)
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

If boolSignedOff Then
	strMsg = "Record cannot be deleten, as month has been signed off."
Else
	If GetErrorCode(strErrorDescription) = 1 Then
		strMsg = "Record cannot be deleten, as there are historical records that depend on it."
	Else
		strMsg = "Record deleted"
	End If
End If

%>
<html>
<head>
<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
<script language="javascript">
	alert('<%= strMsg %>');
	RefreshIFrame_Global_Opener();
	window.close();
</script>
</head>
<body>
</body>
</html>
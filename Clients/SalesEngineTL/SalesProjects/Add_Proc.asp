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

Dim dteProspectDate
Dim dteTenderDate
Dim dteAcceptedDate
Dim dteRejectedDate
Dim strCode
Dim intContactId
Dim strProject
Dim strProduct
Dim decValue
Dim strOneOffSalesProject
Dim decAmountPerMonth
Dim intNumberOfMonths
Dim dtePotentialOrderDate
Dim strComment
Dim sql

dteProspectDate = Trim(Request("ProspectDate"))
dteTenderDate = Trim(Request("TenderDate"))
dteAcceptedDate = Trim(Request("AcceptedDate"))
dteRejectedDate = Trim(Request("RejectedDate"))
strCode = Trim(Request.Cookies("UserSettings")("Code"))
intContactId = CLng(Request("ContactId"))
strProject = Replace(Trim(Request("Project")), "'", "''")
strProduct = Replace(Trim(Request("Product")), "'", "''")
decValue = Trim(Request("Value"))
strOneOffSalesProject = Trim(Request("OneOffSalesProject"))
decAmountPerMonth = Trim(Request("AmountPerMonth"))
intNumberOfMonths = Trim(Request("NumberOfMonths"))
dtePotentialOrderDate = Trim(Request("PotentialOrderDate"))
strComment = Replace(Trim(Request("Comment")), "'", "''")

If Len(decValue) = 0 Then
	decValue = 0
End If

If Len(decAmountPerMonth) = 0 Then
	decAmountPerMonth = 0
End If

If Len(intNumberOfMonths) = 0 Then
	intNumberOfMonths = 0
End If

If dteProspectDate = "" Then
	dteProspectDate = "01-01-01"
End If

If dteTenderDate = "" Then
	dteTenderDate = "01-01-01"
End If

If dteAcceptedDate = "" Then
	dteAcceptedDate = "01-01-01"
End If

If dteRejectedDate = "" Then
	dteRejectedDate = "01-01-01"
End If

If CBool(strOneOffSalesProject) Then
	decAmountPerMonth = 0
End If

sql = "Insert Into SalesProjects (DateEntered, Code, ContactId, Project, Product, [Value], OneOffSalesProject, AmountPerMonth, NumberOfMonths, PotentialOrderDate, Comment, ProspectDate, TenderDate, AcceptedDate, RejectedDate) Values ('" & ServerToEST(Now()) & "', '" & strCode & "', " & intContactId & ", '" & strProject & "', '" & strProduct & "', " & decValue & ", " & strOneOffSalesProject & ", " & decAmountPerMonth & ", " & intNumberOfMonths & ", '" & dtePotentialOrderDate & "', '" & strComment & "', '" & dteProspectDate & "', '" & dteTenderDate & "', '" & dteAcceptedDate & "', '" & dteRejectedDate & "')"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Sales+Project+added")

%>
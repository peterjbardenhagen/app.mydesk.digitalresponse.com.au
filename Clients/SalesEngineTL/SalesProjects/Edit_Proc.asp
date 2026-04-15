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
<%

Dim lngSalesProjectId
Dim dteProspectDate
Dim dteTenderDate
Dim dteAcceptedDate
Dim dteRejectedDate
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

lngSalesProjectId = CLng(Request("SalesProjectId"))
dteProspectDate = Trim(Request("ProspectDate"))
dteTenderDate = Trim(Request("TenderDate"))
dteAcceptedDate = Trim(Request("AcceptedDate"))
dteRejectedDate = Trim(Request("RejectedDate"))
intContactId = CLng(Request("ContactId"))
strProject = Replace(Trim(Request("Project")), "'", "''")
strProduct = Replace(Trim(Request("Product")), "'", "''")
decValue = Trim(Request("Value"))
strOneOffSalesProject = Trim(Request("OneOffSalesProject"))
decAmountPerMonth = Trim(Request("AmountPerMonth"))
intNumberOfMonths = CLng(Request("NumberOfMonths"))
dtePotentialOrderDate = Trim(Request("PotentialOrderDate"))
strComment = Replace(Trim(Request("Comment")), "'", "''")

If Len(decValue) = 0 Then
	decValue = 0
End If

If Len(decAmountPerMonth) = 0 Then
	decAmountPerMonth = 0
End If

If Len(strOneOffSalesProject) = 0 Then
	strOneOffSalesProject = "0"
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

sql = "Update SalesProjects Set ContactId = " & intContactId & ", Project = '" & strProject & "', Product = '" & strProduct & "', [Value] = " & decValue & ", OneOffSalesProject = " & strOneOffSalesProject & ", AmountPerMonth = " & decAmountPerMonth & ", NumberOfMonths = " & intNumberOfMonths & ", PotentialOrderDate = '" & dtePotentialOrderDate & "', Comment = '" & strComment & "', ProspectDate = '" & dteProspectDate & "', TenderDate = '" & dteTenderDate & "', AcceptedDate = '" & dteAcceptedDate & "', RejectedDate = '" & dteRejectedDate & "' Where SalesProjectId = " & lngSalesProjectId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Sales+Project+updated")

%>
<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("UserSettings")("UserTypeId") => 4 Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

strLineManagerCode = Trim(Request("LineManagerCode"))
intDivisionId = CLng(Request("DivisionId"))
intUserRoleId = CLng(Request("UserRoleId"))
intUserTypeId = CLng(Request("UserTypeId"))
intUserRoleId = CLng(Request("UserRoleId"))
intLocationId = CInt(Request("LocationId"))
strName = Trim(Replace(Request("Name"), "'", "''"))
strInitials = Trim(Replace(Request("Initials"), "'", "''"))
strPosition = Trim(Replace(Request("Position"), "'", "''"))
strPhone = Trim(Replace(Request("Phone"), "'", "''"))
strMobile = Trim(Replace(Request("Mobile"), "'", "''"))
strFax = Trim(Replace(Request("Fax"), "'", "''"))
strEmail = Trim(Replace(Request("Email"), "'", "''"))
strPW = Trim(Replace(Request("PW"), "'", "''"))
strActive = Trim(Request("Active"))

' strTimetable = Trim(Request("ReqTimesheet"))
' intDaysPerWeek = CLng(Request("DaysPerWeek"))
' intHoursPerDay = CLng(Request("HoursPerDay"))
' decExpensesPerMonth = Replace(FormatNumber(Request("ExpensesPerMonth"),2),",","")
' decSalesBudget = Replace(FormatNumber(Request("SalesBudget"),2),",","")
' decProspectsBudget = Replace(FormatNumber(Request("ProspectsBudget"),2),",","")
' decPOApprovalLimit = 0
' decPOCapExApprovalLimit = 0
strTimetable = ""
intdaysperweek = 8
inthoursperday = 5
decexpensespermonth = 0
decsalesbudget = 0
decprospectsbudget = 0
decpoapprovallimit = 0
decpocapexapprovallimit = 0

strQuotes = Trim(Request("Quotes"))
strRFQ = Trim(Request("RFQ"))
strPurchaseOrders = Trim(Request("PurchaseOrders"))
decQuoteApprovalLimit = 0

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select Top 1 UserId From Users Order By UserId Desc"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
	intUserId = rs("UserId")+1
Else
	intUserId = 1
End If

strCode = "MD" & MakePadding(intUserId+100, "0", 4)

sql = "Insert Into Users (LocationId, DatePasswordChanged, LineManagerCode, UserId, DivisionId, UserTypeId, UserRoleId, Code, Name, Initials, Position, Phone, Mobile, Fax, Email, PW, Active, DaysPerWeek, HoursPerDay, ExpensesPerMonth, SalesBudget, ProspectsBudget, POApprovalLimit, POCapExApprovalLimit, QuoteApprovalLimit,RequiresTimesheet) Values (" & intLocationId & ", '" & ServerToEST(Now()) & "', '" & strLineManagerCode & "', " & intUserId & ", " & intDivisionId & ", " & intUserTypeId & ", " & intUserRoleId & ", '" & strCode & "', '" & strName & "', '" & strInitials & "', '" & strPosition & "', '" & strPhone & "', '" & strMobile & "', '" & strFax & "', '" & strEmail & "', '" & strPW & "', " & strActive & ", " & intDaysPerWeek & ", " & intHoursPerDay & ", '" & decExpensesPerMonth & "', '" & decSalesBudget & "', '" & decProspectsBudget & "', '" & decPOApprovalLimit & "', '" & decPOCapExApprovalLimit & "', '" & decQuoteApprovalLimit & "','" & strTimetable & "')"
response.write sql
response.end
dbConn.Execute(sql)


Set rsD = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Order By DivisionId"
Set rsD = dbConn.Execute(sql)

Do Until rsD.EOF
	sql = "Insert Into UsersAccess (UserId, DivisionId, Visible, MemberOf, Manager, Quotes, RFQ, PurchaseOrders) Values (" & intUserId & ", " & rsD("DivisionId") & ", " & Request("Visible" & rsD("DivisionId")) & ", " & Request("MemberOf" & rsD("DivisionId")) & ", " & Request("Manager" & rsD("DivisionId")) & ", " & Request("Quotes" & rsD("DivisionId")) & ", " & Request("RFQ" & rsD("DivisionId")) & ", " & Request("PurchaseOrders" & rsD("DivisionId")) & ")"
	sql = Replace(Replace(Replace(sql, ", ,", ", 0,"), ", )", ", 0)"), ", ,", ", 0,")
	dbConn.Execute(sql)
	rsD.MoveNext
Loop

rsD.Close
Set rsD = Nothing

' Insert Admin contact
Set rsNew = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Where Deleted = 0 AND UserId = " & intUserId
Set rsNew = dbConn.Execute(sql)

sql = "Insert Into Contacts (Code, FirstName, Surname, CompanyId, CCompany, StateId, OStateId) Values ('" & rsNew("Code") & "', 'Admin', 'Contact', 142, 'Admin Contact', 9, 9)"
dbConn.Execute(sql)

rsNew.Close
Set rsNew = Nothing

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/Users/?Msg=User+added")

%>
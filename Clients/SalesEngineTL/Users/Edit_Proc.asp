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
intLocationId = CLng(Request("LocationId"))
intUserId = CLng(Request("UserId"))
intUserRoleId = CLng(Request("UserRoleId"))
intUserTypeId = CLng(Request("UserTypeId"))
intDivisionId = CLng(Request("DivisionId"))
strCode = Trim(Replace(Request("Code"), "'", "''"))
strName = Trim(Replace(Request("Name"), "'", "''"))
strInitials = Trim(Replace(Request("Initials"), "'", "''"))
strPosition = Trim(Replace(Request("Position"), "'", "''"))
strPhone = Trim(Replace(Request("Phone"), "'", "''"))
strMobile = Trim(Replace(Request("Mobile"), "'", "''"))
strFax = Trim(Replace(Request("Fax"), "'", "''"))
strEmail = Trim(Replace(Request("Email"), "'", "''"))
strPW = Trim(Replace(Request("PW"), "'", "''"))
strActive = Trim(Request("Active"))
strTimetable = Trim(Request("ReqTimesheet"))
intDaysPerWeek = CLng(Request("DaysPerWeek"))
intHoursPerDay = CLng(Request("HoursPerDay"))
decExpensesPerMonth = Replace(FormatNumber(Request("ExpensesPerMonth"),2),",","")
decSalesBudget = Replace(FormatNumber(Request("SalesBudget"),2),",","")
decProspectsBudget = Replace(FormatNumber(Request("ProspectsBudget"),2),",","")
decPOApprovalLimit = 0
decPOCapExApprovalLimit = 0
decQuoteApprovalLimit = 0

sql = "Update Users Set UserRoleId = " & intUserRoleId & ", LineManagerCode = '" & strLineManagerCode & "', LocationId = " & intLocationId & ", UserTypeId = " & intUserTypeId & ", DivisionId = " & intDivisionId & ", Name = '" & strName & "', Initials = '" & strInitials & "', Position = '" & strPosition & "', Phone = '" & strPhone & "', Mobile = '" & strMobile & "', Fax = '" & strFax & "', Email = '" & strEmail & "', PW = '" & strPW & "', Active = '" & strActive & "', DaysPerWeek = " & intDaysPerWeek & ", HoursPerDay = " & intHoursPerDay & ", ExpensesPerMonth = '" & decExpensesPerMonth & "', SalesBudget = '" & decSalesBudget & "', ProspectsBudget = '" & decProspectsBudget & "', POApprovalLimit = '" & decPOApprovalLimit & "', POCapExApprovalLimit = '" & decPOCapExApprovalLimit & "', QuoteApprovalLimit = '" & decQuoteApprovalLimit & "',RequiresTimesheet= '" & strTimetable & "' Where UserId = " & intUserId
dbConn.Execute(sql)

sql = "Delete From UsersAccess Where DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") And UserId = " & intUserId
dbConn.Execute(sql)

Set rsD = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Order By DivisionId"
Set rsD = dbConn.Execute(sql)

Do Until rsD.EOF
	sql = "Insert Into UsersAccess (UserId, DivisionId, Visible, MemberOf, Manager, Quotes, RFQ, PurchaseOrders, Payroll) Values (" & intUserId & ", " & rsD("DivisionId") & ", " & Request("Visible" & rsD("DivisionId")) & ", " & Request("Visible" & rsD("DivisionId")) & ", " & Request("Manager" & rsD("DivisionId")) & ", " & Request("Quotes" & rsD("DivisionId")) & ", " & Request("RFQ" & rsD("DivisionId")) & ", " & Request("PurchaseOrders" & rsD("DivisionId")) & ", " & Request("Payroll" & rsD("DivisionId")) & ")"
	sql = Replace(Replace(Replace(sql, ", ,", ", 0,"), ", )", ", 0)"), ", ,", ", 0,")
	dbConn.Execute(sql)
	rsD.MoveNext
Loop
rsD.Close
Set rsD = Nothing

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/Users/?Msg=User+updated")

%>
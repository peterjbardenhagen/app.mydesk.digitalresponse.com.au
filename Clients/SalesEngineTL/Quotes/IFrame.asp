<%

Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.AddHeader "pragma","no-cache"
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-cache"

On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dates.inc"-->
<%

Dim nRows ' Number of rows in 
Dim nColumns
Dim rs
Dim cmd
Dim CurPage
Dim strCode
Dim intDivisionId
Dim intCompanyId
Dim strProject
Dim dteDateFrom
Dim dteDateTo
Dim intQuoteStatusId
Dim strKeyword
Dim sql

strCode = Trim(Request("Code"))

If strCode = "" Then
	strCode = "All"
End If

intDivisionId = CInt(Request("DivisionId"))
intCompanyId = CLng(Request("CompanyId"))
strProject = Trim(Request("Project"))

If strProject = "" Then
	strProject = "All"
End If

dteDateFrom = FormatDateU(Request("DateFrom"), False)
dteDateTo = FormatDateU(Request("DateTo"), False)
intQuoteStatusId = CInt(Request("QuoteStatusId"))
strKeyword = Trim(Request("Keyword"))
strCustomerSearch = Trim(Request("CustomerSearch"))

%>
<!--#include virtual="/System/ssi_dbConn_Open.inc"-->
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Quotes List - Techlight MyDesk</title>
	<meta http-equiv="Cache-Control" content="no-cache">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-cache">
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Style_Techlight.css" rel="stylesheet" type="text/css">
	<script src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
	<script>
function copyRecord(id) {
window.open('Copy_Proc.asp?Id=' + id);
parent.document.location.href=parent.document.location.href;
RefreshIFrame_Global();
	// try {
		// var w = window.open("Copy_Proc.asp?Id=" + id, 'winCopy', "width=25,height=25,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
		// w.focus();
		// w.moveTo(0, 0);
	// } catch (e) {
		// alert('Your computer has a pop-up blocker or the results window is already open.');
		// return;
	// }
}
	</script>
	<style>
		body, p, td, span, div {
			font-size:12px !important;
		}
		table {
			width: 100%;
			background-color: #ffffff;
			padding-bottom: 5px;
			padding-top: 5px;
			padding-left: 5px;
			padding-right: 5px;
			margin: 0px;
			border: 5px;
		}
		tr {
			margin: 5px;
		}
		td {
			padding: 10px;
			background-color:#eeeeee;
		}
		.header {
			font-weight: bold;
			background-color: #cccccc;
		}
		td,.header {
			font-size: 12px;
		}
	</style>
<%

function activewidgets_html(Id, s, FieldName)

	If Not IsNull(s) Then
		If IsNumeric(s) And (FieldName = "Total Cost Ex GST ($)" Or FieldName = "Nett Price Total Ex GST ($)") Then
			s = FormatCurrency(s, 2)
		ElseIf s = "QUNEXTAPPROVER" Then
			s = GetQuoteNextLineApprover(Id)
		ElseIf s = "QULASTAPPROVER" Then
			s = GetQuoteLastLineApprover(Id)
		ElseIf IsNumeric(s) And (FieldName = "Margin") Then
			s = FormatNumber(s, 2) & "%"
		ElseIf IsDate(s) Then
			s = FormatDateU2(s, False)
		ElseIf s = "ACTION" Then
			s = "<a href='#' onclick='ViewQuote(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & Id & ");'>View</a> | <a href='" & Request.Cookies("ClientSettings")("WorkingDir") & "/Quotes/Edit.asp?Qid=" & Id & "' target='_parent'>Edit</a> | <a href='#' onclick='UpdateQuoteStatus(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & Id & ");'>Update Status</a> | <a href='#' onclick='copyRecord(" & Id & ");'>Copy Quote</a> | <a href='#' onclick='deleteRecord(" & Id & ");'>Delete</a>"
		ElseIf s = "DESCRIPTION" Then
			Dim rsDesc
			Dim sql
			Dim strDesc
			Dim i
			
			Set rsDesc = Server.CreateObject("ADODB.RecordSet")
			sql = "Select Top 5 Quantity, Units, Days, Description From QuoteContents Where Qid = " & Id & " UNION Select Top 5 Quantity, 0 As Units, 0 As Days, Description From QuoteThirdPartyContents Where QuoteId = " & Id
			Set rsDesc = dbConn.Execute(sql)
			
			If Not(rsDesc.BOF And rsDesc.EOF) Then
				i = 1
				strDesc = "This quote includes the following:" & vbcrlf
				Do Until rsDesc.EOF
					If rsDesc("Units") > 0 Then
						strDesc = strDesc & rsDesc("Units") & " units x " & rsDesc("Days") & " days " & rsDesc("Description") & vbcrlf
					Else
						strDesc = strDesc & rsDesc("Quantity") & " x " & rsDesc("Description") & vbcrlf
					End If
					i = i + 1
					rsDesc.MoveNext
				Loop
			End If
			
			If IsObject(rsDesc) Then
				rsDesc.Close
				Set rsDesc = Nothing
			End If
			
			If strDesc <> "" Then
				s = "<a href='#' onclick='alert(""" & Replace(Replace(Replace(strDesc, vbcrlf, "\n")&"","'","`"),"""","`") & """);'>Click here</a>"
			Else
				s = "No contents"
			End If
		ElseIf s = "HISTORY" Then
			Dim rsHistory
			Dim strSql_History
			Dim strInitials
			Dim dteDate
			Dim strComment
			Dim intTableId
			
			intTableId = 6
			
			Set rsHistory = Server.CreateObject("ADODB.RecordSet")
			strSql_History = "Select Top 1 Comments.*, Users.* From Comments Inner Join Users On Users.Code = Comments.FromCode Where Comments.ItemId = " & Id & " And Comments.TableId = " & intTableId & " Order By Comments.DateEntered Desc"
			Set rsHistory = dbConn.Execute(strSql_History)
			
			If Not(rsHistory.BOF And rsHistory.EOF) Then
				strInitials = rsHistory("Initials")
				dteDate = rsHistory("DateEntered")
				strComment = " : " & rsHistory("Comment")
			Else
				strInitials = ""
				dteDate = ""
				strComment = ""
			End If
			
			If IsObject(rsHistory) Then
				rsHistory.Close
				Set rsHistory = Nothing
			End If
			
			If IsDate(dteDate) Then
				s = "<!--" & DBDate(dteDate) & "--><a href='../TableComments/Comments.asp?TableId=" & intTableId & "&ItemId=" & Id & "'>" & FormatDateU3(dteDate) & " " & strInitials & " " & strComment & "</a> - <a href='../TableComments/Add.asp?TableId=" & intTableId & "&ItemId=" & Id & "'>Add</a>"
			Else
				s = "<a href='../TableComments/Add.asp?TableId=" & intTableId & "&ItemId=" & Id & "'>Add</a>"
			End If
		ElseIf s = "FILES" Then
			Dim rsFiles
			Dim strSql
			Dim strDescription
		
			intTableId = 6
			
			Set rsFiles = Server.CreateObject("ADODB.RecordSet")
			strSql = "Select Top 1 TableFiles.*, Users.* From TableFiles Inner Join Users On Users.Code = TableFiles.Code Where TableFiles.ItemId = " & Id & " And TableFiles.TableId = " & intTableId & " Order By TableFiles.DateEntered Desc"
			Set rsFiles = dbConn.Execute(strSql)
			
			If Not(rsFiles.BOF And rsFiles.EOF) Then
				strInitials = rsFiles("Initials")
				dteDate = rsFiles("DateEntered")
				strDescription = " : " & rsFiles("Description")
			Else
				strInitials = ""
				dteDate = ""
				strDescription = ""
			End If
			
			If IsObject(rsFiles) Then
				rsFiles.Close
				Set rsFiles = Nothing
			End If
			
			If IsDate(dteDate) Then
				s = "<!--" & DBDate(dteDate) & "--><a href='../TableFiles/?TableId=" & intTableId & "&ItemId=" & Id & "'>" & FormatDateU3(dteDate) & " " & strInitials & " " & strDescription & "</a>"
			Else
				s = "<a href='../TableFiles/Add.asp?TableId=" & intTableId & "&ItemId=" & Id & "'>Add</a>"
			End If
		End If
		's = Replace(s, "'", "`")
		s = Replace(s, "\", "\\")
		s = Replace(s, """", "\""")
		s = Replace(s, vbCr, "\r")
		s = Replace(s, vbLf, "\n")
	Else
		's = "Not entered"
	End If

	activewidgets_html = s
end function

%>

</head>
<body style="background-color:#ffffff; margin: 0; padding: 16px; font-family: 'Inter', sans-serif;">
<%

Dim oRecordset
Dim strSql

' Execute a SQL query
strSql = "SELECT DISTINCT Quotes.Qid As [Quote #], Contacts_WithCustomersAndSuppliers_V2.CompanyName, Quotes.Reference As [Project], 'DESCRIPTION' AS [Description], 'ACTION' As Action, QuoteStatus.QuoteStatus As [Quote Status], Quotes.UnitCostTotal, Quotes.NettPriceTotal, Quotes.Margin, Quotes.QuoteDate As [Quote Date], 'FILES' AS Files, 'HISTORY' AS History, 'QUNEXTAPPROVER' As [Next Approver], 'QULASTAPPROVER' As [Final Approver], Users.Name As [Originator] FROM Contacts_WithCustomersAndSuppliers_V2 INNER JOIN (QuoteStatus INNER JOIN (Divisions INNER JOIN (Users INNER JOIN Quotes ON Users.Code = Quotes.Code) ON Divisions.DivisionId = Quotes.DivisionId) ON QuoteStatus.QuoteStatusId = Quotes.QuoteStatusId) ON Contacts_WithCustomersAndSuppliers_V2.ContactId = Quotes.ContactId WHERE "
If intDivisionId > 0 Then
	strSql = strSql & " Quotes.DivisionId = " & intDivisionId & " AND"
Else
	If Not(Request.Cookies("DivisionId") = 2) Then
		strSql = strSql & " Quotes.DivisionId = " & Request.Cookies("DivisionId") & " AND"
	End If
End If
If strCode <> "All" Then
	strSql = strSql & " Users.Code = '" & strCode & "' AND"
End If
If intCompanyId > 0 Then
	strSql = strSql & " Contacts_WithCustomersAndSuppliers_V2.CompanyId = " & intCompanyId & " AND"
End If
If intQuoteStatusId > 0 Then
	If intQuoteStatusId = 555 Then
		strSql = strSql & " Quotes.QuoteStatusId Not In (4,5) AND"
	Else
		strSql = strSql & " Quotes.QuoteStatusId = " & intQuoteStatusId & " AND"
	End If
End If
strSql = strSql & " (Quotes.QuoteDate >= #" & DBDate(dteDateFrom) & "# AND Quotes.QuoteDate < #" & DBDate(dteDateTo) & "#)"
If strKeyword <> "" Then
	strSql = strSql & " AND (Quotes.Reference LIKE '%" & strKeyword & "%' OR Quotes.Terms LIKE '%" & strKeyword & "%' OR Quotes.InternalNotes LIKE '%" & strKeyword & "%' OR Quotes.CustomerNotes LIKE '%" & strKeyword & "%' OR Quotes.Qid LIKE '%" & strKeyword & "%' OR Users.Name LIKE '%" & strKeyword & "%')"
End If
If strCustomerSearch <> "" Then
	strSql = strSql & " AND Contacts_WithCustomersAndSuppliers_V2.CompanyName LIKE '%" & Replace(strCustomerSearch, "'", "''") & "%'"
End If
strSql = strSql & " ORDER BY Qid DESC"

Set oRecordset = dbConn.Execute(strSql)
If oRecordset.BOF And oRecordset.EOF Then MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/NoRecords.asp")

Response.Write("<table class='tl-data-grid'>")
Response.Write("<thead><tr><th>Quote #</th><th>Company</th><th>Project</th><th>Status</th><th>Cost Ex GST</th><th>Price Ex GST</th><th>Margin</th><th>Date</th><th>Actions</th></tr></thead>")
Response.Write("<tbody>")
Do While Not (oRecordset.EOF)
	Dim Id, Action, StatusClass
	Id = oRecordset("Quote #")
	
	' Determine status badge class
	Select Case oRecordset("Quote Status")
		Case "Won", "Approved"
			StatusClass = "tl-badge-success"
		Case "Pending", "Submitted"
			StatusClass = "tl-badge-warning"
		Case "Lost", "Cancelled"
			StatusClass = "tl-badge-danger"
		Case Else
			StatusClass = "tl-badge-info"
	End Select
	
	Action = "<a href='" & Request.Cookies("ClientSettings")("WorkingDir") & "/Quotes/View.asp?Qid=" & Id & "' class='tl-btn-secondary' style='padding:4px 8px;font-size:12px;'>View</a> " & _
			 "<a href='" & Request.Cookies("ClientSettings")("WorkingDir") & "/Quotes/Edit.asp?Qid=" & Id & "' class='tl-btn-primary' style='padding:4px 8px;font-size:12px;'>Edit</a>"
	
	Response.Write("<tr>")
	Response.Write("<td nowrap><strong>" & oRecordset("Quote #") & "</strong></td>")
	Response.Write("<td>" & oRecordset("CompanyName") & "</td>")
	Response.Write("<td>" & oRecordset("Project") & "</td>")
	Response.Write("<td><span class='tl-badge " & StatusClass & "'>" & oRecordset("Quote Status") & "</span></td>")
	Response.Write("<td>$" & FormatNumber(oRecordset("UnitCostTotal"),2) & "</td>")
	Response.Write("<td>$" & FormatNumber(oRecordset("NettPriceTotal"),2) & "</td>")
	Response.Write("<td>" & FormatNumber(oRecordset("Margin"),2) & "%</td>")
	Response.Write("<td nowrap>" & FormatDateU2(oRecordset("Quote Date"), False) & "</td>")
	Response.Write("<td nowrap>" & Action & "</td>")
	Response.Write("</tr>")
	oRecordset.MoveNext
Loop
Response.Write("</tbody></table>")

' Close recordset and connection
oRecordset.close
dbConn.close

If Err.Number <> 0 then
	Response.Write(Err.Description)
	Error.Clear
End If
%>
</body>
</html>
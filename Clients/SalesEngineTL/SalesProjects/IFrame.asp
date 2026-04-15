<%

Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.AddHeader "pragma","no-cache"
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-cache"

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
Dim intCompanyId
Dim strProject
Dim dteDateFrom
Dim dteDateTo

If Request.Cookies("UserSettings")("Manager") Then
	strCode = Trim(Request("Code"))
Else
	strCode = Request.Cookies("UserSettings")("Code")
End If

If strCode = "" Then
	strCode = "All"
End If

intCompanyId = CLng(Request("CompanyId"))
strProject = Trim(Request("Project"))

If strProject = "" Then
	strProject = "All"
End If

dteDateFrom = FormatDateU(Request("DateFrom"), False)
dteDateTo = FormatDateU(Request("DateTo"), False)

%>
<!--#include virtual="/System/ssi_dbConn_Open.inc"-->
<html>
<head>
	<title>MyDesk</title>
	<META http-equiv="Cache-Control" content="no-cache">
	<META http-equiv="Expires" content="0">
	<META http-equiv="Pragma" content="no-cache">
	<style> body, html {Margin:0px; padding: 0px; overflow: hidden;font: menu;border: none;} </style>
	<link href="/System/Style2.css" rel="stylesheet" type="text/css" ></link>
	<link href="/System/grid.css" rel="stylesheet" type="text/css" ></link>
	<script src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
	<script src="/System/grid.js"></script>
	<script src="/System/paging1.js"></script>

	<!-- grid format -->
	<style>
		.active-controls-grid {height: 210px; font: menu;}
		.active-column-0 {width: 0px;}
<%
If strCode = "All" Then
%>
		.active-column-1 {width: 100px;}
<%
Else
%>
		.active-column-1 {width: 0px;}
<%
End If
%>
		.active-column-2 {width: 90px; text-align: left;}
		.active-column-3 {width: 90px; text-align: left;}
		.active-column-4 {width: 90px; text-align: left;}
		.active-column-5 {width: 100px; text-align: left;}
		.active-column-6 {width: 80px; text-align: left;}
		.active-column-7 {width: 45px; text-align: left;}
		.active-column-8 {width: 80px; text-align: left;}
		.active-column-9 {width: 80px; text-align: left;}
		.active-column-10 {width: 80px; text-align: left;}
		.active-column-11 {width: 80px; text-align: left;}
		.active-column-12 {width: 80px; text-align: right;}
		.active-column-13 {width: 80px; text-align: right;}
		.active-grid-column {border-right: 1px solid threedshadow;}
		.active-grid-row {border-bottom: 1px solid threedlightshadow;}
	</style>

<%

function activewidgets_grid(name, oRecordset)

	Dim i, columns, rows, s
	Dim column_count, row_count

	column_count = oRecordset.fields.count

	columns = "var " & name & "Columns = [" & vbNewLine
	For i=0 to (column_count-1)
		If i = (column_count-1) Then
			columns = columns & """" & activewidgets_html("", oRecordset(i).name, "")  & """ "
		Else
			columns = columns & """" & activewidgets_html("", oRecordset(i).name, "")  & """, "
		End If
	Next
	columns = columns & vbNewLine & "];" & vbNewLine

	row_count = 0
	rows = "var " & name & "Data = [" & vbNewLine
	Do while (Not oRecordset.eof)
		row_count = row_count + 1
		rows = rows & "["
		For i=0 to (column_count-1)
			If i = (column_count-1) Then
				rows = rows & """" & activewidgets_html(oRecordset("Sales Project #"), oRecordset(i), oRecordset(i).name) & """ "
			Else
				rows = rows & """" & activewidgets_html(oRecordset("Sales Project #"), oRecordset(i), oRecordset(i).name) & """, "
			End If
		Next
'		If row_count = 20 Then
'			rows = rows & "]" & vbNewLine
'		Else
			rows = rows & "]," & vbNewLine
'		End If
		oRecordset.MoveNext
	Loop
	rows = rows & "];" & vbNewLine

	s = vbNewLine
	s = s & rows & vbNewLine
	s = s & columns & vbNewLine

'	s = s & "</" & "script" & ">" & vbNewLine

	activewidgets_grid = s
	
	nColumns = column_count
	nRows = row_count

end function

function activewidgets_html(Id, s, FieldName)

	If Not IsNull(s) Then
		If IsNumeric(s) And InStr(FieldName, "$") > 0 Then
			s = FormatCurrency(s, 2)
		ElseIf IsDate(s) Then
			s = FormatDateU2(s, False)
		ElseIf s = "ACTION" Then
			s = "<a href='#' onclick='ViewSalesProject(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & Id & ");'>View</a> | <a href='" & Request.Cookies("ClientSettings")("WorkingDir") & "/SalesProjects/Edit.asp?SalesProjectId=" & Id & "' target='_parent'>Edit</a> | <a href='#' onclick='deleteRecord(" & Id & ");'>Delete</a>"
		ElseIf s = "HISTORY" Then
			Dim rsHistory
			Dim sql_History
			Dim strInitials
			Dim dteDate
			Dim strComment
			Dim intTableId
			
			intTableId = 5
			
			Set rsHistory = Server.CreateObject("ADODB.RecordSet")
			sql_History = "Select Top 1 Comments.*, Users.* From Comments Inner Join Users On Users.Code = Comments.FromCode Where Comments.ItemId = " & Id & " And Comments.TableId = " & intTableId & " Order By Comments.DateEntered Desc"
			Set rsHistory = dbConn.Execute(sql_History)
			
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
			Dim sql
			Dim strDescription
		
			intTableId = 5
			
			Set rsFiles = Server.CreateObject("ADODB.RecordSet")
			sql = "Select Top 1 TableFiles.*, Users.* From TableFiles Inner Join Users On Users.Code = TableFiles.Code Where TableFiles.ItemId = " & Id & " And TableFiles.TableId = " & intTableId & " Order By TableFiles.DateEntered Desc"
			Set rsFiles = dbConn.Execute(sql)
			
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
<body style="background-color:#eeeeee;">

<script>
<%

Dim oRecordset
Dim sql

' Execute a SQL query
sql = "SELECT SalesProjects.SalesProjectId As [Sales Project #], Users.Name As [Sales Rep], Contacts_WithCustomers.Company + ' - ' + Contacts_WithCustomers.Surname + ', ' + Contacts_WithCustomers.FirstName AS [Contact], SalesProjects.Project, SalesProjects.Product As [Product/Service], 'ACTION' AS [Action], SalesProjects.Value+(SalesProjects.AmountPerMonth*SalesProjects.NumberOfMonths) AS [Value ($)], SalesProjects.NumberOfMonths AS [Months], iif(SalesProjects.AcceptedDate > #01-Jan-01#, 'ACCEPTED', iif(SalesProjects.RejectedDate > #01-Jan-01#, 'REJECTED', iif(SalesProjects.ProspectDate > #01-Jan-01#, 'PROSPECT', iif(SalesProjects.TenderDate > #01-Jan-01#, 'Tender', 'IN PROGRESS')))) As [Status], SalesProjects.DateEntered As [Date Entered], SalesProjects.PotentialOrderDate As [Potential Order Date], SalesProjects.Comment, 'HISTORY' AS History, 'FILES' As [Files] FROM Users INNER JOIN (SalesProjects INNER JOIN Contacts_WithCustomers ON SalesProjects.ContactId = Contacts_WithCustomers.ContactId) ON Users.Code = SalesProjects.Code"
sql = sql & " WHERE Users.Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ") AND SalesProjects.DateEntered >= #" & dteDateFrom & "# AND SalesProjects.DateEntered <= #" & dteDateTo & "#"
If strCode <> "All" Then
	sql = sql & " AND SalesProjects.Code = '" & strCode & "'"
End If
If intCompanyId <> 0 Then
	sql = sql & " AND Contacts_WithCustomers.CompanyId = " & intCompanyId
End If
If strProject <> "All" Then
	sql = sql & " AND SalesProjects.Project = '" & strProject & "'"
End If
sql = sql & " ORDER BY SalesProjects.Customer"
Set oRecordset = dbConn.Execute(sql)
If oRecordset.BOF And oRecordset.EOF Then MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/NoRecords.asp")

' Write grid to the page
Response.write(activewidgets_grid("my", oRecordset))

' Close recordset and connection
oRecordset.close
dbConn.close

%>

	//	create the grid object
	var obj = new Active.Controls.Grid;

	//	replace the built-in row model with the new one (defined in the patch)
	obj.setModel("row", new Active.Rows.Page);

	obj.setProperty("row/count", <%= nRows %>);
	obj.setProperty("column/count", <%= nColumns %>);
	obj.setProperty("data/text", function(i, j){return myData[i][j]});
	obj.setProperty("column/texts", myColumns);

	//	set page size
	obj.setProperty("row/pageSize", 10);

	//	write grid html to the page
	document.write(obj);

	</script>

	<table width="100%" cellspacing=0 cellpadding=5 height=40 bgcolor="#ebeadb">
		<tr>
			<td style="border-top:1px solid #cbc7b8;border-bottom:1px solid #cbc7b8;" valign="middle">
				<!-- bottom page control buttons -->
				<center>
					<div>
						<button onclick='goToPage(-Infinity)' ID="Button1" style="font-size:10px;">&lt;&lt;</button>&nbsp;
						<button onclick='goToPage(-1)' ID="Button2" style="font-size:10px;">&lt;</button>
						&nbsp;&nbsp;<span id='pageLabel' style="font-size:10px;"></span>&nbsp;&nbsp;
						<button onclick='goToPage(1)' ID="Button3" style="font-size:10px;">&gt;</button>&nbsp;
						<button  onclick='goToPage(Infinity)' ID="Button4" style="font-size:10px;">&gt;&gt;</button>
					</div>
				</center>
			</td>
		</tr>
	</table>

	<!-- button click handler -->
	<script>
	function goToPage(delta){
		var count = obj.getProperty("row/pageCount");
		var number = obj.getProperty("row/pageNumber");
		number += delta;
		if (number < 0) {number = 0}
		if (number > count-1) {number = count-1}
		document.getElementById('pageLabel').innerHTML = "Page " + (number + 1) + " of " + count + " ";
		obj.setProperty("row/pageNumber", number);
		obj.refresh();
	}
	goToPage(0);
	</script>
</body>
</html>

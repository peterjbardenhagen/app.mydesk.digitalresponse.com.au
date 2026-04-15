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
Dim dteDate
Dim intMonth
Dim intYear
Dim intCompanyId
Dim sql
Dim rsCheck

If Request.Cookies("UserSettings")("Manager") Then
	strCode = Trim(Request("Code"))
Else
	strCode = Request.Cookies("UserSettings")("Code")
End If

dteDate =		Request("ExpenseFormDate")
intMonth =		Month(dteDate)
intYear =		Year(dteDate)
intCompanyId =	CLng(Request("CompanyId"))

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
		.active-column-2 {width: 150px; text-align: left;}
		.active-column-3 {width: 90px; text-align: left;}
		.active-column-4 {width: 110px; text-align: left;}
		.active-column-5 {width: 100px; text-align: left;}
		.active-column-6 {width: 100px; text-align: left;}
		.active-column-7 {width: 80px; text-align: right;}
		.active-column-8 {width: 80px; text-align: right;}
		.active-column-9 {width: 80px; text-align: left;}
		.active-column-10 {width: 80px; text-align: left;}
		.active-column-11 {width: 80px; text-align: left;}
		.active-column-12 {width: 80px; text-align: left;}
		.active-column-13 {width: 80px; text-align: left;}
		.active-column-14 {width: 80px; text-align: left;}
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
				rows = rows & """" & activewidgets_html(oRecordset("Expense #"), oRecordset(i), oRecordset(i).name) & """ "
			Else
				rows = rows & """" & activewidgets_html(oRecordset("Expense #"), oRecordset(i), oRecordset(i).name) & """, "
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
		If InStr(FieldName, "$") > 0 Then
			s = FormatCurrency(s, 2)
		ElseIf IsDate(s) Then
			s = FormatDateU2(s, False)
		ElseIf s = "False" Then
			s = "No"
		ElseIf s = "True" Then
			s = "Yes"
		ElseIf s = "ACTION" Then
			s = "<a href='../Expenses/View.asp?ExpenseId=" & Id & "' target='_parent'>View</a> | <a href='" & Request.Cookies("ClientSettings")("WorkingDir") & "/Expenses/Edit.asp?ExpenseId=" & Id & "' target='_parent'>Edit</a> | <a href='#' onclick='deleteRecord(" & Id & ");'>Delete</a>"
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
<body style="background-color:#eeeeee;">
<%
' Check for sign off first
Set rsCheck = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM ExpensesSignOffs WHERE [Month] = " & intMonth & " AND [Year] = " & intYear & " AND Code = '" & strCode & "'"
Set rsCheck = dbConn.Execute(sql)
If Not(rsCheck.BOF and rsCheck.EOF) Then
	Response.Write "<table height=30 bgcolor=""#ebeadb"" width=""100%"" cellpadding=3 cellspacing=0 border=0><tr><td><b>Status:</b> Signed off</td></tr><tr><td bgcolor=""#cbc7b8""></td></tr></table>"
Else
	Response.Write "<table height=30 bgcolor=""#ffcc66"" width=""100%"" cellpadding=3 cellspacing=0 border=0><tr><td style=""color:black;""><b>Status:</b> Not signed off</td></tr><tr><td bgcolor=""#cbc7b8""></td></tr></table>"
End If
If IsObject(rsCheck) Then
	rsCheck.Close
	Set rsCheck = Nothing
End If
%>
<script>
<%

Dim oRecordset

' Execute a SQL query
sql = "SELECT Eid As [Expense #], Users.Name, Description, Comment, ExpenseTypes.ExpenseType As [Expense Type], 'ACTION' As [Action], Contacts_WithCustomersAndSuppliers_V2.CompanyName + ' - ' + Contacts_WithCustomersAndSuppliers_V2.Surname + ', ' + Contacts_WithCustomersAndSuppliers_V2.FirstName As [Customer], CostIncGST As [Cost Inc GST ($)], GST As [GST ($)], DateEntered As [Date Entered], ExpenseDate As [Expense Date], FBTTTL As [Company Staff], FBTNon As [Non Company Staff], TTLCorporateCard As [Corporate Card] FROM ExpenseTypes INNER JOIN (Users INNER JOIN (Contacts_WithCustomersAndSuppliers_V2 INNER JOIN Expenses ON Contacts_WithCustomersAndSuppliers_V2.ContactId = Expenses.ContactId) ON Users.Code = Expenses.Code) ON ExpenseTypes.ExpenseTypeId = Expenses.ExpenseTypeId WHERE Month(Expenses.ExpenseDate) = " & intMonth & " AND Year(Expenses.ExpenseDate) = " & intYear
If strCode <> "All" Then
	sql = sql & " AND Expenses.Code = '" & strCode & "'"
End If
If intCompanyId <> 0 Then
	sql = sql & " AND Contacts_WithCustomersAndSuppliers_V2.CompanyId = " & intCompanyId
End If

sql = sql & " ORDER BY Expenses.Eid"
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
<%

Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.AddHeader "pragma","no-cache"
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-cache"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/Clients/SalesEngineTL/ssi_Security.inc"-->
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
Dim intInvoiceStatusId
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
intInvoiceStatusId = CInt(Request("InvoiceStatusId"))

%>
<!--#include virtual="/System/ssi_dbConn_Open.inc"-->
<html>
<head>
	<title>MyDesk</title>
	<META http-equiv="Cache-Control" content="no-cache">
	<META http-equiv="Expires" content="0">
	<META http-equiv="Pragma" content="no-cache">
	<meta http-equiv="X-UA-Compatible" content="IE=8" />
	<style> body, html {Margin:0px; padding: 0px; overflow: hidden;font: menu;border: none;} </style>
	<link href="/System/Style2.css" rel="stylesheet" type="text/css" ></link>
	<link href="/System/grid.css" rel="stylesheet" type="text/css" ></link>
	<script src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
	<script src="/System/grid.js"></script>
	<script src="/System/paging1.js"></script>

	<!-- grid format -->
	<style>
		.active-controls-grid {height: 210px; font: menu;}
		.active-column-0 {width: 55px;}
		.active-column-1 {width: 0px; text-align: left;}
		.active-column-2 {width: 150px; text-align: left;}
		.active-column-3 {width: 70px; text-align: left;}
		.active-column-4 {width: 180px; text-align: left;}
		.active-column-5 {width: 80px; text-align: left;}
		.active-column-6 {width: 90px; text-align: left;}
		.active-column-7 {width: 80px; text-align: right;}
		.active-column-8 {width: 80px; text-align: left;}
		.active-column-9 {width: 100px; text-align: right;}
		.active-column-10 {width: 80px; text-align: left;}
		.active-column-11 {width: 80px; text-align: right;}
		.active-column-12 {width: 80px; text-align: right;}
		.active-column-13 {width: 80px; text-align: left;}
		.active-column-14 {width: 200px; text-align: right;}
		.active-column-15 {width: 120px; text-align: right;}
		.active-column-16 {width: 120px; text-align: left;}
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
				rows = rows & """" & activewidgets_html(oRecordset("Invoice #"), oRecordset(i), oRecordset(i).name) & """ "
			Else
				rows = rows & """" & activewidgets_html(oRecordset("Invoice #"), oRecordset(i), oRecordset(i).name) & """, "
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
		If IsNumeric(s) And (FieldName = "Total Cost Ex GST ($)" Or FieldName = "Nett Price Total Ex GST ($)") Then
			s = FormatCurrency(s, 2)
		ElseIf s = "QUNEXTAPPROVER" Then
			s = GetInvoiceNextLineApprover(Id)
		ElseIf s = "QULASTAPPROVER" Then
			s = GetInvoiceLastLineApprover(Id)
		ElseIf IsNumeric(s) And (FieldName = "Margin") Then
			s = FormatNumber(s, 2) & "%"
		ElseIf IsDate(s) Then
			s = FormatDateU2(s, False)
		ElseIf s = "DESCRIPTION" Then
			Dim rsDesc
			Dim sql
			Dim strDesc
			Dim i
			
			Set rsDesc = Server.CreateObject("ADODB.RecordSet")
			sql = "Select * From InvoiceContents Where InvoiceId = " & Id
			Set rsDesc = dbConn.Execute(sql)
			
			If Not(rsDesc.BOF And rsDesc.EOF) Then
				i = 1
				strDesc = "This invoice includes the following:" & vbcrlf
				Do Until rsDesc.EOF
					strDesc = strDesc & rsDesc("Description") & vbcrlf
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
		ElseIf s = "ACTION" Then
			s = "<a href='#' onclick='ViewInvoice(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & Id & ");'>Invoice</a> | <a href='#' onclick='ViewInvoiceDeliveryNote(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & Id & ");'>Delivery Note</a> | <a href='#' onclick='deleteRecord(" & Id & ");'>Delete</a>"
		ElseIf s = "HISTORY" Then
			Dim rsHistory
			Dim strSql_History
			Dim strInitials
			Dim dteDate
			Dim strComment
			Dim intTableId
			
			intTableId = 10
			
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
		
			intTableId = 10
			
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
<body>

<script>
<%

Dim oRecordset
Dim strSql

' Execute a SQL query
strSql = "SELECT DISTINCT Invoices.InvoiceId As [Invoice #], Divisions.Division AS [Division], iif(Invoices.CompanyId = 142, Invoices.CCompany, Companies.Company) AS [Company], 'DESCRIPTION' AS [Description], 'ACTION' As Action, InvoiceStatus.InvoiceStatus As [Invoice Status], Invoices.InvoiceDate As [Invoice Date], Invoices.NettPriceTotal AS [Nett Price Total Ex GST ($)], Users.Name As [Invoiced By], 'HISTORY' AS History FROM ((Divisions INNER JOIN (Users INNER JOIN Invoices ON Users.Code = Invoices.Code) ON Divisions.DivisionId = Invoices.DivisionId) INNER JOIN Companies ON Companies.CompanyId = Invoices.CompanyId) INNER JOIN InvoiceStatus ON Invoices.InvoiceStatusId = InvoiceStatus.InvoiceStatusId WHERE "
If intDivisionId > 0 Then
	strSql = strSql & " Invoices.DivisionId = " & intDivisionId & " AND"
Else
	If Not(Request.Cookies("DivisionId") = 2) Then
		strSql = strSql & " Invoices.DivisionId = " & Request.Cookies("DivisionId") & " AND"
	End If
End If
If strCode <> "All" Then
	strSql = strSql & " Users.Code = '" & strCode & "' AND"
End If
If intCompanyId > 0 Then
	strSql = strSql & " Companies.CompanyId = " & intCompanyId & " AND"
End If
If intInvoiceStatusId > 0 Then
	If intInvoiceStatusId = 555 Then
		strSql = strSql & " Invoices.InvoiceStatusId Not In (2) AND"
	Else
		strSql = strSql & " Invoices.InvoiceStatusId = " & intInvoiceStatusId & " AND"
	End If
End If
strSql = strSql & " (Invoices.InvoiceDate >= #" & DBDate(dteDateFrom) & "# AND Invoices.InvoiceDate < #" & DBDate(dteDateTo) & "#) ORDER BY InvoiceId DESC"
Set oRecordset = dbConn.Execute(strSql)

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

<%

Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.AddHeader "pragma","no-cache"
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-cache"

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
Dim sql
Dim intJobOrderStatusCode
Dim strKeyword

strCode = Trim(Request("Code"))

If strCode = "" Then
	strCode = "All"
End If

intDivisionId = CInt(Request("DivisionId"))
intCompanyId = CLng(Request("CompanyId"))

If strProject = "" Then
	strProject = "All"
End If

dteDateFrom = FormatDateU(Request("DateFrom"), False)
dteDateTo = FormatDateU(Request("DateTo"), False)
intJobOrderStatusCode = CInt(Request("JobOrderStatusCode"))
strKeyword = Trim(Request("Keyword"))

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
		.active-column-0 {width: 50px; text-align: left;}
		.active-column-1 {width: 50px; text-align: left;}
		.active-column-2 {width: 50px; text-align: left;}
		.active-column-3 {width: 40px; text-align: left;}
		.active-column-4 {width: 40px; text-align: left;}
		.active-column-5 {width: 0px; text-align: left;}
		.active-column-6 {width: 120px; text-align: left;}
		.active-column-7 {width: 130px; text-align: left;}
		.active-column-8 {width: 90px; text-align: left;}
		.active-column-9 {width: 80px; text-align: left;}
		.active-column-10 {width: 75px; text-align: left;}
		.active-column-11 {width: 90px; text-align: left;}
		.active-column-12 {width: 80px; text-align: left;}
		.active-column-13 {width: 80px; text-align: left;}
		.active-column-14 {width: 80px; text-align: left;}
		.active-column-15 {width: 80px; text-align: left;}
		.active-column-16 {width: 80px; text-align: left;}
		.active-column-17 {width: 80px; text-align: left;}
		.active-column-18 {width: 150px; text-align: left;}
		.active-column-19 {width: 150px; text-align: left;}
		.active-grid-column {border-right: 1px solid threedshadow;}
		.active-grid-row {border-bottom: 1px solid threedlightshadow;}
	</style>

<%

function activewidgets_grid(name, oRecordset)

	Dim i, columns, rows, s
	Dim column_count, row_count
	Dim intQty

	column_count = oRecordset.fields.count

	columns = "var " & name & "Columns = [" & vbNewLine
	For i=0 to (column_count-1)
		If i = (column_count-1) Then
			columns = columns & """" & activewidgets_html("", "", "", "", oRecordset(i).name, "")  & """ "
		Else
			columns = columns & """" & activewidgets_html("", "", "", "", oRecordset(i).name, "")  & """, "
		End If
	Next
	columns = columns & vbNewLine & "];" & vbNewLine

	row_count = 0
	rows = "var " & name & "Data = [" & vbNewLine
	Do while (Not oRecordset.eof)
		If oRecordset("Units") <> 0 Then intQty = oRecordSet("Units") Else intQty = oRecordSet("Qty")
		row_count = row_count + 1
		rows = rows & "["
		For i=0 to (column_count-1)
			If i = (column_count-1) Then
				rows = rows & """" & activewidgets_html(oRecordset("Job #"), intQty, oRecordset("Slip #"), oRecordset("Job"), oRecordset(i), oRecordset(i).name) & """ "
			Else
				rows = rows & """" & activewidgets_html(oRecordset("Job #"), intQty, oRecordset("Slip #"), oRecordset("Job"), oRecordset(i), oRecordset(i).name) & """, "
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

function activewidgets_html(Id, intQty, JobOrderId, ItemLineT, s, FieldName)
	If Not IsNull(s) Then
		If IsNumeric(s) And (InStr(FieldName, "$") > 0) Then
			s = s * intQty
			s = FormatCurrency(s, 2)
		ElseIf IsNumeric(s) And (FieldName = "Margin") Then
			s = FormatNumber(s, 2) & "%"
		ElseIf IsDate(s) Then
			If Year(s) =< 1990 Then
				s = "Not set"
			Else
				s = "<!--" & DBDate(s) & "-->" & FormatDateU2(s, False)
			End If
		ElseIf FieldName = "Quote #" Then
			s = "<a href='#' onclick='ViewQuote(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & s & ")'>" & s & "</a>"
		ElseIf s = "ACTION" Then
			Dim boolTP
			If InStr(ItemLineT, "IT") Then
				boolTP = False
			Else
				boolTP = True
			End If
			s = "<a href='#' onclick='ViewJobOrder(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & JobOrderId & ");'>View</a> | <a href='../Invoices/Add.asp?JobOrderId=" & JobOrderId & "' target='_parent'>Invoice Job</a> | <a href='#' onclick='EditJobContent(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & LCase(boolTP) & ", " & JobOrderId & ", " & Id & ");'>Edit</a> | <a href='EditJobOrder.asp?JobOrderId=" & JobOrderId & "' target='_parent'>Edit Details</a>"
		ElseIf s = "DELIVERYADDRESS" Or s = "INVOICEADDRESS" Then
			Dim rsAdd
			sql = "Select * From JobOrders Where JobOrderId = " & JobOrderId
			Set rsAdd = dbConn.Execute(sql)
			If Not (rsAdd.BOF And rsAdd.EOF) Then
				If s = "DELIVERYADDRESS" Then
					s = rsAdd("DelAddress1")
					If rsAdd("DelAddress2") <> "" Then
						s = s & ", " & rsAdd("DelAddress2")
					End If
					s = s & " " & rsAdd("DelSuburb") & " " & rsAdd("DelPostCode")
				Else
					s = rsAdd("InvAddress1")
					If rsAdd("InvAddress2") <> "" Then
						s = s & ", " & rsAdd("InvAddress2") & " "
					End If
					s = s & " " & rsAdd("InvSuburb") & " " & rsAdd("InvPostCode")
				End If
			End If
			rsAdd.Close
			Set rsAdd = Nothing
		ElseIf s = "DESCRIPTION" Then
			Dim rsDesc
			Dim sql
			Dim strDesc
			Dim i
			
			Set rsDesc = Server.CreateObject("ADODB.RecordSet")
			sql = "Select Top 5 Quantity, Units, Days, Description From JobOrderContents Where JobOrderId = " & Id & " UNION Select Top 5 Quantity, 0 As Units, 0 As Days, Description From JobOrderThirdPartyContents Where JobOrderId = " & Id
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
				s = "<a href='#' onclick='alert(""" & Replace(Replace(strDesc, vbcrlf, "\n")&"","'","`") & """);'>Click here</a>"
			Else
				s = "No contents"
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
<body style="background-color:#eeeeee;">

<script>
<%

Dim oRecordset
Dim strSql

' Execute a SQL query

strSql = "SELECT DISTINCT JobOrderContents.JobOrderContentId AS [Job #], JobOrders.Qid AS [Quote #], JobOrderContents.JobOrderId AS [Slip #], 'IT' & [Job #] As [Job], JobOrderContents.Quantity As [Qty], JobOrderContents.Units, JobOrderContents.Description, 'ACTION' AS [Action], iif(JobOrders.Company<>'',JobOrders.Company,Companies.Company) AS [Customer], JobOrders.CustomerPO AS [Customer PO #], JobOrders.Project AS Project, UCase(JobOrderStatus.JobOrderStatus) As [Job Order Status], JobOrders.DateAccepted AS [Date Accepted], DateDiff('D', JobOrders.DateAccepted, Now()) As [Inactive (days)], JobOrderContents.UnitCost As [Job Cost Ex GST ($)], JobOrderContents.NettPrice As [Job Sell Price Ex GST ($)], JobOrderContents.DateDeliveryRequested AS [Delivery Requested], JobOrderContents.DateDeliveryScheduled AS [Delivery Scheduled], Users.Name AS Originator, 'DELIVERYADDRESS' AS [Delivery Address], 'INVOICEADDRESS' AS [Invoice Address] FROM (JobOrderStatus INNER JOIN (Users INNER JOIN ((Divisions INNER JOIN JobOrders ON Divisions.DivisionId = JobOrders.DivisionId) INNER JOIN JobOrderContents ON JobOrders.JobOrderId = JobOrderContents.JobOrderId) ON Users.Code = JobOrders.Code) ON JobOrderStatus.JobOrderStatusCode = JobOrderContents.JobOrderStatusCode) INNER JOIN Companies ON JobOrders.CompanyId = Companies.CompanyId WHERE "
If intDivisionId > 0 Then
	strSql = strSql & " JobOrders.DivisionId = " & intDivisionId & " AND"
Else
	If Not(Request.Cookies("DivisionId") = 2) Then
		strSql = strSql & " JobOrders.DivisionId = " & Request.Cookies("DivisionId") & " AND"
	End If
End If
If strKeyword <> "" Then
	strSql = strSql & " (JobOrderContents.Description LIKE '%" & strKeyword & "%' OR JobOrderContents.Comment LIKE '%" & strKeyword & "%' OR JobOrders.Project LIKE '%" & strKeyword & "%' OR JobOrders.DelCompany LIKE '%" & strKeyword & "%' OR JobOrders.InvCompany LIKE '%" & strKeyword & "%') AND"
End If
If strCode <> "All" Then
	strSql = strSql & " Users.Code = '" & strCode & "' AND"
End If
If intJobOrderStatusCode = 555 Then ' Working on
	strSql = strSql & "	JobOrderContents.JobOrderStatusCode < 70 AND"
ElseIf intJobOrderStatusCode = 0 Then
	' Do Nothing
Else
	strSql = strSql & "	JobOrderContents.JobOrderStatusCode = " & intJobOrderStatusCode & "AND"
End If
strSql = strSql & " (JobOrders.DateAccepted >= #" & DBDate(dteDateFrom) & "# AND JobOrders.DateAccepted < #" & DBDate(dteDateTo) & "#)"

strSql = strSql & " UNION ALL "

strSql = strSql & "SELECT DISTINCT JobOrderThirdPartyContents.JobOrderThirdPartyId AS [Job #], JobOrders.Qid AS [Quote #], JobOrderThirdPartyContents.JobOrderId AS [Slip #], 'IT' & [Job #] As [Job], JobOrderThirdPartyContents.Quantity As [Qty], 0 As [Units], JobOrderThirdPartyContents.Description, 'ACTION' AS [Action], iif(JobOrders.Company<>'',JobOrders.Company,Companies.Company) AS [Customer], JobOrders.CustomerPO AS [Customer PO #], JobOrders.Project AS Project, UCase(JobOrderStatus.JobOrderStatus) As [Job Order Status], JobOrders.DateAccepted AS [Date Accepted], DateDiff('D', JobOrders.DateAccepted, Now()) As [Inactive (days)], JobOrderThirdPartyContents.UnitCost As [Job Cost Ex GST ($)], JobOrderThirdPartyContents.NettPrice As [Job Sell Price Ex GST ($)], JobOrderThirdPartyContents.DateDeliveryRequested AS [Delivery Requested], JobOrderThirdPartyContents.DateDeliveryScheduled AS [Delivery Scheduled], Users.Name AS Originator, 'DELIVERYADDRESS' AS [Delivery Address], 'INVOICEADDRESS' AS [Invoice Address] FROM Companies INNER JOIN (JobOrderStatus INNER JOIN (Users INNER JOIN ((Divisions INNER JOIN JobOrders ON Divisions.DivisionId = JobOrders.DivisionId) INNER JOIN JobOrderThirdPartyContents ON JobOrders.JobOrderId = JobOrderThirdPartyContents.JobOrderId) ON Users.Code = JobOrders.Code) ON JobOrderStatus.JobOrderStatusCode = JobOrderThirdPartyContents.JobOrderStatusCode) ON Companies.CompanyId = JobOrders.CompanyId WHERE "
If intDivisionId > 0 Then
	strSql = strSql & " JobOrders.DivisionId = " & intDivisionId & " AND"
Else
	If Not(Request.Cookies("DivisionId") = 2) Then
		strSql = strSql & " JobOrders.DivisionId = " & Request.Cookies("DivisionId") & " AND"
	End If
End If
If strKeyword <> "" Then
	strSql = strSql & " (JobOrderThirdPartyContents.Description LIKE '%" & strKeyword & "%' OR JobOrderThirdPartyContents.Supplier LIKE '%" & strKeyword & "%' OR JobOrderThirdPartyContents.OurPartNumber LIKE '%" & strKeyword & "%' OR JobOrderThirdPartyContents.Supplier LIKE '%" & strKeyword & "%' OR JobOrders.Project LIKE '%" & strKeyword & "%' OR JobOrders.DelCompany LIKE '%" & strKeyword & "%' OR JobOrders.InvCompany LIKE '%" & strKeyword & "%') AND"
End If
If strCode <> "All" Then
	strSql = strSql & " Users.Code = '" & strCode & "' AND"
End If
If intJobOrderStatusCode = 555 Then ' Working on
	strSql = strSql & "	JobOrderThirdPartyContents.JobOrderStatusCode < 70 AND"
ElseIf intJobOrderStatusCode = 0 Then
	' Do Nothing
Else
	strSql = strSql & "	JobOrderThirdPartyContents.JobOrderStatusCode = " & intJobOrderStatusCode & "AND"
End If
strSql = strSql & " (JobOrders.DateAccepted >= #" & DBDate(dteDateFrom) & "# AND JobOrders.DateAccepted < #" & DBDate(dteDateTo) & "#)"
strSql = strSql & " ORDER BY [Slip #] DESC, [Job #] ASC"
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

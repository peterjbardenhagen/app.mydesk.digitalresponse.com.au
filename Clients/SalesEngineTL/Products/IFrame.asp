<%

Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.AddHeader "pragma","no-cache"
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-cache"

If Not Request.Cookies("UserSettings")("Manager") Then Response.Redirect("../Portal/AccessDenied.asp")

On Error Resume Next

%>
<!--#include virtual="/System/ssi_Security.inc"-->
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
Dim intProductCatId
Dim dteDateFrom
Dim dteDateTo

strCode = Request.Cookies("UserSettings")("Code")
intDivisionId = CInt(Request("DivisionId"))
intProductCatId = CInt(Request("ProductCatId"))
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
	<script src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>

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
</head>
<body style="background-color:#eeeeee;">

<%

Dim oRecordset
Dim sql

' Execute a SQL query
sql = "SELECT Products.ProductId AS [Product #], Divisions.Division AS [Division], ProductName AS [Product Name], ProductDesc AS [Product Description], UnitCost AS [Unit Cost Ex GST ($)], NettPrice AS [Nett Price Ex GST ($)], MinNettPrice AS [Minimum Nett Price Ex GST ($)], 'HISTORY' AS History, 'FILES' As Files, 'ACTION' As Action FROM Products INNER JOIN Divisions ON Divisions.DivisionId = Products.DivisionId WHERE ProductCatId = " & intProductCatId & " AND Divisions.DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") AND Divisions.DivisionId = " & intDivisionId & " ORDER BY ProductName, ProductDesc"
Set oRecordset = dbConn.Execute(sql)

If oRecordset.BOF And oRecordset.EOF Then MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/NoRecords.asp")

' Write grid to the page
Response.write(activewidgets_grid("my", oRecordset))

' Close recordset and connection
oRecordset.close
dbConn.close


Response.Write("<table cellspacing=0>")
Response.Write("<tr><td class='header'>Product #</td><td class='header'>Product Name</td><td class='header'>Description</td><td class='header'>Action</td></tr>")
Do While Not (oRecordset.EOF)
	Dim Id
	Dim Action
	Id = oRecordset("Product #")
	Action = "<a href='" & Request.Cookies("ClientSettings")("WorkingDir") & "/Products/Edit.asp?ProductId=" & Id & "' target='_parent'>Edit</a> | <a href='#' onclick='deleteRecord(" & Id & ");'>Delete</a>"
	Response.Write("<tr style='height:1px !important;background-color:white;'></tr>")
	Response.Write("<tr>")
	Response.Write("<td>" & oRecordset("Product #") & "</td>")
	Response.Write("<td>" & oRecordset("Product Name") & "</td>")
	Response.Write("<td>" & oRecordset("Description") & "</td>")
	Response.Write("<td>" & Action & "</td>")
	Response.Write("</tr>")
	oRecordset.MoveNext
Loop
Response.Write("</table>")

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

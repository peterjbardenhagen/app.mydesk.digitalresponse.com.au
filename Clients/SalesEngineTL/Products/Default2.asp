<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

If Not Request.Cookies("UserSettings")("Manager") Then Response.Redirect("../Portal/AccessDenied.asp")

Dim strSort
Dim strFilter_Code

If Request.Cookies("UserSettings")("Manager") Then
	strCode = Trim(Request("Code"))
	If strCode = "" Then
		strCode = "All"
	End If
Else
	strCode = Request.Cookies("UserSettings")("Code")
End If

If Request.Cookies("UserSettings")("Manager") Then
	strFilter_Code = Trim(Request("Filter_Code"))
	If strFilter_Code = "" Then
		strFilter_Code = Request.Cookies("UserSettings")("Code")
	End If
Else
	strFilter_Code = Request.Cookies("UserSettings")("Code")
End If

dteDateFrom = FormatDateU(DateAdd("M", -3, ServerToEST(Now())), False)
dteDateTo = FormatDateU(DateAdd("D", 1, ServerToEST(Now())), False)
intDivisionId = CInt(Request("DivisionId"))

If intDivisionId = 0 Then intDivisionId = 555

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
	</head>
	<body bgcolor="#dddddd">
<!--#include virtual="/System/ssi_Header.inc"-->
	<center>
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table3">
		<tr>
			<td>
				<br>
				<table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table4">
					<tr>
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / Products /></span></td>
						<td align="right"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Products/Add.asp" class="Header2">Add Product</a></td>
					</tr>
					<tr>
						<td colspan=2>
<%

strMsg = Trim(Request("Msg"))
If strMsg <> "" Then

%>
							<br>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table5">
								<tr>
									<td><span style="color:red;"><%= strMsg %></span></td>
								</tr>
							</table>
<%

End If

%>
						</td>
					</tr>
				</table>
				<table width=760>
					<tr>
						<td>
							<fieldset style="width:760px;">
								<legend style="font-weight:bold;">Filter</legend>
								<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table1">
									<form name="FormReport" id="FormReport" method="post" action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/SalesProjects/Report.asp" target="MyIFrame">
									<tr>
										<td nowrap style="font-weight:bold;" width=50>Product&nbsp;Category</td>
										<td nowrap>
										<select name="ProductCatId" ID="Select1">
											<option value="555">Select a product category</option>
<%

Set rsPCat = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM ProductCat WHERE DivisionId = " & intDivisionId & " ORDER BY ProductCat"
Set rsPCat = dbConn.Execute(sql)

If Not(rsPCat.BOF And rsPCat.EOF) Then
	Do Until rsPCat.EOF
		Response.Write ("								<option value=""" & rsPCat("ProductCatId") & """>" & rsPCat("ProductCat") & "</option>" & vbNewLine)
		rsPCat.MoveNext
	Loop
End If

If IsObject(rsPCat) Then
	rsPCat.Close
	Set rsPCat = Nothing
End If

%>
										</select>	
										</td>
										<td align="right">
										<input type="submit" value="Filter" ID="Submit2" NAME="Submit2" onclick="FormReport.action='IFrame.asp';FormReport.target='MyIFrame';">
										</td>
									</tr>
								</form>
								</table>
							</fieldset>
						</td>
					</tr>
				</table>
				<table width=100% cellpadding=0 cellspacing=0 border=0 ID="Table2">
					<tr>
						<td>
						<iframe scrolling="yes" style="width:100%;height:550px;overflow:scroll;scroll-y:auto;" id="MyIFrame" name="MyIFrame" src="IFrame.asp?Cache=<%= rnd() %>&CurPage=<%= CurPage %>&Code=<%= strFilter_Code %>&DateFrom=<%= dteDateFrom %>&DateTo=<%= dteDateTo %>&DivisionId=<%= intDivisionId %>&ProductCatId=555"></iframe>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
	</center>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
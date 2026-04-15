<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg

strMsg = Trim(Request("Msg"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
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
		<script language="javascript">
		</script>
	</head>
	<body bgcolor="#dddddd">
<!--#include virtual="/System/ssi_Header.inc"-->
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp">Reports</a> / <a href="SalesReportGen.asp">Sales Reports</a> / Upload Data /></span>
<%

If strMsg <> "" Then

%>
				<br><br>
				<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table2">
					<tr>
						<td><span style="color:red;"><%= strMsg %></span></td>
					</tr>
				</table>
<%

End If

%>
				<table width=100% cellpadding=0 cellspacing=0 border=0 ID="Table1">
					<tr>
						<td>
							<table width="770" cellpadding=3 cellspacing=0 border=0 ID="Table3">
								<tr>
									<td>
									<br>
										The format must be comma delimitted (with double quote qualifiers) or tab delimitted, with no header row. The columns must in the following order: MyDesk Code, Month, Value. Please note that this will overwrite any previously stored values.<br><br>
										<table ID="Table5">
										<form name="Form1" method="post" action="SalesReport.asp" onsubmit="checkForm();" ID="Form1">
											<tr>
												<td width=100 style="font-weight:bold;">File</td>
												<td><input type="file"></td>
											</tr>
											<tr>
												<td colspan=2 align="right"><br><input type="submit" value="Upload"></td>
											</tr>
										</table>
									</td>
								</tr>
							</table>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
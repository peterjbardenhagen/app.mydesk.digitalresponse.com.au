<% 

Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

If Not Request.Cookies("UserSettings")("Manager") Then Response.Redirect("../Portal/AccessDenied.asp")

Dim strSort
Dim strFilter_Code

intDivisionId = CInt(Request("DivisionId"))
strLetter = Trim(Request("Letter"))

If intDivisionId = 0 Then intDivisionId = CInt(Request.Cookies("DivisionId"))
If strLetter = "" Then strLetter = "A"

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<!DOCTYPE html>
<html lang="en">
	<head>
		<title>Companies - Techlight MyDesk</title>
		<meta charset="UTF-8">
		<meta name="viewport" content="width=device-width, initial-scale=1.0">
		<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
		<meta http-equiv="Expires" content="0">
		<meta http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="preconnect" href="https://fonts.googleapis.com">
		<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
		<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Style_Techlight.css">
		<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
	</head>
	<body class="tl-bg-light">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
	<center>
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table3">
		<tr>
			<td>
				<br>
				<table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table4">
					<tr>
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a> / Companies /></span></td>
						<td align="right"><a href="Add.asp" class="Header2">Add Company</a></td>
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
									<form name="FormReport" id="FormReport" method="post" action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/SalesProjects/Report.asp" target="winResults">
									<tr>
										<td style="font-weight:bold;" width=50>Division</td>
										<td>
										<select name="DivisionId" ID="Select1">
											<option value="555">Select a division</option>
<%

Set rsDiv = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Divisions WHERE DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") ORDER BY Division"
Set rsDiv = dbConn.Execute(sql)

If Not(rsDiv.BOF And rsDiv.EOF) Then
	Do Until rsDiv.EOF
		If CLng(intDivisionId) = CLng(rsDiv("DivisionId")) Then
			Response.Write ("								<option selected value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		Else
			Response.Write ("								<option value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		End If
		rsDiv.MoveNext
	Loop
End If

If IsObject(rsDiv) Then
	rsDiv.Close
	Set rsDiv = Nothing
End If

%>
										</select>	
										</td>
										<td style="font-weight:bold;" width=50>Letter</td>
										<td>
										<select name="Letter" ID="Select2">
											<option value="">Select a letter</option>
											<option value="A">A</option>
											<option value="B">B</option>
											<option value="C">C</option>
											<option value="D">D</option>
											<option value="E">E</option>
											<option value="F">F</option>
											<option value="G">G</option>
											<option value="H">H</option>
											<option value="I">I</option>
											<option value="J">J</option>
											<option value="K">K</option>
											<option value="L">L</option>
											<option value="M">M</option>
											<option value="N">N</option>
											<option value="O">O</option>
											<option value="P">P</option>
											<option value="Q">Q</option>
											<option value="R">R</option>
											<option value="S">S</option>
											<option value="T">T</option>
											<option value="U">U</option>
											<option value="V">V</option>
											<option value="W">W</option>
											<option value="X">X</option>
											<option value="Y">Y</option>
											<option value="0">0</option>
											<option value="1">1</option>
											<option value="2">2</option>
											<option value="3">3</option>
											<option value="4">4</option>
											<option value="5">5</option>
											<option value="6">6</option>
											<option value="7">7</option>
											<option value="8">8</option>
											<option value="9">9</option>
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
						<iframe width=100% height=250 name="MyIFrame" src="IFrame.asp?Cache=<%= rnd() %>&CurPage=<%= CurPage %>&Letter=<%= strLetter %>&DivisionId=<%= intDivisionId %>"></iframe>
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
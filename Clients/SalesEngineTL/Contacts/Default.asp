<% 

Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

Dim strMsg

strMsg = Trim(Request("Msg"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim strSort
Dim strFilter_Code
Dim intCompanyId

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

%>
<html>
	<head>
		<title>SalesEngine</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
	</head>
	<body bgcolor="#dddddd">
<!--#include virtual="/System/ssi_Header.inc"-->
	<center>
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0>
		<tr>
			<td>
				<br>
				<table width="100%" cellpadding=0 cellspacing=0 border=0>
					<tr>
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / Contacts /></span></td>
						<td align="right"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Contacts/Add.asp" class="Header2">Add Contact</a></td>
					</tr>
				</table>
				<table width=100% cellpadding=0 cellspacing=0 border=0 ID="Table1">
					<tr>
						<td>
<%

If strMsg <> "" Then

%>
							<br>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table3">
								<tr>
									<td><span style="color:red;"><%= strMsg %></span></td>
								</tr>
							</table>
<%

End If

%>
							<fieldset style="width:750px;">
								<legend style="font-weight:bold;">Filter</legend>
								<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
									<form name="FormReport" id="FormReport" method="post" action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Contacts/IFrame.asp" target="MyIFrame">
									<input type="hidden" name="Cache" value="<%= rnd() %>" ID="Hidden1">
									<tr>
<%

If Request.Cookies("UserSettings")("Manager") Then

%>
										<td style="font-weight:bold;">User</td>
										<td>
											<select name="Code" ID="Select4">
											<option value="All">All users</option>
<%
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND (Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")) Order By Name"
	Set rsUsers = dbConn.Execute(sql)

	If Not(rsUsers.BOF And rsUsers.EOF) Then
		Do Until rsUsers.EOF
			If rsUsers("Code") = strFilter_Code Then
%>
											<option selected value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
			Else
%>
											<option value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
			End If	
			rsUsers.MoveNext
		Loop
	End If

	rsUsers.Close
	Set rsUsers = Nothing

%>
											</select>
										</td>
<%

Else

%>
										<input type="hidden" name="Code" id="Code" value="<%= Request.Cookies("UserSettings")("Code") %>">
<%

End If

Set rsCompany = Server.CreateObject("ADODB.RecordSet")
sql = "Select DistinctRow Companies.CompanyId, Companies.Company From Contacts Inner Join Companies On Companies.CompanyId = Contacts.CompanyId Where Companies.CompanyId <> 142 And ((Companies.DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ")) Or Contacts.Code = '" & Request.Cookies("UserSettings")("Code") & "') Order By Companies.Company"
Set rsCompany = dbConn.Execute(sql)

%>
										<td style="font-weight:bold;">Company</td>
										<td valign="top">
										<select name="CompanyId" style="width:250px;" id="Select1">
											<option value="0">All companies</option>
											<option value="142">Not an account</option>
											<option value="0"></option>
<%

If Not(rsCompany.BOF And rsCompany.EOF) Then
	Do Until rsCompany.EOF
		Response.Write "<option value=""" & rsCompany("CompanyId") & """>" & rsCompany("Company") & "</option>" & vbcrlf
		rsCompany.MoveNext
	Loop
End If

rsCompany.Close
Set rsCompany = Nothing

%>
										</select>										
										</td>
									</tr>
									<tr>
										<td style="font-weight:bold;" width=50>Letter</td>
										<td>
										<select name="Letter" ID="Select2">
											<option value="All" selected>All</option>
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
										<td style="font-weight:bold;" width=50>By</td>
										<td>
										<select name="By" ID="Select3">
											<option value="CompanyName">Company Name</option>
											<option value="Surname">Surname of Contact</option>
										</select>
										</td>
									</tr>
									<tr>
										<td align="right" colspan=4><input onclick="FormReport.action='EmailList.asp';this.form.submit();" type="button" value="Email List" ID="Button1" NAME="Button1"> <input type="button" onclick="FormReport.action='Report.asp';this.form.submit();" value="Generate Report" ID="Button2" NAME="Button1"> <input type="submit" value="Filter" ID="Submit1" NAME="Submit2" onclick="FormReport.action='IFrame.asp';FormReport.target='MyIFrame';"></td>
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
						<iframe id="MyIFrame" name="MyIFrame" scrolling="yes" style="width:100%;height:550px;overflow:scroll;scroll-y:auto;" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Contacts/IFrame.asp?Cache=<%= rnd() %>&Code=<%= strFilter_Code %>&Company=All&Letter=All" name="MyIFrame" id="MyIFrame"></iframe>
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

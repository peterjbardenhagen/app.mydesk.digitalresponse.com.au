<%
Dim strMainBgColor
Dim strBgColor2
Dim strBgColor3
Dim strWorkingDir

If Request.Cookies("LoggedIn")&"" <> "" Then
	strMainBgColor = Request.Cookies("ClientSettings")("MainBgColor")
	strBgColor2 = Request.Cookies("ClientSettings")("BgColor2")
	strBgColor3 = Request.Cookies("ClientSettings")("BgColor3")
	strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
Else
	strMainBgColor = Session("MainBgColor")
	strBgColor2 = Session("BgColor2")
	strBgColor3 = Session("BgColor3")
	strWorkingDir = Session("WorkingDir")
End If
%>
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style.css">
		<script language="javascript" src="<%= strWorkingDir %>/System/Global.js"></script>
		<script language="javascript">
		function jump(form) {
			var s = form.NavVal[form.NavVal.selectedIndex].value;
			if(!(s == '#')){
				if(!(s.indexOf('LogOff')>0)){
					parent.window.location.href = s;
				} else {
					parent.window.location.href = s;
				}
			}
		}
		</script>
	</head>
	<body bgcolor="#dddddd">
<%
Randomize()

If Request.Cookies("LoggedIn")&"" <> "" Then
	If CBool(Request.Cookies("LoggedIn")) Then
%>
	<div style="position:absolute;right:15px;top:117px;">
	<table width=150 ID="Table3">
		<tr>
			<td nowrap style="font-size:12px;color:white;font-weight:bold;">Logged in as <%= Request.Cookies("UserSettings")("Name") %> <% If Request.Cookies("UserSettings")("LineManagerName") <> "" Then %> Your manager is <%= Request.Cookies("UserSettings")("LineManagerName") %> - <a href="mailto:<%= Request.Cookies("UserSettings")("LineManagerEmail") %>" style="font-size:12px;color:white;">Email</a><% End If %></td>
		</tr>
	</table>
	</div>
<%

	End If
End If

%>

	<table bgcolor="<%= strMainBgColor %>" width=100% height="60" cellpadding=0 cellspacing=0 border=0 ID="Table1">
		<tr height=99>
			<td colspan=2 background="<%= strWorkingDir %>/Images/Header_1_Bg.gif">
				<table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table2">
					<tr>
						<td valign="top"><a href="#" onclick="parent.window.location.href='<%= strWorkingDir %>';"><img src="<%= strWorkingDir %>/Images/Header_1_1.gif" border=0 alt=""></a></td>
						<td align="right" valign="top"><img src="<%= strWorkingDir %>/Images/Header_1_2.gif" border=0 alt=""></td>
					</tr>
				</table>
			</td>
		</tr>
		<tr height=10>
			<td valign="top" bgcolor="<%= strBgColor2 %>" background="<%= strWorkingDir %>/Images/Header_Bg.gif"><img src="<%= strWorkingDir %>/Images/Header_2.gif" border=0 alt=""></td>
			<td valign="top" align="right" bgcolor="<%= strBgColor3 %>" background="<%= strWorkingDir %>/Images/Header_Bg.gif"></td>
		</tr>
		<form name="frmNavigate" id="frmNavigate">
		<tr height=25>
			<td colspan=2 valign="top" bgcolor="<%= strBgColor3 %>" nowrap>
<%
If Request.Cookies("LoggedIn")&"" <> "" Then
	If Request.Cookies("LoggedIn") Then
%>
				<table width=98% align="center">
					<tr>
						<td style="font-size:13px;color:white;" width=70>Navigate</td>
						<td>
						<select name="NavVal" id="NavVal" onChange="jump(this.form);" style="width:200px;font-size:14px;">
							<option value="#"></option>
							<option value="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/?Page=<%= Request.Cookies("ClientSettings")("WorkingDir") %>/PortalFrame.asp?Cache=<%= Rnd() %>">Home</option>
							<option value='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/?Page=<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Contacts/?Cache=<%= Rnd() %>'>Contacts</option>
<!--							<option value='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/?Page=<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Products/?Cache=<%= Rnd() %>'>Products</option>-->
							<option value='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/?Page=<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Quotes/?Cache=<%= Rnd() %>'>Quotes</option>
							<option value='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/?Page=<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Invoices/?Cache=<%= Rnd() %>'>Invoices</option>
							<option value='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/?Page=<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Purchasing/?Cache=<%= Rnd() %>'>Purchasing</option>
							<option value='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/?Page=<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup/?Cache=<%= Rnd() %>'>Setup</option>
							<option value='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/?Page=<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Users/?Cache=<%= Rnd() %>'>Users</option>
							<option value='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Portal/LogOff.asp?Cache=<%= Rnd() %>'>Log Off</option>
						</select>
						</td>
					</tr>
				</table>
<%
	End If
End If
%>
			</td>
		</tr>
			</form>
		<tr height=8>
			<td colspan=2 bgcolor="<%= strBgColor3 %>" background="/Images/Header_3.gif"><img src="/Images/Spacer.gif" width=1 height=8 border=0 alt=""></td>
		</tr>
	</table>
	</body>
</html>
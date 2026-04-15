<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

strCategorySelect = GetFilesCategories(0)

%>
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<script language="JavaScript">

		function emptyField(textObj) {
			if (textObj.value.length == 0) return true;
			for (var i=0; i < textObj.value.length; i++) {
				var ch = textObj.value.charAt(i);
				if (ch != ' ' && ch != '\t') return false;
			}
			return true
		}

		function checkForm() {

			var validFlag = true

			if (validFlag) {
				if (emptyField(document.Form1.File1) && emptyField(document.Form1.File2) && emptyField(document.Form1.File3) && emptyField(document.Form1.File4) && emptyField(document.Form1.File5) && emptyField(document.Form1.File6)) {
					alert("Please ensure that you are uploading at least one file.");
					validFlag = false;
				}
			}

			if (validFlag) {
				if ((emptyField(document.Form1.File1) && (!emptyField(document.Form1.File2))) || (!emptyField(document.Form1.File3) && (emptyField(document.Form1.File1) || emptyField(document.Form1.File2)))) {
					alert("Please ensure that you have selected a file for File #1 before selecting a file for File #2.");
					validFlag = false;
				}
			}

			if (validFlag) {
				if ( (emptyField(document.Form1.File1) || emptyField(document.Form1.File2)) && !emptyField(document.Form1.File3) ) {
					alert("Please ensure that you have selected a file for File #1 and File #2 before selecting a file for File #3.");
					validFlag = false;
				}
			}

			if (validFlag) {
				if ( (emptyField(document.Form1.File1) || emptyField(document.Form1.File2) || emptyField(document.Form1.File3)) && !emptyField(document.Form1.File4) ) {
					alert("Please ensure that you have selected a file for File #1, File #2, and File #3 before selecting a file for File #4.");
					validFlag = false;
				}
			}

			if (validFlag) {
				if ( (emptyField(document.Form1.File1) || emptyField(document.Form1.File2) || emptyField(document.Form1.File3) || emptyField(document.Form1.File4)) && !emptyField(document.Form1.File5) ) {
					alert("Please ensure that you have selected a file for File #1, File #2, File #3, and File #4 before selecting a file for File #5.");
					validFlag = false;
				}
			}

			if (validFlag) {
				if ( (emptyField(document.Form1.File1) || emptyField(document.Form1.File2) || emptyField(document.Form1.File3) || emptyField(document.Form1.File4) || emptyField(document.Form1.File5)) && !emptyField(document.Form1.File6) ) {
					alert("Please ensure that you have selected a file for File #1, File #2, File #3, File #4 and File #5 before selecting a file for File #6.");
					validFlag = false;
				}
			}

			if (validFlag) {
				if (!emptyField(document.Form1.File1)) {
					if (emptyField(document.Form1.File1_Desc) || emptyField(document.Form1.File1_Category)) {
						alert("Please check that you have entered a Description and selected a Category for File #1");
						validFlag = false;
					}
				}
			}

			if (validFlag) {
				if (!emptyField(document.Form1.File2)) {
					if (emptyField(document.Form1.File2_Desc) || emptyField(document.Form1.File2_Category)) {
						alert("Please check that you have entered a Description and selected a Category for File #2");
						validFlag = false;
					}
				}
			}

			if (validFlag) {
				if (!emptyField(document.Form1.File3)) {
					if (emptyField(document.Form1.File3_Desc) || emptyField(document.Form1.File3_Category)) {
						alert("Please check that you have entered a Description and selected a Category for File #3");
						validFlag = false;
					}
				}
			}

			if (validFlag) {
				if (!emptyField(document.Form1.File4)) {
					if (emptyField(document.Form1.File4_Desc) || emptyField(document.Form1.File4_Category)) {
						alert("Please check that you have entered a Description and selected a Category for File #3");
						validFlag = false;
					}
				}
			}

			if (validFlag) {
				if (!emptyField(document.Form1.File5)) {
					if (emptyField(document.Form1.File5_Desc) || emptyField(document.Form1.File5_Category)) {
						alert("Please check that you have entered a Description and selected a Category for File #3");
						validFlag = false;
					}
				}
			}

			if (validFlag) {
				if (!emptyField(document.Form1.File6)) {
					if (emptyField(document.Form1.File6_Desc) || emptyField(document.Form1.File6_Category)) {
						alert("Please check that you have entered a Description and selected a Category for File #3");
						validFlag = false;
					}
				}
			}
			return validFlag
		}

		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/FilesLibrary">Files Library</a> / Add File /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();" ENCTYPE="multipart/form-data">
								<tr>
									<td></td>
									<td valign="middle" style="font-weight:bold;"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>File #1</td>
									<td valign="middle" style="font-weight:bold;"><table cellpadding=0 cellspacing=0 border=0><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Description</td><td>&nbsp;&nbsp;</td><td><input type="text" name="File1_Desc" size=30 maxlength=50 ID="Text1"></td></tr></table></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table3"><tr><td width=20><img src="/Images/Spacer.gif" width=20 height=1 border=0 alt=""></td><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=25 height=1 border=0 alt=""><br>File</td><td>&nbsp;&nbsp;</td><td><INPUT TYPE=FILE SIZE=20 NAME="File1" ID="File1"></td></tr></table></td>
								</tr>
								<tr>
									<td></td>
									<td></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table9"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Category</td><td>&nbsp;&nbsp;</td><td><select name="File1_Category" ID="Select1"><%= strCategorySelect %></select></td></tr></table></td>
								</tr>
								<tr>
									<td colspan=4><hr></td>
								</tr>

								<tr>
									<td></td>
									<td valign="middle" style="font-weight:bold;"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>File #2</td>
									<td valign="middle" style="font-weight:bold;"><table cellpadding=0 cellspacing=0 border=0 ID="Table5"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Description</td><td>&nbsp;&nbsp;</td><td><input type="text" name="File2_Desc" size=30 maxlength=50 ID="Text2"></td></tr></table></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table6"><tr><td width=20><img src="/Images/Spacer.gif" width=20 height=1 border=0 alt=""></td><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=25 height=1 border=0 alt=""><br>File</td><td>&nbsp;&nbsp;</td><td><INPUT TYPE=FILE SIZE=20 NAME="File2" ID="File2"></td></tr></table></td>
								</tr>
								<tr>
									<td></td>
									<td></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table7"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Category</td><td>&nbsp;&nbsp;</td><td><select name="File2_Category" ID="Select2"><%= strCategorySelect %></select></td></tr></table></td>
								</tr>
								<tr>
									<td colspan=4><hr></td>
								</tr>

								<tr>
									<td></td>
									<td valign="middle" style="font-weight:bold;"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>File #3</td>
									<td valign="middle" style="font-weight:bold;"><table cellpadding=0 cellspacing=0 border=0 ID="Table8"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Description</td><td>&nbsp;&nbsp;</td><td><input type="text" name="File3_Desc" size=30 maxlength=50 ID="Text3"></td></tr></table></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table10"><tr><td width=20><img src="/Images/Spacer.gif" width=20 height=1 border=0 alt=""></td><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=25 height=1 border=0 alt=""><br>File</td><td>&nbsp;&nbsp;</td><td><INPUT TYPE=FILE SIZE=20 NAME="File3" ID="File3"></td></tr></table></td>
								</tr>
								<tr>
									<td></td>
									<td></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table11"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Category</td><td>&nbsp;&nbsp;</td><td><select name="File3_Category" ID="Select3"><%= strCategorySelect %></select></td></tr></table></td>
								</tr>
								<tr>
									<td colspan=4><hr></td>
								</tr>

								<tr>
									<td></td>
									<td valign="middle" style="font-weight:bold;"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>File #4</td>
									<td valign="middle" style="font-weight:bold;"><table cellpadding=0 cellspacing=0 border=0 ID="Table12"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Description</td><td>&nbsp;&nbsp;</td><td><input type="text" name="File4_Desc" size=30 maxlength=50 ID="Text4"></td></tr></table></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table13"><tr><td width=20><img src="/Images/Spacer.gif" width=20 height=1 border=0 alt=""></td><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=25 height=1 border=0 alt=""><br>File</td><td>&nbsp;&nbsp;</td><td><INPUT TYPE=FILE SIZE=20 NAME="File4" ID="File4"></td></tr></table></td>
								</tr>
								<tr>
									<td></td>
									<td></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table14"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Category</td><td>&nbsp;&nbsp;</td><td><select name="File4_Category" ID="Select4"><%= strCategorySelect %></select></td></tr></table></td>
								</tr>
								<tr>
									<td colspan=4><hr></td>
								</tr>

								<tr>
									<td></td>
									<td valign="middle" style="font-weight:bold;"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>File #5</td>
									<td valign="middle" style="font-weight:bold;"><table cellpadding=0 cellspacing=0 border=0 ID="Table15"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Description</td><td>&nbsp;&nbsp;</td><td><input type="text" name="File5_Desc" size=30 maxlength=50 ID="Text5"></td></tr></table></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table16"><tr><td width=20><img src="/Images/Spacer.gif" width=20 height=1 border=0 alt=""></td><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=25 height=1 border=0 alt=""><br>File</td><td>&nbsp;&nbsp;</td><td><INPUT TYPE=FILE SIZE=20 NAME="File5" ID="File5"></td></tr></table></td>
								</tr>
								<tr>
									<td></td>
									<td></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table17"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Category</td><td>&nbsp;&nbsp;</td><td><select name="File5_Category" ID="Select5"><%= strCategorySelect %></select></td></tr></table></td>
								</tr>
								<tr>
									<td colspan=4><hr></td>
								</tr>

								<tr>
									<td></td>
									<td valign="middle" style="font-weight:bold;"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>File #6</td>
									<td valign="middle" style="font-weight:bold;"><table cellpadding=0 cellspacing=0 border=0 ID="Table18"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Description</td><td>&nbsp;&nbsp;</td><td><input type="text" name="File6_Desc" size=30 maxlength=50 ID="Text6"></td></tr></table></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table19"><tr><td width=20><img src="/Images/Spacer.gif" width=20 height=1 border=0 alt=""></td><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=25 height=1 border=0 alt=""><br>File</td><td>&nbsp;&nbsp;</td><td><INPUT TYPE=FILE SIZE=20 NAME="File6" ID="File6"></td></tr></table></td>
								</tr>
								<tr>
									<td></td>
									<td></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table20"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Category</td><td>&nbsp;&nbsp;</td><td><select name="File6_Category" ID="Select6"><%= strCategorySelect %></select></td></tr></table></td>
								</tr>
								<tr>
									<td colspan=4><hr></td>
								</tr>


								<tr>
									<td colspan=4 valign="top" align="right"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1">&nbsp;<input type="submit" value="Submit" id="Submit" NAME="Submit"></td>
								</tr>
								</form>
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
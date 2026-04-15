<%@ Language=VBScript %>
<!--#include virtual="/System/ssi_functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open_dev.inc"-->
<HTML>
<HEAD>
<LINK REL='Stylesheet' HREF='Style.css'>
</HEAD>
<%
'Request the current year from querystring
Dim strDataURL
strDataURL = "Data.asp"
strDataURL = formatFCUrl(strDataURL)
%>
<table width="98%" border="0" cellpadding="2" cellspacing="0" class="tableWithBorder" align='center'>
  <tr> 
    <td colspan="3">&nbsp;</td>
  </tr>
  <tr> 
    <td valign="top">
    <table width="98%" border="0" cellspacing="0" cellpadding="2" align='center'>
        <tr> 
                <td>
                <div align="center" class="text"> 
					<OBJECT classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000"  codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,0,0" WIDTH="565" HEIGHT="420" id="FC2Column" ALIGN="" VIEWASTEXT>
					<PARAM NAME="FlashVars" value="&dataURL=<%=strDataURL%>&noCache=<%= Server.URLEncode(ServerToEST(Now()))%>">
					<PARAM NAME=movie VALUE="FC2Pie3D.swf">
					<PARAM NAME=quality VALUE=high>
					<PARAM NAME=bgcolor VALUE=#FFFFFF>
					<EMBED src="Charts/FC2Column.swf" FlashVars="&dataURL=<%=strDataURL%>&noCache=<%= Server.URLEncode(ServerToEST(Now()))%>" quality=high bgcolor=#FFFFFF WIDTH="565" HEIGHT="420" NAME="FC2Column" ALIGN="" TYPE="application/x-shockwave-flash" PLUGINSPAGE="http://www.macromedia.com/go/getflashplayer"></EMBED>
					</OBJECT>
				</div>
				</td>
         </tr>               
    </table>
    </td>
  </tr>
</table>
</BODY></html>
<%
	'Destroy objects
	Set oRsYears = nothing	
%>
<%
Function formatFCUrl(strDataURL)
	'This function converts the ? and & present in URL to *
	strDataURL = Replace(strDataURL,"?","*")
	strDataURL = Replace(strDataURL,"&","*")
	'Return it
	formatFCUrl = strDataURL
End Function
%>
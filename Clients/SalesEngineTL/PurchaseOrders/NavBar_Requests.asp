<%
Option Explicit

'patch

sql = "Select POApprovalLimit From Users Where Code = '" & Request.Cookies("UserSettings")("Code") & "'"
Set rsPOCheck = dbConn.Execute(sql)

decPOApprovalLimit = rsPOCheck("POApprovalLimit")

rsPOCheck.Close
Set rsPOCheck = Nothing

%>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="RefreshWindowClose();" ID="Button1" NAME="Button1"> <input type="button" value=" View Purchase Order Request " onclick="document.location.href='ViewRequest.asp?POid=<%= lngPOid %>';" ID="Button4" NAME="Button1"> <input type="button" value=" View History " onclick="document.location.href='ViewHistory.asp?Requests=True&POid=<%= lngPOid %>';" ID="Button8" NAME="Button1"> <input type="button" value=" Update Status " onclick="UpdatePOStatus('<%= Session("WorkingDir") %>', <%= lngPOid %>);" ID="Button10" NAME="Button1"> <% If (GetPONextLineApprover(lngPOid, rsPO("HasCapEx")) = Request.Cookies("UserSettings")("Name") Or (decPOApprovalLimit > rsPO("PriceIncTotal") And SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), rsPO("DivisionId")))) And rsPO("POStatusId") = 2 Then %><input type="button" value="Decline" style="color:red;" onclick="document.location.href='Decline.asp?POid=<%= lngPOid %>'" ID="Button7" NAME="Button7"><input type="button" value="Approve" style="color:red;" onclick="document.location.href='Approve.asp?POid=<%= lngPOid %>';" ID="Button6" NAME="Button6"><% End If %> <% If boolPrint Then %><input type="button" value="Print" style="font-weight:bold;color:red;" onclick="print();" ID="Button2" NAME="Button2"> (Make sure that you set the orientation to portrait)<% Else %><input type="button" value=" Print " onclick="if(confirm('If you proceed the Purchase Orders status will be set to issued.\nAre you sure you want to proceed?')){document.location.href='ViewRequest.asp?POid=<%= lngPOid %>&Print=True'}" ID="Button9" NAME="Button1"><% End If %></td>
			</tr>
<%

If rsCon("CompanyId") = 142 And rsPO("POPaymentTypeId") = 3 Then

%>
			<tr>
				<td><span style="font-weight:bold;color:navy;">Alert:</span> This could be a non-account supplier. The payment type selected is credit application. Please ensure that the credit application has been accepted.</td>		
			</tr>
<%

End If

%>
			<tr>
				<td>
				<!--#include virtual="/System/CurrencySelector.asp"-->
				</td>
			</tr>
		</table>
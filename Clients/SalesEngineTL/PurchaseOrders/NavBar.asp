<%
If Not boolEmail Then
%>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td>
				<input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"> <input type="button" value=" View Purchase Order " onclick="document.location.href='View.asp?POid=<%= lngPOid %>';" ID="Button4" NAME="Button1"> <input type="button" value=" View History " onclick="document.location.href='ViewHistory.asp?POid=<%= lngPOid %>';" ID="Button8" NAME="Button1">
				<input type="button" value=" Update Status " onclick="UpdatePOStatus('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', <%= lngPOid %>);" ID="Button10" NAME="Button1">
<%
	' If is next approver, or approval limit is high enough, or 
	If (rsPO("POStatusId") = 1 Or rsPO("POStatusId") = 2) And GetPOLineApprover_Check(lngPOid,Request.Cookies("UserSettings")("Code"), boolCapEx) Or CheckForLine(rsPO("Code"),Request.Cookies("UserSettings")("Code"), lngPOid, False, True) Then
%>
				<input type="button" value=" Decline " style="color:red;" onclick="document.location.href='Decline.asp?POid=<%= lngPOid %>'" ID="Button7" NAME="Button7">
				<input type="button" value=" Approve " style="color:red;" onclick="document.location.href='Approve.asp?POid=<%= lngPOid %>'" ID="Button6" NAME="Button6">
<%
	End If
%>
				<input type="button" value=" Enter Invoice Details " onclick="document.location.href='EnterInvoiceDetails.asp?POid=<%= rsPO("POid") %>';">
<%
	If ((rsPO("POStatusId") > 2 And rsPO("POStatusId") < 5) Or rsPO("POStatusId") = 7) Or (GetPOLastLineApprover(rsPO("POid"), boolCapEx) = "Already approved") Then
%>
				<input type="button" value=" Email " onclick="document.location.href='Email.asp?POid=<%= lngPOid %>';" ID="Button3" NAME="Button2"> 
<%
		If boolPrint Then
%>
				<input type="button" value=" Print " style="font-weight:bold;color:red;" onclick="print();" ID="Button2" NAME="Button2"> (Make sure that you set the orientation to portrait)
<%
		Else
%>
				<input type="button" value=" Print " onclick="if(confirm('If you proceed the Purchase Orders status will be set to issued.\nAre you sure you want to proceed?')){document.location.href='View.asp?POid=<%= lngPOid %>&Print=True'}" ID="Button9" NAME="Button1">
<%
		End If
	End If
%>
				</td>
			</tr>
<%
    If rsPO("Project") <> "" Then
%>
            <tr>
                <td><strong>Project:</strong> <%= rsPO("Project") %></td>
            </tr>
<%
    End If
    strInternalNotes = rsPO("InternalNotes")
    If strInternalNotes <> "" Then
%>
            <tr>
                <td><strong>Reason For Purchase / Internal Notes:</strong> <%= strInternalNotes %></td>
            </tr>
<%
    End If
	If rsCon("CompanyId") = 142 And rsPO("POPaymentTypeId") = 3 Then
%>
			<tr>
				<td><span style="font-weight:bold;color:navy;">Alert:</span> This could be a non-account supplier. The payment type selected is credit application. Please ensure that the credit application has been accepted.</td>		
			</tr>
<%
	End If
%>
<%
	If rsPO("HasCapEx") Then
%>
			<tr>
				<td><span style="font-weight:bold;color:navy;">Alert:</span> Capital expenditure items exist in this purchase order.</td>		
			</tr>
<%
	End If
%>
<%
	Dim dblRunningTotal
	dblRunningTotal = 0
	
	sql = "Select * From PurchaseOrderInvoices Where POid = " & rsPO("POid") & " Order By InvoiceDate Desc"
	Set rsInv = dbConn.Execute(sql)
	If Not(rsInv.BOF And rsInv.EOF) Then
%>
			<tr>
				<td>
<%
		Do Until rsInv.EOF
%>
		An Invoice (#<%= rsInv("InvoiceNumber") %>) for <% If rsInv("InvoiceAmount") = 0 Then Response.Write("unknown") Else Response.Write(FormatCurrency(rsInv("InvoiceAmount"),2)) %> was issued on <% If IsDate(rsInv("InvoiceDate")) Then Response.Write(FormatDateU(rsInv("InvoiceDate"), False)) %>.<br />
<%
			dblRunningTotal = dblRunningTotal + FormatCurrency(rsInv("InvoiceAmount"),2)
			rsInv.MoveNext
		Loop
%>
				<b>Total invoices</b> = <%= FormatCurrency(dblRunningTotal,2) %>
<%
	End If
%>
				</td>
			</tr>
			<tr>
				<td>
				<!--#include virtual="/System/CurrencySelector.asp"-->
				</td>
			</tr>
		</table>
<%
End If
%>
        <table>

        </table>
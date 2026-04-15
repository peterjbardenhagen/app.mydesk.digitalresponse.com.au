<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim lngContactId
lngContactId = CLng(Request("ContactId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<title></title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
        <script language="javascript">
            function DoSelect(sEmail) {
                try {
                    window.opener.document.getElementById('ToEmail').value = sEmail;
                } catch(e) {
                    alert('The email window is no longer available.');
                } finally {
                    window.close();
                }
            }
        </script>
	</head>
	<body>
<!--#include virtual="/System/ssi_Header.inc"-->
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"></td>
			</tr>
		</table>
		<table cellpadding=5 cellspacing=0 border=0>
		    <tr>
		        <td>
        		<br /><span style="font-size:14px;font-weight:bold;">Select email address from contact</span><br /><br />
<%

Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select ContactId, CompanyName, FirstName, Surname From Contacts_WithCustomersAndSuppliers_V2 Where Deleted = 0 AND Code = '" & Request.Cookies("UserSettings")("Code") & "' ORDER BY CompanyName, Surname, FirstName"
Set rs = dbConn.Execute(sql)

%>
	                <table width=95% align="center" cellpadding=3 cellspacing=0 border=0 ID="Table4">
                        <form name="Form1" method="post" action="">
		                <tr>
			                <td nowrap style="font-weight:bold;">Select a contact</td>
			                <td>
			                <select name="ContactId" onchange="document.location.href='SelectEmailFromContact.asp?ContactId='+document.Form1.ContactId.value;">
			                    <option></option>
<%

Do Until rs.EOF
    Response.Write("<option value=""" & rs("ContactId") & """>" & rs("CompanyName") & " - " & rs("FirstName") & " " & rs("Surname"))
    rs.MoveNext
Loop

%>			
			                </select>
			                </td>
                        </tr>
<%

If lngContactId <> 0 Then
    sql = "Select Email From Contacts Where ContactId = " & lngContactId
    Set rs2 = dbConn.Execute(sql)
    If Not(rs2.BOF And rs2.EOF) Then
    
%>
                        <tr>
                            <td style="font-weight:bold;">Email address</td>
                            <td>
<%

        If rs2("Email") <> "" Then

%>
                            <a href="#" onclick="DoSelect('<%= rs2("Email") %>');"><%= rs2("Email") %> (select)</a>
<%

        Else

%>
                            No email address available
<%

        End If

%>
                            </td>
                        </tr>
<%
   
    End If
    rs2.Close
    Set rs2 = Nothing
End If

%>
                        </form>
                    </table>
		        </td>    
		    </tr>
		</table>
	</body>
</html>
<%

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
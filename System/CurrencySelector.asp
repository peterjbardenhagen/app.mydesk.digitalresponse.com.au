<%

If 1 = 4 Then

If Not(boolEmail) Then
	' Set Session
	If Request.Form("CurrencyName") <> "" Then
		Set rsCurrency = Server.CreateObject("ADODB.RecordSet")
		sql = "Select * From [Currency] Where CurrencyName = '" & Request.Form("CurrencyName") & "'"
		Set rsCurrency = dbConn.Execute(sql)
		If Not(rsCurrency.BOF And rsCurrency.EOF) Then
			Session("CurrencyName") = Request.Form("CurrencyName")
			Session("CurrencyRate") = CDbl(rsCurrency("CurrencyRate"))
			Session("CurrencyPrefix") = Trim(rsCurrency("CurrencyPrefix"))
		End If
		rsCurrency.Close
		Set rsCurrency = Nothing
	End If
End If

If Session("CurrencyName") = "" Or IsNull(Session("CurrencyName")) Or Len(Session("CurrencyName")) = 0 Then
	Session("CurrencyName") = "Australia Dollars"
	Session("CurrencyRate") = 1.0
	Session("CurrencyPrefix") = "$"
End If

If boolEmail Then
	strCurrencyName = Trim(Request("CurrencyName"))
	dblCurrencyRate = CDbl(Request("CurrencyRate"))
	strCurrencyPrefix = Trim(Request("CurrencyPrefix"))
Else
	strCurrencyName = Trim(Session("CurrencyName"))
	dblCurrencyRate = CDbl(Session("CurrencyRate"))
	strCurrencyPrefix = Trim(Session("CurrencyPrefix"))
%>
<table ID="Table1">
	<form name="FormCurrency" method="post" action="?<%= Request.ServerVariables("QUERY_STRING") %>">
	<tr>
		<td>
		<b>Currency:</b>
		<select name="CurrencyName" ID="Select1">
<%

Set rsCurrency = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From [Currency] Order By CurrencyName"
Set rsCurrency = dbConn.Execute(sql)

Do Until rsCurrency.EOF
	If rsCurrency("CurrencyName") = Session("CurrencyName") Then
%>
			<option selected value="<%= rsCurrency("CurrencyName") %>"><%= rsCurrency("CurrencyName") %>&nbsp;(*<%= rsCurrency("CurrencyRate") %>)&nbsp;<%= rsCurrency("CurrencyPrefix") %>
<%
	Else
%>
			<option value="<%= rsCurrency("CurrencyName") %>"><%= rsCurrency("CurrencyName") %>&nbsp;(*<%= rsCurrency("CurrencyRate") %>)&nbsp;<%= rsCurrency("CurrencyPrefix") %>
<%
	End If
	rsCurrency.MoveNext
Loop

rsCurrency.Close
Set rsCurrency = Nothing

%>
		</select>
		<input type="submit" value="Select currency" ID="Submit1" NAME="Submit1">
		</td>
	</tr>
	</form>
</table>
<%
End If
End If
%>
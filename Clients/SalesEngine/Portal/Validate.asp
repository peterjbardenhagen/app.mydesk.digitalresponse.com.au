<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim strUsername
Dim strPassword
Dim strSql

strUsername = Trim(Replace(Request("Username"),"'","''"))
strPassword = Trim(Replace(Request("Password"),"'","''"))

Set rsCheck = Server.CreateObject("ADODB.RecordSet")
strSql = "Select * From SalesPeople Where [Name] = '" & strUsername & "' And [PW] = '" & strPassword & "' And Active = -1"
Set rsCheck = dbConn.Execute(strSql)

If Not(rsCheck.BOF And rsCheck.EOF) Then
	Session("LoggedIn") = True
	Session("Code") = Trim(rsCheck("Code"))
	Session("Name") = Trim(rsCheck("Name"))
	Session("Initials") = Trim(rsCheck("Initials"))
	
	If rsCheck("Admin") = -1 Then
		Session("Admin") = True
	Else
		Session("Admin") = False
	End If

	rsCheck.Close
	Set rsCheck = Nothing	
	
	MyRedirect("/Portal.asp")
Else
	Session("LoggedIn") = False
	Session("Code") = ""
	Session("Name") = ""
	Session("Admin") = False

	rsCheck.Close
	Set rsCheck = Nothing	

	MyRedirect("/Clients/SalesEngine/?Msg=Login+failed,+incorrect+Username+and/or+Password.+Please+try+again.")
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
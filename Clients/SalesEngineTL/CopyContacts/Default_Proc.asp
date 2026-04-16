<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

On Error Resume Next

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim strSQL
Dim strCopyFromCode
Dim strCopyToCode

strCopyFromCode = Trim(Request("CopyFromCode"))
strCopyToCode = Trim(Request("CopyToCode"))

strSQL = "Select * From Contacts Where Code = '" & strCopyFromCode & "'"
Set rsCopy = dbConn.Execute(strSQL)

If Not(rsCopy.BOF And rsCopy.EOF) Then
	Do Until rsCopy.EOF
		lngStateId = rsCopy("StateId")
		lngOStateId = rsCopy("OStateId")
		If Not IsNumeric(lngStateId) Then lngStateId = 9
		If Not IsNumeric(lngOStateId) Then lngOStateId = 9
		strSQL = "Insert Into Contacts (Code, FirstName, Surname, Position, CompanyId, CCompany, Address1, Address2, Suburb, StateId, State, PostCode, Country, OAddress1, OAddress2, OSuburb, OStateId, OState, OPostCode, OCountry, Phone, Fax, Mobile, Email, Website, Notes, InMain) " &_
					"Values ('" & strCopyToCode & "', '" & Replace(rsCopy("FirstName")&"","'","''") & "', '" & Replace(rsCopy("Surname")&"","'","''") & "', '" & Replace(rsCopy("Position")&"","'","''") & "', " & rsCopy("CompanyId") & ", '" & Replace(rsCopy("CCompany")&"","'","''") & "', '" & Replace(rsCopy("Address1")&"","'","''") & "', '" & Replace(rsCopy("Address2")&"","'","''") & "', '" & Replace(rsCopy("Suburb")&"","'","''") & "', " & lngStateId & ", '" & Replace(rsCopy("State")&"","'","''") & "', '" & rsCopy("PostCode")&"" & "', '" & Replace(rsCopy("Country")&"","'","''") & "', '" & Replace(rsCopy("OAddress1")&"","'","''") & "', '" & Replace(rsCopy("OAddress2")&"","'","''") & "', '" & Replace(rsCopy("OSuburb")&"","'","''") & "', " & lngOStateId & ", '" & Replace(rsCopy("OState")&"","'","''") & "', '" & rsCopy("OPostCode")&"" & "', '" & rsCopy("OCountry")&"" & "', '" & rsCopy("Phone")&"" & "', '" & rsCopy("Fax")&"" & "', '" & rsCopy("Mobile")&"" & "', '" & Replace(rsCopy("Email")&"","'","''") & "', '" & Replace(rsCopy("Website")&"","'","''") & "', '" & Replace(rsCopy("Notes")&"","'","''") & "', " & rsCopy("InMain")&"" & ")"
		dbConn.Execute(strSQL)
		If err Then
			response.write strsql
			response.write "<br>"&err.description
			response.end
		End If
		rsCopy.MoveNext
	Loop
End If

If IsObject(rsCopy) Then
	rsCopy.Close
	Set rsCopy = Nothing
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Query+executed+successfully.")

%>
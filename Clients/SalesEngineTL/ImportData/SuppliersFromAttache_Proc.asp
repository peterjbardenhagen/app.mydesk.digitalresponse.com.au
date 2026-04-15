<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Set Upload = Server.CreateObject("SoftArtisans.FileUp")

Upload.Path = Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/ImportData/TempFiles")
Upload.MaxBytes = 100000000	' Limit files to 100mb
Upload.OverWriteFiles = True

strUploadFilename = DateFileName(Upload.Form("File" & i).ShortFilename)
Upload.Form("File" & i).SaveAs(strUploadFilename)

lngDivisionId = CLng(Upload.Form("DivisionId"))

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

For Each File in Upload.Files
	
	Set fs = Server.CreateObject("Scripting.FileSystemObject")
	Set f = fs.OpenTextFile(Server.MapPath("TempFiles/" & File.FileName), 1)

	i = 0

	Do While f.AtEndOfStream = False
		strLine = f.ReadLine
		If InStr(strLine, "CODE:") = 3 Then ' Something useful here
			strCode = Trim(Mid(strLine, 9, 12))
		End If
		If InStr(strLine, "NAME:") = 3 Then
			strCompany = LTrim(RTrim(Mid(Replace(strLine, "'", "''"), 9, 35)))
		End If
		If InStr(strLine, "STREET:") = 1 Then
			strStreet = Trim(Mid(Replace(strLine, "'", "''"), 9, 35))
		End If
		If InStr(strLine, "SUBURB:") = 1 Then
			strSuburb = Trim(Mid(Replace(strLine, "'", "''"), 9, 35))
		End If
		If InStr(strLine, "P'CODE:") = 46 Then
			strPostCode = Trim(Mid(Replace(strLine, "'", "''"), 54, 6))
		End If
		If InStr(strLine, "PHONE:") = 47 Then
			strPhone = Trim(Mid(Replace(strLine, "'", "''"), 54, 16))
		End If
		If InStr(strLine, "FAX:") = 49 Then
			strFax = Trim(Mid(Replace(strLine, "'", "''"), 54, 16))
		End If
		If InStr(strLine, "CONTACT:") = 45 Then
			strContact = Trim(Mid(Replace(strLine, "'", "''"), 54, 23))
		End If

		If strCode <> "" And strCompany <> "" Then
			sql = "Select * From Companies Where SupplierCode = '" & strCode & "' And DivisionId = " & lngDivisionId
			Set rsCheck = dbConn.Execute(sql)
			If Not(rsCheck.BOF And rsCheck.EOF) Then ' Exists
				sql = "Update Companies Set Company = '" & strCompany & "', Address1 = '" & strStreet & "', Suburb = '" & strSuburb & "', PostCode = '" & strPostCode & "', Phone = '" & strPhone & "', Fax = '" & strFax & "', ContactName = '" & strContact & "' Where SupplierCode = '" & strCode & "' And DivisionId = " & lngDivisionId 
				dbConn.Execute(sql)
			Else
				sql = "Insert Into Companies (DivisionId, SupplierCode, Company, Address1, Suburb, PostCode, Phone, Fax, ContactName) Values (" & lngDivisionId & ", '" & strCode & "', '" & strCompany & "', '" & strStreet & "', '" & strSuburb & "', '" & strPostCode & "', '" & strPhone & "', '" & strFax & "', '" & strContact & "')"
				dbConn.Execute(sql)
			End If
			i = i + 1
		End If
	Loop

	fs.DeleteFile(Server.MapPath("TempFiles/" & File.FileName))

	f.Close
	Set f = Nothing
	Set fs = Nothing
Next

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

Response.Redirect(Request.Cookies("ClientSettings")("WorkingDir") & "/ImportData/?Msg=Upload+complete.+Imported+" & i & "+records.")

%>
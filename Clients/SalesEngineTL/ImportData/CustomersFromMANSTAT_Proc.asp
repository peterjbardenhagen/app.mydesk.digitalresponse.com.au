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

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

lngDivisionId = 3
i = 0

For Each File in Upload.Files
	Set fs = Server.CreateObject("Scripting.FileSystemObject")
	Set f = fs.OpenTextFile(Server.MapPath("TempFiles/" & File.FileName), 1)

	Do While f.AtEndOfStream = False
		strLine = f.ReadLine
		If strLine <> "" And InStr(strLine, "	") > 0 Then
			arrValues = Split(strLine, "	")
			strCustomerCode = Replace(arrValues(0), "'", "''")
			strCompany = Replace(arrValues(1), "'", "''")
			strEmail = Replace(arrValues(2), "'", "''")
			strAddressLine1 = Replace(arrValues(3), "'", "''")
			strAddressLine2 = Replace(arrValues(4), "'", "''")
			strSuburb = Replace(arrValues(5), "'", "''")
			strState = Replace(arrValues(6), "'", "''")
			strPostCode = Replace(arrValues(7), "'", "''")

			If arrValues(0) <> "" And arrValues(1) <> "" Then
				sql = "Select * From Companies Where CustomerCode = '" & strCustomerCode & "' And DivisionId = " & lngDivisionId
				Set rsCheck = dbConn.Execute(sql)
				If Not(rsCheck.BOF And rsCheck.EOF) Then ' Exists
					sql = "Update Companies Set Company = '" & strCompany & "', "
					If strEmail <> "" Then
						sql = sql & "Email = '" & strEmail & "', "
					End If
					sql = sql & "Address1 = '" & strAddress1 & "', Address2 = '" & strAddress2 & "', Suburb = '" & strSuburb & "', State = '" & strState & "', PostCode = '" & strPostCode & "' Where CustomerCode = '" & strCustomerCode & "' And DivisionId = " & lngDivisionId 
					dbConn.Execute(sql)
				Else
					sql = "Insert Into Companies (DivisionId, CustomerCode, Company, Email, Address1, Address2, Suburb, State, PostCode) Values (" & lngDivisionId & ", '" & strCustomerCode & "', '" & strCompany & "', '" & strEmail & "', '" & strAddress1 & "', '" & strAddress2 & "', '" & strSuburb & "', '" & strState & "', '" & strPostCode & "')"
					dbConn.Execute(sql)
				End If
				i = i + 1
			End If
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
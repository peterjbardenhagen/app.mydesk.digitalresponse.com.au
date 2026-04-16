<%
'Dim strWorkingDir

If Request.Cookies("LoggedIn")&"" <> "" Then
	strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
Else
	strWorkingDir = Session("WorkingDir")
End If

' Fallback to ensure WorkingDir is never empty
If strWorkingDir = "" Or IsNull(strWorkingDir) Then
	strWorkingDir = "/Clients/SalesEngineTL"
End If

' Get current page for active nav highlighting
Dim currentPage
currentPage = LCase(Request.ServerVariables("SCRIPT_NAME"))

Function IsActive(pageName)
	If InStr(currentPage, LCase(pageName)) > 0 Then
		IsActive = "active"
	Else
		IsActive = ""
	End If
End Function

Function IsDirector()
	Dim userTypeId
	userTypeId = Request.Cookies("UserSettings")("UserTypeID") & ""
	' UserTypeID 1 = Director, 2 = Manager, 3 = Standard User
	IsDirector = (userTypeId = "1")
End Function
%>

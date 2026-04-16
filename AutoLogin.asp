<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%
On Error Resume Next

Dim strToken, rsUser
strToken = Trim(Request.QueryString("t") & "")

If strToken <> "" Then
    Set rsUser = Server.CreateObject("ADODB.Recordset")
    rsUser.Open "SELECT * FROM Users WHERE LoginToken = '" & Replace(strToken, "'", "''") & "'", dbConn, 3, 3
    
    If Not rsUser.EOF Then
        ' Check Expiry
        If IsDate(rsUser("LoginTokenExpiry")) Then
            If CDate(rsUser("LoginTokenExpiry")) > Now() Then
                ' Valid Token - Log user in
                Session("LoggedIn") = True
                Session("Code") = rsUser("Code")
                Session("Name") = rsUser("Name")
                Session("Email") = rsUser("Email")
                Session("Initials") = rsUser("Initials")
                Session("DivisionId") = rsUser("DivisionId")
                Session("Division") = rsUser("Division")
                Session("UserTypeId") = rsUser("UserTypeId")
                Session("LocationId") = rsUser("LocationId")
                Session("ExpenseTypeGroupId") = rsUser("ExpenseTypeGroupId")
                
                ' Clear token to make it one-time use
                rsUser("LoginToken") = Null
                rsUser("LoginTokenExpiry") = Null
                rsUser.Update
                
                Response.Redirect "/Clients/SalesEngineTL/Dashboard.asp"
                Response.End
            Else
                ' Expired
                Response.Redirect "/Default.asp?Msg=" & Server.URLEncode("Your login link has expired. Please request a new one.")
                Response.End
            End If
        End If
    End If
    
    If rsUser.State = 1 Then rsUser.Close
    Set rsUser = Nothing
End If

' If we get here, token was invalid or empty
Response.Redirect "/Default.asp?Msg=" & Server.URLEncode("Invalid secure login link.")
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->

Imports System
Imports System.IO
Imports System.Net
Imports System.Web

Public Class TT_GenerateRFQ
    Inherits System.Web.UI.Page

    Protected WithEvents peter As System.Web.UI.WebControls.Label

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Dim boolEmail As Boolean
        Dim boolFax As Boolean
        Dim lngRFQid As Long = CLng(Request("RFQid"))
        Dim strAttention As String = Request("Attention") & ""
        Dim strToEmail As String = Request("ToEmail") & ""
        Dim strFromFax As String = Request("FromFax") & ""
        Dim strToFax As String = Request("ToFax") & ""
        Dim strWorkingDir As String = Request("WorkingDir") & ""

        Dim intMode As Integer = CInt(Request("Mode"))
        Dim strStream As String
        Dim oRequest As WebRequest = WebRequest.Create("https://" & Request.ServerVariables("SERVER_NAME") & strWorkingDir & "/RFQ/View.asp?Email=" & boolEmail & "&Fax=" & boolFax & "&RFQid=" & lngRFQid)
        Dim oResponse As WebResponse = oRequest.GetResponse()
        Dim oStream As Stream = oResponse.GetResponseStream()
        Dim oStreamReader As New StreamReader(oStream, True)
        Dim strPath As String = Server.MapPath(strWorkingDir & "/RFQ/Files/" & lngRFQid)

        If intMode = 1 Then boolEmail = True Else boolEmail = False
        If intMode = 2 Then boolFax = True Else boolFax = False

        If Not Directory.Exists(strPath) Then
            Directory.CreateDirectory(strPath)
        End If

        Dim myFile As String = strPath & "\RFQ.html"
        File.Delete(myFile)

        Dim oStreamWriter As StreamWriter
        Dim strLine
        Dim strStreamLine

        oStreamWriter = File.CreateText(myFile)

        While oStreamReader.Peek() <> -1
            strStreamLine = oStreamReader.ReadLine()
            strStream += strStreamLine
            oStreamWriter.WriteLine(strStreamLine)
        End While

        oResponse.Close()
        oResponse = Nothing

        oStreamReader.Close()
        oStreamReader = Nothing

        oStreamWriter.Close()
        oStreamWriter = Nothing

        If intMode = 1 Then
            Response.Redirect("Email_Proc.asp?Attention=" & strAttention & "&ToEmail=" & strToEmail & "&RFQid=" & lngRFQid)
        ElseIf intMode = 2 Then
            Response.Redirect("Fax_Proc.asp?Attention=" & strAttention & "&ToFax=" & strToFax & "&FromFax=" & strFromFax & "&RFQid=" & lngRFQid)
        End If
    End Sub
End Class

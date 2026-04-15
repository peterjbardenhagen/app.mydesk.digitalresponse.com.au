<%@ Page Language="vb" AutoEventWireup="false" ClassName="TT_GeneratePO" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Net" %>
<%@ Import Namespace="System.Web" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<html>
    <head>
        <title>Generate</title>
    </head>
    <body>
        <script language="VisualBasic" runat="Server">

            ' Returns the appropriate protocol (http or https) based on the hostname
            ' Production (techlight.digitalresponse.com.au) requires HTTPS
            ' Local/UAT environments can use HTTP or HTTPS
            Private Function GetProtocol(ByVal hostname As String) As String
                Dim hostLower As String = hostname.ToLower()
                
                ' Production hostname - must use HTTPS
                If hostLower = "techlight.digitalresponse.com.au" Then
                    Return "https"
                Else
                    ' Local/UAT environments - use current protocol
                    Dim isHttps As String = Request.ServerVariables("HTTPS")
                    If isHttps = "on" OrElse isHttps = "true" Then
                        Return "https"
                    Else
                        Return "http"
                    End If
                End If
            End Function

            Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
                Dim boolEmail As Boolean
                Dim boolFax As Boolean
                Dim lngPOid As Long = CLng(Request("POid"))
                Dim strNotes As String = Request("Notes")
                Dim strAttention As String = Request("Attention") & ""
                Dim strToEmail As String = Request("ToEmail") & ""
                Dim strFromFax As String = Request("FromFax") & ""
                Dim strToFax As String = Request("ToFax") & ""
                Dim strWorkingDir As String = Request("WorkingDir")

                Dim dblCurrencyRate As Double = CDbl(Request("CurrencyRate"))
                Dim strCurrencyName As String = Request("CurrencyName") & ""
                Dim strCurrencyPrefix As String = Request("CurrencyPrefix") & ""

                Dim intMode As Integer = CInt(Request("Mode"))
                Dim strStream As String = ""
                Dim strProtocol As String = GetProtocol(Request.ServerVariables("SERVER_NAME"))
                Dim oRequest As WebRequest = WebRequest.Create(strProtocol & "://" & Request.ServerVariables("SERVER_NAME") & strWorkingDir & "/PurchaseOrders/View.asp?Email=" & boolEmail & "&Fax=" & boolFax & "&POid=" & lngPOid & "&CurrencyName=" & strCurrencyName & "&CurrencyRate=" & dblCurrencyRate & "&CurrencyPrefix=" & strCurrencyPrefix)
                Dim oResponse As WebResponse = oRequest.GetResponse()
                Dim oStream As Stream = oResponse.GetResponseStream()
                Dim oStreamReader As New StreamReader(oStream, True)
                Dim strPath As String = Server.MapPath(strWorkingDir & "/PurchaseOrders/Files/" & lngPOid)

                If intMode = 1 Then boolEmail = True Else boolEmail = False
                If intMode = 2 Then boolFax = True Else boolFax = False

                If Not Directory.Exists(strPath) Then
                    Directory.CreateDirectory(strPath)
                End If

                Dim myFile As String = strPath & "\PO.html"
                File.Delete(myFile)

                Dim oStreamWriter As StreamWriter
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
                    Response.Redirect("Email_Proc.asp?Notes=" & strNotes.Replace(Convert.ToChar(10), "") & "&Attention=" & strAttention & "&ToEmail=" & strToEmail & "&POid=" & lngPOid & "&CurrencyName=" & strCurrencyName & "&CurrencyRate=" & dblCurrencyRate & "&CurrencyPrefix=" & strCurrencyPrefix)
                ElseIf intMode = 2 Then
                    Response.Redirect("Fax_Proc.asp?Notes=" & strNotes.Replace(Convert.ToChar(10), "") & "&Attention=" & strAttention & "&ToFax=" & strToFax & "&FromFax=" & strFromFax & "&POid=" & lngPOid & "&CurrencyName=" & strCurrencyName & "&CurrencyRate=" & dblCurrencyRate & "&CurrencyPrefix=" & strCurrencyPrefix)
                End If
            End Sub

        </script>
    </body>
</html>
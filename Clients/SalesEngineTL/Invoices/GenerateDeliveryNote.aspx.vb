Imports System
Imports System.IO
Imports System.Net
Imports System.Web

Public Class TL_GenerateDeliveryNote
	Inherits System.Web.UI.Page

	Protected WithEvents peter As System.Web.UI.WebControls.Label

	Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
		Dim boolEmail As Boolean
		Dim boolFax As Boolean
		Dim lngInvoiceId As Long = CLng(Request("InvoiceId"))
		Dim strNotes As String = Request("Notes") & ""
		Dim strAttention As String = Request("Attention") & ""
		Dim strToEmail As String = Request("ToEmail") & ""
		Dim strFromFax As String = Request("FromFax") & ""
		Dim strToFax As String = Request("ToFax") & ""
		Dim strWorkingDir As String = Request("WorkingDir") & ""

		Dim intMode As Integer = CInt(Request("Mode"))
		Dim strStream As String
		Dim oRequest As WebRequest = WebRequest.Create("https://" & Request.ServerVariables("SERVER_NAME") & strWorkingDir & "/Invoices/View.asp?Email=" & boolEmail & "&Fax=" & boolFax & "&InvoiceId=" & lngInvoiceId)
		Dim oResponse As WebResponse = oRequest.GetResponse()
		Dim oStream As Stream = oResponse.GetResponseStream()
		Dim oStreamReader As New StreamReader(oStream, True)
		Dim strPath As String = Server.MapPath(strWorkingDir & "/Invoices/Files/" & lngInvoiceId)

		If intMode = 1 Then boolEmail = True Else boolEmail = False
		If intMode = 2 Then boolFax = True Else boolFax = False

		If Not Directory.Exists(strPath) Then
			Directory.CreateDirectory(strPath)
		End If

		' Dim myFile As String = strPath & "\Invoice.html"
		' File.Delete(myFile)

		' Dim oStreamWriter As StreamWriter
		' Dim strLine
		' Dim strInvoice
		' Dim strStreamLine

		' oStreamWriter = File.CreateText(myFile)

		' While oStreamReader.Peek() <> -1
			' strStreamLine = oStreamReader.ReadLine()
			' strStream += strStreamLine
			' oStreamWriter.WriteLine(strStreamLine)
		' End While

		' oResponse.Close()
		' oResponse = Nothing

		' oStreamReader.Close()
		' oStreamReader = Nothing

		' oStreamWriter.Close()
		' oStreamWriter = Nothing

        If Not Directory.Exists(strPath) Then
            Directory.CreateDirectory(strPath)
        End If

        Dim myFile As String = strPath & "\DeliveryNote.pdf"
        File.Delete(myFile)

        Dim wc As System.Net.WebClient = New System.Net.WebClient()
        wc.DownloadFile("https://" & Request.ServerVariables("SERVER_NAME") & "/MyDeskASPNet/ScrapePDF.aspx?url=" & "https://" & Request.ServerVariables("SERVER_NAME") & strWorkingDir & "/Quotes/View.asp?Email=" & boolEmail & "&Fax=" & boolFax & "&Qid=" & lngQid, myFile)




		If intMode = 1 Then
			Response.Redirect("Email_Proc.asp?Notes=" & strNotes & "&Attention=" & strAttention & "&ToEmail=" & strToEmail & "&InvoiceId=" & lngInvoiceId)
		ElseIf intMode = 2 Then
			'			Response.Redirect("Fax_Proc.asp?Notes=" & strNotes & "&Attention=" & strAttention & "&ToFax=" & strToFax & "&FromFax=" & strFromFax & "&InvoiceId=" & lngInvoiceId)
		End If




	End Sub
End Class

<%

'Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.AddHeader "pragma","no-cache"
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-cache"

If Not Request.Cookies("UserSettings")("Manager") Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dates.inc"-->
<%

Dim nRows ' Number of rows in 
Dim nColumns
Dim rs
Dim cmd
Dim CurPage
Dim strCode
Dim lngDivisionId
Dim strLetter

strCode = Request.Cookies("UserSettings")("Code")
lngDivisionId = CInt(Request("DivisionId"))
strLetter = Trim(Request("Letter"))

%>
<!--#include virtual="/System/ssi_dbConn_Open.inc"-->
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
	<style>
		body { background: white; padding: 0; margin: 0; font-family: 'Inter', sans-serif; }
		.tl-data-table { border-collapse: separate; border-spacing: 0; width: 100%; border: none; }
		.tl-data-table th { background: #f8fafc; position: sticky; top: 0; z-index: 10; border-bottom: 1px solid var(--tl-border); }
		.tl-data-table td { border-bottom: 1px solid var(--tl-border); }
		.pagination-container { position: sticky; bottom: 0; background: white; border-top: 1px solid var(--tl-border); padding: 12px 24px; display: flex; justify-content: center; align-items: center; gap: 16px; box-shadow: 0 -4px 12px rgba(0,0,0,0.05); }
	</style>

<%

function activewidgets_grid(name, oRecordset)

	Dim i, columns, rows, s
	Dim column_count, row_count

	column_count = oRecordset.fields.count

	columns = "var " & name & "Columns = [" & vbNewLine
	For i=0 to (column_count-1)
		If i = (column_count-1) Then
			columns = columns & """" & activewidgets_html("", oRecordset(i).name, "")  & """ "
		Else
			columns = columns & """" & activewidgets_html("", oRecordset(i).name, "")  & """, "
		End If
	Next
	columns = columns & vbNewLine & "];" & vbNewLine

	row_count = 0
	rows = "var " & name & "Data = [" & vbNewLine
	Do while (Not oRecordset.eof)
		row_count = row_count + 1
		rows = rows & "["
		For i=0 to (column_count-1)
			If i = (column_count-1) Then
				rows = rows & """" & activewidgets_html(oRecordset("CompanyId"), oRecordset(i), oRecordset(i).name) & """ "
			Else
				rows = rows & """" & activewidgets_html(oRecordset("CompanyId"), oRecordset(i), oRecordset(i).name) & """, "
			End If
		Next
'		If row_count = 20 Then
'			rows = rows & "]" & vbNewLine
'		Else
			rows = rows & "]," & vbNewLine
'		End If
		oRecordset.MoveNext
	Loop
	rows = rows & "];" & vbNewLine

	s = vbNewLine
	s = s & rows & vbNewLine
	s = s & columns & vbNewLine

'	s = s & "</" & "script" & ">" & vbNewLine

	activewidgets_grid = s
	
	nColumns = column_count
	nRows = row_count

end function

function activewidgets_html(Id, s, FieldName)

	If Not IsNull(s) Then
		If IsDate(s) Then
			s = FormatDateU2(s, False)
		ElseIf s = "ACTION" Then
			s = "<a href='" & Request.Cookies("ClientSettings")("WorkingDir") & "/Companies/Edit.asp?CompanyId=" & Id & "' target='_parent'>Edit</a> | <a href='#' onclick='deleteRecord(" & Id & ");'>Delete</a>"
		End If
		's = Replace(s, "'", "`")
		s = Replace(s, "\", "\\")
		s = Replace(s, """", "\""")
		s = Replace(s, vbCr, "\r")
		s = Replace(s, vbLf, "\n")
	Else
		's = "Not entered"
	End If

	activewidgets_html = s
end function

%>

<div style="height: 100vh; display: flex; flex-direction: column;">
	<div style="flex: 1; overflow: auto; padding-bottom: 60px;">
		<table class="tl-data-table">
			<thead>
				<tr>
					<th>Company</th>
					<th>Customer Code</th>
					<th>Location</th>
					<th>Contact</th>
					<th>Phone</th>
					<th style="text-align: right;">Action</th>
				</tr>
			</thead>
			<tbody id="tableBody">
<%
Dim sql
sql = "SELECT CompanyId, Company, CustomerCode, Suburb, State, ContactName, Phone FROM Companies WHERE Left(Company,1) = '" & strLetter & "' AND DivisionId = " & lngDivisionId & " ORDER BY Company"
Set rs = dbConn.Execute(sql)

If rs.BOF And rs.EOF Then
%>
				<tr>
					<td colspan="6" style="text-align: center; padding: 48px; color: var(--tl-text-light);">
						No companies found starting with "<%= strLetter %>"
					</td>
				</tr>
<%
Else
	Do Until rs.EOF
%>
				<tr class="company-row">
					<td><strong><%= rs("Company") %></strong></td>
					<td><span class="tl-badge tl-badge-info"><%= rs("CustomerCode") %></span></td>
					<td><%= rs("Suburb") %>, <%= rs("State") %></td>
					<td><%= rs("ContactName") %></td>
					<td><%= rs("Phone") %></td>
					<td style="text-align: right;">
						<div class="tl-btn-group">
							<a href="Edit.asp?CompanyId=<%= rs("CompanyId") %>" target="_parent" class="tl-btn-icon" title="Edit">
								<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
							</a>
						</div>
					</td>
				</tr>
<%
		rs.MoveNext
	Loop
End If
rs.Close
%>
			</tbody>
		</table>
	</div>
</div>

<script>
	// Simple client-side search across visible rows
	window.parent.document.getElementById('Select2').addEventListener('change', function() {
		// Parent form will submit and reload this iframe
	});
</script>
</html>

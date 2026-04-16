<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim lngQuoteCOSId
lngQuoteCOSId = CLng(Request("QuoteCOSId"))

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

sql = "Select * From QuoteCOS Where QuoteCOSId = " & lngQuoteCOSId
Set rs = dbConn.Execute(sql)

%>
		<link rel="preconnect" href="https://fonts.googleapis.com">
		<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
		<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
		<script language="JavaScript">

		function emptyField(textObj) {
			if (textObj.value.length == 0) return true;
			for (var i=0; i < textObj.value.length; i++) {
				var ch = textObj.value.charAt(i);
				if (ch != ' ' && ch != '\t') return false;
			}
			return true
		}

		function checkForm() {

			var validFlag = true

			if (validFlag) {
				if (emptyField(document.Form1.QuoteCOS)) {
					alert("Please ensure that you have entered a Description.");
					validFlag = false;
					document.Form1.QuoteCOS.focus();
				}
			}

			if (validFlag) {
				if (emptyField(document.Form1.QuoteCOSFile)) {
					alert("Please ensure that you have selected a File to upload.");
					validFlag = false;
					document.Form1.QuoteCOSFile.focus();
				}
			}

			return validFlag
		}

		</script>
	</head>
	<body class="tl-bg-light">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
	<div class="tl-page-container">
		<nav class="tl-breadcrumb">
			<a href="/Portal.asp">Home</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<a href="Default.asp">Conditions of Sale</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<span>Edit File</span>
		</nav>

		<div class="tl-action-bar">
			<h1 class="tl-page-title">Edit Conditions of Sale</h1>
		</div>

		<div class="tl-main">
			<div class="tl-card">
				<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();" ENCTYPE="multipart/form-data" accept-charset="utf-8" class="tl-form">
					<input type="hidden" name="QuoteCOSId" value="<%= lngQuoteCOSId %>">
					
					<div class="tl-form-group">
						<label class="tl-label">Document Description <span class="tl-required">*</span></label>
						<input type="text" name="QuoteCOS" id="Text1" class="tl-input" placeholder="e.g. Standard Terms & Conditions 2024" value="<%= rs("QuoteCOS") %>">
					</div>

					<div class="tl-form-group">
						<label class="tl-label">Current File: <span style="font-weight: 400; color: var(--tl-text-light);"><%= rs("QuoteCOSFile") %></span></label>
						<label class="tl-label">Upload New File <span class="tl-required">*</span></label>
						<div style="padding: 32px; border: 2px dashed var(--tl-border); border-radius: 12px; text-align: center; background: #f8fafc; transition: all 0.2s ease;">
							<div style="margin-bottom: 12px; color: var(--tl-primary);">
								<svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="17 8 12 3 7 8"></polyline><line x1="12" y1="3" x2="12" y2="15"></line></svg>
							</div>
							<p style="font-size: 14px; color: var(--tl-text-light); margin-bottom: 16px;">Select a PDF or document file to replace the current one</p>
							<input type="file" name="QuoteCOSFile" id="QuoteCOSFile" class="tl-input" style="max-width: 400px; margin: 0 auto; display: block;">
						</div>
					</div>

					<div class="tl-form-actions" style="border-top: 1px solid var(--tl-border); padding-top: 24px; margin-top: 24px; display: flex; justify-content: flex-end; gap: 12px;">
						<button type="button" class="tl-btn" onclick="document.location.href='default.asp';">Cancel</button>
						<button type="submit" class="tl-btn tl-btn-primary">Update Document</button>
					</div>
				</form>
			</div>
		</div>
	</div>
	</body>
</html>
<%

rs.Close
Set rs = Nothing

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
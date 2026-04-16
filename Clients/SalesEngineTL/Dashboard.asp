<%
Option Explicit

' Techlight MyDesk - Modern Dashboard
'===============================================================================' REDESIGNED: Modular architecture for stability and maintainability.
' Widgets are located in /Clients/SalesEngineTL/Dashboard/Widgets/ ' ===============================================================================
%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngineTL/Dashboard/Dashboard_Data.asp"-->

<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Dashboard - Techlight MyDesk</title>
	<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-store">
	
	<!-- UI System -->
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
	<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
	<link rel="icon" type="image/x-icon" href="/favicon.ico">
	
	<% If isDirector1 Then %>
	<!-- Statistics Engine -->
	<script src="/System/js/chart.min.js"></script>
	<style>
		/* Dashboard Specific Micro-styles */
		.tl-kpi-card { transition: transform 0.3s ease, box-shadow 0.3s ease; cursor: default; }
		.tl-kpi-card:hover { transform: translateY(-5px); box-shadow: 0 8px 16px rgba(0,0,0,0.1) !important; }
		.tl-exception-item { display: flex; align-items: center; justify-content: space-between; padding: 12px; background: white; border-radius: 8px; margin-bottom: 8px; border: 1px solid #fed7d7; transition: all 0.2s; }
		.tl-exception-item:hover { border-color: #feb2b2; transform: translateX(5px); }
		.tl-exception-icon { width: 32px; height: 32px; background: #feb2b2; border-radius: 8px; display: flex; align-items: center; justify-content: center; }
		.tl-priority-item { display: flex; align-items: center; gap: 10px; padding: 12px; background: white; border-radius: 8px; text-decoration: none; border: 1px solid #fbd38d; transition: all 0.2s; }
		.tl-priority-item:hover { border-color: #f6ad55; transform: translateX(5px); box-shadow: 0 2px 4px rgba(0,0,0,0.05); }
		.tl-priority-icon { width: 32px; height: 32px; background: #fbd38d; border-radius: 8px; display: flex; align-items: center; justify-content: center; }
		.tl-btn-xs { padding: 4px 8px; font-size: 11px; border-radius: 4px; text-decoration: none; font-weight: 600; }
		.tl-btn-danger { background: #e53e3e; color: white; }
		.tl-action-logout:hover { filter: brightness(1.1); }
	</style>
	<% End If %>
</head>
<body class="tl-bg-light">
	<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
	
	<div class="tl-main">
		<!-- Welcome & Quick Nav -->
		<!--#include virtual="/Clients/SalesEngineTL/Dashboard/Widgets/Welcome.asp"-->
		
		<!-- Action Grid -->
		<!--#include virtual="/Clients/SalesEngineTL/Dashboard/Widgets/QuickActions.asp"-->
		
		<!-- Business Intelligence (Directors) -->
		<!--#include virtual="/Clients/SalesEngineTL/Dashboard/Widgets/KPI_Cards.asp"-->
		<!--#include virtual="/Clients/SalesEngineTL/Dashboard/Widgets/Charts.asp"-->
		<!--#include virtual="/Clients/SalesEngineTL/Dashboard/Widgets/Exceptions.asp"-->

		<!-- Performance & Activity Sections -->
		<div class="tl-dashboard" style="margin-top: 24px;">
			<!-- Side Panel -->
			<!--#include virtual="/Clients/SalesEngineTL/Dashboard/Widgets/Sidebar.asp"-->
			
			<!-- Main Feed -->
			<div style="display: flex; flex-direction: column; gap: 24px;">
				<!--#include virtual="/Clients/SalesEngineTL/Dashboard/Widgets/ActivityTable.asp"-->
				<!--#include virtual="/Clients/SalesEngineTL/Dashboard/Widgets/HelpResources.asp"-->
			</div>
		</div>
	</div>

	<!-- System Footer Scripts -->
	<!--#include virtual="/Clients/SalesEngineTL/Dashboard/Widgets/ChartScripts.asp"-->
	<!--#include virtual="/System/ssi_dbConn_close.inc"-->
</body>
</html>
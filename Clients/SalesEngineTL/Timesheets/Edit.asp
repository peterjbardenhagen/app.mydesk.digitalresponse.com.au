<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
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
			if (emptyField(document.Form1.Code)) {
				alert("Please select a User.");
				validFlag = false;
				document.Form1.Code.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.DateWeekEnding)) {
				alert("Please select Week Ending Date.");
				validFlag = false;
				document.Form1.DateWeekEnding.focus();
			}}
			
			if(validFlag){
				return validate(true);
			} else {
				return false;
			}
		}
		
		function clearLine(i, y) {
			//document.getElementById("Hours_" + i + "_" + y).selectedIndex = 0;
			document.getElementById("ItemTypeId_" + i + "_" + y).selectedIndex = 0;
			document.getElementById("timeFrom"+i+"_"+y).value = ""
			document.getElementById("timeTo"+i+"_"+y).value = ""
			resetActivities(i)
		}
		</script>
	</head>
	<body bgcolor="#dddddd">
<!--#include virtual="/System/ssi_Header.inc"-->
<%

Dim intTimesheetId
Dim rs
Dim sql
Dim intHoursPerWeek

intTimesheetId = CLng(Request("TimesheetId"))

rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Timesheets WHERE TimesheetId = " & intTimesheetId
Set rs = dbConn.Execute(sql)

intHoursPerWeek = GetHoursPerWeek(rs("Code"))

Set rsUsers = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Where Deleted = 0 AND Code = '" & rs("Code") & "'"
Set rsUsers = dbConn.Execute(sql)

If Not(rsUsers.BOF And rsUsers.EOF) Then
	intDivisionId = rsUsers("DivisionId")
	strName = rsUsers("Name")
	strCode = rsUsers("Code")
	strLineManagerCode = rsUsers("LineManagerCode")
End If	

rsUsers.Close
Set rsUsers = Nothing

If rs("TimesheetStatusId") = 2 And Not(CStr(Request.Cookies("UserSettings")("Code")) = CStr(Request.Cookies("UserSettings")("LineManagerCode"))) And Not(CBool(Request.Cookies("UserSettings")("Manager"))) And Not Request.Cookies("UserSettings")("UserTypeId") = 2 Then

%>
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table3">
		<tr>
			<td>
			<br/>
			<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Timesheets</a> / Edit Timesheet /></span>
			<br/><br/>This timesheet has been approved by your line manager. Please contact your line manager to make any changes.</td>
		</tr>
	</table>
<%

ElseIf rs("TimesheetStatusId") = 3 Then

%>
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table7">
		<tr>
			<td>
			<br/>
			<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Timesheets</a> / Edit Timesheet /></span>
			<br/><br/>This timesheet has been approved by payroll. You cannot make any changes.</td>
		</tr>
	</table>
<%

Else

%>
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Timesheets</a> / Edit Timesheet /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table width=770 cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Timesheets/Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<input type="hidden" name="TimesheetId" value="<%= intTimesheetId %>">
								<input type="hidden" name="Leave" value="1" ID="Hidden1">
								<tr>
									<td colspan=3>
<%

If rs("TimesheetStatusId") = 2 Then

%>
									This timesheet has been approved by your line manager.<br><br>
<%

End If

If intDivisionId = 3 Then ' TSA

%>
									<br><br><li><a href="../FilesLibrary/Files/TSA_Leave.pdf" target="_blank">Download a leave application form</a>. <b>Your leave will not be approved until you have submitted a leave request form.</b><br><br>
<%

ElseIf intDivisionId = 4 Then ' DENEEFE

%>
									<br><br><li><a href="../FilesLibrary/Files/Deneefe_Leave.pdf" target="_blank">Download a leave application form</a>. <b>Your leave will not be approved until you have submitted a leave request form.</b><br><br>
<%

End If

%>
									</td>
								</tr>
								<tr>
									<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1">&nbsp;<input type="submit" value="Submit" id="Submit1" NAME="Submit"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">User</td>
									<td valign="top"><%= strName %>
									<input type="hidden" name="Code" value="<%= strCode %>">
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Week Ending</td>
									<td valign="top">
									<select name="DateWeekEnding">
										<%= GetAllWorkingWeeksOfYear(Year(Now)) %>
									</select>
									<script language="javascript">
									for (var i=0;i<document.Form1.DateWeekEnding.options.length;i++) {
										if (document.Form1.DateWeekEnding.options[i].value == '<%= CDate(rs("DateWeekEnding")) %>')
											document.Form1.DateWeekEnding.options[i].selected = true;
									}
									</script>
									</td>
								</tr>
<%
	If Request.Cookies("UserSettings")("Manager") Or Request.Cookies("UserSettings")("UserTypeId") = 4 Or (Request.Cookies("UserSettings")("UserTypeId") = 2 And rs("TimesheetStatusId") = 2) Then
%>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Status</td>
									<td valign="top">
									<select name="TimesheetStatusId" ID="Select2" style="width:280px;">
										<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>

<%
		Set rsStatus = Server.CreateObject("ADODB.RecordSet")
		If rs("TimesheetStatusId") = 2 And Request.Cookies("UserSettings")("UserTypeId") = 2 Then
			sql = "SELECT * FROM TimesheetStatus WHERE TimesheetStatusId <> 1 ORDER BY TimesheetStatusId"
		Else
			sql = "SELECT * FROM TimesheetStatus WHERE TimesheetStatusId <> 3 ORDER BY TimesheetStatusId"
		End If
		Set rsStatus = dbConn.Execute(sql)

		If Not(rsStatus.BOF And rsStatus.EOF) Then
			Do Until rsStatus.EOF
				If CLng(rs("TimesheetStatusId")) = CLng(rsStatus("TimesheetStatusId")) Then
%>
										<option selected value="<%= rsStatus("TimesheetStatusId") %>"><%= rsStatus("TimesheetStatus") %></option>
<%
				Else
%>
										<option value="<%= rsStatus("TimesheetStatusId") %>"><%= rsStatus("TimesheetStatus") %></option>
<%
				End If
				rsStatus.MoveNext
			Loop
		End If

		If IsObject(rsStatus) Then
			rsStatus.Close
			Set rsStatus = Nothing
		End If
%>
									</select>
									</td>
								</tr>
<%
	Else
%>
								<input type="hidden" name="TimesheetStatusId" value="<%= rs("TimesheetStatusId") %>">
<%
	End If
%>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Entries</td>
									<td valign="top">
										<table width=550 cellpadding=3 cellspacing=0 border=0 ID="Table5">
<%
	' Get items as an array
	Dim arrTimesheetItems
	arrTimesheetItems = GetTimesheetItemsAsArr(intTimesheetId)

	Dim arrWeekDays
	arrWeekDays = Split("Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday", ",")

	i = 1
	Do Until i = 8
%>
											<tr>
												<td valign="top"><table><tr><td style="font-weight:bold;"><%= arrWeekDays(i-1) %></td></tr></table></td>
													<td><table><tr><td style="font-weight:bold;">Day Commenced at:</td><td><input type ="text" onchange="javascript:validateTime('startTime<%=i %>',true,true,<%=i %>,0)" name = "startTime<%=i %>" id = "startTime<%=i %>" value ="<% if arrTimesheetItems(i-1, 0, 2) > 1000 then Response.Write(arrTimesheetItems(i-1, 0, 2)) Else If arrTimesheetItems(i-1, 0, 2) > 0 Then Response.Write("0" & arrTimesheetItems(i-1, 0, 2)) end if  %>" style="width:50px;"/></td><td style="font-weight:bold;">Day Ended at:</td><td><input type ="text" onchange="javascript:validateTime('EndTime<%=i %>',true,false,<%=i %>,0)" name = "EndTime<%=i %>" id = "EndTime<%=i %>" value ="<% if arrTimesheetItems(i-1, 0, 3) > 1000 then Response.Write(arrTimesheetItems(i-1, 0, 3)) else if arrTimesheetItems(i-1, 0, 3) > 0 Then Response.Write("0" & arrTimesheetItems(i-1, 0, 3)) end if %>" style="width:50px;"/></td></tr></table></td>
											</tr>
											<tr>
											<td valign="top" style="font-weight:bold;">&nbsp;</td>
												<td valign="top">
												<table width=400 ID="Table6">
														<tr>
															<td style="font-weight:bold;">Time Commenced</td>
															<td style="font-weight:bold;">Time Completed</td>
															<td style="font-weight:bold;">Activity</td>
														</tr>
<%
		Dim y
		y = 1
		Do Until y = 9
%>
														<tr>
															<td valign="top" width=110>
																<input type ="text" onchange="javascript:validateTime('timeFrom<%=i %>_<%= y %>',false,true,<%=i %>,<%= y %>)" style="width:110px;" id="timeFrom<%=i %>_<%= y %>" name="timeFrom<%=i %>_<%= y %>" value ="<% if arrTimesheetItems(i-1, y, 2) > 1000 Or arrTimesheetItems(i-1, y, 2) = "" then Response.Write(arrTimesheetItems(i-1, y, 2)) else Response.Write("0" & arrTimesheetItems(i-1, y, 2)) end if %>" />
															</td>
															<td valign="top" width=110>
																<input type ="text" onchange="javascript:validateTime('timeTo<%=i %>_<%= y %>',false,false,<%= i %>,<%= y %>)" style="width:110px;" id="timeTo<%=i %>_<%= y %>" name="timeTo<%=i %>_<%= y %>" value ="<% if arrTimesheetItems(i-1, y, 3) > 1000 Or arrTimesheetItems(i-1, y, 3) = "" then Response.Write(arrTimesheetItems(i-1, y, 3)) else Response.Write("0" & arrTimesheetItems(i-1, y, 3)) end if %>" />
															</td>
															<td valign="top">
															<select name="ItemTypeId_<%= i %>_<%= y %>" onchange="javascript:validateActivity(<%= i %>,<%= y %>);" style="width:150px;" id="ItemTypeId_<%= i %>_<%= y %>">
																<%= GetActivityTypes(CLng(arrTimesheetItems(i-1, y, 4)),i,y	) %>															
															</select>
															</td>
															<td valign="top"><input type="button" value="Clear" onclick="clearLine(<%= i %>, <%= y %>);" id="Clear<%= i %>_<%= y %>"></td>
														</tr>
<%
			y = y + 1
		Loop
%>
													</table>
												</td>
											</tr>
<%
		i = i + 1
	Loop
%>
											<tr>
												<td></td>
											</tr>
										</table>
									</td>
								</tr>				
								<tr>
									<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};">&nbsp;<input type="submit" value="Submit" id="Submit" NAME="Submit"></td>
								</tr>
								</form>
							</table>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
	</body>
</html>
<script language="javascript">
	var arrWeekDays = new Array("Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday","Sunday")

	function confirmLeaveForm() {
		if(confirm('If you are taking leave, do you want to download a leave form?')) {
			document.Form1.Leave.value = 1;
		} else {
			document.Form1.Leave.value = 2;
		}
	}

	function validate(boolSubmit) {
		var i = 1;
		var y = 1;
		var hours = 0;
		var itemTypeId = 0;
		var totalHours = 0;
		var totalDayHours = 0;
		var boolTooManyHours = false;
		/*while (i<=7){
			while (y<=5){
				hours = parseFloat(document.getElementById("Hours_" + i + "_" + y).value);
				itemTypeId = parseInt(document.getElementById("ItemTypeId_" + i + "_" + y).value);
				if(hours > 0 && itemTypeId > 0) {
					totalHours = totalHours + parseFloat(hours);
					totalDayHours = totalDayHours + parseFloat(hours);
				}
				y++
			}
			if (totalDayHours > <%= Request.Cookies("UserSettings")("HoursPerDay") %>) {
				boolTooManyHours = true;
				alert('Please check ' + arrWeekDays[i-1] + ' as there are too many hours in one day.');
			}
			totalDayHours = 0;
			y = 1;
			i++
		}*/
		if(!boolTooManyHours) {
			//if(totalHours == <%= Request.Cookies("UserSettings")("HoursPerWeek") %>) {
			if(true) {
				if(boolSubmit) {
					confirmLeaveForm();
					return true;
				} else {
					alert('The timesheet is valid.');
				}
			} else {
				var diff = <%= Request.Cookies("UserSettings")("HoursPerWeek") %> - totalHours;
<%
If Request.Cookies("UserSettings")("Manager") Or strLineManagerCode = Request.Cookies("UserSettings")("Code") Then
%>
				if(boolSubmit) {
					if(diff > 0) {
						return confirm('There are ' + diff + ' hours of activities missing. Please fix and try submitting again. Would you like to continue anyway?');
					} else {
						return confirm('There are ' + -diff + ' too many hours. Please fix and try submitting again. Would you like to continue anyway?');
					}
				} else {
					if(diff > 0) {
						alert('There are ' + diff + ' hours of activities missing. Please fix and try submitting again. Only a manager, or line manager can vary the number of hours.');
					} else {
						alert('There are ' + -diff + ' too many hours. Please fix and try submitting again. Only a manager, or line manager can vary the number of hours.');
					}
				}
<%
Else
%>
				if(diff > 0) {
					alert('There are ' + diff + ' hours of activities missing. Please fix and try submitting again. Only a manager, or line manager can vary the number of hours.');
				} else {
					alert('There are ' + diff + ' too many hours. Please fix and try submitting again. Only a manager, or line manager can vary the number of hours.');
				}
<%
End If
%>
				return false;
			}
		} else {
			return false;
		}
	}
<%

x = 0 ' Days
y = 0 ' Items
z = 0 ' Item Type

Do Until x = 6
	y = 0
	Do Until y = 8
		If IsNumeric(arrTimesheetItems(x,y,0)) Then
			If CLng(arrTimesheetItems(x,y,0)) > 0 Then
	'			z = 0
	'			Do Until z = 4
%>
// Hours
for (var i=0;i<document.Form1.Hours_<%= x+1 %>_<%= y+1 %>.options.length;i++) {
	if (document.Form1.Hours_<%= x+1 %>_<%= y+1 %>.options[i].value == '<%= arrTimesheetItems(x,y,0) %>')
		document.Form1.Hours_<%= x+1 %>_<%= y+1 %>.options[i].selected = true;
}
// Activity
for (var i=0;i<document.Form1.ItemTypeId_<%= x+1 %>_<%= y+1 %>.options.length;i++) {
	if (document.Form1.ItemTypeId_<%= x+1 %>_<%= y+1 %>.options[i].value == '<%= arrTimesheetItems(x,y,4) %>')
		document.Form1.ItemTypeId_<%= x+1 %>_<%= y+1 %>.options[i].selected = true;
}

for (var i=0;i<document.getElementById("ProjectId_<%= x+1 %>_<%= y+1 %>").options.length;i++) {
	if (document.getElementById("ProjectId_<%= x+1 %>_<%= y+1 %>").options[i].value == '<%= arrTimesheetItems(x,y,3) %>') {
		document.getElementById("ProjectId_<%= x+1 %>_<%= y+1 %>").options[i].selected = true;
	}
}

<%
			End If
		End If
		y = y + 1
	Loop
	x = x + 1
Loop

%>
	function validateTime(textBox,blnWorkDayField,blnStartTime,intDayOfWeek,intTimeSheetMember)
	{
				
		if(document.getElementById(""+textBox+"").value == "") 
		{
			return;
		}
		var str = document.getElementById(""+textBox+"").value	
		if(str.substr(0,1) != "0" && str.length < 4)
		{
			document.getElementById(""+textBox+"").value = "0" + str
		}
		
		if (IsNumeric(document.getElementById(""+textBox+"").value)==true)
		{
			if (parseInt(document.getElementById(""+textBox+"").value) > 2400)
			{
				alert('Please check the time: ' + document.getElementById(""+textBox+"").value + ' as this input is not in 24 hour time.');
				document.getElementById(""+textBox+"").value = ""
				return;
			}
			tmpNum = document.getElementById(""+textBox+"").value - 60;
			if( tmpNum % 100 < 40)
			{
				alert('Please check the time: ' + document.getElementById(""+textBox+"").value + ' as this input is not in 24 hour time.');
				document.getElementById(""+textBox+"").value = "";
				document.getElementById(""+textBox+"").focus();
				return;
			}
		}
		else
		{
			alert('Please check the time entered: \"' + document.getElementById(""+textBox+"").value + '\" as this is not an integer.');
			document.getElementById(""+textBox+"").value = ""
		}
		if(blnWorkDayField!=true)
		{
			checkInterval(document.getElementById(""+textBox+"").value,intDayOfWeek,intTimeSheetMember,document.getElementById(""+textBox+""))
			if(document.getElementById(""+textBox+"").value != "") 
			{
				if(blnStartTime!=true)
				{
					//check time completed occurs after time started
					if (document.getElementById(""+textBox+"").value < document.getElementById("timeFrom"+intDayOfWeek+"_"+intTimeSheetMember).value)
					{
						alert('The time completed for an activity can not be earlier than the time started');
						document.getElementById(""+textBox+"").value = document.getElementById("timeFrom"+intDayOfWeek+"_"+intTimeSheetMember).value;
						return;
					}
					//check time does not go beyond final time for day
					else if(document.getElementById(""+textBox+"").value != "" && document.getElementById("timeFrom"+intDayOfWeek+"_"+intTimeSheetMember).value == "")
					{
						alert('You need to enter a starting time for the activity before you enter an end time')
						document.getElementById(""+textBox+"").value = ""
					}
					else if(document.getElementById(""+textBox+"").value > document.getElementById("EndTime"+intDayOfWeek).value)
					{
						alert('The time completed for an activity can not be later than the time the day ended');
						document.getElementById(""+textBox+"").value = document.getElementById("EndTime"+intDayOfWeek).value;
						return;
					}
					
				}
				else
				{
					//check activity time does not occur before the start of the day
					if(document.getElementById(""+textBox+"").value < document.getElementById("startTime"+intDayOfWeek).value)
					{
						alert('The time completed for an activity can not be earlier than the time the day started');
						document.getElementById(""+textBox+"").value = document.getElementById("startTime"+intDayOfWeek).value;
						return;
					}
					else if(document.getElementById(""+textBox+"").value > document.getElementById("EndTime"+intDayOfWeek).value)
					{
						alert('The time completed for an activity can not be later than the time the day ended');
						document.getElementById(""+textBox+"").value = document.getElementById("startTime"+intDayOfWeek).value;
						return;
					}
					else if(document.getElementById("startTime"+intDayOfWeek).value == "" && document.getElementById(""+textBox+"").value != "")
					{
						alert('You need to enter a starting and ending time for the day before you enter an activity');
						document.getElementById(""+textBox+"").value = ""
						return;
					}
					else
					{
						document.getElementById("timeTo"+intDayOfWeek+"_"+intTimeSheetMember).value = document.getElementById(""+textBox+"").value;
					}
				}
			}	
				
		}
		else if(blnStartTime!=true)
		{
			//check the starting time of the day is not greater than the end
			if (document.getElementById(""+textBox+"").value < document.getElementById("startTime"+intDayOfWeek).value)
			{
				alert('The time completed for the day can not be earlier than the time started');
				document.getElementById(""+textBox+"").value = document.getElementById("startTime"+intDayOfWeek).value;
				return;
			}
		}	
	}
	//checks that the time has not already been filled by another activity
	function checkInterval(intTime,intDayOfWeek,intTimeSheetMember,textBox)
	{
		for (i = 1; i < 8; i++)
		{
			if(i!=intTimeSheetMember)
			{
				if(document.getElementById("timeFrom"+intDayOfWeek+"_"+i).value != "" || document.getElementById("timeTo"+intDayOfWeek+"_"+i).value != "")
				{
					if(intTime > document.getElementById("timeFrom"+intDayOfWeek+"_"+i).value && intTime < document.getElementById("timeTo"+intDayOfWeek+"_"+i).value)
					{
						alert('That time has already been filled by another activity, please enter a different time for this actvity');
						textBox.value = "";
						textBox.focus();
						return;
					}
				}
			}
		}
	}
	function IsNumeric(strString)
	//  check for valid numeric strings	
    {
		var strValidChars = "0123456789.-";
		var strChar;
		var blnResult = true;

		   if (strString.length == 0) return false;

		//  test strString consists of valid characters listed above
		for (i = 0; i < strString.length && blnResult == true; i++)
		{
			strChar = strString.charAt(i);
			if (strValidChars.indexOf(strChar) == -1)
			{
				blnResult = false;
			}
		}
		return blnResult;
	}
  function blockActivities(intDayIndex,intActivityIndex)
   {
//		alert("Cancel all activities for that day" + intDayIndex)
		//traverse all fields for that day and set to null
		var y = 1;
		while (y<9){
			if (y > 1)
			{
				
			//	alert(intActivityIndex + " " + y + " " + intDayIndex)	
				if(document.getElementById("timeTo" + intDayIndex + "_" + intActivityIndex).value != "" && document.getElementById("timeFrom" + intDayIndex + "_" + intActivityIndex).value != "")
				{
					document.getElementById("ItemTypeId_" + intDayIndex + "_" + y).disabled=true;
					document.getElementById("timeTo" + intDayIndex + "_" + y).disabled=true;
					document.getElementById("timeFrom" + intDayIndex + "_" + y).disabled=true;
					document.getElementById("ItemTypeId_" + intDayIndex + "_" + y).style.visibility = "hidden";
					document.getElementById("timeTo" + intDayIndex + "_" + y).style.visibility = "hidden"; 
					document.getElementById("timeFrom" + intDayIndex + "_" + y).style.visibility = "hidden";	
					document.getElementById("Clear" + intDayIndex + "_" + y).style.visibility = "hidden";
				}
				//alert(document.getElementsByName("ItemTypeId_" + intDayIndex + "_" + y).value)
			}
			/*else
			{
				document.getElementById("timeFrom" + intDayIndex + "_" + y).value='0900';
				document.getElementById("timeTo" + intDayIndex + "_" + y).value='1700';
			}*/
			
			//alert("The i value: " + intDayIndex + " The y value: " + y)
			y++;
		}
   }
     function resetActivities(intDayIndex, intActivityIndex)
   {
//		alert("Reactivate all activities for that day" + intDayIndex)
		//traverse all fields for that day and set to null
		var y = 1;
		while (y<9){
			if (y > 1)
			{
				document.getElementById("ItemTypeId_" + intDayIndex + "_" + y).disabled=false;
				document.getElementById("ItemTypeId_" + intDayIndex + "_" + y).style.visibility = "visible";
				//alert(document.getElementsByName("ItemTypeId_" + intDayIndex + "_" + y).value)
			}
			
			document.getElementById("timeTo" + intDayIndex + "_" + y).disabled=false;
			document.getElementById("timeFrom" + intDayIndex + "_" + y).disabled=false;	
			document.getElementById("timeTo" + intDayIndex + "_" + y).style.visibility = "visible";
			document.getElementById("timeFrom" + intDayIndex + "_" + y).style.visibility = "visible";	
			document.getElementById("Clear" + intDayIndex + "_" + y).style.visibility = "visible";
			//alert("The i value: " + intDayIndex + " The y value: " + y)
			y++;
		}
   }
   function validateActivity(intDayIndex,intItemIndex)
   {
		if(document.getElementById("timeTo" + intDayIndex + "_" + intItemIndex).value == "" || document.getElementById("timeFrom" + intDayIndex + "_" + intItemIndex).value == "")
		{
			alert('Not all times have been entered for this field, please re-enter details')
			document.getElementById("timeFrom" + intDayIndex + "_" + intItemIndex).value = ""
			document.getElementById("timeTo" + intDayIndex + "_" + intItemIndex).value = ""
			document.getElementById("ItemTypeId_" + intDayIndex + "_" + intItemIndex).value = 0
			resetActivities(intDayIndex, "")
		}
		if(document.getElementById("ItemTypeId_" + intDayIndex + "_" + intItemIndex).value ==3 || document.getElementById("ItemTypeId_" + intDayIndex + "_" + intItemIndex).value ==12)
		{
			resetActivities(intDayIndex, "")
		}
		else
		{
			blockActivities(intDayIndex,intItemIndex)
		}
   }
</script>
<%
End If
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<!--

Server Time Now = <%= Now() %>
Local Time Now = <%= ServerToEST(Now()) %>

-->



<script language="javascript">

// Disable Right Click
function right(e) {
	if (parseInt(document.location.host.indexOf('dev.mydesk.com.au')) != -1 || parseInt(document.location.href.indexOf('FilesLibrary')) == -1) {
		if (navigator.appName == 'Netscape' && (e.which == 3 || e.which == 2)) {
			return false;
		} else if (navigator.appName == 'Microsoft Internet Explorer' && (event.button == 2 || event.button == 3)) {                   
			return false;
		}
		return true;
	}  else {
		return true;
	}
}

if (parseInt(document.location.host.indexOf('dev.mydesk.com.au')) != -1 || parseInt(document.location.href.indexOf('FilesLibrary')) == -1) {
	document.onmousedown = right;

	if (document.layers) {
		window.captureEvents(Event.MOUSEDOWN);
	}

	window.onmousedown = right;

//	document.oncontextmenu = function() {
//		alert('Function disabled');
//		return false;
//	}
}

</script>
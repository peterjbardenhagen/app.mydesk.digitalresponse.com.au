<%
Option Explicit

Dim dteCurrentUTC
dteCurrentUTC = currentUTC

Function UTCToEST(dteDate)
'	Dim intPlusGMT
'	intPlusGMT = 0
'	dteDate = DateAdd("h", intPlusGMT, dteDate)
'	UTCToEST = CDate(dteDate)
	UTCToEST = CDate(dteDate)
End Function

Function ESTToUTC(dteDate)
'	Dim intPlusGMT
'	intPlusGMT = 0
'	dteDate = DateAdd("h", -intPlusGMT, dteDate)
'	ESTToUTC = CDate(dteDate)
	ESTToUTC = CDate(dteDate)
End Function

Function ServerToUTC(dteDate)
'	Dim intDiff
'	intDiff = DateDiff("h", Now(), currentUTC())
'	dteDate = DateAdd("h", intDiff, dteDate)
'	ServerToUTC = CDate(dteDate)
	ServerToUTC = CDate(dteDate)
End Function

Function ServerToEST(dteDate)
'	dteDate = ServerToUTC(dteDate)
'	dteDate = UTCToEST(dteDate)
'	ServerToEST = dteDate
	ServerToEST = CDate(dteDate)
End Function

%>

<script language=jscript runat=server>
function currentUTC(){
	var d, s;
	d = new Date();
//	s = d.toGMTString();
//	s = s.substring(4,s.length);
//	s = s.replace(" UTC","");
	s = d;
	return(s);
}
</script>
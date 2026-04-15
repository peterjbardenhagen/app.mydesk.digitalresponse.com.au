<%

Set dbConnTMDSQL = Server.CreateObject("ADODB.Connection")
strConn = "Provider=SQLOLEDB;Data Source=sql32000;Initial Catalog=Traffi;User ID=Traffi;Password=rqebu+zp;"
dbConnTMDSQL.ConnectionTimeout = 15
dbConnTMDSQL.CommandTimeout = 30
dbConnTMDSQL.ConnectionString = strConn
dbConnTMDSQL.Open

%>
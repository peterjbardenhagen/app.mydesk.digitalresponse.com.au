$conn = New-Object System.Data.SqlClient.SqlConnection('Server=(localdb)\MSSQLLocalDB;Database=Techlight_MyDesk;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;');
$conn.Open();
$cmd = $conn.CreateCommand();
$cmd.CommandText = "SELECT * FROM QuoteStatus";
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd);
$dt = New-Object System.Data.DataTable;
$adapter.Fill($dt);
$dt | Format-Table -AutoSize;
$conn.Close();

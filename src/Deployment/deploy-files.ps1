param([string]$NeedsAdmin = "check")

$pub  = 'C:\Development\Techlight-Projects\Techlight.digitalresponse.com.au\src\Deployment\publish'
$path = 'C:\inetpub\wwwroot\DR.MyDesk'
$site = 'DR.MyDesk'

Write-Host "Stopping app pool '$site'..." -ForegroundColor Yellow
& C:\Windows\System32\inetsrv\appcmd.exe stop apppool "/apppool.name:$site" 2>&1 | Out-Null

Write-Host "Ensuring destination exists: $path" -ForegroundColor Yellow
if (!(Test-Path $path)) {
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

Write-Host "Copying files from publish -> IIS..." -ForegroundColor Yellow
robocopy $pub $path /MIR /XD Logs /NJH /NJS /NP

Write-Host "Starting app pool '$site'..." -ForegroundColor Yellow
& C:\Windows\System32\inetsrv\appcmd.exe start apppool "/apppool.name:$site" 2>&1 | Out-Null

Write-Host "Done! Site deployed to $path" -ForegroundColor Green

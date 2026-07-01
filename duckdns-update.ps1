<#
.SYNOPSIS
    Updates DuckDNS with the current public IP.
    Schedule via Task Scheduler to run every 5 minutes.

.SETUP
    1. Register at https://www.duckdns.org — free, no monthly emails
    2. Create a subdomain e.g. pb-legion → pb-legion.duckdns.org
    3. Copy your token from the dashboard
    4. Edit $Domain and $Token below
    5. Run once manually, then:
         Register-ScheduledTask -Xml (Get-Content .\duckdns-task.xml -Raw) -TaskName "DuckDNS Update"
       Or create manually in Task Scheduler:
         Program: powershell.exe
         Arguments: -WindowStyle Hidden -ExecutionPolicy Bypass -File "C:\path\to\duckdns-update.ps1"
         Trigger: Every 5 minutes, indefinitely
#>

$Domain = "pb-legion"           # ← your DuckDNS subdomain (without .duckdns.org)
$Token  = "YOUR-TOKEN-HERE"     # ← from duckdns.org dashboard

$url = "https://www.duckdns.org/update?domains=$Domain&token=$Token&ip="

try {
    $result = Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 10
    $body   = $result.Content.Trim()
    $ts     = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    if ($body -eq "OK") {
        # Success — log is optional, comment out if noisy
        # Add-Content "$PSScriptRoot\duckdns.log" "$ts  OK"
    } else {
        Add-Content "$env:TEMP\duckdns.log" "$ts  FAILED: $body"
    }
} catch {
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Add-Content "$env:TEMP\duckdns.log" "$ts  ERROR: $_"
}

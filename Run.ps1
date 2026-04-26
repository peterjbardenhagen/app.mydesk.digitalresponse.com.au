# ============================================================================
#  DR MyDesk - Self-Healing Launcher with AI
#  Version 4.0 - April 2026
# ============================================================================

param(
    [switch]$SelfHealing = $false,
    [switch]$NoSelfHealing = $false
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Src = "$Root\src"
$Web = "$Src\MyDesk.Web"
$LogsDir = "$Web\Logs"

# ============================================================================
#  AI Self-Healing Module
# ============================================================================

class SelfHealingAI {
    [string]$ApiKey
    [string]$Endpoint = "https://api.openai.com/v1/chat/completions"
    [string]$Model = "gpt-5.4-scan"
    
    SelfHealingAI([string]$apiKey) {
        $this.ApiKey = $apiKey
    }
    
    [PSCustomObject] AnalyzeError([string]$errorLog, [string]$sourceCode) {
        $headers = @{
            "Content-Type" = "application/json"
            "Authorization" = "Bearer $($this.ApiKey)"
        }
        
        $prompt = @"
You are an expert .NET and Blazor developer specializing in error diagnosis and automatic fixes.

ERROR LOG:
$errorLog

SOURCE CODE (relevant file):
$sourceCode

Analyze this error and provide:
1. Root cause analysis
2. Specific fix (exact code changes needed)
3. File path to modify
4. Line numbers to change

Format your response as JSON:
{
    "rootCause": "brief explanation",
    "fixType": "compilation|runtime|database|configuration",
    "filePath": "relative path from project root",
    "lineNumber": 123,
    "originalCode": "exact code to replace",
    "fixedCode": "exact replacement code",
    "confidence": 0.95,
    "explanation": "brief explanation"
}

Only provide fixes for the actual error. If you cannot confidently fix it, set confidence < 0.5.
"@
        
        $body = @{
            model = $this.Model
            messages = @(
                @{ role = "system"; content = "You are a .NET expert that diagnoses and fixes errors. Always respond with valid JSON." }
                @{ role = "user"; content = $prompt }
            )
            temperature = 0.1
            response_format = @{ type = "json_object" }
        } | ConvertTo-Json -Depth 10
        
        try {
            $response = Invoke-RestMethod -Uri $this.Endpoint -Method Post -Headers $headers -Body $body -TimeoutSec 30
            $content = $response.choices[0].message.content | ConvertFrom-Json
            return $content
        }
        catch {
            Write-Warning "AI analysis failed: $_"
            return $null
        }
    }
    
    [bool] ApplyFix([PSCustomObject]$fix) {
        if ($fix.confidence -lt 0.7) {
            Write-Warning "AI confidence too low ($($fix.confidence)). Fix not applied."
            return $false
        }
        
        $filePath = Join-Path $Root $fix.filePath
        if (-not (Test-Path $filePath)) {
            Write-Warning "File not found: $filePath"
            return $false
        }
        
        $content = Get-Content $filePath -Raw -Encoding UTF8
        if ($content -notmatch [regex]::Escape($fix.originalCode)) {
            Write-Warning "Original code not found in file. Fix not applied."
            return $false
        }
        
        $newContent = $content -replace [regex]::Escape($fix.originalCode), $fix.fixedCode
        Set-Content $filePath $newContent -Encoding UTF8 -NoNewline
        
        Write-Host "✓ Fix applied to $($fix.filePath):$($fix.lineNumber)" -ForegroundColor Green
        Write-Host "  $($fix.explanation)" -ForegroundColor Cyan
        return $true
    }
}

# ============================================================================
#  Console Error Capture
# ============================================================================

function Start-BlazorServerWithCapture {
    param(
        [string]$Url = "http://localhost:5235"
    )
    
    $logFile = "$LogsDir\console-capture-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
    $errorPattern = "(ERR|ERROR|Exception|Failed)"
    
    Write-Host "Starting Blazor Server with error capture..." -ForegroundColor Cyan
    Write-Host "Log file: $logFile" -ForegroundColor Gray
    
    $process = Start-Process -FilePath "dotnet" -ArgumentList "run --urls $Url" -WorkingDirectory $Web -NoNewWindow -RedirectStandardOutput $logFile -RedirectStandardError $logFile -PassThru
    
    # Monitor for errors
    $startTime = Get-Date
    $errorsFound = @()
    
    while (-not $process.HasExited) {
        Start-Sleep -Seconds 2
        
        if (Test-Path $logFile) {
            $newContent = Get-Content $logFile -Tail 50 -ErrorAction SilentlyContinue
            foreach ($line in $newContent) {
                if ($line -match $errorPattern) {
                    $errorsFound += $line
                }
            }
        }
        
        # Stop monitoring after 30 seconds of startup
        if ((Get-Date) - $startTime -gt [TimeSpan]::FromSeconds(30)) {
            break
        }
    }
    
    return @{
        Process = $process
        LogFile = $logFile
        Errors = $errorsFound
    }
}

function Invoke-SelfHealing {
    param(
        [string[]]$Errors,
        [string]$LogFile
    )
    
    Write-Host "`n=== SELF-HEALING AI ACTIVATED ===" -ForegroundColor Yellow
    
    # Load configuration
    $configPath = "$Web\appsettings.json"
    $config = Get-Content $configPath | ConvertFrom-Json
    
    if (-not $config.SelfHealing.ApiKey) {
        Write-Warning "Self-healing API key not configured in appsettings.json"
        Write-Host "Add 'SelfHealing.ApiKey' to appsettings.json to enable AI fixes" -ForegroundColor Gray
        return $false
    }
    
    $ai = [SelfHealingAI]::new($config.SelfHealing.ApiKey)
    
    foreach ($errorMsg in $Errors) {
        Write-Host "`nAnalyzing error: $errorMsg" -ForegroundColor Cyan
        
        # Try to extract file path from error
        if ($errorMsg -match "at (.+\.razor|.+\.cs):line (\d+)") {
            $filePath = $matches[1]
            
            $fullPath = Get-ChildItem -Path $Root -Recurse -Filter $filePath | Select-Object -First 1
            if ($fullPath) {
                $sourceCode = Get-Content $fullPath.FullName -Raw -Encoding UTF8
                
                $fix = $ai.AnalyzeError($errorMsg, $sourceCode)
                if ($fix) {
                    $ai.ApplyFix($fix)
                }
            }
        }
    }
    
    Write-Host "`n=== SELF-HEALING COMPLETE ===" -ForegroundColor Yellow
    return $true
}

# ============================================================================
#  Main Menu
# ============================================================================

function Show-Menu {
    Clear-Host
    Write-Host ""
    Write-Host " ==============================================================="
    Write-Host ""
    Write-Host "         DR MyDesk - Self-Healing Launcher"
    Write-Host "            Version 4.0  -  .NET 8 Blazor Server"
    Write-Host ""
    Write-Host " ==============================================================="
    Write-Host ""
    
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if ($isAdmin) {
        Write-Host "   [OK]   Administrator        Full access to all options"
    }
    else {
        Write-Host "   [ !]   Standard User        Options 1, 2, 3 will request elevation"
    }
    
    if ($SelfHealing -and -not $NoSelfHealing) {
        Write-Host "   [AI]   Self-Healing ENABLED  AI-powered error fixing"
    }
    
    Write-Host ""
    Write-Host " ---------------------------------------------------------------"
    Write-Host ""
    Write-Host "   SETUP  (run once, in order)"
    Write-Host ""
    Write-Host "     [1]  Database      Migrate Access DB to SQL Server"
    Write-Host "     [2]  IIS Deploy    Build and publish to local IIS"
    Write-Host "     [3]  Run Tests     Playwright E2E tests (72+)"
    Write-Host ""
    Write-Host "   RUN THE APP"
    Write-Host ""
    Write-Host "     [4]  Launch        Standalone (Kestrel) or IIS"
    Write-Host ""
    Write-Host "   INFO"
    Write-Host ""
    Write-Host "     [5]  Status        Check IIS site, ports, processes"
    Write-Host "     [6]  Open Docs     README.md / TESTING.md"
    Write-Host ""
    Write-Host "     [Q]  Quit"
    Write-Host ""
    Write-Host " ---------------------------------------------------------------"
    Write-Host ""
}

# ============================================================================
#  Main Execution
# ============================================================================

if ($SelfHealing -and -not $NoSelfHealing) {
    Write-Host "Self-healing mode enabled. AI will attempt to fix errors automatically." -ForegroundColor Yellow
    Write-Host ""
}

while ($true) {
    Show-Menu
    $choice = Read-Host "   Choose an option"
    
    switch ($choice) {
        "1" { 
            Write-Host "Running database migration..." -ForegroundColor Cyan
            & "$Src\Deployment\Migration\Install.ps1"
        }
        "2" {
            Write-Host "Running IIS deployment..." -ForegroundColor Cyan
            & "$Src\Deployment\Deploy.ps1"
        }
        "3" {
            Write-Host "Running Playwright tests..." -ForegroundColor Cyan
            Push-Location "$Root\tests\MyDesk.PlaywrightTests"
            dotnet test --logger "console;verbosity=normal" --logger "trx;LogFileName=test-results.trx" --logger "html;LogFileName=test-results.html"
            Pop-Location
        }
        "4" {
            Write-Host ""
            Write-Host "   [1]  Standalone (Kestrel)    Quick dev server"
            Write-Host "        URL: http://localhost:5235"
            Write-Host ""
            Write-Host "   [2]  Local IIS               Production-like"
            Write-Host ""
            $runChoice = Read-Host "   Choose"
            
            if ($runChoice -eq "1") {
                if ($SelfHealing -and -not $NoSelfHealing) {
                    $result = Start-BlazorServerWithCapture
                    if ($result.Errors.Count -gt 0) {
                        Invoke-SelfHealing -Errors $result.Errors -LogFile $result.LogFile
                    }
                }
                else {
                    Push-Location $Web
                    dotnet run --urls "http://localhost:5235"
                    Pop-Location
                }
            }
            elseif ($runChoice -eq "2") {
                Write-Host "Starting IIS..." -ForegroundColor Cyan
                Start-Process "http://localhost"
            }
        }
        "5" {
            Write-Host "System status..." -ForegroundColor Cyan
            netstat -ano | Select-String ":5235"
            netstat -ano | Select-String ":80"
        }
        "6" {
            Start-Process "$Root\README.md"
        }
        "Q" {
            Write-Host "Goodbye!"
            exit
        }
        default {
            Write-Host "Invalid choice." -ForegroundColor Red
            Start-Sleep -Seconds 2
        }
    }
    
    if ($choice -ne "Q") {
        Write-Host "`nPress Enter to continue..."
        Read-Host
    }
}

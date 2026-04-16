Get-ChildItem -Path '.' -Filter '*.asp' -Recurse | Where-Object { 
    $_.FullName -notlike '*System*' -and $_.FullName -notlike '*Errors*' 
} | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    
    # Skip if already has Option Explicit (not commented)
    if ($content -match '<%[\s\n]*Option Explicit') { 
        Write-Host "SKIP: $($_.Name)"
        return 
    }
    
    # Check if has commented Option Explicit
    if ($content -match "'Option Explicit") {
        # Uncomment it
        $content = $content -replace "'Option Explicit", "Option Explicit"
        Write-Host "UNCOMMENT: $($_.Name)"
    }
    else {
        # Add Option Explicit after <%
        $content = $content -replace '^<%\s*\r?\n', "<%`r`nOption Explicit`r`n`r`n"
        Write-Host "ADD: $($_.Name)"
    }
    
    Set-Content $_.FullName $content -NoNewline
}
Write-Host "Done!"

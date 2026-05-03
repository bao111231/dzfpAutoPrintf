param(
    [string]$Token = $env:GITHUB_TOKEN
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($Token)) {
    Write-Host "Error: GitHub Token is required!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  1. Set environment variable: `$env:GITHUB_TOKEN='your_token'" -ForegroundColor Gray
    Write-Host "  2. Or pass as parameter: .\create-release.ps1 -Token 'your_token'" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

Write-Host "Starting GitHub Release creation..." -ForegroundColor Cyan

$headers = @{
    "Accept" = "application/vnd.github+json"
    "Authorization" = "Bearer $Token"
    "X-GitHub-Api-Version" = "2022-11-28"
}

$releaseBody = @"
v1.2.0 - Auto-start Monitoring & Single Process

New Features:
- Auto-start monitoring on boot (no manual action needed)
- Fixed duplicate process issue (was starting twice)
- System tray support with smart notifications
- Improved error handling for auto-start mode

Versions:
- Light version (~190 KB): Requires .NET 8.0 Runtime
- Full version (~154 MB): Standalone, no dependencies
"@

$body = @{
    tag_name = "v1.2.0"
    name = "v1.2.0 - Auto-start Monitoring Fix"
    body = $releaseBody
    draft = $false
    prerelease = $false
} | ConvertTo-Json -Depth 3

try {
    Write-Host "`n[1/3] Creating Release..." -ForegroundColor Yellow
    
    $response = Invoke-RestMethod -Uri "https://api.github.com/repos/bao111231/dzfpAutoPrintf/releases" `
        -Method Post `
        -Headers $headers `
        -ContentType "application/json" `
        -Body $body
    
    Write-Host "Release created successfully!" -ForegroundColor Green
    Write-Host "Release ID: $($response.id)" -ForegroundColor Gray
    Write-Host "URL: $($response.html_url)" -ForegroundColor Gray
    
    $uploadUrl = $response.upload_url -replace '\{.*\}', ''
    
    Write-Host "`n[2/3] Uploading Light version..." -ForegroundColor Yellow
    
    $lightFile = "publish-lightweight\DzfpPdfPrinter.exe"
    if (Test-Path $lightFile) {
        $fileSize = [math]::Round((Get-Item $lightFile).Length / 1MB, 2)
        Write-Host "File size: ${fileSize} MB" -ForegroundColor Gray
        
        Invoke-RestMethod -Uri "${uploadUrl}?name=DzfpPdfPrinter-Light-v1.2.0.exe" `
            -Method Post `
            -Headers @{
                "Accept" = "application/vnd.github+json"
                "Authorization" = "Bearer $Token"
                "Content-Type" = "application/octet-stream"
            } `
            -InFile $lightFile | Out-Null
        
        Write-Host "Light version uploaded!" -ForegroundColor Green
    } else {
        Write-Host "File not found: $lightFile" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`n[3/3] Uploading Full version..." -ForegroundColor Yellow
    
    $fullFile = "publish\DzfpPdfPrinter.exe"
    if (Test-Path $fullFile) {
        $fileSize = [math]::Round((Get-Item $fullFile).Length / 1MB, 2)
        Write-Host "File size: ${fileSize} MB" -ForegroundColor Gray
        
        Invoke-RestMethod -Uri "${uploadUrl}?name=DzfpPdfPrinter-Full-v1.2.0.exe" `
            -Method Post `
            -Headers @{
                "Accept" = "application/vnd.github+json"
                "Authorization" = "Bearer $Token"
                "Content-Type" = "application/octet-stream"
            } `
            -InFile $fullFile | Out-Null
        
        Write-Host "Full version uploaded!" -ForegroundColor Green
    } else {
        Write-Host "File not found: $fullFile" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host "Release published successfully!" -ForegroundColor Green
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Download URL:" -ForegroundColor White
    Write-Host "https://github.com/bao111231/dzfpAutoPrintf/releases/tag/v1.2.0" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Uploaded files:" -ForegroundColor White
    Write-Host "  DzfpPdfPrinter-Light-v1.2.0.exe (Light)" -ForegroundColor Green
    Write-Host "  DzfpPdfPrinter-Full-v1.2.0.exe (Full)" -ForegroundColor Green
    
} catch {
    Write-Host "`nError occurred!" -ForegroundColor Red
    Write-Host "Details: $_" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        $errBody = $reader.ReadToEnd()
        Write-Host "API Response: $errBody" -ForegroundColor Red
    }
    exit 1
}

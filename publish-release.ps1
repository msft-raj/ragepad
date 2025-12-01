# RagePad Release Script
# Creates a zip file ready for GitHub release

param(
    [string]$Version = (Get-Content "version.txt" -ErrorAction SilentlyContinue).Trim()
)

if (-not $Version) {
    $Version = "0.1"
}

Write-Host "Building RagePad v$Version..." -ForegroundColor Cyan

# Clean and publish
dotnet publish -c Release -r win-x64 --self-contained false -o .\publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Create release folder name
$ReleaseName = "RagePad-v$Version-win-x64"
$ZipFile = "$ReleaseName.zip"

# Remove old zip if exists
if (Test-Path $ZipFile) {
    Remove-Item $ZipFile -Force
}

# Create zip from publish folder
Write-Host "Creating $ZipFile..." -ForegroundColor Cyan
Compress-Archive -Path ".\publish\*" -DestinationPath $ZipFile -Force

# Show result
$ZipSize = (Get-Item $ZipFile).Length / 1MB
Write-Host ""
Write-Host "Release package created successfully!" -ForegroundColor Green
Write-Host "  File: $ZipFile" -ForegroundColor White
Write-Host "  Size: $([math]::Round($ZipSize, 2)) MB" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Go to: https://github.com/msft-raj/ragepad/releases/new" -ForegroundColor White
Write-Host "  2. Tag: v$Version" -ForegroundColor White
Write-Host "  3. Title: RagePad v$Version" -ForegroundColor White
Write-Host "  4. Drag and drop: $ZipFile" -ForegroundColor White
Write-Host "  5. Click 'Publish release'" -ForegroundColor White

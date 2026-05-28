# KBMS Uninstaller Script for Windows
# Run in PowerShell as Administrator

if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Warning "Please run Windows PowerShell as Administrator."
    exit
}

Write-Host "Starting KBMS Uninstallation on Windows..." -ForegroundColor Cyan

$ServiceName = "KBMSServer"
$InstallDir = "$env:ProgramFiles\KBMS"
$DataDir = "$env:ProgramData\KBMS"

# 1. Stop and Remove Service
if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Stopping and removing Windows Service..."
    
    # Force kill process to release file locks before stopping service
    $process = Get-Process kbms-server -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "Force killing kbms-server process..."
        Stop-Process -Name "kbms-server" -Force -ErrorAction SilentlyContinue
    }

    Stop-Service $ServiceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
}

# 2. Remove Environment Variables (PATH)
Write-Host "Removing PATH variables..."
$Path = [Environment]::GetEnvironmentVariable("Path", "Machine")
$PathParts = $Path -split ";"
$NewPathParts = $PathParts | Where-Object { $_ -ne "$InstallDir\server" -and $_ -ne "$InstallDir\cli" }
$NewPath = $NewPathParts -join ";"
[Environment]::SetEnvironmentVariable("Path", $NewPath, "Machine")

# 3. Remove Core Binaries
if (Test-Path $InstallDir) {
    Write-Host "Removing core binaries ($InstallDir)..."
    Remove-Item -Recurse -Force $InstallDir
}

# 4. Data Directory Warning
if (Test-Path $DataDir) {
    Write-Host "============================================================" -ForegroundColor Yellow
    Write-Host "WARNING: Your database files and configs are still located in $DataDir"
    Write-Host "To prevent accidental data loss, this script did NOT delete them."
    Write-Host "If you want to completely wipe all data, manually delete that folder."
    Write-Host "============================================================" -ForegroundColor Yellow
}

Write-Host "KBMS has been successfully uninstalled!" -ForegroundColor Green

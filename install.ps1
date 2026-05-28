# KBMS Core Installer for Windows
# Usage: iwr https://raw.githubusercontent.com/tranphat1506/KBMS/main/install.ps1 -useb | iex

Write-Host "========================================="
Write-Host " KBMS Core Installer (Windows)"
Write-Host "========================================="

# Requires Admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Error: Please run PowerShell as Administrator."
    exit 1
}

$Version = "3.5.0"
$RID = "win-x64"
$Repo = "tranphat1506/KBMS"

Write-Host "[1/4] Target Platform: $RID"

$InstallDir = "C:\Program Files\KBMS"
$DataDir = "C:\ProgramData\KBMS\data"
$ConfigDir = "C:\ProgramData\KBMS"

Write-Host "[2/4] Downloading KBMS Core v$Version..."
$TempDir = Join-Path $env:TEMP "kbms_install"
if (Test-Path $TempDir) { Remove-Item -Recurse -Force $TempDir }
New-Item -ItemType Directory -Path $TempDir | Out-Null

$ServerZip = "KBMS_Server_v${Version}_${RID}.zip"
$CliZip = "KBMS_CLI_v${Version}_${RID}.zip"

$ServerUrl = "https://github.com/$Repo/releases/download/v$Version/$ServerZip"
$CliUrl = "https://github.com/$Repo/releases/download/v$Version/$CliZip"

Write-Host "Downloading $ServerZip..."
Invoke-WebRequest -Uri $ServerUrl -OutFile "$TempDir\server.zip"
Write-Host "Downloading $CliZip..."
Invoke-WebRequest -Uri $CliUrl -OutFile "$TempDir\cli.zip"

Write-Host "[3/4] Installing to $InstallDir..."
if (-not (Test-Path "$InstallDir\server")) { New-Item -ItemType Directory -Path "$InstallDir\server" -Force | Out-Null }
if (-not (Test-Path "$InstallDir\cli")) { New-Item -ItemType Directory -Path "$InstallDir\cli" -Force | Out-Null }
if (-not (Test-Path $DataDir)) { New-Item -ItemType Directory -Path $DataDir -Force | Out-Null }

Expand-Archive -Path "$TempDir\server.zip" -DestinationPath "$InstallDir\server" -Force
Expand-Archive -Path "$TempDir\cli.zip" -DestinationPath "$InstallDir\cli" -Force

$ConfigFile = "$ConfigDir\kbms.ini"
$ConfigContent = @"
[Server]
host=127.0.0.1
port=3307
data_dir=$DataDir
max_connections=100
version=$Version
master_key=KBMS_V3_MASTER_SECRET_2026

[Root]
username=root
password=root

[Settings]
default_timeout=60
enable_audit_logs=true
"@
Set-Content -Path $ConfigFile -Value $ConfigContent

Write-Host "[4/4] Configuring Environment PATH..."
$Path = [Environment]::GetEnvironmentVariable("Path", "Machine")

# Add server to PATH if not exists
if ($Path -notlike "*$InstallDir\server*") {
    $Path += ";$InstallDir\server"
}
# Add CLI to PATH if not exists
if ($Path -notlike "*$InstallDir\cli*") {
    $Path += ";$InstallDir\cli"
}

[Environment]::SetEnvironmentVariable("Path", $Path, "Machine")

# Rename binaries to friendly commands for easy invocation
if (Test-Path "$InstallDir\server\KBMS.Server.exe") {
    Rename-Item -Path "$InstallDir\server\KBMS.Server.exe" -NewName "kbms-server.exe" -Force
}
if (Test-Path "$InstallDir\cli\KBMS.CLI.exe") {
    Rename-Item -Path "$InstallDir\cli\KBMS.CLI.exe" -NewName "kbms-cli.exe" -Force
}

Write-Host "[5/5] Registering KBMS as a Windows Service..."
$ServiceName = "KBMSServer"
if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
    Stop-Service $ServiceName -Force
    # Delete old service if exists via sc.exe (PowerShell 5 workaround)
    sc.exe delete $ServiceName | Out-Null
}
New-Service -Name $ServiceName -BinaryPathName "$InstallDir\server\kbms-server.exe" -DisplayName "KBMS Core Server" -Description "Thingent Knowledge Base Management System" -StartupType Automatic | Out-Null
Start-Service $ServiceName | Out-Null

Remove-Item -Recurse -Force $TempDir

Write-Host "========================================="
Write-Host " KBMS Core has been successfully installed!"
Write-Host " Configuration: $ConfigFile"
Write-Host " Data Directory: $DataDir"
Write-Host ""
Write-Host " IMPORTANT: Please restart your terminal (PowerShell/CMD) to use 'kbms-cli' globally."
Write-Host "========================================="

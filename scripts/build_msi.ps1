$ErrorActionPreference = "Stop"

Write-Host "========================================="
Write-Host " KBMS Windows MSI Builder (WiX Toolset)"
Write-Host "========================================="

$Version = "3.5.0"
$RID = "win-x64"
$RootDir = (Resolve-Path "..\").Path
$ScriptDir = $PSScriptRoot
$TempServerDir = "$RootDir\temp_server"
$TempCliDir = "$RootDir\temp_cli"
$WixDir = "$ScriptDir\.wix3"

# 1. Setup local WiX 3.11 binaries
if (-not (Test-Path "$WixDir\candle.exe")) {
    Write-Host "[1/5] Downloading WiX 3.11 binaries locally..."
    New-Item -ItemType Directory -Force -Path $WixDir | Out-Null
    $WixZip = "$WixDir\wix311.zip"
    Invoke-WebRequest -Uri "https://github.com/wixtoolset/wix3/releases/download/wix3112rtm/wix311-binaries.zip" -OutFile $WixZip
    Expand-Archive -Path $WixZip -DestinationPath $WixDir -Force
    Remove-Item $WixZip -Force
} else {
    Write-Host "[1/5] WiX 3.11 binaries already available."
}

# 2. Generate kbms.ini
Write-Host "[2/5] Generating kbms.ini..."
$IniContent = @"
[Server]
host=127.0.0.1
port=3307
data_dir=C:\ProgramData\KBMS\data
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
Set-Content -Path "$RootDir\kbms.ini" -Value $IniContent

# 3. Build Server & CLI
Write-Host "[3/5] Building .NET Components (Server & CLI) for $RID..."
Set-Location $RootDir
dotnet publish KBMS.Server/KBMS.Server.csproj -c Release -r $RID --self-contained true -p:PublishSingleFile=true -o $TempServerDir
dotnet publish KBMS.CLI/KBMS.CLI.csproj -c Release -r $RID --self-contained true -p:PublishSingleFile=true -o $TempCliDir
Set-Location $ScriptDir

# Copy .exe for convenience (Avoid Move-Item lock issues)
if (Test-Path "$TempServerDir\KBMS.Server.exe") { Copy-Item "$TempServerDir\KBMS.Server.exe" "$TempServerDir\kbms-server.exe" -Force }
if (Test-Path "$TempCliDir\KBMS.CLI.exe") { Copy-Item "$TempCliDir\KBMS.CLI.exe" "$TempCliDir\kbms-cli.exe" -Force }

# 4. Skip Heat (Already hardcoded in WXS)
Write-Host "[4/5] Skipping heat.exe (Files manually defined in WXS)..."

# 5. Run WiX Candle & Light
Write-Host "[5/5] Compiling and Linking MSI with candle.exe and light.exe..."
& "$WixDir\candle.exe" -arch x64 KBMS_Win_Setup.wxs
& "$WixDir\light.exe" -ext WixUIExtension KBMS_Win_Setup.wixobj -out KBMS_v${Version}_${RID}.msi

# Cleanup
Write-Host "Cleaning up..."
Remove-Item -Recurse -Force $TempServerDir
Remove-Item -Recurse -Force $TempCliDir
Remove-Item "*.wixobj" -ErrorAction SilentlyContinue

Write-Host "========================================="
Write-Host " Successfully built KBMS_v${Version}_${RID}.msi"
Write-Host "========================================="
